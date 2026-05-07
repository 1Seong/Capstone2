Shader "Custom/UIBorderLine"
{
    Properties
    {
        _MainTex        ("Texture",          2D)    = "white" {}
        _BorderColor    ("Border Color",     Color) = (1,1,1,1)
        _BorderWidth    ("Border Width",     Float) = 2.0
        _InnerColor     ("Inner Color",      Color) = (0,0,0,0.5)
        _UseInnerColor  ("Use Inner Color",  Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _MainTex_TexelSize;
            float4    _BorderColor;
            float     _BorderWidth;
            float4    _InnerColor;
            float     _UseInnerColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 texColor = tex2D(_MainTex, i.uv) * i.color;

                float2 pixel  = _MainTex_TexelSize.xy * _BorderWidth;
                bool isBorder = i.uv.x < pixel.x      || i.uv.x > 1.0 - pixel.x
                             || i.uv.y < pixel.y      || i.uv.y > 1.0 - pixel.y;

                if (isBorder)
                {
                    // 테두리에도 버텍스 컬러(Tint) 반영
                    half4 tintedBorder = _BorderColor * i.color;
                    return half4(tintedBorder.rgb, tintedBorder.a * texColor.a);
                }
                else
                {
                    // _InnerColor에 버텍스 컬러를 곱해 Color Tint Transition 반영
                    half4 tintedInner = _InnerColor * i.color;
                    half4 inner = lerp(texColor, tintedInner, _UseInnerColor);
                    return inner;
                }
            }
            ENDHLSL
        }
    }
}
