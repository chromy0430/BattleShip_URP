using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 배의 각 무기 슬롯(포탑)에 부착되는 스크립트입니다.
/// 장착된 무기(ScriptableObject)의 데이터를 가지며,
/// AutoAttackController로부터 발사 명령을 받습니다.
/// </summary>
public class WeaponMount : MonoBehaviour
{
    [Header("무기 데이터 (SO)")]
    [Tooltip("장착할 무기의 ScriptableObject 데이터")]
    [SerializeField] private BaseWeaponData _weaponData;
    private WeaponStat _runtimeStat;
    public WeaponStat RuntimeStat => _runtimeStat;

    [Header("발사 위치")]
    [Tooltip("발사체가 생성될 위치")]
    public Transform firePoint;

    // 내부 공격 쿨타임 타이머
    private float _currentCooldown = 0f;
    private float _attackInterval = 0f;
    private bool _isReadyToFire = true;

    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// 무기 데이터를 기반으로 쿨타임 등 초기 설정
    /// </summary>
    public void Initialize()
    {
        if (_weaponData != null)
        {
            _runtimeStat = new WeaponStat(_weaponData);

            if (_runtimeStat.FireRate > 0)
            {
                _attackInterval = 1f / _runtimeStat.FireRate;
            }
            else
            {
                _attackInterval = float.MaxValue;
            }
        }

        _currentCooldown = 0f;
        _isReadyToFire = true;
    }

    /// <summary>
    /// FireRate가 강화되었을 때,이미 장착된 무기의 공격 간격 갱신
    /// </summary>
    public void InitializeCooldown()
    {
        if (_runtimeStat != null && _runtimeStat.FireRate > 0)
        {
            // 강화된 FireRate 수치를 실제 시간 간격으로 변환
            _attackInterval = 1f / _runtimeStat.FireRate;
        }
        else
        {
            _attackInterval = float.MaxValue; // 발사 불가 상태
        }
    }

    /// <summary>
    /// 매 프레임 쿨타임을 감소시킵니다.
    /// </summary>
    /// <returns>발사 준비가 되었는지 여부</returns>
    public bool TickCooldown(float deltaTime)
    {
        if (_isReadyToFire) return true;

        _currentCooldown -= deltaTime;
        if (_currentCooldown <= 0f)
        {
            _isReadyToFire = true;
        }

        return _isReadyToFire;
    }

    /// <summary>
    /// AutoAttackController에 의해 호출됩니다.
    /// 실제 발사 로직을 수행합니다.
    /// </summary>
    /// <param name="target">공격할 대상</param>
    public void Fire(List<IEnemy> targets)
    {
        if (_runtimeStat == null || targets == null || targets.Count == 0) return;
        
        _isReadyToFire = false;
        _currentCooldown = _attackInterval;

        Transform fp = (firePoint != null) ? firePoint : this.transform;

        foreach (IEnemy target in targets)
        {
            if (target == null || !target.IsAlive) continue;

            fp.LookAt(target.TargetTransform);

            GameObject projectileGO = ObjectPoolManager.Instance.Get(_runtimeStat.projectilePrefab);
            if (projectileGO == null) continue;

            projectileGO.transform.position = fp.position;
            projectileGO.transform.rotation = fp.rotation;

            Projectile projectile = projectileGO.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Launch(_runtimeStat, target, firePoint);
            }
            else
            {
                Debug.LogError($"{_runtimeStat.projectilePrefab.name} 프리팹에 Projectile.cs 스크립트가 없습니다.");
                ObjectPoolManager.Instance.Return(projectileGO);
            }
        }

        // (임시) 발사 로직 로그
        //Debug.Log($"{WeaponData.Name}이(가) {target.TargetTransform.name}을(를) 향해 발사!");

        // TODO: 발사 이펙트/사운드 풀링 (muzzleEffect, muzzleSfx)
        // if (WeaponData.muzzleEffect) ...
        // if (WeaponData.muzzleSfx) ...
    }
}