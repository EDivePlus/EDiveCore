Shader "EDIVE/Grids/Pristine Grid Additive"
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
            "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline"
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
            #pragma shader_feature_local _UVMODE_MESHUV _UVMODE_WORLDX _UVMODE_WORLDY _UVMODE_WORLDZ
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float _GridScale;
                float _LineWidthX;
                float _LineWidthY;
                half4 _Color;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

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

            #include "PristineGrid.hlsl"

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float grid = PristineGrid(i.uv, float2(_LineWidthX, _LineWidthY));
                half3 color = _Color.rgb * grid * _Color.a;
                return half4(color, 0); // alpha = 0, additive
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
}