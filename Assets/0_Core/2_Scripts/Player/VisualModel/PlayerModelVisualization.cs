using MoreMountains.Feedbacks;
using MoreMountains.Tools;
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

        //MMF_Flicker flickerFeedback = modelFeedback.GetFeedbackOfType<MMF_Flicker>();
        //flickerFeedback.BoundRenderer = ship.GetComponent<MeshRenderer>();

        MMF_ShaderController shaderFeedback = modelFeedback.GetFeedbackOfType<MMF_ShaderController>();
        shaderFeedback.TargetShaderController = ship.GetComponent<ShaderController>();

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
