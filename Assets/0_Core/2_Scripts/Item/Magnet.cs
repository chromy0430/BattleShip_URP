using UnityEngine;

public class Magnet : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMagnet.Instance.ActivateMagnetItem();

            ObjectPoolManager.Instance.Return(this.gameObject);
        }
    }
}
