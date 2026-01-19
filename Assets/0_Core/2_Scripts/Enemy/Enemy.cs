using MoreMountains.Feedbacks;
using UnityEngine;

public class Enemy : MonoBehaviour, IEnemy
{
    [SerializeField] private int maxHealth = 100;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth; 

    private bool _isAlive = true;
    public Transform TargetTransform => transform;
    public bool IsAlive => _isAlive;
    public MMF_Player flickerFeedback;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        _isAlive = true;
        flickerFeedback = GetComponent<MMF_Player>();
    }

    private void Start()
    {      
        CurrentHealth = maxHealth;
        _isAlive = true;
    }

    private void OnEnable()
    {
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterEnemy(this);
        }
    }

    private void OnDisable()
    {
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterEnemy(this);
            //Debug.Log("enemy 리스트에서 제외");
        }
    }

    public void TakeDamage(int damage)
    {
        if (!_isAlive) return;

        flickerFeedback?.PlayFeedbacks();

        CurrentHealth -= damage;
        //Debug.Log($"{name}이(가) {damage} 데미지를 받음. 남은 체력: {CurrentHealth}");
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
        //Debug.Log($"{name} 사망");
        var item = EnemyManager.Instance.GetDrop(this.transform.position);
        //Debug.Log("드랍물 생성");

        // 오브젝트 풀을 사용하므로 Destroy 대신 비활성화
         gameObject.SetActive(false); // OnDisable이 자동으로 호출됨
        //ObjectPoolManager.Instance.Return(this.gameObject);
    }
}