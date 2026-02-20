using UnityEngine;

public class EnemyChaser : MonoBehaviour
{
    [Header("이동 관련")]
    public float moveSpeed = 15f;
    public float turnSpeed = 8.0f;
    public float stopDistance = 3.5f;

    [Header("부력 관련")]
    public float surfaceSize = 1.0f;
    public float heightOffset = 0.0f;

    [HideInInspector] public Transform cachedTransform;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Collider bodyCollider;
    [HideInInspector] public Enemy status;
    [HideInInspector] public float cachedWaterY;

    private void Awake()
    {
        cachedTransform = transform;
        rb = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<Collider>();
        status = GetComponent<Enemy>();

        if (rb != null)
        {
            rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            cachedWaterY = rb.position.y;
        }
        else
        {
            cachedWaterY = transform.position.y;
        }
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