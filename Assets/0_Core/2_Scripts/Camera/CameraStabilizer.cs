using UnityEngine;

public class CameraStabilizer : MonoBehaviour
{
    [SerializeField] private Transform targetBoat;

    private void LateUpdate()
    {
        if (targetBoat == null) return;

        // 멀미의 원천은 배의 흔들림(y값)이므로, 카메라가 배를 따라가되 y값은 바뀌지 않도록 0으로 고정
        transform.position = new Vector3(targetBoat.position.x, 0, targetBoat.position.z);
        Vector3 targetEuler = targetBoat.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, targetEuler.y, 0f);
    }    
}
