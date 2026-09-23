#ifndef EDIVE_UV_DECALS_INCLUDED
#define EDIVE_UV_DECALS_INCLUDED

// 4 slots. Same as UVDecalPainter.MAX_DECALS.

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"

TEXTURE2D(_UVDecal0Tex);
TEXTURE2D(_UVDecal1Tex);
TEXTURE2D(_UVDecal2Tex);
TEXTURE2D(_UVDecal3Tex);

// deriv: ddx.xy, ddy.xy per channel.
struct UVDecalCoords
{
    float2 uv[3];
    float4 deriv[3];
};

// rect: center, size (negative mirrors). parameters: cos, sin, uv channel, smoothness (<0 keeps).
void ApplyUVDecal(TEXTURE2D_PARAM(decalTex, decalSampler), float4 rect, float4 parameters, half4 tint,
                  UVDecalCoords coords, inout SurfaceData surfaceData)
{
    UNITY_BRANCH
    if (abs(rect.z) <= 0.0 || abs(rect.w) <= 0.0)
        return;

    uint channel = min((uint)(parameters.z + 0.5), 2u);
    float2 uv = coords.uv[channel];
    float4 deriv = coords.deriv[channel];

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

void ApplyUVDecals(inout SurfaceData surfaceData, float2 uv0, float2 uv1, float2 uv2)
{
    UVDecalCoords coords;
    coords.uv[0] = uv0;
    coords.uv[1] = uv1;
    coords.uv[2] = uv2;
    coords.deriv[0] = float4(ddx(uv0), ddy(uv0));
    coords.deriv[1] = float4(ddx(uv1), ddy(uv1));
    coords.deriv[2] = float4(ddx(uv2), ddy(uv2));

    ApplyUVDecal(TEXTURE2D_ARGS(_UVDecal0Tex, sampler_TrilinearClamp), _UVDecal0Rect, _UVDecal0Params, _UVDecal0Tint, coords, surfaceData);
    ApplyUVDecal(TEXTURE2D_ARGS(_UVDecal1Tex, sampler_TrilinearClamp), _UVDecal1Rect, _UVDecal1Params, _UVDecal1Tint, coords, surfaceData);
    ApplyUVDecal(TEXTURE2D_ARGS(_UVDecal2Tex, sampler_TrilinearClamp), _UVDecal2Rect, _UVDecal2Params, _UVDecal2Tint, coords, surfaceData);
    ApplyUVDecal(TEXTURE2D_ARGS(_UVDecal3Tex, sampler_TrilinearClamp), _UVDecal3Rect, _UVDecal3Params, _UVDecal3Tint, coords, surfaceData);
}

#endif // EDIVE_UV_DECALS_INCLUDED
