using System.Collections.Generic;
using StylizedWater3;
using Unity.Profiling;
using UnityEngine;

public class EnemyBatchManager : MonoBehaviour
{
    public static EnemyBatchManager Instance { get; private set; }

    private readonly List<EnemyChaser> _enemies = new List<EnemyChaser>(1000);
    private readonly List<EnemyChaser> _aliveEnemies = new List<EnemyChaser>(1000);
    private readonly Dictionary<Vector2Int, List<EnemyChaser>> _spatialGrid = new Dictionary<Vector2Int, List<EnemyChaser>>(512);

    [SerializeField] private Transform _playerTransform;

    [Header("Buoyancy")]
    [SerializeField] private HeightQuerySystem.Interface heightInterface;
    [SerializeField] private WaveProfile waveProfile;
    private HeightQuerySystem.Sampler _samplerHigh;
    private HeightQuerySystem.Sampler _samplerLow;

    [Header("Distance LOD")]
    [SerializeField] private float highQualityDistSqr = 30f * 30f;
    [SerializeField] private float mediumQualityDistSqr = 60f * 60f;

    [Header("Time Slicing")]
    [SerializeField, Min(1)] private int nearUpdateInterval = 1;
    [SerializeField, Min(1)] private int mediumUpdateInterval = 2;
    [SerializeField, Min(1)] private int farUpdateInterval = 4;

    [Header("Vertical Stabilization")]
    [SerializeField] private float verticalMoveThreshold = 0.01f;
    [SerializeField] private float waterHeightLerpSpeed = 5f;

    [Header("Soft Separation")]
    [SerializeField] private bool enableSeparation = true;
    [SerializeField] private float separationRadius = 2.2f;
    [SerializeField] private float separationStrength = 1.1f;
    [SerializeField, Min(1)] private int maxSeparationNeighbors = 8;

    [Header("Formation")]
    [SerializeField] private float fallbackPlayerRadius = 3.5f;
    [SerializeField] private float fallbackEnemyRadius = 0.8f;
    [SerializeField] private float ringPadding = 0.5f;
    [SerializeField] private float slotInfluenceDistance = 16f;
    [SerializeField] private float orbitAngularSpeed = 20f;
    [SerializeField] private float orbitSpeedMultiplier = 0.85f;
    [SerializeField] private float radialCorrectionStrength = 1.25f;
    [SerializeField, Min(1)] private int maxRingCount = 10;
    [SerializeField] private float ringSpacing = 2f;
    [SerializeField] private float minSlotSpacing = 1.6f;

    [Header("Hard Separation")]
    [SerializeField] private float hardSeparationStrength = 2f;
    [SerializeField] private float hardSeparationSpeed = 12f;

    [Header("Crowd Slowdown")]
    [SerializeField] private float crowdSlowdownStrength = 0.45f;
    [SerializeField, Min(1)] private int crowdReferenceNeighbors = 8;

    [Header("Runtime Metrics")]
    [SerializeField] private bool collectCrowdMetrics = false;
    [SerializeField, Min(1)] private int metricsUpdateInterval = 5;
    [SerializeField] private int runtimeAliveCount;
    [SerializeField] private int runtimeOverlapPairs;
    [SerializeField] private float runtimeAverageNearestDistance;
    [SerializeField] private float runtimeAverageRequiredDistance;

    [Header("Debug Visualization")]
    [SerializeField] private bool drawDebugGizmos = true;
    [SerializeField] private bool drawAllEnemyDebug = false;
    [SerializeField, Min(1)] private int maxDebugEnemies = 8;
    [SerializeField] private float debugMaxDistance = 35f;
    [SerializeField] private float gizmoHeightOffset = 0.15f;
    [SerializeField] private Color separationRadiusColor = new Color(1f, 0.45f, 0.15f, 1f);
    [SerializeField] private Color engageRadiusColor = new Color(0.2f, 1f, 0.9f, 1f);
    [SerializeField] private Color slotTargetColor = new Color(0.95f, 0.95f, 0.2f, 1f);
    [SerializeField] private Color velocityColor = new Color(0.45f, 0.8f, 1f, 1f);
    [SerializeField] private Color overlapBoundaryColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Header("Overlap Boundary (Debug + Constraint)")]
    [SerializeField] private float overlapBoundaryPadding = 0.2f;

    private const float UPDATE_DISTANCE_THRESHOLD = 10f;
    private Vector3 _lastQueryPosition;
    private int _fixedTick;
    private float _playerHorizontalRadius;

    [Header("Physics")]
    [SerializeField] private float moveSpeedMultiplier = 1f;
    [SerializeField] private float acceleration = 50f;

    private float _frameBaseEngageRadius;
    private float _frameSlotSpacing;
    private int _frameRingCount = 1;
    private float _gridCellSize = 1f;
    private readonly List<int> _ringCapacities = new List<int>(16);
    private readonly List<int> _ringCumulativeCaps = new List<int>(16);
    private int _totalRingCapacity = 1;

    private static readonly ProfilerMarker FixedUpdateMarker = new ProfilerMarker("EnemyBatchManager.FixedUpdate");
    private static readonly ProfilerMarker WaterReferenceMarker = new ProfilerMarker("EnemyBatchManager.UpdateWaterReference");
    private static readonly ProfilerMarker EnemyLoopMarker = new ProfilerMarker("EnemyBatchManager.EnemyLoop");
    private static readonly ProfilerMarker WaterHeightMarker = new ProfilerMarker("EnemyBatchManager.CalculateWaterHeight");
    private static readonly ProfilerMarker MovementMarker = new ProfilerMarker("EnemyBatchManager.ApplyMovement");
    private static readonly ProfilerMarker RotationMarker = new ProfilerMarker("EnemyBatchManager.ApplyRotation");

    private struct AvoidanceResult
    {
        public Vector3 softDirection;
        public Vector3 hardDirection;
        public int neighborCount;
    }

    private void OnValidate()
    {
        if (mediumQualityDistSqr < highQualityDistSqr)
        {
            mediumQualityDistSqr = highQualityDistSqr;
        }

        if (nearUpdateInterval < 1) nearUpdateInterval = 1;
        if (mediumUpdateInterval < 1) mediumUpdateInterval = 1;
        if (farUpdateInterval < 1) farUpdateInterval = 1;

        if (separationRadius < 0f) separationRadius = 0f;
        if (maxSeparationNeighbors < 1) maxSeparationNeighbors = 1;

        if (fallbackPlayerRadius < 0f) fallbackPlayerRadius = 0f;
        if (fallbackEnemyRadius < 0f) fallbackEnemyRadius = 0f;
        if (ringPadding < 0f) ringPadding = 0f;
        if (slotInfluenceDistance < 0.01f) slotInfluenceDistance = 0.01f;
        if (orbitSpeedMultiplier < 0f) orbitSpeedMultiplier = 0f;
        if (radialCorrectionStrength < 0f) radialCorrectionStrength = 0f;
        if (maxRingCount < 1) maxRingCount = 1;
        if (ringSpacing < 0f) ringSpacing = 0f;
        if (minSlotSpacing < 0.1f) minSlotSpacing = 0.1f;

        if (hardSeparationStrength < 0f) hardSeparationStrength = 0f;
        if (hardSeparationSpeed < 0f) hardSeparationSpeed = 0f;

        if (crowdSlowdownStrength < 0f) crowdSlowdownStrength = 0f;
        if (crowdReferenceNeighbors < 1) crowdReferenceNeighbors = 1;

        if (metricsUpdateInterval < 1) metricsUpdateInterval = 1;

        if (maxDebugEnemies < 1) maxDebugEnemies = 1;
        if (debugMaxDistance < 0f) debugMaxDistance = 0f;
        if (overlapBoundaryPadding < 0f) overlapBoundaryPadding = 0f;
    }

    private void Awake()
    {
        Instance = this;

        _samplerHigh = new HeightQuerySystem.Sampler();
        _samplerHigh.SetSampleCount(4);

        _samplerLow = new HeightQuerySystem.Sampler();
        _samplerLow.SetSampleCount(1);

        if (heightInterface == null) heightInterface = new HeightQuerySystem.Interface();
        if (waveProfile != null) heightInterface.waveProfile = waveProfile;

        _lastQueryPosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);
    }

    private void Start()
    {
        if (PlayerManager.Instance != null)
        {
            _playerTransform = PlayerManager.Instance.PlayerTransform;
            CachePlayerHorizontalRadius();
        }
    }

    private void OnDestroy()
    {
        _samplerHigh?.Dispose();
        _samplerLow?.Dispose();
    }

    public void Register(EnemyChaser enemy)
    {
        if (enemy != null && !_enemies.Contains(enemy))
        {
            _enemies.Add(enemy);
            IgnoreEnemyCollisions(enemy);
            if (heightInterface.HasMissingReferences())
            {
                heightInterface.GetWaterObject(enemy.cachedTransform.position);
            }
        }
    }

    public void Unregister(EnemyChaser enemy)
    {
        if (enemy != null)
        {
            _enemies.Remove(enemy);
            _aliveEnemies.Remove(enemy);
        }
    }

    private void IgnoreEnemyCollisions(EnemyChaser newEnemy)
    {
        if (newEnemy.bodyCollider == null) return;

        for (int i = 0; i < _enemies.Count; i++)
        {
            EnemyChaser other = _enemies[i];
            if (other == null || other == newEnemy || other.bodyCollider == null) continue;
            Physics.IgnoreCollision(newEnemy.bodyCollider, other.bodyCollider, true);
        }
    }
    private void FixedUpdate()
    {
        using (FixedUpdateMarker.Auto())
        {
            if (_playerTransform == null && PlayerManager.Instance != null)
            {
                _playerTransform = PlayerManager.Instance.PlayerTransform;
                CachePlayerHorizontalRadius();
            }

            if (_playerTransform == null) return;

            float deltaTime = Time.fixedDeltaTime;
            Vector3 playerPos = _playerTransform.position;

            using (WaterReferenceMarker.Auto())
            {
                if ((playerPos - _lastQueryPosition).sqrMagnitude > UPDATE_DISTANCE_THRESHOLD * UPDATE_DISTANCE_THRESHOLD
                    || heightInterface.HasMissingReferences())
                {
                    heightInterface.GetWaterObject(playerPos);
                    _lastQueryPosition = playerPos;
                }
            }

            CollectAliveEnemies();
            runtimeAliveCount = _aliveEnemies.Count;
            if (_aliveEnemies.Count == 0)
            {
                _fixedTick++;
                return;
            }

            PrepareFormationContext();
            RebuildSpatialGrid();

            if (collectCrowdMetrics && (_fixedTick % metricsUpdateInterval == 0))
            {
                UpdateRuntimeCrowdMetrics();
            }

            using (EnemyLoopMarker.Auto())
            {
                for (int i = 0; i < _aliveEnemies.Count; i++)
                {
                    EnemyChaser enemy = _aliveEnemies[i];
                    if (enemy == null || enemy.status == null) continue;
                    if (enemy.status.IsKnockbacked) continue;

                    Vector3 currentPos = enemy.cachedTransform.position;
                    float distSqr = (playerPos - currentPos).sqrMagnitude;
                    DampenVerticalVelocity(enemy);

                    int updateInterval = GetUpdateInterval(distSqr);
                    if (((_fixedTick + i) % updateInterval) != 0)
                    {
                        StabilizeVerticalPosition(enemy, enemy.cachedWaterY, deltaTime);
                        continue;
                    }

                    bool isHighQualityWater = distSqr < highQualityDistSqr;

                    float targetY;
                    using (WaterHeightMarker.Auto())
                    {
                        targetY = CalculateWaterHeight(enemy, currentPos, isHighQualityWater);
                    }
                    enemy.cachedWaterY = targetY;

                    using (MovementMarker.Auto())
                    {
                        ApplyMovement(enemy, playerPos, currentPos, targetY, deltaTime, distSqr);
                    }

                    using (RotationMarker.Auto())
                    {
                        ApplyRotation(enemy, playerPos, currentPos, deltaTime);
                    }
                }
            }

            _fixedTick++;
        }
    }

    private void CollectAliveEnemies()
    {
        _aliveEnemies.Clear();

        for (int i = 0; i < _enemies.Count; i++)
        {
            EnemyChaser enemy = _enemies[i];
            if (enemy == null || enemy.status == null) continue;
            if (!enemy.status.IsAlive) continue;
            _aliveEnemies.Add(enemy);
        }
    }

    private void PrepareFormationContext()
    {
        float playerRadius = _playerHorizontalRadius > 0f ? _playerHorizontalRadius : fallbackPlayerRadius;
        _frameBaseEngageRadius = playerRadius + fallbackEnemyRadius + ringPadding;

        float overlapDiameter = (fallbackEnemyRadius + overlapBoundaryPadding) * 2f;
        _frameSlotSpacing = Mathf.Max(ringSpacing, Mathf.Max(minSlotSpacing, overlapDiameter));

        _frameRingCount = ComputeRingCount(_aliveEnemies.Count, _frameBaseEngageRadius, _frameSlotSpacing, maxRingCount);
        BuildRingCapacities(_frameRingCount, _frameBaseEngageRadius, _frameSlotSpacing);
        _gridCellSize = Mathf.Max(1f, Mathf.Max(separationRadius, _frameSlotSpacing));
    }

    private int ComputeRingCount(int aliveCount, float baseRadius, float spacing, int maxRings)
    {
        if (aliveCount <= 0) return 1;

        int accumulatedCapacity = 0;
        for (int ring = 0; ring < maxRings; ring++)
        {
            float r = baseRadius + (ring * spacing);
            int cap = Mathf.Max(6, Mathf.FloorToInt((2f * Mathf.PI * r) / Mathf.Max(0.5f, minSlotSpacing)));
            accumulatedCapacity += cap;
            if (accumulatedCapacity >= aliveCount)
            {
                return ring + 1;
            }
        }

        return maxRings;
    }

    private void BuildRingCapacities(int ringCount, float baseRadius, float spacing)
    {
        _ringCapacities.Clear();
        _ringCumulativeCaps.Clear();
        _totalRingCapacity = 0;

        int safeRingCount = Mathf.Max(1, ringCount);
        for (int ring = 0; ring < safeRingCount; ring++)
        {
            float r = baseRadius + (ring * spacing);
            int cap = Mathf.Max(6, Mathf.FloorToInt((2f * Mathf.PI * r) / Mathf.Max(0.5f, minSlotSpacing)));
            _ringCapacities.Add(cap);
            _totalRingCapacity += cap;
            _ringCumulativeCaps.Add(_totalRingCapacity);
        }

        if (_totalRingCapacity <= 0)
        {
            _ringCapacities.Clear();
            _ringCumulativeCaps.Clear();
            _ringCapacities.Add(1);
            _ringCumulativeCaps.Add(1);
            _totalRingCapacity = 1;
        }
    }

    private void RebuildSpatialGrid()
    {
        foreach (List<EnemyChaser> bucket in _spatialGrid.Values)
        {
            bucket.Clear();
        }

        for (int i = 0; i < _aliveEnemies.Count; i++)
        {
            EnemyChaser enemy = _aliveEnemies[i];
            if (enemy == null) continue;

            Vector2Int key = WorldToGrid(enemy.cachedTransform.position);
            if (!_spatialGrid.TryGetValue(key, out List<EnemyChaser> bucket))
            {
                bucket = new List<EnemyChaser>(8);
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
    private void ApplyMovement(EnemyChaser enemy, Vector3 playerPos, Vector3 currentPos, float targetY, float deltaTime, float distSqr)
    {
        Vector3 toPlayer = playerPos - currentPos;
        toPlayer.y = 0f;
        float distToPlayer = toPlayer.magnitude;
        if (distToPlayer <= 0.0001f)
        {
            StabilizeVerticalPosition(enemy, targetY, deltaTime);
            return;
        }

        Vector3 toPlayerDir = toPlayer / distToPlayer;
        float stopDistance = enemy.effectiveStopDistance > 0f ? enemy.effectiveStopDistance : enemy.stopDistance;
        float engageRadius = GetEngageRadius(enemy, stopDistance);
        float engageRadiusSqr = engageRadius * engageRadius;

        Vector3 slotTargetPos = GetSlotTargetPosition(playerPos, enemy, engageRadius);
        Vector3 toSlot = slotTargetPos - currentPos;
        toSlot.y = 0f;
        Vector3 slotDir = toSlot.sqrMagnitude > 0.0001f ? toSlot.normalized : toPlayerDir;

        float slotBlend = Mathf.InverseLerp(engageRadius + slotInfluenceDistance, engageRadius, distToPlayer);
        Vector3 desiredDir = Vector3.Slerp(toPlayerDir, slotDir, slotBlend);

        AvoidanceResult avoidance = EvaluateAvoidance(enemy, currentPos);

        if (avoidance.softDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 blended = desiredDir + (avoidance.softDirection * separationStrength);
            if (blended.sqrMagnitude > 0.0001f)
            {
                desiredDir = blended.normalized;
            }
        }

        Vector3 targetVelocity;
        if (distSqr > engageRadiusSqr)
        {
            targetVelocity = desiredDir * enemy.moveSpeed * moveSpeedMultiplier;
        }
        else
        {
            Vector3 tangent = Vector3.Cross(Vector3.up, toPlayerDir) * enemy.orbitDirectionSign;
            float radialError = engageRadius - distToPlayer;
            Vector3 radialCorrection = -toPlayerDir * (radialError * radialCorrectionStrength);

            Vector3 orbitDir = tangent + radialCorrection;
            if (avoidance.softDirection.sqrMagnitude > 0.0001f)
            {
                orbitDir += avoidance.softDirection * separationStrength;
            }

            if (orbitDir.sqrMagnitude <= 0.0001f)
            {
                orbitDir = tangent.sqrMagnitude > 0.0001f ? tangent : desiredDir;
            }

            targetVelocity = orbitDir.normalized * enemy.moveSpeed * moveSpeedMultiplier * orbitSpeedMultiplier;
        }

        float crowdFactor = 1f / (1f + ((avoidance.neighborCount / (float)crowdReferenceNeighbors) * crowdSlowdownStrength));
        targetVelocity *= crowdFactor;

        if (avoidance.hardDirection.sqrMagnitude > 0.0001f)
        {
            targetVelocity += avoidance.hardDirection.normalized * hardSeparationSpeed * hardSeparationStrength;
        }

        float maxSpeed = enemy.moveSpeed * moveSpeedMultiplier + (hardSeparationSpeed * hardSeparationStrength);
        if (targetVelocity.sqrMagnitude > maxSpeed * maxSpeed)
        {
            targetVelocity = targetVelocity.normalized * maxSpeed;
        }

        Vector3 currentVel = enemy.rb.linearVelocity;
        Vector3 newVelXz = Vector3.MoveTowards(
            new Vector3(currentVel.x, 0f, currentVel.z),
            new Vector3(targetVelocity.x, 0f, targetVelocity.z),
            acceleration * deltaTime
        );

        enemy.rb.linearVelocity = new Vector3(newVelXz.x, 0f, newVelXz.z);
        StabilizeVerticalPosition(enemy, targetY, deltaTime);
    }

    private AvoidanceResult EvaluateAvoidance(EnemyChaser self, Vector3 selfPos)
    {
        AvoidanceResult result = default;
        if (!enableSeparation) return result;

        float softRadius = separationRadius;
        float softRadiusSqr = softRadius * softRadius;

        float hardRadius = Mathf.Max(softRadius, GetOverlapBoundaryRadius(self) * 2f + _frameSlotSpacing);
        float hardRadiusSqr = hardRadius * hardRadius;

        Vector2Int centerCell = WorldToGrid(selfPos);
        int range = Mathf.CeilToInt(hardRadius / Mathf.Max(0.0001f, _gridCellSize));

        Vector3 softAccum = Vector3.zero;
        float softWeight = 0f;
        Vector3 hardAccum = Vector3.zero;
        float hardWeight = 0f;
        int softNeighborsConsidered = 0;

        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                Vector2Int key = new Vector2Int(centerCell.x + x, centerCell.y + z);
                if (!_spatialGrid.TryGetValue(key, out List<EnemyChaser> bucket) || bucket.Count == 0) continue;

                for (int i = 0; i < bucket.Count; i++)
                {
                    EnemyChaser other = bucket[i];
                    if (other == null || other == self || other.status == null || !other.status.IsAlive) continue;

                    Vector3 away = selfPos - other.cachedTransform.position;
                    away.y = 0f;
                    float sqr = away.sqrMagnitude;
                    if (sqr > hardRadiusSqr) continue;

                    Vector3 dir;
                    float dist;
                    if (sqr <= 0.000001f)
                    {
                        dir = GetDeterministicEscapeDirection(self, other);
                        dist = 0.001f;
                    }
                    else
                    {
                        dist = Mathf.Sqrt(sqr);
                        dir = away / dist;
                    }

                    if (sqr <= softRadiusSqr)
                    {
                        result.neighborCount++;
                        if (softNeighborsConsidered < maxSeparationNeighbors)
                        {
                            float weight = 1f - Mathf.Clamp01(dist / Mathf.Max(0.0001f, softRadius));
                            softAccum += dir * weight;
                            softWeight += weight;
                            softNeighborsConsidered++;
                        }
                    }

                    float requiredDistance = GetPairMinDistance(self, other);
                    if (dist < requiredDistance)
                    {
                        float penetration = (requiredDistance - dist) / Mathf.Max(requiredDistance, 0.0001f);
                        hardAccum += dir * penetration;
                        hardWeight += penetration;
                    }
                }
            }
        }
        if (softWeight > 0f)
        {
            result.softDirection = softAccum / softWeight;
        }

        if (hardWeight > 0f)
        {
            result.hardDirection = hardAccum / hardWeight;
        }

        return result;
    }

    private Vector3 GetDeterministicEscapeDirection(EnemyChaser a, EnemyChaser b)
    {
        int seedA = a != null && a.cachedTransform != null ? a.cachedTransform.GetInstanceID() : 0;
        int seedB = b != null && b.cachedTransform != null ? b.cachedTransform.GetInstanceID() : 0;
        int hash = (seedA * 73856093) ^ (seedB * 19349663);
        float t = (hash & 1023) / 1024f;
        float ang = t * Mathf.PI * 2f;
        return new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
    }
    private void ApplyRotation(EnemyChaser enemy, Vector3 playerPos, Vector3 currentPos, float deltaTime)
    {
        Vector3 moveDir = playerPos - currentPos;
        moveDir.y = 0f;
        if (moveDir.sqrMagnitude <= 0.001f) return;

        Quaternion lookRot = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
        enemy.rb.MoveRotation(Quaternion.Slerp(enemy.rb.rotation, lookRot, deltaTime * enemy.turnSpeed));
    }

    private void DampenVerticalVelocity(EnemyChaser enemy)
    {
        Vector3 vel = enemy.rb.linearVelocity;
        if (Mathf.Abs(vel.y) <= 0.001f) return;
        enemy.rb.linearVelocity = new Vector3(vel.x, 0f, vel.z);
    }

    private void StabilizeVerticalPosition(EnemyChaser enemy, float targetY, float deltaTime)
    {
        Vector3 rbPos = enemy.rb.position;
        float nextY = Mathf.Lerp(rbPos.y, targetY, deltaTime * waterHeightLerpSpeed);
        if (Mathf.Abs(nextY - rbPos.y) <= verticalMoveThreshold) return;

        rbPos.y = nextY;
        enemy.rb.MovePosition(rbPos);
    }

    private float GetEngageRadius(EnemyChaser enemy, float stopDistance)
    {
        float playerRadius = _playerHorizontalRadius > 0f ? _playerHorizontalRadius : fallbackPlayerRadius;
        float enemyRadius = GetHorizontalRadius(enemy.bodyCollider, fallbackEnemyRadius);
        float physicalContactRadius = playerRadius + enemyRadius + ringPadding;

        int ringCount = Mathf.Max(1, _frameRingCount);
        int ringIndex = GetRingIndex(enemy, ringCount);
        float baseRadius = _frameBaseEngageRadius > 0f ? _frameBaseEngageRadius : physicalContactRadius;
        float spacing = _frameSlotSpacing > 0f ? _frameSlotSpacing : Mathf.Max(ringSpacing, minSlotSpacing);
        float ringRadius = baseRadius + (ringIndex * spacing);

        return Mathf.Max(stopDistance, Mathf.Max(physicalContactRadius, ringRadius));
    }

    private int GetRingIndex(EnemyChaser enemy, int ringCount)
    {
        if (ringCount <= 1) return 0;

        int seed = enemy != null && enemy.cachedTransform != null
            ? enemy.cachedTransform.GetInstanceID()
            : (enemy != null ? enemy.GetInstanceID() : 0);

        int hashed = PositiveHash(seed);
        int slot = _totalRingCapacity > 0 ? (hashed % _totalRingCapacity) : 0;

        int capCount = Mathf.Min(ringCount, _ringCumulativeCaps.Count);
        for (int i = 0; i < capCount; i++)
        {
            if (slot < _ringCumulativeCaps[i])
            {
                return i;
            }
        }

        return Mathf.Max(0, capCount - 1);
    }

    private Vector3 GetSlotTargetPosition(Vector3 playerPos, EnemyChaser enemy, float engageRadius)
    {
        int ringCount = Mathf.Max(1, _frameRingCount);
        int ringIndex = GetRingIndex(enemy, ringCount);
        int seed = enemy != null && enemy.cachedTransform != null
            ? enemy.cachedTransform.GetInstanceID()
            : (enemy != null ? enemy.GetInstanceID() : 0);

        int slotCount = 1;
        if (ringIndex >= 0 && ringIndex < _ringCapacities.Count)
        {
            slotCount = Mathf.Max(1, _ringCapacities[ringIndex]);
        }

        int slotHash = PositiveHash(seed ^ 0x2C1B3C6D);
        int slotIndex = slotHash % slotCount;
        float slotAngleStep = 360f / Mathf.Max(1, slotCount);
        float baseAngle = slotAngleStep * slotIndex;

        int jitterHash = PositiveHash(seed ^ 0x68E31DA4);
        float jitterUnit = (jitterHash & 1023) / 1023f;
        float jitter = (jitterUnit - 0.5f) * slotAngleStep * 0.35f;

        float angleDeg = baseAngle + jitter + (Time.time * orbitAngularSpeed * enemy.orbitDirectionSign);
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 ringDir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
        return playerPos + (ringDir * engageRadius);
    }

    private int PositiveHash(int seed)
    {
        int h = seed;
        h ^= (h >> 16);
        h *= unchecked((int)0x7feb352d);
        h ^= (h >> 15);
        h *= unchecked((int)0x846ca68b);
        h ^= (h >> 16);
        return h & 0x7fffffff;
    }

    private void CachePlayerHorizontalRadius()
    {
        _playerHorizontalRadius = fallbackPlayerRadius;
        if (_playerTransform == null) return;

        Collider[] colliders = _playerTransform.GetComponentsInChildren<Collider>(true);
        float maxRadius = 0f;
        for (int i = 0; i < colliders.Length; i++)
        {
            float radius = GetHorizontalRadius(colliders[i], 0f);
            if (radius > maxRadius) maxRadius = radius;
        }

        if (maxRadius > 0f)
        {
            _playerHorizontalRadius = maxRadius;
        }
    }

    private float GetHorizontalRadius(Collider col, float fallback)
    {
        if (col == null) return fallback;
        Bounds b = col.bounds;
        float radius = Mathf.Max(b.extents.x, b.extents.z);
        return radius > 0.0001f ? radius : fallback;
    }

    private float GetOverlapBoundaryRadius(EnemyChaser enemy)
    {
        float r = GetHorizontalRadius(enemy != null ? enemy.bodyCollider : null, fallbackEnemyRadius);
        return r + overlapBoundaryPadding;
    }

    private float GetPairMinDistance(EnemyChaser a, EnemyChaser b)
    {
        return GetOverlapBoundaryRadius(a) + GetOverlapBoundaryRadius(b);
    }

    private void UpdateRuntimeCrowdMetrics()
    {
        int count = _aliveEnemies.Count;
        runtimeAliveCount = count;

        if (count <= 1)
        {
            runtimeOverlapPairs = 0;
            runtimeAverageNearestDistance = 0f;
            runtimeAverageRequiredDistance = 0f;
            return;
        }

        int overlapPairs = 0;

        for (int i = 0; i < count - 1; i++)
        {
            EnemyChaser a = _aliveEnemies[i];
            Vector3 aPos = a.cachedTransform.position;

            for (int j = i + 1; j < count; j++)
            {
                EnemyChaser b = _aliveEnemies[j];
                Vector3 bPos = b.cachedTransform.position;

                Vector3 d = bPos - aPos;
                d.y = 0f;
                float dist = d.magnitude;
                float required = GetPairMinDistance(a, b);
                if (dist < required)
                {
                    overlapPairs++;
                }
            }
        }

        float sumNearest = 0f;
        float sumRequired = 0f;

        for (int i = 0; i < count; i++)
        {
            EnemyChaser a = _aliveEnemies[i];
            Vector3 aPos = a.cachedTransform.position;

            float bestDist = float.MaxValue;
            float bestRequired = 0f;

            for (int j = 0; j < count; j++)
            {
                if (i == j) continue;

                EnemyChaser b = _aliveEnemies[j];
                Vector3 bPos = b.cachedTransform.position;

                Vector3 d = bPos - aPos;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestRequired = GetPairMinDistance(a, b);
                }
            }

            if (bestDist < float.MaxValue)
            {
                sumNearest += bestDist;
                sumRequired += bestRequired;
            }
        }

        runtimeOverlapPairs = overlapPairs;
        runtimeAverageNearestDistance = sumNearest / count;
        runtimeAverageRequiredDistance = sumRequired / count;
    }
    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos) return;

        Transform player = _playerTransform;
        if (player == null && PlayerManager.Instance != null)
        {
            player = PlayerManager.Instance.PlayerTransform;
        }
        if (player == null) return;

        Vector3 playerPos = player.position;
        Vector3 playerDrawPos = playerPos + (Vector3.up * gizmoHeightOffset);
        float previewEngageRadius = fallbackPlayerRadius + fallbackEnemyRadius + ringPadding;

        Gizmos.color = engageRadiusColor;
        DrawCircleXZ(playerDrawPos, previewEngageRadius, 48);

        if (!Application.isPlaying) return;

        int drawn = 0;
        float debugMaxDistSqr = debugMaxDistance * debugMaxDistance;

        for (int i = 0; i < _aliveEnemies.Count; i++)
        {
            EnemyChaser enemy = _aliveEnemies[i];
            if (enemy == null || enemy.status == null || !enemy.status.IsAlive) continue;

            Vector3 enemyPos = enemy.cachedTransform.position;
            if (debugMaxDistance > 0f && (enemyPos - playerPos).sqrMagnitude > debugMaxDistSqr) continue;

            DrawEnemyDebugGizmo(enemy, playerPos);
            drawn++;

            if (!drawAllEnemyDebug && drawn >= maxDebugEnemies) break;
        }
    }

    private void DrawEnemyDebugGizmo(EnemyChaser enemy, Vector3 playerPos)
    {
        Vector3 enemyPos = enemy.cachedTransform.position;
        Vector3 enemyDrawPos = enemyPos + (Vector3.up * gizmoHeightOffset);

        float stopDistance = enemy.effectiveStopDistance > 0f ? enemy.effectiveStopDistance : enemy.stopDistance;
        float engageRadius = GetEngageRadius(enemy, stopDistance);

        Vector3 slotPos = GetSlotTargetPosition(playerPos, enemy, engageRadius);
        Vector3 slotDrawPos = slotPos + (Vector3.up * gizmoHeightOffset);

        Gizmos.color = engageRadiusColor;
        DrawCircleXZ(playerPos + (Vector3.up * gizmoHeightOffset), engageRadius, 64);

        Gizmos.color = separationRadiusColor;
        Gizmos.DrawWireSphere(enemyDrawPos, separationRadius);

        Gizmos.color = overlapBoundaryColor;
        Gizmos.DrawWireSphere(enemyDrawPos, GetOverlapBoundaryRadius(enemy));

        Gizmos.color = slotTargetColor;
        Gizmos.DrawLine(enemyDrawPos, slotDrawPos);
        Gizmos.DrawWireSphere(slotDrawPos, 0.2f);

        Vector3 vel = enemy.rb != null ? enemy.rb.linearVelocity : Vector3.zero;
        Vector3 velDraw = new Vector3(vel.x, 0f, vel.z);
        if (velDraw.sqrMagnitude > 0.0001f)
        {
            Gizmos.color = velocityColor;
            Gizmos.DrawLine(enemyDrawPos, enemyDrawPos + (velDraw.normalized * 1.25f));
        }
    }

    private void DrawCircleXZ(Vector3 center, float radius, int segments)
    {
        if (radius <= 0f || segments < 3) return;

        float step = 360f / segments;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float ang = step * i * Mathf.Deg2Rad;
            Vector3 next = center + new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }

    public bool TryGetEnemyDebugInfo(
        EnemyChaser enemy,
        out Vector3 playerPos,
        out float engageRadius,
        out float currentSeparationRadius,
        out Vector3 slotTargetPos,
        out float yOffset,
        out Color separationColor,
        out Color engageColor,
        out Color slotColor,
        out float overlapBoundaryRadius,
        out Color overlapColor,
        out bool hasNearestEnemy,
        out Vector3 nearestEnemyPos,
        out float nearestDistance,
        out float nearestRequiredDistance)
    {
        playerPos = Vector3.zero;
        engageRadius = 0f;
        currentSeparationRadius = separationRadius;
        slotTargetPos = Vector3.zero;
        yOffset = gizmoHeightOffset;
        separationColor = separationRadiusColor;
        engageColor = engageRadiusColor;
        slotColor = slotTargetColor;
        overlapBoundaryRadius = 0f;
        overlapColor = overlapBoundaryColor;
        hasNearestEnemy = false;
        nearestEnemyPos = Vector3.zero;
        nearestDistance = 0f;
        nearestRequiredDistance = 0f;

        if (enemy == null) return false;

        Transform player = _playerTransform;
        if (player == null && PlayerManager.Instance != null)
        {
            player = PlayerManager.Instance.PlayerTransform;
        }
        if (player == null) return false;

        playerPos = player.position;

        if (_playerTransform == null && player != null)
        {
            _playerTransform = player;
            CachePlayerHorizontalRadius();
        }

        float stopDistance = enemy.effectiveStopDistance > 0f ? enemy.effectiveStopDistance : enemy.stopDistance;
        engageRadius = GetEngageRadius(enemy, stopDistance);
        slotTargetPos = GetSlotTargetPosition(playerPos, enemy, engageRadius);
        overlapBoundaryRadius = GetOverlapBoundaryRadius(enemy);

        float bestSqr = float.MaxValue;
        EnemyChaser nearest = null;
        Vector3 selfPos = enemy.cachedTransform != null ? enemy.cachedTransform.position : enemy.transform.position;

        for (int i = 0; i < _aliveEnemies.Count; i++)
        {
            EnemyChaser other = _aliveEnemies[i];
            if (other == null || other == enemy || other.status == null || !other.status.IsAlive) continue;

            Vector3 otherPos = other.cachedTransform != null ? other.cachedTransform.position : other.transform.position;
            Vector3 d = otherPos - selfPos;
            d.y = 0f;
            float sqr = d.sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                nearest = other;
            }
        }

        if (nearest != null)
        {
            hasNearestEnemy = true;
            nearestEnemyPos = nearest.cachedTransform != null ? nearest.cachedTransform.position : nearest.transform.position;
            nearestDistance = Mathf.Sqrt(bestSqr);
            nearestRequiredDistance = GetPairMinDistance(enemy, nearest);
        }

        return true;
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
        else
        {
            sampler.SetSamplePosition(0, pos);
        }

        Gerstner.ComputeHeight(sampler, heightInterface);

        if (highQuality)
        {
            float h1 = sampler.heightValues[0];
            float h2 = sampler.heightValues[1];
            float h3 = sampler.heightValues[2];
            float h4 = sampler.heightValues[3];
            return ((h1 + h2 + h3 + h4) * 0.25f) + enemy.heightOffset;
        }

        return sampler.heightValues[0] + enemy.heightOffset;
    }
}
