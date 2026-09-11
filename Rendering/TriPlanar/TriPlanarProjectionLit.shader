// URP Lit with triplanar UVs. Same properties as URP Lit.
Shader "EDIVE/TriPlanar Projection Lit"
{
    Properties
    {
        // Specular vs Metallic workflow
        _WorkflowMode("WorkflowMode", Float) = 1.0

        [MainTexture] [NoScaleOffset] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
        _BaseMapStrength("Base Map Strength", Range(0.0, 1.0)) = 1.0

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        [NoScaleOffset] _MetallicGlossMap("Metallic", 2D) = "white" {}

        _SpecColor("Specular", Color) = (0.2, 0.2, 0.2)
        [NoScaleOffset] _SpecGlossMap("Specular", 2D) = "white" {}

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0

        _BumpScale("Scale", Float) = 1.0
        [NoScaleOffset] [Normal] _BumpMap("Normal Map", 2D) = "bump" {}

        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        [NoScaleOffset] _OcclusionMap("Occlusion", 2D) = "white" {}

        _Parallax("Scale", Range(0.005, 0.08)) = 0.005
        [NoScaleOffset] _ParallaxMap("Height Map", 2D) = "black" {}

        [HDR] _EmissionColor("Color", Color) = (0, 0, 0)
        [NoScaleOffset] _EmissionMap("Emission", 2D) = "white" {}

        // Detail, own tiling
        [NoScaleOffset] _DetailMask("Detail Mask", 2D) = "white" {}
        _DetailAlbedoMapScale("Scale", Range(0.0, 2.0)) = 1.0
        [NoScaleOffset] _DetailAlbedoMap("Detail Albedo x2", 2D) = "linearGrey" {}
        _DetailNormalMapScale("Scale", Range(0.0, 2.0)) = 1.0
        [NoScaleOffset] [Normal] _DetailNormalMap("Normal Map", 2D) = "bump" {}
        _DetailTiling("Detail Tiling", Vector) = (1, 1, 1, 0)
        _DetailOffset("Detail Offset", Vector) = (0, 0, 0, 0)

        // Triplanar projection
        [KeywordEnum(World, Object)] _ProjectionSpace("Projection Space", Float) = 0
        _Tiling("Tiling", Vector) = (1, 1, 1, 0)
        _ProjectionOffset("Offset", Vector) = (0, 0, 0, 0)
        _BlendSharpness("Blend Sharpness", Range(1.0, 64.0)) = 8.0

        // Set by material GUI
        _Surface("__surface", Float) = 0.0
        _Blend("__blend", Float) = 0.0
        _Cull("__cull", Float) = 2.0
        [ToggleUI] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _BlendModePreserveSpecular("_BlendModePreserveSpecular", Float) = 1.0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0

        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
        _QueueOffset("Queue offset", Float) = 0.0

        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"

        #if defined(LOD_FADE_CROSSFADE)
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
        #endif

        #if defined(_DETAIL_MULX2) || defined(_DETAIL_SCALED)
        #define _TRIPLANAR_DETAIL
        #endif

        TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);
        TEXTURE2D(_SpecGlossMap);       SAMPLER(sampler_SpecGlossMap);
        TEXTURE2D(_OcclusionMap);       SAMPLER(sampler_OcclusionMap);
        TEXTURE2D(_ParallaxMap);        SAMPLER(sampler_ParallaxMap);
        TEXTURE2D(_DetailMask);         SAMPLER(sampler_DetailMask);
        TEXTURE2D(_DetailAlbedoMap);    SAMPLER(sampler_DetailAlbedoMap);
        TEXTURE2D(_DetailNormalMap);    SAMPLER(sampler_DetailNormalMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _Tiling;
            float4 _ProjectionOffset;
            float4 _DetailTiling;
            float4 _DetailOffset;
            half4 _BaseColor;
            half4 _SpecColor;
            half4 _EmissionColor;
            half _Cutoff;
            half _Smoothness;
            half _Metallic;
            half _BumpScale;
            half _OcclusionStrength;
            half _DetailAlbedoMapScale;
            half _DetailNormalMapScale;
            half _Parallax;
            half _BaseMapStrength;
            float _BlendSharpness;
            float _Surface;
        CBUFFER_END

        struct TriplanarUV
        {
            float2 x;
            float2 y;
            float2 z;
            half3 weights;
        };

        void GetProjectionSpace(float3 positionWS, float3 normalWS, out float3 position, out float3 normal)
        {
        #if defined(_PROJECTIONSPACE_OBJECT)
            position = TransformWorldToObject(positionWS);
            normal = normalize(mul(normalWS, (float3x3)GetObjectToWorldMatrix()));
        #else
            position = positionWS;
            normal = normalWS;
        #endif
        }

        float3 WorldToProjectionDir(float3 dirWS)
        {
        #if defined(_PROJECTIONSPACE_OBJECT)
            return TransformWorldToObjectDir(dirWS, false);
        #else
            return dirWS;
        #endif
        }

        half3 ProjectionToWorldNormal(half3 normal)
        {
        #if defined(_PROJECTIONSPACE_OBJECT)
            return TransformObjectToWorldNormal(normal);
        #else
            return normal;
        #endif
        }

        TriplanarUV GetTriplanarUV(float3 position, float3 normal, float3 tiling)
        {
            float3 p = (position - _ProjectionOffset.xyz) * tiling;
            TriplanarUV uv;
            uv.x = p.zy;
            uv.y = p.xz;
            uv.z = p.xy;
            float3 weights = pow(abs(normal), _BlendSharpness);
            uv.weights = half3(weights / (weights.x + weights.y + weights.z));
            return uv;
        }

        // Shares base weights
        TriplanarUV GetDetailUV(float3 position, TriplanarUV baseUV)
        {
            float3 p = (position - _ProjectionOffset.xyz - _DetailOffset.xyz) * _DetailTiling.xyz;
            TriplanarUV uv;
            uv.x = p.zy;
            uv.y = p.xz;
            uv.z = p.xy;
            uv.weights = baseUV.weights;
            return uv;
        }

        half4 SampleTriplanar(TEXTURE2D_PARAM(map, sampler_map), TriplanarUV uv)
        {
            return SAMPLE_TEXTURE2D(map, sampler_map, uv.x) * uv.weights.x
                 + SAMPLE_TEXTURE2D(map, sampler_map, uv.y) * uv.weights.y
                 + SAMPLE_TEXTURE2D(map, sampler_map, uv.z) * uv.weights.z;
        }

        half4 SampleBaseMap(TriplanarUV uv)
        {
            return lerp(half4(1, 1, 1, 1), SampleTriplanar(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), uv), _BaseMapStrength) * _BaseColor;
        }

        // One-step parallax per plane
        void ApplyTriplanarParallax(float3 viewDirWS, float3 normal, inout TriplanarUV uv, inout TriplanarUV detailUV)
        {
        #if defined(_PARALLAXMAP)
            half3 v = half3(normalize(WorldToProjectionDir(viewDirWS)));
            half3 facing = half3(sign(normal.x), sign(normal.y), sign(normal.z));

            float2 offsetX = ParallaxMapping(TEXTURE2D_ARGS(_ParallaxMap, sampler_ParallaxMap), half3(v.z, v.y, v.x * facing.x), _Parallax, uv.x);
            float2 offsetY = ParallaxMapping(TEXTURE2D_ARGS(_ParallaxMap, sampler_ParallaxMap), half3(v.x, v.z, v.y * facing.y), _Parallax, uv.y);
            float2 offsetZ = ParallaxMapping(TEXTURE2D_ARGS(_ParallaxMap, sampler_ParallaxMap), half3(v.x, v.y, v.z * facing.z), _Parallax, uv.z);

            uv.x += offsetX;
            uv.y += offsetY;
            uv.z += offsetZ;

            // Scale offset to detail tiling
            float3 baseTiling = _Tiling.xyz;
            baseTiling = abs(baseTiling) < 1e-6 ? float3(1, 1, 1) : baseTiling;
            float3 ratio = _DetailTiling.xyz / baseTiling;
            detailUV.x += offsetX * ratio.zy;
            detailUV.y += offsetY * ratio.xz;
            detailUV.z += offsetZ * ratio.xy;
        #endif
        }

        // Base tiling, like URP Lit
        half SampleDetailMask(TriplanarUV uv)
        {
        #if defined(_TRIPLANAR_DETAIL)
            return SampleTriplanar(TEXTURE2D_ARGS(_DetailMask, sampler_DetailMask), uv).a;
        #else
            return 1;
        #endif
        }

        half3 ApplyDetailAlbedo(half3 albedo, TriplanarUV detailUV, half mask)
        {
        #if defined(_TRIPLANAR_DETAIL)
            half3 detailAlbedo = SampleTriplanar(TEXTURE2D_ARGS(_DetailAlbedoMap, sampler_DetailAlbedoMap), detailUV).rgb;
        #if defined(_DETAIL_SCALED)
            detailAlbedo = half(2.0) * detailAlbedo * _DetailAlbedoMapScale - _DetailAlbedoMapScale + half(1.0);
        #else
            detailAlbedo = half(2.0) * detailAlbedo;
        #endif
            return albedo * LerpWhiteTo(detailAlbedo, mask);
        #else
            return albedo;
        #endif
        }

        half3 ApplyDetailNormal(half3 normalTS, float2 detailUV, half mask)
        {
        #if defined(_TRIPLANAR_DETAIL)
            half3 detailNormalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, detailUV), _DetailNormalMapScale);
            return lerp(normalTS, BlendNormalRNM(normalTS, normalize(detailNormalTS)), mask);
        #else
            return normalTS;
        #endif
        }

        // Whiteout blend, projection space
        half3 SampleTriplanarNormal(TriplanarUV uv, float3 normal, TriplanarUV detailUV, half detailMask)
        {
        #if defined(_NORMALMAP) || defined(_TRIPLANAR_DETAIL)
            half3 normalX = half3(0, 0, 1);
            half3 normalY = half3(0, 0, 1);
            half3 normalZ = half3(0, 0, 1);

        #if defined(_NORMALMAP)
            normalX = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv.x), _BumpScale);
            normalY = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv.y), _BumpScale);
            normalZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv.z), _BumpScale);
        #endif

            normalX = ApplyDetailNormal(normalX, detailUV.x, detailMask);
            normalY = ApplyDetailNormal(normalY, detailUV.y, detailMask);
            normalZ = ApplyDetailNormal(normalZ, detailUV.z, detailMask);

            normalX = half3(normalX.xy + normal.zy, abs(normalX.z) * normal.x);
            normalY = half3(normalY.xy + normal.xz, abs(normalY.z) * normal.y);
            normalZ = half3(normalZ.xy + normal.xy, abs(normalZ.z) * normal.z);

            return normalize(normalX.zyx * uv.weights.x
                           + normalY.xzy * uv.weights.y
                           + normalZ.xyz * uv.weights.z);
        #else
            return half3(normal);
        #endif
        }

        half4 SampleMetallicSpecGloss(TriplanarUV uv, half albedoAlpha)
        {
            half4 specGloss;
        #ifdef _METALLICSPECGLOSSMAP
        #ifdef _SPECULAR_SETUP
            specGloss = SampleTriplanar(TEXTURE2D_ARGS(_SpecGlossMap, sampler_SpecGlossMap), uv);
        #else
            specGloss = SampleTriplanar(TEXTURE2D_ARGS(_MetallicGlossMap, sampler_MetallicGlossMap), uv);
        #endif
        #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            specGloss.a = albedoAlpha * _Smoothness;
        #else
            specGloss.a *= _Smoothness;
        #endif
        #else
        #ifdef _SPECULAR_SETUP
            specGloss.rgb = _SpecColor.rgb;
        #else
            specGloss.rgb = _Metallic.xxx;
        #endif
        #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            specGloss.a = albedoAlpha * _Smoothness;
        #else
            specGloss.a = _Smoothness;
        #endif
        #endif
            return specGloss;
        }

        half SampleOcclusion(TriplanarUV uv)
        {
        #ifdef _OCCLUSIONMAP
            half occ = SampleTriplanar(TEXTURE2D_ARGS(_OcclusionMap, sampler_OcclusionMap), uv).g;
            return LerpWhiteTo(occ, _OcclusionStrength);
        #else
            return half(1.0);
        #endif
        }

        void InitializeTriplanarSurfaceData(TriplanarUV uv, TriplanarUV detailUV, half detailMask, out SurfaceData surfaceData)
        {
            surfaceData = (SurfaceData)0;

            half4 baseMap = SampleBaseMap(uv);
            surfaceData.alpha = Alpha(baseMap.a, half4(1, 1, 1, 1), _Cutoff);
            surfaceData.albedo = ApplyDetailAlbedo(baseMap.rgb, detailUV, detailMask);

            half4 specGloss = SampleMetallicSpecGloss(uv, baseMap.a);
        #if _SPECULAR_SETUP
            surfaceData.metallic = half(1.0);
            surfaceData.specular = specGloss.rgb;
        #else
            surfaceData.metallic = specGloss.r;
            surfaceData.specular = half3(0.0, 0.0, 0.0);
        #endif
            surfaceData.smoothness = specGloss.a;
            surfaceData.occlusion = SampleOcclusion(uv);
            surfaceData.emission = SampleEmission(uv.z, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
            surfaceData.normalTS = half3(0, 0, 1);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            ZWrite [_ZWrite]
            Cull [_Cull]
            AlphaToMask [_AlphaToMask]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #pragma shader_feature_local _PROJECTIONSPACE_WORLD _PROJECTIONSPACE_OBJECT
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _PARALLAXMAP
            #pragma shader_feature_local_fragment _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT

            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile_fragment _ REFLECTION_PROBE_ROTATION
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma instancing_options renderinglayer

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 staticLightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                half4 fogFactorAndVertexLight : TEXCOORD2;
            #else
                half fogFactor : TEXCOORD2;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord : TEXCOORD3;
            #endif
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 4);
            #ifdef USE_APV_PROBE_OCCLUSION
                float4 probeOcclusion : TEXCOORD5;
            #endif
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionWS = vertexInput.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                half fogFactor = 0;
            #if !defined(_FOG_FRAGMENT)
                fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
            #endif
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                output.fogFactorAndVertexLight = half4(fogFactor, VertexLighting(vertexInput.positionWS, output.normalWS));
            #else
                output.fogFactor = fogFactor;
            #endif

                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
                OUTPUT_SH4(vertexInput.positionWS, output.normalWS, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, output.probeOcclusion);

            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                output.shadowCoord = GetShadowCoord(vertexInput);
            #endif

                output.positionCS = vertexInput.positionCS;
                return output;
            }

            void LitPassFragment(
                Varyings input
                , out half4 outColor : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            #ifdef LOD_FADE_CROSSFADE
                LODFadeCrossFade(input.positionCS);
            #endif

                float3 position, normal;
                GetProjectionSpace(input.positionWS, normalize(input.normalWS), position, normal);
                TriplanarUV uv = GetTriplanarUV(position, normal, _Tiling.xyz);
                TriplanarUV detailUV = GetDetailUV(position, uv);
                ApplyTriplanarParallax(GetWorldSpaceNormalizeViewDir(input.positionWS), normal, uv, detailUV);

                half detailMask = SampleDetailMask(uv);

                SurfaceData surfaceData;
                InitializeTriplanarSurfaceData(uv, detailUV, detailMask, surfaceData);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = ProjectionToWorldNormal(SampleTriplanarNormal(uv, normal, detailUV, detailMask));
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                inputData.shadowCoord = input.shadowCoord;
            #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            #else
                inputData.shadowCoord = float4(0, 0, 0, 0);
            #endif
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1), input.fogFactorAndVertexLight.x);
                inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
            #else
                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1), input.fogFactor);
            #endif
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

            #if !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
                inputData.bakedGI = SAMPLE_GI(input.vertexSH,
                    GetAbsolutePositionWS(inputData.positionWS),
                    inputData.normalWS,
                    inputData.viewDirectionWS,
                    input.positionCS.xy,
                    input.probeOcclusion,
                    inputData.shadowMask);
            #else
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
            #endif

            #if defined(_DBUFFER)
                ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
            #endif

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));
                outColor = color;

            #ifdef _WRITE_RENDERING_LAYERS
                outRenderingLayers = EncodeMeshRenderingLayer();
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
            #pragma target 2.0

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma shader_feature_local _PROJECTIONSPACE_WORLD _PROJECTIONSPACE_OBJECT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            #if defined(_ALPHATEST_ON)
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                output.positionCS = ApplyShadowClamping(output.positionCS);
            #if defined(_ALPHATEST_ON)
                output.positionWS = positionWS;
                output.normalWS = normalWS;
            #endif
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
            #if defined(_ALPHATEST_ON)
                float3 position, normal;
                GetProjectionSpace(input.positionWS, normalize(input.normalWS), position, normal);
                Alpha(SampleBaseMap(GetTriplanarUV(position, normal, _Tiling.xyz)).a, half4(1, 1, 1, 1), _Cutoff);
            #endif
            #ifdef LOD_FADE_CROSSFADE
                LODFadeCrossFade(input.positionCS);
            #endif
                return 0;
            }
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
            #pragma target 2.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma shader_feature_local _PROJECTIONSPACE_WORLD _PROJECTIONSPACE_OBJECT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            #if defined(_ALPHATEST_ON)
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            #if defined(_ALPHATEST_ON)
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            #endif
                return output;
            }

            half DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
            #if defined(_ALPHATEST_ON)
                float3 position, normal;
                GetProjectionSpace(input.positionWS, normalize(input.normalWS), position, normal);
                Alpha(SampleBaseMap(GetTriplanarUV(position, normal, _Tiling.xyz)).a, half4(1, 1, 1, 1), _Cutoff);
            #endif
            #ifdef LOD_FADE_CROSSFADE
                LODFadeCrossFade(input.positionCS);
            #endif
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #pragma shader_feature_local _PROJECTIONSPACE_WORLD _PROJECTIONSPACE_OBJECT
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _PARALLAXMAP
            #pragma shader_feature_local_fragment _DETAIL_MULX2 _DETAIL_SCALED

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            void DepthNormalsFragment(
                Varyings input
                , out half4 outNormalWS : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            #ifdef LOD_FADE_CROSSFADE
                LODFadeCrossFade(input.positionCS);
            #endif

                float3 position, normal;
                GetProjectionSpace(input.positionWS, normalize(input.normalWS), position, normal);
                TriplanarUV uv = GetTriplanarUV(position, normal, _Tiling.xyz);
                TriplanarUV detailUV = GetDetailUV(position, uv);
                ApplyTriplanarParallax(GetWorldSpaceNormalizeViewDir(input.positionWS), normal, uv, detailUV);

            #if defined(_ALPHATEST_ON)
                Alpha(SampleBaseMap(uv).a, half4(1, 1, 1, 1), _Cutoff);
            #endif

                half detailMask = SampleDetailMask(uv);
                outNormalWS = half4(ProjectionToWorldNormal(SampleTriplanarNormal(uv, normal, detailUV, detailMask)), 0);

            #ifdef _WRITE_RENDERING_LAYERS
                outRenderingLayers = EncodeMeshRenderingLayer();
            #endif
            }
            ENDHLSL
        }

        // Lightmap bake, uses UV0
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex MetaPassVertex
            #pragma fragment MetaPassFragment

            #pragma shader_feature_local_fragment _EMISSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings MetaPassVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = MetaVertexPosition(input.positionOS, input.uv1, input.uv2, unity_LightmapST, unity_DynamicLightmapST);
                output.uv = input.uv0;
                return output;
            }

            half4 MetaPassFragment(Varyings input) : SV_Target
            {
                MetaInput meta = (MetaInput)0;
                meta.Albedo = lerp(1.0, SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb, _BaseMapStrength) * _BaseColor.rgb;
                meta.Emission = SampleEmission(input.uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
                return UnityMetaFragment(meta);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "EDIVE.Rendering.TriPlanar.TriPlanarProjectionLitShaderGUI"
}
