Shader "Custom/StencilWriter"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-1" } // 큐브보다 먼저 그려야 함
        
        // [핵심] 화면엔 안 그리고(0), 스텐실에만 1을 쓴다(Replace)
        ColorMask 0
        ZWrite On
        
        Stencil
        {
            Ref 1
            Comp Always
            Pass Replace
        }
        
        Pass { CGPROGRAM ... (기본 vertex/frag - 내용 없어도 됨) ... ENDCG }
    }
}