Shader "Custom/WorldSpaceGradient"
{
    Properties
    {
        _ColorX ("X축 색", Color) = (1, 0.3, 0.3, 1)
        _ColorY ("Y축 색", Color) = (0.1, 0.7, 0.5, 1)
        _ColorZ ("Z축 색", Color) = (0.75, 0.45, 0.1, 1)
        _CubeCenter ("큐브 중심 (월드)", Vector) = (0, 0, 0, 0)
        _CubeSize ("큐브 크기", Float) = 10.0
        _Luma("채도", Float) = 0.25
        _Whiteness("톤", Float) = 0.3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorX, _ColorY, _ColorZ;
                float4 _CubeCenter;
                float _CubeSize;
                float _Luma;
                float _Whiteness;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.worldPos = TransformObjectToWorld(IN.positionOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 큐브 중심 기준으로 -0.5~0.5 정규화 후 0~1로
                float3 t = (IN.worldPos - _CubeCenter.xyz) / _CubeSize + 0.5;
                t = saturate(t);

                float3 col = _ColorX.rgb * t.x
                           + _ColorY.rgb * t.y
                           + _ColorZ.rgb * t.z;

                // 채도 살짝 낮추고 밝기 lift (축 원색과 분리)
                float luma = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(col, luma, _Luma);
                col = lerp(col, float3(1,1,1), _Whiteness); // 밝고 부드러운 톤
                col = col * 0.9 + 0.07;

                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
