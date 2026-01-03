using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private GameObject m_EnemyDropPrefab;

    #region 싱글톤   
    public static EnemyManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }
        else
        {
            Destroy(gameObject);
        }

        // 드랍물 오브젝트풀링 시작
        InitDrop();
    }
    #endregion
    public IReadOnlyList<IEnemy> ActiveEnemies => _activeEnemies;
    private readonly List<IEnemy> _activeEnemies = new List<IEnemy>(); 
    private readonly List<IEnemy> _enemiesToRemove = new List<IEnemy>();

#if UNITY_EDITOR
    [SerializeField, Header("Debug View (Editor Only)")]
    private List<UnityEngine.Object> _debugActiveEnemies = new List<UnityEngine.Object>();

    private void Update()
    {
        if (_activeEnemies.Count != _debugActiveEnemies.Count)
        {
            SyncDebugView();
        }
    }

    private void SyncDebugView()
    {
        _debugActiveEnemies.Clear();
        foreach (var enemy in _activeEnemies)
        {
            if (enemy is UnityEngine.Object obj)
            {
                _debugActiveEnemies.Add(obj);
            }
        }
    }
#endif
    public void InitDrop()
    {
        ObjectPoolManager.Instance.CreatePool(m_EnemyDropPrefab, 500);
    }

    public GameObject GetDrop()
    {
        var dropitem = ObjectPoolManager.Instance.Get(m_EnemyDropPrefab);
        return dropitem;
    }
    public void RegisterEnemy(IEnemy enemy)
    {
        if (enemy != null && !_activeEnemies.Contains(enemy))
        {
            _activeEnemies.Add(enemy);
            Debug.Log("enemy 추가");
        }
    }

    public void UnregisterEnemy(IEnemy enemy)
    {
        if (enemy != null)
        {
            _activeEnemies.Remove(enemy);
            Debug.Log("enemy 삭제");
        }
    }
    public void CleanUpDeadEnemies()
    {
        foreach (var enemy in _activeEnemies)
        {
            if (enemy == null || !enemy.IsAlive)
            {
                _enemiesToRemove.Add(enemy);
            }
        }

        foreach (var enemy in _enemiesToRemove)
        {
            _activeEnemies.Remove(enemy);
        }

        _enemiesToRemove.Clear();
    }
}