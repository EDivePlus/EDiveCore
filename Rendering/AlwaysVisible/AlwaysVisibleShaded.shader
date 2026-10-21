// Drawn on top of everything with correct self occlusion.
// First pass pushes depth under the silhouette to the far plane, second pass draws with normal depth test.
// Shading ignores scene lights: fixed key direction combined with view facing.
// Vertex color red blends from outline color (0) to base color (1).
Shader "EDIVE/Always Visible Shaded"
{
    Properties
    {
        [MainColor] _BaseColor ("Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0.58,0.58,0.6,1)
        _KeyDirection ("Key Direction (World)", Vector) = (0.45, 0.82, 0.35, 0)
        _ShadeMin ("Shade Min", Range(0, 1)) = 0.4
        _ShadeInputRange ("Shade Input Range", Vector) = (0.3, 0.85, 0, 0)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Overlay"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _OutlineColor;
            float4 _KeyDirection;
            float _ShadeMin;
            float4 _ShadeInputRange;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ClearDepth"
            Tags { "LightMode"="SRPDefaultUnlit" }

            ZWrite On
            ZTest Always
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionHCS.z = lerp(1e-5, 1 - 1e-5, UNITY_RAW_FAR_CLIP_VALUE) * OUT.positionHCS.w;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Shaded"
            Tags { "LightMode"="UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                float key = 0.5 + 0.5 * dot(normalWS, normalize(_KeyDirection.xyz));
                float facing = saturate(dot(normalWS, viewDirWS));
                float t = saturate((key * facing - _ShadeInputRange.x) / max(_ShadeInputRange.y - _ShadeInputRange.x, 1e-4));
                half4 color = lerp(_OutlineColor, _BaseColor, IN.color.r);
                return half4(color.rgb * lerp(_ShadeMin, 1, t), color.a);
            }
            ENDHLSL
        }
    }
}
