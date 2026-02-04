using UnityEngine;
using Battleship_Enum;

[CreateAssetMenu(fileName = "LevelUP-Upgrade", menuName = "BattleShip/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [Header("강화 속성")]
    public string upgradeName;       // 강화 이름
    [TextArea] public string description; // 강화 설명
    public Sprite icon;              // 표시할 UI 아이콘

    [Header("타겟 설정")]
    [Tooltip("비어있으면 공통 업그레이드로 항상 나옴, 무기 ID적으면 해당 무기 전용 업그레이드")]
    public string relatedWeaponID; // 예시 : "Wpn_Gun_01"

    [Header("강화할 능력치")]
    public StatType targetStat;      // 바꿀 스탯 종류 
    public float value;              // 증가량 

    [Header("최대 선택 횟수")]
    public int maxCount = 5;

    public string GetDescription()
    {
        string displayValue = "";

        // 스탯 타입에 따라 표시 형식 다르게 하기
        switch (targetStat)
        {
            // 퍼센트 단위인 친구들 (예: 공격력, 이속)
            case StatType.Damage:
            case StatType.MoveSpeed:
            case StatType.TurnSpeed:                     
            case StatType.MagnetRange:
            case StatType.FireRate:
            case StatType.Acceleration:
                // 0.1 -> 10, 0.5 -> 50 으로 변환
                displayValue = (value * 100).ToString("0");
                break;

            // 정수 단위인 친구들 (예: 체력, 방어력)
            default:
                displayValue = value.ToString();
                break;
        }

        // 텍스트 교체
        return description.Replace("{value}", displayValue);
    }
}