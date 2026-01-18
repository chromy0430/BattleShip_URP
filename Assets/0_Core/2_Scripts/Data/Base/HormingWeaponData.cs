using UnityEngine;

[CreateAssetMenu(fileName = "HomingWeaponData_", menuName = "BattleShip/Weapon Data/Missile", order = 1)]
public class HomingWeaponData : BaseWeaponData
{
    [Header("유도 속성")]
    public float homingTurnSpeed; // 유도 회전 속도
    public float acceleration;    // 가속도
}