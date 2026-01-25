using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// [수정]
/// 게임 내 모든 GameObject(투사체, 이펙트, 적)를 풀링하여 관리하는 싱글톤 매니저입니다.
/// 요청하신 RuntimeInitializeOnLoadMethod 싱글톤 패턴과 문자열 보간법($)을 적용합니다.
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    #region 싱글톤
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }
        else
        {
            Destroy(this);
        }

    }
    #endregion

    [Header("풀 설정")]
    [Tooltip("풀을 미리 생성할 때 사용할 기본 크기")]
    [SerializeField] private int _initialPoolSize = 500;

    private readonly Dictionary<GameObject, Queue<GameObject>> _pools =
        new Dictionary<GameObject, Queue<GameObject>>();

    private readonly Dictionary<GameObject, GameObject> _prefabLookup =
        new Dictionary<GameObject, GameObject>();

    // 프리펩별 활성화된 객체 수
    private readonly Dictionary<GameObject, int> _activeCounts = new Dictionary<GameObject, int>();

    #region 풀링 관련
    /// <summary>
    /// 풀을 미리 생성하고 채워넣습니다. (Pre-warming)
    /// </summary>
    public void CreatePool(GameObject prefab, int size)
    {
        if (!_pools.ContainsKey(prefab))
        {
            _pools.Add(prefab, new Queue<GameObject>());
            for (int i = 0; i < size; i++)
            {
                AddNewObjectToPool(prefab);
            }
        }
    }

    /// <summary>
    /// 풀에 새 오브젝트를 생성하여 추가하는 내부 함수
    /// </summary>
    private GameObject AddNewObjectToPool(GameObject prefab)
    {
        var obj = Instantiate(prefab, this.transform);

#if UNITY_EDITOR
        // 문자열 보간법 적용
        obj.name = $"{prefab.name} (Pool)";
#endif

        _prefabLookup.Add(obj, prefab);
        obj.SetActive(false);
        _pools[prefab].Enqueue(obj);
        return obj;
    }

    /// <summary>
    /// 풀에서 오브젝트를 가져옵니다.
    /// </summary>
    public GameObject Get(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("[ObjectPoolManager] Get 호출 시 Prefab이 null입니다.");
            return null;
        }

        if (!_pools.ContainsKey(prefab))
        {
            CreatePool(prefab, _initialPoolSize);
        }

        Queue<GameObject> pool = _pools[prefab];
        GameObject obj;

        if (pool.Count == 0)
        {
            // [수정] 문자열 보간법 적용
            Debug.LogWarning($"풀({prefab.name})이 비어있어 확장합니다.");
            obj = AddNewObjectToPool(prefab);
        }
        else
        {
            obj = pool.Dequeue();
        }

        if (!_activeCounts.ContainsKey(prefab)) _activeCounts[prefab] = 0;
        _activeCounts[prefab]++;

        //obj = pool.Dequeue();
        obj.SetActive(true);
        return obj;
    }

    /// <summary>
    /// 사용이 끝난 오브젝트를 풀에 반환합니다.
    /// </summary>
    public void Return(GameObject obj)
    {
        if (obj == null) return;
        if (!obj.activeSelf) return; // 이미 반환된 객체

        if (_prefabLookup.TryGetValue(obj, out GameObject prefab))
        {
            obj.SetActive(false);
            _pools[prefab].Enqueue(obj);

            if (_activeCounts.ContainsKey(prefab))
            {
                _activeCounts[prefab] = Mathf.Max(0, _activeCounts[prefab] - 1);
            }
        }
        else
        {
            Debug.LogWarning($"풀에 등록되지 않은 오브젝트({obj.name}) 반환 시도. Destroy합니다.");
            Destroy(obj);
        }
    }

    #endregion

    /// <summary>
    /// 특정 프리펩으로 생성되어 현재 필드에 활성화된 객체 수를 반환합니다.
    /// </summary>
    public int GetActiveCount(GameObject prefab)
    {
        if (_activeCounts.TryGetValue(prefab, out int count))
        {
            return count;
        }
        return 0;
    }

    public void Return(GameObject obj, float delay)
    {
        ReturnDelayed(obj, delay).Forget();
    }

    private async UniTaskVoid ReturnDelayed(GameObject obj, float delay)
    {
        // 딜레이만큼 기다림 (이 동안 씬이 바뀌거나 오브젝트가 파괴되면 취소)
        await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: obj.GetCancellationTokenOnDestroy());

        // 반납 실행
        Return(obj);
    }
}