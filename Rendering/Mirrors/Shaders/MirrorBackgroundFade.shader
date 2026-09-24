Shader "Hidden/EDIVE/MirrorBackgroundFade"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off

        // Scales the reflection alpha, colour stays.
        Blend Zero One, Zero SrcAlpha
        ColorMask A

        Pass
        {
            Name "Fade"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FadeFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _MirrorFadeLength;
            float4 _MirrorCullPlane;

            // _BlitTexture is the camera depth.
            half4 FadeFragment(Varyings input) : SV_Target
            {
                float depth = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, input.texcoord, 0).r;
                float4 positionWS = float4(ComputeWorldSpacePosition(input.texcoord, depth, UNITY_MATRIX_I_VP), 1.0);

                // The oblique projection tilts the far plane, so measure to the one that actually clips.
                float4x4 viewProjection = UNITY_MATRIX_VP;
#if UNITY_REVERSED_Z
                float4 farPlane = viewProjection[2];
#else
                float4 farPlane = viewProjection[3] - viewProjection[2];
#endif
                float toFar = dot(farPlane, positionWS) / length(farPlane.xyz);

                // Whole objects are culled past this one.
                float toCull = dot(_MirrorCullPlane, positionWS);

                return half4(0, 0, 0, saturate(min(toFar, toCull) / _MirrorFadeLength));
            }
            ENDHLSL
        }
    }
}
