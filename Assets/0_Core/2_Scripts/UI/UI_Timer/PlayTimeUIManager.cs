using BattleShip;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class PlayTimeUIManager : MonoBehaviour
{
    public TextMeshProUGUI playTimerText;

    private float _currentPlayTime = 0f;
    private bool _isTimeRunning = true;

    public float CurrentPlayTime => _currentPlayTime;

    void Update()
    {
        if (!_isTimeRunning) return;

        _currentPlayTime += Time.deltaTime;

        //UpdateTimeUI();
        playTimerText.SetText(Utils.FormatTime(_currentPlayTime));
    }

    /*private void UpdateTimeUI()
    {
        int min = Mathf.FloorToInt(_currentPlayTime / 60f);
        int sec = Mathf.FloorToInt(_currentPlayTime % 60f);

        playTimerText.SetText($"{min:D2}:{sec:D2}");
    }*/

    public void StopTimer()
    {
        _isTimeRunning = false;
    }
}
