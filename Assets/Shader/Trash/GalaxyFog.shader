Shader "Custom/GalaxyFog"
{
    Properties
    {
        // 색상 3단계
        _ColorDeep    ("Color Deep",    Color) = (0.02, 0.05, 0.25, 1)  // 깊은 곳 (짙은 파랑)
        _ColorMid     ("Color Mid",     Color) = (0.35, 0.1,  0.8,  1)  // 중간 (보라)
        _ColorSurface ("Color Surface", Color) = (0.7,  0.4,  1.0,  1)  // 표면 (밝은 보라)
        _EmissionColor("Emission",      Color) = (0.5,  0.2,  1.0,  1)
        _EmissionStr  ("Emission Strength", Range(0, 3)) = 1.2

        [NoScaleOffset]
        _NoiseTex     ("Noise Texture (RG)", 2D) = "white" {}

        _FlowSpeed    ("Flow Speed",  Range(0, 0.5)) = 0.08
        _FlowScale    ("Flow Scale",  Range(1, 6))   = 3.0
        _Turbulence   ("Turbulence",  Range(0, 1))   = 0.4

        // Parallax 깊이감
        _ParallaxDepth("Parallax Depth", Range(0, 0.3)) = 0.12  // 클수록 깊어 보임
        _ParallaxSteps("Parallax Steps", Range(2, 6))   = 4     // 성능과 품질 트레이드오프

        _RevealProgress("Reveal Progress", Range(0, 1)) = 0.0
        _RevealEdge    ("Reveal Edge",     Range(0, 0.4)) = 0.12
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "GalaxyFogPass"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off

            // Premultiplied Alpha: 오버드로우 비용이 일반 Alpha보다 낮음
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorDeep;
                half4 _ColorMid;
                half4 _ColorSurface;
                half4 _EmissionColor;
                half  _EmissionStr;

                float _FlowSpeed;
                float _FlowScale;
                float _Turbulence;

                float _ParallaxDepth;
                float _ParallaxSteps;

                float _RevealProgress;
                float _RevealEdge;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                // 탄젠트 공간 뷰 방향 (Parallax에 필요)
                float3 viewDirTS   : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = IN.uv;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);

                // 탄젠트 공간 뷰 방향 계산
                // 큐브 표면 방향에 따라 탄젠트를 직접 구성
                float3 posWS  = TransformObjectToWorld(IN.positionOS.xyz);
                float3 viewWS = GetWorldSpaceViewDir(posWS);
                float3 N      = TransformObjectToWorldNormal(IN.normalOS);

                // N에 수직인 임의의 탄젠트 벡터 구성
                float3 up      = abs(N.y) < 0.99 ? float3(0,1,0) : float3(1,0,0);
                float3 tangent  = normalize(cross(up, N));
                float3 binormal = cross(N, tangent);

                // 월드 뷰 방향 → 탄젠트 공간
                OUT.viewDirTS = float3(
                    dot(viewWS, tangent),
                    dot(viewWS, binormal),
                    dot(viewWS, N)
                );

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float2 uv  = IN.uv;
                float  t   = _Time.y;

                // 탄젠트 공간 뷰 방향 정규화
                float3 viewTS  = normalize(IN.viewDirTS);
                // 표면 법선 방향(Z) 기준으로 XY 성분만 Parallax에 사용
                float2 parallaxDir = viewTS.xy / max(viewTS.z, 0.1);

                // ── Parallax 레이어드 샘플링 ──────────────────────
                // 뷰 방향으로 UV를 단계적으로 오프셋 → 깊이감
                // 각 레이어가 다른 깊이에 있는 안개층을 표현
                int   steps      = (int)clamp(_ParallaxSteps, 2, 6);
                float stepDepth  = _ParallaxDepth / steps;

                float fogAccum   = 0.0;  // 누적 밀도
                float depthWeight = 0.0; // 깊이 가중치 (색상 결정용)

                for (int i = 0; i < steps; i++)
                {
                    float  layerDepth  = stepDepth * i;
                    float2 offsetUV    = uv - parallaxDir * layerDepth;

                    // 레이어마다 흐름 방향/속도를 다르게 → 층마다 다른 안개
                    float  fi          = (float)i;
                    float2 flow1 = offsetUV * _FlowScale
                                 + float2( _FlowSpeed,  _FlowSpeed * 0.6) * t
                                 + fi * 0.13;
                    float2 flow2 = offsetUV * _FlowScale * 1.6
                                 + float2(-_FlowSpeed * 0.7, _FlowSpeed) * t
                                 + fi * 0.27;

                    float2 n1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, flow1).rg;
                    float2 n2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, flow2).rg;

                    float layerDensity = saturate(
                        (n1.r + n2.r) * 0.5
                      + (n1.g + n2.g) * 0.5 * _Turbulence
                    );

                    // 깊은 레이어일수록 가중치 감소 (빛이 덜 닿는 느낌)
                    float weight = 1.0 - (fi / steps);
                    fogAccum    += layerDensity * weight;
                    depthWeight += weight;
                }

                // 정규화
                fogAccum = saturate(fogAccum / max(depthWeight, 0.001));

                // ── Fresnel ───────────────────────────────────────
                float3 N       = normalize(IN.normalWS);
                float3 V       = normalize(IN.viewDirTS); // 근사값으로 사용
                float  fresnel = 1.0 - saturate(dot(N, normalize(
                                     TransformObjectToWorldNormal(float3(0,0,1)))));
                fresnel = pow(fresnel, 2.5);

                // ── 3단계 색상 ────────────────────────────────────
                // fogAccum 낮음 → Deep, 중간 → Mid, 높음 → Surface
                half3 fogColor = fogAccum < 0.5
                    ? lerp(_ColorDeep.rgb, _ColorMid.rgb, fogAccum * 2.0)
                    : lerp(_ColorMid.rgb,  _ColorSurface.rgb, (fogAccum - 0.5) * 2.0);

                // 가장자리 Fresnel은 Deep 쪽으로
                fogColor = lerp(fogColor, _ColorDeep.rgb, fresnel * 0.6);

                // ── 발광 ──────────────────────────────────────────
                half3 emission = _EmissionColor.rgb * fogAccum * _EmissionStr;

                half3 finalColor = fogColor + emission;

                // ── Reveal 디졸브 ─────────────────────────────────
                float dissolve  = fogAccum - _RevealProgress;
                float alpha     = saturate(dissolve / max(_RevealEdge, 0.001));

                // 경계 엣지 글로우
                float edgeBand  = smoothstep(0.0, _RevealEdge, dissolve)
                                * (1.0 - smoothstep(_RevealEdge, _RevealEdge * 2.0, dissolve));
                finalColor += _EmissionColor.rgb * edgeBand * _EmissionStr * 3.0;

                // Premultiplied alpha 적용
                return half4(finalColor * alpha, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
