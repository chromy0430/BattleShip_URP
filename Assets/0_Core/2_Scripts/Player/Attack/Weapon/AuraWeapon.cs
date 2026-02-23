using UnityEngine;
using System.Collections.Generic;

public class AuraWeapon : MonoBehaviour
{
    private WeaponStat _stat;
    private float _tickTimer;
    private float _tickInterval;

    [Header("Visual Settings")]
    [Tooltip("사거리 1당 적용될 실제 크기 비율")]
    [SerializeField] private float _rangeToScaleRatio = 0.01f;

    [Tooltip("프리팹의 기본 스케일 (초기값 복원용)")]
    [SerializeField] private Vector3 _baseScale = new Vector3(25, 25, 25);

    // 캐싱 버퍼
    private readonly List<EnemyChaser> _targetsBuffer = new List<EnemyChaser>(128);
    private float _actualHitRadius;

    // 무기 생성 시 스탯 주입
    public void Initialize(WeaponStat stat)
    {
        _stat = stat;

        // FireRate가 높을수록 데미지 주기가 빨라짐 (예: 2 -> 0.5초마다)
        _tickInterval = 1f / Mathf.Max(0.1f, stat.FireRate);

        // 사거리(Range)에 맞춰 크기 조절
        // Range 300 * 0.01 = 3배수
        float scaleMultiplier = stat.Range * _rangeToScaleRatio;
        transform.localScale = _baseScale * scaleMultiplier;

        // 로컬 스케일 x의 절반을 실제 타격 반경으로 캐싱
        _actualHitRadius = transform.localScale.x * 0.5f;
    }

    private void OnEnable()
    {
        _tickTimer = _tickInterval;
    }

    private void Update()
    {
        // 매 프레임 타이머 체크
        _tickTimer += Time.deltaTime;
        if (_tickTimer >= _tickInterval)
        {
            DealDamageToAll();
            _tickTimer = 0f;
        }
    }

    private void DealDamageToAll()
    {
        if (EnemyBatchManager.Instance == null) return;

        int finalDamage = _stat.Damage;
        if (PlayerDataLoad.Instance != null)
        {
            finalDamage = PlayerDataLoad.Instance.CurrentPlayerStat.GetFinalDamage(_stat.Damage);
        }

        EnemyBatchManager.Instance.GetEnemiesInRadius(transform.position, _actualHitRadius, _targetsBuffer);

        for (int i = _targetsBuffer.Count - 1; i >= 0; i--)
        {
            Enemy enemyStatus = _targetsBuffer[i].status;

            if (enemyStatus == null || !enemyStatus.IsAlive) continue;

            Vector3 knockbackDir = (enemyStatus.TargetTransform.position - transform.position).normalized;
            knockbackDir.y = 0f;

            enemyStatus.TakeDamage(finalDamage, out _);
            enemyStatus.ApplyKnockback(knockbackDir * 1.2f, 0.2f).Forget();
        }
    }
}