Shader "EDIVE/Vignette"
{
    Properties
    {
        _ApertureSize("Aperture Size", Range(-1, 1)) = 0.7
        _FeatheringEffect("Feathering Effect", Range(0, 1)) = 0.2
        _Color("Color", Color) = (1, 1, 1, 1)
        _Gradient("Gradient Ramp", 2D) = "white" {}
        _Alpha("Alpha", Range(0, 1)) = 1
        _CloseEase("Close Ease", Range(0, 1)) = 0.7
        _VerticalOffset("Vertical Offset", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+5" "IgnoreProjector" = "True" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest Always
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_Gradient);
            SAMPLER(sampler_Gradient);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _ApertureSize;
                float _FeatheringEffect;
                float _Alpha;
                float _CloseEase;
                float _VerticalOffset;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS = input.positionOS.xyz;
                positionOS.y += _VerticalOffset;
                output.positionCS = TransformObjectToHClip(positionOS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float uvY = input.uv.y;
                float aperture = _ApertureSize;
                float alphaMin = lerp(aperture, aperture * aperture * aperture, _CloseEase); // linear<->cubic ease-in blend; odd (no mirror), smooth (no jump), detail near 1; negative seals to full black
                float alpha = saturate((uvY - alphaMin) / (_FeatheringEffect * _FeatheringEffect + 0.0001));
                half4 color = _Color * SAMPLE_TEXTURE2D(_Gradient, sampler_Gradient, float2(saturate(uvY), 0.5));
                color.a *= alpha * _Alpha;
                return color;
            }
            ENDHLSL
        }
    }
}
