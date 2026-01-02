using UnityEngine;

[CreateAssetMenu(fileName = "SmallShipData_", menuName = "BattleShip/Ship Data/Small", order = 1)]
public class SmallShipData : BaseShipData
{
    protected override void ApplyTypeSpecificOverrides()
    {
        baseSpeed = 5f;
        baseMinSpeed = 4f;
        baseMaxSpeed = 6f;
        baseTurnSpeed = 10f;
    }
}
