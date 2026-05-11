Shader "Custom/CellPulse"
{
    Properties
    {
        _EmissionColor    ("Emission 색상",  Color) = (1.0, 1.0, 0.3, 1)
        _EmissionStrength ("Emission 강도",  Range(0, 5)) = 0.0

        _GridColor        ("그리드 선 색상", Color) = (0.0, 0.0, 0.0, 1)
        _GridWidth        ("그리드 선 두께", Range(0, 0.2)) = 0.04
        _GridDarkness     ("그리드 선 강도", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Back
        ZWrite On
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _EmissionColor;
                float  _EmissionStrength;
                float4 _GridColor;
                float  _GridWidth;
                float  _GridDarkness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // BaseColor 없이 EmissionColor만 사용
                // 사용자 지정 BaseColor 영향 없음
                float3 col = _EmissionColor.rgb * max(_EmissionStrength, 0.1);

                // 그리드 라인
                float2 grid     = abs(frac(IN.uv) - 0.5) * 2.0;
                float  gridLine = smoothstep(1.0 - _GridWidth * 2.0, 1.0, max(grid.x, grid.y));
                float3 gridCol  = lerp(col, _GridColor.rgb, _GridDarkness);
                col             = lerp(col, gridCol, gridLine);

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
