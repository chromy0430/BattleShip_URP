using UnityEngine;
using Battleship_Enum;
using System.Collections.Generic;

namespace BattleShip
{
    [CreateAssetMenu(fileName = "EquipData_", menuName = "BattleShip/Item/Equipment Data", order = 2)]
    public class BaseEquipmentData : BaseItemData
    {
        [Header("장비 전용 속성")]
        public EquipmentSlot equipSlot; // 장착 부위 (Hull, Keel, Mast, RepairKit, Wheel)

        [Header("레벨별 능력치 설정")]
        // 기획서: "레벨별 능력치 표에서 데이터를 뽑아옴"
        // 간단하게 구현하기 위해, 1레벨당 증가량을 설정하거나 레벨별 리스트를 만들 수 있습니다.
        // 여기서는 '기본 능력치(1레벨)'와 '레벨당 성장치'를 정의하는 방식을 추천합니다.

        public List<StatModifier> baseStats = new List<StatModifier>(); // 1레벨 스탯
        public List<StatModifier> growthPerLevel = new List<StatModifier>(); // 레벨업당 증가량

        private void OnEnable()
        {
            itemType = ItemType.Equipment;
            MaxLevel = 15;
        }
    }
}