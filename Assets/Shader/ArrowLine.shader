Shader "Custom/ArrowLine"
{
    Properties
    {
        _Color     ("Line Color", Color) = (0.2, 0.8, 1.0, 1.0)
        _DashCount ("Dash Count", Float) = 8.0
        _DashRatio ("Dash Ratio", Range(0.01, 0.99)) = 0.6
        _Speed     ("Flow Speed", Float) = 1.0
        _Brightness("Brightness", Float) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Name "Arrow"
            ZTest LEqual
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            float4 _Color;
            float  _DashCount;
            float  _DashRatio;
            float  _Speed;
            float  _Brightness;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 점선 패턴 — 0~DashRatio 구간이 화살표 하나
                float scrolled = IN.uv.x * _DashCount - _Time.y * _Speed;
                float dash = frac(scrolled);

                // 공백 구간 제거
                if (dash > _DashRatio) discard;

                // < 모양: dash가 0일 때 넓고, DashRatio에 가까울수록 좁아짐
                // dash=0 → 뾰족한 끝 (앞), dash=DashRatio → 넓은 끝 (뒤)
                // position0→position1 방향으로 흐르므로 뾰족한 쪽이 진행 방향
                float t = 1.0 - dash / _DashRatio;          // 0(앞뾰족) ~ 1(뒤넓음)
                float halfWidth = 0.5 * t;             // 허용 v 범위
                float distFromCenter = abs(IN.uv.y - 0.5);

                if (distFromCenter > halfWidth) discard;

                half4 col = _Color;
                col.rgb *= _Brightness;
                return col;
            }
            ENDHLSL
        }
    }
}
