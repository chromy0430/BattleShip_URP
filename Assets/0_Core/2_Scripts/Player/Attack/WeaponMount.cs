using Battleship_Enum;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 배의 각 무기 슬롯(포탑)에 부착되는 스크립트
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

    private AuraWeapon _activeAura;
    private Coroutine _auraRoutine;

    #region 초기화
    /// <summary>
    /// 무기 데이터를 기반으로 쿨타임 등 초기 설정
    /// </summary>
    public void Initialize()
    {
        if (_weaponData != null)
        {
            if (_runtimeStat == null)
            {
                _runtimeStat = new WeaponStat(_weaponData);
            }

            
            InitializeCooldown();

            // 무기 타입에 따른 초기화 분기
            if (_weaponData.weaponType == WeaponType.Pulse)
            {
                if (_activeAura == null) // 없을 때만 만듦
                {
                    CreateAuraWeapon();
                }
                else
                {
                    // 이미 있다면 스탯만 최신 버전으로 주입
                    _activeAura.Initialize(_runtimeStat);
                }

                // 코루틴이 꺼져있다면 재가동
                if (_auraRoutine == null && gameObject.activeInHierarchy)
                {
                    _auraRoutine = StartCoroutine(AuraLifecycleLoop());
                }
            }
        }
    }

    private IEnumerator AuraLifecycleLoop()
    {
        while (true)
        {
            // Duration 동안 켜짐
            if (_activeAura != null) _activeAura.gameObject.SetActive(true);
            yield return new WaitForSeconds(_runtimeStat.Duration);

            // Cooltime 동안 꺼짐
            if (_activeAura != null) _activeAura.gameObject.SetActive(false);
            yield return new WaitForSeconds(_runtimeStat.Cooltime);
        }
    }

    private void CreateAuraWeapon()
    {
        if (_runtimeStat.projectilePrefab == null) return;

        GameObject go = Instantiate(_runtimeStat.projectilePrefab, transform);
        go.transform.localPosition = Vector3.zero;        

        _activeAura = go.GetComponent<AuraWeapon>();
        if (_activeAura != null)
        {
            _activeAura.Initialize(_runtimeStat);
        }
    }

    /// <summary>
    /// FireRate가 강화되었을 때,이미 장착된 무기의 공격 간격 갱신
    /// </summary>
    public void InitializeCooldown()
    {
        if (_runtimeStat != null && _runtimeStat.FireRate > 0)
        {
            _attackInterval = 1f / _runtimeStat.FireRate;
        }
        else
        {
            _attackInterval = float.MaxValue;
        }
        _currentCooldown = 0f;
        _isReadyToFire = true;
    }
    #endregion

    #region 발사 관련
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
        if (_weaponData.weaponType == WeaponType.Pulse) return;

        if (_runtimeStat == null || targets == null || targets.Count == 0) return;
        
        _isReadyToFire = false;
        _currentCooldown = _attackInterval;

        Transform fp = (firePoint != null) ? firePoint : this.transform;

        foreach (IEnemy target in targets)
        {
            if (target == null || !target.IsAlive) continue;

            fp.LookAt(target.TargetTransform);

            GameObject projectileGO = AddressablePoolManager.Instance.Get(_runtimeStat.projectilePrefab);
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
                AddressablePoolManager.Instance.Return(projectileGO);
            }
        }

        // (임시) 발사 로직 로그
        //Debug.Log($"{WeaponData.Name}이(가) {target.TargetTransform.name}을(를) 향해 발사!");

        // TODO: 발사 이펙트/사운드 풀링 (muzzleEffect, muzzleSfx)
        // if (WeaponData.muzzleEffect) ...
        // if (WeaponData.muzzleSfx) ...
    }
    #endregion

    #region 강화 관련
    public string GetWeaponID()
    {
        return _weaponData != null ? _weaponData.ID : "";
    }

    public void ApplyUpgradeBonus(StatType type, float amount)
    {
        if (_runtimeStat == null) return;

        switch (type)
        {
            case StatType.Damage:
                _runtimeStat.Damage += (int)amount; // 혹은 퍼센트 로직
                if (AuraCheck()) _activeAura.Initialize(_runtimeStat);
                break;
            case StatType.FireRate: // 투사체 초당 발사 횟수 증가
                _runtimeStat.FireRate += amount;
                InitializeCooldown(); // 쿨타임 재계산 필수
                if (AuraCheck()) _activeAura.Initialize(_runtimeStat);
                break;
            case StatType.ProjectileSpeed: // 투사체 발사 속도 증가
                _runtimeStat.projectileSpeed += amount;
                break;
            case StatType.PierceCount: // 적 관통 수 증가
                _runtimeStat.PierceCount += (int)amount;
                break;
            case StatType.MultiProjectile: // 다수 적 공격 수 증가
                _runtimeStat.EnemyCount += (int)amount;
                break;
            case StatType.Acceleration: // 투사체 가속력 증가
                _runtimeStat.Acceleration += amount;
                break;
            case StatType.WeaponRange: // 사거리(오라 크기) 증가
                _runtimeStat.Range += (int)amount;
                if (AuraCheck()) _activeAura.Initialize(_runtimeStat);
                break;

            // 펄스형
            case StatType.Duration: // 지속시간 증가
                _runtimeStat.Duration += amount;
                if (AuraCheck()) _activeAura.Initialize(_runtimeStat);
                break;
            case StatType.CoolTime: // 재사용 대기시간 감소
                _runtimeStat.Cooltime -= amount;
                if (AuraCheck()) _activeAura.Initialize(_runtimeStat);
                break;
        }
    }
    #endregion

    #region 헬퍼 함수
    private bool AuraCheck()
    {
        return _activeAura != null;
    }

    public WeaponType GetWeaponType()
    {
        return _weaponData != null ? _weaponData.weaponType : WeaponType.Gun;
    }

    public bool HasWeapon()
    {
        return _weaponData != null;
    }

    public bool IsMaxLevel()
    {
        return (_runtimeStat != null && _weaponData != null && _runtimeStat.WeaponLevel >= _weaponData.MaxLevel);
    }

    // 무기 레벨업
    public void LevelUp()
    {
        if (_runtimeStat == null) return;

        if (_runtimeStat.WeaponLevel >= _weaponData.MaxLevel)
        {
            // TODO 만렙일 경우 대비, 보상로직 추가 가능
            // 만렙일 경우에 레벨업 시 체력, 마나 회복 또는 골드 증가

            return;
        }

        // 무기 레벨업
        _runtimeStat.WeaponLevel++;

        // 무기 강화 적용
        if (_weaponData != null && _weaponData.growthPerLevel != null)
        {
            foreach (var modifier in _weaponData.growthPerLevel)
            {
                ApplyUpgradeBonus(modifier.targetStat, modifier.value);
            }
        }

        // TODO 무기 레벨업 시 이펙트, 사운드 추가시 여기서 추가
    }

    public void EquipWeapon(BaseWeaponData newData)
    {
        _weaponData = newData;
        _runtimeStat = new WeaponStat(_weaponData);

        Initialize();

        if(UpgradeManager.Instance != null) UpgradeManager.Instance.RefreshWeaponList();
    }
    #endregion

    private void OnDisable()
    {
        if (_auraRoutine != null) StopCoroutine(_auraRoutine);
        if (_activeAura != null) _activeAura.gameObject.SetActive(false);
    }
}