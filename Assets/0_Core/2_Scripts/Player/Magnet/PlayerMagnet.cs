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

    private readonly List<EnemyDrop> _allCoins = new List<EnemyDrop>();
    private readonly List<EnemyDrop> _magnetTargetCoins = new List<EnemyDrop>();

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
        if (_PlayerTransform == null) return;

        if (_isMagnetItemActive)
        {
            ProcessMagnetItemCoins();
        }


        if (_allCoins.Count > 0)
        {
            ProcessNormalCoins();
        }
    }

    #region 아이템 줍기

    private void ProcessNormalCoins()
    {
        Vector3 playerPos = _PlayerTransform.position;
        float deltaTime = Time.deltaTime;

        for (int i = _allCoins.Count - 1; i >= 0; i--)
        {
            EnemyDrop coin = _allCoins[i];

            if (coin == null || !coin.gameObject.activeInHierarchy)
            {
                RemoveCoinFromList(_allCoins, i);
                continue;
            }

            Vector3 coinPos = coin.transform.position;
            float distSqr = (coinPos - playerPos).sqrMagnitude;

            if (distSqr <= _collectionRadiusSqr)
            {
                RemoveCoinFromList(_allCoins, i);
                ObjectPoolManager.Instance.Return(coin.gameObject);
            }
            else if (distSqr <= _magnetRadiusSqr)
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
                ObjectPoolManager.Instance.Return(coin.gameObject);
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

    public void RegisterCoin(EnemyDrop coin)
    {
        if (!_allCoins.Contains(coin) && !_magnetTargetCoins.Contains(coin))
        {
            _allCoins.Add(coin);
        }
    }

    public void UnregisterCoin(EnemyDrop coin)
    {
        if (coin == null) return;

        int indexAll = _allCoins.IndexOf(coin);
        if (indexAll >= 0)
        {
            RemoveCoinFromList(_allCoins, indexAll);
            return;
        }

        int indexMagnet = _magnetTargetCoins.IndexOf(coin);
        if (indexMagnet >= 0) 
        {
            RemoveCoinFromList(_magnetTargetCoins, indexMagnet);
        }
    }

    public void ActivateMagnetItem()
    {
        if (_allCoins.Count > 0)
        {
            _magnetTargetCoins.AddRange(_allCoins);
            _allCoins.Clear();

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
