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
        _CubeCenter ("큐브 중심 (월드)", Vector) = (0, 0, 0, 0)
        _CubeSize   ("큐브 크기",        Float) = 10.0
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
                float4 _CubeCenter;
                float  _CubeSize;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 worldPos : TEXCOORD2; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                OUT.worldPos = TransformObjectToWorld(IN.positionOS);
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
                // Orthographic: 레이가 카메라 forward 방향으로 평행
                float3 rayDir    = -UNITY_MATRIX_V[2].xyz;
                // 빌보드 월드 포지션에서 카메라 forward 역방향으로 충분히 뒤에서 시작
                float3 rayOrigin = IN.worldPos - rayDir * 1000.0;

                float3 boxMin = _CubeCenter.xyz - _CubeSize * 0.5;
                float3 boxMax = _CubeCenter.xyz + _CubeSize * 0.5;
                float3 invDir = 1.0 / rayDir;
                float3 t0     = (boxMin - rayOrigin) * invDir;
                float3 t1     = (boxMax - rayOrigin) * invDir;
                float3 tMin   = min(t0, t1);
                float3 tMax   = max(t0, t1);
                float  tEnter = max(max(tMin.x, tMin.y), tMin.z);
                float  tExit  = min(min(tMax.x, tMax.y), tMax.z);

                float distToPixel = length(IN.worldPos - rayOrigin);
                if (tExit > tEnter && tEnter > 0.0 && distToPixel > tExit + 0.01) discard;
                
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

                float alpha = saturate(starMask);
                if (alpha <= 0.001) return half4(0, 0, 0, 0);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}