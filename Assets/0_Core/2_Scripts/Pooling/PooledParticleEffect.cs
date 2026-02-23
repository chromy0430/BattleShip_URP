using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

[RequireComponent(typeof(ParticleSystem))]
public class PooledParticleEffect : MonoBehaviour
{
    private ParticleSystem _ps;
    private CancellationTokenSource _cts;

    private void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        // 활성화될 때마다 자동으로 반환 로직 실행
        _cts = new CancellationTokenSource();
        CheckAliveAndReturn(_cts.Token).Forget();
    }

    private void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async UniTaskVoid CheckAliveAndReturn(CancellationToken token)
    {
        // 파티클이 재생 중인지 확인 (IsAlive가 false가 될 때까지 대기)
        // withChildren: true는 자식 파티클까지 모두 포함해서 검사
        await UniTask.WaitUntil(() => !_ps.IsAlive(true), cancellationToken: token);

        // 파티클 재생이 끝나면 풀로 반환
        AddressablePoolManager.Instance.Return(this.gameObject);
    }
}