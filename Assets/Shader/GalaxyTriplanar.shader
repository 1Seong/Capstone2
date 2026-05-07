Shader "Custom/GalaxyTrilinear"
{
    Properties
    {
        _FogTex         ("Fog Texture",       2D)             = "white" {}
        _FogColor       ("Fog Color",         Color)          = (0.3, 0.1, 0.8, 1)
        _EmissionColor  ("Emission Color",    Color)          = (0.5, 0.2, 1.0, 1)
        _EmissionStr    ("Emission Str",      Range(0, 3))    = 1.2

        _Scale          ("Triplanar Scale",   Range(0.1, 5))  = 1.0
        _BlendSharpness ("Blend Sharpness",   Range(1, 8))    = 2.0

        _FlowX          ("Flow Speed X",      Range(0, 0.3))  = 0.04
        _FlowY          ("Flow Speed Y",      Range(0, 0.3))  = 0.07
        _FlowZ          ("Flow Speed Z",      Range(0, 0.3))  = 0.05

        // UV 왜곡: 안개가 실제로 울렁거리는 느낌
        _DistortStrength("Distort Strength",  Range(0, 0.3))  = 0.08
        _DistortSpeed   ("Distort Speed",     Range(0, 0.2))  = 0.03

        // UV 엣지 마스크: 메시 경계를 부드럽게 소멸
        _EdgeSoftness   ("Edge Softness",     Range(0, 0.5))  = 0.25

        _RevealProgress ("Reveal Progress",   Range(0, 1))    = 0.0
        _RevealEdge     ("Reveal Edge",       Range(0, 0.4))  = 0.12
        _Threshold      ("Dark Cutoff",       Range(0, 0.8))  = 0.3
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
            Name "FogTriplanarPass"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_FogTex);
            SAMPLER(sampler_FogTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                half4 _EmissionColor;
                half  _EmissionStr;

                float _Scale;
                float _BlendSharpness;

                float _FlowX;
                float _FlowY;
                float _FlowZ;

                float _DistortStrength;
                float _DistortSpeed;

                float _EdgeSoftness;

                float _RevealProgress;
                float _RevealEdge;
                float _Threshold;
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
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 posWS = IN.positionWS * _Scale;
                float  t     = _Time.y;

                // ── UV 왜곡용 저주파 노이즈 (느리게 흐름) ─────────
                // 왜곡 자체가 안개의 울렁거림처럼 보임
                float2 distortUV = posWS.xz * (_Scale * 0.4)
                                 + float2(_DistortSpeed, _DistortSpeed * 0.7) * t;
                float2 distort   = SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, distortUV).rg;
                // 0~1 → -1~1 범위로 변환 후 강도 적용
                float2 offset    = (distort - 0.5) * 2.0 * _DistortStrength;

                // ── Triplanar UV (왜곡 오프셋 적용) ──────────────
                float2 uvX = posWS.yz + float2( _FlowX,  _FlowX * 0.7) * t + offset;
                float2 uvY = posWS.xz + float2( _FlowY * 0.8, -_FlowY) * t + offset;
                float2 uvZ = posWS.xy + float2(-_FlowZ,  _FlowZ * 0.9) * t + offset;

                half sX = SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, uvX).r;
                half sY = SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, uvY).r;
                half sZ = SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, uvZ).r;

                // ── Dark Cutoff + 재매핑 ──────────────────────────
                sX = saturate((sX - _Threshold) / max(1.0 - _Threshold, 0.001));
                sY = saturate((sY - _Threshold) / max(1.0 - _Threshold, 0.001));
                sZ = saturate((sZ - _Threshold) / max(1.0 - _Threshold, 0.001));

                // ── 블렌딩 가중치 ──────────────────────────────────
                float3 blend = pow(abs(normalize(IN.normalWS)), _BlendSharpness);
                blend /= dot(blend, float3(1, 1, 1));

                half fogDensity = sX * blend.x + sY * blend.y + sZ * blend.z;

                // ── UV 엣지 마스크: 메시 UV 경계를 부드럽게 소멸 ─
                // smoothstep으로 UV 0~EdgeSoftness, (1-EdgeSoftness)~1 구간을 페이드
                float2 uv       = IN.uv;
                float  maskX    = smoothstep(0.0, _EdgeSoftness, uv.x)
                                * smoothstep(1.0, 1.0 - _EdgeSoftness, uv.x);
                float  maskY    = smoothstep(0.0, _EdgeSoftness, uv.y)
                                * smoothstep(1.0, 1.0 - _EdgeSoftness, uv.y);
                float  edgeMask = maskX * maskY;

                // ── 색상 ──────────────────────────────────────────
                half3 finalColor = _FogColor.rgb
                                 + _EmissionColor.rgb * fogDensity * _EmissionStr;

                // ── Reveal 디졸브 ─────────────────────────────────
                float dissolve = fogDensity - _RevealProgress;
                float alpha    = saturate(dissolve / max(_RevealEdge, 0.001));

                // 엣지 마스크를 알파에 곱함 → 메시 경계 자연스럽게 소멸
                alpha *= edgeMask;

                // 엣지 글로우
                float edgeBand = smoothstep(0.0, _RevealEdge, dissolve)
                               * (1.0 - smoothstep(_RevealEdge, _RevealEdge * 2.0, dissolve));
                finalColor += _EmissionColor.rgb * edgeBand * _EmissionStr * 3.0;

                return half4(finalColor * alpha, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
