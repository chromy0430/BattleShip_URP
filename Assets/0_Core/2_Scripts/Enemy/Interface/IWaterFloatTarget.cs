using StylizedWater3;
using UnityEngine;

public interface IWaterFloatTarget
{
    Transform Transform { get; } // 현재 위치
    float HeightOffset { get; } // 지정 높이
    float SurfaceSize { get; } // 부력을 받을 오브젝트 면 크기
    bool IsActive { get; } // 활성화 여부

    void SetWaterObject(HeightQuerySystem.Interface heightInterface);
}