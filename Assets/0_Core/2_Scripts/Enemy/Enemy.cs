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

    public void Initialize(EnemyData data)
    {
        _enemyData = data;

        maxHealth = data.MaxHP;
        _Shield = data.Shield;

        CurrentHealth = maxHealth;

        if (TryGetComponent<EnemyChaser>(out var chaser))
        {
            chaser.moveSpeed = data.MoveSpeed;
            chaser.turnSpeed = data.TurnSpeed;
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

        EnableInterpolationDelay().Forget();
    }

    private void OnDisable()
    {
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterEnemy(this);
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

        if (EnemyManager.Instance != null)
        {
            GameObject dropObj = EnemyManager.Instance.GetDrop(this.transform.position);

            // 2. EnemyDrop 컴포넌트를 가져와서 데이터 주입
            if (dropObj != null && dropObj.TryGetComponent<EnemyDrop>(out var dropItem))
            {
                // _enemyData가 null이 아닐 때만 경험치 적용 (안전장치)
                int reward = _enemyData != null ? _enemyData.ExpReward : 10;
                dropItem.Initialize(reward);
            }
        }

        //var item = EnemyManager.Instance.GetDrop(this.transform.position);

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
}