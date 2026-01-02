Shader "Custom/URP_StencilMask"
{
    Properties
    {
        [IntRange] _StencilRef ("Stencil Reference ID", Range(0, 255)) = 1
    }
    SubShader
    {
        // URP 및 투명 렌더링 설정
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent-1" // 파티클(3000)보다 살짝 먼저 그려지도록 설정 (2999)
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            // 핵심 1: 눈에 보이지 않게 처리
            Blend Zero One // 색상을 아예 출력하지 않음 (완전 투명)
            ZWrite Off     // 핵심 2: 깊이 버퍼를 쓰지 않음 (뒤에 있는 적/이펙트가 가려지지 않음)
            ColorMask 0    // R,G,B,A 아무것도 기록하지 않음

            // 핵심 3: 스텐실 기록
            Stencil
            {
                Ref [_StencilRef] // Inspector에서 설정한 ID (기본 1)
                Comp Always       // 무조건 그림
                Pass Replace      // 이 영역에 ID를 덮어씀
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(0,0,0,0); // 실제로는 Blend Zero One 때문에 무시됨
            }
            ENDHLSL
        }
    }
}