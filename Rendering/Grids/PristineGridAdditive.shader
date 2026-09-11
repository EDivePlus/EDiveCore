Shader "URP/PristineGridAdditive"
{
    Properties
    {
        [KeywordEnum(MeshUV, WorldX, WorldY, WorldZ)] _UVMode ("UV Mode", Float) = 2
        _GridScale ("Grid Scale", Float) = 1.0
        _LineWidthX ("Line Width X", Range(0,1.0)) = 0.01
        _LineWidthY ("Line Width Y", Range(0,1.0)) = 0.01
        _Color ("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent" "Queue" = "Transparent"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend One One // Additive blending
            ZWrite Off // No depth writes for transparent object
            Cull Off // Optional: render both sides

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _UVMODE_MESHUV _UVMODE_WORLDX _UVMODE_WORLDZ

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _GridScale;
            float _LineWidthX;
            float _LineWidthY;
            half4 _Color;

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);

                #if defined(_UVMODE_MESHUV)
                output.uv = input.uv * _GridScale;
                #else
                float3 cameraOffset = floor(_WorldSpaceCameraPos * _GridScale);
                float3 snappedWorldPos = worldPos * _GridScale - cameraOffset;

                #if defined(_UVMODE_WORLDX)
                output.uv = snappedWorldPos.xy;
                #elif defined(_UVMODE_WORLDZ)
                output.uv = snappedWorldPos.yz;
                #else
                output.uv = snappedWorldPos.xz;
                #endif
                #endif

                return output;
            }

            float PristineGrid(float2 uv, float2 lineWidth)
            {
                lineWidth = saturate(lineWidth);
                float4 uvDDXY = float4(ddx(uv), ddy(uv));
                float2 uvDeriv = float2(length(uvDDXY.xz), length(uvDDXY.yw));
                bool2 invertLine = lineWidth > 0.5;
                float2 targetWidth = invertLine ? 1.0 - lineWidth : lineWidth;
                float2 drawWidth = clamp(targetWidth, uvDeriv, 0.5);
                float2 lineAA = max(uvDeriv, 0.000001) * 1.5;
                float2 gridUV = abs(frac(uv) * 2.0 - 1.0);
                gridUV = invertLine ? gridUV : 1.0 - gridUV;
                float2 grid2 = smoothstep(drawWidth + lineAA, drawWidth - lineAA, gridUV);
                grid2 *= saturate(targetWidth / drawWidth);
                grid2 = lerp(grid2, targetWidth, saturate(uvDeriv * 2.0 - 1.0));
                grid2 = invertLine ? 1.0 - grid2 : grid2;
                return lerp(grid2.x, 1.0, grid2.y);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float grid = PristineGrid(i.uv, float2(_LineWidthX, _LineWidthY));
                half3 color = _Color.rgb * grid * _Color.a;
                return half4(color, 0); // alpha = 0, additive
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
}