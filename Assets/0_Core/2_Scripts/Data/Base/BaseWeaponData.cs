using Battleship_Enum;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData_", menuName = "BattleShip/Weapon Data", order = 1)]
public class BaseWeaponData : ScriptableObject 
{
    [Header("무기 속성")]
    public string WeaponID;
    public string WeaponName; // 무기 이름
    [TextArea] public string WeaponDescription; // 무기 설명
    public WeaponType weaponType;
    public WeaponAttribute weaponAttribute;
    public int ShopCost;

    [Header("전투 능력치")]
    public int Damage;                // 기본 공격력
    public int Range;                 // 사거리
    public int EnemyCount;            // 동시 타격 가능 수
    public int PierceCount;           // 관통 횟수
    public float FireRate;            // 초당 발사 횟수
    public float Cooltime;            // 재사용 대기시간
    public float Duration;            // 지속 시간 (펄스 등)
    public float ProjectileSpeed;     // 투사체 속도
    public float Acceleration;        // 투사체 가속력

    [Header("유도 속성")]
    public bool IsHoming; // 유도 속성    
    public float homingTurnSpeed; // 유도 회전 속도

    [Header("비주얼/사운드")]
    public Sprite Icon; // 무기 아이콘
    public GameObject ProjectilePrefab; // 투사체
    public GameObject MuzzleEffect; // 발사하는 순간의 이펙트
    public GameObject ShotEffect; // 맞는 순간의 이펙트
    public AudioClip MuzzleSfx; // 발사하는 순간의 사운드
    public AudioClip ShotSfx; // 맞는 순간의 사운드

#if UNITY_EDITOR
    private void OnValidate()
    {
        EditorUtility.SetDirty(this);
    }
#endif
}
