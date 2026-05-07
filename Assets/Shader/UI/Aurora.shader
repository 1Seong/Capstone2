Shader "Custom/UI/Aurora"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _ColorA         ("색상 A", Color) = (0.0, 0.8, 0.6, 1)
        _ColorB         ("색상 B", Color) = (0.2, 0.3, 0.9, 1)
        _ColorC         ("색상 C", Color) = (0.5, 0.1, 0.7, 1)

        _Speed          ("흐름 속도", Range(0, 2)) = 0.3
        _WaveFreq       ("가로 파동 빈도", Range(1, 10)) = 4.0
        _WaveAmp        ("커튼 흔들림 진폭", Range(0, 0.3)) = 0.08
        _BandCount      ("색상 밴드 수", Range(1, 6)) = 3.0
        _Sharpness      ("밴드 선명도", Range(1, 8)) = 3.0

        _FadeBottom     ("아래 페이드 범위", Range(0, 0.5)) = 0.05
        _FadeTop        ("위 페이드 범위", Range(0, 1)) = 0.6
        _Brightness     ("밝기", Range(0, 3)) = 1.2
        _Opacity        ("전체 투명도", Range(0, 1)) = 0.85

        _StencilComp    ("Stencil Comparison", Float) = 8
        _Stencil        ("Stencil ID", Float) = 0
        _StencilOp      ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask", Float) = 255
        _ColorMask      ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA;
                float4 _ColorB;
                float4 _ColorC;
                float  _Speed;
                float  _WaveFreq;
                float  _WaveAmp;
                float  _BandCount;
                float  _Sharpness;
                float  _FadeBottom;
                float  _FadeTop;
                float  _Brightness;
                float  _Opacity;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            // 저비용 hash (sin 기반)
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // 1옥타브 스무스 노이즈 — 커튼 흔들림용으로 1개만 사용
            float smoothNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv    = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                // uv.x: 가로 (0=왼쪽, 1=오른쪽)
                // uv.y: 세로 (0=아래, 1=위)

                float t = _Time.y * _Speed;

                // ── 커튼 흔들림 ──────────────────────────────────────
                // 가로 위치에 따라 세로 offset이 노이즈로 흔들림
                // uv.y를 왜곡해서 커튼이 일렁이는 느낌
                float waveOffset = smoothNoise(float2(uv.x * _WaveFreq, t * 0.5)) * 2.0 - 1.0;
                float warpedY = uv.y + waveOffset * _WaveAmp;

                // ── 색상 밴드 ─────────────────────────────────────────
                // 가로 방향으로 흐르는 색상 밴드
                // 밴드마다 위상 차이를 줘서 A→B→C가 교차
                float bandUV = uv.x * _BandCount - t;

                // 0~1 사이 부드러운 주기 함수 (sin 대신 frac+smoothstep으로 저비용)
                float band     = frac(bandUV);
                float bandSmooth = band * band * (3.0 - 2.0 * band); // smoothstep 근사

                // 세 색상을 두 번 lerp로 블렌딩
                // bandSmooth 0→0.5: A→B, 0.5→1: B→C
                float t1 = saturate(bandSmooth * 2.0);
                float t2 = saturate(bandSmooth * 2.0 - 1.0);
                float3 bandColor = lerp(lerp(_ColorA.rgb, _ColorB.rgb, t1), _ColorC.rgb, t2);

                // ── 밴드 선명도 (밴드 경계를 선명하게) ──────────────
                // pow 대신 반복 곱셈으로 근사
                float sharpBand = bandSmooth;
                sharpBand = sharpBand * sharpBand; // ^2
                if (_Sharpness > 2.0) sharpBand = sharpBand * sharpBand; // ^4 근사
                float bandIntensity = sharpBand;

                // ── 세로 방향 페이드 (커튼 형태) ─────────────────────
                // 아래: 선명하게 시작, 위로 갈수록 사라짐
                float fadeBottom = smoothstep(0.0, _FadeBottom, warpedY);
                float fadeTop    = 1.0 - smoothstep(_FadeTop, 1.0, warpedY);
                float vertFade   = fadeBottom * fadeTop;

                // 위쪽이 더 빨리 사라지도록 추가 감쇠 (오로라 특유의 흐릿한 상단)
                float topAttenuation = (1.0 - warpedY) * (1.0 - warpedY);
                vertFade *= topAttenuation;

                // ── 최종 합성 ─────────────────────────────────────────
                float3 col    = bandColor * bandIntensity * vertFade * _Brightness;
                float  alpha  = vertFade * bandIntensity * _Opacity;

                // Unity UI 버텍스 컬러(Tint/Fade) 반영
                half4 result = half4(col, alpha) * IN.color;

                return result;
            }
            ENDHLSL
        }
    }
}
