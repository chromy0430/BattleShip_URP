using System.Collections.Generic;
using BattleShip;
using UnityEngine;
using System;
using ZLinq;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }
    public event Action onChangedWeaponStat;    
         
    [Header("Level Up UI Settings")]
    [SerializeField] private Transform cardParent;      
    [SerializeField] private GameObject levelUpCardPrefab;     
    [SerializeField] private List<UpgradeData> allUpgrades;    
    [SerializeField] private int canChoice = 3;

    [Header("LootBox UI")]
    [SerializeField] private GameObject lootBoxPanel;
    [SerializeField] private Transform lootBoxCardParent;
    [SerializeField] private int lootBoxChoiceCount = 2;
    [SerializeField] private GameObject LootBoxCardPrefab;

    [Header("LootBox Data")]
    public List<BaseWeaponData> allWeapons;
    public List<BaseEquipmentData> allEquipments;
    public List<BaseCargoData> allCargos;

    [Header("참조")]
    [SerializeField] private List<WeaponMount> _activeWeaponMounts = new List<WeaponMount>();
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
        if (lootBoxPanel != null) lootBoxPanel.SetActive(false);
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

    #region 레벨업 시스템
    private void HandleLevelUp(int newLevel)
    {
        ShowRandomUpgrades();
    }

    public void ShowRandomUpgrades()
    {
        _isProcessing = false;
        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(false);
        if (lootBoxPanel != null) lootBoxPanel .SetActive(false);

        foreach (Transform child in cardParent)
        {
            Destroy(child.gameObject);
        }

        List<UpgradeData> randomPicks = GetRandomUpgrades(canChoice);

        foreach (var data in randomPicks)
        {
            GameObject go = Instantiate(levelUpCardPrefab, cardParent);
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
                PlayerDataLoad.playerData.UpdateStatAndNotify();
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

            if (!applied) Debug.LogWarning($"[Upgrade] 해당 무기를 찾지 못함: {data.relatedWeaponID}");
        }

        CloseAllUI();
    }
    #endregion

    #region 부유물 시스템

    public void ShowLootBoxSelected()
    {
        _isProcessing = false;

        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(false);
        if (cardParent != null) cardParent.gameObject.SetActive(false);

        foreach (Transform child in lootBoxCardParent)
        {
            Destroy(child.gameObject);
        }

        List<BaseItemData> randomPicks = GetRandomItems(lootBoxChoiceCount);

        foreach (var itemData in randomPicks)
        {
            GameObject go = Instantiate(LootBoxCardPrefab, lootBoxCardParent);
            UpgradeCardUI cardUI = go.GetComponent<UpgradeCardUI>();

            string statusText = "획득가능!";
            // 이미 보유중인지 체크해서 텍스트 다르게 표시

            cardUI.Setup(itemData, ApplyItemSelection, statusText);
        }

        lootBoxPanel.SetActive(true);
        Time.timeScale = 0;
    }

    private void ApplyItemSelection(BaseItemData itemData)
    {
        if (_isProcessing) return;
        _isProcessing = true;
        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(true);

        Debug.Log($"[LootBox] 아이템 선택: {itemData.Name}");

        if (PlayerItemManager.Instance != null)
        {
            PlayerItemManager.Instance.ObtainItem(itemData);
        }
        else
        {
            Debug.LogError("PlayerItemManager가 없습니다!");
        }

        CloseAllUI();
    }

    #endregion

    private void CloseAllUI()
    {
        if (cardParent != null)
        {
            foreach (Transform child in cardParent) Destroy(child.gameObject);
            cardParent.gameObject.SetActive(false);
        }

        if (lootBoxCardParent != null)
        {
            foreach (Transform child in lootBoxCardParent) Destroy(child.gameObject);
        }

        if (lootBoxPanel != null) lootBoxPanel.SetActive(false);
        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(false);

        Time.timeScale = 1; // 게임 재개
        onChangedWeaponStat?.Invoke();
    }

    private List<BaseItemData> GetRandomItems(int count)
    {
        List<BaseItemData> pool = new List<BaseItemData>();
        if (allWeapons != null) pool.AddRange(allWeapons);
        if (allEquipments != null) pool.AddRange(allEquipments);
        if (allCargos != null) pool.AddRange(allCargos);

        // TODO: 여기서 이미 만렙인 장비나 획득 불가한 아이템을 pool에서 제거하는 로직 추가 가능

        List<BaseItemData> results = new List<BaseItemData>();
        if (pool.Count == 0) return results;

        // 랜덤 뽑기
        count = Mathf.Min(count, pool.Count);
        for (int i = 0; i < count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, pool.Count);
            results.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex); // 중복 방지
        }

        return results;
    }

    private List<UpgradeData> GetRandomUpgrades(int count)
    {
        List<UpgradeData> validPool = GetValidUpgrades();
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
        HashSet<string> myWeaponIDs = new HashSet<string>();

        foreach (var mount in _activeWeaponMounts)
        {
            if (mount != null && mount.gameObject.activeSelf)
            {
                string id = mount.GetWeaponID();
                if (!string.IsNullOrEmpty(id))
                {
                    myWeaponIDs.Add(id);
                }
            }
        }

        foreach (var data in allUpgrades)
        {
            int currentCount = _upgradeCounts.ContainsKey(data.upgradeName) ? _upgradeCounts[data.upgradeName] : 0;
            if (currentCount >= data.maxCount) continue;

            if (string.IsNullOrEmpty(data.relatedWeaponID))
            {
                if (PlayerDataLoad.playerData != null)
                {
                    if (PlayerDataLoad.playerData.CurrentPlayerStat.IsStatCapped(data.targetStat)) continue;
                }
                list.Add(data);
            }
            else
            {
                if (myWeaponIDs.Contains(data.relatedWeaponID)) list.Add(data);
            }
        }
        return list;
    }
}