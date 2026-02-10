using UnityEngine;
using UnityEngine.Serialization;
using Battleship_Enum;
using UnityEditor;

[CreateAssetMenu(fileName = "ShipData_", menuName = "BattleShip/Ship Data", order = 1)]
public class BaseShipData : ScriptableObject
{
    [Header("배 속성")]

    [FormerlySerializedAs("shipID")]
    public string shipID;
    [FormerlySerializedAs("ShipName")]
    public string ShipName;
    [FormerlySerializedAs("ShipDescription")]
    public string ShipDescription;

    public ShipSize shipSize;

    public int BaseHP; // 배 체력
    public int BaseMP; // 배 마나
    public int BaseDEF; // 배 방어력
    public int BaseMagnetRange; // 배 자석 범위
    public int BaseSPR; // 체력 재생 속도
    public int ShopCost; // 상점 가격

    
    public float BaseMoveSpeed = 10f; // 배 기본 속도
    public float BaseTurnSpeed = 10f; // 배 회전 속도
        
    [Header("배 시각화")]
    public GameObject shipVisual; // 배 모습
    public Sprite shipIcon; // 배 아이콘
    public Vector3 ColliderCenter; // 콜라이더 센터
    public Vector3 ColliderSize; // 콜라이더 사이즈

    [Header("부력 관련")]
    public Vector2 BaseSurfaceSize; // 배 부력 면적 
    public float BaseHeightOffset; // 배 부력 높이
    public float rollAmount; // 부력 강도
    public float smoothing; // 부력 강도 조절 보간


#if UNITY_EDITOR
    private void OnValidate()
    {
        EditorUtility.SetDirty(this);
    }
#endif
}
