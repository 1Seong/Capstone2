Shader "Custom/ArrowLineAurora"
{
    Properties
    {
        _ColorA    ("색상 A", Color) = (0.2, 0.8, 1.0, 1.0)
        _ColorB    ("색상 B", Color) = (0.5, 0.1, 0.9, 1.0)
        _DashCount ("Dash 개수", Float) = 8.0
        _DashRatio ("Dash 비율", Range(0.01, 0.99)) = 0.6
        _Speed     ("흐름 속도", Float) = 1.0
        _Brightness("밝기", Float) = 1.5
        _GlowWidth ("글로우 폭", Range(0, 1)) = 0.3
        _ColorSpeed("색상 변화 속도", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }

        Pass
        {
            Name "ArrowAurora"
            ZTest LEqual
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
            };

            float4 _ColorA;
            float4 _ColorB;
            float  _DashCount;
            float  _DashRatio;
            float  _Speed;
            float  _Brightness;
            float  _GlowWidth;
            float  _ColorSpeed;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS   = GetWorldSpaceViewDir(worldPos);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 노멀이 카메라를 향하지 않는 면(뒷면) discard
                float facing = dot(normalize(IN.normalWS), normalize(IN.viewDirWS));
                if (facing <= 0.15) discard;

                float t = _Time.y;

                float scrolled = IN.uv.x * _DashCount - t * _Speed;
                float dash     = frac(scrolled);

                if (dash > _DashRatio) discard;

                float progress       = 1.0 - dash / _DashRatio;
                float halfWidth      = 0.5 * progress;
                float distFromCenter = abs(IN.uv.y - 0.5);

                if (distFromCenter > halfWidth) discard;

                float colorT = sin(t * _ColorSpeed + IN.uv.x * 2.0) * 0.5 + 0.5;
                float3 col   = lerp(_ColorA.rgb, _ColorB.rgb, colorT);

                float normalizedDist = distFromCenter / max(halfWidth, 0.001);
                float glow           = pow(1.0 - normalizedDist, 1.0 / max(_GlowWidth, 0.01));
                col *= (1.0 + glow * _Brightness);

                float headGlow = smoothstep(0.3, 0.0, progress);
                col           += _ColorA.rgb * headGlow * _Brightness;

                float alpha = glow * _ColorA.a;

                return half4(col, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
