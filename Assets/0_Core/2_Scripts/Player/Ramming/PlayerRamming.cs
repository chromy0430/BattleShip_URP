using System.Collections.Generic;
using UnityEngine;

public class PlayerRamming : MonoBehaviour
{
    [SerializeField] PlayerMove playerMove;

    [Header("충각 관련")]
    [SerializeField] private float rammingDamage = 50f;
    [SerializeField] private float rammingRadius = 4.0f;
    [SerializeField] private float rammingKnockbackForce = 30f;
    [SerializeField] private float damageCooldown = 0.5f;
    [SerializeField] private float minRammingSpeed = 20.0f;

    [SerializeField, Range(0f, 1f), Tooltip("적중 각도. 0.5는 전방 90도 부채꼴을 의미합니다.")]
    private float rammingAngleThreshold = 0.5f;

    private readonly List<EnemyChaser> _rammingTargetsBuffer = new List<EnemyChaser>(32);
    private readonly Dictionary<int, float> _hitCooldowns = new Dictionary<int, float>();
    private readonly List<int> _keysToRemoveBuffer = new List<int>(32);
    private float _cleanupTimer = 0f;

    private PlayerMove _playerMove;
    private Transform _transform;

    private void Awake()
    {
        _playerMove = GetComponent<PlayerMove>();
        _transform = transform;
    }

    private void Update()
    {
        _cleanupTimer += Time.deltaTime;
        if (_cleanupTimer >= 1.0f)
        {
            CleanUpExpiredCooldowns();
            _cleanupTimer = 0f;
        }

        if (_playerMove != null && _playerMove.currentSpeed >= minRammingSpeed /* && _playerMove.isDashing */)
        {
            ProcessRamming();
        }    
    }

    private void ProcessRamming()
    {
        if (EnemyBatchManager.Instance == null) return;

        // 반경 내의 모든 적을 논알록으로 가져옴
        EnemyBatchManager.Instance.GetEnemiesInRadius(_transform.position, rammingRadius, _rammingTargetsBuffer);

        Vector3 playerForward = _transform.forward;
        playerForward.y = 0f;
        playerForward.Normalize();
        float currentTime = Time.time;

        for (int i = 0; i < _rammingTargetsBuffer.Count; i++)
        {
            EnemyChaser target = _rammingTargetsBuffer[i];
            int instanceId = target.GetInstanceID();

            // 이미 타격한 적이라면 패스
            if (_hitCooldowns.TryGetValue(instanceId, out float expirationTime))
            {
                if (currentTime < expirationTime) continue;
            }

            Vector3 dirToEnemy = target.cachedTransform.position - _transform.position;
            dirToEnemy.y = 0f;
            dirToEnemy.Normalize();
            float dotProduct = Vector3.Dot(playerForward, dirToEnemy);

            // 타겟이 부채꼴 각도 바깥(측면, 후면)에 있다면 데미지 무시
            if (dotProduct < rammingAngleThreshold) continue;

            target.status.TakeDamage((int)rammingDamage, out _);
            target.status.ApplyKnockback(dirToEnemy * rammingKnockbackForce, 0.2f).Forget();

            // 현재 시간 + 쿨타임을 더해서 '만료 시각'을 기록
            _hitCooldowns[instanceId] = currentTime + damageCooldown;
        }
    }

    private void CleanUpExpiredCooldowns()
    {
        if (_hitCooldowns.Count == 0) return;

        float currentTime = Time.time;
        _keysToRemoveBuffer.Clear();

        // 딕셔너리를 순회하며 만료된(과거가 된) 키값만 찾아 버퍼에 담음
        foreach (var kvp in _hitCooldowns)
        {
            if (currentTime >= kvp.Value)
            {
                _keysToRemoveBuffer.Add(kvp.Key);
            }
        }

        for (int i = 0; i < _keysToRemoveBuffer.Count; i++)
        {
            _hitCooldowns.Remove(_keysToRemoveBuffer[i]);
        }
    }
}
