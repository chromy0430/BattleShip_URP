using UnityEngine;
using System.Collections.Generic;

public class AuraWeapon : MonoBehaviour
{
    private WeaponStat _stat;
    private HashSet<IEnemy> _targetsInRagne = new HashSet<IEnemy>(); // 영역 내 적 목록
    private float _tickTimer;
    private float _tickInterval;

    // 무기 생성 시 스탯 주입
    public void Initialize(WeaponStat stat)
    {
        _stat = stat;

        // FireRate가 높을수록 데미지 주기가 빨라짐 (예: 2 -> 0.5초마다)
        _tickInterval = 1f / Mathf.Max(0.1f, stat.FireRate);

        // 사거리(Range)에 맞춰 크기 조절
        // 게임 스케일에 맞춰 보정값(0.01f 등)이 필요할 수 있습니다.
        // 현재는 Range 값을 그대로 Scale에 적용합니다.
        float finalScale = stat.Range * 0.01f; // 예: Range 300 -> Scale 3

        // 나중에 다시할 때 사이즈 변경
        //transform.localScale = Vector3.one * finalScale;
        transform.localScale = this.transform.localScale * finalScale;
    }

    private void OnEnable()
    {
        // 켜질 때 목록 초기화
        _targetsInRagne.Clear();
        //_tickTimer = 0f;
        _tickTimer = _tickInterval;
    }

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