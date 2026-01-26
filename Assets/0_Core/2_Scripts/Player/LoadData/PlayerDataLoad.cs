using UnityEngine;

public class PlayerDataLoad : MonoBehaviour
{
    public static PlayerDataLoad playerData { get; set; }

    public BaseShipData shipData;

    private void Awake()
    {
        if(playerData == null)
        {
            playerData = this;
        }
        else
        {
            Destroy(this);
        }

        
        if (shipData == null)
            Debug.LogError("데이터가 없습니다.");   
    }
}
