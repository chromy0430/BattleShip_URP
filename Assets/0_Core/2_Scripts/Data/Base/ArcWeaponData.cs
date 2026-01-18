using UnityEngine;

[CreateAssetMenu(fileName = "ArcWeaponData_", menuName = "BattleShip/Weapon Data/Cannon", order = 1)]
public class ArcWeaponData : BaseWeaponData
{
    [Header("곡선 공격 속성")]
    public float explosionRadius; // 폭발 범위
    public float arcHeight;       // 포물선 높이
}
