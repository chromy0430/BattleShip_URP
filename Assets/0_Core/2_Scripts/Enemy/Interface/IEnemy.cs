using UnityEngine;

public interface IEnemy
{
    Transform TargetTransform { get; } // 적 현재 위치
    bool IsAlive { get; } // 적이 살아있는지 여부
    void TakeDamage(int damage); // 데미지 받는 함수
}
