using UnityEngine;

public class RadarManager : MonoBehaviour
{
    public static RadarManager Instance { get; set; }

    [Header("Target Settings")]
    public Transform Actor;         // 플레이어 = 중심
    public Transform ActorCamera;   // 메인카메라 = 회전값 참조

    [Header("UI Refs")]
    public GameObject Panel_Radar;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (Actor == null && PlayerManager.Instance != null) Actor = PlayerManager.Instance.PlayerTransform;

        if (ActorCamera == null && Camera.main != null) ActorCamera = Camera.main.transform;

        if (Panel_Radar != null) Panel_Radar.SetActive(true); 
    }
}
