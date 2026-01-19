using UnityEngine;
using Cysharp.Threading.Tasks; 
using System.Threading;
using DG.Tweening;
using System; 

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    private WeaponStat _stat;
    private IEnemy _target;
    private Rigidbody _rb;
    private Collider _collider;
    private CancellationTokenSource _lifeTimeCts;
    private bool debug = false;
    private Vector3 projectileSize;

    [SerializeField] private LayerMask waterLayer;
    public GameObject DamageObject; // 데미지 폰트
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

        // 이전의 비행 로직(DOTween, UniTask)이 남아있다면 취소
        _lifeTimeCts?.Cancel();
        _lifeTimeCts = new CancellationTokenSource();
        transform.DOKill(); // DOTween 중지

        // 10초 후 자동 파괴(풀 반환) 로직 시작
        StartLifetimeLimit(3.0f, _lifeTimeCts.Token).Forget();
        //DelayDestroy(2.5f, _lifeTimeCts.Token).Forget();

        // --- 데이터 타입별 비행 로직 분기 ---

        if (!data.IsHoming)
        {
            Vector3 fireDirection = firePoint.forward;
            fireDirection.y = 0f;
            fireDirection.Normalize();
            _rb.AddForce(fireDirection * data.projectileSpeed, ForceMode.VelocityChange);

            //_rb.AddForce(firePoint.forward * data.projectileSpeed, ForceMode.VelocityChange);
            _rb.useGravity = false;
        }
        /*else if (_stat is ArcWeaponData cannon)
        {
            // [Cannon] 포물선 비행 (기획서 우선순위)
            if (_target != null && _target.IsAlive)
            {
                Vector3 targetPos = _target.TargetTransform.position;
                // 중간 정점 계산 (포물선 높이 적용)
                Vector3 midPoint = (transform.position + targetPos) / 2f + (Vector3.up * cannon.arcHeight);
                Vector3[] path = { midPoint, targetPos };

                //_rb.isKinematic = true; // DOPath 사용 시 물리 비활성화

                // [수정] OnComplete가 파라미터 없는 'OnCannonHitTarget'를 호출하도록 변경
                transform.DOPath(path, cannon.projectileSpeed, PathType.CatmullRom)
                    .SetSpeedBased()
                    .SetEase(Ease.Linear)
                    .OnComplete(OnCannonHitTarget); // <-- [수정] '메서드 그룹' 오류 해결
            }
            else
            {
                //Disable();
                ReturnToPool(); // 타겟이 없으면 즉시 반환
            }
        }*/
        else if (data.IsHoming)
        {
            HomingLoop(_lifeTimeCts.Token).Forget();
            //HomingLoop(data, _lifeTimeCts.Token).Forget(); // 유도 루프 시작
        }
    }

    /// <summary>
    /// 타겟을 추적하는 비동기 루프
    /// </summary>
    private async UniTaskVoid HomingLoop(CancellationToken token)
    {
        float currentSpeed = _stat.projectileSpeed;
        var rb = _rb;
        var tr = transform;

        while (!token.IsCancellationRequested)
        {
            if (rb == null || tr == null) break;

            bool isTargetActive = _target != null && _target.IsAlive && _target.TargetTransform.gameObject.activeInHierarchy;

            if (!isTargetActive)
            {
                rb.useGravity = true; 
                break;
            }

            Vector3 direction = (_target.TargetTransform.position - transform.position).normalized;
            direction.y += .2f;
            Quaternion lookRotation = Quaternion.LookRotation(direction);

            rb.rotation = Quaternion.Slerp(rb.rotation, lookRotation, _stat.HomingTurnSpeed * Time.deltaTime);

            currentSpeed += _stat.Acceleration * Time.deltaTime;

            rb.linearVelocity = tr.forward * currentSpeed;

            await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
        }
    }

    private async UniTaskVoid DelayDestroy(float duration, CancellationToken token)
    {
        bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: token)
                                       .SuppressCancellationThrow();

        if (!isCanceled)
        {
            Disable().Forget();
        }
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

        /*if ((waterLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            Debug.Log("바다에 닿음");
            OnHitTarget(null);
        }*/
    }

    /// <summary>
    /// DOPath.OnComplete를 위한 파라미터 없는 콜백
    /// </summary>
    private void OnCannonHitTarget()
    {
        if (!this.gameObject.activeSelf) return;
        OnHitTarget(_target);
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
                enemy.TakeDamage(1);
            }
            else
            {
                enemy.TakeDamage(_stat.Damage);
                var damageFont = ObjectPoolManager.Instance.Get(DamageObject);
                damageFont.GetComponent<DamageTMP>().Initialize
                    (
                        Base_Canvas.Instance.transform,
                        transform.position,
                        _stat.Damage.ToString()
                    );
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