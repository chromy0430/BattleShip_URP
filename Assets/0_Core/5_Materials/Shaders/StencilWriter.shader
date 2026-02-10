Shader "Custom/StencilWriter"
{
    SubShader
    {
        // [설정] 불투명 큐(Geometry)보다 1단계 먼저 그려서 자리를 선점합니다.
        Tags { "RenderType"="Opaque" "Queue"="Geometry-1" }
        
        // [핵심 1] 화면에는 투명하게 처리 (색상 출력 안 함)
        ColorMask 0
        
        // [핵심 2] 원본 배 모델과 겹쳐있으므로, 깊이 싸움(Z-Fighting) 방지를 위해 Off
        ZWrite Off
        
        // [핵심 3] 이 물체가 있는 픽셀에 스텐실 값 '1'을 기록
        Stencil
        {
            Ref 1
            Comp Always     // 조건 없이 무조건
            Pass Replace    // 1로 바꿔치기(기록) 한다
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // 입력: 메쉬의 정점 데이터
            struct appdata
            {
                float4 vertex : POSITION;
            };

            // 출력: 화면 좌표
            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                // 3D 월드 좌표를 2D 화면 좌표로 변환
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 색상은 중요하지 않음 (ColorMask 0이라 안 보임)
                // 하지만 이 함수가 실행되어야 스텐실이 기록됨
                return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}