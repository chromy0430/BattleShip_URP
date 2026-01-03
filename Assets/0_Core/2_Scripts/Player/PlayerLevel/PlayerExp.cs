using System.Collections.Generic;
using UnityEngine;

public class PlayerExpManager : MonoBehaviour
{
    public static PlayerExpManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float _globalMagnetSpeed = 30.0f;

    private int[] PlayerRequireExp =
    {
        10, 12, 15, 30, 35, 45, 60, 80, 90, 100
    };
    [SerializeField] private int currentExp = 0;

    // 활성화된 아이템 리스트
    private List<EnemyDrop> _activeCoins = new List<EnemyDrop>();
    private List<EnemyDrop> _allCoins = new List<EnemyDrop>();

    // 자석 아이템 먹으면 들어오는 아이템 리스트
    private List<EnemyDrop> _magnetTargetCoins = new List<EnemyDrop>();

    private bool _isGlobalMagnetActive = false;
    private Transform _playerTransform;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    private void Start()
    {
        // PlayerManager가 싱글톤이라면 거기서 가져옴
        if (PlayerManager.Instance != null)
        {
            _playerTransform = PlayerManager.Instance.PlayerTransform;
        }
    }

    private void Update()
    {
        // 전역 자석 모드가 켜져있을 때만 리스트를 순회하며 이동
        if (_isGlobalMagnetActive && _playerTransform != null)
        {
            PullAllCoins();
        }
    }
    public void RegisterCoin(EnemyDrop coin)
    {
        if (!_allCoins.Contains(coin))
        {
            _allCoins.Add(coin);
        }
    }

    public void UnregisterCoin(EnemyDrop coin)
    {
        if (_allCoins.Contains(coin)) _allCoins.Remove(coin);

        if (_magnetTargetCoins.Contains(coin)) _magnetTargetCoins.Remove(coin);
    }
    public void ActivateGlobalMagnet()
    {
        _magnetTargetCoins.Clear();
        _magnetTargetCoins.AddRange(_allCoins);

        if (_magnetTargetCoins.Count > 0)
        {
            _isGlobalMagnetActive = true;
        }       
        Debug.Log("[PlayerExpManager] 전역 자석 발동! 모든 코인이 빨려옵니다.");
    }

    private void PullAllCoins()
    {
        for (int i = _magnetTargetCoins.Count - 1; i >= 0; i--)
        {
            EnemyDrop coin = _magnetTargetCoins[i];

            // 방어 코드
            if (coin == null || !coin.gameObject.activeInHierarchy)
            {
                _magnetTargetCoins.RemoveAt(i);
                continue;
            }

            coin.transform.position = Vector3.MoveTowards(
                coin.transform.position,
                _playerTransform.position,
                _globalMagnetSpeed * Time.deltaTime
            );
        }

        if (_magnetTargetCoins.Count == 0)
        {
            _isGlobalMagnetActive = false;
            Debug.Log("자석 모드 종료");
        }
    }

    public void GetExp(int amount = 1)
    {
        currentExp += amount;
    }
}
