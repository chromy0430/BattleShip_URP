using StylizedWater3;
using Unity.Collections;
using UnityEngine;

public class PlayerFloating : MonoBehaviour, IWaterFloatTarget
{
    [Header("배 데이터")]
    private BaseShipData shipData;
    

    [Header("부력 관련")]
    [SerializeField] private Vector2 surfaceSize = new Vector2(1.5f, 8f); // 부력 면적
    [SerializeField] private float heightOffset = 0f; // 부력 기준 높이
    [Range(0f, 8f)][SerializeField] private float rollAmount = 1.0f; // 부력 강도
    [SerializeField] private float smoothing = 0.15f; // 부력 강도 보간

    private Rigidbody _rb;
    private BoxCollider _boxCollider;
    private Vector3 _currentNormal = Vector3.up;
    private Vector3 _targetNormal = Vector3.up;
    private float _currentHeight;
    private float _targetHeight;
    private bool _isActive = true;

    public Transform Transform => transform;
    public Vector2 SurfaceSize => surfaceSize;
    public float HeightOffset => heightOffset;
    public bool IsActive => _isActive;
    private HeightQuerySystem.Interface _heightInterface;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _boxCollider = GetComponent<BoxCollider>();
    }

    private void Start()
    {
        
        if (PlayerDataLoad.Instance != null)
        {            
            shipData = PlayerDataLoad.Instance.shipData;
            surfaceSize = shipData.BaseSurfaceSize;
            heightOffset = shipData.BaseHeightOffset;
            rollAmount = shipData.rollAmount;
            smoothing = shipData.smoothing;

            if (_boxCollider != null)
            {
                _boxCollider.center = shipData.ColliderCenter;
                _boxCollider.size = shipData.ColliderSize;
            }
        }

        // 중앙 매니저에 자신을 등록
        if (WaterBatchManager.Instance != null)
        {
            WaterBatchManager.Instance.Register(this);
        }
        else
        {
            Debug.LogError("WaterBatchManager가 씬에 존재하지 않습니다!");
        }

        _currentHeight = transform.position.y;
    }

    private void OnEnable()
    {
        _isActive = true;
    }

    private void OnDisable()
    {
        _isActive = false;
        // 비활성화 시 매니저 목록에서 제거하여 연산 낭비 방지
        if (WaterBatchManager.Instance != null)
        {
            WaterBatchManager.Instance.Unregister(this);
        }
    }

    private void OnDestroy()
    {
        if (WaterBatchManager.Instance != null)
        {
            WaterBatchManager.Instance.Unregister(this);
        }
    }

    public void OnHeightCalculated(NativeArray<float> heights)
    {
        if (heights.Length < 4) return;

        float xNeg = heights[0];
        float xPos = heights[1];
        float zNeg = heights[2];
        float zPos = heights[3];

        float newHeight = (xNeg + xPos + zNeg + zPos) * 0.25f;
        _targetHeight = newHeight + heightOffset;

        _targetNormal = HeightQuerySystem.DeriveNormal(xNeg, xPos, zNeg, zPos, rollAmount);
    }

    private void FixedUpdate()
    {
        if (!_isActive) return;

        // 부드러운 보간 (Smoothing) 적용
        if (smoothing > 0)
        {
            _currentHeight = Mathf.Lerp(_currentHeight, _targetHeight, Time.fixedDeltaTime / smoothing);
            _currentNormal = Vector3.Lerp(_currentNormal, _targetNormal, Time.fixedDeltaTime / smoothing);
        }
        else
        {
            _currentHeight = _targetHeight;
            _currentNormal = _targetNormal;
        }

        Vector3 nextPos = _rb.position;
        nextPos.y = _currentHeight;

        Vector3 flatNormal = Vector3.ProjectOnPlane(_targetNormal, transform.right);
        // 회전값 산출
        Quaternion waveRotation = Quaternion.FromToRotation(Vector3.up, flatNormal);

        Vector3 finalEuler = new Vector3(waveRotation.eulerAngles.x, transform.eulerAngles.y, 0f);
        Quaternion finalRotation = Quaternion.Euler(finalEuler);

        if (rollAmount > 0)
        {
            Quaternion smoothedRotation = Quaternion.Slerp(_rb.rotation, finalRotation, 5f * Time.fixedDeltaTime);
            _rb.Move(nextPos, smoothedRotation);
        }
        else
        {
            _rb.MovePosition(nextPos);
        }

        // 물리적 적용 (MovePosition/Rotation)
        //Vector3 nextPos = _rb.position;
        //nextPos.y = _currentHeight;

        //Quaternion finalRotation = waveRotation * Quaternion.Euler(0, transform.eulerAngles.y, 0);

        /*if (rollAmount > 0)
            _rb.Move(nextPos, finalRotation);
        else
            _rb.MovePosition(nextPos);*/
    }

    public void SetWaterObject(HeightQuerySystem.Interface heightInterface)
    {
        if (_heightInterface == heightInterface)
            return;

        _heightInterface = heightInterface;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero + (Vector3.up * heightOffset), new Vector3(surfaceSize.x, 0f, surfaceSize.y));
    }
}
