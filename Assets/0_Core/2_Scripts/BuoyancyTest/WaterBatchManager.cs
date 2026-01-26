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
    Vector3 queryPos;
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

            UpdateSamplePositions(target);

            Gerstner.ComputeHeight(_sampler, heightInterface);

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
        //float half = target.SurfaceSize * 0.5f;
        var rot = target.Transform.rotation;

        float halfW = target.SurfaceSize.x * 0.5f;
        float halfL = target.SurfaceSize.y * 0.5f;

        // 타겟 기준으로 부력 받는 위치에서 높이 계산
        _sampler.SetSamplePosition(0, pos + (rot * new Vector3(-halfW, 0, 0))); 
        _sampler.SetSamplePosition(1, pos + (rot * new Vector3(halfW, 0, 0)));  
        _sampler.SetSamplePosition(2, pos + (rot * new Vector3(0, 0, -halfL))); 
        _sampler.SetSamplePosition(3, pos + (rot * new Vector3(0, 0, halfL)));
    }
    private void ApplyHeight(IWaterFloatTarget target)
    {
        if (target is PlayerFloating player)
        {
            player.OnHeightCalculated(_sampler.heightValues);
        }
        else
        {
            // 일반 오브젝트는 기존처럼 단순 높이 적용
            float h = (_sampler.heightValues[0] + _sampler.heightValues[1] +
                       _sampler.heightValues[2] + _sampler.heightValues[3]) * 0.25f;
            Vector3 pos = target.Transform.position;
            pos.y = h + target.HeightOffset;
            target.Transform.position = pos;
        }

        /*// 계산된 값으로 높이 적용
        float h =
            (_sampler.heightValues[0] +
             _sampler.heightValues[1] +
             _sampler.heightValues[2] +
             _sampler.heightValues[3]) * 0.25f;

        var pos = target.Transform.position;
        pos.y = h + target.HeightOffset;
        target.Transform.position = pos;*/
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
            //Debug.LogWarning($"[WaterBatchManager] No WaterObject found for {target}");
            //return;
        }

        // 타겟에 WaterObject 캐싱
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
        _sampler?.Dispose();
    }
}
