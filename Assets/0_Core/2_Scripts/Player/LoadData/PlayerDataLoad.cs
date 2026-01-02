using UnityEngine;

public class PlayerDataLoad : MonoBehaviour
{
    public BaseShipData shipData;

    private void Awake()
    {
        if (shipData == null)
            Debug.LogError("데이터가 없습니다.");   
    }
}
