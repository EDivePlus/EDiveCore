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
            #pragma target 3.5

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "ProceduralShape.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #define LAYER_FILL 0
            #define LAYER_OUTER_SHADOW 1
            #define LAYER_INNER_SHADOW 2
            #define LAYER_OUTLINE 3
            #define LAYER_SOLID_FILL 4

            // One quad per layer, packed by SDFGraphic.OnPopulateMesh. The canvas rotates and scales normals and tangents
            // with the RectTransform, so everything lives in the uv channels.
            //   uv0, uv1: grid position, size, roundness and arc, see DecodeGeometry
            //   uv2.x: layer, corner shape, frame and sharp arc apex, see DecodeLayer
            //   uv2.yzw by layer:
            //     fill:    encoded fill with texture flag, gradient rgb, gradient alpha + inset * 256 in 1/16 px
            //     shadow:  round(size) + round(blur) * 4096, offset, power + round(start) * 256; start is past the outlines
            //     outline: size + placement * 4096, overlap under the fill
            //   color: Graphic.color for the fill, the effect color for effects (alpha times Graphic.color alpha when they use it)

            struct appdata
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float4 uv0      : TEXCOORD0;
                float4 uv1      : TEXCOORD1;
                float4 uv2      : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Every value but the position and color is the same over the quad, so it is flat.
            // Pixels only load what their path reads, so keep the common path to few values.
            struct v2f
            {
                float4 vertex                   : SV_POSITION;
                fixed4 color                    : COLOR;
                float2 sdfPos                   : TEXCOORD0; // px from the rect center
                nointerpolation float4 roundness : TEXCOORD1;
                nointerpolation float4 shape    : TEXCOORD2; // xy = half size, z = layer, w = corner style + hasArc * 2 + hasTexture * 4
                nointerpolation float4 ring     : TEXCOORD3; // x = ring center, y = ring half width (< 0 without frame), z = corner join, w = arc corner radius
                nointerpolation float4 sectorA  : TEXCOORD4; // see PrepareSector
                nointerpolation float4 sectorB  : TEXCOORD5;
                nointerpolation float4 params   : TEXCOORD6; // fill: mode, radial size, -, inset; shadow: size, blur, power, start; outline: outer, inner reach
                nointerpolation float4 params2  : TEXCOORD7; // fill: gradient color; shadow: offset
                #ifdef UNITY_UI_CLIP_RECT
                float2 clipPos                  : TEXCOORD8;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            int _UIVertexColorAlwaysGammaSpace;

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

                float layer, sharpApex, cornerShape, framePlacement, frameWidth;
                bool frameMode;
                DecodeLayer(v.uv2.x, layer, sharpApex, cornerShape, frameMode, framePlacement, frameWidth);
                float cornerStyle, cornerJoin;
                DecodeCornerShape(cornerShape, cornerStyle, cornerJoin);
                float frameOuter = frameMode ? PlacementOuterExtent(framePlacement, frameWidth) : 0.0;
                float frameInner = frameOuter - frameWidth;

                float4 params = 0;
                float4 params2 = 0;
                float hasTexture = 0;
                float reach = 0;
                if (layer < 0.5 || layer > 3.5)
                {
                    DecodeFill(v.uv2.y, params.x, params.y, hasTexture);
                    float insetCode = floor(v.uv2.w / 256.0);
                    params.w = insetCode / 16.0;
                    params2 = UnpackColor(float2(v.uv2.z, v.uv2.w - insetCode * 256.0));
                    params2.rgb *= _Color.rgb;
                }
                else if (layer < 2.5)
                {
                    float shadowSize, shadowBlur;
                    DecodeShadow(v.uv2.y, shadowSize, shadowBlur);
                    float2 shadowOffset = DecodeOffsets(v.uv2.z);
                    float start = floor(v.uv2.w / 256.0);
                    params = float4(shadowSize, shadowBlur, max(v.uv2.w - start * 256.0, 0.01), start);
                    params2 = float4(shadowOffset, 0, 0);
                    if (layer < 1.5)
                        reach = start + shadowSize + shadowBlur + max(abs(shadowOffset.x), abs(shadowOffset.y));
                }
                else
                {
                    float placement = floor(v.uv2.y / 4096.0);
                    float outlineSize = v.uv2.y - placement * 4096.0;
                    float outer = PlacementOuterExtent(placement, outlineSize);
                    params = float4(outer, outlineSize - outer + v.uv2.z, 0, 0);
                    reach = outer;
                }

                float hasArc = ArcIsFull(arc) ? 0.0 : 1.0;
                PrepareSector(arc, sharpApex > 0.5 ? 0.0 : cornerRadius, OUT.sectorA, OUT.sectorB);

                // The quad reaches past the rect by the same margin SDFGraphic adds
                float padding = frameOuter + reach + 1.0;
                float2 normPad = padding / size;
                float2 uv = gridUv * (1 + normPad * 2) - normPad;

                OUT.sdfPos = (uv - 0.5) * size;
                OUT.roundness = min(roundness, min(size.x, size.y) * 0.5);
                OUT.shape = float4(size * 0.5, layer, cornerStyle + hasArc * 2.0 + hasTexture * 4.0);
                OUT.ring = float4((frameOuter + frameInner) * 0.5, frameMode ? frameWidth * 0.5 : -1.0, cornerJoin, cornerRadius);
                OUT.params = params;
                OUT.params2 = params2;
                #ifdef UNITY_UI_CLIP_RECT
                OUT.clipPos = v.vertex.xy;
                #endif

                return OUT;
            }

            // Shape with the ring transform in frame mode and the arc cut. Effects follow the ring, so they wrap both its edges.
            float LayerDistance(float2 p, v2f IN, float cornerStyle, float cornerJoin, float hasArc, float edgeCos)
            {
                float dist = ShapeDistance(p, IN.shape.xy, IN.roundness, cornerStyle, cornerJoin);
                float shape = IN.ring.y >= 0 ? abs(dist - IN.ring.x) - IN.ring.y : dist;
                [branch]
                if (hasArc > 0.5)
                    shape = JoinedMax(shape, SectorDistance(p, IN.sectorA, IN.sectorB), IN.ring.w, edgeCos, cornerJoin);
                return shape;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float layer = IN.shape.z;
                float flags = IN.shape.w;
                float hasTexture = step(3.5, flags);
                flags -= hasTexture * 4.0;
                float hasArc = step(1.5, flags);
                float cornerStyle = flags - hasArc * 2.0;
                float cornerJoin = IN.ring.z;

                float2 p = IN.sdfPos;

                // The solid middle of the fill is always covered and skips the distance
                float sdf = -1.0;
                float edgeCos = 1;
                [branch]
                if (layer < 3.5)
                {
                    float dist = ShapeDistance(p, IN.shape.xy, IN.roundness, cornerStyle, cornerJoin);
                    float shape = IN.ring.y >= 0 ? abs(dist - IN.ring.x) - IN.ring.y : dist;
                    sdf = shape;

                    // The arc is the same over the whole quad, so shapes without one skip the sector and its derivatives
                    [branch]
                    if (hasArc > 0.5)
                    {
                        float sector = SectorDistance(p, IN.sectorA, IN.sectorB);
                        edgeCos = EdgeCosine(shape, sector, p);
                        sdf = JoinedMax(shape, sector, IN.ring.w, edgeCos, cornerJoin);
                    }
                }

                float delta = fwidth(sdf);

                half3 rgb;
                float alpha;
                [branch]
                if (layer < 0.5 || layer > 3.5)
                {
                    // The inset pulls the edge in under an outline, so its antialiasing stays hidden
                    rgb = IN.color.rgb;
                    alpha = 1.0;
                    float2 uv = p / (IN.shape.xy * 2.0) + 0.5;

                    // Gradient between the vertex fill color and the packed gradient color
                    [branch]
                    if (IN.params.x > 0.5)
                    {
                        float gradient = GradientFactor(IN.params.x, uv, IN.shape.xy * 2.0, IN.params.y);
                        rgb = lerp(rgb, IN.params2.rgb, gradient);
                        alpha = lerp(alpha, IN.params2.a, gradient);
                    }

                    [branch]
                    if (hasTexture > 0.5)
                    {
                        half4 graphic = tex2D(_MainTex, uv) + _TextureSampleAdd;
                        rgb *= graphic.rgb;
                        alpha *= graphic.a;
                    }

                    alpha *= layer > 3.5 ? 1.0 : 1 - smoothstep(0, delta, sdf + IN.params.w);
                }
                else if (layer < 2.5)
                {
                    float shadowSize = IN.params.x;
                    float shadowBlur = IN.params.y;
                    float start = IN.params.w;

                    // The shadow samples the same fields shifted by its offset, with the corner angle of the unshifted pixel
                    float2 shadowOffset = IN.params2.xy;
                    float shadowSdf = sdf;
                    [branch]
                    if (dot(shadowOffset, shadowOffset) > 0)
                        shadowSdf = LayerDistance(p - shadowOffset, IN, cornerStyle, cornerJoin, hasArc, edgeCos);

                    rgb = IN.color.rgb;
                    if (layer < 1.5)
                    {
                        float edge = start + shadowSize;
                        alpha = pow(1 - smoothstep(edge - shadowBlur, edge + delta, shadowSdf), IN.params.z);
                    }
                    else
                    {
                        // Inner shadow stays inside the shape
                        float edge = -(start + shadowSize);
                        alpha = pow(smoothstep(edge - delta, edge + shadowBlur, shadowSdf), IN.params.z) * (1 - smoothstep(0, delta, sdf));
                    }
                }
                else
                {
                    // Band from the inner reach inside the edge out to the outer reach
                    float outer = IN.params.x;
                    float inner = IN.params.y;
                    rgb = IN.color.rgb;
                    alpha = smoothstep(-inner - delta, -inner, sdf) * (1 - smoothstep(outer, outer + delta, sdf));
                }

                // vertex.color.a is the master alpha: Graphic.color and CanvasGroup
                alpha *= IN.color.a;
                half4 result = half4(rgb * alpha, alpha);

                #ifdef UNITY_UI_CLIP_RECT
                result *= UnityGet2DClipping(IN.clipPos, _ClipRect);
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
