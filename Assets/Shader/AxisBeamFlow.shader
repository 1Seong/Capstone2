Shader "Custom/AxisBeamFlow"
{
    Properties
    {
        _Color          ("색상", Color) = (1, 0.4, 0.4, 1)
        _Opacity        ("최대 투명도", Range(0, 1)) = 0.5
        _Speed          ("흐름 속도", Range(0, 10)) = 3.0
        _BeamLength     ("빔 길이", Range(0.01, 0.5)) = 0.15
        _BeamSharpness  ("빔 선명도", Range(1, 16)) = 4.0
        _BeamCount      ("빔 개수", Range(1, 10)) = 4.0
        _BeamWidth      ("빔 폭", Range(0.01, 0.5)) = 0.1
        _FadeDistance   ("표면 페이드 거리", Range(0.1, 5)) = 1.5
        _FlowAxis       ("흐름 축 (0=X 1=Y 2=Z)", Float) = 1.0
        _CubeCenter     ("큐브 중심", Vector) = (0, 0, 0, 0)
        _CubeSize       ("큐브 크기", Float) = 10.0
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

        Blend SrcAlpha One
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
                float  _BeamLength;
                float  _BeamSharpness;
                float  _BeamCount;
                float  _BeamWidth;
                float  _FadeDistance;
                float  _FlowAxis;
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
                float3 worldPos   : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv         = IN.uv;
                float3 worldPos = TransformObjectToWorld(IN.positionOS);
                OUT.eyeDepth   = -TransformWorldToView(worldPos).z;
                OUT.screenPos  = ComputeScreenPos(OUT.positionCS);
                OUT.worldPos   = worldPos;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 wp  = IN.worldPos;
                float2 uv  = IN.uv;

                // ── 흐름 좌표: 월드 포지션 기반 ─────────────────────
                float flowCoord;
                if (_FlowAxis < 0.5)        flowCoord = wp.x;
                else if (_FlowAxis < 1.5)   flowCoord = wp.y;
                else                         flowCoord = wp.z;

                // ── 빔 위치: UV 기반으로 여러 빔 배치 ───────────────
                // uv.x를 _BeamCount로 나눠서 각 셀에 빔 하나씩
                float beamCell      = frac(uv.x * _BeamCount);
                float distFromCenter = abs(beamCell - 0.5) * 2.0;  // 0=중심, 1=가장자리
                // _BeamWidth로 빔이 셀에서 차지하는 비율 제어
                // 0.2면 셀의 20%만 빔, 나머지 80%는 어두움
                float widthFade = smoothstep(_BeamWidth, _BeamWidth * 0.3, distFromCenter);
                // ── 빛 흐름: 월드 포지션 긴 방향으로만 이동 ────────
                float flow     = frac(flowCoord * 0.2 - _Time.y * _Speed * 0.1);
                float beamHead = smoothstep(_BeamLength, 0.0, flow);
                float beamTail = smoothstep(0.0, _BeamLength * 0.3, flow)
                               * smoothstep(_BeamLength, _BeamLength * 0.5, flow);
                float beam     = pow(saturate(beamHead + beamTail * 0.3), _BeamSharpness);

                // ── Depth fade ───────────────────────────────────────
                float2 screenUV  = IN.screenPos.xy / IN.screenPos.w;
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float depthFade  = saturate((sceneDepth - IN.eyeDepth) / _FadeDistance);

                // ── Cube fade ────────────────────────────────────────
                float3 localPos = abs(wp - _CubeCenter.xyz) / (_CubeSize * 0.5);
                float  cubeDist = max(localPos.x, max(localPos.y, localPos.z));
                float  cubeFade = smoothstep(1.0, 1.0 + _CubeFadeMargin / (_CubeSize * 0.5), cubeDist);

                float alpha = beam * widthFade * depthFade * cubeFade * _Opacity;

                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
