using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using BattleShip;

public class UpgradeCardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button selectButton;

    private UpgradeData _upgradeData;
    private Action<UpgradeData> _onUpgradeSelected; // 클릭 시 실행

    private BaseItemData _itemData;
    private Action<BaseItemData> _onItemSelected;

    public void Setup(UpgradeData data, Action<UpgradeData> onSelectedCallback)
    {
        _upgradeData = data;
        _onUpgradeSelected = onSelectedCallback;

        _itemData = null;
        _onItemSelected = null;

        titleText.text = data.upgradeName;

        string finalDesc = data.GetDescription();

        // 디버그용, 후에 삭제 예정
        if (string.IsNullOrEmpty(data.relatedWeaponID) && PlayerDataLoad.Instance != null)
        {
            float currentVal = PlayerDataLoad.Instance.CurrentPlayerStat.GetCurrentStatValue(data.targetStat);
            finalDesc += $"\n<size=80%><color=#FFFF00>([디버그용] 현재 값 : {currentVal:F1})</color></size>";
        }

        descText.text = finalDesc;

        if (data.icon != null) iconImage.sprite = data.icon;

        RefreshButtonListener();
    }

    public void Setup(BaseItemData itemData, Action<BaseItemData> onSelectedCallback, string customDesc = "")
    {
        _itemData = itemData;
        _onItemSelected = onSelectedCallback;

        _upgradeData = null;
        _onUpgradeSelected = null;

        titleText.SetText(itemData.name);

        if (!string.IsNullOrEmpty(customDesc))
        {
            descText.SetText(customDesc);
        }
        else
        {
            descText.SetText(itemData.Description);
        }

        if (itemData.Icon != null) iconImage.sprite = itemData.Icon;

        RefreshButtonListener();
    }

    private void RefreshButtonListener()
    {
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (_upgradeData != null)
        {
            _onUpgradeSelected?.Invoke(_upgradeData);
        }
        else if (_itemData != null)
        {
            _onItemSelected?.Invoke(_itemData);
        }
    }
}