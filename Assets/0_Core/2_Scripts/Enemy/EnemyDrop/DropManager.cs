using BattleShip;
using Battleship_Enum;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class DropManager : StaticInstance<DropManager>
{
    // 맵에 존재하는 모든 경험치 리스트
    public readonly List<EnemyDrop> activeDrops = new List<EnemyDrop>(1000);

    [Header("경험치 구체 설정 (100% 드랍)")]
    [FormerlySerializedAs("gemPrefab")]
    [SerializeField] private GameObject _expOrbPrefab;
    [SerializeField] private int _maxExpCount = 300;

    [Header("부유물 상자 설정")]
    [SerializeField] private GameObject _lootBoxPrefab; // 상자 프리팹 (FieldItem 붙은거)
    [SerializeField, Range(0f, 1f)] private float _lootDropChance = 0.1f; // 상자 드롭 확률 (기본 10%)

    [Tooltip("상자가 생성될 수면 위 높이 (부력 시스템 안정을 위한 초기값)")]
    [SerializeField] private float _spawnYHeight = 0f;

    [Header("Loot Weights (확률 가중치)")]
    [SerializeField] private int _weaponWeight = 30;
    [SerializeField] private int _equipmentWeight = 40;
    [SerializeField] private int _cargoWeight = 30;

    [Header("Item Data Pools")]
    public List<BaseWeaponData> _weaponList;
    public List<BaseEquipmentData> _equipmentList;
    public List<BaseCargoData> _cargoList;

    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// 적 사망 시 외부(Enemy)에서 호출하는 통합 함수
    /// </summary>
    public void SpawnDrops(Vector3 position, int expAmount)
    {
        // 1. 경험치 생성 (기존 최적화 로직 포함)
        SpawnExperience(position, expAmount);

        // 2. 전리품 상자 생성 (확률)
        if (Random.value <= _lootDropChance)
        {
            SpawnLootBox(position);
        }
    }

    private void SpawnExperience(Vector3 position, int amount)
    {
        if (_expOrbPrefab == null) return;

        // 경험치 구슬 개수 확인
        int currentActive = activeDrops.Count;

        // 화면에 구체가 너무 많으면 병합(Merge) 시도
        if (currentActive > _maxExpCount)
        {
            float mergeDistSqr = 3f * 3f;
            for (int i = 0; i < activeDrops.Count; i++)
            {
                if ((activeDrops[i].transform.position - position).sqrMagnitude < mergeDistSqr)
                {
                    activeDrops[i].AddExp(amount);
                    return;
                }
            }
        }

        var newOrbObj = AddressablePoolManager.Instance.Get(_expOrbPrefab);
        newOrbObj.transform.position = position;

        if (newOrbObj.TryGetComponent<EnemyDrop>(out var dropItem))
        {
            dropItem.Initialize(amount);
            activeDrops.Add(dropItem);
        }
    }

    public void RemoveDrop(EnemyDrop drop)
    {
        int index = activeDrops.IndexOf(drop);
        if (index >= 0)
        {
            int lastIndex = activeDrops.Count - 1;
            activeDrops[index] = activeDrops[lastIndex];
            activeDrops.RemoveAt(lastIndex);
        }
    }

    private void SpawnLootBox(Vector3 pos)
    {
        if (_lootBoxPrefab == null) return;

        // 랜덤 아이템 데이터 뽑기
        ItemType type = GetRandomItemType();
        BaseItemData data = GetRandomDataByType(type);

        if (data != null)
        {
            Vector3 spawnPos = new Vector3(pos.x, _spawnYHeight, pos.z);
            GameObject box = Instantiate(_lootBoxPrefab, spawnPos, Quaternion.identity);
            // 데이터 주입
            FieldItem fieldItem = box.GetComponent<FieldItem>();
            fieldItem?.Initialize(data);            
        }
    }

    #region 헬퍼 함수
    private ItemType GetRandomItemType()
    {
        int total = _weaponWeight + _equipmentWeight + _cargoWeight;
        int rnd = Random.Range(0, total);

        if (rnd < _weaponWeight) return ItemType.Weapon;
        rnd -= _weaponWeight;
        if (rnd < _equipmentWeight) return ItemType.Equipment;
        return ItemType.Cargo;
    }

    private BaseItemData GetRandomDataByType(ItemType type)
    {
        switch (type)
        {
            case ItemType.Weapon:
                return _weaponList.Count > 0 ? _weaponList[Random.Range(0, _weaponList.Count)] : null;
            case ItemType.Equipment:
                return _equipmentList.Count > 0 ? _equipmentList[Random.Range(0, _equipmentList.Count)] : null;
            case ItemType.Cargo:
                return _cargoList.Count > 0 ? _cargoList[Random.Range(0, _cargoList.Count)] : null;
        }
        return null;
    }
    #endregion
}
