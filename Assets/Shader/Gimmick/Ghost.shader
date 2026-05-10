Shader "Custom/Gimmick/Ghost"
{
    Properties
    {
        _ColorA         ("색상 A",          Color) = (0.4, 0.8, 1.0, 1)
        _ColorB         ("색상 B",          Color) = (0.7, 0.3, 1.0, 1)
        _EmissionStrength ("Emission 강도", Range(0, 5)) = 1.5
        _Speed          ("변형 속도",       Range(0, 3)) = 0.6
        _BlobScale      ("아메바 크기",     Range(0.5, 3)) = 1.2
        _WiggleAmp      ("꼬불 진폭",       Range(0, 1)) = 0.35
        _WiggleFreq     ("꼬불 빈도",       Range(1, 10)) = 4.0
        _LayerCount     ("레이어 수",       Range(1, 4)) = 3.0
        _LayerSpread    ("레이어 간격",     Range(0, 0.3)) = 0.08
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
                float4 _ColorA;
                float4 _ColorB;
                float  _EmissionStrength;
                float  _Speed;
                float  _BlobScale;
                float  _WiggleAmp;
                float  _WiggleFreq;
                float  _LayerCount;
                float  _LayerSpread;
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

            // ── 노이즈 ───────────────────────────────────────────────
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float smoothNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash(i),              b = hash(i + float2(1, 0));
                float c = hash(i + float2(0,1)), d = hash(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // 2옥타브 FBM — 아메바 경계 왜곡용
            float fbm(float2 p)
            {
                return smoothNoise(p) * 0.6 + smoothNoise(p * 2.1 + 1.7) * 0.4;
            }

            // ── 아메바 SDF ───────────────────────────────────────────
            // 극좌표 기반으로 반지름을 노이즈로 왜곡 → 꼬불꼬불한 경계
            float amoebaSDF(float2 uv, float timeOffset, float radiusOffset)
            {
                float2 centered = uv - 0.5;
                float  r        = length(centered);
                float  angle    = atan2(centered.y, centered.x);
                float  t        = _Time.y * _Speed + timeOffset;

                // 각도 방향으로 노이즈를 샘플링해서 반지름 왜곡
                // 여러 주파수의 sin을 합산 → 곱창 머리밴드 느낌
                float2 noiseUV  = float2(
                    cos(angle) * _WiggleFreq + t * 0.4,
                    sin(angle) * _WiggleFreq - t * 0.3
                );
                float warp = fbm(noiseUV) * 2.0 - 1.0;  // -1 ~ 1

                // 반지름 기준 (radiusOffset으로 레이어마다 크기 차이)
                float baseRadius = (0.32 + radiusOffset) * _BlobScale;
                float warpedR    = baseRadius + warp * _WiggleAmp * 0.5;

                // SDF: 양수 = 바깥, 음수 = 안쪽
                return r - warpedR;
            }

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

                float totalAlpha = 0.0;
                float colorBlend = 0.0;

                // ── 레이어 합산 ──────────────────────────────────────
                // 각 레이어마다 시간 위상과 크기를 살짝 다르게 해서 나풀나풀한 느낌
                int layers = clamp((int)_LayerCount, 1, 4);
                for (int i = 0; i < layers; i++)
                {
                    float fi          = (float)i;
                    float timeOffset  = fi * 1.3;                    // 레이어별 시간 위상
                    float radiusOffset = fi * _LayerSpread * -1.0;   // 안쪽으로 겹침

                    float sdf    = amoebaSDF(IN.uv, timeOffset, radiusOffset);

                    // 경계를 부드럽게 — 안쪽은 불투명, 바깥은 페이드
                    float inner  = smoothstep(0.02, -0.04, sdf);     // 채워진 내부
                    float edge   = smoothstep(0.06, 0.0, abs(sdf));  // 경계선 강조

                    float layerAlpha = inner * 0.35 + edge * 0.5;

                    // 레이어마다 색상 비율을 다르게
                    colorBlend  += (fi / max(layers - 1, 1)) * layerAlpha;
                    totalAlpha  += layerAlpha;
                }

                // 레이어 합산 후 알파 정규화
                totalAlpha = saturate(totalAlpha);
                colorBlend = totalAlpha > 0.001 ? saturate(colorBlend / totalAlpha) : 0.0;

                if (totalAlpha <= 0.001) return half4(0, 0, 0, 0);

                // ── 색상 ─────────────────────────────────────────────
                float3 col = lerp(_ColorA.rgb, _ColorB.rgb, colorBlend);
                col       *= (1.0 + _EmissionStrength);

                // 중심부 약간 밝게
                float2 centered = IN.uv - 0.5;
                float  r        = length(centered);
                float  centerGlow = smoothstep(0.25, 0.0, r) * 0.4;
                col += lerp(_ColorA.rgb, _ColorB.rgb, 0.5) * centerGlow * _EmissionStrength;

                return half4(col, totalAlpha);
            }
            ENDHLSL
        }
    }
}
