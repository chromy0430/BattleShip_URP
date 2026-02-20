using BattleShip;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZLinq;

public class UpgradeManager : StaticInstance<UpgradeManager>
{
    public event Action onChangedWeaponStat;

    [Header("시작 무기")]
    public List<BaseWeaponData> startingWeaponPool;

    [Header("레벨업 UI")]
    [SerializeField] private Transform cardParent;      
    [SerializeField] private GameObject levelUpCardPrefab;     
    [SerializeField] private List<UpgradeData> allUpgrades;    
    [SerializeField] private int canChoice = 3;

    [Header("부유물 상자 UI")]
    [SerializeField] private GameObject lootBoxPanel;
    [SerializeField] private Transform lootBoxCardParent;
    [SerializeField] private int lootBoxChoiceCount = 2;
    [SerializeField] private GameObject LootBoxCardPrefab;

    [Header("부유물 상자 애니메이션 연출")]
    [Tooltip("건져올리는 연출용 UI 패널 (비워두면 스킵됨)")]
    [SerializeField] private GameObject lootRetrievalPanel;
    [Tooltip("연출 재생 시간 (초)")]
    [SerializeField] private float animationDuration = 2.0f;

    [Header("레벨업 시 획득 가능한 무기, 장비")]
    public List<BaseWeaponData> allWeapons; // 레벨업 시 획득 가능한 무기
    public List<BaseEquipmentData> allEquipments; // // 레벨업 시 획득 가능한 장비

    [Header("부유물 상자를 통해 획득 가능한 화물")]
    public List<BaseCargoData> allCargos;

    [Header("참조")]
    [SerializeField] private List<WeaponMount> _activeWeaponMounts = new List<WeaponMount>();
    [SerializeField] private GameObject inputBlockerPanel;
    
    // 강화 횟수 제한 <강화 이름, 현재 강화 횟수>
    private Dictionary<string, int> _upgradeCounts = new Dictionary<string, int>();
    
    // 중복 실행 방지용 플래그
    private bool _isProcessing = false;

    protected override void Awake()
    {
        base.Awake();

        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(false);
        if (cardParent != null) cardParent.gameObject.SetActive(false);
        if (lootBoxPanel != null) lootBoxPanel.SetActive(false);
        if (lootRetrievalPanel != null) lootRetrievalPanel.SetActive(false);
    }

    private void Start()
    {
        if (PlayerExpManager.Instance != null)
        {
            PlayerExpManager.Instance.OnLevelUp += HandleLevelUp;
        }

        RefreshWeaponList();
    }
    
    private void OnDisable()
    {
        if (PlayerExpManager.Instance != null)
            PlayerExpManager.Instance.OnLevelUp -= HandleLevelUp;
    }

    #region 시작 시 무기 선택

    public void ShowStartingWeaponSelection()
    {
        _isProcessing = false;

        // UI 초기화
        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(false);
        if (lootBoxPanel != null) lootBoxPanel.SetActive(false);
        foreach (Transform child in cardParent) Destroy(child.gameObject);

        // 랜덤 3개 뽑기 (중복 없이)
        List<BaseWeaponData> randomPicks = GetRandomStartingWeapons(canChoice); // 보통 3개

        // 카드 생성
        foreach (var weaponData in randomPicks)
        {
            GameObject go = Instantiate(levelUpCardPrefab, cardParent);
            UpgradeCardUI cardUI = go.GetComponent<UpgradeCardUI>();

            // 선택 시 실행할 콜백 함수 연결 -> ApplyStartingWeapon
            cardUI.Setup(weaponData, ApplyStartingWeapon, "New Start!");
        }

        // UI 띄우기 및 시간 정지
        cardParent.gameObject.SetActive(true);
        Time.timeScale = 0;
    }

    // 시작 무기 선택 시 실행됨
    private void ApplyStartingWeapon(BaseItemData itemData)
    {
        if (_isProcessing) return;
        _isProcessing = true;
        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(true);

        Debug.Log($"[Start] 시작 무기 선택 완료: {itemData.Name}");

        if (PlayerItemManager.Instance != null)
        {
            // 선택한 무기 1개만 즉시 획득
            PlayerItemManager.Instance.ObtainItemImmediate(itemData);
        }

        CloseAllUI(); // UI 닫고 게임 시작(TimeScale = 1)
    }
    #endregion

    #region 레벨업 시스템
    private void HandleLevelUp(int newLevel)
    {
        ShowRandomUpgrades();
    }

    public void ShowRandomUpgrades()
    {
        _isProcessing = false;

        // UI 정리
        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(false);
        if (lootBoxPanel != null) lootBoxPanel .SetActive(false);

        // 기존 카드 제거
        foreach (Transform child in cardParent)
        {
            Destroy(child.gameObject);
        }

        // 카드 데이터 생성
        List<object> randomPicks = GetWeightedRandomChoices(canChoice);

        // 카드 생성
        foreach (var item in randomPicks)
        {
            GameObject go = Instantiate(levelUpCardPrefab, cardParent);
            UpgradeCardUI cardUI = go.GetComponent<UpgradeCardUI>();

            if (item is UpgradeData upgradeData)
            {
                // 스탯/무기 강화
                cardUI.Setup(upgradeData, ApplyUpgrade);
            }
            else if (item is BaseWeaponData weaponData)
            {
                // 신규 무기 획득
                cardUI.Setup(weaponData, ApplyNewItemFromLevelUp, "New Weapon!");
            }
            else if (item is BaseEquipmentData equipData)
            {
                // 신규 장비 획득
                cardUI.Setup(equipData, ApplyNewItemFromLevelUp, "New Equipment!");
            }

            //cardUI.Setup(data, ApplyUpgrade);
        }

        cardParent.gameObject.SetActive(true);
        Time.timeScale = 0; // 게임 정지
    }

    // 카드를 통한 무기, 장비 획득 함수
    private void ApplyNewItemFromLevelUp(BaseItemData itemData)
    {
        if (_isProcessing) return;
        _isProcessing = true;
        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(true);

        Debug.Log($"[LevelUp] 신규 아이템 선택: {itemData.Name}");

        if (PlayerItemManager.Instance != null)
        {
            // 즉시 획득 처리
            PlayerItemManager.Instance.ObtainItemImmediate(itemData);
        }

        CloseAllUI();
    }

    private void ApplyUpgrade(UpgradeData data)
    {
        if (_isProcessing) return;        
        _isProcessing = true;
        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(true);

        Debug.Log($"[Upgrade] 선택 확정: {data.upgradeName}");

        // 선택한 강화 횟수 증가
        if (_upgradeCounts.ContainsKey(data.upgradeName))
        {
            _upgradeCounts[data.upgradeName]++;
        }
        else
        {
            _upgradeCounts[data.upgradeName] = 1;
        }

        // 무기 ID가 있으면 무기에 적용, 없으면 통합에 적용
        if (string.IsNullOrEmpty(data.relatedWeaponID))
        {
            // 통합 능력치 강화
            if (PlayerDataLoad.Instance != null)
            {
                PlayerDataLoad.Instance.CurrentPlayerStat.AddBonus(data.targetStat, data.value);
                PlayerDataLoad.Instance.UpdateStatAndNotify();
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
                    mount.LevelUp();
                    applied = true;
                }
            }

            if (!applied) Debug.LogWarning($"[Upgrade] 해당 무기를 찾지 못함: {data.relatedWeaponID}");
        }

        CloseAllUI();
    }

    private List<object> GetWeightedRandomChoices(int count)
    {
        List<object> results = new List<object>();

        // 내 무기 목록 체크
        int currentWeaponCount = 0;
        int maxWeaponCount = _activeWeaponMounts.Count;
        HashSet<string> myWeaponIDs = new HashSet<string>();

        foreach (var mount in _activeWeaponMounts)
        {
            if (mount.HasWeapon())
            {
                currentWeaponCount++;
                string id = mount.GetWeaponID();
                if (!string.IsNullOrEmpty(id)) myWeaponIDs.Add(id);
            }
        }


        // 스탯 강화
        List<UpgradeData> poolStat = allUpgrades.AsValueEnumerable()
    .Where(u => string.IsNullOrEmpty(u.relatedWeaponID) && IsUpgradeValid(u))
    .ToList();

        // 무기 강화
        List<UpgradeData> poolWeaponUp = allUpgrades.AsValueEnumerable()
            .Where(u => 
            {
                // 무기 ID가 없거나, 내가 안 가진 무기면 탈락
                if (string.IsNullOrEmpty(u.relatedWeaponID) || !myWeaponIDs.Contains(u.relatedWeaponID))
                    return false;

                // 기본 유효성 체크 (횟수 제한 등)
                if (!IsUpgradeValid(u))
                    return false;

                // 해당 무기가 이미 '만렙(MaxLevel)'이면 탈락
                // _activeWeaponMounts 리스트에서 해당 ID를 가진 무기를 찾음
                var targetMount = _activeWeaponMounts.FirstOrDefault(m => m.GetWeaponID() == u.relatedWeaponID);

                // 무기가 있고, 만렙이라면강화 카드는
                if (targetMount != null && targetMount.IsMaxLevel())
                    return false;

                return true;
            })
        .ToList();

        // 신규 무기
        List<BaseWeaponData> poolNewWeapon = new List<BaseWeaponData>();
        if (allWeapons != null)
        {
            poolNewWeapon = allWeapons.AsValueEnumerable()
                .Where(w => !myWeaponIDs.Contains(w.ID))
                .ToList();
        }

        // 장비 (전체)
        List<BaseEquipmentData> poolEquipment = new List<BaseEquipmentData>();
        if (allEquipments != null)
        {
            poolEquipment = allEquipments.AsValueEnumerable().ToList();
        }

        // 확률 설정 (단위: %)
        int pStat = 30;
        int pWpUp = 40;
        int pNewWp = 20;

        bool isFull = (currentWeaponCount >= maxWeaponCount);
        bool isInitial = (currentWeaponCount <= 1);

        if (isFull)
        {
            pStat = 37;
            pWpUp = 60;
            pNewWp = 2;
        }
        else if (isInitial)
        {
            pStat = 20;
            pWpUp = 30;
            pNewWp = 35;
        }

        // 주사위 굴리기
        for (int i = 0; i < count; i++)
        {
            int dice = UnityEngine.Random.Range(0, 100);
            object selectedItem = null;

            // 순서: 스탯 -> 무기강화 -> 신규무기 -> 장비

            // 스탯 강화
            if (dice < pStat)
            {
                if (poolStat.Count > 0)
                {
                    int idx = UnityEngine.Random.Range(0, poolStat.Count);
                    selectedItem = poolStat[idx];
                    poolStat.RemoveAt(idx);
                }
                else dice += 100;
            }

            // 무기 강화
            if (selectedItem == null && dice < (pStat + pWpUp))
            {
                if (poolWeaponUp.Count > 0)
                {
                    int idx = UnityEngine.Random.Range(0, poolWeaponUp.Count);
                    selectedItem = poolWeaponUp[idx];
                    poolWeaponUp.RemoveAt(idx);
                }
                else dice += 100;
            }

            // 신규 무기
            if (selectedItem == null && dice < (pStat + pWpUp + pNewWp))
            {
                if (poolNewWeapon.Count > 0)
                {
                    int idx = UnityEngine.Random.Range(0, poolNewWeapon.Count);
                    selectedItem = poolNewWeapon[idx];
                    poolNewWeapon.RemoveAt(idx);
                }
                else dice += 100;
            }

            // 장비
            if (selectedItem == null)
            {
                if (poolEquipment.Count > 0)
                {
                    int idx = UnityEngine.Random.Range(0, poolEquipment.Count);
                    selectedItem = poolEquipment[idx];
                    poolEquipment.RemoveAt(idx);
                }
                else
                {
                    if (poolStat.Count > 0)
                    {
                        int idx = UnityEngine.Random.Range(0, poolStat.Count);
                        selectedItem = poolStat[idx];
                        poolStat.RemoveAt(idx);
                    }
                }
            }

            if (selectedItem != null) results.Add(selectedItem);
        }

        return results;
    }

    private bool IsUpgradeValid(UpgradeData data)
    {
        // 횟수 제한 체크
        int currentCount = _upgradeCounts.ContainsKey(data.upgradeName) ? _upgradeCounts[data.upgradeName] : 0;
        if (currentCount >= data.maxCount) return false;

        // 스탯 Cap 체크
        if (string.IsNullOrEmpty(data.relatedWeaponID) && PlayerDataLoad.Instance != null)
        {
            if (PlayerDataLoad.Instance.CurrentPlayerStat.IsStatCapped(data.targetStat)) return false;
        }

        return true;
    }
    #endregion

    #region 부유물 시스템

    public void TriggerLootBoxEvent()
    {
        LootBoxSequence().Forget();
    }

    private async UniTaskVoid LootBoxSequence()
    {
        Time.timeScale = 0;
        _isProcessing = false;

        if (lootRetrievalPanel != null)
        {
            lootRetrievalPanel.SetActive(true);

            await UniTask.Delay(TimeSpan.FromSeconds(animationDuration), ignoreTimeScale: true);
            lootRetrievalPanel.SetActive(false);
        }

        ShowLootBoxSelection();
    }
   

    public void ShowLootBoxSelection()
    {
        // UI 정리
        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(false);
        if (cardParent != null) cardParent.gameObject.SetActive(false);

        // 기존 카드 제거
        foreach (Transform child in lootBoxCardParent)
        {
            Destroy(child.gameObject);
        }

        // 카드 데이터 생성
        List<BaseItemData> randomPicks = GetRandomItems(lootBoxChoiceCount);

        // 카드 생성
        foreach (var itemData in randomPicks)
        {
            GameObject go = Instantiate(LootBoxCardPrefab, lootBoxCardParent);
            UpgradeCardUI cardUI = go.GetComponent<UpgradeCardUI>();


            // 이미 보유중인지 체크해서 텍스트 다르게 표시
            string statusText = "획득가능!";           
            cardUI.Setup(itemData, ApplyItemSelection, statusText);
        }

        lootBoxPanel.SetActive(true);
    }

    private void ApplyItemSelection(BaseItemData itemData)
    {
        if (_isProcessing) return;
        _isProcessing = true;
        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(true);

        Debug.Log($"[LootBox] 아이템 선택: {itemData.Name}");

        if (PlayerItemManager.Instance != null)
        {
            PlayerItemManager.Instance.ObtainItemImmediate(itemData);
        }
        else
        {
            Debug.LogError("PlayerItemManager가 없습니다!");
        }

        CloseAllUI();
    }

    #endregion

    #region 헬퍼 함수
    public void RefreshWeaponList()
    {
        // 씬 내의 활성화된 모든 WeaponMount를 찾아서 리스트에 등록
        _activeWeaponMounts = FindObjectsByType<WeaponMount>(FindObjectsSortMode.None).AsValueEnumerable().ToList();
    }

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
        if (lootRetrievalPanel != null) lootRetrievalPanel.SetActive(false);
        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(false);

        Time.timeScale = 1; // 게임 재개
        onChangedWeaponStat?.Invoke();
    }

    private List<BaseItemData> GetRandomItems(int count)
    {
        List<BaseItemData> pool = new List<BaseItemData>();
        //if (allWeapons != null) pool.AddRange(allWeapons);
        //if (allEquipments != null) pool.AddRange(allEquipments);
        if (allCargos != null) pool.AddRange(allCargos);

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

    // 시작 무기 풀에서 랜덤 n개 뽑기
    private List<BaseWeaponData> GetRandomStartingWeapons(int count)
    {
        List<BaseWeaponData> pool = new List<BaseWeaponData>(startingWeaponPool);
        List<BaseWeaponData> results = new List<BaseWeaponData>();

        if (pool.Count == 0) return results;

        count = Mathf.Min(count, pool.Count);
        for (int i = 0; i < count; i++)
        {
            int idx = UnityEngine.Random.Range(0, pool.Count);
            results.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return results;
    }
    #endregion
}