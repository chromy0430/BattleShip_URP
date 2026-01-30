using UnityEngine;
using System.Collections.Generic;
using Battleship_Enum;

namespace BattleShip
{
    public class PlayerItemManager : MonoBehaviour
    {
        public static PlayerItemManager Instance { get; private set; }

        [Header("Status")]
        // 장비 슬롯 5개 (null이면 비어있음)
        public BaseEquipmentData[] EquipmentSlots = new BaseEquipmentData[5];

        // 획득한 화물 리스트 (중첩 가능하므로 리스트로 관리)
        public List<BaseCargoData> CollectedCargo = new List<BaseCargoData>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        // --- [핵심] 외부(FieldItem)에서 아이템을 먹었을 때 호출하는 함수 ---
        public void ObtainItem(BaseItemData itemData)
        {
            if (itemData == null) return;

            switch (itemData.itemType)
            {
                case ItemType.Weapon:
                    EquipWeapon(itemData as BaseWeaponData);
                    break;

                case ItemType.Equipment:
                    HandleEquipment(itemData as BaseEquipmentData);
                    break;

                case ItemType.Cargo:
                    HandleCargo(itemData as BaseCargoData);
                    break;
            }
        }

        // 1. 무기 획득 처리
        private void EquipWeapon(BaseWeaponData weaponData)
        {
            Debug.Log($"[Weapon] {weaponData.Name} 획득! (무기 장착 로직 연결 필요)");
            // TODO: WeaponMount.cs나 WeaponManager에 무기 추가 요청
            // 예: PlayerManager.Instance.WeaponSystem.AddWeapon(weaponData);
        }

        // 2. 장비 획득 처리 (빈 슬롯 체크 -> 장착 or 스왑)
        private void HandleEquipment(BaseEquipmentData equipData)
        {
            // A. 빈 슬롯 찾기
            int emptyIndex = -1;
            for (int i = 0; i < EquipmentSlots.Length; i++)
            {
                if (EquipmentSlots[i] == null)
                {
                    emptyIndex = i;
                    break;
                }
            }

            // B. 빈 슬롯이 있다면 -> 장착
            if (emptyIndex != -1)
            {
                EquipItemAtIndex(equipData, emptyIndex);
            }
            // C. 꽉 찼다면 -> 스왑 (일단 가장 오래된 0번과 교체하거나, UI를 띄워야 함)
            else
            {
                Debug.Log("[Equipment] 슬롯이 가득 찼습니다! (스왑 로직 발동)");
                // 기획서: "현재 장착된 장비와 위치를 바꿈"
                // 임시로 0번 슬롯과 교체합니다. (나중에 UI 선택으로 고도화 가능)
                SwapItem(equipData, 0);
            }
        }

        // 3. 화물 획득 처리
        private void HandleCargo(BaseCargoData cargoData)
        {
            CollectedCargo.Add(cargoData);
            Debug.Log($"[Cargo] {cargoData.Name} 화물 적재 완료.");

            // 화물은 획득 즉시 스탯 적용
            ApplyStats(cargoData.statModifiers, true);
        }

        // --- 내부 로직 (장착/해제/스왑) ---

        private void EquipItemAtIndex(BaseEquipmentData data, int index)
        {
            EquipmentSlots[index] = data;
            Debug.Log($"[Equip] {data.Name}을(를) {index}번 슬롯에 장착했습니다.");

            // 스탯 적용 (+값)
            ApplyStats(data.baseStats, true); // true: 더하기
        }

        private void UnEquipItemAtIndex(int index)
        {
            if (EquipmentSlots[index] == null) return;

            BaseEquipmentData oldItem = EquipmentSlots[index];
            Debug.Log($"[UnEquip] {oldItem.Name} 장착 해제.");

            // 스탯 제거 (-값, 즉 false로 처리하여 빼기)
            ApplyStats(oldItem.baseStats, false);

            EquipmentSlots[index] = null;
        }

        private void SwapItem(BaseEquipmentData newItem, int index)
        {
            UnEquipItemAtIndex(index); // 기존 것 빼고 (스탯 차감)
            EquipItemAtIndex(newItem, index); // 새 것 끼기 (스탯 증가)
        }

        // --- 스탯 계산기 (Stat Calculator) ---
        // isAdding: true면 능력치 추가, false면 능력치 제거(장비 해제 시)
        private void ApplyStats(List<StatModifier> modifiers, bool isAdding)
        {
            if (PlayerManager.Instance == null) return;

            // 플레이어 스탯 매니저 가져오기 (가정: PlayerManager에 Stat 관련 접근자가 있다고 가정)
            // 만약 PlayerStat 클래스에 직접 접근해야 한다면 아래 로직을 수정해야 합니다.

            // 예시 로직: PlayerStat이 있고, 거기에 AddModifier 함수가 있다고 가정
            /* var playerStat = PlayerManager.Instance.Stat; 
            foreach (var mod in modifiers)
            {
                float value = isAdding ? mod.value : -mod.value; // 더하거나 뺌
                playerStat.ApplyModifier(mod.targetStat, value, mod.isPercentage);
            }
            */

            // [임시 로그] 실제 PlayerStat 연결 전 확인용
            foreach (var mod in modifiers)
            {
                string op = isAdding ? "증가" : "감소";
                Debug.Log($"[Stat] {mod.targetStat} {mod.value} {op} 적용됨.");
            }
        }
    }
}