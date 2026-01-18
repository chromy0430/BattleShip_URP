using UnityEngine;

[System.Serializable]
public class WeaponStat 
{
    // 기존 무기 스탯
    public int Damage;
    public int Range;
    public int EnemyCount;
    public float FireRate;
    public float projectileSpeed;

    // 유도 타입 무기 스탯
    public bool IsHoming;
    public float HomingTurnSpeed;    
    public float Acceleration;

    // 아이콘
    public Sprite Icon; // 무기 아이콘

    // 투사체
    public GameObject projectilePrefab; // 투사체

    // 이펙트 관련
    public GameObject shotEffect;
    public GameObject muzzleEffect;

    // 무기 사운드
    public AudioClip muzzleSfx; // 발사하는 순간의 사운드
    public AudioClip shotSfx; // 맞는 순간의 사운드

    public WeaponStat(BaseWeaponData data)
    {
        Damage = data.Damage;
        Range = data.Range;
        EnemyCount = data.EnemyCount;
        FireRate = data.FireRate;
        projectileSpeed = data.projectileSpeed;
        IsHoming = data.isHorming;

        Icon = data.Icon;

        projectilePrefab = data.projectilePrefab;
        shotEffect = data.shotEffect;
        muzzleEffect = data.muzzleEffect;

        if (data is HomingWeaponData homingData)
        {
            HomingTurnSpeed = homingData.homingTurnSpeed;            
            Acceleration = homingData.acceleration;
        }
        else
        {
            HomingTurnSpeed = 0f;
            Acceleration = 0f;
        }
    }
}
