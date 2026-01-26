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
    private Action<UpgradeData> _onSelected; // 클릭 시 실행할 함수(대리자)

    // 매니저가 이 함수를 호출해서 정보를 꽂아줍니다. (Dependency Injection)
    public void Setup(UpgradeData data, Action<UpgradeData> onSelectedCallback)
    {
        _data = data;
        _onSelected = onSelectedCallback;

        // UI 갱신
        titleText.text = data.upgradeName;
        descText.text = data.description;
        if (data.icon != null) iconImage.sprite = data.icon;

        // 버튼 리스너 초기화
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        // 매니저에게 "이 데이터가 선택되었어!"라고 보고
        _onSelected?.Invoke(_data);
    }
}