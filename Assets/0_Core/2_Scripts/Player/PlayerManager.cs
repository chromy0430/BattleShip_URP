using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    public Transform PlayerTransform;//{ get; private set; }

    private Collider[] _coinBuffer = new Collider[50];
    [Header("Local Magnet Settings")]
    [SerializeField] private float _magnetRadius = 5.0f;
    [SerializeField] private float _moveSpeed = 30.0f;
    [SerializeField] private LayerMask _coinLayer;

    void Update()
    {
        if (PlayerTransform == null) return;

        // 평상시 근거리 자석 로직 (기존 유지)
        PullNearbyCoins();
    }

    private void PullNearbyCoins()
    {
        int count = Physics.OverlapSphereNonAlloc(
            PlayerTransform.position,
            _magnetRadius,
            _coinBuffer,
            _coinLayer
        );

        for (int i = 0; i < count; i++)
        {
            Collider col = _coinBuffer[i];

            // 코인이 이미 전역 자석에 이끌리고 있다면 중복 연산 방지 (선택 사항)
            // 여기서는 단순히 이동만 시킴
            col.transform.position = Vector3.MoveTowards(
                col.transform.position,
                PlayerTransform.position,
                _moveSpeed * Time.deltaTime
            );
        }
    }

    #region 싱글톤

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }
        else
        {
            Destroy(Instance);
        }

    }
    #endregion    
}