using System;
using UnityEngine;

public delegate void OnExpChanged(float currentExp, float maxExp); 

public class PlayerExpManager : MonoBehaviour
{
    public static PlayerExpManager Instance { get; private set; }
    public event OnExpChanged onExpChanged;
    public event Action<int> OnLevelUp;

    [Header("경험치 관련")]
    [SerializeField] private int baseExp = 10; // 첫 레벨업 경험치 (1레벨 필요 경험치)
    [SerializeField] private float growthMultiplier = 1.2f; // 필요 경험치 지수 상승 
    
    [SerializeField] private int currentExp = 0; // 현재 경험치
    public int currentLevel = 0; // 현재 레벨

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public int GetRequiredExp(int level)
    {
        return Mathf.FloorToInt(baseExp * Mathf.Pow(growthMultiplier, level - 1));
    }

    public void GetExp(int amount)
    {
        currentExp += amount;
        int requiredExp = GetRequiredExp(currentLevel + 1);

        // 레벨업 판단 루프
        while (currentExp >= requiredExp)
        {
            currentExp = 0;
            currentLevel++;
            requiredExp = GetRequiredExp(currentLevel + 1); // 다음 레벨 기준 갱신            
            OnLevelUp?.Invoke(currentLevel); // 레벨업 알림
        }

        // UI 갱신용 데이터 전달
        onExpChanged?.Invoke(currentExp, requiredExp);
    }
}
