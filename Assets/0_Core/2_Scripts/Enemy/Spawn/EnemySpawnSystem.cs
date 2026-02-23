using Cysharp.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

public class EnemySpawnSystem : MonoBehaviour
{
    [SerializeField] private PlayTimeUIManager playTime;

    [Tooltip("적이 스폰될 반경")]
    [SerializeField] private float maxSpawnRadius = 25f;

    [Header("원을 몇 등분 나눌 것인가")]
    [Tooltip("원을 몇 등분으로 나눌 것인가 (예: 12 = 시계 방향)")]
    [SerializeField] private int spawnDivisions = 12;
    [Tooltip("스폰 위치에 약간의 무작위성을 더해 너무 기계적으로 보이지 않게 함 (각도 오차 범위)")]
    [SerializeField] private float angleJitter = 5f;
    private int currentSpawnIndex = 0;

    [Header("기본 난이도")]
    [Tooltip("초기 스폰 주기 (초)")]
    [SerializeField] private float startSpawnInterval = 2.0f;

    [Tooltip("초기 한 번에 스폰할 마리 수")]
    [SerializeField] private int startSpawnCount = 1;

    [Header("동적 난이도 스케일링")]
    [SerializeField] private float minSpawnIntervalLimit = 0.3f; // 최소 스폰 주기
    [SerializeField] private float intervalReductionPerMinute = 0.2f; // 분당 스폰 주기 감소량
    [SerializeField] private int maxSpawnCountBatch = 10; // 최대 동시 스폰 개수
    [SerializeField] private float countIncreasePerMinute = 1.0f; // 분당 스폰 개수 증가량 - 1이면 1분마다 1마리씩 더 나옴
    [SerializeField] private int maxActiveEnemies = 500; // 최대 활성 가능한 적 수

    [SerializeField, ReadOnly] private float currentInterval;
    [SerializeField, ReadOnly] private int currentBatchCount;
    [SerializeField, ReadOnly] private float currentPlayTime = 0f;

    [Header("스폰 설정")]
    [Tooltip("배치 스폰 시, 마리당 소환 간격 (초). 0이면 즉시 소환.")]
    [SerializeField] private float spawnDelayInBatch = 0.1f;




    private Transform playerTransform;
    private float currentSpawnTimer = 0f;

    private void Start()
    {
        if (PlayerManager.Instance != null)
            playerTransform = PlayerManager.Instance.transform;
        
        if (playTime == null)
            playTime = FindAnyObjectByType<PlayTimeUIManager>();

        currentInterval = startSpawnInterval;
        currentBatchCount = startSpawnCount;
    }

    private void Update()
    {
        if (playerTransform == null) return;

        if (AddressablePoolManager.Instance == null || !AddressablePoolManager.Instance.IsReady)
        {
            return;
        }

        /*if (playTime != null)
        {
            currentPlayTime = playTime.CurrentPlayTime;
            CalculateDifficulty(); // 난이도 갱신
        }*/

        SpawnRoutine();
    }
    private void CalculateDifficulty()
    {
        // 분 단위 계산
        float minutesPassed = currentPlayTime / 60f;

        // 주기 감소: 시작 주기 - (지난 분 * 감소량)
        float newInterval = startSpawnInterval - (minutesPassed * intervalReductionPerMinute);
        currentInterval = Mathf.Max(minSpawnIntervalLimit, newInterval); // 하한선 체크

        // 개수 증가: 시작 개수 + (지난 분 * 증가량)
        int newCount = startSpawnCount + Mathf.FloorToInt(minutesPassed * countIncreasePerMinute);
        currentBatchCount = Mathf.Min(maxSpawnCountBatch, newCount); // 상한선 체크
    }

    private void SpawnRoutine()
    {
        currentSpawnTimer += Time.deltaTime;

        if (currentSpawnTimer >= currentInterval)
        {
            currentSpawnTimer = 0f;
            SpawnBatchAsync().Forget();
        }
    }
    private async UniTaskVoid SpawnBatchAsync()
    {
        // 현재 프레임에서 소환해야 할 마리 수 확정
        int batchCount = currentBatchCount;

        if (AddressablePoolManager.Instance == null) return;

        for (int i = 0; i < batchCount; i++)
        {
            // 1. 최대 마리수 체크 (게임 터짐 방지)
            if (EnemyManager.Instance != null &&
                EnemyManager.Instance.ActiveEnemies.Count >= maxActiveEnemies)
                break;

            // 2. 위치 계산 (여기서 Index가 1씩 증가해야 함!)
            Vector3 spawnPos = GetClockwiseSpawnPoint(playerTransform.position, maxSpawnRadius);

            // 3. 적 소환
            SpawnEnemy(spawnPos);

            if (spawnDelayInBatch > 0f && i < batchCount - 1)
            {
                await UniTask.Delay(
                    System.TimeSpan.FromSeconds(spawnDelayInBatch),
                    cancellationToken: this.GetCancellationTokenOnDestroy()
                );
            }
        }
    }

    private void SpawnEnemy(Vector3 position)
    {
        EnemyData selectedData = GetWeightedRandomEnemy();

        if (selectedData == null)
        {
            Debug.LogWarning("스폰할 적 데이터가 없습니다. PoolManager의 리스트를 확인하세요.");
            return;
        }

        AddressablePoolManager.Instance.GetEnemy(selectedData, position, Quaternion.identity);
    }

    private EnemyData GetWeightedRandomEnemy()
    {
        var spawnList = AddressablePoolManager.Instance.stageEnemies;

        if (spawnList == null || spawnList.Count == 0) return null;

        int totalWeight = 0;
        foreach (var item in spawnList)
        {
            totalWeight += item.weight;
        }

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var item in spawnList)
        {
            currentWeight += item.weight;
            if (randomValue < currentWeight)
            {
                return item.data;
            }
        }

        return spawnList[0].data;
    }

    Vector3 GetClockwiseSpawnPoint(Vector3 center, float radius)
    {
        float angleStep = 360f / spawnDivisions;
        float currentAngleDeg = currentSpawnIndex * angleStep;
        float jitter = Random.Range(-angleJitter, angleJitter);
        float finalAngleDeg = currentAngleDeg + jitter;
        float angleRad = finalAngleDeg * Mathf.Deg2Rad;
        float x = Mathf.Cos(angleRad) * radius;
        float z = Mathf.Sin(angleRad) * radius;

        currentSpawnIndex = (currentSpawnIndex + 1) % spawnDivisions;

        return new Vector3(center.x + x, 0, center.z + z);
    }

    // 테두리 찾기
    Vector3 GetRandomPointOnCircleEdge(Vector3 center, float radius)
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;
        return new Vector3(center.x + x, 0, center.z + z);
    }
}