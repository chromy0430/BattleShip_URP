using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponInfoSlot : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private Image iconImage; // 무기 아이콘
    [SerializeField] private TextMeshProUGUI levelText; // 무기 현재 레벨

    [Header("무기 슬롯 이미지")]
    [SerializeField] private Sprite emptySlotIcon; // 빈 슬롯일 때 보여줄 이미지 (투명 or 기본 아이콘)
    [SerializeField] private Color emptyColor = new Color(0f, 0f, 0f, 64f / 255f); // 빈 슬롯 색상 (반투명)
    [SerializeField] private Color activeColor = Color.white; // 활성 슬롯 색상

    private WeaponMount _targetMount;

    public void Initialize(WeaponMount mount)
    {
        _targetMount = mount;
        RefreshUI();
    }

    // 데이터 갱신
    public void RefreshUI()
    {
        if (_targetMount == null) return;

        // 무기가 없는 경우 빈 슬롯 처리
        if (!_targetMount.HasWeapon())
        {
            SetEmptyState();
            return;
        }

        // 무기가 있는 경우 정보 표시
        SetActiveState(_targetMount.RuntimeStat);
    }

    // 빈 슬롯 함수
    private void SetEmptyState()
    {
        if (iconImage != null)
        {
            if (emptySlotIcon != null)
            {
                iconImage.sprite = emptySlotIcon;
                iconImage.color = emptyColor; // 반투명하게             
            }
            else
            {
                iconImage.sprite = null;
                iconImage.color = emptyColor;
            }
        }

        iconImage.enabled = true;

        if (levelText != null)
        {
            levelText.SetText("");
        }
    }

    // 무기 획득 시 변경
    private void SetActiveState(WeaponStat stat)
    {
        if (stat == null) return;

        // 아이콘 설정
        if (iconImage != null)
        {
            iconImage.enabled = true;
            iconImage.color = activeColor;
            if (stat.Icon != null)
            {
                iconImage.sprite = stat.Icon;
            }
        }

        if (levelText != null)
        {
            levelText.SetText($"Lv. {stat.WeaponLevel}");
        }
    }
}
