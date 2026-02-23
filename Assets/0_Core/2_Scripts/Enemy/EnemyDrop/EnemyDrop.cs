using DG.Tweening;
using StylizedWater3;
using UnityEngine;
public class EnemyDrop : MonoBehaviour, IWaterFloatTarget
{
    [Header("시각적 요소")]
    [SerializeField] private MeshRenderer _renderer;
    private Vector3 _baseScale;

    [Header("부력 관련")]
    public float heightOffset = 0.1f;
    public Vector2 surfaceSize = new Vector2(.5f, .5f);
    public int expAmount = 10;

    public Transform Transform => transform;
    public float HeightOffset => heightOffset;
    public Vector2 SurfaceSize => surfaceSize;
    public bool IsActive => gameObject.activeInHierarchy;
    public bool UseSimpleWaterPhysics => true;
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
        UpdateVisual(true);
    }

    public void SetWaterObject(HeightQuerySystem.Interface heightInterface)
    {
        if (_heightInterface == heightInterface)  return;
        _heightInterface = heightInterface;
    }

    public HeightQuerySystem.Interface GetHeightInterface()
    {
        return _heightInterface;
    }

    private void OnEnable()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerMagnet.Instance.RegisterCoin(this);
        }

        if (WaterBatchManager.Instance != null)
        {
            WaterBatchManager.Instance.Register(this);
        }
    }
    private void OnDisable()
    {
        transform.DOKill();

        if (PlayerManager.Instance != null)
        {
            PlayerMagnet.Instance.UnregisterCoin(this);
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
            AddressablePoolManager.Instance.Return(this.gameObject);            
        }
    }
}
