using BattleShip;
using StylizedWater3;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBatchManager : Singleton<EnemyBatchManager>
{
    private readonly List<EnemyChaser> _aliveEnemies = new List<EnemyChaser>(1000);    
    private readonly Dictionary<Vector2Int, List<EnemyChaser>> _spatialGrid = new Dictionary<Vector2Int, List<EnemyChaser>>(512);

    [SerializeField] private Transform _playerTransform;

    [Header("부력 관련")]
    [SerializeField] private HeightQuerySystem.Interface heightInterface;
    [SerializeField] private WaveProfile waveProfile;
    private HeightQuerySystem.Sampler _samplerHigh;
    private HeightQuerySystem.Sampler _samplerLow;

    [Header("최적화 - 거리 LOD")]
    [SerializeField] private float highQualityDistSqr = 30f * 30f;
    [SerializeField] private float mediumQualityDistSqr = 60f * 60f;
    [SerializeField, Min(1)] private int nearUpdateInterval = 1;
    [SerializeField, Min(1)] private int mediumUpdateInterval = 2;
    [SerializeField, Min(1)] private int farUpdateInterval = 4;

    [Header("보이드 알고리즘")]
    [SerializeField, Range(0.1f, 2.0f), Tooltip("배 충돌체 크기 조절 배율 (1.0 = 원본 크기)")]
    private float playerBoxScale = 1.0f;
    [SerializeField, Tooltip("플레이어 배 표면 주변의 AT 필드 두께")]
    private float playerBoxBuffer = 1.0f;
    [SerializeField, Tooltip("배의 가장 긴 길이의 절반 (Size Z 9.2의 절반인 4.6 권장)")]
    private float physicalRadius = 4.6f;
    [SerializeField, Tooltip("부드럽게 밀어내기 시작하는 거리 (12 유지)")]
    private float separationRadius = 12f;
    [SerializeField, Tooltip("소프트 분리 힘 (5 유지)")]
    private float separationWeight = 5f;
    [SerializeField, Tooltip("물리 반경 침범 시 튕겨내는 절대 힘 (20 권장)")]
    private float hardCollisionWeight = 20f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float waterHeightLerpSpeed = 10f;

    private float _gridCellSize;
    private int _fixedTick;

    private Vector3 _dynamicPlayerBoxSize = new Vector3(4.0f, 2.0f, 9.2f);
    private Vector3 _dynamicPlayerBoxCenter = Vector3.zero;
    private float _dynamicPlayerBoxBuffer = 1.0f;

    protected override void Awake()
    {
        base.Awake();

        _samplerHigh = new HeightQuerySystem.Sampler();
        _samplerHigh.SetSampleCount(4);

        _samplerLow = new HeightQuerySystem.Sampler();
        _samplerLow.SetSampleCount(1);

        if (heightInterface == null) heightInterface = new HeightQuerySystem.Interface();
        if (waveProfile != null) heightInterface.waveProfile = waveProfile;

        _gridCellSize = Mathf.Max(1f, separationRadius);
    }

    private void Start()
    {
        if (PlayerManager.Instance != null)
        {
            _playerTransform = PlayerManager.Instance.PlayerTransform;
        }

        if (PlayerDataLoad.Instance != null)
        {
            UpdatePlayerBoxSize();
            PlayerDataLoad.Instance.OnStatUpdated += UpdatePlayerBoxSize;
        }
    }

    private void OnDestroy()
    {
        _samplerHigh?.Dispose();
        _samplerLow?.Dispose();

        if (PlayerDataLoad.Instance != null)
        {
            PlayerDataLoad.Instance.OnStatUpdated -= UpdatePlayerBoxSize;
        }
    }

    #region 등록
    private void UpdatePlayerBoxSize()
    {
        if (PlayerDataLoad.Instance != null && PlayerDataLoad.Instance.CurrentPlayerStat != null)
        {
            _dynamicPlayerBoxSize = PlayerDataLoad.Instance.CurrentPlayerStat.shipColliderSize * playerBoxScale;
            _dynamicPlayerBoxCenter = PlayerDataLoad.Instance.CurrentPlayerStat.shipColliderCenter;
            _dynamicPlayerBoxBuffer = playerBoxBuffer * playerBoxScale;
        }
    }

    public void Register(EnemyChaser enemy)
    {
        if (enemy != null && !_aliveEnemies.Contains(enemy))
        {
            _aliveEnemies.Add(enemy);
        }
    }

    public void Unregister(EnemyChaser enemy)
    {
        if (enemy == null) return;

        int index = _aliveEnemies.IndexOf(enemy);
        if (index >= 0)
        {
            int lastIndex = _aliveEnemies.Count - 1;
            if (index != lastIndex)
            {
                _aliveEnemies[index] = _aliveEnemies[lastIndex];
            }
            _aliveEnemies.RemoveAt(lastIndex);
        }
    }
    #endregion

    private void Update()
    {
        if (_playerTransform == null && PlayerManager.Instance != null)
        {
            _playerTransform = PlayerManager.Instance.PlayerTransform;
        }
        if (_playerTransform == null || _aliveEnemies.Count == 0) return;

        float deltaTime = Time.deltaTime;
        Vector3 playerPos = _playerTransform.position;

        heightInterface.GetWaterObject(playerPos);
        RebuildSpatialGrid();

        for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
        {
            EnemyChaser enemy = _aliveEnemies[i];
            if (enemy == null || enemy.status == null || !enemy.status.IsAlive)
            {
                Unregister(enemy);
                continue;
            }

            if (enemy.status.IsKnockbacked) continue;

            Vector3 currentPos = enemy.cachedTransform.position;
            float distSqr = (playerPos - currentPos).sqrMagnitude;
            bool isHighQualityWater = distSqr < highQualityDistSqr;

            int updateInterval = GetUpdateInterval(distSqr);
            if (((_fixedTick + i) % updateInterval) == 0)
            {
                // updateInterval만큼의 시간이 곱해져야 가속도가 정상적으로 적용됨
                float lodDeltaTime = deltaTime * updateInterval;
                ApplySwarmMovement(enemy, _playerTransform, currentPos, lodDeltaTime, Mathf.Sqrt(distSqr));
                ApplyRotation(enemy, playerPos, currentPos, lodDeltaTime);
            }

            enemy.cachedTransform.position += enemy.currentVelocity * deltaTime;
            enemy.cachedWaterY = CalculateWaterHeight(enemy, enemy.cachedTransform.position, isHighQualityWater);
            StabilizeVerticalPosition(enemy, enemy.cachedWaterY, deltaTime);
        }

        _fixedTick++;
    }

    #region 보이드 관련
    private void ApplySwarmMovement(EnemyChaser enemy, Transform playerTransform, Vector3 currentPos, float lodDeltaTime, float distToPlayer)
    {
        Vector3 separationDir = CalculateSeparationVector(enemy, currentPos, playerTransform);

        float separationPressure = separationDir.magnitude;
        float pursuitMultiplier = Mathf.Clamp01(1f - (separationPressure / hardCollisionWeight));

        Vector3 pursuitDir = Vector3.zero;
        if (distToPlayer > enemy.stopDistance)
        {
            pursuitDir = (playerTransform.position - currentPos).normalized * pursuitMultiplier;
        }

        Vector3 desiredDir = (pursuitDir + separationDir).normalized;
        Vector3 targetVelocity = desiredDir * enemy.moveSpeed;

        Vector3 currentVel = enemy.currentVelocity;
        Vector3 newVelXz = Vector3.MoveTowards(
            new Vector3(currentVel.x, 0f, currentVel.z),
            new Vector3(targetVelocity.x, 0f, targetVelocity.z),
            acceleration * lodDeltaTime // 최적화된 시간을 곱해 보정
        );

        enemy.currentVelocity = new Vector3(newVelXz.x, 0f, newVelXz.z);
    }

    private Vector3 CalculateSeparationVector(EnemyChaser self, Vector3 selfPos, Transform playerTransform)
    {
        Vector2Int centerCell = WorldToGrid(selfPos);
        Vector3 softSeparation = Vector3.zero;
        Vector3 hardSeparation = Vector3.zero;

        float softRadiusSqr = separationRadius * separationRadius;
        float hardRadius = physicalRadius * 2f; // 배 두 대가 닿는 물리적 최소 거리
        float hardRadiusSqr = hardRadius * hardRadius;

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                Vector2Int key = new Vector2Int(centerCell.x + x, centerCell.y + z);
                if (!_spatialGrid.TryGetValue(key, out List<EnemyChaser> bucket)) continue;

                for (int i = 0; i < bucket.Count; i++)
                {
                    EnemyChaser other = bucket[i];
                    if (other == self || other == null || other.status == null || !other.status.IsAlive) continue;

                    Vector3 diff = selfPos - other.cachedTransform.position;
                    diff.y = 0f;
                    float distSqr = diff.sqrMagnitude;

                    if (distSqr < softRadiusSqr)
                    {
                        if (distSqr <= 0.0001f)
                        {
                            // 완벽히 겹쳤을 때 (Singularity 방어) -> 하드 충돌로 분류
                            hardSeparation += GetDeterministicEscapeDirection(self, other) * hardCollisionWeight;
                        }
                        else
                        {
                            float dist = Mathf.Sqrt(distSqr);
                            Vector3 dir = diff / dist;

                            if (distSqr < hardRadiusSqr)
                            {
                                // [하드 충돌] 선체가 겹치기 시작함 -> 거리에 반비례하여 기하급수적으로 밀어냄
                                float penetration = hardRadius - dist;
                                float pushForce = (penetration / hardRadius) * hardCollisionWeight;
                                hardSeparation += dir * pushForce;
                            }
                            else
                            {
                                // [소프트 충돌] separationRadius 안에는 들어왔으나 선체는 닿지 않음 -> 부드럽게 밀어냄
                                float softPushForce = (1f - (dist / separationRadius)) * separationWeight;
                                softSeparation += dir * softPushForce;
                            }
                        }
                    }
                }
            }
        }

        // 2. 플레이어와의 하드 충돌 방어 (OBB 기반 3D 직사각형 필드)
        Vector3 localPos = playerTransform.InverseTransformPoint(selfPos);
        Vector3 centeredLocalPos = localPos - _dynamicPlayerBoxCenter;
        centeredLocalPos.y = 0f;

        float extX = _dynamicPlayerBoxSize.x * 0.5f;
        float extZ = _dynamicPlayerBoxSize.z * 0.5f;

        Vector3 closestLocal = new Vector3(
            Mathf.Clamp(centeredLocalPos.x, -extX, extX),
            0f,
            Mathf.Clamp(centeredLocalPos.z, -extZ, extZ)
        );

        Vector3 localDiff = centeredLocalPos - closestLocal;
        float distToBoxSqr = localDiff.sqrMagnitude;

        // [수정 핵심] 동적 버퍼 적용
        float bufferSqr = _dynamicPlayerBoxBuffer * _dynamicPlayerBoxBuffer;

        if (distToBoxSqr < bufferSqr)
        {
            float distToBox = Mathf.Sqrt(distToBoxSqr);
            Vector3 pushLocalDir;
            float penetration;

            if (distToBox < 0.0001f)
            {
                float distToEdgeX = extX - Mathf.Abs(centeredLocalPos.x);
                float distToEdgeZ = extZ - Mathf.Abs(centeredLocalPos.z);

                if (distToEdgeX < distToEdgeZ)
                {
                    pushLocalDir = new Vector3(Mathf.Sign(centeredLocalPos.x), 0, 0);
                    penetration = distToEdgeX + _dynamicPlayerBoxBuffer; // 동적 버퍼 적용
                }
                else
                {
                    pushLocalDir = new Vector3(0, 0, Mathf.Sign(centeredLocalPos.z));
                    penetration = distToEdgeZ + _dynamicPlayerBoxBuffer; // 동적 버퍼 적용
                }
            }
            else
            {
                pushLocalDir = localDiff / distToBox;
                penetration = _dynamicPlayerBoxBuffer - distToBox; // 동적 버퍼 적용
            }

            Vector3 pushWorldDir = playerTransform.TransformDirection(pushLocalDir);

            // [수정 핵심] 힘을 계산할 때도 동적 버퍼로 나눔
            float pushForce = (penetration / _dynamicPlayerBoxBuffer) * hardCollisionWeight * 3.0f;
            hardSeparation += pushWorldDir * pushForce;
        }

        Vector3 totalSeparation = softSeparation + hardSeparation;

        float maxAllowedForce = hardCollisionWeight * 1.5f;
        if (totalSeparation.sqrMagnitude > maxAllowedForce * maxAllowedForce)
        {
            totalSeparation = totalSeparation.normalized * maxAllowedForce;
        }

        return totalSeparation;
    }

    // ID 기반의 결정론적 분리 방향 생성 (겹침 블랙홀 완벽 방어)
    private Vector3 GetDeterministicEscapeDirection(EnemyChaser a, EnemyChaser b)
    {
        int seedA = a.GetInstanceID();
        int seedB = b.GetInstanceID();
        int hash = (seedA * 73856) ^ (seedB * 19349);
        float ang = ((hash & 1023) / 1024f) * Mathf.PI * 2f;
        return new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
    }
    #endregion

    #region 그리드 관련
    private void RebuildSpatialGrid()
    {
        foreach (var bucket in _spatialGrid.Values) bucket.Clear();

        for (int i = 0; i < _aliveEnemies.Count; i++)
        {
            EnemyChaser enemy = _aliveEnemies[i];
            Vector2Int key = WorldToGrid(enemy.cachedTransform.position);

            if (!_spatialGrid.TryGetValue(key, out List<EnemyChaser> bucket))
            {
                bucket = new List<EnemyChaser>(16);
                _spatialGrid.Add(key, bucket);
            }
            bucket.Add(enemy);
        }
    }

    private Vector2Int WorldToGrid(Vector3 pos)
    {
        float inv = 1f / Mathf.Max(0.0001f, _gridCellSize);
        return new Vector2Int(Mathf.FloorToInt(pos.x * inv), Mathf.FloorToInt(pos.z * inv));
    }

    private int GetUpdateInterval(float distSqr)
    {
        if (distSqr < highQualityDistSqr) return nearUpdateInterval;
        if (distSqr < mediumQualityDistSqr) return mediumUpdateInterval;
        return farUpdateInterval;
    }

    private void ApplyRotation(EnemyChaser enemy, Vector3 playerPos, Vector3 currentPos, float deltaTime)
    {
        Vector3 moveDir = playerPos - currentPos;
        moveDir.y = 0f;
        if (moveDir.sqrMagnitude <= 0.001f) return;

        Quaternion lookRot = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
        // [수정 핵심] rb.MoveRotation 대신 Transform.rotation Slerp 사용
        enemy.cachedTransform.rotation = Quaternion.Slerp(enemy.cachedTransform.rotation, lookRot, deltaTime * enemy.turnSpeed);
    }

    private void StabilizeVerticalPosition(EnemyChaser enemy, float targetY, float deltaTime)
    {
        // [수정 핵심] rb.position 대신 Transform.position 조작
        Vector3 pos = enemy.cachedTransform.position;
        float nextY = Mathf.Lerp(pos.y, targetY, deltaTime * waterHeightLerpSpeed);
        pos.y = nextY;
        enemy.cachedTransform.position = pos;
    }
    private float CalculateWaterHeight(EnemyChaser enemy, Vector3 pos, bool highQuality)
    {
        HeightQuerySystem.Sampler sampler = highQuality ? _samplerHigh : _samplerLow;
        if (highQuality)
        {
            float half = enemy.surfaceSize * 0.5f;
            sampler.SetSamplePosition(0, pos + new Vector3(-half, 0f, 0f));
            sampler.SetSamplePosition(1, pos + new Vector3(half, 0f, 0f));
            sampler.SetSamplePosition(2, pos + new Vector3(0f, 0f, -half));
            sampler.SetSamplePosition(3, pos + new Vector3(0f, 0f, half));
        }
        else sampler.SetSamplePosition(0, pos);

        Gerstner.ComputeHeight(sampler, heightInterface);

        if (highQuality) return ((sampler.heightValues[0] + sampler.heightValues[1] + sampler.heightValues[2] + sampler.heightValues[3]) * 0.25f) + enemy.heightOffset;
        return sampler.heightValues[0] + enemy.heightOffset;
    }
    #endregion

    #region 투사체용 공간 탐색
    /// <summary>
    /// 특정 좌표 반경 내에 있는 가장 가까운 적을 반환합니다. (물리 콜라이더 완벽 대체)
    /// </summary>
    public EnemyChaser GetClosestEnemyInRadius(Vector3 position, float radius)
    {
        float radiusSqr = radius * radius;
        EnemyChaser closest = null;
        float minSqr = float.MaxValue;

        Vector2Int centerCell = WorldToGrid(position);
        // radius를 그리드 크기로 나누어 탐색할 주변 셀 범위(보통 1~2)를 구함
        int range = Mathf.CeilToInt(radius / Mathf.Max(0.0001f, _gridCellSize));

        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                Vector2Int key = new Vector2Int(centerCell.x + x, centerCell.y + z);
                if (!_spatialGrid.TryGetValue(key, out List<EnemyChaser> bucket)) continue;

                for (int i = 0; i < bucket.Count; i++)
                {
                    EnemyChaser enemy = bucket[i];
                    if (enemy == null || enemy.status == null || !enemy.status.IsAlive) continue;

                    float sqr = (enemy.cachedTransform.position - position).sqrMagnitude;
                    if (sqr <= radiusSqr && sqr < minSqr)
                    {
                        minSqr = sqr;
                        closest = enemy;
                    }
                }
            }
        }
        return closest;
    }
    #endregion

    #region 광역 탐색 
    /// <summary>
    /// 반경 내의 모든 적을 탐색하여 결과 리스트에 담아 반환합니다. (GC 발생 0)
    /// </summary>
    public void GetEnemiesInRadius(Vector3 position, float radius, List<EnemyChaser> results)
    {
        results.Clear();
        float radiusSqr = radius * radius;
        Vector2Int centerCell = WorldToGrid(position);
        int range = Mathf.CeilToInt(radius / Mathf.Max(0.0001f, _gridCellSize));

        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                Vector2Int key = new Vector2Int(centerCell.x + x, centerCell.y + z);
                if (!_spatialGrid.TryGetValue(key, out List<EnemyChaser> bucket)) continue;

                for (int i = 0; i < bucket.Count; i++)
                {
                    EnemyChaser enemy = bucket[i];
                    if (enemy == null || enemy.status == null || !enemy.status.IsAlive) continue;

                    float sqr = (enemy.cachedTransform.position - position).sqrMagnitude;
                    if (sqr <= radiusSqr)
                    {
                        results.Add(enemy);
                    }
                }
            }
        }
    }
    #endregion

    #region 시각적 디버깅 (Gizmos)
    private void OnDrawGizmos()
    {
        if (_playerTransform == null || !Application.isPlaying) return;

        Vector3 worldCenter = _playerTransform.TransformPoint(_dynamicPlayerBoxCenter);
        Gizmos.matrix = Matrix4x4.TRS(worldCenter, _playerTransform.rotation, Vector3.one);

        // 1. 선체 물리 사이즈 (빨간색 3D 큐브)
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Vector3.zero, _dynamicPlayerBoxSize);

        // 2. 완충 지대 (노란색 3D 큐브)
        Gizmos.color = Color.yellow;
        Vector3 bufferSize = new Vector3(
            _dynamicPlayerBoxSize.x + (_dynamicPlayerBoxBuffer * 2), // 동적 버퍼 두 배 합산
            _dynamicPlayerBoxSize.y,
            _dynamicPlayerBoxSize.z + (_dynamicPlayerBoxBuffer * 2)  // 동적 버퍼 두 배 합산
        );
        Gizmos.DrawWireCube(Vector3.zero, bufferSize);

        Gizmos.matrix = Matrix4x4.identity;
    }
    #endregion
}