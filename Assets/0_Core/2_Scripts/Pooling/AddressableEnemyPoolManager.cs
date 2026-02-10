using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Pool;
using System.Collections.Generic;
using Cysharp.Threading.Tasks; // UniTask 활용 권장

public class AddressableEnemyPoolManager : MonoBehaviour
{
    public static AddressableEnemyPoolManager Instance { get; private set; }

    [System.Serializable]
    public struct SpawnWeightInfo
    {
        public EnemyData data;
        public int weight; // 가중치 (높을수록 자주 나옴)
    }

    [Header("Spawn Configuration")]
    public List<SpawnWeightInfo> stageEnemies = new List<SpawnWeightInfo>();

    // 런타임 풀 저장소: Key는 EnemyData (또는 ID)
    private Dictionary<EnemyData, IObjectPool<GameObject>> _pools = new Dictionary<EnemyData, IObjectPool<GameObject>>();

    // 로드된 프리팹 캐싱 (메모리 해제를 위해 들고 있음)
    private Dictionary<EnemyData, GameObject> _loadedPrefabs = new Dictionary<EnemyData, GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 게임 시작 시 리스트에 있는 적들을 미리 로드합니다.
        PreloadEnemyAssets().Forget();
    }

    /// <summary>
    /// 스테이지에 등장할 적들의 에셋을 미리 다운로드/로드하고 풀을 생성합니다.
    /// </summary>
    private async UniTaskVoid PreloadEnemyAssets()
    {
        foreach (var info in stageEnemies)
        {
            if (info.data == null || _pools.ContainsKey(info.data)) continue;

            // 1. Addressables로 프리팹 로드 (비동기)
            var handle = info.data.ModelPrefab.LoadAssetAsync<GameObject>();
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject loadedPrefab = handle.Result;
                _loadedPrefabs[info.data] = loadedPrefab;

                // 2. ObjectPool 생성
                var pool = new ObjectPool<GameObject>(
                    createFunc: () => CreateEnemy(loadedPrefab, info.data),
                    //actionOnGet: (obj) => obj.SetActive(true),
                    actionOnGet: null,
                    actionOnRelease: (obj) => obj.SetActive(false),
                    actionOnDestroy: (obj) => Destroy(obj),
                    defaultCapacity: 10,
                    maxSize: 1000
                );

                _pools.Add(info.data, pool);
            }
            else
            {
                Debug.LogError($"적 데이터 로드 실패 : {info.data.EnemyName}");
            }
        }

        Debug.Log("데이터 로드 성공.");
    }

    private GameObject CreateEnemy(GameObject prefab, EnemyData data)
    {
        GameObject obj = Instantiate(prefab, transform);
        // 적 초기화 (데이터 주입)
        if (obj.TryGetComponent<Enemy>(out var enemy))
        {
            enemy.Initialize(data); // Enemy 스크립트에 이 함수를 추가해야 함
        }
        return obj;
    }

    /// <summary>
    /// 외부(SpawnSystem)에서 적을 요청할 때 사용
    /// </summary>
    public GameObject GetEnemy(EnemyData data, Vector3 position, Quaternion rotation)
    {
        if (_pools.TryGetValue(data, out var pool))
        {
            GameObject obj = pool.Get();
            obj.transform.position = position;
            obj.transform.rotation = rotation;

            obj.SetActive(true);
            return obj;
        }

        Debug.LogWarning($"Pool not found for {data.EnemyName}. Ensure it is in the Stage Enemies list.");
        return null;
    }

    /// <summary>
    /// 적을 풀로 반환
    /// </summary>
    public void ReturnEnemy(GameObject obj, EnemyData data)
    {
        if (_pools.TryGetValue(data, out var pool))
        {
            pool.Release(obj);
        }
        else
        {
            Destroy(obj); // 풀에 없으면 파괴
        }
    }

    // 씬 전환 시 메모리 정리
    private void OnDestroy()
    {
        foreach (var kvp in _loadedPrefabs)
        {
            // Addressables 메모리 해제
            if (kvp.Key.ModelPrefab.IsValid())
            {
                kvp.Key.ModelPrefab.ReleaseAsset();
            }
        }
        _loadedPrefabs.Clear();
        _pools.Clear();
    }
}