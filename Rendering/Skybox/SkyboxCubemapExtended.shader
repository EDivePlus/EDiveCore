// Based on https://assetstore.unity.com/packages/vfx/shaders/free-skybox-extended-shader-107400
// Slightly modified and cleaned up

Shader "EDIVE/Skybox/Cubemap Extended"
{
    Properties
    {
        [Header(Cubemap Settings)][Space]
        [NoScaleOffset] _MainTex("Cubemap (HDR)", Cube) = "black" {}
        _Exposure("Exposure", Range(0, 8)) = 1
        [Gamma] _Tint("Tint Color", Color) = (0.5, 0.5, 0.5, 1)
        _MainTexVerticalOffset("Cubemap Vertical Offset", Float) = 0

        [Header(Blend Settings)][Space]
        [Toggle(_BLEND_ON)] _EnableBlend("Enable Blend", Float) = 0
        [NoScaleOffset] _BlendTex("Cubemap Blend (HDR)", Cube) = "black" {}
        _BlendFactor("Cubemap Transition", Range(0, 1)) = 0

        [Header(Rotation Settings)][Space]
        [Toggle(_ENABLEROTATION_ON)] _EnableRotation("Enable Rotation", Float) = 0
        _RotationDegrees("Rotation Degrees", Range(0, 360)) = 0
        _RotationSpeed("Rotation Speed", Float) = 1

        [Header(Fog Settings)][Space]
        [Toggle(_ENABLEFOG_ON)] _EnableFog("Enable Fog", Float) = 0
        _FogIntensity("Fog Intensity", Range(0, 1)) = 1
        _FogHeight("Fog Height", Range(0, 1)) = 1
        _FogSmoothness("Fog Smoothness", Range(0.01, 1)) = 0.01
        _FogFill("Fog Fill", Range(0, 1)) = 0.5
        _FogVerticalOffset("Fog Vertical Offset", Float) = 0

        [HideInInspector] _MainTex_HDR("DecodeInstructions", Vector) = (0, 0, 0, 0)
        [HideInInspector] _BlendTex_HDR("DecodeInstructions", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Background"
            "Queue" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            Name "Unlit"

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _BLEND_ON
            #pragma shader_feature_local _ENABLEROTATION_ON
            #pragma shader_feature_local_fragment _ENABLEFOG_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

            // unity_ColorSpaceDouble
            #ifdef UNITY_COLORSPACE_GAMMA
                #define COLOR_SPACE_DOUBLE half4(2.0, 2.0, 2.0, 2.0)
            #else
                #define COLOR_SPACE_DOUBLE half4(4.59479380, 4.59479380, 4.59479380, 2.0)
            #endif

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
                float heightOS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURECUBE(_MainTex);  SAMPLER(sampler_MainTex);
            TEXTURECUBE(_BlendTex); SAMPLER(sampler_BlendTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _MainTex_HDR;
                half4 _BlendTex_HDR;
                half4 _Tint;
                half _Exposure;
                float _MainTexVerticalOffset;
                half _BlendFactor;
                float _RotationDegrees;
                float _RotationSpeed;
                half _FogIntensity;
                half _FogHeight;
                half _FogSmoothness;
                half _FogFill;
                float _FogVerticalOffset;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Aspect correction for ortho
                float orthoAspect = lerp(1.0, unity_OrthoParams.y / unity_OrthoParams.x, unity_OrthoParams.w);
                float3 direction = float3(input.positionOS.x, input.positionOS.y * orthoAspect, input.positionOS.z);

            #ifdef _ENABLEROTATION_ON
                float s, c;
                sincos(radians(_RotationDegrees + _Time.y * _RotationSpeed), s, c);
                direction.xz = float2(direction.x * c + direction.z * s, direction.z * c - direction.x * s);
            #endif

                direction.y -= _MainTexVerticalOffset;

                output.direction = direction;
                output.heightOS = input.positionOS.y;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 color = DecodeHDREnvironment(SAMPLE_TEXTURECUBE(_MainTex, sampler_MainTex, input.direction), _MainTex_HDR);
            #ifdef _BLEND_ON
                half3 blend = DecodeHDREnvironment(SAMPLE_TEXTURECUBE(_BlendTex, sampler_BlendTex, input.direction), _BlendTex_HDR);
                color = lerp(color, blend, _BlendFactor);
            #endif
                half4 sky = half4(color, 0.0) * COLOR_SPACE_DOUBLE * _Tint * _Exposure;

            #ifdef _ENABLEFOG_ON
                // Height-based fog mask
                float height = input.heightOS - _FogVerticalOffset;
                float t = saturate(pow(abs(height) / max(_FogHeight, 1e-6), 1.0 - _FogSmoothness));
                float fogMask = lerp(1.0, lerp(t, 0.0, _FogFill), _FogIntensity);
                return lerp(unity_FogColor, sky, fogMask);
            #else
                return sky;
            #endif
            }
            ENDHLSL
        }
    }

    Fallback "Skybox/Cubemap"
}
