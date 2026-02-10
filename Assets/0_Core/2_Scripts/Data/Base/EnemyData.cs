using UnityEngine;
using UnityEngine.AddressableAssets;
using Battleship_Enum;

[CreateAssetMenu(fileName = "EnemyData", menuName = "BattleShip/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("적 속성")]
    public string EnemyID;
    public string EnemyName;
    [TextArea] public string Description;

    public float MoveSpeed;
    public float TurnSpeed;
    public float SizeRadius;

    [Header("적 능력치")]
    public int Damage;
    public int DEF;
    public int MaxHP;
    public int Level;
    public int HPRecovery;
    public int ExpReward;

    [Header("어드레서블")]
    public AssetReferenceGameObject ModelPrefab;
    public AssetReference DeathAnim;

    [Header("적 타입")]
    public EnemyAttackType AttackType;
    public EnemySizeType SizeType;

    [Header("레이더 아이콘")]
    public Sprite MinimapIcon;
}
