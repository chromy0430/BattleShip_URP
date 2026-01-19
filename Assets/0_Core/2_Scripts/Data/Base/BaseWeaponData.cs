using UnityEditor;
using UnityEngine;

public class BaseWeaponData : ScriptableObject
{
    [Header("무기 속성")]
    public string Name; // 무기 이름
    public string Description; // 무기 설명
        
    public int Damage; // 데미지
    public int Range; // 범위
    public int EnemyCount; // 동시 공격가능한 적 숫자

    public float ProjectileSpeed; // 투사체 속도
    public float FireRate; // 공격 횟수 (공격속도와 비슷한 개념), 프레임당 몇번, 높을수록 빠름

    public bool IsHorming; // 유도 속성

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
        Debug.Log("BaseWeaponData 변경");
    }
#endif
}
