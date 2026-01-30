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

        // 외부(FieldItem)에서 아이템을 먹었을 때 호출하는 함수 
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

        // 무기 획득 처리
        private void EquipWeapon(BaseWeaponData weaponData)
        {
            // Debug.Log($"[Weapon] {weaponData.Name} 획득! (무기 장착 로직 연결 필요)");

            // [수정] PlayerManager에게 무기 장착 요청
            if (PlayerManager.Instance != null)
            {
                bool success = PlayerManager.Instance.TryAddWeapon(weaponData);

                if (!success)
                {
                    // 꽉 차서 장착 못했을 때의 처리 (예: 경험치로 변환? 혹은 인벤토리?)
                    Debug.Log("무기 획득 실패: 슬롯 가득 참");
                }
            }
        }

        // 장비 획득 처리 (빈 슬롯 체크 -> 장착 or 스왑)
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

        // 화물 획득 처리
        private void HandleCargo(BaseCargoData cargoData)
        {
            CollectedCargo.Add(cargoData);
            Debug.Log($"[Cargo] {cargoData.Name} 화물 적재 완료.");

            // [수정됨] CS1061 에러 해결: statModifiers -> effectPerStack
            ApplyStats(cargoData.effectPerStack, true);
        }

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
            if (PlayerDataLoad.playerData == null)
            {
                Debug.LogWarning("PlayerDataLoad가 초기화되지 않아 스탯을 적용할 수 없습니다.");
                return;
            }

            var playerStat = PlayerDataLoad.playerData.CurrentPlayerStat;
            if (playerStat == null) return;

            foreach (var mod in modifiers)
            {
                float finalValue = 0f;

                // 1. 퍼센트(%) 여부에 따른 값 변환
                if (mod.isPercentage)
                {
                    // 10 -> 0.1 (10%)로 변환
                    float percentage = mod.value / 100f;

                    switch (mod.targetStat)
                    {
                        // [계수형 스탯] 이동속도, 선회속도, 데미지 등은 PlayerStat에서 (1.0 + bonus)로 계산하므로 0.1을 더하면 됨
                        case StatType.MoveSpeed:
                        case StatType.TurnSpeed:
                        case StatType.MagnetRange:
                        case StatType.Damage:
                            finalValue = percentage;
                            break;

                        // [플랫형 스탯] HP, MP, 방어력 등은 고정값이므로 기본값에 비례하여 증가량 계산 (예: 기본HP 100의 10% = 10)
                        case StatType.HP:
                            finalValue = playerStat.shipHP * percentage;
                            break;
                        case StatType.MP:
                            finalValue = playerStat.shipMP * percentage;
                            break;
                        case StatType.DEF:
                            finalValue = playerStat.shipDEF * percentage;
                            break;
                        case StatType.SPR:
                            finalValue = playerStat.shipSPR * percentage;
                            break;

                        default:
                            finalValue = mod.value; // 예외 처리
                            break;
                    }
                }
                else
                {
                    // 고정값 (Flat)
                    switch (mod.targetStat)
                    {
                        // 속도 계열은 고정값을 더하려면 기본 속도로 나누어 비율로 변환해줘야 함 (PlayerStat 구조상)
                        case StatType.MoveSpeed:
                            finalValue = (playerStat.shipMoveSpeed > 0) ? (mod.value / playerStat.shipMoveSpeed) : 0;
                            break;
                        case StatType.TurnSpeed:
                            finalValue = (playerStat.shipTurnSpeed > 0) ? (mod.value / playerStat.shipTurnSpeed) : 0;
                            break;

                        default:
                            finalValue = mod.value;
                            break;
                    }
                }

                // 2. 스탯 적용 (장착이면 +, 해제면 -)
                float valueToApply = isAdding ? finalValue : -finalValue;

                playerStat.AddBonus(mod.targetStat, valueToApply);

                Debug.Log($"[Stat Applied] {mod.targetStat}: {valueToApply:F2} (Original: {mod.value}, Is%: {mod.isPercentage})");
            }

            // [수정됨] 스탯 변경 후 이벤트 알림 (UI 갱신 등)
            PlayerDataLoad.playerData.UpdateStatAndNotify();
        }
    }
}