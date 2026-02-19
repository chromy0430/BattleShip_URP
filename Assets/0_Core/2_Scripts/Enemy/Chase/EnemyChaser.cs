using UnityEngine;
using StylizedWater3;

public class EnemyChaser : MonoBehaviour
{
    
    public float moveSpeed = 30f; // 속도        
    public float turnSpeed = 3.0f; // 회전 속도
    // 회전을 멈출 거리 - stopDistance 거리 안에 있을 시 회전 없이 직진, 아니면 회전까지 해줌
    public float stopDistance = 5.0f;
    [SerializeField, Min(0f)] private float stopDistanceJitter = 1.25f;

    [Header("부력 관련")]
    public float surfaceSize = 1.0f; // 배의 너비
    public float heightOffset = 0.0f; // 배의 부력 적용 높이

    [Header("디버그 시각화")]
    [SerializeField] private bool drawLocalDebugGizmo = true;
    [SerializeField] private bool drawDebugWhenSelectedOnly = true;
    [SerializeField, Min(6)] private int debugCircleSegments = 48;

    public Transform cachedTransform;
    public Rigidbody rb;    
    public Collider bodyCollider;
    public Enemy status;
    public float cachedWaterY;
    public float effectiveStopDistance;
    public float orbitAngleDeg;
    public int orbitDirectionSign;

    public float currentRotationVelocity;

    private void Awake()
    {
        cachedTransform = transform;
        rb = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<Collider>();
        status = GetComponent<Enemy>();

        if (rb != null)
        {
            // 충돌 시 배가 기울어져 위로 올라타는 현상을 줄이기 위해 X/Z 회전을 고정한다.
            rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            cachedWaterY = rb.position.y;
        }
        else
        {
            cachedWaterY = transform.position.y;
        }

        RefreshStopDistance();
        RefreshFormationSlot();
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
        RefreshStopDistance();
        RefreshFormationSlot();
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

    private void RefreshStopDistance()
    {
        float jitter = stopDistanceJitter > 0f ? Random.Range(-stopDistanceJitter, stopDistanceJitter) : 0f;
        effectiveStopDistance = Mathf.Max(0.25f, stopDistance + jitter);
    }

    private void RefreshFormationSlot()
    {
        int seed = Mathf.Abs(GetInstanceID());
        orbitAngleDeg = seed % 360;
        orbitDirectionSign = ((seed & 1) == 0) ? 1 : -1;
    }

    private void OnDrawGizmosSelected()
    {
        if (drawDebugWhenSelectedOnly)
        {
            DrawDebugGizmosInternal();
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugWhenSelectedOnly)
        {
            DrawDebugGizmosInternal();
        }
    }

    private void DrawDebugGizmosInternal()
    {
        if (!drawLocalDebugGizmo) return;

        EnemyBatchManager mgr = EnemyBatchManager.Instance;
        if (mgr == null) return;

        if (!mgr.TryGetEnemyDebugInfo(
                this,
                out Vector3 playerPos,
                out float engageRadius,
                out float sepRadius,
                out Vector3 slotTargetPos,
                out float yOffset,
                out Color sepColor,
                out Color engageColor,
                out Color slotColor,
                out float overlapRadius,
                out Color overlapColor,
                out bool hasNearestEnemy,
                out Vector3 nearestEnemyPos,
                out float nearestDistance,
                out float nearestRequiredDistance))
        {
            return;
        }

        Vector3 enemyPos = transform.position + (Vector3.up * yOffset);
        Vector3 playerDrawPos = playerPos + (Vector3.up * yOffset);
        Vector3 slotDrawPos = slotTargetPos + (Vector3.up * yOffset);

        Gizmos.color = engageColor;
        DrawCircleXZ(playerDrawPos, engageRadius, debugCircleSegments);

        Gizmos.color = sepColor;
        Gizmos.DrawWireSphere(enemyPos, sepRadius);

        Gizmos.color = overlapColor;
        Gizmos.DrawWireSphere(enemyPos, overlapRadius);

        Gizmos.color = slotColor;
        Gizmos.DrawLine(enemyPos, slotDrawPos);
        Gizmos.DrawWireSphere(slotDrawPos, 0.18f);

        if (hasNearestEnemy)
        {
            Vector3 nearestDrawPos = nearestEnemyPos + (Vector3.up * yOffset);
            bool isOverlapped = nearestDistance < nearestRequiredDistance;
            Gizmos.color = isOverlapped ? Color.red : Color.green;
            Gizmos.DrawLine(enemyPos, nearestDrawPos);
        }
    }

    private void DrawCircleXZ(Vector3 center, float radius, int segments)
    {
        if (radius <= 0f || segments < 3) return;

        float step = 360f / segments;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float ang = step * i * Mathf.Deg2Rad;
            Vector3 next = center + new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
