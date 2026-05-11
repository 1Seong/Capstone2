Shader "Custom/Gimmick/Ghost"
{
    Properties
    {
        _ColorA           ("색상 A",          Color) = (0.4, 0.8, 1.0, 1)
        _ColorB           ("색상 B",          Color) = (0.7, 0.3, 1.0, 1)
        _EmissionStrength ("Emission 강도",   Range(0, 5)) = 1.5
        _Speed            ("변형 속도",       Range(0, 3)) = 0.6
        _BlobScale        ("아메바 크기",     Range(0.5, 3)) = 1.2
        _WiggleAmp        ("꼬불 진폭",       Range(0, 1)) = 0.35
        _WiggleFreq       ("꼬불 빈도",       Range(1, 10)) = 4.0
        _LayerCount       ("레이어 수",       Range(1, 4)) = 3.0
        _LayerSpread      ("레이어 간격",     Range(0, 0.3)) = 0.08
    }
    SubShader
    {
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" }

        Stencil
        {
            Ref 1
            Comp NotEqual
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA;
                float4 _ColorB;
                float  _EmissionStrength;
                float  _Speed;
                float  _BlobScale;
                float  _WiggleAmp;
                float  _WiggleFreq;
                float  _LayerCount;
                float  _LayerSpread;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float smoothNoise(float2 p)
            {
                float2 i = floor(p); float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash(i), b = hash(i+float2(1,0)), c = hash(i+float2(0,1)), d = hash(i+float2(1,1));
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }
            float fbm(float2 p) { return smoothNoise(p)*0.6 + smoothNoise(p*2.1+1.7)*0.4; }
            float amoebaSDF(float2 uv, float timeOffset, float radiusOffset)
            {
                float2 centered = uv - 0.5;
                float  r = length(centered), angle = atan2(centered.y, centered.x);
                float  t = _Time.y * _Speed + timeOffset;
                float2 noiseUV = float2(cos(angle)*_WiggleFreq+t*0.4, sin(angle)*_WiggleFreq-t*0.3);
                float warp = fbm(noiseUV) * 2.0 - 1.0;
                return r - ((0.32 + radiusOffset) * _BlobScale + warp * _WiggleAmp * 0.5);
            }
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                return OUT;
            }
            half4 frag(Varyings IN) : SV_Target
            {
                float totalAlpha = 0.0, colorBlend = 0.0;
                int layers = clamp((int)_LayerCount, 1, 4);
                for (int i = 0; i < layers; i++)
                {
                    float fi = (float)i;
                    float sdf = amoebaSDF(IN.uv, fi*1.3, fi*_LayerSpread*-1.0);
                    float layerAlpha = smoothstep(0.02,-0.04,sdf)*0.35 + smoothstep(0.06,0.0,abs(sdf))*0.5;
                    colorBlend += (fi/max(layers-1,1))*layerAlpha;
                    totalAlpha += layerAlpha;
                }
                totalAlpha = saturate(totalAlpha);
                if (totalAlpha <= 0.001) return half4(0,0,0,0);
                colorBlend = saturate(colorBlend/totalAlpha);
                float3 col = lerp(_ColorA.rgb, _ColorB.rgb, colorBlend) * (1.0+_EmissionStrength);
                float r = length(IN.uv-0.5);
                col += lerp(_ColorA.rgb,_ColorB.rgb,0.5)*smoothstep(0.25,0.0,r)*0.4*_EmissionStrength;
                return half4(col, totalAlpha);
            }
            ENDHLSL
        }
    }
}
