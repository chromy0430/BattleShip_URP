using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

public class HammerSkillSystem : MonoBehaviour
{
    [Header("Data Reference")]
    [SerializeField] private HammerSkillData skillData; 

    [Header("Optimization")]
    [SerializeField] private int maxColliders = 20;

    // 내부 상태 변수
    private float _lastSkillTime = -999f;
    private Collider[] _hitColliders; 

    private void Awake()
    {
        _hitColliders = new Collider[maxColliders];
    }

    private void Update()
    {
        // 쿨타임 체크
        if (Time.time < _lastSkillTime + skillData.cooldown) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            PerformGroundSmash();
            _lastSkillTime = Time.time; // 쿨타임 초기화
        }
    }

    public void PerformGroundSmash()
    {
        if (skillData == null) return;
        PerformSmashSequence().Forget();
    }

    private async UniTaskVoid PerformSmashSequence()
    {
        var token = this.GetCancellationTokenOnDestroy();

        // 선딜레이 망치떨어지는 순간
        //await UniTask.Delay(TimeSpan.FromSeconds(.5f), cancellationToken: token);

        Vector3 impactPoint = transform.position;

        // 이펙트 먼저 재생
        PlayEffects(impactPoint);

        // 짧은 순간 대기 (타격감 연출)
        await UniTask.Delay(TimeSpan.FromSeconds(0.51f), cancellationToken: token);

        // 물리 판정 및 데미지/넉백 적용
        int numColliders = Physics.OverlapSphereNonAlloc(
            impactPoint,
            skillData.radius,
            _hitColliders,
            skillData.targetLayer
        );

        for (int i = 0; i < numColliders; i++)
        {
            Collider col = _hitColliders[i];
            if (col.gameObject == gameObject) continue;

            // 데미지 처리
            if (col.TryGetComponent<IDamageable>(out var target))
            {
                target.TakeDamage(skillData.damage, col.ClosestPoint(impactPoint));
                target.TakeEffect(col.ClosestPoint(impactPoint));
            }

            // 넉백 처리
            if (col.TryGetComponent<IKnockbackable>(out var knockbackTarget))
            {
                knockbackTarget.ApplyKnockback(
                    impactPoint,
                    skillData.knockbackDistance,
                    skillData.knockbackDuration
                );
            }
        }
    }

    private void PlayEffects(Vector3 position)
    {
        // VFX 생성
        if (skillData.hitVFX != null)
        {
            Instantiate(skillData.hitVFX, skillData.hitVFX.transform.position, Quaternion.identity);
        }

        // 사운드 재생
        /* if (skillData.hitSound != null)
        {
            AudioSource.PlayClipAtPoint(skillData.hitSound, position);
        } 
        */
    }

    // 기즈모도 데이터 값을 따라감
    private void OnDrawGizmosSelected()
    {
        if (skillData == null) return;

        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, skillData.radius);
    }
}