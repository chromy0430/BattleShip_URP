using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IEnemy
{
    //GameObject EnemyPrefab { get;  set; }
    Transform TargetTransform { get; } // 적 현재 위치
    bool IsAlive { get; } // 적이 살아있는지 여부

    UniTaskVoid ApplyKnockback(Vector3 force, float duration = 0.2f);
    void TakeDamage(int damage, out int dmg); // 데미지 받는 함수
}
