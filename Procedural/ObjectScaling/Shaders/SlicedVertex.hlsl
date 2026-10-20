// Author: František Holubec
// Created: 19.10.2026

// Slicing from ShaderMeshModule bake: UV6 offset, UV7 slope, _SliceScale is the stretch.
// Include after the URP pass file, the pass is wrapped by its Sliced* vertex function.

#ifndef EDIVE_SLICED_VERTEX_INCLUDED
#define EDIVE_SLICED_VERTEX_INCLUDED

#define SLICE_ATTRIBUTES \
    float3 sliceOffset : TEXCOORD6; \
    float3 sliceSlope : TEXCOORD7;

// scale - 1 per axis, zero at unit scale
float3 GetSliceFactor()
{
    return _SliceScale.xyz - 1;
}

float3 SlicePosition(float3 positionOS, float3 offset)
{
    return positionOS + offset * GetSliceFactor();
}

// Object space stretch of the vertex region, normals divide, tangents multiply
float3 SliceStretch(float3 slope)
{
    return 1 + slope * GetSliceFactor();
}

#if defined(UNIVERSAL_FORWARD_LIT_PASS_INCLUDED) || defined(UNIVERSAL_LIT_GBUFFER_PASS_INCLUDED)
struct SlicedAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 texcoord : TEXCOORD0;
    float2 staticLightmapUV : TEXCOORD1;
    float2 dynamicLightmapUV : TEXCOORD2;
    SLICE_ATTRIBUTES
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Attributes ToSlicedAttributes(SlicedAttributes input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    Attributes output = (Attributes)0;
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 stretch = SliceStretch(input.sliceSlope);
    output.positionOS = float4(SlicePosition(input.positionOS.xyz, input.sliceOffset), input.positionOS.w);
    output.normalOS = input.normalOS / stretch;
    output.tangentOS = float4(input.tangentOS.xyz * stretch, input.tangentOS.w);
    output.texcoord = input.texcoord;
    output.staticLightmapUV = input.staticLightmapUV;
    output.dynamicLightmapUV = input.dynamicLightmapUV;
    return output;
}
#endif

#if defined(UNIVERSAL_FORWARD_LIT_PASS_INCLUDED)
Varyings SlicedLitPassVertex(SlicedAttributes input)
{
    return LitPassVertex(ToSlicedAttributes(input));
}
#endif

#if defined(UNIVERSAL_LIT_GBUFFER_PASS_INCLUDED)
Varyings SlicedLitGBufferPassVertex(SlicedAttributes input)
{
    return LitGBufferPassVertex(ToSlicedAttributes(input));
}
#endif

#if defined(UNIVERSAL_SHADOW_CASTER_PASS_INCLUDED)
struct SlicedAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 texcoord : TEXCOORD0;
    SLICE_ATTRIBUTES
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings SlicedShadowPassVertex(SlicedAttributes input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    Attributes output = (Attributes)0;
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    output.positionOS = float4(SlicePosition(input.positionOS.xyz, input.sliceOffset), input.positionOS.w);
    output.normalOS = input.normalOS / SliceStretch(input.sliceSlope);
    output.texcoord = input.texcoord;
    return ShadowPassVertex(output);
}
#endif

#if defined(UNIVERSAL_DEPTH_ONLY_PASS_INCLUDED)
struct SlicedAttributes
{
    float4 position : POSITION;
    float2 texcoord : TEXCOORD0;
    SLICE_ATTRIBUTES
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings SlicedDepthOnlyVertex(SlicedAttributes input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    Attributes output = (Attributes)0;
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    output.position = float4(SlicePosition(input.position.xyz, input.sliceOffset), input.position.w);
    output.texcoord = input.texcoord;
    return DepthOnlyVertex(output);
}
#endif

#if defined(UNIVERSAL_FORWARD_LIT_DEPTH_NORMALS_PASS_INCLUDED)
struct SlicedAttributes
{
    float4 positionOS : POSITION;
    float4 tangentOS : TANGENT;
    float2 texcoord : TEXCOORD0;
    float3 normal : NORMAL;
    SLICE_ATTRIBUTES
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings SlicedDepthNormalsVertex(SlicedAttributes input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    Attributes output = (Attributes)0;
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 stretch = SliceStretch(input.sliceSlope);
    output.positionOS = float4(SlicePosition(input.positionOS.xyz, input.sliceOffset), input.positionOS.w);
    output.tangentOS = float4(input.tangentOS.xyz * stretch, input.tangentOS.w);
    output.normal = input.normal / stretch;
    output.texcoord = input.texcoord;
    return DepthNormalsVertex(output);
}
#endif

#endif
