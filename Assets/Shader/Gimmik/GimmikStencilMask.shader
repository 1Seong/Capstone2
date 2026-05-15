Shader "Custom/Gimmick/StencilMask"
{
    // 기믹 Quad의 자식으로 배치, 로컬 Z 오프셋으로 큐브 바깥 뒷면에 위치
    // Opaque → 실제 빌보드(Transparent+1)보다 반드시 먼저 스텐실 Write
    // ColorMask 0 → 색상 출력 없음, 시각적 영향 없음
    Properties { }

    SubShader
    {
        Tags { "Queue"="Geometry+1" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Stencil
        {
            Ref 1
            Comp Always
            Pass Replace
        }

        Cull Off
        ZWrite On
        ZTest LEqual
        ColorMask 0

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
