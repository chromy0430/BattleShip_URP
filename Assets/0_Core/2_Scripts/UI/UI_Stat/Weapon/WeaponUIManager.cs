using System.Collections.Generic;
using UnityEngine;

public class WeaponUIManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private AutoAttackController attackController;
    [SerializeField] private Transform slotParent; // 슬롯들이 들어갈 Grid Layout Group
    
    //[SerializeField] private GameObject weaponSlotPrefab; // WeaponInfoSlot 프리팹

    private List<WeaponInfoSlot> _uiSlots = new List<WeaponInfoSlot>();
    
    private void Start()
    {
        CreateWeaponSlots();

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.onChangedWeaponStat += RefreshAllSlots;
        }
        else
        {
            Debug.LogWarning("[WeaponUIManager] UpgradeManager 인스턴스를 찾을 수 없습니다.");
        }
    }

    private void OnDisable()
    {
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.onChangedWeaponStat -= RefreshAllSlots;
        }
    }

    // 처음 슬롯 생성
    public void CreateWeaponSlots()
    {
        if (attackController == null)
        {
            Debug.LogError("[WeaponUIManager] AutoAttackController가 연결되지 않았습니다!");
            return;
        }

        // 슬롯 데이터 추가
        _uiSlots.Clear();
        WeaponInfoSlot[] foundSlots = slotParent.GetComponentsInChildren<WeaponInfoSlot>();
        _uiSlots.AddRange(foundSlots);

        List<WeaponMount> mounts = attackController.GetWeaponMounts();
        int count = Mathf.Min(_uiSlots.Count, mounts.Count);

        for (int i = 0; i < count; i++)
        {
            // i번째 슬롯 UI에 i번째 무기 마운트를 연결
            _uiSlots[i].Initialize(mounts[i]);
        }
    }

    // 업그레이드 후 내용 갱신
    public void RefreshAllSlots()
    {
        foreach (var slot in _uiSlots)
        {
            if (slot != null) slot.RefreshUI();
        }
    }
}