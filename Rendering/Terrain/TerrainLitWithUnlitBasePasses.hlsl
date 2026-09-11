#ifndef EDIVE_TERRAIN_LIT_WITH_UNLIT_BASE_PASSES_INCLUDED
#define EDIVE_TERRAIN_LIT_WITH_UNLIT_BASE_PASSES_INCLUDED

// Included after URP's TerrainLitInput/TerrainLitPasses; reuses their helpers and the
// already-declared _BaseMap/sampler_BaseMap (re-declaring would be a redefinition error).

// Mirrors URP's SplatmapFragment (forward path).
void SplatmapFragmentUnlitBase(
    Varyings IN
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
    )
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
#ifdef _ALPHATEST_ON
    ClipHoles(IN.uvMainAndLM.xy);
#endif

    half3 normalTS = half3(0.0h, 0.0h, 1.0h);

    half4 hasMask = half4(_LayerHasMask0, _LayerHasMask1, _LayerHasMask2, _LayerHasMask3);
    half4 masks[4];
    ComputeMasks(masks, hasMask, IN);

    float2 splatUV = (IN.uvMainAndLM.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
    half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);

    half alpha = dot(splatControl, 1.0h);
#ifdef _TERRAIN_BLEND_HEIGHT
    // disable Height Based blend when there are more than 4 layers (multi-pass breaks the normalization)
    if (_NumLayersCount <= 4)
        HeightBasedSplatModify(splatControl, masks);
#endif

    half weight;
    half4 mixedDiffuse;
    half4 defaultSmoothness;
    SplatmapMix(IN.uvMainAndLM, IN.uvSplat01, IN.uvSplat23, splatControl, weight, mixedDiffuse, defaultSmoothness, normalTS);
    half3 albedo = mixedDiffuse.rgb;

#ifdef _TERRAIN_UNLIT_BASE
    // Layer 0 is the base. An untextured layer 0 samples the default checkerboard, which
    // leaks in at layer edges; swap its splat albedo for the base map so only the base shows.
    half3 unlitBase = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uvMainAndLM.xy).rgb;
    half3 splat0 = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, IN.uvSplat01.xy).rgb * _DiffuseRemapScale0.rgb;
    albedo += (unlitBase - splat0) * splatControl.r;
#endif

    half4 defaultMetallic = half4(_Metallic0, _Metallic1, _Metallic2, _Metallic3);
    half4 defaultOcclusion = half4(_MaskMapRemapScale0.g, _MaskMapRemapScale1.g, _MaskMapRemapScale2.g, _MaskMapRemapScale3.g) +
                            half4(_MaskMapRemapOffset0.g, _MaskMapRemapOffset1.g, _MaskMapRemapOffset2.g, _MaskMapRemapOffset3.g);

    half4 maskSmoothness = half4(masks[0].a, masks[1].a, masks[2].a, masks[3].a);
    defaultSmoothness = lerp(defaultSmoothness, maskSmoothness, hasMask);
    half smoothness = dot(splatControl, defaultSmoothness);

    half4 maskMetallic = half4(masks[0].r, masks[1].r, masks[2].r, masks[3].r);
    defaultMetallic = lerp(defaultMetallic, maskMetallic, hasMask);
    half metallic = dot(splatControl, defaultMetallic);

    half4 maskOcclusion = half4(masks[0].g, masks[1].g, masks[2].g, masks[3].g);
    defaultOcclusion = lerp(defaultOcclusion, maskOcclusion, hasMask);
    half occlusion = dot(splatControl, defaultOcclusion);

    InputData inputData;
    InitializeInputData(IN, normalTS, inputData);
    SetupTerrainDebugTextureData(inputData, IN.uvMainAndLM.xy);

#if defined(_DBUFFER)
    half3 specular = half3(0.0h, 0.0h, 0.0h);
    ApplyDecal(IN.clipPos,
        albedo,
        specular,
        inputData.normalWS,
        metallic,
        occlusion,
        smoothness);
#endif

    InitializeBakedGIData(IN, inputData);

    half4 color = UniversalFragmentPBR(inputData, albedo, metallic, /* specular */ half3(0.0h, 0.0h, 0.0h), smoothness, occlusion, /* emission */ half3(0, 0, 0), alpha);

#ifdef _TERRAIN_UNLIT_BASE
    // Base is drawn unlit (its lighting is baked into the photo): fade the lit result back
    // to the raw base map over layer 0's weight. Before SplatmapFinalColor so it still fogs.
    color.rgb = lerp(color.rgb, unlitBase, splatControl.r);
#endif

    SplatmapFinalColor(color, inputData.fogCoord);

    outColor = half4(color.rgb, 1.0h);

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif // EDIVE_TERRAIN_LIT_WITH_UNLIT_BASE_PASSES_INCLUDED
