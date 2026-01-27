using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI speedText;

    private void Start()
    {
        if (PlayerDataLoad.playerData != null)
        {
            PlayerDataLoad.playerData.OnStatUpdated += RefreshUI;
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        PlayerStat stat = PlayerDataLoad.playerData.CurrentPlayerStat;

        if (stat.bonusMoveSpeed > 0)
        {
            speedText.text = $"Speed: {stat.shipMoveSpeed + stat.bonusMoveSpeed} <color=green>(+{stat.bonusMoveSpeed})</color>";
        }
        else
        {
            speedText.text = $"Speed: {stat.shipMoveSpeed + stat.bonusMoveSpeed}";
        }
    }
}
