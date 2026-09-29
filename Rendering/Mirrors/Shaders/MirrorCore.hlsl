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
    // 0 colour, 1 reflection probe, 2 depth probe. Set by MirrorSurface.
    float _Environment;
    float _BoxProjection;
    // XYZ capture point, W range.
    float4 _DepthProbePos;
    float _DepthProbeSteps;
    float _MirrorEye;
    float _MirrorFlipY;
    float _MirrorBlend;
    float _MirrorBackground;
    // View projection the reflection was drawn with.
    float4x4 _MirrorVpLeft;
    float4x4 _MirrorVpRight;
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

// Distance from the capture point in R.
TEXTURECUBE(_DepthProbe);          SAMPLER(sampler_DepthProbe);
TEXTURECUBE(_DepthProbeDistance);  SAMPLER(sampler_DepthProbeDistance);

#ifdef MIRROR_USE_URP_PROBES
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
#endif

static const half kMirrorBlurCenter = 0.147761h;
static const half kMirrorBlurEdge   = 0.118318h;
static const half kMirrorBlurCorner = 0.0947416h;

// _MirrorEye: 0 left, 1 right. Negative uses unity_StereoEyeIndex, single pass only.
bool MirrorIsLeftEye()
{
    uint eye = (uint) max(_MirrorEye, 0);

#if defined(USING_STEREO_MATRICES)
    if (_MirrorEye < 0)
        eye = unity_StereoEyeIndex;
#endif

    return eye == 0;
}

// Real screen position. The probe wants this, the mirror does not.
float2 MirrorScreenUV(float4 positionCS)
{
    return GetNormalizedScreenSpaceUV(positionCS);
}

// The reflection may have been drawn for another view, so find the texel by world position.
float2 MirrorReprojectUV(float3 positionWS)
{
    float4x4 vp = MirrorIsLeftEye() ? _MirrorVpLeft : _MirrorVpRight;
    float4 clip = mul(vp, float4(positionWS, 1.0));
    float w = (abs(clip.w) < 1e-6) ? 1e-6 : clip.w;
    // Off screen fragments would land outside and the wrap mode would repeat the room.
    return saturate(clip.xy / w * 0.5 + 0.5);
}

float2 MirrorSampleUV(float3 positionWS)
{
    float2 uv = MirrorReprojectUV(positionWS);
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

// MirrorBlurTaps for probes. The tap spacing becomes an angle along the screen axes.
// Returns the tap count, 1 when there is no blur.
int MirrorProbeBlur(half blurScale, float3 dirDX, float3 dirDY, out float3 axisX, out float3 axisY)
{
    float angle = 0.0;
#ifdef _BLUR_ON
    angle = _Blur * 0.01 * blurScale * 2.0 * atan(1.0 / abs(UNITY_MATRIX_P._m11));
#endif
    axisX = SafeNormalize(dirDX) * angle;
    axisY = SafeNormalize(dirDY) * angle;
    return angle > 1e-5 ? 9 : 1;
}

float3 MirrorProbeTap(uint i, int taps, float3 dir, float3 axisX, float3 axisY, out half weight)
{
    float2 o = taps > 1 ? float2(i % 3u, i / 3u) - 1.0 : 0;
    weight = taps == 1 ? 1.0h : (o.x == 0 && o.y == 0 ? kMirrorBlurCenter : (o.x * o.y == 0 ? kMirrorBlurEdge : kMirrorBlurCorner));
    return dir + axisX * o.x + axisY * o.y;
}

half3 MirrorBoxProject(half3 dirWS, float3 positionWS, float4 probePos, float4 boxMin, float4 boxMax)
{
    UNITY_BRANCH if (_BoxProjection > 0.5 && probePos.w > 0.0)
    {
        float3 invDir = rcp(dirWS);
        float3 t1 = (boxMax.xyz - positionWS) * invDir;
        float3 t2 = (boxMin.xyz - positionWS) * invDir;
        float3 tmax = max(t1, t2);
        float dist = min(min(tmax.x, tmax.y), tmax.z);
        dirWS = dirWS * dist + (positionWS - probePos.xyz);
    }
    return dirWS;
}

half3 SampleMirrorProbeTap(float3 positionWS, half3 r, half perceptualRoughness, half mip, float2 screenUV)
{
    // Zero W means no probe was assigned, so use the one Unity picked.
    UNITY_BRANCH if (_FallbackProbePos.w <= 0.0)
    {
#ifdef MIRROR_USE_URP_PROBES
        return GlossyEnvironmentReflection(r, positionWS, perceptualRoughness, 1.0h, screenUV);
#else
        return DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, r, mip), unity_SpecCube0_HDR);
#endif
    }
    else
    {
        r = MirrorBoxProject(r, positionWS, _FallbackProbePos, _FallbackBoxMin, _FallbackBoxMax);
        return DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(_FallbackCubemap, sampler_FallbackCubemap, r, mip), _FallbackCubemapHDR);
    }
}

half3 SampleMirrorProbe(float3 positionWS, half3 r, half perceptualRoughness, int taps, float3 blurX, float3 blurY, float2 screenUV)
{
    half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);

    half3 color = 0;
    for (int i = 0; i < taps; i++)
    {
        half weight;
        half3 tap = normalize(MirrorProbeTap(i, taps, r, blurX, blurY, weight));
        color += SampleMirrorProbeTap(positionWS, tap, perceptualRoughness, mip, screenUV) * weight;
    }
    return color;
}

static const int   kDepthProbeRefine = 5;
static const float kDepthProbeStart  = 0.1;

// Marches the ray until it passes behind a baked surface.
// The mip comes from the smooth reflection derivatives, the hit direction jumps at edges.
// Blur taps share one march and spread around the hit.
half3 SampleDepthProbe(float3 positionWS, float3 dirWS, float3 dirDX, float3 dirDY, int taps, float3 blurX, float3 blurY)
{
    float3 origin = positionWS - _DepthProbePos.xyz;
    // Steps grow with distance.
    int steps = max((int)_DepthProbeSteps, 1);
    float growth = exp2(log2(max(_DepthProbePos.w, kDepthProbeStart * 2.0) / kDepthProbeStart) / steps);

    float before = 0.0;
    float t = kDepthProbeStart;
    bool hit = false;

    UNITY_LOOP
    for (int i = 0; i < steps; i++)
    {
        float3 p = origin + dirWS * t;
        float stored = SAMPLE_TEXTURECUBE_LOD(_DepthProbeDistance, sampler_DepthProbeDistance, p, 0).r;
        if (dot(p, p) >= stored * stored)
        {
            hit = true;
            break;
        }
        before = t;
        t *= growth;
    }

    UNITY_BRANCH if (hit)
    {
        float after = t;
        UNITY_LOOP
        for (int j = 0; j < kDepthProbeRefine; j++)
        {
            float mid = 0.5 * (before + after);
            float3 p = origin + dirWS * mid;
            float stored = SAMPLE_TEXTURECUBE_LOD(_DepthProbeDistance, sampler_DepthProbeDistance, p, 0).r;
            if (dot(p, p) >= stored * stored)
                after = mid;
            else
                before = mid;
        }
        t = after;
    }

    float3 hitDir = normalize(origin + dirWS * t);

    half3 color = 0;
    for (int k = 0; k < taps; k++)
    {
        half weight;
        float3 tap = MirrorProbeTap(k, taps, hitDir, blurX, blurY, weight);
        color += _DepthProbe.SampleGrad(sampler_DepthProbe, tap, dirDX, dirDY).rgb * weight;
    }
    return color;
}

struct MirrorSurface
{
    half  alpha;
    half3 normalWS;

    // Used when the live reflection is there.
    half3 albedo;
    half3 cameraReflection;
    half  cameraAmount;

    // Fallback material. Specular is already scaled by metalness.
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

    // Refraction offset. X scaled to stay round at any aspect.
    float2 mirrorUV = MirrorSampleUV(positionWS);
    mirrorUV += normalTS.xy * _Refraction * float2(_ScaledScreenParams.y / _ScaledScreenParams.x, 1.0);

    // Probe wants a real screen position.
    float2 screenUV = MirrorScreenUV(positionCS);

    // A mask keeps polished spots sharp.
    half blurScale = 1.0h;
#ifdef _MASKMAP
    blurScale = 1.0h - mask.a;
#endif

    // Probe blur runs along these, so derivatives are taken outside the branches.
    // Only the background is blurred. It fades out with the reflection, so the fallback stays sharp.
    float3 probeDir = reflect(-viewDirWS, s.normalWS);
    float3 probeDirDX = ddx(probeDir);
    float3 probeDirDY = ddy(probeDir);
    float3 blurX, blurY;
    int taps = MirrorProbeBlur(blurScale * _MirrorBackground * _MirrorBlend, probeDirDX, probeDirDY, blurX, blurY);

    half fresnel = pow(1.0h - saturate(dot(s.normalWS, viewDirWS)), _FresnelPower);

    // Live reflection, skipped when faded out. Alpha is 0 where nothing was drawn.
    // Reflectivity is head on, Fresnel takes it to 1 at grazing.
    half coverage = 0.0h;
    UNITY_BRANCH if (_MirrorBlend > 0.001)
    {
        half4 live = SampleMirror(mirrorUV, blurScale);
        s.cameraReflection = live.rgb * _ReflectionTint.rgb;
        coverage = live.a;
    }
    s.cameraAmount = saturate(lerp(_Reflectivity, 1.0h, fresnel) * mask.r);

    // Fallback. The environment is a colour or a probe, split into diffuse and specular by metalness.
    half3 environment = _FallbackEnvColor.rgb;
    UNITY_BRANCH if (_Environment > 0.5 && _Environment < 1.5)
        environment = SampleMirrorProbe(positionWS, probeDir, 1.0h - mask.a * _Smoothness, taps, blurX, blurY, screenUV);

    half metallic = _Metallic * mask.r;
    half3 f0 = lerp(half3(0.04h, 0.04h, 0.04h), _FallbackColor.rgb, metallic);
    s.fallbackAlbedo = _FallbackColor.rgb * (1.0h - metallic);
    s.fallbackSpecular = environment * lerp(f0, half3(1, 1, 1), fresnel);

    // With a background the environment fills what the reflection left empty.
    s.blend = _MirrorBlend * lerp(1.0h, coverage, _MirrorBackground);

    // Depth probe replaces the fallback material. Zero W means not baked, keep the colour.
    UNITY_BRANCH if (_Environment > 1.5 && _DepthProbePos.w > 0.0)
    {
        UNITY_BRANCH if (s.blend < 0.999)
        {
            half3 probe = SampleDepthProbe(positionWS, probeDir, probeDirDX, probeDirDY, taps, blurX, blurY) * _ReflectionTint.rgb;
            s.cameraReflection = lerp(probe, s.cameraReflection, s.blend);
        }
        s.blend = 1.0h;
    }
    return s;
}

#endif
