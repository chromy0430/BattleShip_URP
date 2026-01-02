using UnityEngine;

public interface IKnockbackable
{
    void ApplyKnockback(Vector3 TargetPos, float distance, float duration = 0.2f);
}