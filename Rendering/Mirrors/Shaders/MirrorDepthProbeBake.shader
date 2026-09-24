Shader "Hidden/EDIVE/MirrorDepthProbeBake"
{
    SubShader
    {
        ZTest Always ZWrite Off Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct BakeAttributes
        {
            float4 positionOS : POSITION;
            float2 uv         : TEXCOORD0;
        };

        struct BakeVaryings
        {
            float4 positionCS : SV_POSITION;
            float2 uv         : TEXCOORD0;
        };

        BakeVaryings BakeVertex(BakeAttributes input)
        {
            BakeVaryings output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.uv = input.uv;
            return output;
        }
        ENDHLSL

        // One face. Distance from the capture point.
        Pass
        {
            Name "Distance"

            HLSLPROGRAM
            #pragma vertex BakeVertex
            #pragma fragment DistanceFragment

            TEXTURE2D_FLOAT(_BakeDepth);
            float4 _BakeZParams;
            float _BakeFar;
            float _BakeEmpty;

            float4 DistanceFragment(BakeVaryings input) : SV_Target
            {
                float depth = SAMPLE_TEXTURE2D_LOD(_BakeDepth, sampler_PointClamp, input.uv, 0).r;
                float eye = 1.0 / (_BakeZParams.z * depth + _BakeZParams.w);

                // 90 degree face, so the ray length is the eye depth over the cosine.
                float2 ndc = input.uv * 2.0 - 1.0;
                float distance = eye * length(float3(ndc, 1.0));

                if (eye >= _BakeFar * 0.999)
                    distance = _BakeEmpty;

                return float4(distance, 0.0, 0.0, 1.0);
            }
            ENDHLSL
        }
    }
}
