Shader "Custom/Silhouette"
{
    Properties
    {
        _SilhouetteColor ("Silhouette Color", Color) = (0.2, 0.6, 1.0, 0.6)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        // Pass 1: 실루엣 (큐브에 가려진 부분에만)
        Pass
        {
            Name "Silhouette"
            ZTest Greater       // 깊이 테스트 역전 — 가려진 부분에만 그림
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionHCS : SV_POSITION; };

            float4 _SilhouetteColor;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _SilhouetteColor;
            }
            ENDHLSL
        }

        // Pass 2: 일반 렌더 (기존 머티리얼 위에 덧씌울 것이므로 생략 가능,
        //          플레이어 본래 머티리얼을 그대로 두면 됨)
    }
}
