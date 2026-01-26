using UnityEngine;

public class PlayerModelVisualization : MonoBehaviour
{
    private GameObject shipModel;

    private void Start()
    {
        shipModel = PlayerDataLoad.playerData.shipData.shipVisual;

        GameObject ship = Instantiate(shipModel, this.transform);
        ship.transform.localPosition = shipModel.transform.position;
        ship.transform.localRotation = shipModel.transform.rotation;
    }

}
