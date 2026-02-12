using UnityEngine;
using System.Collections.Generic;
using StylizedWater3;

public class EnemyBatchManager : MonoBehaviour
{
    public static EnemyBatchManager Instance { get; private set; }
    private readonly List<EnemyChaser> _enemies = new List<EnemyChaser>(1000);

    [SerializeField] private Transform _playerTransform;

    [Header("부력 관련")]
    [SerializeField] private HeightQuerySystem.Interface heightInterface;
    [SerializeField] private WaveProfile waveProfile;
    private HeightQuerySystem.Sampler _samplerHigh;
    private HeightQuerySystem.Sampler _samplerLow;

    [Header("최적화 관련")]
    [SerializeField] private float highQualityDistSqr = 30f * 30f;
    private const float UPDATE_DISTANCE_THRESHOLD = 10.0f;
    private Vector3 _lastQueryPosition;

    [Header("물리 관련")]
    [SerializeField] private float moveSpeedMultiplier = 1.0f;
    [SerializeField] private float waveAlignmentStrength = 2.0f;
    [SerializeField] private float acceleration = 50.0f; // 가속력

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

    public void Register(EnemyChaser enemy)
    {
        if (enemy != null && !_enemies.Contains(enemy))
        {
            _enemies.Add(enemy);
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
        }
    }

    private void FixedUpdate()
    {
        if (_playerTransform == null) return;

        float deltaTime = Time.fixedDeltaTime;
        Vector3 playerPos = _playerTransform.position;

        // 물 위치 갱신
        if ((playerPos - _lastQueryPosition).sqrMagnitude > UPDATE_DISTANCE_THRESHOLD * UPDATE_DISTANCE_THRESHOLD
            || heightInterface.HasMissingReferences())
        {
            heightInterface.GetWaterObject(playerPos);
            _lastQueryPosition = playerPos;
        }

        //모든 적 업데이트
        for (int i = 0; i < _enemies.Count; i++)
        {
            var e = _enemies[i];
            if (e == null) continue;

            if (!e.status.IsAlive || e.status.IsKnockbacked) continue;

            Vector3 currentPos = e.cachedTransform.position;
            float distSqr = (playerPos - currentPos).sqrMagnitude;
            bool isHighQuality = distSqr < highQualityDistSqr;

            // 물 높이 계산
            (float targetY, Vector3 targetNormal) = CalculateWaterHeightAndNormal(e, currentPos, isHighQuality);

            Vector3 dir = (playerPos - currentPos);
            dir.y = 0;

            Vector3 targetVelocity = Vector3.zero;

            if (dir.sqrMagnitude > e.stopDistance * e.stopDistance)
            {
                dir.Normalize();
                targetVelocity = dir * e.moveSpeed * moveSpeedMultiplier;
            }

            Vector3 currentVel = e.rb.linearVelocity;
            Vector3 newVelXz = Vector3.MoveTowards(
                new Vector3(currentVel.x, 0, currentVel.z),
                new Vector3(targetVelocity.x, 0, targetVelocity.z),
                acceleration * deltaTime
            );

            e.rb.linearVelocity = new Vector3(newVelXz.x, currentVel.y, newVelXz.z);

            // 이동
            /*Vector3 dir = (playerPos - currentPos);
            dir.y = 0; // 높이 차이 무시


            if (dir.sqrMagnitude > e.stopDistance * e.stopDistance)
            {
                dir.Normalize();
                // Rigidbody를 통해 이동 
                Vector3 targetVelocity = dir * e.moveSpeed * moveSpeedMultiplier;

                // Y축 속도는 유지 
                targetVelocity.y = e.rb.linearVelocity.y;
                e.rb.linearVelocity = targetVelocity;
            }
            else
            {
                // 멈춤
                e.rb.linearVelocity = new Vector3(0, e.rb.linearVelocity.y, 0);
            }*/

            // 높이 보정 (물리 위에 둥둥 뜨는 느낌)
            Vector3 fixedPos = e.rb.position;
            fixedPos.y = Mathf.Lerp(fixedPos.y, targetY, deltaTime * 5f);
            e.rb.MovePosition(fixedPos); // Y축 강제 동기화

            // 회전
            if (e.rb.linearVelocity.sqrMagnitude > 0.1f)
            {
                Vector3 moveDir = e.rb.linearVelocity;
                moveDir.y = 0;
                if (moveDir.sqrMagnitude > 0.01f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
                    if (isHighQuality && waveAlignmentStrength > 0)
                    {
                        Quaternion waveRot = Quaternion.FromToRotation(Vector3.up, targetNormal);
                        lookRot = waveRot * lookRot;
                    }
                    e.rb.MoveRotation(Quaternion.Slerp(e.rb.rotation, lookRot, deltaTime * e.turnSpeed));
                }
            }
        }
    }

    // 물 계산 로직
    private (float, Vector3) CalculateWaterHeightAndNormal(EnemyChaser e, Vector3 pos, bool highQuality)
    {
        HeightQuerySystem.Sampler sampler = highQuality ? _samplerHigh : _samplerLow;
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
}