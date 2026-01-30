namespace Battleship_Enum
{
    public enum ItemType
    {
        Weapon, Equipment, Cargo
    }

    public enum EquipmentSlot
    {
        None,
        Hull,
        Keel,
        MastSail,
        RepairKit,
        Wheel
    }

    public enum EnemyAttackType
    {
        melee, Ranged, SelfDestruct
    }

    public enum EnemySizeType
    {
        Small, Medium, Large, Boss
    }

    public enum ShipSize
    {
        Small, Medium, Large
    }

    public enum StatType
    {
        // 통합 능력치
        Damage,         // 전역 공격력
        MoveSpeed,
        TurnSpeed,
        MagnetRange,
        HP,
        MP,
        DEF,
        SPR,

        // 무기 능력치
        WeaponRange,
        FireRate,         // 연사 속도
        ProjectileSpeed,  // 투사체 속도
        Duration,         // 지속 시간 (레이저, 장판 등)
        PierceCount,      // 관통 횟수
        Knockback,        // 넉백 파워
        ProjectileSize,   // 투사체 크기
        Acceleration,     // 가속도
        MultiProjectile   // 다중 발사 개수 (멀티샷)
    }

    public enum WeaponType
    {
        Gun, Cannon, Missile, Shotgun, Arrow, Laser, Grenade, Pulse 
    }

    public enum WeaponAttribute
    {
        None, Lightning, Fire, Ice
    }
}