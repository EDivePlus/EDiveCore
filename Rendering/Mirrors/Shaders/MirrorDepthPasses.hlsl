#ifndef MIRROR_DEPTH_PASSES_INCLUDED
#define MIRROR_DEPTH_PASSES_INCLUDED

// ShadowCaster / DepthOnly / DepthNormals for both mirror shaders.
// Only alpha matters, so no mirror sampling.

struct DepthAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float4 tangentOS  : TANGENT;
    float2 uv         : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct DepthVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
#if defined(DEPTH_NORMALS_PASS)
    half3  normalWS   : TEXCOORD1;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

half MirrorDepthAlpha(float2 uv)
{
    return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv * _BaseMap_ST.xy + _BaseMap_ST.zw).a * _BaseColor.a * _Alpha;
}

void MirrorAlphaClip(float2 uv)
{
#ifdef _ALPHATEST_ON
    clip(MirrorDepthAlpha(uv) - _Cutoff);
#endif
}

#if defined(SHADOW_CASTER_PASS)
float3 _LightDirection;
float3 _LightPosition;

float4 MirrorShadowPosition(float3 positionWS, half3 normalWS)
{
#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif
    return positionCS;
}

DepthVaryings ShadowVertex(DepthAttributes input)
{
    DepthVaryings output = (DepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    half3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.positionCS = MirrorShadowPosition(positionWS, normalWS);
    output.uv = input.uv;
    return output;
}

half4 ShadowFragment(DepthVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    MirrorAlphaClip(input.uv);
    return 0;
}
#endif

#if defined(DEPTH_ONLY_PASS)
DepthVaryings DepthOnlyVertex(DepthAttributes input)
{
    DepthVaryings output = (DepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.uv;
    return output;
}

half4 DepthOnlyFragment(DepthVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    MirrorAlphaClip(input.uv);
    return 0;
}
#endif

#if defined(DEPTH_NORMALS_PASS)
DepthVaryings DepthNormalsVertex(DepthAttributes input)
{
    DepthVaryings output = (DepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs nrm = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    output.positionCS = pos.positionCS;
    output.normalWS = nrm.normalWS;
    output.uv = input.uv;
    return output;
}

half4 DepthNormalsFragment(DepthVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    MirrorAlphaClip(input.uv);
    return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
}
#endif

#endif
