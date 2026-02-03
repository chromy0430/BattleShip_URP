using UnityEngine;
using System.Collections.Generic;

public class AuraWeapon : MonoBehaviour
{
    private WeaponStat _stat;
    private List<IEnemy> _targetsInRange = new List<IEnemy>();
    private float _tickTimer;
    private float _tickInterval;

    [Header("Visual Settings")]
    [Tooltip("사거리 1당 적용될 실제 크기 비율")]
    [SerializeField] private float _rangeToScaleRatio = 0.01f;

    [Tooltip("프리팹의 기본 스케일 (초기값 복원용)")]
    [SerializeField] private Vector3 _baseScale = new Vector3(25, 25, 25);

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
    }

    private void OnEnable()
    {
        // 켜질 때 목록 초기화
        _targetsInRange.Clear();
        _tickTimer = _tickInterval;
    }

    #region 트리거 함수

    // 트리거 진입
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out IEnemy enemy))
        {
            if (enemy.IsAlive && !_targetsInRange.Contains(enemy))
            {
                _targetsInRange.Add(enemy);
            }
        }
    }

    // 트리거 이탈
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out IEnemy enemy))
        {
            if (_targetsInRange.Contains(enemy))
            {
                _targetsInRange.Remove(enemy);
            }
        }
    }
    #endregion

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
        int finalDamage = _stat.Damage;
        if (PlayerDataLoad.playerData != null)
        {
            finalDamage = PlayerDataLoad.playerData.CurrentPlayerStat.GetFinalDamage(_stat.Damage);
        }

        for (int i = _targetsInRange.Count - 1; i >= 0; i--)
        {
            var enemy = _targetsInRange[i];

            if (enemy == null || !enemy.IsAlive)
            {
                _targetsInRange.RemoveAt(i);
                continue;
            }

            Vector3 knockbackDir = (enemy.TargetTransform.position - transform.position).normalized;
            enemy.TakeDamage(finalDamage, out int finalDmg);
            enemy.ApplyKnockback(knockbackDir * 1.2f, 0.2f);
        }
    }
}