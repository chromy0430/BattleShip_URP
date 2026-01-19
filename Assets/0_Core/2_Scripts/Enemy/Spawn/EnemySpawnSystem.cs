using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemySpawnSystem : MonoBehaviour
{
    [SerializeField] private PlayTimeUIManager playTime;

    [Header("Spawn Settings")]
    [Tooltip("테스트용 적 프리팹 (나중에 SO 데이터로 교체 예정)")]
    [SerializeField] private GameObject testEnemyPrefab;

    [Tooltip("적이 스폰될 반경")]
    [SerializeField] private float maxSpawnRadius = 25f;

    [Header("Base Difficulty")]
    [Tooltip("초기 스폰 주기 (초)")]
    [SerializeField] private float startSpawnInterval = 2.0f;

    [Tooltip("초기 한 번에 스폰할 마리 수")]
    [SerializeField] private int startSpawnCount = 1;

    [Header("Dynamic Difficulty (Scaling)")]
    [Tooltip("최소 스폰 주기 (이 이하로는 안 줄어듦)")]
    [SerializeField] private float minSpawnIntervalLimit = 0.1f;

    [Tooltip("분당 스폰 주기 감소량 (예: 0.1이면 1분마다 0.1초씩 빨라짐)")]
    [SerializeField] private float intervalReductionPerMinute = 0.2f;

    [Tooltip("최대 동시 스폰 개수 (한 번 틱에 이 이상은 안 나옴)")]
    [SerializeField] private int maxSpawnCountBatch = 10;

    [Tooltip("분당 스폰 개수 증가량 (예: 1이면 1분마다 1마리씩 더 나옴)")]
    [SerializeField] private float countIncreasePerMinute = 1.0f;

    [Tooltip("최대 활성 적 수")]
    [SerializeField] private int maxActiveEnemies = 500; // 테스트를 위해 100으로 설정

    [SerializeField, ReadOnly] private float currentInterval;
    [SerializeField, ReadOnly] private int currentBatchCount;
    [SerializeField, ReadOnly] private float currentPlayTime = 0f;    

    [Header("Debug")]
    [SerializeField] private bool showSpawnRadius = true;

    private Transform playerTransform;
    private float currentSpawnTimer = 0f;

    private void Start()
    {
        if (PlayerManager.Instance != null)
        {
            playerTransform = PlayerManager.Instance.transform;
        }

        if (playTime == null)
            playTime = FindAnyObjectByType<PlayTimeUIManager>();

        currentInterval = startSpawnInterval;
        currentBatchCount = startSpawnCount;
    }

    private void Update()
    {
        if (playerTransform == null) return;

        if (playTime != null)
        {
            currentPlayTime = playTime.CurrentPlayTime;
            CalculateDifficulty(); // 난이도 갱신
        }

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
            for (int i = 0; i < currentBatchCount; i++)
            {
                if (EnemyManager.Instance != null &&
                    EnemyManager.Instance.ActiveEnemies.Count >= maxActiveEnemies)
                    break;

                float angleStep = (Mathf.PI * 2f) / currentBatchCount;
                float randomStartAngle = Random.Range(0f, Mathf.PI * 2f);

                Vector3 basePos = CalculatePositionFromAngle
                                    (
                                        playerTransform.position,
                                        maxSpawnRadius,
                                        randomStartAngle
                                    );

                Vector3 offset = new Vector3
                (
                    Random.Range(-1.5f, 1.5f),
                    0,
                    Random.Range(-1.5f, 1.5f)
                );

                SpawnEnemy(basePos + offset);
            }

            currentSpawnTimer = 0f;
        }
    }

    Vector3 CalculatePositionFromAngle(Vector3 center, float radius, float angle)
    {
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;
        return new Vector3(center.x + x, 0, center.z + z);
    }

    private void SpawnEnemy(Vector3 position)
    {
        GameObject enemyObj = ObjectPoolManager.Instance.Get(testEnemyPrefab);

        if (enemyObj != null)
        {
            enemyObj.transform.position = position;
            enemyObj.transform.rotation = Quaternion.identity;
        }
    }

    /*private void SpawnEnemy()
    {
        Vector3 spawnPosition = GetRandomPointOnCircleEdge(playerTransform.position, maxSpawnRadius); //GetRandomSpawnPosition();

        GameObject enemyObj = ObjectPoolManager.Instance.Get(testEnemyPrefab);

        if (enemyObj != null)
        {
            // 위치 및 회전 설정
            enemyObj.transform.position = spawnPosition;
            enemyObj.transform.rotation = Quaternion.identity;

        }
    }*/

    // 테두리 찾기
    Vector3 GetRandomPointOnCircleEdge(Vector3 center, float radius)
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;
        return new Vector3(center.x + x, 0, center.z + z);
    }

    // 에디터에서 스폰 범위를 눈으로 확인하기 위한 기즈모
    private void OnDrawGizmos()
    {
        if (!showSpawnRadius || playerTransform == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerTransform.position, maxSpawnRadius);
    }
}