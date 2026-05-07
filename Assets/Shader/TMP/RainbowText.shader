Shader "Custom/TMP/RainbowText"
{
    Properties
    {
        _FaceColor          ("Face Color", Color) = (1,1,1,1)
        _FaceDilate         ("Face Dilate", Range(-1,1)) = 0

        _OutlineColor       ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth       ("Outline Thickness", Range(0,1)) = 0
        _OutlineSoftness    ("Outline Softness", Range(0,1)) = 0

        _UnderlayColor      ("Border Color", Color) = (0,0,0,0.5)
        _UnderlayOffsetX    ("Border OffsetX", Range(-1,1)) = 0
        _UnderlayOffsetY    ("Border OffsetY", Range(-1,1)) = 0
        _UnderlayDilate     ("Border Dilate", Range(-1,1)) = 0
        _UnderlaySoftness   ("Border Softness", Range(0,1)) = 0

        _WeightNormal       ("Weight Normal", Float) = 0
        _WeightBold         ("Weight Bold", Float) = 0.5

        _ShaderFlags        ("Flags", Float) = 0
        _ScaleRatioA        ("Scale RatioA", Float) = 1
        _ScaleRatioB        ("Scale RatioB", Float) = 1
        _ScaleRatioC        ("Scale RatioC", Float) = 1

        _MainTex            ("Font Atlas", 2D) = "white" {}
        _TextureWidth       ("Texture Width", Float) = 512
        _TextureHeight      ("Texture Height", Float) = 512
        _GradientScale      ("Gradient Scale", Float) = 5
        _ScaleX             ("Scale X", Float) = 1
        _ScaleY             ("Scale Y", Float) = 1
        _PerspectiveFilter  ("Perspective Correction", Range(0,1)) = 0.875
        _Sharpness          ("Sharpness", Range(-1,1)) = 0

        _VertexOffsetX      ("Vertex OffsetX", Float) = 0
        _VertexOffsetY      ("Vertex OffsetY", Float) = 0

        // ── 무지개 효과 프로퍼티 ──
        _RainbowSpeed       ("무지개 흐름 속도", Range(0, 2)) = 0.4
        _RainbowScale       ("무지개 가로 스케일", Range(0.1, 5)) = 1.5
        _Saturation         ("채도 (파스텔 강도)", Range(0, 1)) = 0.45
        _Brightness         ("밝기", Range(0, 2)) = 0.95
        _FaceOpacity        ("텍스트 투명도", Range(0, 1)) = 1.0

        _StencilComp        ("Stencil Comparison", Float) = 8
        _Stencil            ("Stencil ID", Float) = 0
        _StencilOp          ("Stencil Operation", Float) = 0
        _StencilWriteMask   ("Stencil Write Mask", Float) = 255
        _StencilReadMask    ("Stencil Read Mask", Float) = 255

        _CullMode           ("Cull Mode", Float) = 0
        _ColorMask          ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull [_CullMode]
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            // TMP SDF 샘플링에 필요한 변수들
            sampler2D   _MainTex;
            float       _TextureWidth;
            float       _TextureHeight;
            float       _GradientScale;
            float       _ScaleX;
            float       _ScaleY;
            float       _PerspectiveFilter;
            float       _Sharpness;

            float4      _FaceColor;
            float       _FaceDilate;
            float       _OutlineWidth;
            float       _OutlineSoftness;
            float4      _OutlineColor;
            float       _WeightNormal;
            float       _WeightBold;
            float       _ScaleRatioA;
            float       _ScaleRatioB;
            float       _ScaleRatioC;

            // 무지개 프로퍼티
            float       _RainbowSpeed;
            float       _RainbowScale;
            float       _Saturation;
            float       _Brightness;
            float       _FaceOpacity;

            struct appdata
            {
                float4 vertex   : POSITION;
                float3 normal   : NORMAL;
                float4 color    : COLOR;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float4 color    : COLOR;
                float2 atlas    : TEXCOORD0;   // Font atlas UV
                float2 screenUV : TEXCOORD1;   // 무지개용 스크린 UV
                float  scale    : TEXCOORD2;
            };

            // ── HSV → RGB 변환 ───────────────────────────────────────
            float3 hsv2rgb(float h, float s, float v)
            {
                float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
                float3 p = abs(frac(h + K.xyz) * 6.0 - K.www);
                return v * lerp(K.xxx, saturate(p - K.xxx), s);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // TMP 버텍스 컬러 (Bold 가중치 등 인코딩됨)
                o.color = v.color;

                // Font atlas UV
                o.atlas = v.texcoord0;

                // 무지개 색상 계산용: 오브젝트 공간 X 위치를 UV로 사용
                // 글자 위치 기반이라 화면 이동해도 색이 유지됨
                o.screenUV = float2(v.vertex.x, v.vertex.y);

                // TMP SDF 스케일 계산 (Bold 여부는 color.a로 인코딩)
                bool  isBold  = v.texcoord1.y > 0;
                float weight  = isBold ? _WeightBold : _WeightNormal;
                float sd      = (weight + _FaceDilate) * _ScaleRatioA * 0.5;
                float scale   = rsqrt(v.texcoord1.x) * v.texcoord1.x * 1.5 * _GradientScale;
                o.scale = scale;

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // ── TMP SDF 샘플링 ───────────────────────────────────
                half d = tex2D(_MainTex, i.atlas).a;

                float weight    = lerp(_WeightNormal, _WeightBold, i.color.a < 0.1);
                float sd        = (weight + _FaceDilate) * _ScaleRatioA * 0.5;
                float edgeWidth = max(0.001, i.scale * (_OutlineWidth * _ScaleRatioB * 0.5
                                    + _OutlineSoftness * _ScaleRatioB * 0.5));
                float faceEdge  = max(0.001, i.scale * (1.0 - _Sharpness) * 0.25);

                // 글자 내부 마스크 (SDF → 0/1 알파)
                float face      = smoothstep(0.5 - sd - faceEdge, 0.5 - sd + faceEdge, d);

                // ── 무지개 색상 생성 ─────────────────────────────────
                // 가로 위치 + 시간으로 hue 결정
                float hue = frac(i.screenUV.x * _RainbowScale * 0.1 - _Time.y * _RainbowSpeed);

                // 파스텔: 채도를 낮추고 밝기를 높게 유지
                // _Saturation 0.4~0.5 = 파스텔, 0.8~1.0 = 원색
                float3 rainbow = hsv2rgb(hue, _Saturation, _Brightness);

                // FaceColor와 혼합 — FaceColor.a로 무지개 강도 조절 가능
                float3 finalColor = rainbow * _FaceColor.rgb;
                float  finalAlpha = face * _FaceColor.a * _FaceOpacity * i.color.a;

                // Outline
                if (_OutlineWidth > 0.0)
                {
                    float outline = smoothstep(
                        0.5 - sd - edgeWidth,
                        0.5 - sd,
                        d
                    );
                    float outlineAlpha = outline * (1.0 - face);
                    finalColor = lerp(finalColor, _OutlineColor.rgb, outlineAlpha);
                    finalAlpha = max(finalAlpha, outlineAlpha * _OutlineColor.a);
                }

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }

    CustomEditor "TMPro.EditorUtilities.TMP_SDFShaderGUI"
}
