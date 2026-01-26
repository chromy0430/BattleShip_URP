using Battleship_Enum;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private Transform cardParent;      
    [SerializeField] private GameObject cardPrefab;     
    [SerializeField] private List<UpgradeData> allUpgrades; 

    [Header("Safety")]
    [SerializeField] private GameObject inputBlockerPanel;

    // 중복 실행 방지용 플래그
    private bool _isProcessing = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(false);
    }

    private void Start()
    {
        if (PlayerExpManager.Instance != null)
        {
            PlayerExpManager.Instance.OnLevelUp += HandleLevelUp;
        }
    }

    private void OnDisable()
    {
        if (PlayerExpManager.Instance != null)
            PlayerExpManager.Instance.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        ShowRandomUpgrades();
    }

    public void ShowRandomUpgrades()
    {
        _isProcessing = false;
        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(false);

        foreach (Transform child in cardParent)
        {
            Destroy(child.gameObject);
        }

        List<UpgradeData> randomPicks = GetRandomUpgrades(3);

        foreach (var data in randomPicks)
        {
            GameObject go = Instantiate(cardPrefab, cardParent);
            UpgradeCardUI cardUI = go.GetComponent<UpgradeCardUI>();
            cardUI.Setup(data, ApplyUpgrade);
        }

        cardParent.gameObject.SetActive(true);
        Time.timeScale = 0;
    }

    private void ApplyUpgrade(UpgradeData data)
    {
        if (_isProcessing) return;        
        _isProcessing = true;
        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(true);

        Debug.Log($"[Upgrade] 선택 확정: {data.upgradeName}");

        if (PlayerDataLoad.playerData != null)
        {
            PlayerDataLoad.playerData.CurrentShipStat.AddBonus(data.targetStat, data.value);
            PlayerDataLoad.playerData.UpdateStatAndNotify();
        }
        else
        {
            Debug.LogError("PlayerDataLoad가 없습니다! 스탯 적용 실패.");
        }

        CloseUpgradeUI();
    }

    private void CloseUpgradeUI()
    {
        foreach (Transform child in cardParent)
        {
            Destroy(child.gameObject);
        }

        cardParent.gameObject.SetActive(false);
        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(false);

        Time.timeScale = 1;
    }

    private List<UpgradeData> GetRandomUpgrades(int count)
    {
        List<UpgradeData> pool = new List<UpgradeData>(allUpgrades);
        List<UpgradeData> results = new List<UpgradeData>();

        count = Mathf.Min(count, pool.Count);

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            results.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex);
        }

        return results;
    }
}