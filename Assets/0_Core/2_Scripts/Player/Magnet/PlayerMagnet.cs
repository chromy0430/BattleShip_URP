using BattleShip;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMagnet : StaticInstance<PlayerMagnet>
{    
    [Header("획득 관련")]
    [SerializeField] private float _collectionRadius = 1.5f;
    private float _collectionRadiusSqr;

    [Header("코인 자석 효과")]
    [SerializeField] private float _magnetRadius;
    [SerializeField] private float _coinMoveSpeed = 30.0f;
    private float _magnetRadiusSqr;

    [Header("자석 아이템 관련")]
    [SerializeField] private float _magnetItemSpeed = 50.0f;
    private bool _isMagnetItemActive = false;
    private readonly List<EnemyDrop> _magnetTargetCoins = new List<EnemyDrop>(1000);

    private Transform _PlayerTransform;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        if (PlayerManager.Instance != null)
        {
            _PlayerTransform = PlayerManager.Instance.PlayerTransform;
        }

        if (PlayerDataLoad.Instance != null)
        {
            UpdateMagnetStat();
            PlayerDataLoad.Instance.OnStatUpdated += UpdateMagnetStat;
        }
    }

    private void OnDisable()
    {
        if (PlayerDataLoad.Instance != null) PlayerDataLoad.Instance.OnStatUpdated -= UpdateMagnetStat;
    }

    void Update()
    {
        if (_PlayerTransform == null || DropManager.Instance == null) return;

        if (_isMagnetItemActive)
        {
            ProcessMagnetItemCoins();
        }


        if (DropManager.Instance.activeDrops.Count > 0)
        {
            ProcessNormalCoins();
        }
    }

    #region 아이템 줍기

    private void ProcessNormalCoins()
    {
        Vector3 playerPos = _PlayerTransform.position;
        float deltaTime = Time.deltaTime;

        var activeDrops = DropManager.Instance.activeDrops;

        for (int i = activeDrops.Count - 1; i >= 0; i--)
        {
            EnemyDrop coin = activeDrops[i];
            if (coin == null || !coin.gameObject.activeInHierarchy) continue;

            Vector3 coinPos = coin.transform.position;
            float distSqr = (coinPos - playerPos).sqrMagnitude;

            if (distSqr <= _collectionRadiusSqr) // 획득
            {
                if (PlayerExpManager.Instance != null) PlayerExpManager.Instance.GetExp(coin.expAmount);
                DropManager.Instance.RemoveDrop(coin);
                AddressablePoolManager.Instance.Return(coin.gameObject);
            }
            else if (distSqr <= _magnetRadiusSqr) // 자석 범위 내 끌림
            {
                coin.transform.position = Vector3.MoveTowards(coinPos, playerPos, _coinMoveSpeed * deltaTime);
            }
        }
    }

    private void ProcessMagnetItemCoins()
    {
        Vector3 playerPos = _PlayerTransform.position;
        float deltaTime = Time.deltaTime;

        for (int i = _magnetTargetCoins.Count - 1; i >= 0; i--)
        {
            EnemyDrop coin = _magnetTargetCoins[i];

            if (coin == null || !coin.gameObject.activeInHierarchy)
            {
                RemoveCoinFromList(_magnetTargetCoins, i);
                continue;
            }

            Vector3 coinPos = coin.transform.position;
            float distSqr = (coinPos - playerPos).sqrMagnitude;

            // 획득
            if (distSqr <= _collectionRadiusSqr)
            {
                RemoveCoinFromList(_magnetTargetCoins, i);
                AddressablePoolManager.Instance.Return(coin.gameObject);
            }
            else
            {
                coin.transform.position = Vector3.MoveTowards(coinPos, playerPos, _magnetItemSpeed * deltaTime);
            }
        }

        if (_magnetTargetCoins.Count == 0)
        {
            _isMagnetItemActive = false;
        }
    }

    #endregion

    #region 헬퍼 함수
    private void RemoveCoinFromList(List<EnemyDrop> list, int index)
    {
        int lastIndex = list.Count - 1;
        if (index != lastIndex)
        {
            list[index] = list[lastIndex];
        }
        list.RemoveAt(lastIndex);
    }

    public void ActivateMagnetItem()
    {
        var activeDrops = DropManager.Instance.activeDrops;
        if (activeDrops.Count > 0)
        {
            _magnetTargetCoins.AddRange(activeDrops);
            activeDrops.Clear();
            _isMagnetItemActive = true;
        }
    }

    private void UpdateMagnetStat()
    {
        // 자석 범위 재할당
        _collectionRadiusSqr = _collectionRadius * _collectionRadius;
        _magnetRadius = PlayerDataLoad.Instance.CurrentPlayerStat.GetTotalMagnetRange();
        _magnetRadiusSqr = _magnetRadius * _magnetRadius;
    }
    #endregion    
}
