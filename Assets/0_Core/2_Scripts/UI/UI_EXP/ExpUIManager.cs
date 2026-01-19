using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExpUIManager : MonoBehaviour
{
    public Image ExpFill;
    public TextMeshProUGUI LevelText;

    private void Start()
    {
        ExpChange(0, 10);

        if (PlayerExpManager.Instance != null)
            PlayerExpManager.Instance.onExpChanged += ExpChange;
    }
    private void OnDestroy()
    {
        if (PlayerExpManager.Instance != null)
            PlayerExpManager.Instance.onExpChanged -= ExpChange;
    }

    public void ExpChange(float currentExp, float maxExp)
    {
        if (maxExp == 0) maxExp = 1;

        float expPercentage = currentExp / maxExp;
        ExpFill.fillAmount = expPercentage;

        LevelText.text = string.Format(
            "Lv.{0} <color=#FFFF00>{1:0.0}</color>%",
            (PlayerExpManager.Instance.currentLevel + 1),
            expPercentage * 100f
        );
    }
}
