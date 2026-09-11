#ifndef EDIVE_LIT_TOP_LAYER_INCLUDED
#define EDIVE_LIT_TOP_LAYER_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"

TEXTURE2D(_TopMask);
TEXTURE2D(_TopMaskNoiseMap);
TEXTURE2D(_TopMap);     SAMPLER(sampler_TopMap);
TEXTURE2D(_TopBumpMap);

// uv has base map tiling. Mask alpha 1 = top layer.
void ApplyTopLayer(float2 uv, inout SurfaceData surfaceData)
{
    float2 meshUV = (uv - _BaseMap_ST.zw) / _BaseMap_ST.xy;
    float2 topUV = TRANSFORM_TEX(meshUV, _TopMap);
    half3 topColor = SAMPLE_TEXTURE2D(_TopMap, sampler_TopMap, topUV).rgb * _TopColor.rgb;

    half noise = SAMPLE_TEXTURE2D(_TopMaskNoiseMap, sampler_TopMap, TRANSFORM_TEX(meshUV, _TopMaskNoiseMap)).r;
    half mask = SAMPLE_TEXTURE2D(_TopMask, sampler_LinearClamp, meshUV).a;
    mask = saturate((mask + (noise - 0.5h) * _TopMaskNoise - 0.5h) * _TopMaskSharpness + 0.5h);

#if defined(_SPECULAR_SETUP)
    surfaceData.albedo = lerp(surfaceData.albedo, topColor * (1.0h - _TopMetallic), mask);
    surfaceData.specular = lerp(surfaceData.specular, lerp(half3(0.04h, 0.04h, 0.04h), topColor, _TopMetallic), mask);
#else
    surfaceData.albedo = lerp(surfaceData.albedo, topColor, mask);
    surfaceData.metallic = lerp(surfaceData.metallic, _TopMetallic, mask);
#endif
    surfaceData.smoothness = lerp(surfaceData.smoothness, _TopSmoothness, mask);
    surfaceData.occlusion = lerp(surfaceData.occlusion, 1.0h, mask);
    surfaceData.emission *= 1.0h - mask;

#if defined(_NORMALMAP)
    half3 topNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_TopBumpMap, sampler_TopMap, topUV), _TopBumpScale);
    surfaceData.normalTS = normalize(lerp(surfaceData.normalTS, topNormal, mask));
#endif
}

#endif // EDIVE_LIT_TOP_LAYER_INCLUDED
