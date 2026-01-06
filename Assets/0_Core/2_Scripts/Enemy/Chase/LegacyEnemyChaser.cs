using UnityEngine;
using StylizedWater3;
using Cysharp.Threading.Tasks;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AlignToWater))]
[RequireComponent(typeof(Enemy))]
public class LegacyEnemyChaser : MonoBehaviour
{
    [Header("Chasing Settings")]
    [SerializeField] private float moveSpeed = 15f; // 적절한 추격 속도
    [SerializeField] private float turnSpeed = 2.0f; // 회전 반응 속도
    [SerializeField] private float stopDistance = 5.0f; // 공격 사거리 (이 거리 안에서는 멈춤)

    private Rigidbody _rb;
    private AlignToWater _atw;
    private Enemy _enemyStatus;
    private Transform _target;
    private Transform _transform;

    // 최적화를 위한 캐싱 변수
    private float _currentRotationVelocity; // SmoothDamp용

    private void Awake()
    {
        _transform = transform;
        _rb = GetComponent<Rigidbody>();
        _atw = GetComponent<AlignToWater>();
        _enemyStatus = GetComponent<Enemy>();

        //_target = PlayerManager.Instance.PlayerTransform;
    }

    private void OnEnable()
    {
        // 적이 활성화될 때마다 타겟(플레이어)을 다시 찾음 (오브젝트 풀링 대응)
        if (_target == null)
        {
            FindTarget();
        }
    }

    private void FindTarget()
    {
        if (PlayerManager.Instance != null && PlayerManager.Instance.PlayerTransform != null)
        {
            _target = PlayerManager.Instance.PlayerTransform;
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _target = playerObj.transform;
            }
            else
            {
                Debug.LogWarning($"[EnemyChaser] 타겟을 찾을 수 없습니다. {name}");
            }
        }
    }

    private void FixedUpdate()
    {
        // 1. 타겟이 없거나, 적이 죽었으면(IsAlive == false) 움직임 중단
        if (_target == null || !_enemyStatus.IsAlive)
        {
            // 사망 시 미끄러짐 방지 (원한다면 제거 가능)
            _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 2f);
            return;
        }

        // 2. 거리 및 방향 계산 (Y축 높이 무시 -> 물 위니까 X, Z만 중요)
        Vector3 directionToTarget = _target.position - _transform.position;
        float distanceSqr = directionToTarget.sqrMagnitude; // 거리 제곱 (성능상 sqrt 안 씀)

        directionToTarget.y = 0; // 높이 차이 무시하고 수평 방향만 봄

        // 3. 회전 로직 (AlignToWater 제어)
        if (directionToTarget != Vector3.zero)
        {
            // 목표 각도 계산
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            float targetYAngle = targetRotation.eulerAngles.y;

            // AlignToWater의 rotation 변수를 부드럽게 변경
            // Mathf.MoveTowardsAngle이나 SmoothDampAngle이 Lerp보다 회전에 더 적합함
            _atw.rotation = Mathf.SmoothDampAngle(_atw.rotation, targetYAngle, ref _currentRotationVelocity, 0.5f / turnSpeed);
        }

        // 4. 이동 로직 (Rigidbody 제어)
        // 공격 사거리보다 멀리 있을 때만 전진
        if (distanceSqr > stopDistance * stopDistance)
        {
            // PlayerMove와 동일하게 linearVelocity 사용 (Unity 6)
            _rb.linearVelocity = _transform.forward * moveSpeed;
        }
        else
        {
            // 사거리 내 도달 시 속도 줄임 (공격 태세)
            _rb.linearVelocity = Vector3.MoveTowards(_rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 10f);
        }
    }
}