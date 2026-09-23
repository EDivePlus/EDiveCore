// Ported from PurrUI (MIT), Copyright 2025 Pebbles Games Consultancy Corporation, Riten SARL
Shader "Hidden/EDIVE/ProceduralUI/SimpleSDF"
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

            // Vertex attributes packed by OnPopulateMesh. The canvas rotates and scales normals and tangent.xyz
            // with the RectTransform, so everything lives in the uv channels and tangent.w.
            //   uv0, uv1: grid position, size, roundness and arc, see DecodeGeometry
            //   uv2: encodedOutline, encodedShadow, shadowOffset, encodedShadowPow
            //        encodedOutline: outlineSize + outlinePlacement*4096 + framePlacement*16384 + cornerShape*65536; < 0 → frame mode, abs = 1 + that
            //        encodedShadow: round(shadowSize) + round(shadowBlur)*4096, negated and offset by 1 when inset
            //        shadowOffset: x and y as 12-bit fixed point, (px + 512) * 4
            //        encodedShadowPow: shadowPow + round(frameWidth)*256
            //   uv3: outline, shadow and gradient color, three bytes per float
            //   tangent.w: encoded fill: radialSize*100 + mode*4096 + fillAlpha*32768 + sharpApex*8388608
            //   color: fill color * Graphic.color with the tint alpha alone; fill alpha is in the encoded fill

            struct appdata
            {
                float4 vertex   : POSITION;
                float4 tangent  : TANGENT;
                float4 color    : COLOR;
                float4 uv0      : TEXCOORD0;
                float4 uv1      : TEXCOORD1;
                float4 uv2      : TEXCOORD2;
                float4 uv3      : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex       : SV_POSITION;
                fixed4 color        : COLOR;
                float4 texAndSdf    : TEXCOORD0; // xy=texcoord, zw=sdfPosition
                float4 roundness    : TEXCOORD1;
                float4 params       : TEXCOORD2; // xy=halfSize, z=rawOutline, w=rawShadow
                float4 params2      : TEXCOORD3; // x=unused, y=rawShadowPow, zw=worldPos
                half4 outlineColor  : TEXCOORD4;
                half4 shadowColor   : TEXCOORD5;
                float4 arc          : TEXCOORD6; // xy=apex, z=startAngle, w=endAngle
                float4 arcExtra     : TEXCOORD7; // x=cornerRadius, y=sharpApex, zw=shadowOffset
                float3 fill         : TEXCOORD8; // x=mode, y=radialSize, z=fillAlpha
                half4 gradientColor : TEXCOORD9;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            int _UIVertexColorAlwaysGammaSpace;

            // Placement: 0 = Inside, 1 = Center, 2 = Outside. Returns how far a band of the given width reaches past the edge.
            float PlacementOuterExtent(float placement, float width)
            {
                return placement < 0.5 ? 0.0 : placement < 1.5 ? width * 0.5 : width;
            }

            void DecodeOutline(float raw, out bool frameMode, out float outlineSize, out float outlinePlacement, out float framePlacement, out float cornerShape)
            {
                frameMode = raw < -0.5;
                float info = frameMode ? abs(raw) - 1.0 : raw;
                cornerShape = floor(info / 65536.0 + 0.001);
                info -= cornerShape * 65536.0;
                framePlacement = floor(info / 16384.0 + 0.001);
                info -= framePlacement * 16384.0;
                outlinePlacement = floor(info / 4096.0 + 0.001);
                outlineSize = info - outlinePlacement * 4096.0;
            }

            // Shape with the ring transform in frame mode, before the arc cut
            float RingDistance(float2 p, float2 halfSize, float4 roundness, float cornerStyle, float cornerJoin,
                bool frameMode, float ringCenter, float ringHalf)
            {
                float dist = ShapeDistance(p, halfSize, roundness, cornerStyle, cornerJoin);
                return frameMode ? abs(dist - ringCenter) - ringHalf : dist;
            }

            v2f vert(appdata v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex = UnityObjectToClipPos(v.vertex);
                if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
                    v.color.rgb = UIGammaToLinear(v.color.rgb);
                OUT.color = v.color * _Color;

                float2 gridUv, size;
                float4 roundness, arc;
                float cornerRadius;
                DecodeGeometry(v.uv0, v.uv1, gridUv, size, roundness, arc, cornerRadius);

                // UV transform for padding
                bool frameMode;
                float outlineSize, outlinePlacement, framePlacement, cornerShape;
                DecodeOutline(v.uv2.x, frameMode, outlineSize, outlinePlacement, framePlacement, cornerShape);
                float outlineExtent = PlacementOuterExtent(outlinePlacement, outlineSize);
                if (frameMode)
                {
                    float fw = floor(v.uv2.w / 256.0 + 0.001);
                    outlineExtent += PlacementOuterExtent(framePlacement, fw);
                }
                float shadowSize, shadowBlur;
                bool shadowInset;
                DecodeShadow(v.uv2.y, shadowSize, shadowBlur, shadowInset);
                float2 shadowOffset = DecodeOffsets(v.uv2.z);
                float offsetExtent = max(abs(shadowOffset.x), abs(shadowOffset.y));
                float shadowExtent = shadowInset ? 0 : shadowSize + shadowBlur + offsetExtent; // inset shadows stay within the shape
                float padding = outlineExtent + shadowExtent + 1.0; // outline+frame + shadow + blur + offset + 1px
                float2 normPad = padding / size;
                float2 uv = gridUv * (1 + normPad * 2) - normPad;

                OUT.texAndSdf = float4(uv, (uv - 0.5) * size);
                OUT.roundness = roundness;
                OUT.params = float4(size * 0.5, v.uv2.x, v.uv2.y);
                OUT.params2 = float4(0, v.uv2.w, v.vertex.xy);

                // Unpack colors in vertex shader (4 verts) instead of fragment (thousands)
                half4 gradientColor;
                UnpackColors(v.uv3, OUT.outlineColor, OUT.shadowColor, gradientColor);
                gradientColor.rgb *= _Color.rgb;
                OUT.gradientColor = gradientColor;
                float sharpApex;
                DecodeFill(v.tangent.w, OUT.fill.x, OUT.fill.y, OUT.fill.z, sharpApex);
                OUT.arc = arc;
                OUT.arcExtra = float4(cornerRadius, sharpApex, shadowOffset);

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 halfSize = IN.params.xy;
                float rawOutline = IN.params.z;
                float shadowSize, shadowBlur;
                bool shadowInset;
                DecodeShadow(IN.params.w, shadowSize, shadowBlur, shadowInset);

                bool frameMode;
                float outlineSize, outlinePlacement, framePlacement, cornerShape;
                DecodeOutline(rawOutline, frameMode, outlineSize, outlinePlacement, framePlacement, cornerShape);
                float cornerStyle, cornerJoin;
                DecodeCornerShape(cornerShape, cornerStyle, cornerJoin);

                float outerEdge = 0;
                float innerEdge = 0;
                float shadowPow = IN.params2.y;
                if (frameMode)
                {
                    float fw = floor(IN.params2.y / 256.0 + 0.001);
                    shadowPow = IN.params2.y - fw * 256.0;
                    outerEdge = PlacementOuterExtent(framePlacement, fw);
                    innerEdge = outerEdge - fw;
                }

                // Outline band reaches outlineOuter past the edge and outlineInner into the shape
                float outlineOuter = PlacementOuterExtent(outlinePlacement, outlineSize);
                float outlineInner = outlineSize - outlineOuter;

                half4 graphic = tex2D(_MainTex, IN.texAndSdf.xy) + _TextureSampleAdd;

                // Ring in frame mode: negative inside the wall, positive in the hole and outside, so effects wrap both edges
                float ringCenter = frameMode ? (outerEdge + innerEdge) * 0.5 : 0;
                float ringHalf = frameMode ? (outerEdge - innerEdge) * 0.5 : 0;
                float2 p = IN.texAndSdf.zw;
                float shape = RingDistance(p, halfSize, IN.roundness, cornerStyle, cornerJoin, frameMode, ringCenter, ringHalf);
                float sector = ArcSector(p, IN.arc, IN.arcExtra.x, IN.arcExtra.y);
                float edgeCos = EdgeCosine(shape, sector, p);
                float sdf = ApplyArc(shape, sector, IN.arc, IN.arcExtra.x, cornerJoin, edgeCos);

                // The shadow samples the same fields shifted by its offset, with the corner angle of the unshifted pixel
                float2 shadowOffset = IN.arcExtra.zw;
                bool hasOffset = dot(shadowOffset, shadowOffset) > 0;
                float shadowSdf = sdf;
                if (hasOffset)
                {
                    float2 sp = p - shadowOffset;
                    float shadowShape = RingDistance(sp, halfSize, IN.roundness, cornerStyle, cornerJoin, frameMode, ringCenter, ringHalf);
                    float shadowSector = ArcSector(sp, IN.arc, IN.arcExtra.x, IN.arcExtra.y);
                    shadowSdf = ApplyArc(shadowShape, shadowSector, IN.arc, IN.arcExtra.x, cornerJoin, edgeCos);
                }

                float delta = fwidth(sdf);

                // One coverage term antialiases the outer edge of fill and outline together,
                // so no background leaks between two separately faded layers
                float coverage = 1 - smoothstep(outlineOuter, outlineOuter + delta, sdf);

                // Fill only needs its own edge when the outline reaches past it
                float fill = outlineOuter > 0 ? 1 - smoothstep(0, delta, sdf) : 1;

                // Outline: band from outlineInner inside the edge outward, clipped by coverage
                float outline = outlineSize > 0
                    ? smoothstep(-outlineInner - delta, -outlineInner, sdf)
                    : 0;

                // Shadow: beyond the outline, or inset from its inner edge and clipped to the fill
                float shadow = 0;
                float shadowRange = shadowSize + shadowBlur;
                if (shadowRange > 0 || hasOffset)
                {
                    if (shadowInset)
                    {
                        float shadowEdge = -outlineInner - shadowSize;
                        shadow = smoothstep(
                            shadowEdge - delta,
                            shadowEdge + shadowBlur,
                            shadowSdf);
                    }
                    else
                    {
                        float shadowEdge = outlineOuter + shadowSize;
                        shadow = 1 - smoothstep(
                            shadowEdge - shadowBlur,
                            shadowEdge + delta,
                            shadowSdf);
                    }
                    shadow = pow(shadow, shadowPow);
                }

                // Gradient between the vertex fill color and the packed gradient color
                float gradient = GradientFactor(IN.fill.x, IN.texAndSdf.xy, halfSize * 2.0, IN.fill.y);
                half3 fillRgb = graphic.rgb * lerp(IN.color.rgb, IN.gradientColor.rgb, gradient);
                float fillAlpha = lerp(IN.fill.z, IN.gradientColor.a, gradient);

                // vertex.color.a is the master alpha: Graphic.color and CanvasGroup, without the fill alpha
                float masterAlpha = IN.color.a;
                float gA = fill * graphic.a * fillAlpha * masterAlpha;
                float oA = outline * IN.outlineColor.a * masterAlpha;
                float sA = shadow * IN.shadowColor.a * masterAlpha;
                float innerSA = shadowInset ? sA * fill : 0;
                float outerSA = shadowInset ? 0 : sA;

                // Inset shadow over fill, outline over that, clipped by coverage, then over the outer shadow, all premultiplied
                float oneMinusOA = 1 - oA;
                half3 innerRgb = IN.shadowColor.rgb * innerSA + fillRgb * gA * (1 - innerSA);
                float innerA = innerSA + gA * (1 - innerSA);
                half3 shapeRgb = (IN.outlineColor.rgb * oA + innerRgb * oneMinusOA) * coverage;
                float shapeA = (oA + innerA * oneMinusOA) * coverage;

                half4 result;
                result.rgb = shapeRgb + IN.shadowColor.rgb * outerSA * (1 - shapeA);
                result.a = shapeA + outerSA * (1 - shapeA);

                #ifdef UNITY_UI_CLIP_RECT
                result *= UnityGet2DClipping(IN.params2.zw, _ClipRect);
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
