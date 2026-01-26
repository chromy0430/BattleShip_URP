using UnityEngine;
using System;
public class PlayerDataLoad : MonoBehaviour
{
    public static PlayerDataLoad playerData { get; set; }

    public BaseShipData shipData;
    public ShipStat CurrentShipStat { get; private set; }
    public event Action OnStatUpdated;

    private void Awake()
    {
        if(playerData == null)
        {
            playerData = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
            return;
        }
        
        if (shipData == null)
            Debug.LogError("데이터가 없습니다.");

        CurrentShipStat = new ShipStat(shipData);
    }

    public void UpdateStatAndNotify()
    {
        OnStatUpdated?.Invoke();
    }
}
