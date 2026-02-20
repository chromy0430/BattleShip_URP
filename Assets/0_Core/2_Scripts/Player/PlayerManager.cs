using BattleShip;
using System;
using UnityEngine;

public class PlayerManager : StaticInstance<PlayerManager>
{
    public Transform PlayerTransform { get; set; }
    
    protected override void Awake()
    {
        base.Awake();

        PlayerTransform = this.transform;
        autoAttackController = GetComponentInChildren<AutoAttackController>();
    }

    #region 무기 획득 함수
    // 무기 관련
    [Header("무기 시스템")]
    private AutoAttackController autoAttackController;

    // 외부(PlayerItemManager)에서 호출할 함수
    public bool TryAddWeapon(BaseWeaponData newWeaponData)
    {
        if (autoAttackController == null)
        {
            Debug.LogError("[PlayerManager] AutoAttackController 컴포넌트가 없습니다!");
            return false;
        }

        var mounts = autoAttackController.GetWeaponMounts();

        foreach (var mount in mounts)
        {
            // 같은 무기를 가지고 있는지 확인
            if (mount.HasWeapon() && mount.GetWeaponID() == newWeaponData.ID)
            {
                // 가지고 있다면 레벨업
                mount.LevelUp();
                return true;
            }
        }

        foreach (var mount in mounts)
        {
            // 무기가 장착되어 있지 않다면
            if (!mount.HasWeapon())
            {
                // 장착 실행
                mount.EquipWeapon(newWeaponData);
                Debug.Log($"[PlayerManager] {newWeaponData.Name} 장착 완료 (슬롯: {mount.name})");
                return true;
            }
        }

        Debug.Log("[PlayerManager] 무기 슬롯이 가득 차서 획득할 수 없습니다. (교체 불가)");
        return false;
    }

    #endregion

    #region 데미지 피해

    // 이벤트 관련
    public event Action getDamage;

    public void Damaged()
    {
        getDamage?.Invoke();
    }
    #endregion
}