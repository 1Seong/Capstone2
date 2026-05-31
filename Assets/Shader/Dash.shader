Shader "Custom/Gimmick/Dash"
{
    Properties
    {
        // 오로라 배경
        _AuroraColorA   ("오로라 색상 A",       Color) = (0.2, 0.6, 1.0, 1)
        _AuroraColorB   ("오로라 색상 B",       Color) = (0.4, 0.1, 0.8, 1)
        _AuroraOpacity  ("오로라 투명도",       Range(0, 1)) = 0.35
        _AuroraSpeed    ("오로라 흐름 속도",    Range(0, 2)) = 0.3
        _AuroraScale    ("오로라 스케일",       Range(0.5, 5)) = 2.0

        // 화살표
        _ArrowColor     ("화살표 색상",         Color) = (0.8, 0.95, 1.0, 1)
        _ArrowCount     ("화살표 개수",         Range(1, 10)) = 5.0
        _ArrowRatio     ("화살표 길이 비율",    Range(0.1, 0.9)) = 0.5
        _ArrowSpeed     ("화살표 속도",         Range(0, 5)) = 1.5
        _ArrowBrightness("화살표 밝기",         Range(0, 5)) = 2.0
        _ArrowWidth     ("화살표 최대 폭",      Range(0.1, 1)) = 0.7
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
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _AuroraColorA;
                float4 _AuroraColorB;
                float  _AuroraOpacity;
                float  _AuroraSpeed;
                float  _AuroraScale;
                float4 _ArrowColor;
                float  _ArrowCount;
                float  _ArrowRatio;
                float  _ArrowSpeed;
                float  _ArrowBrightness;
                float  _ArrowWidth;
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

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float smoothNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash(i), b = hash(i+float2(1,0)), c = hash(i+float2(0,1)), d = hash(i+float2(1,1));
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
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
                float2 uv = IN.uv;
                float  t  = _Time.y;

                // ── 오로라 배경 ──────────────────────────────────────
                float2 noiseUV  = float2(uv.x * _AuroraScale + t * _AuroraSpeed, uv.y * _AuroraScale * 2.0);
                float  noise    = smoothNoise(noiseUV) * 0.6 + smoothNoise(noiseUV * 2.1) * 0.4;
                float  edgeFade = smoothstep(0.0, 0.2, uv.y) * smoothstep(1.0, 0.8, uv.y);
                float  colorT   = smoothNoise(float2(uv.x * 0.5 - t * _AuroraSpeed * 0.3, 0.5));
                float3 auroraCol   = lerp(_AuroraColorA.rgb, _AuroraColorB.rgb, colorT);
                float  auroraAlpha = noise * edgeFade * _AuroraOpacity;

                // ── 화살표 ───────────────────────────────────────────
                float scrolled = uv.x * _ArrowCount - t * _ArrowSpeed;
                float dash     = frac(scrolled);

                float  arrowAlpha = 0.0;
                float3 arrowCol   = _ArrowColor.rgb;

                if (dash < _ArrowRatio)
                {
                    float progress   = 1.0 - dash / _ArrowRatio;
                    float halfWidth  = 0.5 * progress * _ArrowWidth;
                    float distCenter = abs(uv.y - 0.5);

                    if (distCenter < halfWidth)
                    {
                        float widthFade = 1.0 - (distCenter / halfWidth);
                        widthFade       = widthFade * widthFade;
                        float headGlow  = smoothstep(0.4, 0.0, progress);
                        arrowAlpha      = widthFade * edgeFade;
                        arrowCol       *= _ArrowBrightness * (1.0 + headGlow * 2.0);
                    }
                }

                // ── 합성 ─────────────────────────────────────────────
                float3 col  = lerp(auroraCol, arrowCol, arrowAlpha);
                float  alpha = saturate(auroraAlpha + arrowAlpha);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
