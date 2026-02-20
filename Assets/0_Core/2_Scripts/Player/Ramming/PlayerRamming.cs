using UnityEngine;

public class PlayerRamming : MonoBehaviour
{
    [SerializeField] PlayerMove playerMove;

    [Header("충각 관련")]
    [SerializeField] private float rammingThresholdSpeed = 20f; // 충각 발동 최소 속도
    [SerializeField] private float rammingDamageMultiplier = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float impactDirectionThreshold = 0.7f;
    [SerializeField] private float knockbackPower = 2f;

    private void Awake()
    {
        if (playerMove == null) playerMove = GetComponent<PlayerMove>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 상대방 확인
        if (collision.gameObject.TryGetComponent<IEnemy>(out var enemy))
        {
            // 속도 체크
            if (playerMove.currentSpeed >= rammingThresholdSpeed)
            {
                // 정면 충돌 판정
                Vector3 collisionDir = (collision.transform.position - transform.position).normalized;
                float dot = Vector3.Dot(transform.forward, collisionDir);

                if (dot >= impactDirectionThreshold)
                {
                    int damage = Mathf.FloorToInt(playerMove.currentSpeed * rammingDamageMultiplier);
                    enemy.TakeDamage(damage, out int finalDamage);

                    Vector3 knockbackDir = (collision.transform.position - transform.position).normalized;
                    float knockbackStrength = playerMove.currentSpeed * knockbackPower; // 속도 비례 힘

                    enemy.ApplyKnockback(knockbackDir * knockbackStrength);

                    Debug.Log($"[신뢰] 충각 성공! 방향 일치도: {dot:F2}, 최종 데미지: {finalDamage}");
                }
            }
        }
    }
}
