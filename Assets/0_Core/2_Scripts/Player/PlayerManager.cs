using StylizedWater3;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    public Transform PlayerTransform { get; set; }

    [Header("Weapon System")]
    private AutoAttackController autoAttackController;

    public event Action getDamage;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }

        PlayerTransform = this.transform;
        autoAttackController = GetComponentInChildren<AutoAttackController>();
    }

    #region 자석 아이템 로직

    [Header("Magnet Item Settings")]
    [SerializeField] private float _globalMagnetSpeed = 30.0f;

    // 활성화된 아이템 리스트
    private List<EnemyDrop> _activeCoins = new List<EnemyDrop>();
    private List<EnemyDrop> _allCoins = new List<EnemyDrop>();

    // 자석 아이템 먹으면 들어오는 아이템 리스트
    private List<EnemyDrop> _magnetTargetCoins = new List<EnemyDrop>();

    private bool _isGlobalMagnetActive = false;

    private void Update()
    {
        // 전역 자석 모드가 켜져있을 때만 리스트를 순회하며 이동
        if (_isGlobalMagnetActive && PlayerTransform != null)
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
        if (_allCoins.Contains(coin))
        {
            _allCoins.Remove(coin);
        }

        if (_magnetTargetCoins.Contains(coin))
        {
            _magnetTargetCoins.Remove(coin);
        }
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
        //PlayerManager.Instance.Doingmagnet = true;

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
                PlayerTransform.position,
                _globalMagnetSpeed * Time.deltaTime
            );
        }

        if (_magnetTargetCoins.Count == 0)
        {
            _isGlobalMagnetActive = false;

            //PlayerManager.Instance.Doingmagnet = false;
            Debug.Log("자석 모드 종료");
        }
    }

    #endregion

    #region 무기 획득 함수

    // 외부(PlayerItemManager)에서 호출할 함수
    public bool TryAddWeapon(BaseWeaponData newWeaponData)
    {
        if (autoAttackController == null)
        {
            Debug.LogError("[PlayerManager] AutoAttackController 컴포넌트가 없습니다!");
            return false;
        }

        var mounts = autoAttackController.GetWeaponMounts();

        // 리스트를 순회하며 빈 포문 찾기
        foreach (var mount in mounts)
        {
            // [조건] 무기가 장착되어 있지 않다면?
            if (!mount.HasWeapon())
            {
                // 장착 실행
                mount.EquipWeapon(newWeaponData);
                Debug.Log($"[PlayerManager] {newWeaponData.Name} 장착 완료 (슬롯: {mount.name})");
                return true; // 장착 성공하고 종료
            }
        }

        // [결과] 모든 반복문을 돌았는데 return이 안 됐다면 = 꽉 찼다는 뜻
        Debug.Log("[PlayerManager] 무기 슬롯이 가득 차서 획득할 수 없습니다. (교체 불가)");
        return false;
    }

    #endregion

    #region 데미지 피해
    public void Damaged()
    {
        getDamage?.Invoke();
    }
    #endregion
}