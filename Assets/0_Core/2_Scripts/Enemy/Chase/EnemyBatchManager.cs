using UnityEngine;
using System.Collections.Generic;
using StylizedWater3;

public class EnemyBatchManager : MonoBehaviour
{
    public static EnemyBatchManager Instance { get; private set; }

    private readonly List<EnemyChaser> _enemies = new List<EnemyChaser>(1000);
    
    [SerializeField] private Transform _playerTransform;

    [Header("부력 시스템 관련")]
    [SerializeField] private HeightQuerySystem.Interface heightInterface;
    [SerializeField] private WaveProfile waveProfile;
    private HeightQuerySystem.Sampler _samplerHigh; // 고품질용 (4개)
    private HeightQuerySystem.Sampler _samplerLow;
    //private HeightQuerySystem.Sampler _sampler;

    [Header("최적화 관련")]
    [SerializeField] private float highQualityDistSqr = 30f * 30f; // 30m 이내는 고품질
    private const float UPDATE_DISTANCE_THRESHOLD = 10.0f;
    private float _updateDistanceSqr;
    private Vector3 _lastQueryPosition;

    [Header("부력 관련")]
    [SerializeField] private int buoyancyFrameInterval = 4;
    [SerializeField] private float buoyancyStrength = 3.0f;
    [SerializeField] private float waterDrag = 2.0f;
    [SerializeField] private float waveAlignmentStrength = 0.5f;

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

            if (!e.status.IsAlive || e.status.IsKnockbacked)
            {
                // 약간의 감속만 적용
                if (!e.status.IsAlive)
                    e.rb.linearVelocity = Vector3.Lerp(e.rb.linearVelocity, Vector3.zero, deltaTime * 0.5f);
                continue;
            }

            Vector3 currentPos = e.rb.position;
            Vector3 diff = playerPos - currentPos;     
            diff.y = 0;
            float distSqr = diff.sqrMagnitude;

            bool shouldUpdateBuoyancy = ((Time.frameCount + e.buoyancyFrameOffset) % buoyancyFrameInterval) == 0;

            if (shouldUpdateBuoyancy)
            {
                // [개선된 부력 로직]
                HandleBuoyancyAdvanced(e, currentPos, distSqr);
            }

            // 이동 및 회전 처리 (X, Z축 제어)
            HandleMovement(e, diff, distSqr, deltaTime);
        }
    }


    /// <summary>
    /// 물 높이를 계산하고 Y축 속도를 제어
    /// </summary>
    private void HandleBuoyancyAdvanced(EnemyChaser e, Vector3 currentPos, float distSqr)
    {
        HeightQuerySystem.Sampler currentSampler;
        int currentSampleCount;
        bool isHighQuality = distSqr < highQualityDistSqr;

        if (isHighQuality)
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
            currentSampler = _samplerLow;
            currentSampleCount = 1;
            currentSampler.SetSamplePosition(0, currentPos);
        }

        Gerstner.ComputeHeight(currentSampler, heightInterface);

        float avgHeight = 0f;
        Vector3 targetNormal = Vector3.up;

        if (currentSampleCount == 4)
        {
            float h1 = currentSampler.heightValues[0];
            float h2 = currentSampler.heightValues[1];
            float h3 = currentSampler.heightValues[2];
            float h4 = currentSampler.heightValues[3];

            avgHeight = (h1 + h2 + h3 + h4) * 0.25f;

            // Stylized Water 3 내장 함수로 파도의 기울기 계산
            targetNormal = HeightQuerySystem.DeriveNormal(h1, h2, h3, h4, 1.0f);
        }
        else
        {
            avgHeight = currentSampler.heightValues[0];
            // 저품질은 기울기 계산 생략 (성능 최적화)
        }

        float targetY = avgHeight + e.heightOffset;
        float heightDiff = targetY - currentPos.y;
        Vector3 currentVel = e.rb.linearVelocity;
        float buoyancyForce = (heightDiff * buoyancyStrength) - (currentVel.y * waterDrag);
        currentVel.y += buoyancyForce * Time.fixedDeltaTime; // 가속도 적용
        e.rb.linearVelocity = currentVel;

        if (isHighQuality && waveAlignmentStrength > 0)
        {
            Vector3 forward = e.transform.forward;
            Vector3 projectedForward = Vector3.ProjectOnPlane(forward, targetNormal).normalized;
            Quaternion targetRot = Quaternion.LookRotation(projectedForward, targetNormal);
            e.rb.rotation = Quaternion.Slerp(e.rb.rotation, targetRot, Time.fixedDeltaTime * 2.0f * waveAlignmentStrength);
        }
    }

    /// <summary>
    /// 플레이어를 추적하고 회전
    /// </summary>
    private void HandleMovement(EnemyChaser e, Vector3 diff, float distSqr, float deltaTime)
    {
        e.rb.angularVelocity = Vector3.zero;

        // 회전
        if (distSqr > 1.0f)
        {
            Vector3 lookDir = diff.normalized;
            Vector3 upDir = e.transform.up;
            Quaternion targetLookRot = Quaternion.LookRotation(lookDir, upDir);
            e.rb.MoveRotation(Quaternion.Slerp(e.rb.rotation, targetLookRot, deltaTime * e.turnSpeed));
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