using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
public class TargetScanner : MonoBehaviour
{
    // 잡에서 계산할 데이터 구조체
    public struct EnemyDistData : System.IComparable<EnemyDistData>
    {
        public int index; // EnemyManager 리스트에서의 원래 인덱스
        public float distanceSqr; // 거리 제곱

        public int CompareTo(EnemyDistData other)
        {
            return distanceSqr.CompareTo(other.distanceSqr); // 오름차순(가까운 순) 정렬
        }
    }

    // CPU 병렬 처리를 위한 잡
    [BurstCompile]
    struct CalculateDistanceJob : IJobParallelFor
    {
        [ReadOnly] public float3 playerPos;
        [ReadOnly] public NativeArray<float3> enemyPositions;
        [WriteOnly] public NativeArray<EnemyDistData> results;

        public void Execute(int i)
        {
            float3 dir = enemyPositions[i] - playerPos;
            results[i] = new EnemyDistData
            {
                index = i,
                distanceSqr = math.lengthsq(dir)
            };
        }
    }

    private NativeArray<EnemyDistData> _sortedEnemies;
    private bool _isAllocated;

    // 매 프레임 적들의 거리 계산 및 정렬
    public void UpdateScanner(Vector3 playerPos, IReadOnlyList<IEnemy> activeEnemies)
    {
        int count = activeEnemies.Count;
        if (count == 0) return;

        // 메모리 할당 (TempJob: 프레임 종료 시 자동 해제되는 빠른 할당)
        NativeArray<float3> enemyPosArray = new NativeArray<float3>(count, Allocator.TempJob);
        NativeArray<EnemyDistData> resultDistArray = new NativeArray<EnemyDistData>(count, Allocator.TempJob);

        // 위치 데이터 복사
        for (int i = 0; i < count; i++)
        {
            if (activeEnemies[i] != null && activeEnemies[i].IsAlive)
                enemyPosArray[i] = activeEnemies[i].TargetTransform.position;
            else
                enemyPosArray[i] = float3.zero; // 죽은 적 예외처리
        }

        // 잡 스케줄링 및 즉시 실행
        var calcJob = new CalculateDistanceJob
        {
            playerPos = playerPos,
            enemyPositions = enemyPosArray,
            results = resultDistArray
        };
        calcJob.Schedule(count, 64).Complete(); // 즉시 완료 대기

        // 고속 정렬 (NativeArray Sort는 매우 빠름)
        resultDistArray.Sort();

        // 결과 캐싱 (이전 프레임 데이터 해제 후 덮어쓰기)
        if (_isAllocated) _sortedEnemies.Dispose();
        _sortedEnemies = new NativeArray<EnemyDistData>(resultDistArray, Allocator.Persistent);
        _isAllocated = true;

        enemyPosArray.Dispose();
        resultDistArray.Dispose();
    }

    // 정렬된 리스트에서 조건에 맞는 적을 추출 (GC 발생 없음)
    public void GetTargets(float range, int count, List<IEnemy> outTargets, IReadOnlyList<IEnemy> originalList)
    {
        outTargets.Clear();
        if (!_isAllocated || _sortedEnemies.Length == 0) return;

        float rangeSqr = range * range;
        int foundCount = 0;

        for (int i = 0; i < _sortedEnemies.Length; i++)
        {
            if (foundCount >= count) break;

            EnemyDistData data = _sortedEnemies[i];

            // 사거리 밖이면 그 뒤는 볼 필요도 없음 (정렬되어 있으므로) -> 최적화 핵심
            if (data.distanceSqr > rangeSqr) break;

            // 원본 리스트 유효성 체크
            if (data.index < originalList.Count)
            {
                IEnemy enemy = originalList[data.index];
                if (enemy != null && enemy.IsAlive)
                {
                    outTargets.Add(enemy);
                    foundCount++;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (_isAllocated) _sortedEnemies.Dispose();
    }
}