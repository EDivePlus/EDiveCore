#ifndef EDIVE_LIT_UV_DECALS_INPUT_INCLUDED
#define EDIVE_LIT_UV_DECALS_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"

// --- UV-space decals --------------------------------------------------------
//
// A small set of 2D texture stamps placed directly in the mesh's own UV0 space.
// No projection and no extra vertex stream: because UV0 is authored per vertex and
// constant, the stamps deform with the surface exactly like the base texture does.
//
// All per-decal data is set per-renderer from UVDecalPainter.cs via a
// MaterialPropertyBlock, so it lives OUTSIDE UnityPerMaterial. That makes this
// shader not SRP Batcher compatible; see the note in UVDecalPainter.cs.
// MAX_UV_DECALS must match MAX_DECALS in UVDecalPainter.cs.

#ifndef MAX_UV_DECALS
#define MAX_UV_DECALS 2
#endif

TEXTURE2D_ARRAY(_UVDecals);   SAMPLER(sampler_UVDecals);

float4 _UVDecalRect[MAX_UV_DECALS];  // xy: UV centre, zw: UV size (0 size = slot unused)
float4 _UVDecalRot [MAX_UV_DECALS];  // x: cos, y: sin, z: texture-array slice, w: UV set (0 or 1)
float4 _UVDecalTint[MAX_UV_DECALS];  // rgba, a scales the whole stamp
float  _UVDecalCount;

half3 ApplyUVDecals(half3 albedo, float2 uv0, float2 uv1)
{
    int count = min((int) _UVDecalCount, MAX_UV_DECALS);

    [unroll]
    for (int i = 0; i < MAX_UV_DECALS; i++)
    {
        if (i >= count) break;

        float2 size = _UVDecalRect[i].zw;
        if (size.x <= 0.0 || size.y <= 0.0) continue;

        float2 uv = (_UVDecalRot[i].w < 0.5) ? uv0 : uv1;

        // into the decal's local, axis-aligned unit square
        float2 d = uv - _UVDecalRect[i].xy;
        float2 r = _UVDecalRot[i].xy;
        d = float2(d.x * r.x - d.y * r.y, d.x * r.y + d.y * r.x);
        float2 duv = d / size + 0.5;

        float2 inside = step(0.0, duv) * step(duv, 1.0);
        half4 tex = SAMPLE_TEXTURE2D_ARRAY(_UVDecals, sampler_UVDecals, duv, _UVDecalRot[i].z);
        half weight = tex.a * _UVDecalTint[i].a * half(inside.x * inside.y);

        albedo = lerp(albedo, tex.rgb * _UVDecalTint[i].rgb, weight);
    }

    return albedo;
}

#endif // EDIVE_LIT_UV_DECALS_INPUT_INCLUDED
