Shader "Custom/AuroraEffect"
{
    Properties
    {
        _Color      ("색상", Color) = (0.5, 0.8, 1.0, 1)
        _Opacity    ("최대 투명도", Range(0, 1)) = 0.3
        _Speed      ("흐름 속도", Range(0, 2)) = 0.2
        _NoiseScale ("노이즈 스케일", Range(0.1, 5)) = 1.5
        _CurtainSharpness ("커튼 선명도", Range(1, 8)) = 3.0
        _FadeDistance ("표면 페이드 거리", Range(0.1, 5)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _Opacity;
                float  _Speed;
                float  _NoiseScale;
                float  _CurtainSharpness;
                float  _FadeDistance;
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
                float  eyeDepth   : TEXCOORD1;
                float4 screenPos  : TEXCOORD2;
            };

            // 간단한 2D 노이즈
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
                float2 u = f * f * (3.0 - 2.0 * f); // smoothstep

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
                    v += smoothNoise(p) * amp;
                    p *= 2.0;
                    amp *= 0.5;
                }
                return v;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                float3 worldPos = TransformObjectToWorld(IN.positionOS);
                OUT.eyeDepth = -TransformWorldToView(worldPos).z;
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // 시간에 따라 흐르는 노이즈
                float2 noiseUV = uv * _NoiseScale + float2(_Time.y * _Speed, 0);
                float noise = fbm(noiseUV);

                // 오로라 커튼 형태: 세로 방향으로 줄기 생성
                float curtain = pow(noise, _CurtainSharpness);

                // 가장자리 fade (쿼드 경계가 티나지 않도록)
                float edgeFade = uv.x * (1.0 - uv.x) * uv.y * (1.0 - uv.y);
                edgeFade = saturate(edgeFade * 16.0);

                // Depth fade
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float sceneDepth = LinearEyeDepth(
                    SampleSceneDepth(screenUV), _ZBufferParams
                );
                float depthFade = saturate((sceneDepth - IN.eyeDepth) / _FadeDistance);

                // 색상 변화 (노이즈에 따라 살짝 hue shift)
                float3 col = _Color.rgb;
                col += float3(noise * 0.1, -noise * 0.05, noise * 0.08);
                col = saturate(col);

                float alpha = curtain * edgeFade * depthFade * _Opacity;

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
