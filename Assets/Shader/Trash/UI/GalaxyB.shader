Shader "Custom/UI/GalaxyB"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color          ("베이스 색상", Color) = (0.06, 0.05, 0.12, 0.92)
        _EdgeColor      ("엣지 글로우 색", Color) = (0.48, 0.37, 0.75, 1)
        _EdgeThickness  ("엣지 두께", Range(0, 0.5)) = 0.04
        _EdgeGlow       ("엣지 글로우 강도", Range(0, 3)) = 1.2

        // 은하수 노이즈
        _NebulaColor    ("성운 색상", Color) = (0.3, 0.2, 0.6, 1)
        _NebulaScale    ("성운 스케일", Range(0.5, 5)) = 2.0
        _NebulaOpacity  ("성운 투명도", Range(0, 1)) = 0.18
        _NebulaSpeed    ("성운 흐름 속도", Range(0, 0.5)) = 0.04

        // 별
        _StarDensity    ("별 밀도", Range(10, 200)) = 80.0
        _StarBrightness ("별 밝기", Range(0, 2)) = 0.9
        _StarTwinkle    ("별 반짝임 속도", Range(0, 3)) = 1.0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _EdgeColor;
                float  _EdgeThickness;
                float  _EdgeGlow;
                float4 _NebulaColor;
                float  _NebulaScale;
                float  _NebulaOpacity;
                float  _NebulaSpeed;
                float  _StarDensity;
                float  _StarBrightness;
                float  _StarTwinkle;
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

            // 노이즈 함수
            float hash(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            float smoothNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(hash(i),               hash(i + float2(1,0)), u.x),
                    lerp(hash(i + float2(0,1)), hash(i + float2(1,1)), u.x),
                    u.y
                );
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float amp = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    v   += smoothNoise(p) * amp;
                    p   *= 2.0;
                    amp *= 0.5;
                }
                return v;
            }

            // 별 생성
            float stars(float2 uv, float density)
            {
                float2 cell = floor(uv * density);
                float2 local = frac(uv * density);
                float2 starPos = float2(hash(cell), hash(cell + 7.3));
                float dist = length(local - starPos);
                float brightness = hash(cell + 13.7);
                float twinkle = sin(_Time.y * _StarTwinkle * (brightness * 3.0 + 0.5)) * 0.5 + 0.5;
                return smoothstep(0.06, 0.0, dist) * brightness * twinkle;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // 베이스 — 거의 단색
                float4 col = _Color;

                // 엣지까지 거리
                float2 edgeDist = min(uv, 1.0 - uv);
                float edge = min(edgeDist.x, edgeDist.y);
                float edgeT = 1.0 - smoothstep(0.0, _EdgeThickness, edge);

                // 엣지 영역에만 성운 노이즈
                float2 nebulaUV = uv * _NebulaScale + float2(_Time.y * _NebulaSpeed, 0);
                float nebula = fbm(nebulaUV);
                col.rgb += _NebulaColor.rgb * nebula * _NebulaOpacity * edgeT;

                // 엣지 글로우
                float glow = pow(edgeT, 0.5);
                col.rgb += _EdgeColor.rgb * glow * _EdgeGlow;

                // 엣지 영역에만 별
                float star = stars(uv, _StarDensity) * edgeT;
                col.rgb += star * _StarBrightness;

                // 내부는 아주 살짝만 별이 보이게
                float innerStar = stars(uv * 1.7, _StarDensity * 0.5) * (1.0 - edgeT) * 0.15;
                col.rgb += innerStar;

                col *= IN.color;
                return half4(col.rgb, col.a);
            }
            ENDHLSL
        }
    }
}
