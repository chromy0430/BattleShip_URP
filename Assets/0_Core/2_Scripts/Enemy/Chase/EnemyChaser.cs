using UnityEngine;
using StylizedWater3;

public class EnemyChaser : MonoBehaviour
{
    public float moveSpeed = 30f;
    public float turnSpeed = 3.0f;
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
            Debug.Log("EnemyBatchManager 등록");
        }
        else
        {
            Debug.Log("EnemyBatchManager 없음");
        }
    }

    private void OnDisable()
    {
        if (EnemyBatchManager.Instance != null)
        {
            EnemyBatchManager.Instance.Unregister(this);
            Debug.Log("EnemyBatchManager 해제");
        }
    }
}