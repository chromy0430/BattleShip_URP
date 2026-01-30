using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI turnSpeedText;
    [SerializeField] private TextMeshProUGUI magnetRangeText;
    [SerializeField] private TextMeshProUGUI maxHPText;
    [SerializeField] private TextMeshProUGUI maxMPText;
    [SerializeField] private TextMeshProUGUI defText;
    [SerializeField] private TextMeshProUGUI SPRText;

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

        if (stat.bonusTurnSpeed > 0)
        {
            turnSpeedText.SetText($"TurnSpeed: {stat.shipTurnSpeed + stat.bonusTurnSpeed} <color=green>(+{stat.bonusTurnSpeed})</color>");
        }
        else
        {
            turnSpeedText.SetText($"TurnSpeed: {stat.shipTurnSpeed + stat.bonusTurnSpeed}");
        }

        if (stat.bonusMagnetRange > 0)
        {
            magnetRangeText.SetText($"MagnetRange: {stat.GetTotalMagnetRange()} <color=green>(+{stat.bonusMagnetRange})</color>");
        }
        else
        {
            magnetRangeText.SetText($"MagnetRange: {stat.shipMagnetRange}");
        }

        if (stat.bonusHP > 0)
        {
            maxHPText.SetText($"MaxHP: {stat.GetFinalMaxHP()} <color=green>(+{stat.bonusHP})</color>");
        }
        else
        {
            maxHPText.SetText($"MaxHP: {stat.GetFinalMaxHP()}");
        }

        if (stat.bonusMP > 0)
        {
            maxMPText.SetText($"MaxMP: {stat.GetFinalMaxMP()} <color=green>(+{stat.bonusMP})</color>");
        }
        else
        {
            maxMPText.SetText($"MaxMP: {stat.GetFinalMaxMP()}");
        }

        if (stat.bonusDEF > 0)
        {
            defText.SetText($"DEF: {stat.shipDEF + stat.bonusDEF} <color=green>(+{stat.bonusDEF})</color>");
        }
        else
        {
            defText.SetText($"DEF: {stat.shipDEF}");
        }

        if (stat.bonusSPR > 0)
        {
            SPRText.SetText($"SPR: {stat.shipSPR + stat.bonusSPR} <color=green>(+{stat.bonusSPR})</color>");
        }
        else
        {
            SPRText.SetText($"SPR: {stat.shipSPR}");
        }

        
    }
}
