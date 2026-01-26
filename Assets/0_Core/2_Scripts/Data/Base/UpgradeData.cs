using UnityEngine;
using Battleship_Enum;

[CreateAssetMenu(fileName = "LevelUP-Upgrade", menuName = "BattleShip/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [Header("UI Info")]
    public string upgradeName;       // 강화 이름
    [TextArea] public string description; // 강화 설명
    public Sprite icon;              // 표시할 UI 아이콘

    [Header("Stat Logic")]
    public StatType targetStat;      // 바꿀 스탯 종류 
    public float value;              // 증가량 
}