Shader "EDIVE/Grids/Infinite Sandbox Grid"
{
    Properties
    {
        // ---------- SMALL GRID ----------
        _SmallGridScale ("Small Grid Scale", Float) = 1
        _SmallWidth ("Small Thickness (m)", Float) = 0.02
        _SmallColor ("Small Color", Color) = (0.7,0.7,0.7,1)

        // ---------- BIG GRID ----------
        _BigGridScale ("Big Grid Scale", Float) = 0.1
        _BigWidth ("Big Thickness (m)", Float) = 0.15
        _BigColor ("Big Color", Color) = (1,1,1,1)

        // ---------- BACKGROUND ----------
        _BGColor ("Background", Color) = (0.05,0.05,0.05,0.25)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float _SmallGridScale;
                float _SmallWidth;
                float4 _SmallColor;

                float _BigGridScale;
                float _BigWidth;
                float4 _BigColor;

                float4 _BGColor;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPos = TransformObjectToWorld(v.positionOS.xyz);
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            #include "PristineGrid.hlsl"

            // fade when grid becomes subpixel
            float pixelFade(float2 uv)
            {
                float2 d = fwidth(uv);
                float density = max(d.x, d.y);
                return saturate(1.0 - density * 1.5);
            }

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float2 smallUV = i.worldPos.xz * _SmallGridScale;
                float2 bigUV   = i.worldPos.xz * _BigGridScale;

                // Thickness is half width.
                float small = PristineGrid(smallUV, 2.0 * _SmallWidth * _SmallGridScale);
                float big   = PristineGrid(bigUV,   2.0 * _BigWidth * _BigGridScale);

                // automatic fade by pixel density
                float smallFade = pixelFade(smallUV * 1.5); // fade earlier
                float bigFade   = pixelFade(bigUV);
                float bgFade    = bigFade;

                small *= smallFade;
                big   *= bigFade;

                float3 col = _BGColor.rgb;
                col = lerp(col, _SmallColor.rgb, small);
                col = lerp(col, _BigColor.rgb, big);

                float alpha =
                    max(_BGColor.a * bgFade,
                    max(small * _SmallColor.a,
                        big * _BigColor.a));

                return float4(col, alpha);
            }

            ENDHLSL
        }
    }
}
