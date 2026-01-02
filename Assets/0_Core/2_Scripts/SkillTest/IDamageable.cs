using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage, Vector3 hitPoint);
    void TakeEffect(Vector3 hitPoint);

}