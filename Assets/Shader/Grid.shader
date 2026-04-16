Shader "Custom/Grid"
{
    Properties
    {
        _GridColor ("Grid Color", Color) = (1, 1, 1, 0.3)
        _LineThickness ("Line Thickness", Range(0.001, 0.05)) = 0.02
        _GridSize ("Grid Size", Float) = 10.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float4 _GridColor;
                float  _LineThickness;
                float  _GridSize;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 gridUV = frac(IN.uv * _GridSize);
                float  gridLine = step(1.0 - _LineThickness, gridUV.x)  // 오른쪽
                + step(gridUV.x, _LineThickness)          // 왼쪽
                + step(1.0 - _LineThickness, gridUV.y)   // 위쪽
                + step(gridUV.y, _LineThickness);         // 아래쪽
                clip(gridLine - 0.001);
                return half4(_GridColor.rgb, _GridColor.a * saturate(gridLine));
            }
            ENDHLSL
        }
    }
}