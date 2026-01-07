using UnityEngine;
using StylizedWater3;

public class EnemyChaser : MonoBehaviour
{
    // 속도
    public float moveSpeed = 30f;

    // 회전속도
    public float turnSpeed = 3.0f;

    // 회전을 멈출 거리 - stopDistance 거리 안에 있을 시 회전 없이 직진, 아니면 회전까지 해줌
    public float stopDistance = 5.0f;

    public Rigidbody rb;
    public AlignToWater atw;
    public Enemy status;
    public float currentRotationVelocity;
    public Transform cachedTransform;

    private void Awake()
    {
        cachedTransform = transform;
        rb = GetComponent<Rigidbody>();
        atw = GetComponent<AlignToWater>();
        status = GetComponent<Enemy>();
    }

    private void OnEnable()
    {
        if (EnemyBatchManager.Instance != null)
        {
            EnemyBatchManager.Instance.Register(this);
        }
    }

    private void OnDisable()
    {
        if (EnemyBatchManager.Instance != null)
        {
            EnemyBatchManager.Instance.Unregister(this);
        }
    }
}