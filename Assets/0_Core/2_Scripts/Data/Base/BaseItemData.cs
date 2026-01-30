using UnityEngine;
using Battleship_Enum;
using UnityEngine.Serialization;

namespace BattleShip
{
    public abstract class BaseItemData : ScriptableObject
    {
        [Header("공통 속성 (Base)")]

        [FormerlySerializedAs("WeaponID")]
        public string ID;               // 고유 ID (기존 WeaponID 대체)
        
        [FormerlySerializedAs("WeaponName")]
        public string Name;             // 아이템 이름 (기존 WeaponName 대체)

        [FormerlySerializedAs("WeaponDescription")]
        [TextArea] public string Description; // 설명

        [FormerlySerializedAs("Icon")]
        public Sprite Icon;             // 아이콘
        
        public ItemType itemType;       // 아이템 타입 (무기/장비/화물)

        [FormerlySerializedAs("ShopCost")]
        public int ShopCost;            // 가격

        public int DefaultLevel = 1;
        public int MaxLevel = 15;
        // 획득 시, 등급이나 레벨 개념이 필요하다면 여기에 추가
        // public int Tier; 
    }
}
