using UnityEngine;

public class EnemyDrop : MonoBehaviour
{
    private void OnEnable()
    {
        // 오브젝트 풀에서 꺼내질 때(활성화) 자동 등록
        if (PlayerExpManager.Instance != null)
        {
            PlayerExpManager.Instance.RegisterCoin(this);
        }
    }
    private void OnDisable()
    {
        // 오브젝트 풀로 돌아갈 때(비활성화) 자동 해제
        if (PlayerExpManager.Instance != null)
        {
            PlayerExpManager.Instance.UnregisterCoin(this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerExpManager.Instance.GetExp(10);
            ObjectPoolManager.Instance.Return(this.gameObject);            
        }
    }
}
