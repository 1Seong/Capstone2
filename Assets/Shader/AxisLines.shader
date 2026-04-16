Shader "Custom/AxisLines"
{
    Properties
    {
        _Color ("색상", Color) = (1, 0.3, 0.3, 1)
        _Opacity ("투명도", Range(0, 1)) = 0.4
        _Speed ("흐르는 속도", Range(0, 2)) = 0.3
        _FadeDistance ("표면 페이드 거리", Range(0.1, 5)) = 1.5
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
                float _Opacity;
                float _Speed;
                float _FadeDistance;
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
                float4 screenPos : TEXCOORD2;
            };

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
                // 양 끝 fade
                float along = frac(IN.uv.x + _Time.y * _Speed);
                float endFade = sin(IN.uv.x * 3.14159);

                // 흐르는 밝기 변화 (별자리 느낌)
                float sparkle = sin(along * 6.28318 * 3.0) * 0.5 + 0.5;
                sparkle = pow(sparkle, 4.0); // 뾰족하게

                // Depth fade (큐브 표면 근처에서 사라짐)
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float sceneDepth = LinearEyeDepth(
                    SampleSceneDepth(screenUV),
                    _ZBufferParams
                );
                float depthFade = saturate((sceneDepth - IN.eyeDepth) / _FadeDistance);
                float alpha = endFade * _Opacity * depthFade;
                alpha += sparkle * endFade * _Opacity * 0.5 * depthFade;
                alpha = saturate(alpha);

                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
