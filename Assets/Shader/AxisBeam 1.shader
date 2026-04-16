Shader "Custom/AxisBeam"
{
    Properties
    {
        _Color          ("색상", Color) = (1, 0.4, 0.4, 1)
        _Opacity        ("최대 투명도", Range(0, 1)) = 0.5
        _Speed          ("흐름 속도", Range(0, 5)) = 1.0
        _PulseCount     ("펄스 개수", Range(1, 10)) = 3.0
        _PulseSharpness ("펄스 선명도", Range(1, 16)) = 4.0
        _BeamWidth      ("빔 폭 소프트니스", Range(0.1, 2)) = 0.8
        _FadeDistance   ("표면 페이드 거리", Range(0.1, 5)) = 1.5
        _CubeCenter ("큐브 중심", Vector) = (0, 0, 0, 0)
        _CubeSize   ("큐브 크기", Float) = 10.0
        _CubeFadeMargin ("큐브 페이드 여백", Range(0.1, 5)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
        }

        Blend SrcAlpha One  // Additive — 겹칠수록 밝아짐
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
                float  _PulseCount;
                float  _PulseSharpness;
                float  _BeamWidth;
                float  _FadeDistance;
                float4 _CubeCenter;
                float  _CubeSize;
                float  _CubeFadeMargin;
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
                float3 worldPos : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                float3 worldPos = TransformObjectToWorld(IN.positionOS);
                OUT.eyeDepth = -TransformWorldToView(worldPos).z;
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                OUT.worldPos = TransformObjectToWorld(IN.positionOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // uv.x: 축 방향 (0=시작, 1=끝)
                // uv.y: 빔 폭 방향 (0~1, 중심=0.5)

                // - → + 방향으로 흐르는 펄스
                float flow = frac(uv.x * _PulseCount - _Time.y * _Speed);
                float pulse = pow(sin(flow * 3.14159), _PulseSharpness);

                // 빔 폭 방향 소프트 페이드 (중심이 밝고 가장자리가 어두움)
                float widthFade = 1.0 - abs(uv.y - 0.5) * 2.0;
                widthFade = pow(saturate(widthFade), _BeamWidth);

                // 양 끝 fade (빔 시작/끝이 자연스럽게 사라짐)
                float endFade = smoothstep(0.0, 0.08, uv.x) * smoothstep(1.0, 0.92, uv.x);

                // Depth fade
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float sceneDepth = LinearEyeDepth(
                    SampleSceneDepth(screenUV), _ZBufferParams
                );
                float depthFade = saturate((sceneDepth - IN.eyeDepth) / _FadeDistance);

                // 수정
                float3 localPos = abs(IN.worldPos - _CubeCenter.xyz) / (_CubeSize * 0.5);
                float cubeDist = max(localPos.x, max(localPos.y, localPos.z));

                // cubeDist: 1.0 = 큐브 표면, 1.0 미만 = 안쪽, 초과 = 바깥
                // 표면에서 margin 거리까지 0→1로 페이드
                float cubeFade = smoothstep(1.0, 1.0 + _CubeFadeMargin / (_CubeSize * 0.5), cubeDist);

                float alpha = pulse * widthFade * endFade * depthFade * cubeFade * _Opacity;

                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
