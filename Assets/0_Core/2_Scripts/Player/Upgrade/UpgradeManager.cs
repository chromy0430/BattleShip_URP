using System.Collections.Generic;
using UnityEngine;
using System;
using ZLinq;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }
    public event Action onChangedWeaponStat;    
         
    [Header("Settings")]
    [SerializeField] private Transform cardParent;      
    [SerializeField] private GameObject cardPrefab;     
    [SerializeField] private List<UpgradeData> allUpgrades;
    [SerializeField] private List<WeaponMount> _activeWeaponMounts = new List<WeaponMount>();
    [SerializeField] private int canChoice = 3;
    [Header("Safety")]
    [SerializeField] private GameObject inputBlockerPanel;

    // 강화 횟수 제한 <강화 이름, 현재 강화 횟수>
    private Dictionary<string, int> _upgradeCounts = new Dictionary<string, int>();
    
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
        if (cardParent != null) cardParent.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (PlayerExpManager.Instance != null)
        {
            PlayerExpManager.Instance.OnLevelUp += HandleLevelUp;
        }

        RefreshWeaponList();
    }

    public void RefreshWeaponList()
    {
        // 씬 내의 활성화된 모든 WeaponMount를 찾아서 리스트에 등록
        _activeWeaponMounts = FindObjectsByType<WeaponMount>(FindObjectsSortMode.None).AsValueEnumerable().ToList();
        Debug.Log($"[UpgradeManager] 무기 목록 갱신 완료. 총 {_activeWeaponMounts.Count}개");
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

        List<UpgradeData> randomPicks = GetRandomUpgrades(canChoice);

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

        // 선택한 강화 횟수 증가
        if (_upgradeCounts.ContainsKey(data.upgradeName))
            _upgradeCounts[data.upgradeName]++;
        else
            _upgradeCounts[data.upgradeName] = 1;

        // 무기 ID가 있으면 무기에 적용, 없으면 통합에 적용
        if (string.IsNullOrEmpty(data.relatedWeaponID))
        {
            // 통합 능력치 강화
            if (PlayerDataLoad.playerData != null)
            {
                PlayerDataLoad.playerData.CurrentPlayerStat.AddBonus(data.targetStat, data.value);
                Debug.Log($"TargetStat = {data.targetStat}, Value = {data.value}");
                PlayerDataLoad.playerData.UpdateStatAndNotify();
            }
            else
            {
                Debug.LogError("PlayerDataLoad가 없습니다! 스탯 적용 실패.");
            }
        }
        else
        {
            // 무기 강화
            bool applied = false;
            foreach (var mount in _activeWeaponMounts)
            {
                // 이 포탑이 해당 무기 ID를 가지고 있다면 적용
                if (mount.GetWeaponID() == data.relatedWeaponID)
                {
                    mount.ApplyUpgradeBonus(data.targetStat, data.value);
                    applied = true;
                }
            }

            if (applied) Debug.Log($"[Upgrade] 무기 강화 완료: {data.relatedWeaponID}");
            else Debug.LogWarning($"[Upgrade] 해당 무기를 찾지 못함: {data.relatedWeaponID}");
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
        onChangedWeaponStat?.Invoke();
    }

    private List<UpgradeData> GetRandomUpgrades(int count)
    {
        List<UpgradeData> validPool = GetValidUpgrades();
        //List<UpgradeData> pool = new List<UpgradeData>(allUpgrades);
        List<UpgradeData> results = new List<UpgradeData>();

        count = Mathf.Min(count, validPool.Count);

        for (int i = 0; i < count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, validPool.Count);
            results.Add(validPool[randomIndex]);
            validPool.RemoveAt(randomIndex);
        }

        return results;
    }

    private List<UpgradeData> GetValidUpgrades()
    {
        List<UpgradeData> list = new List<UpgradeData>();

        // 내 무기 ID 목록 수집 - HashSet으로 중복 제거
        HashSet<string> myWeaponIDs = new HashSet<string>();
        foreach (var mount in _activeWeaponMounts)
        {
            if (mount != null && mount.gameObject.activeSelf)
            {
                string id = mount.GetWeaponID();
                //myWeaponIDs.Add(mount.GetWeaponID());
                if (!string.IsNullOrEmpty(id))
                {
                    myWeaponIDs.Add(id);
                }
            }
        }

        foreach (var data in allUpgrades)
        {
            // 최대 레벨 체크
            int currentCount = _upgradeCounts.ContainsKey(data.upgradeName) ? _upgradeCounts[data.upgradeName] : 0;
            if (currentCount >= data.maxCount) continue;

            // 조건 체크
            if (string.IsNullOrEmpty(data.relatedWeaponID))
            {
                if (PlayerDataLoad.playerData != null)
                {
                    // 이미 제한값(30 or 12)에 도달했다면 리스트에 넣지 않음
                    if (PlayerDataLoad.playerData.CurrentPlayerStat.IsStatCapped(data.targetStat))
                    {
                        continue;
                    }
                }
            }

            if (string.IsNullOrEmpty(data.relatedWeaponID))
            {
                list.Add(data);
            }
            else
            {
                if (myWeaponIDs.Contains(data.relatedWeaponID))
                {
                    list.Add(data);
                }
            }
        }
        return list;
    }
}