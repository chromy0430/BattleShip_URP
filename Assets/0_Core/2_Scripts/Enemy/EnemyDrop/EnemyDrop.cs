using StylizedWater3;
using UnityEngine;
public class EnemyDrop : MonoBehaviour, IWaterFloatTarget
{
    public float heightOffset = 0.1f;
    public float surfaceSize = 0.5f;
    public int expAmount = 10;

    public Transform Transform => transform;
    public float HeightOffset => heightOffset;
    public float SurfaceSize => surfaceSize;
    public bool IsActive => gameObject.activeInHierarchy;

    private HeightQuerySystem.Interface _heightInterface;

    public void Initialize(int exp)
    {
        this.expAmount = exp;
    }

    public void SetWaterObject(HeightQuerySystem.Interface heightInterface)
    {
        // 이미 같은 WaterObject면 아무 것도 안 함
        if (_heightInterface == heightInterface)
            return;

        _heightInterface = heightInterface;
    }

    // 필요 시 WaterBatchManager에서 접근 가능
    public HeightQuerySystem.Interface GetHeightInterface()
    {
        return _heightInterface;
    }

    // 변경 필수 - OnEnable 될때마다 Register 되기때문에 WaterBatchManager에 들어가면 CPU 병목 발생
    private void OnEnable()
    {
        // 오브젝트 풀에서 꺼내질 때(활성화) 자동 등록
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.RegisterCoin(this);
        }

        if (WaterBatchManager.Instance != null)
        {
            WaterBatchManager.Instance.Register(this);
        }
    }
    private void OnDisable()
    {
        // 오브젝트 풀로 돌아갈 때(비활성화) 자동 해제
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.UnregisterCoin(this);
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
            PlayerExpManager.Instance.GetExp(expAmount);
            ObjectPoolManager.Instance.Return(this.gameObject);            
        }
    }
}
