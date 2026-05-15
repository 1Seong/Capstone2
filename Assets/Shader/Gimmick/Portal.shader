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
                float  _Speed;
                float  _SwirlTightness;
                float  _EdgeFade;
                float4 _CubeCenter;
                float  _CubeSize;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 worldPos : TEXCOORD2;};
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                OUT.worldPos = TransformObjectToWorld(IN.positionOS);
                return OUT;
            }
            half4 frag(Varyings IN) : SV_Target
            {
                // 카메라 → 픽셀 방향 레이가 중심 큐브 AABB를 통과하는지 체크
                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDir    = normalize(IN.worldPos - rayOrigin);
                float3 boxMin    = _CubeCenter.xyz - _CubeSize * 0.5;
                float3 boxMax    = _CubeCenter.xyz + _CubeSize * 0.5;
                float3 invDir    = 1.0 / rayDir;
                float3 t0        = (boxMin - rayOrigin) * invDir;
                float3 t1        = (boxMax - rayOrigin) * invDir;
                float3 tMin      = min(t0, t1);
                float3 tMax      = max(t0, t1);
                float  tEnter    = max(max(tMin.x, tMin.y), tMin.z);
                float  tExit     = min(min(tMax.x, tMax.y), tMax.z);

                // 레이가 큐브를 통과하고, 빌보드가 큐브 뒤에 있으면 discard
                float distToPixel = length(IN.worldPos - rayOrigin);
                if (tExit > tEnter && tEnter > 0.0 && distToPixel > tExit + 0.01) discard;
                
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
                float alpha = circleMask * lerp(0.6, 1.0, emission);
                alpha = saturate(alpha);
                
                if (alpha <= 0.001) return half4(0, 0, 0, 0);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
