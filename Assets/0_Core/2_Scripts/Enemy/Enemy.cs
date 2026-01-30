using Cysharp.Threading.Tasks;
using MoreMountains.Feedbacks;
using UnityEngine;
using Battleship_Enum;

public class Enemy : MonoBehaviour, IEnemy
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int _Shield = 0;

    private EnemyData _enemyData;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth; 

    private bool _isAlive = false;
    public Transform TargetTransform => transform;
    public bool IsAlive => _isAlive;
    public MMF_Player flickerFeedback;

    [SerializeField] private Rigidbody _rb;
    public GameObject DamageObject; // 데미지 폰트
    private bool _isKnockbacked = false; // 넉백 상태
    public bool IsKnockbacked => _isKnockbacked;
    public bool canAttack;
    public void Initialize(EnemyData data)
    {
        canAttack = true;
        _enemyData = data;

        maxHealth = data.MaxHP;
        _Shield = data.Shield;

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

        transform.localScale = new Vector3(2, 3, 2) * (data.SizeType == EnemySizeType.Large ? 2f : 1);
    }

    private void Awake()
    {
        //CurrentHealth = maxHealth;
        //_isAlive = false;
        flickerFeedback = GetComponent<MMF_Player>();
        _rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        CurrentHealth = maxHealth;
        _isAlive = true;

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterEnemy(this);
        }

        if (RadarSystem.Instance != null && _enemyData != null)
        {
            RadarSystem.Instance.AddTarget(gameObject, _enemyData.MinimapIcon);
        }

        EnableInterpolationDelay().Forget();
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
    }

    private async UniTaskVoid EnableInterpolationDelay()
    {
        if (_rb != null) _rb.interpolation = RigidbodyInterpolation.None;

        await UniTask.WaitForFixedUpdate();

        if (_rb != null && _isAlive)
        {
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
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

        var damageFont = ObjectPoolManager.Instance.Get(DamageObject);
        damageFont.GetComponent<DamageTMP>().Initialize
            (
                Base_Canvas.Instance.transform,
                transform.position,
                finalDamage.ToString()
            //_stat.Damage.ToString()
            );

        CurrentHealth -= finalDamage;
        //CurrentHealth -= damage;
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

        if (_rb != null)
        {
            _rb.interpolation = RigidbodyInterpolation.None;
        }

        if (DropManager.Instance != null)
        {
            // [수정] 직접 생성하지 않고 DropManager에게 요청
            int reward = _enemyData != null ? _enemyData.ExpReward : 10;

            // 이 함수 하나로 "신규 생성" 혹은 "주변 보석과 병합"이 자동으로 결정됨
            DropManager.Instance.SpawnDrops(this.transform.position, reward);
        }


        flickerFeedback?.ResetFeedbacks();

        if (_enemyData != null && AddressableEnemyPoolManager.Instance != null)
        {
            AddressableEnemyPoolManager.Instance.ReturnEnemy(this.gameObject, _enemyData);
        }
        else
        {
            ObjectPoolManager.Instance.Return(this.gameObject);
        }

        //ObjectPoolManager.Instance.Return(this.gameObject);
    }

    public async UniTaskVoid ApplyKnockback(Vector3 force, float duration = 0.2f)
    {
        if (!_isAlive || _rb == null) return;

        _isKnockbacked = true;
        _rb.linearVelocity = Vector3.zero; // 기존 속도 초기화
        force.y = 0;
        _rb.AddForce(force, ForceMode.Impulse);

        // duration 동안 대기 후 상태 해제
        await UniTask.Delay(System.TimeSpan.FromSeconds(duration));

        _isKnockbacked = false; 
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            collision.collider.TryGetComponent<PlayerCondition>(out PlayerCondition pc);

            // 데미지 처리 후 플레이어 몸체 붉은색 표시 및 넉백 + 잠깐의 멈춤
            if (canAttack)
            {
                canAttack = false;
                pc.TakeDamage(_enemyData.Damage);
                PlayerManager.Instance.Damaged();
                UniTask.Delay(500);
            }
        }
    }
}