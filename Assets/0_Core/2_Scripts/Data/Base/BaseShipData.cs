using UnityEngine;
using UnityEngine.Serialization;

public class BaseShipData : ScriptableObject
{
    [Header("배 속성")]

    [FormerlySerializedAs("shipID")]
    public string shipID;
    [FormerlySerializedAs("ShipName")]
    public string ShipName;
    [FormerlySerializedAs("ShipDescription")]
    public string ShipDescription;

    public float BaseHP; // 배 체력
    public float BaseDEF; // 배 방어력

    public virtual float BaseSpeed => baseSpeed;
    public virtual float BaseMinSpeed => baseMinSpeed;
    public virtual float BaseMaxSpeed => baseMaxSpeed;
    public virtual float BaseTurnSpeed => baseTurnSpeed;

    [SerializeField] protected float baseSpeed;      // 기본값 설정 가능
    [SerializeField] protected float baseMinSpeed;
    [SerializeField] protected float baseMaxSpeed;
    [SerializeField] protected float baseTurnSpeed;

    public float BaseSPR; // 체력 재생 속도
    
    public int ShopCost; // 상점 가격
    
    [Header("배 시각화")]
    public GameObject shipVisual; // 배 모습
    public Sprite shipIcon; // 배 아이콘

    protected virtual void ApplyTypeSpecificOverrides() { }

    private void Reset()
    {
        ApplyTypeSpecificOverrides(); // 컴포넌트 추가나 Reset 메뉴 클릭 시 딱 1번만 실행됨
    }

}
