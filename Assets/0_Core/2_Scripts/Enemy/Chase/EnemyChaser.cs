using UnityEngine;
using StylizedWater3;

public class EnemyChaser : MonoBehaviour
{
    
    public float moveSpeed = 30f; // 속도        
    public float turnSpeed = 3.0f; // 회전 속도
    // 회전을 멈출 거리 - stopDistance 거리 안에 있을 시 회전 없이 직진, 아니면 회전까지 해줌
    public float stopDistance = 5.0f;

    [Header("부력 관련")]
    public float surfaceSize = 1.0f; // 배의 너비
    public float heightOffset = 0.0f; // 배의 부력 적용 높이

    public Transform cachedTransform;
    public Rigidbody rb;    
    public Enemy status;

    public float currentRotationVelocity;

    private void Awake()
    {
        cachedTransform = transform;
        rb = GetComponent<Rigidbody>();
        status = GetComponent<Enemy>();
    }

    private void Start()
    {
        if (EnemyBatchManager.Instance != null)
        {
            EnemyBatchManager.Instance.Register(this);
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