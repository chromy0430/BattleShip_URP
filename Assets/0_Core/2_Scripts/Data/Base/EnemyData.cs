using UnityEngine;
using UnityEngine.AddressableAssets;
using Battleship_Enum;

[CreateAssetMenu(fileName = "EnemyData", menuName = "BattleShip/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Enemy Data")]
    public string EnemyID;
    public string EnemyName;
    [TextArea] public string Description;

    public float MoveSpeed;
    public float TurnSpeed;
    public float SizeRadius;

    [Header("Enemy Stats")]
    public int Damage;
    public int Shield;
    public int MaxHP;
    public int Level;
    public int HPRecovery;
    public int ExpReward;

    [Header("Addressable Assets")]
    public AssetReferenceGameObject ModelPrefab;
    public AssetReference DeathAnim;

    [Header("Types")]
    public EnemyAttackType AttackType;
    public EnemySizeType SizeType;

}
