using UnityEngine;
using System.Collections.Generic;
using StylizedWater3;

public class WaterBatchManager : MonoBehaviour
{
    public static WaterBatchManager Instance { get; private set; }
    private List<Transform> _targetTransforms = new List<Transform>(1000);

    // 물 높이 로직 관련
    [SerializeField] private HeightQuerySystem.Interface heightInterface;
    [SerializeField] private WaveProfile waveProfile;
    private readonly List<IWaterFloatTarget> _targets = new();
    private HeightQuerySystem.Sampler _sampler;

    private void Awake()
    {
        Instance = this;

        _sampler = new HeightQuerySystem.Sampler();
        _sampler.SetSampleCount(4);

        heightInterface.waveProfile = waveProfile;
    }

    private void FixedUpdate()
    {
        if (_targets.Count == 0) return;

        foreach (var target in _targets)
        {
            if (!target.IsActive) continue;

            // 물 오브젝트 가져옴
            heightInterface.GetWaterObject(target.Transform.position);

            // 없으면 패스
            if (heightInterface.HasMissingReferences())
                continue;

            // 타겟의 샘플 위치 계산 - 부력을 받는 기준
            UpdateSamplePositions(target);

            // 컴퓨트셰이더 계산
            Gerstner.ComputeHeight(_sampler, heightInterface);

            // 높이 적용
            ApplyHeight(target);
        }
    }

    // 카메라 프러스텀에서 벗어났을 때 아래로 내려가는 것 방지
    /*private void LateUpdate()
    {
        foreach (var target in _targets)
        {
            if (!target.IsActive) continue;

            if (transform.position.y < -1f)
            {
                Vector3 pos = transform.position;
                pos.y = -.8f;
                transform.position = pos;
            }
        }
    }*/

    private void UpdateSamplePositions(IWaterFloatTarget target)
    {
        var pos = target.Transform.position;
        float half = target.SurfaceSize * 0.5f;

        // 타겟 기준으로 부력 받는 위치에서 높이 계산
        _sampler.SetSamplePosition(0, pos + new Vector3(-half, 0, 0));
        _sampler.SetSamplePosition(1, pos + new Vector3(half, 0, 0));
        _sampler.SetSamplePosition(2, pos + new Vector3(0, 0, -half));
        _sampler.SetSamplePosition(3, pos + new Vector3(0, 0, half));
    }
    private void ApplyHeight(IWaterFloatTarget target)
    {
        // 계산된 값으로 높이 적용
        float h =
            (_sampler.heightValues[0] +
             _sampler.heightValues[1] +
             _sampler.heightValues[2] +
             _sampler.heightValues[3]) * 0.25f;

        var pos = target.Transform.position;
        pos.y = h + target.HeightOffset;
        target.Transform.position = pos;
    }

    // 리스트에 등록
    public void Register(IWaterFloatTarget target)
    {
        if (!_targets.Contains(target))
            _targets.Add(target);
    }

    // 리스트에서 해제
    public void Unregister(IWaterFloatTarget target)
    {
        _targets.Remove(target);
    }
}
