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
        Tags { "Queue" = "Transparent+5" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest Always
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            sampler2D _Gradient;
            float _ApertureSize;
            float _FeatheringEffect;
            float _Alpha;
            float _CloseEase;
            float _VerticalOffset;

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 vertex = v.vertex;
                vertex.y += _VerticalOffset;
                o.vertex = UnityObjectToClipPos(vertex);
                o.uv = v.uv;
                return o;
            }

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float uvY = i.uv.y;
                float aperture = _ApertureSize;
                float alphaMin = lerp(aperture, aperture * aperture * aperture, _CloseEase); // linear<->cubic ease-in blend; odd (no mirror), smooth (no jump), detail near 1; negative seals to full black
                float alpha = saturate(((uvY - alphaMin) / (_FeatheringEffect * _FeatheringEffect + 0.0001)));
                fixed4 color = _Color * tex2D(_Gradient, float2(saturate(uvY), 0.5));
                color.w *= alpha * _Alpha;

                return color;
            }
            ENDCG
        }
    }
}
