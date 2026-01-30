using UnityEngine;

namespace BattleShip
{
    public class Utils
    {
        public static string FormatTime(float time)
        {
            int min = Mathf.FloorToInt(time / 60f);
            int sec = Mathf.FloorToInt(time % 60f);
            return string.Format($"{min:D2}:{sec:D2}");
        }
    }
}
