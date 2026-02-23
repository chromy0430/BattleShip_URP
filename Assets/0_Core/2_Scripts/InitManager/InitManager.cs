using BattleShip;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class InitManager : StaticInstance<InitManager>
{
    // 풀링용 리스트
    public List<BaseWeaponData> allWeaponDatas;
    // 풀링용 데미지 폰트
    public GameObject DamageFont;
    //public List<GameObject> allEnemyPrefabs;

    [Header("UI Transition")]
    public GameObject loadingPanel;

    #region 의존성 주입 변수
    public UpgradeManager upgradeManager;
    public WeaponUIManager weaponUIManager;    
    public EnemyManager enemyManager;
    public PlayerManager playerManager;
    public DropManager dropManager;
    public PlayerMagnet playerMagnet;
    #endregion    

    #region 싱글톤
    protected override void Awake()
    {
        base.Awake();
        BootSequenceAsync().Forget();
    }
    #endregion

    private void OnEnable()
    {
        weaponUIManager.RefreshAllSlots();
    }

    private async UniTaskVoid BootSequenceAsync()
    {
        Debug.Log("[Init] 1. 시스템 매니저 할당 대기 중...");

        // 1. 핵심 매니저들이 씬에 인스턴스화될 때까지 대기
        await UniTask.WaitUntil(() =>
            AddressablePoolManager.Instance != null &&
            DropManager.Instance != null &&
            PlayerMagnet.Instance != null
        );

        Debug.Log("[Init] 2. 어드레서블 로딩 대기 중...");

        // 2. 어드레서블 데이터 100% 로드 대기
        await UniTask.WaitUntil(() => AddressablePoolManager.Instance.IsReady);

        Debug.Log("[Init] 3. 자원 프리웜(Prewarm) 시작");

        // 3. 자석 등 모든 매니저가 보장된 상태에서 프리웜 가동 (Null 완벽 차단)
        InitializePooling();

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        Debug.Log("[Init] 4. UI 렌더링 및 게임 시작");

        if (weaponUIManager != null) weaponUIManager.RefreshAllSlots();
        if (upgradeManager != null) upgradeManager.ShowStartingWeaponSelection();
    }

    private void InitializePooling()
    {
        foreach (var data in allWeaponDatas)
        {
            if (data.ProjectilePrefab != null) AddressablePoolManager.Instance.Prewarm(data.ProjectilePrefab, 500);
            if (data.ShotEffect != null) AddressablePoolManager.Instance.Prewarm(data.ShotEffect, 500);
        }

        if (DamageFont != null) AddressablePoolManager.Instance.Prewarm(DamageFont, 2000);
        if (enemyManager != null && enemyManager.m_EnemyDropPrefab != null)
        {
            AddressablePoolManager.Instance.Prewarm(enemyManager.m_EnemyDropPrefab, 1000);
        }    
    }
}
