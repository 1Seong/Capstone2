Shader "Custom/CrystalTile"
{
    Properties
    {
        _GlassColor     ("Glass Color",      Color)        = (0.7, 0.8, 1.0, 1)
        _GlassOpacity   ("Glass Opacity",    Range(0, 1))  = 0.15
        _FresnelPow     ("Fresnel Power",    Range(0.5, 6))= 2.5
        _FresnelStr     ("Fresnel Strength", Range(0, 2))  = 1.0

        _LightColor     ("Light Color",      Color)        = (0.6, 0.3, 1.0, 1)
        _LightStr       ("Light Strength",   Range(0, 10)) = 5.0

        // 0 = 꺼짐, 1 = 완전히 켜짐
        _ActivateProgress("Activate Progress", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        // ── 패스 1: 내부 불빛 (뒷면) ─────────────────────────
        Pass
        {
            Name "InnerLight"
            Cull Front
            ZWrite Off
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment fragInner
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _GlassColor;
                half  _GlassOpacity;
                half  _FresnelPow;
                half  _FresnelStr;
                half4 _LightColor;
                half  _LightStr;
                float _ActivateProgress;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings   { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 fragInner(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                float p = _ActivateProgress;

                // 내부 발광: 활성화 진행도에 따라 밝아짐
                half3 color = _LightColor.rgb * _LightStr * p;
                half  alpha = p * 0.85;

                return half4(color * alpha, alpha);
            }
            ENDHLSL
        }

        // ── 패스 2: 유리 표면 (앞면) ─────────────────────────
        Pass
        {
            Name "GlassSurface"
            Cull Back
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   vert2
            #pragma fragment fragGlass
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _GlassColor;
                half  _GlassOpacity;
                half  _FresnelPow;
                half  _FresnelStr;
                half4 _LightColor;
                half  _LightStr;
                float _ActivateProgress;
            CBUFFER_END

            struct Attributes2 { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings2   { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings2 vert2(Attributes2 IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings2 OUT;
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 fragGlass(Varyings2 IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float  p = _ActivateProgress;

                // Fresnel: 가장자리일수록 불투명 → 유리 두께감
                float fresnel    = pow(1.0 - saturate(dot(N, V)), _FresnelPow);

                // 켜질수록 유리 표면에 발광색이 살짝 반영
                half3 color = lerp(_GlassColor.rgb,
                                   _LightColor.rgb,
                                   p * 0.3);
                color += _LightColor.rgb * fresnel * _FresnelStr * p;

                half alpha = saturate(_GlassOpacity + fresnel * 0.6);

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
