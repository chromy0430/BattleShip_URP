using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class DebugManager : MonoBehaviour
{
    public static DebugManager instance { get; set; }

    public EnemyManager enemyManager;

    [Header("디버그 용")]
    public bool IsDebug = false;

    public TextMeshProUGUI enemyCount;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this);

        QualitySettings.vSyncCount = 0;
    }

    private void Update()
    {
        enemyCount.SetText("Enemy Count : " + enemyManager.ActiveEnemies.Count.ToString());
    }
}
