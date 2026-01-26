using Cysharp.Threading.Tasks; 
using DG.Tweening;
using System; 
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    private IEnemy _target;

    private static readonly Collider[] _searchBuffer = new Collider[10]; // 검색할 최대 타겟 수 

    private WeaponStat _stat; // 무기 스탯
    private Rigidbody _rb; // 리지드바디
    private Collider _collider; // 콜라이더
    private CancellationTokenSource _lifeTimeCts; // 유니태스크 토큰
    private Transform _launcher; // 투사체 위치
    private int boundValue = 3;
    private int currentbound = 0;
    
    private int _currentTargetId;
    private bool debug = false;

    private Vector3 projectileSize;
    [SerializeField] private LayerMask _targetMask;

    
    
    private void Awake()
    {
        TryGetComponent<Rigidbody>(out _rb);
        TryGetComponent<Collider>(out _collider);
        _rb.useGravity = false;
        debug = DebugManager.instance.IsDebug;
        projectileSize = this.transform.localScale;
    }

    public void Initialize(WeaponStat stat)
    {
        this._stat = stat;
    }

    /// <summary>
    /// WeaponMount에서 호출되어 투사체 발사
    /// </summary>
    public void Launch(WeaponStat data, IEnemy target, Transform firePoint)
    {
        if (_collider != null)
            _collider.enabled = true;

        this._stat = data;
        this._target = target;
        this._launcher = firePoint;
        this.currentbound = 0;

        if (target != null)
        {
            // 유니티 오브젝트의 고유 ID 혹은 커스텀 ID 저장
            _currentTargetId = target.TargetTransform.gameObject.GetInstanceID();
        }

        // 이전의 비행 로직(DOTween, UniTask)이 남아있다면 취소
        _lifeTimeCts?.Cancel();
        _lifeTimeCts = new CancellationTokenSource();
        transform.DOKill(); // DOTween 중지

        // 10초 후 자동 파괴(풀 반환) 로직 시작
        StartLifetimeLimit(3.0f, _lifeTimeCts.Token).Forget();

        // 비유도 타입
        if (!data.IsHoming)
        {
            Vector3 fireDirection = firePoint.forward;
            fireDirection.y = 0f;
            fireDirection.Normalize();
            //_rb.AddForce(fireDirection * data.projectileSpeed, ForceMode.VelocityChange);
            _rb.linearVelocity = fireDirection * data.projectileSpeed;
            _rb.useGravity = false;
        }
        else //if (data.IsHoming)
        {
            HomingLoop(_lifeTimeCts.Token).Forget(); // 유도 타입
        }
    }

    /// <summary>
    /// 타겟을 추적하는 비동기 루프
    /// </summary>
    private async UniTaskVoid HomingLoop(CancellationToken token)
    {
        float currentSpeed = _stat.projectileSpeed;
        float maxRangeSqr = _stat.Range * _stat.Range;

        //var rb = _rb;
        //var tr = transform;

        while (!token.IsCancellationRequested)
        {
            if (_rb == null || _launcher == null) break;
            //if (rb == null || tr == null) break;

            /*bool isTargetActive = _target != null && _target.IsAlive && _target.TargetTransform.gameObject.activeInHierarchy;

            if (!isTargetActive)
            {
                rb.useGravity = true; 
                break;
            }*/

            if (currentbound > boundValue)
            {
                Disable().Forget();
                break;
            }

            float distFromLauncherSqr = (transform.position - _launcher.position).sqrMagnitude;
            if (distFromLauncherSqr > maxRangeSqr * 1.44f) // 사거리의 1.2배 지점에서 소멸
            {
                Disable().Forget();
                break;
            }

            bool isTargetValid = _target != null && _target.IsAlive &&
                                 _target.TargetTransform.gameObject.activeInHierarchy &&
                                 _target.TargetTransform.gameObject.GetInstanceID() == _currentTargetId;

            if (!isTargetValid)            
            {             
                _target = FindNewTarget();

                if (_target != null) 
                {
                    _currentTargetId = _target.TargetTransform.gameObject.GetInstanceID();
                }
                else
                {
                    _rb.useGravity = true;
                    break;
                }
            }


            //Vector3 direction = (_target.TargetTransform.position - transform.position).normalized;
            Vector3 toTarget = _target.TargetTransform.position - transform.position;
            float distance = toTarget.magnitude;
            Vector3 direction = toTarget / distance;
            direction.y += 0.2f;

            Quaternion lookRotation = Quaternion.LookRotation(direction);
            float proximityBonus = distance < 3.0f ? 2.0f : 1.0f;

            _rb.rotation = Quaternion.Slerp(_rb.rotation, lookRotation, _stat.HomingTurnSpeed * proximityBonus * Time.deltaTime);
            currentSpeed += _stat.Acceleration * Time.deltaTime;
            _rb.linearVelocity = transform.forward * currentSpeed;

            await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);
        }
    }

    private IEnemy FindNewTarget()
    {
        currentbound++;
        Vector3 searchOrigin = _launcher != null ? _launcher.position : transform.position;
        float searchRadius = _stat.Range;

        int count = Physics.OverlapSphereNonAlloc
        (
            searchOrigin, // 플레이어 중심 기준
            searchRadius, // 무기 범위
            _searchBuffer, // 최대 검색할 적 수
            _targetMask // 타겟 레이어
        );

        IEnemy closestEnemy = null;
        float minDistanceSqr = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            if (_searchBuffer[i].TryGetComponent<IEnemy>(out var enemy) && enemy.IsAlive)
            {
                float distSqr = (enemy.TargetTransform.position - transform.position).sqrMagnitude;
                if (distSqr < minDistanceSqr)
                {
                    minDistanceSqr = distSqr;
                    closestEnemy = enemy;
                }
            }
        }
        return closestEnemy;
    }

    /// <summary>
    /// 투사체 수명 제한
    /// </summary>
    private async UniTaskVoid StartLifetimeLimit(float duration, CancellationToken token)
    {
        // duration 만큼 대기
        bool canceled = await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: token)
                                     .SuppressCancellationThrow();

        if (!canceled)
        {
            Disable().Forget();
        }
    }

    /// <summary>
    /// 충돌 감지 
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IEnemy>(out var enemy))
        {
            if (enemy != null && enemy.IsAlive)
            {
                OnHitTarget(enemy);
            }
        }
    }

    /// <summary>
    /// 타겟에 명중 (OnTriggerEnter 또는 OnCannonHitTarget에서 호출됨)
    /// </summary>
    private void OnHitTarget(IEnemy enemy) // [수정] enemy = null 기본값 제거
    {
        if (_collider != null)
            _collider.enabled = false;

        if (enemy != null && enemy.IsAlive)
        {
            if (debug)
            {
                enemy.TakeDamage(1, out int finalDmg);
            }
            else
            {
                //int finalDamage;
                enemy.TakeDamage(_stat.Damage, out int finalDamage);                
            }
        }

        // 피격 이펙트 (shotEffect) 생성
        if (_stat.shotEffect != null)
        {
            var hitEffect = ObjectPoolManager.Instance.Get(_stat.shotEffect);
            if (hitEffect != null)
            {
                hitEffect.transform.position = transform.position;
            } 
        }

        Disable().Forget();
    }


    /// <summary>
    /// 모든 작업을 중지하고 오브젝트 풀에 반환
    /// </summary>
    private void ReturnToPool()
    {
        if (_lifeTimeCts == null) return;

        _lifeTimeCts?.Cancel();
        _lifeTimeCts = null;

        transform.DOKill();

        ObjectPoolManager.Instance.Return(this.gameObject);
        transform.localScale = projectileSize;
    }


    private async UniTaskVoid Disable()
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        _lifeTimeCts?.Cancel();

        await transform.DOScale(Vector3.zero, 0.5f)
                       .SetEase(Ease.OutQuint)
                       .SetLink(this.gameObject) // 오브젝트 파괴 시 안전장치
                       .ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());


        // 3. 애니메이션 종료 후 풀 반환
        ReturnToPool();
    }

    private void OnDisable()
    {
        _lifeTimeCts?.Cancel();
        _lifeTimeCts?.Dispose();
        _lifeTimeCts = null;

        transform.DOKill();
    }
}