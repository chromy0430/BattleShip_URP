using UnityEngine;

[CreateAssetMenu(fileName = "HammerSkillData", menuName = "Skill/Hammer Smash Data")]
public class HammerSkillData : ScriptableObject
{
    [Header("Basic Stats")]
    [Tooltip("스킬 데미지")]
    public float damage = 50f;

    [Tooltip("스킬 쿨타임 (초)")]
    public float cooldown = 2.0f;

    [Header("Physics Settings")]
    [Tooltip("충격파 반경 (미터)")]
    public float radius = 5.0f;

    [Tooltip("넉백 거리")]
    public float knockbackDistance = 2.0f;

    [Tooltip("넉백 소요 시간")]
    public float knockbackDuration = 0.2f;

    [Tooltip("피해를 입힐 대상 레이어")]
    public LayerMask targetLayer;

    [Header("Visual & Audio")]
    [Tooltip("타격 시 생성될 이펙트 (Prefab)")]
    public GameObject hitVFX;

    [Tooltip("타격 시 재생될 사운드")]
    public AudioClip hitSound;
}