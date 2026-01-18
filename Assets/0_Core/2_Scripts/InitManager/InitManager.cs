using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class InitManager : MonoBehaviour
{
    public List<BaseWeaponData> allWeaponDatas;
    public List<GameObject> allEnemyPrefabs;

    private void Start()
    {
        if (ObjectPoolManager.Instance != null)
        {
            foreach (var data in allWeaponDatas)
            {
                ObjectPoolManager.Instance.CreatePool(data.ProjectilePrefab, 500);
                ObjectPoolManager.Instance.CreatePool(data.ShotEffect, 500);
                Debug.Log("투사체 풀링 생성");
            }

            // 적 데이터들로부터 풀 생성
            foreach (var enemy in allEnemyPrefabs)
            {
                ObjectPoolManager.Instance.CreatePool(enemy, 1000);
                Debug.Log("적 풀링 생성");
            }

            //PrewarmAllPools().Forget();
        }
        else
        {
            Debug.Log("ObjectPoolManager 없음");
        }
    }
}
