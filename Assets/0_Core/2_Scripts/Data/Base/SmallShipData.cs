using UnityEngine;

[CreateAssetMenu(fileName = "SmallShipData_", menuName = "BattleShip/Ship Data/Small", order = 1)]
public class SmallShipData : BaseShipData
{
    protected override void ApplyTypeSpecificOverrides()
    {
        baseMoveSpeed = 5f;
        baseTurnSpeed = 10f;
    }
}
