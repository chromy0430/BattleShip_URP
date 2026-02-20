using Battleship_Enum;
using Cysharp.Threading.Tasks;
using MoreMountains.Feedbacks;
using System;
using UnityEngine;

public class Enemy : MonoBehaviour, IEnemy
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int _Shield = 0;
    [SerializeField] private float _attackRadius = 2.5f;
        
    public bool canAttack;
    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth; 
    public bool IsKnockbacked => _isKnockbacked;
    public bool IsAlive => _isAlive;

    public Transform TargetTransform => transform;    
    
    public MMF_Player flickerFeedback;
    public GameObject damageObject; // 데미지 폰트
    public GameObject wakeTrailObject;

    private EnemyData _enemyData;    
    private TrailRenderer _wakeTrail;
    private bool _isAlive = false;
    private bool _isKnockbacked = false; // 넉백 상태
    
    public void Initialize(EnemyData data)
    {
        canAttack = true;
        _enemyData = data;
        maxHealth = data.MaxHP;
        _Shield = data.DEF;
        CurrentHealth = maxHealth;

        if (TryGetComponent<EnemyChaser>(out var chaser))
        {
            chaser.moveSpeed = data.MoveSpeed;
            chaser.turnSpeed = data.TurnSpeed;
        }

        if (RadarSystem.Instance != null && _enemyData != null)
        {
            RadarSystem.Instance.AddTarget(gameObject, _enemyData.MinimapIcon);
        }

        transform.localScale = transform.localScale * (data.SizeType == EnemySizeType.Large ? 2f : 1);
    }

    private void Awake()
    {
        flickerFeedback = GetComponent<MMF_Player>();
        if (wakeTrailObject != null)
        {
            _wakeTrail = wakeTrailObject.GetComponentInChildren<TrailRenderer>();            
        }
    }

    private void OnEnable()
    {
        CurrentHealth = maxHealth;
        _isAlive = true;
        canAttack = true;

        if (wakeTrailObject != null) wakeTrailObject.SetActive(true);
        ResetTrailRoutine().Forget();


        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterEnemy(this);
        }

        if (RadarSystem.Instance != null && _enemyData != null)
        {
            RadarSystem.Instance.AddTarget(gameObject, _enemyData.MinimapIcon);
        }
    }

    private void OnDisable()
    {
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterEnemy(this);
        }

        if (RadarSystem.Instance != null)
        {
            RadarSystem.Instance.RemoveTarget(this.gameObject);
        }

        if (_wakeTrail != null)
        {
            _wakeTrail.Clear();
            _wakeTrail.enabled = false;
        }
    }

    private void Update()
    {
        if (!_isAlive || !canAttack || _isKnockbacked) return;

        if (PlayerManager.Instance != null && PlayerManager.Instance.PlayerTransform != null)
        {
            float distSqr = (transform.position - PlayerManager.Instance.PlayerTransform.position).sqrMagnitude;
            if (distSqr <= _attackRadius * _attackRadius)
            {
                AttackPlayer();
            }
        }
    }

    private void AttackPlayer()
    {
        canAttack = false;

        // 플레이어에게 데미지 전달
        PlayerCondition pc = PlayerManager.Instance.PlayerTransform.GetComponent<PlayerCondition>();
        if (pc != null)
        {
            pc.TakeDamage(_enemyData.Damage);
            PlayerManager.Instance.Damaged();
        }

        ResumeAttackAbility().Forget();
    }

    public void TakeDamage(int damage, out int finalDamage)
    {
        if (!_isAlive)
        {
            finalDamage = 0;
            return;
        }

        finalDamage = Mathf.Max(1, damage - _Shield);

        flickerFeedback?.PlayFeedbacks();

        var damageFont = ObjectPoolManager.Instance.Get(damageObject);
        damageFont.GetComponent<DamageTMP>().Initialize
            (
                Base_Canvas.Instance.transform,
                transform.position,
                finalDamage.ToString()
            );

        CurrentHealth -= finalDamage;
        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    public void InstantKill()
    {
        if (!_isAlive) return;

        Die();
    }

    private void Die()
    {
        _isAlive = false;

        if (wakeTrailObject != null) wakeTrailObject.SetActive(false);

        if (DropManager.Instance != null)
        {
            int reward = _enemyData != null ? _enemyData.ExpReward : 10;
            DropManager.Instance.SpawnDrops(this.transform.position, reward);
        }

        flickerFeedback?.ResetFeedbacks();

        if (_enemyData != null && AddressableEnemyPoolManager.Instance != null)
        {
            AddressableEnemyPoolManager.Instance.ReturnEnemy(this.gameObject, _enemyData);
        }
    }

    public async UniTaskVoid ApplyKnockback(Vector3 force, float duration = 0.2f)
    {
        if (!_isAlive) return;

        _isKnockbacked = true;
        Vector3 knockbackVelocity = force;
        knockbackVelocity.y = 0;
        float elapsed = 0f;        

        while (elapsed < duration)
        {
            if (!_isAlive) break;

            float dt = Time.fixedDeltaTime;
            elapsed += dt;

            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, dt * 5.0f);
            transform.position += knockbackVelocity * dt;

            await UniTask.WaitForFixedUpdate();
        }
        _isKnockbacked = false; 
    }

    private async UniTaskVoid ResumeAttackAbility(float delay = 0.5f)
    {
        // UniTask.Delay는 비동기적으로 작동하며, 해당 시간만큼 대기합니다.
        await UniTask.Delay(TimeSpan.FromSeconds(delay), ignoreTimeScale: false);
        if (_isAlive)
        { 
            canAttack = true;
        }
    }

    private async UniTaskVoid ResetTrailRoutine()
    {
        if (_wakeTrail == null) return;

        // 일단 끄고 초기화
        _wakeTrail.enabled = false;
        _wakeTrail.Clear();

        // 위치 이동이 완전히 끝날 때까지 1프레임 대기 (EndOfFrame)
        await UniTask.WaitForEndOfFrame(this);

        // 다시 초기화 후 활성화
        if (_isAlive) // 살아있는지 체크 (대기 중에 죽었을 수도 있음)
        {
            _wakeTrail.Clear(); // 켜기 직전에 한 번 더 지움 (확실하게)
            _wakeTrail.enabled = true;
        }
    }
}