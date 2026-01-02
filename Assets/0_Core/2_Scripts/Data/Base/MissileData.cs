using UnityEngine;

[CreateAssetMenu(fileName = "MissileData_", menuName = "BattleShip/Weapon Data/Missile", order = 1)]
public class MissileData : BaseWeaponData
{
    [Header("미사일 속성")]
    public float homingTurnSpeed; // 유도 회전 속도
    public float projectileSpeed; // 투사체 속도
    public float acceleration;    // 가속도
}