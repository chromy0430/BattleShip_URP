using UnityEngine;

[System.Serializable]
public class WeaponStat 
{
    // 기존 무기 스탯
    public string Name;
    public string Description;

    public int WeaponLevel;
    public int Damage;
    public int Range;
    public int EnemyCount;
    [SerializeField] private float _fireRate;
    public float FireRate 
    { 
        get => _fireRate; 
        set 
        { 
            float clampedValue = Mathf.Clamp(value, 0.1f, 5.0f);

            _fireRate = Mathf.Round(clampedValue * 100f) / 100f;
        } 
    }
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
        WeaponLevel = 1;
        Name = data.WeaponName;
        Description = data.WeaponDescription;
        Damage = data.Damage;
        Range = data.Range;
        EnemyCount = data.EnemyCount;
        FireRate = data.FireRate;
        projectileSpeed = data.ProjectileSpeed;
        IsHoming = data.IsHorming;

        Icon = data.Icon;

        projectilePrefab = data.ProjectilePrefab;
        shotEffect = data.ShotEffect;
        muzzleEffect = data.MuzzleEffect;

        if (data is HomingWeaponData homingData)
        {
            HomingTurnSpeed = homingData.homingTurnSpeed;            
            Acceleration = homingData.acceleration;
        }
        else
        {
            HomingTurnSpeed = 20f;
            Acceleration = 30f;
        }
    }
}
