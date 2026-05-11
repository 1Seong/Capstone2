Shader "Custom/Gimmick/Inverter"
{
    Properties
    {
        _PulseColor       ("펄스 색상",         Color) = (1.0, 1.0, 1.0, 1)
        _PulseSpeed       ("펄스 속도",         Range(0.1, 5)) = 1.5
        _PulseThickness   ("펄스 두께",         Range(0.01, 0.3)) = 0.08
        _PulseCount       ("동시 펄스 수",      Range(1, 4)) = 2.0
        _EmissionStrength ("Emission 강도",     Range(0, 5)) = 2.0
        _SpikeAmp         ("삐죽 진폭",         Range(0, 0.3)) = 0.1
        _SpikeFreq        ("삐죽 빈도",         Range(2, 20)) = 8.0
        _SpikeSpeed       ("삐죽 변형 속도",    Range(0, 3)) = 1.2
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
                float4 _PulseColor;
                float  _PulseSpeed;
                float  _PulseThickness;
                float  _PulseCount;
                float  _EmissionStrength;
                float  _SpikeAmp;
                float  _SpikeFreq;
                float  _SpikeSpeed;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float smoothNoise(float2 p)
            {
                float2 i = floor(p); float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash(i), b = hash(i+float2(1,0)), c = hash(i+float2(0,1)), d = hash(i+float2(1,1));
                return lerp(lerp(a,b,u.x),lerp(c,d,u.x),u.y);
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
                float t = _Time.y;
                float2 centered = IN.uv - 0.5;
                float  r = length(centered), angle = atan2(centered.y, centered.x);
                float circleMask = smoothstep(0.5, 0.45, r);
                if (circleMask <= 0.001) return half4(0,0,0,0);
                float2 spikeUV = float2(cos(angle)*_SpikeFreq+t*_SpikeSpeed*0.5, sin(angle)*_SpikeFreq-t*_SpikeSpeed*0.3);
                float warpedR = r - (smoothNoise(spikeUV)*2.0-1.0)*_SpikeAmp*r*2.0;
                float totalAlpha = 0.0;
                int pulseCount = clamp((int)_PulseCount, 1, 4);
                for (int i = 0; i < pulseCount; i++)
                {
                    float phase = frac(warpedR/0.5 - t*_PulseSpeed + (float)i/_PulseCount);
                    float half_t = _PulseThickness*0.5;
                    float ring = smoothstep(0.0,half_t,phase)*smoothstep(_PulseThickness,half_t,phase);
                    float falloff = 1.0-smoothstep(0.1,0.48,warpedR); falloff=falloff*falloff;
                    totalAlpha += ring*falloff*smoothstep(0.0,0.1,warpedR);
                }
                totalAlpha = saturate(totalAlpha);
                if (totalAlpha <= 0.001) return half4(0,0,0,0);
                return half4(_PulseColor.rgb*(1.0+_EmissionStrength), totalAlpha*circleMask);
            }
            ENDHLSL
        }
    }
}
