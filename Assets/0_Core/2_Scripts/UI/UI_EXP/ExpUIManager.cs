using DG.Tweening.Core.Easing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExpUIManager : MonoBehaviour
{
    public Image ExpFill;
    public TextMeshProUGUI LevelText;

    private void Start()
    {
        ExpChange(0);
        PlayerExpManager.Instance.onExpChanged += ExpChange;
    }
    private void OnDestroy()
    {
        PlayerExpManager.Instance.onExpChanged -= ExpChange;
    }

    public void ExpChange(float exp)
    {
        float expPercentage = exp / 100f;
        ExpFill.fillAmount = expPercentage;
        LevelText.text = string.Format
        (
            "Lv.{0} <color=#FFFF00>{1:0.0}</color>%",
            (PlayerExpManager.Instance.currentLevel + 1),
            exp
        );
    }
}
