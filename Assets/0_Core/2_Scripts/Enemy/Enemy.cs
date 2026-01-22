using Cysharp.Threading.Tasks;
using MoreMountains.Feedbacks;
using UnityEngine;

public class Enemy : MonoBehaviour, IEnemy
{
    [SerializeField] private int maxHealth = 100;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth; 

    private bool _isAlive = false;
    public Transform TargetTransform => transform;
    public bool IsAlive => _isAlive;
    public MMF_Player flickerFeedback;

    [SerializeField] private Rigidbody _rb;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        _isAlive = false;
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

    public void TakeDamage(int damage)
    {
        if (!_isAlive) return;

        flickerFeedback?.PlayFeedbacks();

        CurrentHealth -= damage;
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

        var item = EnemyManager.Instance.GetDrop(this.transform.position);        

        flickerFeedback?.ResetFeedbacks();
        ObjectPoolManager.Instance.Return(this.gameObject);
    }
}