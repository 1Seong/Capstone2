Shader "Custom/Fullscreen/GhostEffect"
{
    Properties
    {
        _Saturation       ("채도 (0=흑백)",     Range(0, 1)) = 0.3
        _BorderWidth      ("페이드 두께",   Range(0, 0.3)) = 0.12
        _BorderStart      ("테두리 시작",   Range(0.5, 1.0)) = 0.82
        _ColorA           ("아메바 색상 A",      Color) = (0.4, 0.8, 1.0, 1)
        _ColorB           ("아메바 색상 B",      Color) = (0.7, 0.3, 1.0, 1)
        _EmissionStrength ("Emission 강도",      Range(0, 5)) = 1.5
        _Speed            ("변형 속도",          Range(0, 3)) = 0.6
        _BlobScale        ("아메바 크기",        Range(0.5, 3)) = 1.2
        _WiggleAmp        ("꼬불 진폭",          Range(0, 1)) = 0.35
        _WiggleFreq       ("꼬불 빈도",          Range(1, 10)) = 4.0
        _Intensity        ("전체 강도 (0~1)",    Range(0, 1)) = 0.0
        _AspectRatio      ("화면 비율",          Float) = 1.777
        _BorderAlpha ("테두리 불투명도", Range(0, 1)) = 0.85
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Blend SrcAlpha OneMinusSrcAlpha  // ← 이 줄 추가

        Pass
        {
            Name "GhostEffect"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float  _Saturation;
                float  _BorderWidth;
                float4 _ColorA;
                float4 _ColorB;
                float  _EmissionStrength;
                float  _Speed;
                float  _BlobScale;
                float  _WiggleAmp;
                float  _WiggleFreq;
                float  _Intensity;
                float  _AspectRatio;
                float _BorderAlpha;
                float _BorderStart;
            CBUFFER_END

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float smoothNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash(i), b = hash(i+float2(1,0));
                float c = hash(i+float2(0,1)), d = hash(i+float2(1,1));
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }
            float fbm(float2 p) { return smoothNoise(p)*0.6 + smoothNoise(p*2.1+1.7)*0.4; }

            float borderDist(float2 uv)
            {
                float2 d = abs(uv - 0.5) * 2.0;
                return max(d.x, d.y);
            }

            float amoebaBorder(float2 uv, float t)
            {
                float2 centered = uv - 0.5;
                // centered.x *= _AspectRatio;  ← 제거: r 계산이 왜곡됨

                float r = length(centered);  // 이제 0~0.707 범위 (코너 기준)
                float angle = atan2(centered.y, centered.x);

                // noise는 aspect 보정 유지 (시각적 굴곡을 위해)
                float2 noiseUV = float2(cos(angle) * _WiggleFreq + t * 0.4,
                                        sin(angle) * _WiggleFreq - t * 0.3);
                float warp = fbm(noiseUV) * 2.0 - 1.0;

                float edgeRadius = _BorderStart * 0.5 + warp * _WiggleAmp * 0.04;
                float borderMask = smoothstep(edgeRadius, edgeRadius + _BorderWidth, r);

                return borderMask;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float luma = dot(sceneColor.rgb, float3(0.299, 0.587, 0.114));
                float3 desaturated = lerp(sceneColor.rgb, float3(luma, luma, luma),
                                          (1.0 - _Saturation) * _Intensity);

                float t = _Time.y * _Speed;
                float amoeba = amoebaBorder(uv, t);

                float3 ghostCol = lerp(_ColorA.rgb, _ColorB.rgb,
                                        fbm(uv * 3.0 + t * 0.2));
                ghostCol *= (1.0 + _EmissionStrength);

                // amoeba: 0=중앙(씬 그대로), 1=테두리(ghost색)
                float amoebaMask = saturate(amoeba) * _Intensity;
                float3 finalCol  = lerp(desaturated, ghostCol, amoebaMask);
                float  finalAlpha = lerp(1.0, _BorderAlpha, amoebaMask);

                return half4(finalCol, finalAlpha);
            }
            ENDHLSL
        }
    }
}
