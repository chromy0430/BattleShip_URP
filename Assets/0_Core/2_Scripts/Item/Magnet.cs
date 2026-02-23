using UnityEngine;

public class Magnet : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMagnet.Instance.ActivateMagnetItem();

            AddressablePoolManager.Instance.Return(this.gameObject);
        }
    }
}
