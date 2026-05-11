Shader "Custom/Gimmick/Laser"
{
    Properties
    {
        _ColorInner       ("중심 색상",         Color) = (1.0, 0.95, 0.6, 1)
        _ColorOuter       ("외곽 색상",         Color) = (1.0, 0.4, 0.1, 1)
        _EmissionStrength ("Emission 강도",     Range(0, 5)) = 2.0
        _StarScale        ("별 크기",           Range(0.1, 0.8)) = 0.4
        _InnerRadius      ("안쪽 오목 깊이",    Range(0.1, 0.9)) = 0.45
        _PulseSpeed       ("Pulse 속도",        Range(0, 3)) = 1.2
        _PulseAmount      ("Pulse 강도",        Range(0, 0.3)) = 0.08
        _GlowRadius       ("중심 글로우 범위",  Range(0, 0.5)) = 0.15
    }

    SubShader
    {
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" }

        Stencil
        {
            Ref 1
            Comp NotEqual
        }

        Cull Off
        ZWrite Off
        ZTest Always
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
                float  _StarScale;
                float  _InnerRadius;
                float  _PulseSpeed;
                float  _PulseAmount;
                float  _GlowRadius;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            float star6SDF(float2 p, float outerR, float innerR)
            {
                float angle = atan2(p.y, p.x);
                float pi    = 3.14159265;
                float sector  = pi / 6.0;
                float sectorI = floor((angle + pi) / sector);
                float a0      = sectorI * sector - pi;
                float a1      = a0 + sector;
                float offset  = 3.0;
                float r0 = (fmod(sectorI + offset, 2.0) < 1.0) ? outerR : innerR;
                float r1 = (fmod(sectorI + offset + 1.0, 2.0) < 1.0) ? outerR : innerR;
                float2 pa = float2(cos(a0), sin(a0)) * r0;
                float2 pb = float2(cos(a1), sin(a1)) * r1;
                float2 ab  = pb - pa;
                float2 ap  = p - pa;
                float  tt  = saturate(dot(ap, ab) / dot(ab, ab));
                float2 closest = pa + ab * tt;
                float  dist = length(p - closest);
                float inside = sign(cross(float3(ab, 0), float3(ap, 0)).z);
                return dist * -inside;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y;
                float2 centered = IN.uv - 0.5;

                float pulse = sin(t * _PulseSpeed * 6.28318) * _PulseAmount;
                float scale = _StarScale + pulse;
                float inner = scale * _InnerRadius;

                float sdf = star6SDF(centered, scale, inner);

                float starMask = smoothstep(0.01, -0.01, sdf);
                if (starMask <= 0.001) return half4(0, 0, 0, 0);

                float r      = length(centered);
                float colorT = smoothstep(0.0, scale, r);
                float3 col   = lerp(_ColorInner.rgb, _ColorOuter.rgb, colorT);
                col *= (1.0 + _EmissionStrength);

                float glow = smoothstep(_GlowRadius, 0.0, r);
                glow       = glow * glow;
                col       += _ColorInner.rgb * glow * _EmissionStrength;

                return half4(col, saturate(starMask));
            }
            ENDHLSL
        }
    }
}
