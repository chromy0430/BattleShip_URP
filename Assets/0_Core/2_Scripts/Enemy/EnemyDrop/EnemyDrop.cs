using StylizedWater3;
using UnityEngine;

public interface IWaterFloatTarget
{
    Transform Transform { get; }
    float HeightOffset { get; }
    float SurfaceSize { get; }
    bool IsActive { get; }
}

public class EnemyDrop : MonoBehaviour, IWaterFloatTarget
{
    public float heightOffset = 0.1f;
    public float surfaceSize = 0.5f;

    public Transform Transform => transform;
    public float HeightOffset => heightOffset;
    public float SurfaceSize => surfaceSize;
    public bool IsActive => gameObject.activeInHierarchy;

    private void OnEnable()
    {
        // 오브젝트 풀에서 꺼내질 때(활성화) 자동 등록
        if (PlayerExpManager.Instance != null)
        {
            PlayerExpManager.Instance.RegisterCoin(this);
        }

        if (WaterBatchManager.Instance != null)
        {
            WaterBatchManager.Instance.Register(this);
        }
    }
    private void OnDisable()
    {
        // 오브젝트 풀로 돌아갈 때(비활성화) 자동 해제
        if (PlayerExpManager.Instance != null)
        {
            PlayerExpManager.Instance.UnregisterCoin(this);
        }

        if (WaterBatchManager.Instance != null)
        {
            WaterBatchManager.Instance.Unregister(this);
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
