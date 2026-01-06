using UnityEngine;

public class DebugManager : MonoBehaviour
{
    public static DebugManager instance { get; set; }

    [Header("디버그 용")]
    public bool IsDebug = false;
    
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        QualitySettings.vSyncCount = 0;
    }
}
