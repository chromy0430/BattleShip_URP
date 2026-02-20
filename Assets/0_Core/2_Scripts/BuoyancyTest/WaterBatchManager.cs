using UnityEngine;
using System.Collections.Generic;
using StylizedWater3;
using BattleShip;

public class WaterBatchManager : StaticInstance<WaterBatchManager>
{

    // 물 높이 로직 관련
    [SerializeField] private HeightQuerySystem.Interface heightInterface;
    [SerializeField] private WaveProfile waveProfile;

    private readonly List<IWaterFloatTarget> _targets = new();

    // 최적화를 위한 샘플러 구분
    private HeightQuerySystem.Sampler _hqSampler;
    private HeightQuerySystem.Sampler _lqSampler;

    Vector3 queryPos;

    protected override void Awake()
    {
        base.Awake();

        // 플레이어용
        _hqSampler = new HeightQuerySystem.Sampler();
        _hqSampler.SetSampleCount(4);

        // 아이템용
        _lqSampler = new HeightQuerySystem.Sampler();
        _lqSampler.SetSampleCount(1);

        heightInterface.waveProfile = waveProfile;
    }

    private void FixedUpdate()
    {
        if (_targets.Count == 0) return;

        queryPos = transform.position;
        if (PlayerManager.Instance != null && PlayerManager.Instance.PlayerTransform != null)
        {
            queryPos = PlayerManager.Instance.PlayerTransform.position;
        }
        else if (_targets.Count > 0 && _targets[0] != null)
        {
            queryPos = _targets[0].Transform.position;
        }

        // 위치 갱신
        heightInterface.GetWaterObject(queryPos);
        if (heightInterface.HasMissingReferences()) return;

        foreach (var target in _targets)
        {
            if (!target.IsActive) continue;

            if (target.UseSimpleWaterPhysics)
            {
                ProcessLowQuality(target);
            }
            else
            {
                ProcessHighQuality(target);
            }
        }
    }

    // 리스트에 등록
    public void Register(IWaterFloatTarget target)
    {
        if (_targets.Contains(target))
            return;

        heightInterface.GetWaterObject(target.Transform.position);

        if (heightInterface.HasMissingReferences())
        {
            heightInterface.GetWaterObject(queryPos);
        }

        target.SetWaterObject(heightInterface);
        _targets.Add(target);
    }

    // 리스트에서 해제
    public void Unregister(IWaterFloatTarget target)
    {
        _targets.Remove(target);
    }

    public void RefreshWaterObject(IWaterFloatTarget target)
    {
        heightInterface.GetWaterObject(target.Transform.position);

        if (!heightInterface.HasMissingReferences())
        {
            target.SetWaterObject(heightInterface);
        }
    }

    private void OnDestroy()
    {
        _hqSampler?.Dispose();
        _lqSampler?.Dispose();
    }


    private void ProcessLowQuality(IWaterFloatTarget target)
    {
        _lqSampler.SetSamplePosition(0, target.Transform.position);
        Gerstner.ComputeHeight(_lqSampler, heightInterface);
        float h = _lqSampler.heightValues[0];
        float stregth = 1f;
        Vector3 pos = target.Transform.position;
        //pos.y = h + target.HeightOffset;
        pos.y = Mathf.Lerp(pos.y, h + target.HeightOffset + 1f, stregth);
        target.Transform.position = pos;
    }

    private void ProcessHighQuality(IWaterFloatTarget target)
    {
        UpdateSamplePositionsHQ(target);

        Gerstner.ComputeHeight(_hqSampler, heightInterface);

        ApplyHeightHQ(target);
    }

    private void UpdateSamplePositionsHQ(IWaterFloatTarget target)
    {
        var pos = target.Transform.position;
        var rot = target.Transform.rotation;

        float halfW = target.SurfaceSize.x * 0.5f;
        float halfL = target.SurfaceSize.y * 0.5f;

        // 타겟 기준으로 부력 받는 4개 위치 계산
        _hqSampler.SetSamplePosition(0, pos + (rot * new Vector3(-halfW, 0, 0)));
        _hqSampler.SetSamplePosition(1, pos + (rot * new Vector3(halfW, 0, 0)));
        _hqSampler.SetSamplePosition(2, pos + (rot * new Vector3(0, 0, -halfL)));
        _hqSampler.SetSamplePosition(3, pos + (rot * new Vector3(0, 0, halfL)));
    }

    private void ApplyHeightHQ(IWaterFloatTarget target)
    {
        if (target is PlayerFloating player)
        {
            // 플레이어는 파도에 따른 기울기(Tilt)까지 처리
            player.OnHeightCalculated(_hqSampler.heightValues);
        }
        else
        {
            // Fallback: UseSimpleWaterPhysics가 false인데 PlayerFloating이 아닌 경우 (평균값 적용)
            float h = (_hqSampler.heightValues[0] + _hqSampler.heightValues[1] +
                       _hqSampler.heightValues[2] + _hqSampler.heightValues[3]) * 0.25f;
            Vector3 pos = target.Transform.position;
            pos.y = h + target.HeightOffset;
            target.Transform.position = pos;
        }
    }
}
