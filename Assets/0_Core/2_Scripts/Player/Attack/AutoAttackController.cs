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
    private readonly List<(float distanceSqr, IEnemy enemy)> _candidateList = new List<(float, IEnemy)>();

    private void Start()
    {
        // 최대 탐지 범위의 제곱값을 미리 계산 (Vector3.sqrMagnitude 비교용)
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

        // 모든 무기 마운트의 쿨타임 업데이트
        foreach (var mount in _weaponMounts)
        {
            if (mount == null || !mount.enabled) continue;

            // 쿨타임이 다 돌았는지 확인
            if (mount.TickCooldown(Time.deltaTime))
            {
                int enemyCount = mount.WeaponData.EnemyCount;
                if (enemyCount <= 0) enemyCount = 1;

                // 발사 준비가 되었다면, 해당 무기의 사거리 내 적 탐색
                FindClosestEnemiesInRange(mount.WeaponData.Range, enemyCount, _targetList);

                // 타겟이 1명이라도 있으면 리스트를 통째로 전달하여 발사
                if (_targetList.Count > 0)
                {
                    mount.Fire(_targetList);
                }
            }
        }
    }

    /// <summary>
    /// 지정된 사거리(range) 내에서 가장 가까운 적을 'count'만큼 찾아
    /// 'out_targets' 리스트에 채워넣습니다.
    /// </summary>
    private void FindClosestEnemiesInRange(float range, int count, List<IEnemy> out_targets)
    {
        // 1. 캐시된 리스트들을 비웁니다. (메모리 재할당 없음)
        _candidateList.Clear();
        out_targets.Clear();

        IReadOnlyList<IEnemy> enemies = EnemyManager.Instance.ActiveEnemies;
        if (enemies.Count == 0)
        {
            return;
        }

        float rangeSqr = range * range; // 무기 사거리 제곱
        Vector3 myPosition = transform.position;

        // 2. 사거리 내의 모든 적을 후보 리스트(_candidateList)에 추가
        for (int i = 0; i < enemies.Count; i++)
        {
            IEnemy enemy = enemies[i];

            if (enemy == null || !enemy.IsAlive)
            {
                continue;
            }

            float dSqrToTarget = (enemy.TargetTransform.position - myPosition).sqrMagnitude;

            // (최적화) 1차로 최대 탐지 범위(_maxDetectionRangeSqr)보다 멀면 무시
            if (dSqrToTarget > _maxDetectionRangeSqr)
            {
                continue;
            }

            // 2차로 이 무기의 유효 사거리(rangeSqr)보다 가까운지 확인
            if (dSqrToTarget < rangeSqr)
            {
                _candidateList.Add((dSqrToTarget, enemy));
                
            }
        }

        // 3. 후보 리스트가 비어있으면 종료
        if (_candidateList.Count == 0)
        {
            return;
        }

        // 4. 후보 리스트를 '가까운 순'으로 정렬 (Sort는 GC를 발생시키지 않음)
        _candidateList.Sort((a, b) => a.distanceSqr.CompareTo(b.distanceSqr));

        // 5. 정렬된 후보 중 최대 'count'만큼만 최종 'out_targets' 리스트에 복사
        int numToTake = Mathf.Min(_candidateList.Count, count);
        for (int i = 0; i < numToTake; i++)
        {
            out_targets.Add(_candidateList[i].enemy);
            SyncDebugView();
        }
    }

}
