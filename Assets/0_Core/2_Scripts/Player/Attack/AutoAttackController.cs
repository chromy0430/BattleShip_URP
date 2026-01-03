using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 배에 부착되어 자동 공격을 총괄하는 컨트롤러
/// 장착된 모든 WeaponMount를 관리하며, EnemyManager를 참조
/// 사거리 내의 적을 탐지하고 공격 
/// </summary>
public class AutoAttackController : MonoBehaviour
{
    [Header("무기 장착대")]
    [Tooltip("이 배에 장착된 모든 WeaponMount 리스트")]
    [SerializeField]
    private List<WeaponMount> _weaponMounts = new List<WeaponMount>();

    [Header("탐지 설정")]
    [Tooltip("적을 탐지할 최대 범위. 개별 무기 사거리와 별개, 탐색 연산 자체의 최대치")]
    [SerializeField]
    private float _maxDetectionRange = 100f;
    private float _maxDetectionRangeSqr; 
    private readonly List<IEnemy> _targetList = new List<IEnemy>();
    private TargetScanner _scanner;

    private void Start()
    {
        _scanner = GetComponent<TargetScanner>();
        if (_scanner == null) _scanner = gameObject.AddComponent<TargetScanner>();

        // 최대 탐지 범위의 제곱값을 미리 계산
        _maxDetectionRangeSqr = _maxDetectionRange * _maxDetectionRange;

        // 모든 무기 마운트 초기화
        foreach (var mount in _weaponMounts)
        {
            mount.Initialize();
        }
    }

#if UNITY_EDITOR
    [SerializeField, Header("Debug View (Editor Only)")]
    private List<UnityEngine.Object> _debugActiveEnemies = new List<UnityEngine.Object>();

    private void SyncDebugView()
    {
        _debugActiveEnemies.Clear();
        foreach (var enemy in _targetList)
        {
            if (enemy is UnityEngine.Object obj)
            {
                _debugActiveEnemies.Add(obj);
            }
        }
    }
#endif

    private void Update()
    {
#if UNITY_EDITOR
        if (_targetList.Count != _debugActiveEnemies.Count)
        {
            SyncDebugView();
        }
#endif
        if (EnemyManager.Instance != null)
        {
            _scanner.UpdateScanner(transform.position, EnemyManager.Instance.ActiveEnemies);
        }

        // 모든 무기 마운트의 쿨타임 업데이트
        foreach (var mount in _weaponMounts)
        {
            if (mount == null || !mount.enabled) continue;

            // 쿨타임이 다 돌았는지 확인
            if (mount.TickCooldown(Time.deltaTime))
            {
                int enemyCount = mount.WeaponData.EnemyCount;
                if (enemyCount <= 0) enemyCount = 1;

                _scanner.GetTargets(
                    mount.WeaponData.Range,
                    enemyCount,
                    _targetList,
                    EnemyManager.Instance.ActiveEnemies
                );

                // 타겟이 1명이라도 있으면 리스트를 통째로 전달하여 발사
                if (_targetList.Count > 0)
                {
                    mount.Fire(_targetList);
                }
            }
        }
    }
}
