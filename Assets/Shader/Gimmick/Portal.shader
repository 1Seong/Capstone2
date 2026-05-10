Shader "Custom/Gimmick/Portal"
{
    Properties
    {
        _ColorInner       ("중심 색상 (Emission)", Color) = (0.8, 0.95, 1.0, 1)
        _ColorOuter       ("외곽 색상",            Color) = (0.1, 0.05, 0.4, 1)
        _EmissionStrength ("Emission 강도",        Range(0, 5)) = 2.5
        _Speed            ("회전 속도",            Range(-5, 5)) = 1.2
        _SwirlTightness   ("소용돌이 조임",        Range(0, 20)) = 6.0
        _EdgeFade         ("외곽 페이드 범위",      Range(0, 0.5)) = 0.15
    }

    SubShader
    {
        Tags { "Queue"="Transparent+2" "RenderType"="Transparent" }

        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorInner;
                float4 _ColorOuter;
                float  _EmissionStrength;
                float  _Speed;
                float  _SwirlTightness;
                float  _EdgeFade;
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
                OUT.uv         = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y * _Speed;

                float2 centered = IN.uv - 0.5;
                float  r        = length(centered);
                float  angle    = atan2(centered.y, centered.x);

                float circleMask = smoothstep(0.5, 0.5 - _EdgeFade, r);
                if (circleMask <= 0.001) return half4(0, 0, 0, 0);

                float swirlAngle = angle - t + (1.0 - r * 2.0) * _SwirlTightness;
                float swirl      = sin(swirlAngle) * 0.5 + 0.5;

                float  colorT = smoothstep(0.0, 0.48, r);
                float3 col    = lerp(_ColorInner.rgb, _ColorOuter.rgb, colorT);
                col = lerp(col, col * (0.7 + swirl * 0.6), 0.4);

                float emission = smoothstep(0.2, 0.0, r);
                emission       = emission * emission;
                col           += _ColorInner.rgb * emission * _EmissionStrength;

                float alpha = circleMask;
                alpha      *= lerp(0.6, 1.0, emission);

                return half4(col, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
