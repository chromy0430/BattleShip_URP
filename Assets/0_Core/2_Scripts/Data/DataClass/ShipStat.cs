using Battleship_Enum;
using UnityEngine;

public class ShipStat : MonoBehaviour
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
    }
}
