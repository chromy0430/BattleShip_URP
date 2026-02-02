using System;
using Battleship_Enum;

namespace BattleShip
{
    [Serializable]
    public struct StatModifier
    {
        public StatType targetStat; // 적용할 스탯 (예: HP, MoveSpeed)
        public float value;         // 적용 수치 (예: 10)
        public bool isPercentage;   // 퍼센트 여부 (예: true면 value가 10일시 10% 증가)
    }
}