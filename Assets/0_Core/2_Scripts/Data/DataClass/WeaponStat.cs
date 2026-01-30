using Battleship_Enum;
using UnityEngine;

[System.Serializable]
public class WeaponStat 
{
    // 기존 무기 스탯
    public WeaponType weaponType;
    public WeaponAttribute weaponAttribute;
    public string Name;
    public string Description;

    public int WeaponLevel;
    public int Damage;
    public int Range;
    public int EnemyCount;
    public int PierceCount;

    public float projectileSpeed;
    public float Acceleration;
    public float Cooltime;
    public float Duration;   
    
    [SerializeField] private float _fireRate; // 최대 속도 5 미만
    public float FireRate 
    { 
        get => _fireRate; 
        set 
        { 
            float clampedValue = Mathf.Clamp(value, 0.1f, 5.0f);

            _fireRate = Mathf.Round(clampedValue * 100f) / 100f;
        } 
    }
    
    // 유도 타입 무기 스탯
    public bool IsHoming; // 유도성 체크
    public float HomingTurnSpeed; // 유도 시 회전 속도
    

    public Sprite Icon; // 무기 아이콘
    public GameObject projectilePrefab; // 투사체 모델
    public GameObject shotEffect; // 피격 이펙트
    public GameObject muzzleEffect; // 발사 이펙트 (아마 안쓸듯)
    public AudioClip muzzleSfx; // 발사하는 순간의 사운드
    public AudioClip shotSfx; // 맞는 순간의 사운드

    public WeaponStat(BaseWeaponData data)
    {
        WeaponLevel = 1;

        weaponType = data.weaponType;
        weaponAttribute = data.weaponAttribute;
        
        Name = data.Name;
        Description = data.Description;
        Damage = data.Damage;
        Range = data.Range;
        EnemyCount = data.EnemyCount;
        PierceCount = data.PierceCount;
        FireRate = data.FireRate;
        projectileSpeed = data.ProjectileSpeed;
        Acceleration = data.Acceleration;
        Cooltime = data.Cooltime;
        Duration = data.Duration;

        IsHoming = data.IsHoming;
        HomingTurnSpeed = data.homingTurnSpeed;

        Icon = data.Icon;
        projectilePrefab = data.ProjectilePrefab;
        shotEffect = data.ShotEffect;
        muzzleEffect = data.MuzzleEffect;
        muzzleSfx = data.MuzzleSfx;
        shotSfx = data.ShotSfx;
    }
}
