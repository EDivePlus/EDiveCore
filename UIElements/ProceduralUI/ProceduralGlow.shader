// Ported from PurrUI (MIT), Copyright 2025 Pebbles Games Consultancy Corporation, Riten SARL
Shader "Hidden/EDIVE/ProceduralUI/Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "ProceduralShape.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            // Vertex attributes packed by GlowGraphic.OnPopulateMesh:
            //   uv0: texU, texV, width, height
            //   uv1: roundness (x, y, z, w)
            //   uv2: spread, blur, power, encodedFrame
            //        encodedFrame: cornerShape*65536 + frame; frame = 0 for a solid shape, else 1 + placement*4096 + round(frameWidth)
            //   tangent: arc apex in sdf space (xy), arc start and end angle in radians (zw); a full sweep disables the arc
            //   uv3: gradient color (x = rgb, y = a)
            //   normal: arc corner radius (x), free (y), encoded fill (z): radialSize*100 + mode*4096 + fillAlpha*32768
            //   color: glow color * Graphic.color with the tint alpha alone; fill alpha is in the encoded fill

            struct appdata
            {
                float4 vertex  : POSITION;
                float3 normal  : NORMAL;
                float4 tangent : TANGENT;
                float4 color   : COLOR;
                float4 uv0     : TEXCOORD0;
                float4 uv1     : TEXCOORD1;
                float4 uv2     : TEXCOORD2;
                float4 uv3     : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex     : SV_POSITION;
                fixed4 color      : COLOR;
                float2 sdfPos     : TEXCOORD0;
                float4 roundness  : TEXCOORD1;
                float4 params     : TEXCOORD2; // xy=halfSize, z=spread, w=blur
                float2 params2    : TEXCOORD3; // x=power, y=encodedFrame
                float2 worldPos   : TEXCOORD4;
                float4 arc        : TEXCOORD5; // xy=apex, z=startAngle, w=endAngle
                float2 arcExtra   : TEXCOORD6; // x=cornerRadius, y=sharpApex
                float2 uv         : TEXCOORD7;
                float3 fill       : TEXCOORD8; // x=mode, y=radialSize, z=fillAlpha
                half4 gradientColor : TEXCOORD9;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float4 _ClipRect;

            void DecodeFrame(float raw, out float cornerShape, out bool frameMode, out float placement, out float frameWidth)
            {
                cornerShape = floor(raw / 65536.0 + 0.001);
                float frame = raw - cornerShape * 65536.0;
                frameMode = frame > 0.5;
                float info = max(frame - 1.0, 0.0);
                placement = floor(info / 4096.0 + 0.001);
                frameWidth = info - placement * 4096.0;
            }

            v2f vert(appdata v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.color = v.color * _Color;

                // UV transform for padding
                float2 size = v.uv0.zw;
                float spread = v.uv2.x;
                float blur = v.uv2.y;
                float frameVal = v.uv2.w;
                float cornerShape, placement, fw;
                bool frameMode;
                DecodeFrame(frameVal, cornerShape, frameMode, placement, fw);
                float frameOutward = 0;
                if (frameMode)
                {
                    frameOutward = placement < 0.5 ? 0.0
                                 : placement < 1.5 ? fw * 0.5
                                 : fw;
                }
                float padding = spread + blur + frameOutward + 1.0;
                float2 normPad = padding / size;
                float2 uv = v.uv0.xy * (1 + normPad * 2) - normPad;

                OUT.sdfPos = (uv - 0.5) * size;
                OUT.roundness = v.uv1;
                OUT.params = float4(size * 0.5, spread, blur);
                OUT.params2 = float2(v.uv2.z, frameVal);
                OUT.worldPos = v.vertex.xy;
                float sharpApex;
                DecodeFill(v.normal.z, OUT.fill.x, OUT.fill.y, OUT.fill.z, sharpApex);
                OUT.arc = v.tangent;
                OUT.arcExtra = float2(v.normal.x, sharpApex);
                OUT.uv = uv;
                half4 gradientColor = UnpackColor(v.uv3.xy);
                gradientColor.rgb *= _Color.rgb;
                OUT.gradientColor = gradientColor;

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 halfSize = IN.params.xy;
                float spread = IN.params.z;
                float blur = max(IN.params.w, 0.001);
                float power = IN.params2.x;
                float frameVal = IN.params2.y;

                float cornerShape, placement, fw;
                bool frameMode;
                DecodeFrame(frameVal, cornerShape, frameMode, placement, fw);
                float cornerStyle, cornerJoin;
                DecodeCornerShape(cornerShape, cornerStyle, cornerJoin);

                float dist = ShapeDistance(IN.sdfPos, halfSize, IN.roundness, cornerStyle, cornerJoin);

                // In frame mode, derive ring SDF so glow wraps both ring edges
                float sdf;
                if (frameMode)
                {
                    float outerEdge, innerEdge;
                    if (placement < 0.5)       { outerEdge = 0.0;      innerEdge = -fw; }
                    else if (placement < 1.5)  { outerEdge = fw * 0.5; innerEdge = -fw * 0.5; }
                    else                       { outerEdge = fw;       innerEdge = 0.0; }

                    float ringCenter = (outerEdge + innerEdge) * 0.5;
                    float ringHalf = (outerEdge - innerEdge) * 0.5;
                    sdf = abs(dist - ringCenter) - ringHalf;
                }
                else
                {
                    sdf = dist;
                }

                // Arc cut after the ring so frames become open segments
                sdf = ApplyArc(sdf, IN.sdfPos, IN.arc, IN.arcExtra.x, IN.arcExtra.y);

                // Glow: smooth falloff centered around 'spread' distance from shape/ring edge
                float glow = 1.0 - smoothstep(spread - blur, spread + blur, sdf);
                glow = pow(glow, power);

                // Gradient between the vertex color and the packed gradient color
                float gradient = GradientFactor(IN.fill.x, IN.uv, halfSize * 2.0, IN.fill.y);
                half3 rgb = lerp(IN.color.rgb, IN.gradientColor.rgb, gradient);
                float fillAlpha = lerp(IN.fill.z, IN.gradientColor.a, gradient);

                // Premultiplied alpha output
                float alpha = IN.color.a * fillAlpha * glow;
                half4 result;
                result.rgb = rgb * alpha;
                result.a = alpha;

                #ifdef UNITY_UI_CLIP_RECT
                result *= UnityGet2DClipping(IN.worldPos, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif

                return result;
            }
            ENDCG
        }
    }
}
