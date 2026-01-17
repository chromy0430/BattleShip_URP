using UnityEngine;

public class Magnet : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerManager.Instance.ActivateGlobalMagnet();

            ObjectPoolManager.Instance.Return(this.gameObject);
        }
    }
}
