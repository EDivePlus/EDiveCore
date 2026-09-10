Shader "EDIVE/Mirror Lit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor]   _BaseColor("Base Color", Color) = (1,1,1,1)
        _AlbedoSpeed("Base Map Scroll", Vector) = (0,0,0,0)

        [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Scale", Float) = 1
        _NormalSpeed("Normal Map Scroll", Vector) = (0,0,0,0)

        [NoScaleOffset] _MaskMap("Mask Map", 2D) = "white" {}

        [NoScaleOffset] _MirrorTexLeft("Reflection Left", 2D) = "black" {}
        [NoScaleOffset] _MirrorTexRight("Reflection Right", 2D) = "black" {}
        _ReflectionTint("Reflection Tint", Color) = (1,1,1,1)
        _Reflectivity("Reflectivity", Range(0,1)) = 1
        _FresnelPower("Fresnel Power", Range(1,8)) = 5
        _Blur("Blur", Range(0,4)) = 0
        _Refraction("Refraction", Range(0,0.2)) = 0
        _FallbackColor("Fallback Color", Color) = (1,1,1,1)
        _Metallic("Fallback Metallic", Range(0,1)) = 1
        _Smoothness("Fallback Smoothness", Range(0,1)) = 1

        [HideInInspector] _MirrorBlend("Distance Blend", Range(0,1)) = 1
        _FallbackEnvColor("Fallback Environment Color", Color) = (0,0,0,0)
        _Alpha("Alpha", Range(0,1)) = 1
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5

        [HideInInspector] _ProbeFallback("__probeFallback", Float) = 0
        [HideInInspector] _BoxProjection("__boxProjection", Float) = 0
        [HideInInspector] _FallbackCubemap("Fallback Cubemap", Cube) = "" {}
        [HideInInspector] _FallbackCubemapHDR("__fallbackHDR", Vector) = (1,1,0,0)
        [HideInInspector] _FallbackProbePos("__fallbackProbePos", Vector) = (0,0,0,0)
        [HideInInspector] _FallbackBoxMin("__fallbackBoxMin", Vector) = (0,0,0,0)
        [HideInInspector] _FallbackBoxMax("__fallbackBoxMax", Vector) = (0,0,0,0)

        [HideInInspector] _MirrorEye("MirrorEye", Float) = -1
        [HideInInspector] _MirrorFlipY("MirrorFlipY", Float) = 0

        // Blending state, driven by MirrorShaderGUI
        [HideInInspector] _Surface("__surface", Float) = 0
        [HideInInspector] _Blend("__blend", Float) = 0
        [HideInInspector] _Cull("__cull", Float) = 2
        [HideInInspector][ToggleUI] _AlphaClip("__clip", Float) = 0
        [HideInInspector] _SrcBlend("__src", Float) = 1
        [HideInInspector] _DstBlend("__dst", Float) = 0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0
        [HideInInspector] _ZWrite("__zw", Float) = 1
        [HideInInspector] _ZWriteControl("__zwc", Float) = 0
        [HideInInspector] _ZTest("__zt", Float) = 4
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0
        [HideInInspector] _BlendModePreserveSpecular("__blendModePreserveSpecular", Float) = 1
        [HideInInspector][ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Unlit"
            "Queue" = "Geometry"
        }
        LOD 300

        Pass
        {
            Name "Universal Forward"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Cull [_Cull]
            AlphaToMask [_AlphaToMask]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex MirrorVertex
            #pragma fragment MirrorFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _MASKMAP
            #pragma shader_feature_local_fragment _BLUR_ON
            #pragma shader_feature_local_fragment _PROBE_FALLBACK
            #pragma shader_feature_local_fragment _BOXPROJECTION_ON
            #pragma shader_feature_local_fragment _PROBE_EXPLICIT

            // GlossyEnvironmentReflection needs these. Without them the atlas and blending paths compile out.
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ REFLECTION_PROBE_ROTATION
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _RECEIVE_SHADOWS_OFF

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
            #define MIRROR_USE_URP_PROBES
            #include "MirrorCore.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                float2 dynamicLightmapUV : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3  normalWS   : TEXCOORD2;
                half4  tangentWS  : TEXCOORD3;
                half   fogFactor  : TEXCOORD4;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);
#ifdef DYNAMICLIGHTMAP_ON
                float2 dynamicLightmapUV : TEXCOORD6;
#endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings MirrorVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = nrm.normalWS;
                output.tangentWS = half4(nrm.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(pos.positionCS.z);

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.staticLightmapUV);
                OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
#ifdef DYNAMICLIGHTMAP_ON
                output.dynamicLightmapUV = input.dynamicLightmapUV * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
                return output;
            }

            // Diffuse only. The reflection gives all the specular.
            // A BRDF lobe here would double every light.
            half3 MirrorDiffuseLighting(Varyings input, half3 normalWS, float2 screenUV)
            {
                half4 shadowMask = half4(1, 1, 1, 1);
#if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
                shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
#elif !defined(LIGHTMAP_ON)
                shadowMask = unity_ProbesOcclusion;
#endif

#ifdef DYNAMICLIGHTMAP_ON
                half3 lighting = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, normalWS);
#else
                half3 lighting = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, normalWS);
#endif

#if defined(_SCREEN_SPACE_OCCLUSION)
                lighting *= GetScreenSpaceAmbientOcclusion(screenUV).indirectAmbientOcclusion;
#endif

#ifdef _RECEIVE_SHADOWS_OFF
                Light mainLight = GetMainLight();
#else
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS), input.positionWS, shadowMask);
#endif
                lighting += mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation
                                               * saturate(dot(normalWS, mainLight.direction)));

#ifdef _ADDITIONAL_LIGHTS
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalizedScreenSpaceUV = screenUV;

                uint lightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                    lighting += light.color * (light.distanceAttenuation * light.shadowAttenuation
                                               * saturate(dot(normalWS, light.direction)));
                LIGHT_LOOP_END
#endif
                return lighting;
            }

            half4 MirrorFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                MirrorSurface s = GetMirrorSurface(input.uv, input.positionCS, input.positionWS,
                                                   input.normalWS, input.tangentWS, viewDirWS);

#ifdef _ALPHATEST_ON
                clip(s.alpha - _Cutoff);
#endif

                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                half3 lighting = MirrorDiffuseLighting(input, s.normalWS, screenUV);
                half3 cameraColor = lerp(s.albedo * lighting, s.cameraReflection, s.cameraAmount);
                half3 fallbackColor = s.fallbackAlbedo * lighting + s.fallbackSpecular;
                half3 color = lerp(fallbackColor, cameraColor, s.blend);
                color = MixFog(color, input.fogFactor);

                color = AlphaModulate(color, s.alpha);
                color = AlphaPremultiply(color, s.alpha);

#if defined(_SURFACE_TYPE_TRANSPARENT)
                return half4(color, s.alpha);
#else
                return half4(color, 1.0h);
#endif
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #define SHADOW_CASTER_PASS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "MirrorCore.hlsl"
            #include "MirrorDepthPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing
            #define DEPTH_ONLY_PASS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
            #include "MirrorCore.hlsl"
            #include "MirrorDepthPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormalsOnly" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing
            #define DEPTH_NORMALS_PASS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
            #include "MirrorCore.hlsl"
            #include "MirrorDepthPasses.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "EDIVE.Rendering.Mirrors.MirrorShaderGUI"
    FallBack "Universal Render Pipeline/Lit"
}
