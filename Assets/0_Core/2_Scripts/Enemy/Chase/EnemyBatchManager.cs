using UnityEngine;
using System.Collections.Generic;
using StylizedWater3;

public class EnemyBatchManager : MonoBehaviour
{
    public static EnemyBatchManager Instance { get; private set; }

    private readonly List<EnemyChaser> _enemies = new List<EnemyChaser>(1000);
    
    [SerializeField] private Transform _playerTransform;

    [Header("Water System Settings")]
    [SerializeField] private HeightQuerySystem.Interface heightInterface;
    [SerializeField] private WaveProfile waveProfile;
    //private HeightQuerySystem.Sampler _sampler;

    private HeightQuerySystem.Sampler _samplerHigh; // 고품질용 (4개)
    private HeightQuerySystem.Sampler _samplerLow;

    [Header("Optimization Settings")]
    [SerializeField] private float highQualityDistSqr = 30f * 30f; // 30m 이내는 고품질
    private const float UPDATE_DISTANCE_THRESHOLD = 10.0f;
    private float _updateDistanceSqr;
    private Vector3 _lastQueryPosition;

    [Header("Buoyancy Update")]
    [SerializeField] private int buoyancyFrameInterval = 4;
    private bool shouldUpdateBuoyancy = false;
    private void Awake()
    {
        Instance = this;

        _samplerHigh = new HeightQuerySystem.Sampler();
        _samplerHigh.SetSampleCount(4);

        _samplerLow = new HeightQuerySystem.Sampler();
        _samplerLow.SetSampleCount(1);

        if (heightInterface == null) heightInterface = new HeightQuerySystem.Interface();
        if (waveProfile != null) heightInterface.waveProfile = waveProfile;

        _updateDistanceSqr = UPDATE_DISTANCE_THRESHOLD * UPDATE_DISTANCE_THRESHOLD;
        _lastQueryPosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);
    }

    private void Start()
    {
        // 레이스 컨디션 발생 - 후에 로직 변경
        if (PlayerManager.Instance != null)
        {
            _playerTransform = PlayerManager.Instance.PlayerTransform;
        }
        else
        {
            Debug.Log("플레이어매니저 작동 안됌");            
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

            if (heightInterface.HasMissingReferences() && enemy.cachedTransform != null)
            {
                heightInterface.GetWaterObject(enemy.cachedTransform.position);
                _lastQueryPosition = enemy.cachedTransform.position;
            }

            //if (heightInterface != null)
            //    heightInterface.GetWaterObject(enemy.cachedTransform.position);
        }
    }
    public void Unregister(EnemyChaser enemy)
    {
        if (enemy != null) _enemies.Remove(enemy);
    }

    private void FixedUpdate()
    {
        if (_playerTransform == null) return;

        Vector3 playerPos = _playerTransform.position;
        float movedDistSqr = (playerPos - _lastQueryPosition).sqrMagnitude;

        if (movedDistSqr > _updateDistanceSqr || heightInterface.HasMissingReferences())
        {
            heightInterface.GetWaterObject(playerPos);
            _lastQueryPosition = playerPos;
        }
        if (heightInterface.HasMissingReferences()) return;

        if (_enemies.Count == 0) return;

        float deltaTime = Time.fixedDeltaTime;

        for (int i = 0; i < _enemies.Count; i++)
        {
            var e = _enemies[i];

            if (e == null) continue;

            if (!e.status.IsAlive) 
            {
                e.rb.linearVelocity = Vector3.Lerp(e.rb.linearVelocity, Vector3.zero, deltaTime * 2f);
                continue;
            }

            if (e.status.IsKnockbacked)
            {
                continue;
            }

            // 적 생존 여부 확인
            /*if (e == null || !e.status.IsAlive)
            {
                if (e != null) e.rb.linearVelocity = Vector3.Lerp(e.rb.linearVelocity, Vector3.zero, deltaTime * 2f);
                continue;
            }*/

            Vector3 currentPos = e.rb.position;
            Vector3 diff = playerPos - currentPos;
            diff.y = 0;
            float distSqr = diff.sqrMagnitude;

            shouldUpdateBuoyancy = ((Time.frameCount + e.buoyancyFrameOffset) % buoyancyFrameInterval) == 0;

            if (shouldUpdateBuoyancy)
            {
                HandleBuoyancy(e, currentPos, distSqr);
            }

            // 이동 및 회전 처리 (X, Z축 제어)
            HandleMovement(e, playerPos, deltaTime);
        }
    }

    /// <summary>
    /// 물 높이를 계산하고 Y축 속도를 제어
    /// </summary>
    private void HandleBuoyancy(EnemyChaser e, Vector3 currentPos, float distSqr)
    {
        if (!shouldUpdateBuoyancy)
        {
            Vector3 vel = e.rb.linearVelocity;
            vel.y = Mathf.Lerp(vel.y, 0f, 0.05f);
            e.rb.linearVelocity = vel;
            return;
        }

        HeightQuerySystem.Sampler currentSampler;
        int currentSampleCount;

        if (distSqr < highQualityDistSqr)
        {
            currentSampler = _samplerHigh;
            currentSampleCount = 4;

            float halfSize = e.surfaceSize * 0.5f;
            currentSampler.SetSamplePosition(0, currentPos + new Vector3(-halfSize, 0, 0));
            currentSampler.SetSamplePosition(1, currentPos + new Vector3(halfSize, 0, 0));
            currentSampler.SetSamplePosition(2, currentPos + new Vector3(0, 0, -halfSize));
            currentSampler.SetSamplePosition(3, currentPos + new Vector3(0, 0, halfSize));
        }
        else
        {
            // [Low Quality] 1점 샘플링
            currentSampler = _samplerLow;
            currentSampleCount = 1;
            currentSampler.SetSamplePosition(0, currentPos);
        }

        Gerstner.ComputeHeight(currentSampler, heightInterface);

        float avgHeight = 0f;

        if (currentSampleCount == 4)
        {
            avgHeight = (currentSampler.heightValues[0] + currentSampler.heightValues[1] +
                         currentSampler.heightValues[2] + currentSampler.heightValues[3]) * 0.25f;
        }
        else
        {
            avgHeight = currentSampler.heightValues[0];
        }

        // 부력 물리 적용
        float targetY = avgHeight + e.heightOffset;
        float heightDiff = targetY - currentPos.y;

        Vector3 velFinal = e.rb.linearVelocity;
        velFinal.y = heightDiff * 2.0f; // 10.0f는 부력 강도 (필요 시 조절)
        e.rb.linearVelocity = velFinal;
    }

    /// <summary>
    /// 플레이어를 추적하고 회전
    /// </summary>
    private void HandleMovement(EnemyChaser e, Vector3 playerPos, float deltaTime)
    {
        e.rb.angularVelocity = Vector3.zero;

        Vector3 currentPos = e.rb.position;
        Vector3 diff = playerPos - currentPos;
        diff.y = 0; // 높이 차 무시
        float distSqr = diff.sqrMagnitude;

        // 회전
        if (distSqr > 1.0f)
        {
            Quaternion targetRot = Quaternion.LookRotation(diff);

            // Slerp로 부드럽게 회전
            Quaternion nextRot = Quaternion.Slerp(e.rb.rotation, targetRot, deltaTime * e.turnSpeed);
            e.rb.MoveRotation(nextRot);
        }

        // 이동
        Vector3 currentVelocity = e.rb.linearVelocity;

        if (distSqr > e.stopDistance * e.stopDistance)
        {            
            Vector3 moveDir = diff.normalized * e.moveSpeed;            

            currentVelocity.x = moveDir.x;
            currentVelocity.z = moveDir.z;
        }
        else
        {
            // 정지 시 감속
            currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, 0, deltaTime * 10f);
            currentVelocity.z = Mathf.MoveTowards(currentVelocity.z, 0, deltaTime * 10f);
        }

        // 최종 속도 적용
        e.rb.linearVelocity = currentVelocity;
    }
}