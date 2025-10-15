Shader "Custom/BraceletHP"
{
    Properties
    {
        _Color1 ("HP Rest Color", Color) = (1, 0, 0, 1)
        _Color2 ("HP Bar Color", Color) = (0, 0, 1, 1)
        _EmissionStrength ("Emission Strength", Range(0, 1)) = 1
        _Opacity ("Opacity", Range(0, 1)) = 0.5
        _BlendPos ("Blend Position (0-1)", Range(0, 1)) = 0.5
        
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.0

        //Only used for passing UV
        [HideInInspector] _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows alpha

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
        };

        fixed4 _Color1;
        fixed4 _Color2;
        float _EmissionStrength;
        float _Opacity;
        float _BlendPos;

        half _Glossiness;
        half _Metallic;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            bool isColor2 = IN.uv_MainTex.x < _BlendPos;
            o.Albedo = isColor2 ? _Color1.rgb : _Color2.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = _Opacity;
            o.Emission = o.Albedo * 0.1;
            o.Emission = isColor2 ? _Color2.rgb * _EmissionStrength : float3(0,0,0);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
