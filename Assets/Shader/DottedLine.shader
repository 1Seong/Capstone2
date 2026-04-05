Shader "Custom/DottedLine"
{
    Properties
    {
        _Color      ("Line Color",    Color)  = (0.2, 0.8, 1.0, 1.0)
        _DashCount  ("Dash Count",    Float)  = 8.0    // 선 전체에 점선 몇 개
        _DashRatio  ("Dash Ratio",    Range(0.01, 0.99)) = 0.5  // 점:공백 비율
        _Speed      ("Flow Speed",    Float)  = 1.0    // 흐르는 속도
        _Brightness ("Brightness",    Float)  = 1.5    // HDR 글로우용
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        // ── Pass 2: 일반 패스 (가려지지 않은 부분) ────────────────────
        Pass
        {
            Name "Visible"
            ZTest Always
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragVisible
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            float4 _Color;
            float  _DashCount;
            float  _DashRatio;
            float  _Speed;
            float  _Brightness;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 fragVisible(Varyings IN) : SV_Target
            {
                float dash = frac(IN.uv.x * _DashCount - _Time.y * _Speed);
                if (dash > _DashRatio) discard;

                half4 col = _Color;
                col.rgb *= _Brightness;
                return col;
            }
            ENDHLSL
        }

        // ── Pass 1: 실루엣 패스 (큐브에 가려진 부분) ──────────────────
        Pass
        {
            Name "Occluded"
            ZTest Greater
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragOccluded
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            float4 _Color;
            float  _DashCount;
            float  _DashRatio;
            float  _Speed;
            float  _Brightness;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 fragOccluded(Varyings IN) : SV_Target
            {
                // 점선 패턴 (가려진 구간은 절반 투명도로)
                float dash = frac(IN.uv.x * _DashCount - _Time.y * _Speed);
                if (dash > _DashRatio) discard;

                half4 col = _Color;
                col.rgb *= _Brightness;
                col.a   *= 0.35;   // 가려진 부분은 흐리게
                return col;
            }
            ENDHLSL
        }
    }
}
