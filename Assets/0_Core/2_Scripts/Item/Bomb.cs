using UnityEngine;

public class Bomb : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.KillAllEnemies();
            }

            ObjectPoolManager.Instance.Return(this.gameObject);
        }
    }
}
