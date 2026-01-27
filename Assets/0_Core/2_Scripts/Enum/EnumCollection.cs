namespace Battleship_Enum
{
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
        Damage,
        MoveSpeed,
        TurnSpeed,
        MagnetRange,
        HP,
        MP,
        DEF,
        SPR
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