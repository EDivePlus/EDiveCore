#ifndef EDIVE_UV_DECALS_INCLUDED
#define EDIVE_UV_DECALS_INCLUDED

// 4 slots. Same as UVDecalPainter.MAX_DECALS.

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"

TEXTURE2D(_UVDecal0Tex);
TEXTURE2D(_UVDecal1Tex);
TEXTURE2D(_UVDecal2Tex);
TEXTURE2D(_UVDecal3Tex);

// rect: center, size. parameters: cos, sin, uv set, smoothness (<0 keeps).
void ApplyUVDecal(TEXTURE2D_PARAM(decalTex, decalSampler), float4 rect, float4 parameters, half4 tint,
                  float2 uv0, float2 uv1, float4 deriv0, float4 deriv1, inout SurfaceData surfaceData)
{
    UNITY_BRANCH
    if (rect.z <= 0.0 || rect.w <= 0.0)
        return;

    bool secondary = parameters.z > 0.5;
    float2 uv = secondary ? uv1 : uv0;
    float4 deriv = secondary ? deriv1 : deriv0;

    float2x2 rotation = float2x2(parameters.x, -parameters.y, parameters.y, parameters.x);
    float2 stampUV = mul(rotation, uv - rect.xy) / rect.zw + 0.5;
    float2 stampDdx = mul(rotation, deriv.xy) / rect.zw;
    float2 stampDdy = mul(rotation, deriv.zw) / rect.zw;

    float2 inside = step(0.0, stampUV) * step(stampUV, 1.0);
    half4 stamp = SAMPLE_TEXTURE2D_GRAD(decalTex, decalSampler, stampUV, stampDdx, stampDdy) * tint;
    half weight = stamp.a * half(inside.x * inside.y);

    surfaceData.albedo = lerp(surfaceData.albedo, stamp.rgb, weight);
    if (parameters.w >= 0.0)
        surfaceData.smoothness = lerp(surfaceData.smoothness, half(parameters.w), weight);
}

void ApplyUVDecals(inout SurfaceData surfaceData, float2 uv0, float2 uv1)
{
    float4 deriv0 = float4(ddx(uv0), ddy(uv0));
    float4 deriv1 = float4(ddx(uv1), ddy(uv1));

    ApplyUVDecal(TEXTURE2D_ARGS(_UVDecal0Tex, sampler_TrilinearClamp), _UVDecal0Rect, _UVDecal0Params, _UVDecal0Tint, uv0, uv1, deriv0, deriv1, surfaceData);
    ApplyUVDecal(TEXTURE2D_ARGS(_UVDecal1Tex, sampler_TrilinearClamp), _UVDecal1Rect, _UVDecal1Params, _UVDecal1Tint, uv0, uv1, deriv0, deriv1, surfaceData);
    ApplyUVDecal(TEXTURE2D_ARGS(_UVDecal2Tex, sampler_TrilinearClamp), _UVDecal2Rect, _UVDecal2Params, _UVDecal2Tint, uv0, uv1, deriv0, deriv1, surfaceData);
    ApplyUVDecal(TEXTURE2D_ARGS(_UVDecal3Tex, sampler_TrilinearClamp), _UVDecal3Rect, _UVDecal3Params, _UVDecal3Tint, uv0, uv1, deriv0, deriv1, surfaceData);
}

#endif // EDIVE_UV_DECALS_INCLUDED
