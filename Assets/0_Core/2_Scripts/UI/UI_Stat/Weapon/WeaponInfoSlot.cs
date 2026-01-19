using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponInfoSlot : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image iconImage; // 무기 아이콘
    [SerializeField] private TextMeshProUGUI nameText; // 무기 이름
    [SerializeField] private TextMeshProUGUI LevelText; // 무기 현재 레벨
    [SerializeField] private TextMeshProUGUI damageText; // 무기 피해량
    [SerializeField] private TextMeshProUGUI fireRateText; // 무기 초당 발사 속도

    private WeaponMount _targetMount;

    public void Initialize(WeaponMount mount)
    {
        _targetMount = mount;
        RefreshUI();
    }

    // 데이터 갱신
    public void RefreshUI()
    {
        if (_targetMount == null || _targetMount.RuntimeStat == null) return;

        WeaponStat stat = _targetMount.RuntimeStat;

        // 아이콘 설정
        if (stat.Icon != null) iconImage.sprite = stat.Icon;

        nameText.text = stat.Name;

        LevelText.text = $"Lv.{stat.WeaponLevel}";

        // 스탯 표시
        damageText.text = $"DMG: {stat.Damage}";

        // 소수점 2자리까지만 예쁘게 표시 (F2)
        fireRateText.text = $"SPD: {stat.FireRate:F2}";
    }
}
