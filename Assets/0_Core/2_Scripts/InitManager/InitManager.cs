using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class InitManager : MonoBehaviour
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
    public static InitManager Instance { get; private set; }
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
    }
    #endregion

    private void Start()
    {
        #region 초기화 풀링
        if (ObjectPoolManager.Instance != null)
        {
            foreach (var data in allWeaponDatas)
            {
                ObjectPoolManager.Instance.CreatePool(data.ProjectilePrefab, 500);
                ObjectPoolManager.Instance.CreatePool(data.ShotEffect, 500);
            }

            /*foreach (var enemy in allEnemyPrefabs)
            {
                ObjectPoolManager.Instance.CreatePool(enemy, 1000);
            }*/

            // 데미지 폰트 풀링
            ObjectPoolManager.Instance.CreatePool(DamageFont, 2000);

            // 드랍템 풀링
            if (enemyManager != null)
                        ObjectPoolManager.Instance.CreatePool(enemyManager.m_EnemyDropPrefab, 1000);
        }
        else
        {
            Debug.Log("ObjectPoolManager 없음");
        }
        #endregion
        #region UI 초기화
        weaponUIManager.RefreshAllSlots();
        #endregion
    }


}
