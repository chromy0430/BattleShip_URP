using UnityEngine;

[CreateAssetMenu(fileName = "CannonData_", menuName = "BattleShip/Weapon Data/Cannon", order = 1)]
public class CannonData : BaseWeaponData
{
    [Header("대포 속성")]
    public float explosionRadius; // 폭발 범위
    public float projectileSpeed; // 투사체 속도 (곡선 비행용)
    public float arcHeight;       // 포물선 높이
}
