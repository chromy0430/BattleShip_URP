using BattleShip;
using Battleship_Enum;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class DropManager : MonoBehaviour
{
    public static DropManager Instance { get; set; }

    [Header("경험치 구체 설정 (100% 드랍)")]
    [FormerlySerializedAs("gemPrefab")]
    [SerializeField] private GameObject _expOrbPrefab;
    [FormerlySerializedAs("gemLayer")]
    [SerializeField] private LayerMask _expLayer;
    [SerializeField] private int _maxExpCount = 300;
    private Collider[] _mergeBuffer = new Collider[5];

    [Header("부유물 상자 설정")]
    [SerializeField] private GameObject _lootBoxPrefab; // 상자 프리팹 (FieldItem 붙은거)
    [Range(0f, 1f)]
    [SerializeField] private float _lootDropChance = 0.1f; // 상자 드롭 확률 (기본 10%)

    [Tooltip("상자가 생성될 수면 위 높이 (부력 시스템 안정을 위한 초기값)")]
    [SerializeField] private float _spawnYHeight = 0f;

    [Header("Loot Weights (확률 가중치)")]
    [SerializeField] private int _weaponWeight = 30;
    [SerializeField] private int _equipmentWeight = 40;
    [SerializeField] private int _cargoWeight = 30;

    [Header("Item Data Pools")]
    // 에디터에서 만든 SO 데이터들을 여기에 리스트로 할당하세요.
    public List<BaseWeaponData> _weaponList;
    public List<BaseEquipmentData> _equipmentList;
    public List<BaseCargoData> _cargoList;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
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

        // 풀링된 오브젝트 개수 확인
        int currentActive = ObjectPoolManager.Instance.GetActiveCount(_expOrbPrefab);

        // 화면에 구체가 너무 많으면 병합(Merge) 시도
        if (currentActive > _maxExpCount)
        {
            // 주변에 있는 경험치 구체 탐색
            int count = Physics.OverlapSphereNonAlloc(position, 3f, _mergeBuffer, _expLayer);
            if (count > 0)
            {
                // 가장 가까운 구체 하나에 경험치 몰아주고 종료 (생성 X)
                if (_mergeBuffer[0].TryGetComponent<EnemyDrop>(out var existingOrb))
                {
                    existingOrb.AddExp(amount);
                    return;
                }
            }
        }

        var newOrbObj = ObjectPoolManager.Instance.Get(_expOrbPrefab);
        newOrbObj.transform.position = position;

        if (newOrbObj.TryGetComponent<EnemyDrop>(out var dropItem))
        {
            dropItem.Initialize(amount);
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
            // 상자는 자주 생성되지 않으므로 Instantiate 사용 (필요 시 풀링으로 전환 가능)
            // 높이 보정(_spawnYHeight) 적용
            Vector3 spawnPos = new Vector3(pos.x, _spawnYHeight, pos.z);

            GameObject box = Instantiate(_lootBoxPrefab, spawnPos, Quaternion.identity);

            // 데이터 주입
            FieldItem fieldItem = box.GetComponent<FieldItem>();
            if (fieldItem != null)
            {
                fieldItem.Initialize(data);
            }
        }
    }

    // --- Helper Functions ---
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
}
