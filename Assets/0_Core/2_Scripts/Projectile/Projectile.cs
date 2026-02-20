using Cysharp.Threading.Tasks; 
using DG.Tweening;
using System; 
using System.Threading;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    #region 필드
    // 컴포넌트
    private IEnemy _target;  
    private WeaponStat _stat; // 무기 스탯
    private CancellationTokenSource _lifeTimeCts; // 유니태스크 토큰
    private Transform _launcher; // 투사체 위치
        
    private int boundValue = 3; // 유도 재탐색 횟수
    private int currentbound = 0; // 현재 탐색 수
    private int _currentTargetId; // 탐색된 타겟 아이디
    private int _currentPierceCount; // 관통 관련

    // 디버그 전용
    private bool debug = false;
    private Vector3 projectileSize;

    // 피해 관련
    private int _finalCachedDamage;

    [Header("충돌 판정 반경")]
    [SerializeField] private float hitRadius = 1.5f; // 콜라이더를 대체하는 수학적 충돌 반경
    #endregion

    #region 초기화
    private void Awake()
    {
        debug = DebugManager.instance.IsDebug;
        projectileSize = this.transform.localScale;
    }

    public void Initialize(WeaponStat stat)
    {
        this._stat = stat;
    }
    #endregion

    #region 핵심 로직 (투사체 발사)
    /// <summary>
    /// WeaponMount에서 호출되어 투사체 발사
    /// </summary>
    public void Launch(WeaponStat data, IEnemy target, Transform firePoint)
    {
        this._stat = data;
        this._target = target;
        this._launcher = firePoint;
        this.currentbound = 0;
        this._currentPierceCount = 0;

        _finalCachedDamage = PlayerDataLoad.Instance != null
            ? PlayerDataLoad.Instance.CurrentPlayerStat.GetFinalDamage(data.Damage)
            : data.Damage;

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

        FlightLoop(_lifeTimeCts.Token).Forget();
    }
    #endregion

    #region UniTask 함수
    /// <summary>
    /// 리지드바디를 대체하는 순수 수학 비행 루프
    /// </summary>
    private async UniTaskVoid FlightLoop(CancellationToken token)
    {
        float currentSpeed = _stat.projectileSpeed;
        float maxRangeSqr = _stat.Range * _stat.Range;
        Vector3 direction = _launcher != null ? _launcher.forward : transform.forward;
        direction.y = 0f;
        direction.Normalize();

        while (!token.IsCancellationRequested)
        {
            if (_launcher == null) break;

            float deltaTime = Time.deltaTime;

            // 1. 방향 및 속도 연산
            if (_stat.IsHoming)
            {
                if (currentbound > boundValue || (transform.position - _launcher.position).sqrMagnitude > maxRangeSqr * 1.44f)
                {
                    Disable().Forget(); break;
                }

                bool isTargetValid = _target != null && _target.IsAlive && _target.TargetTransform.gameObject.activeInHierarchy && _target.TargetTransform.gameObject.GetInstanceID() == _currentTargetId;

                if (!isTargetValid)
                {
                    _target = FindNewTarget();
                    if (_target != null) _currentTargetId = _target.TargetTransform.gameObject.GetInstanceID();
                    else break; // 타겟 없으면 직진
                }

                if (_target != null)
                {
                    Vector3 toTarget = _target.TargetTransform.position - transform.position;
                    float distance = toTarget.magnitude;
                    if (distance > 0.001f)
                    {
                        Vector3 desiredDir = toTarget / distance;
                        desiredDir.y += 0.2f;
                        Quaternion lookRotation = Quaternion.LookRotation(desiredDir);
                        float proximityBonus = distance < 3.0f ? 2.0f : 1.0f;
                        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, _stat.HomingTurnSpeed * proximityBonus * deltaTime);
                        direction = transform.forward;
                    }
                }
                currentSpeed += _stat.Acceleration * deltaTime;
            }

            // 2. 이동 (Transform 직접 조작)
            transform.position += direction * (currentSpeed * deltaTime);

            // 3. 충돌 감지 (EnemyBatchManager의 공간 그리드 활용 - 콜라이더 대체)
            if (EnemyBatchManager.Instance != null)
            {
                EnemyChaser hitEnemyChaser = EnemyBatchManager.Instance.GetClosestEnemyInRadius(transform.position, hitRadius);
                if (hitEnemyChaser != null && hitEnemyChaser.status.IsAlive)
                {
                    OnHitTarget(hitEnemyChaser.status);

                    if (_currentPierceCount > _stat.PierceCount)
                    {
                        Disable().Forget();
                        break;
                    }
                }
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }

    private IEnemy FindNewTarget()
    {
        currentbound++;
        // 탐색도 물리 OverlapSphere 대신 BatchManager에 요청 가능하지만, 현재 로직 유지해도 무방 (부하 미미함)
        return EnemyBatchManager.Instance?.GetClosestEnemyInRadius(transform.position, _stat.Range)?.status;
    }

    private void OnHitTarget(IEnemy enemy)
    {
        if (enemy != null && enemy.IsAlive)
        {
            int dmg = debug ? 1 : _finalCachedDamage;
            enemy.TakeDamage(dmg, out int finalDamage);
        }

        if (_stat.shotEffect != null)
        {
            var hitEffect = ObjectPoolManager.Instance.Get(_stat.shotEffect);
            if (hitEffect != null) hitEffect.transform.position = transform.position;
        }

        _currentPierceCount++;
    }

    /// <summary>
    /// 투사체 수명 제한
    /// </summary>
    private async UniTaskVoid StartLifetimeLimit(float duration, CancellationToken token)
    {
        bool canceled = await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: token).SuppressCancellationThrow();
        if (!canceled) Disable().Forget();
    }


    /// <summary>
    /// 투사체 비활성화 함수
    /// </summary>
    /// <returns></returns>
    private async UniTaskVoid Disable()
    {
        _lifeTimeCts?.Cancel();
        await transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.OutQuint).SetLink(this.gameObject).ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
        ReturnToPool();
    }
    #endregion

    #region 충돌 관련
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
    #endregion

    #region 비활성화 
    /// <summary>
    /// 모든 작업을 중지하고 오브젝트 풀에 반환
    /// </summary>
    private void ReturnToPool()
    {
        _lifeTimeCts?.Cancel();
        _lifeTimeCts = null;
        transform.DOKill();
        ObjectPoolManager.Instance.Return(this.gameObject);
        transform.localScale = projectileSize;
    }

    private void OnDisable()
    {
        _lifeTimeCts?.Cancel();
        _lifeTimeCts?.Dispose();
        _lifeTimeCts = null;

        transform.DOKill();
    }
    #endregion
}