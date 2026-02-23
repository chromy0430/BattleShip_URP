using UnityEngine;
using System;

public class PlayerCondition : MonoBehaviour
{
    [Header("상태 확인용")]
    [SerializeField] private int _currentHp;
    [SerializeField] private int _currentMp;

    private int _prevMaxHp;
    private int _prevMaxMp;

    private PlayerStat stat;

    public int CurrentHp => _currentHp;
    public int CurrentMp => _currentMp;

    // 현재값과 최대값 UI, 사망 이벤트
    public event Action<int, int> OnHpChanged;
    public event Action<int, int> OnMpChanged;
    public event Action OnPlayerDead;

    public GameObject DamageFont;

    private void Start()
    {        
        // 게임 시작 시 최대치
        if (PlayerDataLoad.Instance != null)
        {
            stat = PlayerDataLoad.Instance.CurrentPlayerStat;
            
            _prevMaxHp = stat.GetFinalMaxHP();
            _prevMaxMp = stat.GetFinalMaxMP();

            _currentHp = _prevMaxHp;
            _currentMp = _prevMaxMp;
            
            // 이벤트 구독
            PlayerDataLoad.Instance.OnStatUpdated += HandleStatUpdated;

            // UI 초기화
            NotifyHpChanged();
            NotifyMpChanged();
        }
    }

    private void OnDisable()
    {
        if (PlayerDataLoad.Instance != null)
        {
            PlayerDataLoad.Instance.OnStatUpdated -= HandleStatUpdated;
        }
    }

    // 업그레이드로 최대 체력/마력이 늘어났을 때 호출됨
    private void HandleStatUpdated()
    {
        var stat = PlayerDataLoad.Instance.CurrentPlayerStat;
        int newMaxHp = stat.GetFinalMaxHP();
        int newMaxMp = stat.GetFinalMaxMP();

        int diffHp = newMaxHp - _prevMaxHp;
        int diffMp = newMaxMp - _prevMaxMp;

        if (diffHp > 0)
        {
            _currentHp += diffHp;
            Debug.Log($"[Condition] 최대 체력 증가({diffHp})로 인해 체력 회복됨.");
        }

        if (diffMp > 0)
        {
            _currentMp += diffMp;
        }

        _prevMaxHp = newMaxHp;
        _prevMaxMp = newMaxMp;

        NotifyHpChanged();
        NotifyMpChanged();
    }

    #region 체력 (HP) 로직
    public void TakeDamage(int amount)
    {
        if (_currentHp <= 0) return; // 이미 사망

        int finalDmg = Mathf.Max(1, amount - (stat.shipDEF + stat.bonusDEF));
        _currentHp -= finalDmg;

        var damageFont = AddressablePoolManager.Instance.Get(DamageFont);
        damageFont.GetComponent<DamageTMP>().Initialize
            (
                Base_Canvas.Instance.transform,
                transform.position + (transform.right * UnityEngine.Random.Range(-6, 6)) + (Vector3.up * 2.5f), 
                finalDmg,
                true
            //_stat.Damage.ToString()
            );

        Debug.Log($"피해 입음, 현재 체력 {_currentHp}, 피해량 {finalDmg}");


        if (_currentHp <= 0)
        {
            _currentHp = 0;
            Die();
        }

        NotifyHpChanged();
    }

    public void Heal(int amount)
    {
        int maxHp = PlayerDataLoad.Instance.CurrentPlayerStat.GetFinalMaxHP();
        _currentHp = Mathf.Min(_currentHp + amount, maxHp);
        NotifyHpChanged();
    }

    private void Die()
    {
        Debug.Log("플레이어 사망!");
        OnPlayerDead?.Invoke();
        // 여기에 게임 오버 처리 로직 추가
    }
    #endregion

    #region 마력 (MP) 로직
    public bool UseMana(int amount)
    {
        if (_currentMp >= amount)
        {
            _currentMp -= amount;
            NotifyMpChanged();
            return true; // 사용 성공
        }
        return false; // 마나 부족
    }

    public void RestoreMana(int amount)
    {
        int maxMp = PlayerDataLoad.Instance.CurrentPlayerStat.GetFinalMaxMP();
        _currentMp = Mathf.Min(_currentMp + amount, maxMp);
        NotifyMpChanged();
    }
    #endregion

    private void NotifyHpChanged()
    {
        if (PlayerDataLoad.Instance?.CurrentPlayerStat != null)
        {
            int maxHp = PlayerDataLoad.Instance.CurrentPlayerStat.GetFinalMaxHP();
            OnHpChanged?.Invoke(_currentHp, maxHp);
        }
    }

    private void NotifyMpChanged()
    {
        if (PlayerDataLoad.Instance?.CurrentPlayerStat != null)
        {
            int maxMp = PlayerDataLoad.Instance.CurrentPlayerStat.GetFinalMaxMP();
            OnMpChanged?.Invoke(_currentMp, maxMp);
        }
    }
}