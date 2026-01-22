using UnityEngine;
using System.Collections.Generic;
using MoreMountains.Feedbacks;

public class EnemyManager : MonoBehaviour
{
    public GameObject m_EnemyDropPrefab;

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
            Destroy(this);
        }
    }
    #endregion
    public IReadOnlyList<IEnemy> ActiveEnemies => _activeEnemies;
    private readonly List<IEnemy> _activeEnemies = new List<IEnemy>(); 
    private readonly List<IEnemy> _enemiesToRemove = new List<IEnemy>();

    private bool _isBombKill = false;

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
    #region 적 리스트 관련
    // 적 리스트 등록 및 제거
    public void RegisterEnemy(IEnemy enemy)
    {
        if (enemy != null && !_activeEnemies.Contains(enemy))
        {
            _activeEnemies.Add(enemy);           
        }
    }

    public void UnregisterEnemy(IEnemy enemy)
    {
        if (enemy != null)
        {
            if (_isBombKill) return;

            _activeEnemies.Remove(enemy);
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
    #endregion

    #region 아이템 드랍
    
    // 아이템 드랍 함수
    public GameObject GetDrop(Vector3 deathPos)
    {
        Vector3 dropPos = deathPos;
        dropPos.y += 0.5f;
        var dropitem = ObjectPoolManager.Instance.Get(m_EnemyDropPrefab);
        dropitem.transform.position = dropPos;
        return dropitem;
    }
    #endregion

    #region 올 킬 아이템 로직
    public void KillAllEnemies()
    {
        _isBombKill = true;

        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            var enemy = _activeEnemies[i];

            if(enemy is Enemy aliveEnemy)
            {
                if (aliveEnemy.IsAlive)
                {
                    aliveEnemy.InstantKill();
                }
            }
            else
            {
                enemy.TakeDamage(7777, out int dmg);
            }
        }

        _activeEnemies.Clear();

        _isBombKill = false;

        /*for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            var enemy = _activeEnemies[i];

            if (enemy is Enemy concreteEnemy)
            {
                if (concreteEnemy.IsAlive)
                {
                    concreteEnemy.InstantKill();
                }
            }
            else
            {
                enemy.TakeDamage(7777);
            }
        }*/
    }
    #endregion

}