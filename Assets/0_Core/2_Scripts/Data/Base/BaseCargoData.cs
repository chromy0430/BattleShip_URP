using UnityEngine;
using Battleship_Enum;
using System.Collections.Generic;

namespace BattleShip
{
    [CreateAssetMenu(fileName = "CargoData_", menuName = "BattleShip/Item/Cargo Data", order = 3)]
    public class BaseCargoData : BaseItemData
    {
        [Header("화물 전용 속성")]
        // 화물은 레벨이 없고, '보유 개수'에 따라 효과가 중첩됩니다.
        public bool isInstantEffect; // 획득 즉시 소모 여부 (회복약 등)

        [Header("중첩 효과 (Per Stack)")]
        // 화물 1개당 적용될 능력치
        public List<StatModifier> effectPerStack = new List<StatModifier>();

        private void OnEnable()
        {
            itemType = ItemType.Cargo;
            MaxLevel = 0; // 화물은 레벨 개념 없음
            DefaultLevel = 0;
        }
    }
}