using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class UpgradeCardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button selectButton;

    private UpgradeData _data;
    private Action<UpgradeData> _onSelected; // 클릭 시 실행

    public void Setup(UpgradeData data, Action<UpgradeData> onSelectedCallback)
    {
        _data = data;
        _onSelected = onSelectedCallback;

        titleText.text = data.upgradeName;

        string finalDesc = data.GetDescription();

        if (string.IsNullOrEmpty(data.relatedWeaponID) && PlayerDataLoad.playerData != null)
        {
            float currentVal = PlayerDataLoad.playerData.CurrentPlayerStat.GetCurrentStatValue(data.targetStat);

            // 소수점 1자리까지만 예쁘게 포맷팅 (예: 12.5)
            // 색상을 넣어서 강조 (Rich Text 사용)
            finalDesc += $"\n<size=80%><color=#FFFF00>(현재 값 : {currentVal:F1})</color></size>";
        }

        descText.text = finalDesc;

        if (data.icon != null) iconImage.sprite = data.icon;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        _onSelected?.Invoke(_data);
    }
}