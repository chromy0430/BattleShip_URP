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
    }

    private void OnDestroy()
    {
        _samplerHigh?.Dispose();
        _samplerLow?.Dispose();
    }

    #region 등록
    public void Register(EnemyChaser enemy)
    {
        if (enemy != null && !_aliveEnemies.Contains(enemy))
        {
            _aliveEnemies.Add(enemy);
            IgnoreEnemyCollisions(enemy);
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

    private void IgnoreEnemyCollisions(EnemyChaser newEnemy)
    {
        if (newEnemy.bodyCollider == null) return;
        for (int i = 0; i < _aliveEnemies.Count; i++)
        {
            EnemyChaser other = _aliveEnemies[i];
            if (other == null || other == newEnemy || other.bodyCollider == null) continue;
            Physics.IgnoreCollision(newEnemy.bodyCollider, other.bodyCollider, true);
        }
    }
    #endregion

    private void FixedUpdate()
    {
        if (_playerTransform == null && PlayerManager.Instance != null)
        {
            _playerTransform = PlayerManager.Instance.PlayerTransform;
        }
        if (_playerTransform == null || _aliveEnemies.Count == 0) return;

        float deltaTime = Time.fixedDeltaTime;
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

            int updateInterval = GetUpdateInterval(distSqr);
            if (((_fixedTick + i) % updateInterval) != 0)
            {
                StabilizeVerticalPosition(enemy, enemy.cachedWaterY, deltaTime);
                continue;
            }

            // 부력 계산
            bool isHighQualityWater = distSqr < highQualityDistSqr;
            enemy.cachedWaterY = CalculateWaterHeight(enemy, currentPos, isHighQualityWater);

            // 스웜 로직 적용
            ApplySwarmMovement(enemy, playerPos, currentPos, deltaTime, Mathf.Sqrt(distSqr));
            ApplyRotation(enemy, playerPos, currentPos, deltaTime);
        }

        _fixedTick++;
    }

    #region 보이드 관련
    private void ApplySwarmMovement(EnemyChaser enemy, Vector3 playerPos, Vector3 currentPos, float deltaTime, float distToPlayer)
    {
        Vector3 separationDir = CalculateSeparationVector(enemy, currentPos);

        // [핵심 1: Traffic Jam (교통 체증) 로직]
        // 사방이 막혀서 분리 압력(separationDir의 크기)이 높아지면, 플레이어를 향한 돌진(Pursuit)을 멈춘다.
        float separationPressure = separationDir.magnitude;
        float pursuitMultiplier = Mathf.Clamp01(1f - (separationPressure / hardCollisionWeight));

        Vector3 pursuitDir = Vector3.zero;
        if (distToPlayer > enemy.stopDistance)
        {
            // 막혀있으면 pursuitMultiplier가 0에 가까워져 전진하지 않음
            pursuitDir = (playerPos - currentPos).normalized * pursuitMultiplier;
        }

        // [핵심 2: 벡터 합성]
        // 분리 벡터(separationDir)는 이미 가중치가 곱해져 반환되므로 바로 더해줍니다.
        Vector3 desiredDir = (pursuitDir + separationDir).normalized;

        Vector3 targetVelocity = desiredDir * enemy.moveSpeed;

        // 관성을 가진 부드러운 가속 (Jitter 방지)
        Vector3 currentVel = enemy.rb.linearVelocity;
        Vector3 newVelXz = Vector3.MoveTowards(
            new Vector3(currentVel.x, 0f, currentVel.z),
            new Vector3(targetVelocity.x, 0f, targetVelocity.z),
            acceleration * deltaTime
        );

        enemy.rb.linearVelocity = new Vector3(newVelXz.x, 0f, newVelXz.z);
        StabilizeVerticalPosition(enemy, enemy.cachedWaterY, deltaTime);
    }

    private Vector3 CalculateSeparationVector(EnemyChaser self, Vector3 selfPos)
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
                    if (other == self) continue;

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

        // [핵심 3: 절대 평균(Average) 내지 않음]
        // 평균을 내면 사방에서 밀 때 0이 되어 압사당함. 힘을 전부 더해서 압력을 누적시킴.
        Vector3 totalSeparation = softSeparation + hardSeparation;

        // 물리 엔진이 폭발하는 것을 막기 위해 최대 압력 한계선(Cap) 설정
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
        enemy.rb.MoveRotation(Quaternion.Slerp(enemy.rb.rotation, lookRot, deltaTime * enemy.turnSpeed));
    }

    private void StabilizeVerticalPosition(EnemyChaser enemy, float targetY, float deltaTime)
    {
        Vector3 rbPos = enemy.rb.position;
        float nextY = Mathf.Lerp(rbPos.y, targetY, deltaTime * waterHeightLerpSpeed);
        rbPos.y = nextY;
        enemy.rb.MovePosition(rbPos);
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
}