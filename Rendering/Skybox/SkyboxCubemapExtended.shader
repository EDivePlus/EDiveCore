// Based on https://assetstore.unity.com/packages/vfx/shaders/free-skybox-extended-shader-107400
// Slightly modified and cleaned up

Shader "Skybox/Cubemap Extended"
{
    Properties
    {
        [Header(Cubemap Settings)][Space]
        [NoScaleOffset] _MainTex("Cubemap (HDR)", Cube) = "black" {}
        _Exposure("Exposure", Range(0, 8)) = 1
        [Gamma] _Tint("Tint Color", Color) = (0.5, 0.5, 0.5, 1)
        _MainTexVerticalOffset("Cubemap Vertical Offset", Float) = 0

        [Header(Rotation Settings)][Space]
        [Toggle(_ENABLEROTATION_ON)] _EnableRotation("Enable Rotation", Float) = 0
        _RotationDegrees("Rotation Degrees", Range(0, 360)) = 0
        _RotationSpeed("Rotation Speed", Float) = 1

        [Header(Fog Settings)][Space]
        [Toggle(_ENABLEFOG_ON)] _EnableFog("Enable Fog", Float) = 0
        _FogIntensity("Fog Intensity", Range(0, 1)) = 1
        _FogHeight("Fog Height", Range(0, 1)) = 1
        _FogSmoothness("Fog Smoothness", Range(0.01, 1)) = 0.01
        _FogFill("Fog Fill", Range(0, 1)) = 0.5
        _FogVerticalOffset("Fog Vertical Offset", Float) = 0

        // Internal decode params for HDR cubemap sampling (hidden)
        [HideInInspector] _MainTex_HDR("DecodeInstructions", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "PreviewType"="Skybox" }
        LOD 0

        CGINCLUDE
        #pragma target 2.0
        ENDCG

        Blend Off
        AlphaToMask Off
        Cull Off
        ColorMask RGBA
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "Unlit"

            CGPROGRAM

            #ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
            #define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
            #endif

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "UnityShaderVariables.cginc"
            #pragma shader_feature_local _ENABLEFOG_ON
            #pragma shader_feature_local _ENABLEROTATION_ON

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 dirWS  : TEXCOORD0;
                float4 posOS  : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Textures & decode
            samplerCUBE _MainTex;
            half4 _MainTex_HDR;   // HDR decode parameters

            // Color/exposure
            half4 _Tint;
            half  _Exposure;

            // Placement
            float _MainTexVerticalOffset;

            // Rotation
            half  _RotationDegrees;
            half  _RotationSpeed;

            // Fog
            half  _FogIntensity;
            half  _FogHeight;
            half  _FogSmoothness;
            half  _FogFill;
            float _FogVerticalOffset;

            inline half3 DecodeHDR_Cubemap(float4 data)
            {
                return DecodeHDR(data, _MainTex_HDR);
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                // Aspect correction for ortho
                float orthoAspect = lerp(1.0, (unity_OrthoParams.y / unity_OrthoParams.x), unity_OrthoParams.w);
                float3 pos = float3(v.vertex.xyz.x, v.vertex.xyz.y * orthoAspect, v.vertex.xyz.z);

                // Vertical offset
                float3 skyOffset = float3(0.0, -_MainTexVerticalOffset, 0.0);

                // Rotation around Y
                float angleRad = radians(_RotationDegrees + (_Time.y * _RotationSpeed));
                float3 axisY = float3(0, 1, 0);
                float3 axisPart = float3(pos.x, 0.0, pos.z);
                float3 yPart    = float3(0.0, pos.y, 0.0);

                #ifdef _ENABLEROTATION_ON
                    float c = cos(angleRad);
                    float s = sin(angleRad);
                    float3 rotated = yPart + axisPart * c + cross(axisY, axisPart) * s;
                    o.dirWS = rotated + skyOffset;
                #else
                    o.dirWS = pos + skyOffset;
                #endif

                o.posOS = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Sample and decode HDR cubemap
                half4 cubeSample = texCUBE(_MainTex, i.dirWS);
                half3 cubeRGB    = DecodeHDR_Cubemap(cubeSample);
                half4 skyRGBA    = float4(cubeRGB, 0.0) * unity_ColorSpaceDouble * _Tint * _Exposure;

                // Height-based fog mask
                float y = i.posOS.y - _FogVerticalOffset;
                float h = max(_FogHeight, 1e-6);
                float t = saturate(pow(abs(y) / h, (1.0 - _FogSmoothness)));
                float fogMask = lerp(t, 0.0, _FogFill);
                fogMask = lerp(1.0, fogMask, _FogIntensity);

                // Blend with fog color if enabled
                float4 colorWithFog = lerp(unity_FogColor, skyRGBA, fogMask);
                #ifdef _ENABLEFOG_ON
                    return colorWithFog;
                #else
                    return skyRGBA;
                #endif
            }
            ENDCG
        }
    }

    Fallback "Skybox/Cubemap"
}
