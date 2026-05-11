Shader "Custom/TileUnlit"
{
    Properties
    {
        _BaseColor        ("베이스 색상",       Color) = (0.2, 0.2, 0.2, 1)
        _EmissionColor    ("Emission 색상",     Color) = (0.3, 0.6, 1.0, 1)
        _EmissionStrength ("Emission 강도",     Range(0, 5)) = 0.0

        _GridColor        ("그리드 선 색상",    Color) = (0.0, 0.0, 0.0, 1)
        _GridWidth        ("그리드 선 두께",    Range(0, 0.2)) = 0.04
        _GridDarkness     ("그리드 선 강도",    Range(0, 1)) = 0.6

        [Header(Pulse)]
        _PulseEnabled     ("Pulse 활성화",        Float) = 0.0
        _PulseMin         ("Pulse 최소 Emission", Range(0, 5)) = 0.2
        _PulseMax         ("Pulse 최대 Emission", Range(0, 5)) = 1.5
        _PulsePeriod      ("Pulse 주기 (초)",     Range(0.1, 10)) = 1.5
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
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float  _EmissionStrength;
                float4 _GridColor;
                float  _GridWidth;
                float  _GridDarkness;
                float  _PulseEnabled;
                float  _PulseMin;
                float  _PulseMax;
                float  _PulsePeriod;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float emission;
                if (_PulseEnabled > 0.5)
                {
                    float t  = sin(_Time.y * (6.28318 / _PulsePeriod)) * 0.5 + 0.5;
                    emission = lerp(_PulseMin, _PulseMax, t);
                }
                else
                {
                    emission = _EmissionStrength;
                }

                float3 faceCol  = _BaseColor.rgb + _EmissionColor.rgb * emission;
                float2 grid     = abs(frac(IN.uv) - 0.5) * 2.0;
                float  gridLine = smoothstep(1.0 - _GridWidth * 2.0, 1.0, max(grid.x, grid.y));
                float3 gridCol  = lerp(faceCol, _GridColor.rgb, _GridDarkness);
                float3 col      = lerp(faceCol, gridCol, gridLine);

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
