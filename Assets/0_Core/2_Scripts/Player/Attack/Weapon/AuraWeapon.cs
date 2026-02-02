using UnityEngine;
using System.Collections.Generic;

public class AuraWeapon : MonoBehaviour
{
    private WeaponStat _stat;
    private HashSet<IEnemy> _targetsInRagne = new HashSet<IEnemy>(); // 영역 내 적 목록
    private float _tickTimer;
    private float _tickInterval;

    [Header("Visual Settings")]
    [Tooltip("사거리 1당 적용될 실제 크기 비율")]
    [SerializeField] private float _rangeToScaleRatio = 0.01f;

    [Tooltip("프리팹의 기본 스케일 (초기값 복원용)")]
    [SerializeField] private Vector3 _baseScale = new Vector3(20, 20, 20);

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
        
        //transform.localScale = this.transform.localScale * finalScale;
    }

    private void OnEnable()
    {
        // 켜질 때 목록 초기화
        _targetsInRagne.Clear();
        _tickTimer = _tickInterval;
    }

    #region 트리거 함수
    // 영역 진입 감지
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out IEnemy enemy))
        {
            if (enemy.IsAlive) _targetsInRagne.Add(enemy);
        }
    }

    // 영역 이탈 감지
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out IEnemy enemy))
        {
            _targetsInRagne.Remove(enemy);
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
        // 죽은 적이나 null인 적은 리스트에서 청소
        _targetsInRagne.RemoveWhere(e => e == null || !e.IsAlive);

        int finalDamage = _stat.Damage;

        if (PlayerDataLoad.playerData != null)
        {
            // PlayerStat에 있는 데미지 공식 활용 (기본공격력 * 보너스%)
            finalDamage = PlayerDataLoad.playerData.CurrentPlayerStat.GetFinalDamage(_stat.Damage);
        }

        // 범위 내 모든 적에게 데미지
        foreach (var enemy in _targetsInRagne)
        {
            Vector3 knockbackDir = (this.transform.position - transform.position).normalized;            

            enemy.TakeDamage(finalDamage, out int finalDmg);
            enemy.ApplyKnockback(knockbackDir * 1.2f, 0.2f);
        }
    }
}