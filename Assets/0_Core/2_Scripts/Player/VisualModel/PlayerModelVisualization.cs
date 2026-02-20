using MoreMountains.Feedbacks;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerModelVisualization : MonoBehaviour
{
    private GameObject shipModel;

    [SerializeField] MMF_Player modelFeedback;

    private void Start()
    {
        if (PlayerManager.Instance != null) PlayerManager.Instance.getDamage += DamageFeedback;

        shipModel = PlayerDataLoad.Instance.shipData.shipVisual;

        GameObject ship = Instantiate(shipModel, this.transform);
        ship.transform.localPosition = shipModel.transform.position;
        ship.transform.localRotation = shipModel.transform.rotation;

        if (modelFeedback == null) modelFeedback.GetComponent<MMF_Player>();

        MMF_Flicker flickerFeedback = modelFeedback.GetFeedbackOfType<MMF_Flicker>();
        flickerFeedback.BoundRenderer = ship.GetComponent<MeshRenderer>();
    }

    private void OnDisable()
    {
        if (PlayerManager.Instance != null) PlayerManager.Instance.getDamage -= DamageFeedback;
    }

    private void DamageFeedback()
    {       
        modelFeedback?.PlayFeedbacks();
    }
}
