#ifndef MIRROR_CORE_INCLUDED
#define MIRROR_CORE_INCLUDED

// Shared by MirrorUnlit and MirrorLit. Same properties, so materials swap freely.

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseColor;
    float4 _ReflectionTint;
    float4 _FallbackEnvColor;
    float4 _FallbackColor;
    float _Metallic;
    float4 _AlbedoSpeed;
    float4 _NormalSpeed;
    float4 _FallbackCubemapHDR;
    float4 _FallbackProbePos;
    float4 _FallbackBoxMin;
    float4 _FallbackBoxMax;
    float _MirrorEye;
    float _MirrorFlipY;
    float _MirrorBlend;
    float _Blur;
    float _Refraction;
    float _BumpScale;
    float _Reflectivity;
    float _FresnelPower;
    float _Smoothness;
    float _Alpha;
    float _Cutoff;
CBUFFER_END

TEXTURE2D(_MirrorTexLeft);   SAMPLER(sampler_MirrorTexLeft);
TEXTURE2D(_MirrorTexRight);  SAMPLER(sampler_MirrorTexRight);
TEXTURE2D(_BaseMap);    SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap);    SAMPLER(sampler_BumpMap);
TEXTURE2D(_MaskMap);    SAMPLER(sampler_MaskMap);

// Optional. Empty uses the probe Unity bound.
TEXTURECUBE(_FallbackCubemap);  SAMPLER(sampler_FallbackCubemap);

#ifdef MIRROR_USE_URP_PROBES
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
#endif

static const half kMirrorBlurCenter = 0.147761h;
static const half kMirrorBlurEdge   = 0.118318h;
static const half kMirrorBlurCorner = 0.0947416h;

// _MirrorEye: 0 left, 1 right, negative means work it out.
// Only single pass stereo leaves it negative, and that is the only
// case where unity_StereoEyeIndex means anything.
bool MirrorIsLeftEye()
{
    uint eye = (uint) max(_MirrorEye, 0);

#if defined(USING_STEREO_MATRICES)
    if (_MirrorEye < 0)
        eye = unity_StereoEyeIndex;
#endif

    return eye == 0;
}

float2 MirrorScreenUV(float4 positionCS)
{
    float y = (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - positionCS.y) : positionCS.y;
    float2 uv = float2(positionCS.x, y) / _ScaledScreenParams.xy;
    uv.y = 1.0 - uv.y;
    if (_MirrorFlipY > 0.5)
        uv.y = 1.0 - uv.y;
    return uv;
}

// 3x3 gaussian. Offsets are screen sized. X scaled to keep it round.
half4 MirrorBlurTaps(TEXTURE2D_PARAM(tex, samp), float2 uv, float2 s)
{
    half4 c  = SAMPLE_TEXTURE2D_LOD(tex, samp, uv,                        0) * kMirrorBlurCenter;
    c += SAMPLE_TEXTURE2D_LOD(tex, samp, uv + float2(-s.x, 0),            0) * kMirrorBlurEdge;
    c += SAMPLE_TEXTURE2D_LOD(tex, samp, uv + float2( s.x, 0),            0) * kMirrorBlurEdge;
    c += SAMPLE_TEXTURE2D_LOD(tex, samp, uv + float2( 0,  -s.y),          0) * kMirrorBlurEdge;
    c += SAMPLE_TEXTURE2D_LOD(tex, samp, uv + float2( 0,   s.y),          0) * kMirrorBlurEdge;
    c += SAMPLE_TEXTURE2D_LOD(tex, samp, uv + float2(-s.x, -s.y),         0) * kMirrorBlurCorner;
    c += SAMPLE_TEXTURE2D_LOD(tex, samp, uv + float2( s.x, -s.y),         0) * kMirrorBlurCorner;
    c += SAMPLE_TEXTURE2D_LOD(tex, samp, uv + float2(-s.x,  s.y),         0) * kMirrorBlurCorner;
    c += SAMPLE_TEXTURE2D_LOD(tex, samp, uv + float2( s.x,  s.y),         0) * kMirrorBlurCorner;
    return c;
}

// One eye. No mips, so LOD 0 keeps the branch safe.
half4 SampleMirror(float2 uv, half blurScale)
{
    bool left = MirrorIsLeftEye();
    half4 c = half4(0, 0, 0, 1);

#ifdef _BLUR_ON
    float b = _Blur * 0.01 * blurScale;
    float2 s = float2(b * (_ScaledScreenParams.y / _ScaledScreenParams.x), b);
    UNITY_BRANCH if (b > 1e-5)
    {
        if (left)
            c = MirrorBlurTaps(TEXTURE2D_ARGS(_MirrorTexLeft, sampler_MirrorTexLeft), uv, s);
        else
            c = MirrorBlurTaps(TEXTURE2D_ARGS(_MirrorTexRight, sampler_MirrorTexRight), uv, s);
    }
    else
#endif
    {
        if (left)
            c = SAMPLE_TEXTURE2D_LOD(_MirrorTexLeft, sampler_MirrorTexLeft, uv, 0);
        else
            c = SAMPLE_TEXTURE2D_LOD(_MirrorTexRight, sampler_MirrorTexRight, uv, 0);
    }
    return c;
}

half3 MirrorBoxProject(half3 dirWS, float3 positionWS, float4 probePos, float4 boxMin, float4 boxMax)
{
#ifdef _BOXPROJECTION_ON
    if (probePos.w > 0.0)
    {
        float3 invDir = rcp(dirWS);
        float3 t1 = (boxMax.xyz - positionWS) * invDir;
        float3 t2 = (boxMin.xyz - positionWS) * invDir;
        float3 tmax = max(t1, t2);
        float dist = min(min(tmax.x, tmax.y), tmax.z);
        dirWS = dirWS * dist + (positionWS - probePos.xyz);
    }
#endif
    return dirWS;
}

half3 SampleMirrorProbe(float3 positionWS, half3 normalWS, half3 viewDirWS, half perceptualRoughness, float2 screenUV)
{
    half3 r = reflect(-viewDirWS, normalWS);

#if defined(_PROBE_EXPLICIT) || !defined(MIRROR_USE_URP_PROBES)
    r = MirrorBoxProject(r, positionWS, _FallbackProbePos, _FallbackBoxMin, _FallbackBoxMax);
    half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);
    half4 encoded = SAMPLE_TEXTURECUBE_LOD(_FallbackCubemap, sampler_FallbackCubemap, r, mip);
    return DecodeHDREnvironment(encoded, _FallbackCubemapHDR);
#else
    return GlossyEnvironmentReflection(r, positionWS, perceptualRoughness, 1.0h, screenUV);
#endif
}

struct MirrorSurface
{
    half  alpha;
    half3 normalWS;

    // Used when the live reflection is there.
    half3 albedo;
    half3 cameraReflection;
    half  cameraAmount;

    // Used when it is not. Own material: diffuse to light, specular already scaled by metalness.
    half3 fallbackAlbedo;
    half3 fallbackSpecular;

    half  blend;
};

// positionCS is SV_Position. tangentWS.w carries the bitangent sign.
MirrorSurface GetMirrorSurface(float2 uv, float4 positionCS, float3 positionWS, half3 normalWS, half4 tangentWS, half3 viewDirWS)
{
    MirrorSurface s = (MirrorSurface)0;

    normalWS = normalize(normalWS);
    viewDirWS = normalize(viewDirWS);

    float2 normalUV = uv * _BaseMap_ST.xy + _BaseMap_ST.zw + _NormalSpeed.xy * _Time.y;
    half3 normalTS = half3(0, 0, 1);
#ifdef _NORMALMAP
    normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, normalUV), _BumpScale);
    half3 bitangentWS = cross(normalWS, tangentWS.xyz) * tangentWS.w;
    half3x3 tbn = half3x3(tangentWS.xyz, bitangentWS, normalWS);
    s.normalWS = normalize(mul(normalTS, tbn));
#else
    s.normalWS = normalWS;
#endif

    half4 mask = half4(1, 1, 1, 1);
#ifdef _MASKMAP
    mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uv * _BaseMap_ST.xy + _BaseMap_ST.zw);
#endif

    float2 albedoUV = uv * _BaseMap_ST.xy + _BaseMap_ST.zw + _AlbedoSpeed.xy * _Time.y;
    half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, albedoUV);
    s.albedo = albedo.rgb * _BaseColor.rgb;
    s.alpha = albedo.a * _BaseColor.a * _Alpha;

    // Screen offset. X scaled to stay round at any aspect.
    float2 screenUV = MirrorScreenUV(positionCS);
    screenUV += normalTS.xy * _Refraction * float2(_ScaledScreenParams.y / _ScaledScreenParams.x, 1.0);

    // Blur is for the live reflection. A mask keeps polished spots sharp.
    half blurScale = 1.0h;
#ifdef _MASKMAP
    blurScale = 1.0h - mask.a;
#endif
    half fresnel = pow(1.0h - saturate(dot(s.normalWS, viewDirWS)), _FresnelPower);

    // FROM CAMERA. Reflectivity is head on. Fresnel takes it to 1 at grazing.
    s.cameraReflection = SampleMirror(screenUV, blurScale).rgb * _ReflectionTint.rgb;
    s.cameraAmount = saturate(lerp(_Reflectivity, 1.0h, fresnel) * mask.r);

    // FALLBACK. Still PBR. The environment is a colour or a probe.
    // Metalness splits diffuse from specular the same either way.
    half3 environment = _FallbackEnvColor.rgb;
#ifdef _PROBE_FALLBACK
    environment = SampleMirrorProbe(positionWS, s.normalWS, viewDirWS, 1.0h - mask.a * _Smoothness, screenUV);
#endif

    half metallic = _Metallic * mask.r;
    half3 f0 = lerp(half3(0.04h, 0.04h, 0.04h), _FallbackColor.rgb, metallic);
    s.fallbackAlbedo = _FallbackColor.rgb * (1.0h - metallic);
    s.fallbackSpecular = environment * lerp(f0, half3(1, 1, 1), fresnel);

    s.blend = _MirrorBlend;
    return s;
}

#endif
