using UnityEngine;

public class PlayerMagnet : MonoBehaviour
{
    //private PlayerStat _stat => PlayerDataLoad.playerData.CurrentPlayerStat;

    private Collider[] _coinBuffer = new Collider[50];
    [Header("Local Magnet Settings")]
    [SerializeField] private float _magnetRadius;
    [SerializeField] private float _moveSpeed = 30.0f;
    [SerializeField] private LayerMask _coinLayer;

    public bool Doingmagnet = false;

    private Transform _PlayerTransform;
    private void Start()
    {
        if (PlayerManager.Instance != null)
        {
            _PlayerTransform = PlayerManager.Instance.PlayerTransform;
        }

        if (PlayerDataLoad.playerData != null)
        {
            _magnetRadius = PlayerDataLoad.playerData.CurrentPlayerStat.shipMagnetRange;
            PlayerDataLoad.playerData.OnStatUpdated += UpdateMagnetStat;
        }
    }

    private void OnDisable()
    {
        if (PlayerDataLoad.playerData != null) PlayerDataLoad.playerData.OnStatUpdated -= UpdateMagnetStat;
    }

    void Update()
    {
        if (_PlayerTransform == null) return;

        // 평상시 근거리 자석 로직 (기존 유지)
        PullNearbyItems();
    }

    #region 아이템 줍기

    private void PullNearbyItems()
    {
        if (Doingmagnet) return;

        int count = Physics.OverlapSphereNonAlloc(
            _PlayerTransform.position,
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
                _PlayerTransform.position,
                _moveSpeed * Time.deltaTime
            );
        }
    }
    #endregion

    private void UpdateMagnetStat()
    {
        // 자석 범위 재할당
        _magnetRadius = PlayerDataLoad.playerData.CurrentPlayerStat.GetTotalMagnetRange();//GetMagnetRange(_magnetRadius);
    }
}
