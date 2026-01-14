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

    [Header("Water System")]
    public float surfaceSize = 1.0f; // 배의 너비 (샘플링 범위)
    public float heightOffset = 0.0f;

    public Transform cachedTransform;
    public Rigidbody rb;    
    public Enemy status;
    public float currentRotationVelocity;
    public int buoyancyFrameOffset;

    //public AlignToWater atw;

    private void Awake()
    {
        cachedTransform = transform;
        rb = GetComponent<Rigidbody>();
        //atw = GetComponent<AlignToWater>();
        status = GetComponent<Enemy>();

        buoyancyFrameOffset = Random.Range(0, 4); // 4프레임 분산
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