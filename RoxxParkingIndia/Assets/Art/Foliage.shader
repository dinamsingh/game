Shader "Roxx/Foliage"
{
    Properties
    {
        _MainTex ("Leaf colour", 2D) = "white" {}
        _OpacityMap ("Leaf mask", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Cutout", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Off
        AlphaToMask On
        CGPROGRAM
        #pragma surface surf Lambert alphatest:_Cutoff addshadow fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex, _OpacityMap;
        fixed4 _Color;
        struct Input { float2 uv_MainTex; float4 color : COLOR; float facing : VFACE; };
        void surf(Input input, inout SurfaceOutput output)
        {
            output.Albedo = tex2D(_MainTex, input.uv_MainTex).rgb * _Color.rgb * input.color.rgb;
            output.Alpha = tex2D(_OpacityMap, input.uv_MainTex).r;
            output.Normal = float3(0, 0, input.facing > 0 ? 1 : -1);
        }
        ENDCG
    }
    Fallback "Transparent/Cutout/Diffuse"
}
