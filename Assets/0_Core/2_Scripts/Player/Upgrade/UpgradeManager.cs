using BattleShip;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZLinq;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }
    public event Action onChangedWeaponStat;    
         
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

    [Header("아이템 풀")]
    public List<BaseWeaponData> allWeapons; // 레벨업 시 획득 가능한 무기
    public List<BaseEquipmentData> allEquipments; // // 레벨업 시 획득 가능한 장비
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

    private List<object> GetWeightedRandomChoices(int count)
    {
        List<object> results = new List<object>();

        // 1. 현재 상태 분석 (내 무기 목록)
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

        // 2. 풀(Pool) 분류

        // A. 스탯 강화 풀
        List<UpgradeData> poolStat = allUpgrades.AsValueEnumerable()
    .Where(u => string.IsNullOrEmpty(u.relatedWeaponID) && IsUpgradeValid(u))
    .ToList();

        // B. 무기 강화 풀
        List<UpgradeData> poolWeaponUp = allUpgrades.AsValueEnumerable()
            .Where(u => !string.IsNullOrEmpty(u.relatedWeaponID) && myWeaponIDs.Contains(u.relatedWeaponID) && IsUpgradeValid(u))
            .ToList();

        // C. 신규 무기 (안 가진 것)
        List<BaseWeaponData> poolNewWeapon = new List<BaseWeaponData>();
        if (allWeapons != null)
        {
            poolNewWeapon = allWeapons.AsValueEnumerable()
                .Where(w => !myWeaponIDs.Contains(w.ID))
                .ToList();
        }

        // D. 장비 (전체)
        List<BaseEquipmentData> poolEquipment = new List<BaseEquipmentData>();
        if (allEquipments != null)
        {
            // [중요] 나중에 로직에서 RemoveAt을 사용하므로, 원본(ScriptableObject 리스트)을 건드리지 않게
            // 반드시 'ToList()'로 새로운 리스트 복사본을 만들어야 합니다.
            poolEquipment = allEquipments.AsValueEnumerable().ToList();
        }

        // 3. 확률 설정 (단위: %)
        // 기본값 (Normal)
        int pStat = 30;
        int pWpUp = 40;
        int pNewWp = 20;
        int pEquip = 10;

        bool isFull = (currentWeaponCount >= maxWeaponCount);
        bool isInitial = (currentWeaponCount <= 1);

        if (isFull)
        {
            // [요청 반영] 꽉 찼을 때: 스왑 기회 3% (무기 2% + 장비 1%)
            pStat = 37;
            pWpUp = 60;
            pNewWp = 2;
            pEquip = 1;
        }
        else if (isInitial)
        {
            // 초반: 무기 획득 확률 높임
            pStat = 20;
            pWpUp = 30;
            pNewWp = 35;
            pEquip = 15;
        }

        // 4. 주사위 굴리기 (Cumulative Probability)
        for (int i = 0; i < count; i++)
        {
            int dice = UnityEngine.Random.Range(0, 100);
            object selectedItem = null;

            // 순서: 스탯 -> 무기강화 -> 신규무기 -> 장비

            // 1. 스탯 강화 (0 ~ pStat)
            if (dice < pStat)
            {
                if (poolStat.Count > 0)
                {
                    int idx = UnityEngine.Random.Range(0, poolStat.Count);
                    selectedItem = poolStat[idx];
                    poolStat.RemoveAt(idx);
                }
                else dice += 100; // 풀 비었으면 다음으로
            }

            // 2. 무기 강화 ( ~ pStat + pWpUp)
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

            // 3. 신규 무기 ( ~ pStat + pWpUp + pNewWp)
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

            // 4. 장비 (나머지)
            if (selectedItem == null)
            {
                if (poolEquipment.Count > 0)
                {
                    int idx = UnityEngine.Random.Range(0, poolEquipment.Count);
                    selectedItem = poolEquipment[idx];
                    // 장비는 중복 허용(레벨업)하므로 RemoveAt 안 해도 되지만,
                    // 한 화면에 똑같은 장비 2개 뜨는게 싫다면 RemoveAt 사용.
                    // 여기선 다양성을 위해 RemoveAt 사용
                    poolEquipment.RemoveAt(idx);
                }
                else
                {
                    // 정말 줄 게 없으면 최후의 수단: 스탯 강화
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
        if (string.IsNullOrEmpty(data.relatedWeaponID) && PlayerDataLoad.playerData != null)
        {
            if (PlayerDataLoad.playerData.CurrentPlayerStat.IsStatCapped(data.targetStat)) return false;
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
    #endregion
}