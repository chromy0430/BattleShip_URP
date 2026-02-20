using UnityEngine;
using System;
using BattleShip;
public class PlayerDataLoad : StaticInstance<PlayerDataLoad>
{
    public BaseShipData shipData;
    public PlayerStat CurrentPlayerStat { get; private set; }
    public event Action OnStatUpdated;

    protected override void Awake()
    {
        base.Awake();
        
        if (shipData == null)   Debug.LogError("데이터가 없습니다.");

        CurrentPlayerStat = new PlayerStat(shipData);
    }

    public void UpdateStatAndNotify()
    {
        if (OnStatUpdated != null && DebugManager.instance.IsDebug)
        {
            // 구독자 수 확인 (GetInvocationList 사용)
            int subscriberCount = OnStatUpdated.GetInvocationList().Length;
            Debug.Log($"[Event Check] 현재 구독자 수: {subscriberCount}명");

            foreach (var subscriber in OnStatUpdated.GetInvocationList())
            {
                Debug.Log($"[Event Check] 구독 중인 객체: {subscriber.Method.DeclaringType}.{subscriber.Method.Name}");
            }
        }

        OnStatUpdated?.Invoke();
    }
}
