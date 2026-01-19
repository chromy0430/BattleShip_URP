using System.Collections.Generic;
using UnityEngine;

public class WeaponUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AutoAttackController attackController;
    [SerializeField] private Transform slotParent; // 슬롯들이 들어갈 Grid Layout Group
    [SerializeField] private GameObject weaponSlotPrefab; // WeaponInfoSlot 프리팹

    private List<WeaponInfoSlot> _spawnedSlots = new List<WeaponInfoSlot>();
    


    private void Start()
    {
        CreateWeaponSlots();

        if (InitManager.Instance != null)
            InitManager.Instance.upgradeManager.onChangedWeaponStat += RefreshAllSlots; 
        else
        {
            Debug.Log("Init Manager 인스턴스 없는 상태");
        }
    }

    private void OnDisable()
    {
        if (InitManager.Instance != null)
            InitManager.Instance.upgradeManager.onChangedWeaponStat += RefreshAllSlots;
    }

    // 1. 처음 슬롯 생성
    public void CreateWeaponSlots()
    {
        // 기존 슬롯 청소
        foreach (var slot in _spawnedSlots) Destroy(slot.gameObject);
        _spawnedSlots.Clear();

        // 실제 무기 리스트 가져오기
        List<WeaponMount> mounts = attackController.GetWeaponMounts();

        foreach (var mount in mounts)
        {
            GameObject go = Instantiate(weaponSlotPrefab, slotParent);
            WeaponInfoSlot slotScript = go.GetComponent<WeaponInfoSlot>();

            // 슬롯에 실제 무기(WeaponMount)를 연결
            slotScript.Initialize(mount);

            _spawnedSlots.Add(slotScript);
        }
    }

    // 2. 업그레이드 후 수치만 갱신 (슬롯 갯수는 그대로)
    public void RefreshAllSlots()
    {
        foreach (var slot in _spawnedSlots)
        {
            slot.RefreshUI();
        }
    }
}