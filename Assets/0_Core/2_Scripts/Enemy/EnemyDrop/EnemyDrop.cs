using DG.Tweening;
using StylizedWater3;
using UnityEngine;
public class EnemyDrop : MonoBehaviour, IWaterFloatTarget
{
    [Header("Visual Settings")]
    [SerializeField] private MeshRenderer _renderer;
    private Vector3 _baseScale;
    private Tween _pulseTween;

    [Header("Floating Settings")]
    public float heightOffset = 0.1f;
    public float surfaceSize = 0.5f;
    public int expAmount = 10;

    public Transform Transform => transform;
    public float HeightOffset => heightOffset;
    public float SurfaceSize => surfaceSize;
    public bool IsActive => gameObject.activeInHierarchy;

    private HeightQuerySystem.Interface _heightInterface;

    private void Awake()
    {
        _baseScale = transform.localScale;
        if (_renderer == null) _renderer = GetComponentInChildren<MeshRenderer>();
    }
    public void AddExp(int amount)
    {
        this.expAmount += amount;
        UpdateVisual(true); // 합쳐질 때 팡 터지는 연출
    }

    private void UpdateVisual(bool playAnimation)
    {
        float scaleMultiplier = 1f + Mathf.Log10(expAmount / 10f) * 0.5f;
        Vector3 targetScale = _baseScale * Mathf.Min(scaleMultiplier, 2.5f);

        if (playAnimation)
        {
            transform.DOKill();

            transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 10, 1f);
            transform.DOScale(targetScale, 0.3f).SetEase(Ease.OutBack);

            if (_renderer != null)
            {
                Color targetColor = expAmount > 100 ? Color.yellow : Color.cyan;
                _renderer.material.DOColor(targetColor, "_BaseColor", 0.3f);
            }
        }
        else
        {
            transform.localScale = targetScale;
        }
    }

    public void Initialize(int exp)
    {
        this.expAmount = exp;
        UpdateVisual(false);
    }

    public void SetWaterObject(HeightQuerySystem.Interface heightInterface)
    {
        // 이미 같은 WaterObject면 아무 것도 안 함
        if (_heightInterface == heightInterface)
            return;

        _heightInterface = heightInterface;
    }

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
        transform.DOKill();
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
