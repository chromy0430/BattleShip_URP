using UnityEngine;
using System.Collections.Generic;

public class EnemyBatchManager : MonoBehaviour
{
    public static EnemyBatchManager Instance { get; private set; }

    private readonly List<EnemyChaser> _enemies = new List<EnemyChaser>(1000);
    [SerializeField] private Transform _playerTransform;

    private void Awake()
    {
        Instance = this;

        // 레이스 컨디션 발생 - 후에 로직 변경
        /*if (PlayerManager.Instance != null)
        {
            _playerTransform = PlayerManager.Instance.PlayerTransform;
        }
        else
        {
            Debug.Log("플레이어매니저 작동 안됌");
            _playerTransform = FindAnyObjectByType<PlayerManager>().PlayerTransform;
        }*/
    }


    public void Register(EnemyChaser enemy) => _enemies.Add(enemy);
    public void Unregister(EnemyChaser enemy) => _enemies.Remove(enemy);

    private void FixedUpdate()
    {
        if (_playerTransform == null || _enemies.Count == 0) return;

        Vector3 playerPos = _playerTransform.position;
        float deltaTime = Time.fixedDeltaTime;

        // 700마리의 루프를 단 한 번의 FixedUpdate에서 수행
        for (int i = 0; i < _enemies.Count; i++)
        {
            var e = _enemies[i];
            if (!e.status.IsAlive)
            {
                e.rb.linearVelocity = Vector3.Lerp(e.rb.linearVelocity, Vector3.zero, deltaTime * 2f);
                continue;
            }

            Vector3 enemyPos = e.cachedTransform.position;
            Vector3 diff = playerPos - enemyPos;
            float distSqr = diff.sqrMagnitude;
            diff.y = 0;

            // 회전 - AlignToWater
            if (diff != Vector3.zero)
            {
                float targetYAngle = Quaternion.LookRotation(diff).eulerAngles.y;
                e.atw.rotation = Mathf.SmoothDampAngle(
                    e.atw.rotation,
                    targetYAngle,
                    ref e.currentRotationVelocity,
                    0.5f / e.turnSpeed
                );
            }

            // 이동 - Rigidbody
            if (distSqr > e.stopDistance * e.stopDistance)
            {
                e.rb.linearVelocity = e.cachedTransform.forward * e.moveSpeed;
            }
            else
            {
                e.rb.linearVelocity = Vector3.MoveTowards(e.rb.linearVelocity, Vector3.zero, deltaTime * 10f);
            }
        }
    }
}