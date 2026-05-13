Shader "Custom/Fullscreen/RippleEffect"
{
    Properties
    {
        _RippleColor    ("링 색상",         Color) = (1.0, 1.0, 1.0, 0.8)
        _RippleWidth    ("링 두께",         Range(0.001, 0.1)) = 0.02
        _RippleProgress ("진행도 (0~1)",    Range(0, 1)) = 0.0
        _OriginUV       ("시작 위치 (UV)",  Vector) = (0.5, 0.5, 0, 0)
        _Distortion     ("왜곡 강도",       Range(0, 0.05)) = 0.01
        _DistortionOn   ("왜곡 활성화",     Float) = 1.0
        _AspectRatio    ("화면 비율",       Float) = 1.777
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Blend Off
        Cull Off

        Pass
        {
            Name "RippleEffect"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _RippleColor;
                float  _RippleWidth;
                float  _RippleProgress;
                float4 _OriginUV;
                float  _Distortion;
                float  _DistortionOn;
                float  _AspectRatio;
            CBUFFER_END

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv     = input.texcoord;
                float2 origin = _OriginUV.xy;

                // 화면 비율 보정
                float2 diff = uv - origin;
                diff.x     *= _AspectRatio;
                float dist  = length(diff);

                float maxRadius = 1.5;
                float radius    = _RippleProgress * maxRadius;

                // ── 왜곡 ─────────────────────────────────────────────
                float2 sampleUV = uv;
                if (_DistortionOn > 0.5 && _RippleProgress > 0.0)
                {
                    float distFromRing = abs(dist - radius);
                    float warpMask     = smoothstep(_RippleWidth * 2.0, 0.0, distFromRing);
                    float2 warpDir     = normalize(diff + 0.0001);
                    sampleUV          += warpDir * warpMask * _Distortion;
                }

                // 원본 화면 샘플링 (Blit.hlsl의 _BlitTexture 사용)
                half4 screen = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sampleUV);

                // ── 링 마스크 ─────────────────────────────────────────
                float distFromRing = abs(dist - radius);
                float ringMask     = smoothstep(_RippleWidth, 0.0, distFromRing);
                float fadeOut      = 1.0 - smoothstep(0.5, 1.0, _RippleProgress);
                ringMask          *= fadeOut;

                half4 col  = screen;
                col.rgb    = lerp(col.rgb, _RippleColor.rgb, ringMask * _RippleColor.a);

                return half4(col.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
