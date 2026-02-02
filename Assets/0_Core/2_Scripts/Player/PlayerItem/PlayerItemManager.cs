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

        // 각 장비 레벨 체크
        private int[] _equipmentLevels = new int[5];

        // 획득한 화물 리스트 (중첩 가능하므로 리스트로 관리)
        public List<BaseCargoData> CollectedCargo = new List<BaseCargoData>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        // 외부에서 아이템을 먹었을 때 호출
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

        // 무기 획득
        private void EquipWeapon(BaseWeaponData weaponData)
        {
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.TryAddWeapon(weaponData);
            }
        }

        // 장비 획득 처리 (빈 슬롯 체크 -> 장착 or 스왑)
        private void HandleEquipment(BaseEquipmentData equipData)
        {
            // 장착된 장비 체크
            for (int i = 0; i < EquipmentSlots.Length; i++)
            {
                if (EquipmentSlots[i] != null && EquipmentSlots[i].ID == equipData.ID)
                {
                    // 레벨업 실행
                    LevelUpEquipmentAtIndex(i);
                    return;
                }
            }

            // 빈 슬롯 체크
            int emptyIndex = -1;
            for (int i = 0; i < EquipmentSlots.Length; i++)
            {
                if (EquipmentSlots[i] == null)
                {
                    emptyIndex = i;
                    break;
                }
            }

            // 장착 또는 변경
            if (emptyIndex != -1)
            {
                EquipItemAtIndex(equipData, emptyIndex);
            }
            else
            {
                Debug.Log("[Equipment] 슬롯이 가득 찼습니다! (스왑 로직 발동)");
                SwapItem(equipData, 0);
            }
        }

        // 화물 획득 처리
        private void HandleCargo(BaseCargoData cargoData)
        {
            CollectedCargo.Add(cargoData);
            Debug.Log($"[Cargo] {cargoData.Name} 화물 적재 완료.");
            ApplyStats(cargoData.effectPerStack, true);
        }

        private void EquipItemAtIndex(BaseEquipmentData data, int index)
        {
            EquipmentSlots[index] = data;
            Debug.Log($"[Equip] {data.Name}을(를) {index}번 슬롯에 장착했습니다.");

            // 스탯 적용 (+값)
            ApplyStats(data.baseStats, true); // true: 더하기
        }

        private void LevelUpEquipmentAtIndex(int index)
        {
            BaseEquipmentData data = EquipmentSlots[index];
            if (data == null) return;

            if (_equipmentLevels[index] >= data.MaxLevel)
            {
                Debug.Log($"[Equip] {data.Name}은(는) 이미 최대 레벨입니다.");
                return;
            }

            _equipmentLevels[index]++;
            Debug.Log($"[Equip] {data.Name} 레벨업! (Lv.{_equipmentLevels[index]})");

            // 성장 스탯 적용 (Growth 값을 한번 더해줌)
            ApplyStats(data.growthPerLevel, true);
        }

        private void UnEquipItemAtIndex(int index)
        {
            if (EquipmentSlots[index] == null) return;

            BaseEquipmentData oldItem = EquipmentSlots[index];
            int currentLevel = _equipmentLevels[index];

            Debug.Log($"[UnEquip] {oldItem.Name} 해제.");

            // 1. 기본 스탯 제거
            ApplyStats(oldItem.baseStats, false);

            // 2. 레벨업으로 오른 스탯 전부 제거 ( (레벨-1) * 성장치 )
            if (currentLevel > 1)
            {
                for (int i = 0; i < currentLevel - 1; i++)
                {
                    ApplyStats(oldItem.growthPerLevel, false);
                }
            }

            EquipmentSlots[index] = null;
            _equipmentLevels[index] = 0;
        }

        private void SwapItem(BaseEquipmentData newItem, int index)
        {
            UnEquipItemAtIndex(index); // 기존 장비 해제 (스탯 초기화)
            EquipItemAtIndex(newItem, index); // 새 장비 장착 (1레벨 스탯)
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

                // 퍼센트(%) 여부에 따른 값 변환
                if (mod.isPercentage)
                {
                    // 10 -> 0.1 (10%)로 변환
                    float percentage = mod.value / 100f;

                    switch (mod.targetStat)
                    {
                        // [계수형 스탯]
                        // 이동속도, 선회속도, 데미지 등
                        // PlayerStat에서 (1.0 + bonus)로 계산하므로 0.1을 더하면 됨
                        case StatType.MoveSpeed:
                        case StatType.TurnSpeed:
                        case StatType.MagnetRange:
                        case StatType.Damage:
                        case StatType.WeaponRange:
                            finalValue = percentage;
                            break;

                        // [플랫형 스탯]
                        // HP, MP, 방어력 등은 고정값이므로
                        // 기본값에 비례하여 증가량 계산 (예: 기본HP 100의 10% = 10)
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

                // 스탯 적용 (장착이면 +, 해제면 -)
                float valueToApply = isAdding ? finalValue : -finalValue;

                playerStat.AddBonus(mod.targetStat, valueToApply);

                Debug.Log($"[Stat Applied] {mod.targetStat}: {valueToApply:F2} (Original: {mod.value}, Is%: {mod.isPercentage})");
            }

            // [수정됨] 스탯 변경 후 이벤트 알림 (UI 갱신 등)
            PlayerDataLoad.playerData.UpdateStatAndNotify();
        }
    }
}