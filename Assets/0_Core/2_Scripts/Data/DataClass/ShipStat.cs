using Battleship_Enum;
using UnityEngine;

public class ShipStat
{
    // 변하는 능력치만 지정
    public ShipSize shipSize;
    public float shipMoveSpeed;
    public float shipTurnSpeed;
    public float shipMagnetRange;

    public int shipHP;
    public int shipMP;
    public int shipDEF;
    public int shipSPR;

    public Vector2 shipSurfaceSize;
    public float shipHeightOffset;

    // 보너스 값
    public float bonusMoveSpeed;
    public float bonusTurnSpeed;
    public float bonusMagnetRange;

    public int bonusHP;
    public int bonusMP;
    public int bonusDEF;
    public int bonusSPR;

    public ShipStat(BaseShipData data)
    {
        shipSize = data.shipSize;

        shipMoveSpeed = data.BaseMoveSpeed;
        shipTurnSpeed = data.BaseTurnSpeed;
        shipMagnetRange = data.BaseMagnetRange;

        shipHP = data.BaseHP;
        shipMP = data.BaseMP;
        shipDEF = data.BaseDEF;
        shipSPR = data.BaseSPR;

        shipSurfaceSize = data.BaseSurfaceSize;
        shipHeightOffset = data.BaseHeightOffset;

        bonusMoveSpeed = 0f;
        bonusTurnSpeed = 0f;
        bonusMagnetRange = 0;

        bonusHP = 0;
        bonusMP = 0;
        bonusDEF = 0;
        bonusSPR = 0;
    }

    public void AddBonus(StatType type, float amount)
    {
        switch (type)
        {
            case StatType.MoveSpeed:
                bonusMoveSpeed += amount;
                break;
            case StatType.TurnSpeed:
                bonusTurnSpeed += amount;
                break;
            case StatType.MagnetRange:
                bonusMagnetRange += amount;
                break;

            case StatType.HP:
                bonusHP += (int)amount;
                break;
            case StatType.MP:
                bonusMP += (int)amount;
                break;
            case StatType.DEF:
                bonusDEF += (int)amount;
                break;
            case StatType.SPR:
                bonusSPR += (int)amount;
                break;

            default:
                Debug.LogWarning($"[ShipStat] 처리되지 않은 스탯 타입: {type}");
                break;
        }
    }
}
