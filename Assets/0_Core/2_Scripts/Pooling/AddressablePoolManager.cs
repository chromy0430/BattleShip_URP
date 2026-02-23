using BattleShip; 
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablePoolManager : StaticInstance<AddressablePoolManager>
{
    [System.Serializable]
    public struct SpawnWeightInfo
    {
        public EnemyData data;
        public int weight; // 가중치 (높을수록 자주 나옴)
    }

    [Header("스폰 설정")]
    public List<SpawnWeightInfo> stageEnemies = new List<SpawnWeightInfo>();

    // 추후 아이템이나 투사체 추가 시, 리스트 추가
    // public List<ItemSpawnInfo> stageItems = new List<ItemSpawnInfo>();

    // Key를 object 타입으로 선언하여 SO, AssetReference, GameObject 모두 수용
    private Dictionary<object, IObjectPool<GameObject>> _pools = new Dictionary<object, IObjectPool<GameObject>>();
    private Dictionary<object, GameObject> _loadedPrefabs = new Dictionary<object, GameObject>();

    private Dictionary<GameObject, object> _instanceToKeyLookup = new Dictionary<GameObject, object>();


    public bool IsReady { get; private set; } = false;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        // 게임 시작 시, 리스트에 있는 적 미리 로드
        InitializePoolsAsync().Forget();
    }

    #region 풀링 시스템

    // 풀링 대기 함수
    private async UniTaskVoid InitializePoolsAsync()
    {
        IsReady = false;

        await PreloadEnemyAssets();

        // 추후 아이템 로딩이 생기면 여기에 추가: await PreloadItemAssets();

        IsReady = true;
    }

    // 비동기 에셋 로드 후 풀 생성
    private async UniTask PreloadAssetAsync(object key, AssetReference assetRef)
    {
        if (key == null || _pools.ContainsKey(key)) return;

        var handle = assetRef.LoadAssetAsync<GameObject>();
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject loadedPrefab = handle.Result;
            _loadedPrefabs[key] = loadedPrefab;
            CreatePoolInternal(key, loadedPrefab, 400);
        }
        else
        {
            Debug.LogError($"[Addressable Pool] 에셋 로드 실패 : {key}");
        }
    }

    // 오브젝트 풀 생성
    private void CreatePoolInternal(object key, GameObject prefab, int prewarmCount = 100)
    {
        if (_pools.ContainsKey(key)) return;

        var pool = new ObjectPool<GameObject>
            (
            createFunc: () =>
                {
                    GameObject obj = Instantiate(prefab, transform);
                    _instanceToKeyLookup[obj] = key;

                    if (key is EnemyData enemyData && obj.TryGetComponent<Enemy>(out var enemy))
                    {
                        enemy.Initialize(enemyData);
                    }

                    return obj;
                },
            actionOnGet: null,
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) =>
            {
                _instanceToKeyLookup.Remove(obj);
                Destroy(obj);
            },
            defaultCapacity: prewarmCount,
            maxSize: 1500
        );
        _pools.Add(key, pool);

        GameObject[] prewarmObjects = new GameObject[prewarmCount];
        for (int i = 0; i < prewarmCount; i++)
        {
            prewarmObjects[i] = pool.Get();
        }
        for (int i = 0; i < prewarmCount; i++)
        {
            pool.Release(prewarmObjects[i]);
        }
    }

    // 특정 프리펩으로 활성화된 객체 수 반환
    public int GetActiveCount(object key)
    {
        if (key != null && _pools.TryGetValue(key, out var pool))
        {
            if (pool is ObjectPool<GameObject> concretePool)
            {
                return concretePool.CountActive;
            }
        }
        return 0;
    }

    private async UniTask PreloadEnemyAssets()
    {
        foreach (var info in stageEnemies)
        {
            if (info.data != null && !_pools.ContainsKey(info.data))
            {
                await PreloadAssetAsync(info.data, info.data.ModelPrefab);
            }
        }
    }

    public GameObject GetEnemy(EnemyData data, Vector3 position, Quaternion rotation)
    {
        if (_pools.TryGetValue(data, out var pool))
        {
            GameObject obj = pool.Get();
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);            
            return obj;
        }

        Debug.LogWarning($"[Pool] 등록되지 않은 적군 요청: {data.EnemyName}");
        return null;
    }

    #region 풀링 반환
    public void ReturnEnemy(GameObject obj, EnemyData data)
    {
        ReturnToPool(obj, data);
    }

    public void ReturnToPool(GameObject obj, object key)
    {
        if (obj == null) return;

        if (key != null && _pools.TryGetValue(key, out var pool))
        {
            if (obj.activeSelf) pool.Release(obj);
        }
        else
        {
            Destroy(obj);
        }
    }
    #endregion

    #endregion

    public void CreatePool(GameObject prefab, int poolCount)
    {
        if (prefab != null && poolCount > 0)
        {
            CreatePoolInternal(prefab, prefab, poolCount);
        }
    }

    // GameObject 자체를 Key로 사용하는 Get
    public GameObject Get(GameObject prefab)
    {
        if (prefab == null) return null;

        // 사전 로드되지 않은 프리팹이라면 즉시 동기식 생성
        if (!_pools.ContainsKey(prefab))
        {
            _loadedPrefabs[prefab] = prefab;
            CreatePoolInternal(prefab, prefab, 500);
        }

        GameObject obj = _pools[prefab].Get();
        obj.SetActive(true);
        return obj;
    }

    public void Return(GameObject obj)
    {
        if (obj == null || !obj.activeSelf) return;

        if (_instanceToKeyLookup.TryGetValue(obj, out object key))
        {
            if (_pools.TryGetValue(key, out var pool))
                pool.Release(obj);
        }
        else
        {
            Destroy(obj);
        }
    }

    // 지연 반환 지원
    public void Return(GameObject obj, float delay)
    {
        ReturnDelayed(obj, delay).Forget();
    }

    private async UniTaskVoid ReturnDelayed(GameObject obj, float delay)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: obj.GetCancellationTokenOnDestroy());
        Return(obj);
    }

    

    // 씬 전환 시 메모리 정리
    private void OnDestroy()
    {
        foreach (var kvp in _loadedPrefabs)
        {
            // 어드레서블로 로드된 자원(EnemyData 등)만 메모리 직접 릴리즈
            if (kvp.Key is EnemyData ed && ed.ModelPrefab.IsValid())
            {
                ed.ModelPrefab.ReleaseAsset();
            }
            // 추후 ItemData가 추가된다면 여기에 else if 로 릴리즈를 덧붙임
        }
        _loadedPrefabs.Clear();
        _pools.Clear();
        _instanceToKeyLookup.Clear();
    }
}