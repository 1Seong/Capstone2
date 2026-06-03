Shader "Custom/2DGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _Color ("Tint", Color) = (1, 1, 1, 1)

        [HDR] _GlowColor ("Glow Color", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Float) = 1

        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Flip ("Flip", Vector) = (1, 1, 1, 1)

        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
            "PreviewType"="Plane"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "SpriteUnlit"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_AlphaTex);
            SAMPLER(sampler_AlphaTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _GlowColor;
                half _Intensity;
                half _EnableExternalAlpha;
            CBUFFER_END

            half4 _RendererColor;
            float4 _Flip;

            struct Attributes
            {
                float3 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            float3 UnityFlipSprite(float3 positionOS, float2 flip)
            {
                return float3(positionOS.xy * flip, positionOS.z);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 positionOS = UnityFlipSprite(input.positionOS, _Flip.xy);

                output.positionCS = TransformObjectToHClip(positionOS);
                output.uv = input.uv;

                output.color = input.color * _Color * _RendererColor;

                return output;
            }

            half4 SampleSpriteTexture(float2 uv)
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                #if ETC1_EXTERNAL_ALPHA
                    half alpha = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, uv).r;
                    color.a = lerp(color.a, alpha, _EnableExternalAlpha);
                #endif

                return color;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SampleSpriteTexture(input.uv);

                half4 color = texColor * input.color;

                // 알파는 원본 Sprite 형태 유지
                color.a = texColor.a * input.color.a;

                // RGB만 HDR로 증폭
                color.rgb *= _GlowColor.rgb * _Intensity;

                return color;
            }

            ENDHLSL
        }
    }
}
