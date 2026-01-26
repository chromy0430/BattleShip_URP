using System.Collections.Generic;
using UnityEngine;

public delegate void OnChangedWeaponStat();

public class UpgradeManager : MonoBehaviour
{
    [SerializeField] private AutoAttackController attackController;

    public event OnChangedWeaponStat onChangedWeaponStat;

    private void Start()
    {
        if (PlayerExpManager.Instance != null)
        {
            PlayerExpManager.Instance.OnLevelUp += HandleLevelUp;
        }
    }

    private void OnDisable()
    {
        if (PlayerExpManager.Instance != null)
            PlayerExpManager.Instance.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        // 실제로는 여기서 선택지 UI를 띄워야 하지만,
        // 로직 확인을 위해 모든 무기의 데미지를 10% 올리는 예시를 작성합니다.
        //UpgradeAllWeaponsDamage(1.1f);

        UpgradeWeaponFireRate(0.1f);

        // 5레벨마다 모든 탄환에 유도 기능 부여 (동적 변화 테스트)
        if (newLevel % 5 == 0)
        {
            UnlockHomingUpgrade();
        }

        onChangedWeaponStat?.Invoke();
    }

    private void UpgradeWeaponFireRate(float multiplier)
    {
        var mounts = attackController.GetWeaponMounts();
        if (mounts == null || mounts.Count == 0) return;

        List<WeaponMount> abc = new(4);

        foreach (var item in mounts)
        {
            abc.Add(item);
        }

        if (abc[0].RuntimeStat.FireRate > 3.0f) return;

        abc[0].RuntimeStat.FireRate = abc[0].RuntimeStat.FireRate + multiplier;
        WeaponLevelUp(abc[0]);

        abc[0].InitializeCooldown();

        abc.Clear();
    }

    private void WeaponLevelUp(WeaponMount stat)
    {
        stat.RuntimeStat.WeaponLevel++;
    }

    private void UpgradeAllWeaponsDamage(float multiplier)
    {
        // AutoAttackController가 들고 있는 모든 마운트를 순회
        var mounts = attackController.GetWeaponMounts();

        foreach (var mount in mounts)
        {
            // [핵심] RuntimeStat을 직접 수정
            mount.RuntimeStat.Damage = Mathf.FloorToInt(mount.RuntimeStat.Damage * multiplier);

            // 공격 속도가 바뀌었다면 마운트 초기화가 필요할 수 있음
            mount.InitializeCooldown();

            Debug.Log($"[Upgrade] {mount.name} 강화! 데미지: {mount.RuntimeStat.Damage}");
        }
    }

    private void UnlockHomingUpgrade()
    {
        var mounts = attackController.GetWeaponMounts();
        foreach (var mount in mounts)
        {
            if (!mount.RuntimeStat.IsHoming)
            {
                mount.RuntimeStat.IsHoming = true;
                mount.RuntimeStat.HomingTurnSpeed = 180f; // 유도 성능 부여
                mount.RuntimeStat.Acceleration = 10f;
                Debug.Log($"[Upgrade] {mount.name} 유도 기능 잠금 해제!");
            }
        }
    }
}
