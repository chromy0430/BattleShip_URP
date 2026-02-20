using BattleShip;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class InitManager : StaticInstance<InitManager>
{
    // 풀링용 리스트
    public List<BaseWeaponData> allWeaponDatas;
    //public List<GameObject> allEnemyPrefabs;

    #region 의존성 주입
    public UpgradeManager upgradeManager;
    public WeaponUIManager weaponUIManager;
    public GameObject DamageFont;
    public EnemyManager enemyManager;
    #endregion


    #region 싱글톤
    protected override void Awake()
    {
        base.Awake();
    }
    #endregion

    private void OnEnable()
    {
        weaponUIManager.RefreshAllSlots();
    }

    private void Start()
    {
        #region 초기화 풀링
        InitializePooling();

        if (upgradeManager != null)
        {
            // 약간의 딜레이를 주어 다른 시스템(UI 등)이 모두 준비된 후 띄우는 것이 안전함
            ShowSelectionDelayed().Forget();
        }
        else
        {
            Debug.Log("업그레이드 매니저 없음");
        }

            #endregion
            #region UI 초기화
            weaponUIManager.RefreshAllSlots();
        #endregion
    }

    private void InitializePooling()
    {
        if (ObjectPoolManager.Instance != null)
        {
            foreach (var data in allWeaponDatas)
            {
                ObjectPoolManager.Instance.CreatePool(data.ProjectilePrefab, 500);
                ObjectPoolManager.Instance.CreatePool(data.ShotEffect, 500);
            }
            ObjectPoolManager.Instance.CreatePool(DamageFont, 2000);
            if (enemyManager != null)
                ObjectPoolManager.Instance.CreatePool(enemyManager.m_EnemyDropPrefab, 1000);
        }
    }

    private async UniTaskVoid ShowSelectionDelayed()
    {
        // 0.1초 정도 대기 (Start 로직들이 꼬이지 않게 안전장치)
        await UniTask.Delay(100);

        upgradeManager.ShowStartingWeaponSelection();
    }
}
