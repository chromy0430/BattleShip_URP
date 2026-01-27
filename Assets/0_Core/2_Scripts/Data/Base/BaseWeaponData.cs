using Battleship_Enum;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData_", menuName = "BattleShip/Weapon Data", order = 1)]
public class BaseWeaponData : ScriptableObject 
{
    [Header("무기 속성")]
    public string WeaponName; // 무기 이름
    [TextArea] public string WeaponDescription; // 무기 설명
    public WeaponType weaponType;
    public WeaponAttribute weaponAttribute;
    public int ShopCost;

    [Header("전투 능력치")]
    public int Damage; // 데미지
    public int Range; // 범위
    public int EnemyCount; // 동시 공격가능한 적 숫자
    public int PierceCount;

    public float ProjectileSpeed; // 투사체 속도
    public float FireRate; // 공격 횟수 (공격속도와 비슷한 개념), 프레임당 몇번, 높을수록 빠름

    [Header("특수 속성")]
    public bool IsHoming; // 유도 속성    

    [Header("유도 속성")]
    public float homingTurnSpeed; // 유도 회전 속도
    public float acceleration;    // 가속도

    [Header("시각 요소")]
    public Sprite Icon; // 무기 아이콘

    [Header("무기 이펙트")]
    public GameObject ProjectilePrefab; // 투사체
    public GameObject MuzzleEffect; // 발사하는 순간의 이펙트
    public GameObject ShotEffect; // 맞는 순간의 이펙트

    [Header("무기 사운드")]
    public AudioClip MuzzleSfx; // 발사하는 순간의 사운드
    public AudioClip ShotSfx; // 맞는 순간의 사운드

    

#if UNITY_EDITOR
    private void OnValidate()
    {
        EditorUtility.SetDirty(this);
    }
#endif
}
