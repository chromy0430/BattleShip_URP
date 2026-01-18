using Cysharp.Threading.Tasks;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MonsterMovement : MonoBehaviour, IKnockbackable, IDamageable
{
    [SerializeField] private float hp = 100f;

    public GameObject DamagedVFX;
    public void TakeEffect(Vector3 hitPoint)
    {
        Instantiate(DamagedVFX, hitPoint, Quaternion.identity);        
    }

    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        hp -= amount;
        Debug.Log($"[Monster] {gameObject.name} took {amount} damage. Current HP: {hp}");

        // [Visual] 피격 이펙트 등을 hitPoint에 생성

        if (hp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // 사망 로직
        Destroy(this);
    }

    private Rigidbody _rb;
    private bool _isKnockedBack = false;

    // [Optimization] GC 방지를 위해 캐싱
    private WaitForFixedUpdate _waitFixedUpdate = new WaitForFixedUpdate();

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void ApplyKnockback(Vector3 sourcePosition, float distance, float duration)
    {        
        // .Forget() = 비동기 실행
        KnockbackRoutine(sourcePosition, distance, duration).Forget();
    }

    // 반환값이 없는 비동기 메서드
    private async UniTaskVoid KnockbackRoutine(Vector3 sourcePosition, float distance, float duration)
    {
        // 이미 넉백 중이면 중복 실행 방지
        if (_isKnockedBack) return;

        // 오브젝트가 파괴되면 자동으로 취소되는 토큰 생성
        var cancellationToken = this.GetCancellationTokenOnDestroy();

        _isKnockedBack = true;

        // 방향 및 목표 계산
        Vector3 direction = (transform.position - sourcePosition).normalized;
        direction.y = 0;
        Vector3 startPos = _rb.position;
        Vector3 targetPos = startPos + (direction * distance);

        float elapsed = 0f;

        try
        {
            while (elapsed < duration)
            {
                // FixedUpdate 타이밍 대기
                await UniTask.WaitForFixedUpdate(cancellationToken);

                elapsed += Time.fixedDeltaTime;
                float t = elapsed / duration;

                // 선형 보간 이동
                Vector3 currentFramePos = Vector3.Lerp(startPos, targetPos, t);
                _rb.MovePosition(currentFramePos);
            }

            // 최종 위치 보정
            _rb.MovePosition(targetPos);
        }
        catch (System.OperationCanceledException)
        {
            // 넉백 도중 몬스터가 죽거나 파괴되면 이쪽으로 넘어옴
            // 별도 처리가 필요 없다면 비워둬도 됨
        }
        finally
        {
            // 성공하든 취소되든 상태 복구
            _isKnockedBack = false;
        }
    }
}