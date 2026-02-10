using UnityEngine;
using System.Collections.Generic;
using StylizedWater3;

public class EnemyBatchManager : MonoBehaviour
{
    public static EnemyBatchManager Instance { get; private set; }

    private readonly List<EnemyChaser> _enemies = new List<EnemyChaser>(1000);

    [SerializeField] private Transform _playerTransform;

    [Header("Water System")]
    [SerializeField] private HeightQuerySystem.Interface heightInterface;
    [SerializeField] private WaveProfile waveProfile;
    private HeightQuerySystem.Sampler _samplerHigh;
    private HeightQuerySystem.Sampler _samplerLow;

    [Header("Optimization")]
    [SerializeField] private float highQualityDistSqr = 30f * 30f;
    private const float UPDATE_DISTANCE_THRESHOLD = 10.0f;
    private Vector3 _lastQueryPosition;

    [Header("Movement & Physics (Kinematic)")]
    [SerializeField] private int buoyancyFrameInterval = 4;
    [Tooltip("파도 기울기에 배를 맞출 강도")]
    [SerializeField] private float waveAlignmentStrength = 2.0f;

    [Header("Separation (겹침 방지)")]
    [Tooltip("적끼리 서로 밀어내는 반경")]
    [SerializeField] private float separationRadius = 3.0f;
    [Tooltip("밀어내는 힘의 강도")]
    [SerializeField] private float separationForce = 2.0f;
    [Tooltip("적 레이어 (자기들끼리만 밀어내기 위함)")]
    [SerializeField] private LayerMask enemyLayer;

    // 최적화를 위한 버퍼 (OverlapSphereNonAlloc용)
    private Collider[] _neighborBuffer = new Collider[10];

    private void Awake()
    {
        Instance = this;
        _samplerHigh = new HeightQuerySystem.Sampler(); _samplerHigh.SetSampleCount(4);
        _samplerLow = new HeightQuerySystem.Sampler(); _samplerLow.SetSampleCount(1);

        if (heightInterface == null) heightInterface = new HeightQuerySystem.Interface();
        if (waveProfile != null) heightInterface.waveProfile = waveProfile;

        _lastQueryPosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);
    }

    private void Start()
    {
        if (PlayerManager.Instance != null) _playerTransform = PlayerManager.Instance.PlayerTransform;
    }

    private void OnDestroy() { _samplerHigh?.Dispose(); _samplerLow?.Dispose(); }

    public void Register(EnemyChaser enemy)
    {
        if (enemy != null && !_enemies.Contains(enemy))
        {
            _enemies.Add(enemy);
            // 초기화 시 물 높이 갱신
            if (heightInterface.HasMissingReferences())
                heightInterface.GetWaterObject(enemy.cachedTransform.position);
        }
    }
    public void Unregister(EnemyChaser enemy) { if (enemy != null) _enemies.Remove(enemy); }

    private void FixedUpdate()
    {
        if (_playerTransform == null) return;

        // 1. 물 시스템 쿼리 위치 갱신 (최적화)
        Vector3 playerPos = _playerTransform.position;
        if ((playerPos - _lastQueryPosition).sqrMagnitude > UPDATE_DISTANCE_THRESHOLD * UPDATE_DISTANCE_THRESHOLD
            || heightInterface.HasMissingReferences())
        {
            heightInterface.GetWaterObject(playerPos);
            _lastQueryPosition = playerPos;
        }
        if (heightInterface.HasMissingReferences()) return;

        float deltaTime = Time.fixedDeltaTime;

        // 2. 모든 적 업데이트
        for (int i = 0; i < _enemies.Count; i++)
        {
            var e = _enemies[i];
            if (e == null) continue;
            if (!e.status.IsAlive || e.status.IsKnockbacked) continue; // 넉백/사망 시 이동 중지

            // 거리 및 샘플링 품질 결정
            Vector3 currentPos = e.cachedTransform.position;
            float distSqr = (playerPos - currentPos).sqrMagnitude;
            bool isHighQuality = distSqr < highQualityDistSqr;

            // A. 높이(부력) 및 회전 계산
            (float targetY, Vector3 targetNormal) = CalculateWaterHeightAndNormal(e, currentPos, isHighQuality);

            // B. 이동 벡터 계산 (추적 + 회피)
            Vector3 finalMoveDir = CalculateMovementVector(e, playerPos, currentPos);

            // C. 최종 위치 적용 (Kinematic Movement)
            // 수평 이동 적용
            Vector3 nextPos = currentPos + (finalMoveDir * e.moveSpeed * deltaTime);

            // 수직 위치 강제 고정 (절대 가라앉지 않음)
            nextPos.y = targetY;

            // 위치 갱신
            e.rb.MovePosition(nextPos);

            // D. 회전 적용 (진행 방향 + 파도 기울기)
            if (finalMoveDir.sqrMagnitude > 0.01f)
            {
                // 1. 진행 방향 바라보기 (Yaw)
                Quaternion lookRot = Quaternion.LookRotation(finalMoveDir.normalized, Vector3.up);

                // 2. 파도 기울기 적용 (Pitch/Roll) - 고품질일 때만
                if (isHighQuality && waveAlignmentStrength > 0)
                {
                    // 파도 Normal에 Up 벡터를 맞춤
                    Quaternion waveRot = Quaternion.FromToRotation(Vector3.up, targetNormal);
                    lookRot = waveRot * lookRot; // 기울기 합성
                }

                // 부드러운 회전
                e.rb.MoveRotation(Quaternion.Slerp(e.rb.rotation, lookRot, deltaTime * e.turnSpeed * waveAlignmentStrength));
            }
        }
    }

    // [로직 분리] 물 높이와 법선 벡터 계산
    private (float, Vector3) CalculateWaterHeightAndNormal(EnemyChaser e, Vector3 pos, bool highQuality)
    {
        HeightQuerySystem.Sampler sampler = highQuality ? _samplerHigh : _samplerLow;
        int count = highQuality ? 4 : 1;

        if (highQuality)
        {
            float half = e.surfaceSize * 0.5f;
            sampler.SetSamplePosition(0, pos + new Vector3(-half, 0, 0));
            sampler.SetSamplePosition(1, pos + new Vector3(half, 0, 0));
            sampler.SetSamplePosition(2, pos + new Vector3(0, 0, -half));
            sampler.SetSamplePosition(3, pos + new Vector3(0, 0, half));
        }
        else
        {
            sampler.SetSamplePosition(0, pos);
        }

        Gerstner.ComputeHeight(sampler, heightInterface);

        float avgHeight = 0f;
        Vector3 normal = Vector3.up;

        if (highQuality)
        {
            float h1 = sampler.heightValues[0]; float h2 = sampler.heightValues[1];
            float h3 = sampler.heightValues[2]; float h4 = sampler.heightValues[3];
            avgHeight = (h1 + h2 + h3 + h4) * 0.25f;
            normal = HeightQuerySystem.DeriveNormal(h1, h2, h3, h4, 1.0f);
        }
        else
        {
            avgHeight = sampler.heightValues[0];
        }

        return (avgHeight + e.heightOffset, normal);
    }

    // [핵심] 이동 벡터 + 서로 밀어내기(Separation) 계산
    private Vector3 CalculateMovementVector(EnemyChaser e, Vector3 targetPos, Vector3 currentPos)
    {
        // 1. 목표 추적 벡터
        Vector3 diff = targetPos - currentPos;
        diff.y = 0;
        float distSqr = diff.sqrMagnitude;
        Vector3 moveDir = Vector3.zero;

        if (distSqr > e.stopDistance * e.stopDistance)
        {
            moveDir = diff.normalized;
        }

        // 2. 분리(Separation) 로직: 주변 적들과 겹치면 밀어내는 힘 추가
        Vector3 separationVec = Vector3.zero;
        int count = Physics.OverlapSphereNonAlloc(currentPos, separationRadius, _neighborBuffer, enemyLayer);

        for (int i = 0; i < count; i++)
        {
            Collider col = _neighborBuffer[i];
            if (col.gameObject == e.gameObject) continue; // 나 자신은 제외

            Vector3 awayDir = currentPos - col.transform.position;
            awayDir.y = 0; // 수직 밀림 방지

            float sqrMag = awayDir.sqrMagnitude;
            if (sqrMag > 0.01f) // 0으로 나누기 방지
            {
                // 가까울수록 더 강하게 밀어냄 (1/거리)
                separationVec += awayDir.normalized / sqrMag;
            }
        }

        // 추적 벡터 + (밀어내기 벡터 * 강도)
        // .normalized를 한 번 더 해줘서 속도가 폭주하는 것을 방지
        return (moveDir + (separationVec * separationForce)).normalized;
    }
}