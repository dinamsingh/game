Shader "Roxx/VisualFinish"
{
    Properties { _MainTex("Scene",2D)="white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex,_CameraDepthNormalsTexture,_OcclusionTex;
        float4 _MainTex_TexelSize;
        float _Radius,_ContactStrength;
        float3 viewPosition(float2 uv,out float3 normal,out float depth)
        {
            DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture,uv),depth,normal);
            float z=depth*_ProjectionParams.z;
            return float3((uv*2-1)*float2(1/unity_CameraProjection._m00,1/unity_CameraProjection._m11)*z,-z);
        }
        fixed4 contact(v2f_img i):SV_Target
        {
            float3 n;float depth;float3 p=viewPosition(i.uv,n,depth);
            if(depth>.995)return 1;
            float2 radius=_Radius*float2(unity_CameraProjection._m00,unity_CameraProjection._m11)/max(.5,-p.z)*.5;
            float result=0;
            for(int k=0;k<8;k++)
            {
                float angle=k*2.3999632;
                float2 offset=float2(cos(angle),sin(angle))*radius*(.3+.09*k);
                float3 qn;float qd;float3 q=viewPosition(saturate(i.uv+offset),qn,qd);
                float3 delta=q-p;float d=length(delta);
                result+=max(0,dot(n,delta/max(d,.001))-.1)*saturate(1-d/(_Radius*2.2));
            }
            return 1-saturate(result*.3)*_ContactStrength;
        }
        half4 grade(v2f_img i):SV_Target
        {
            half3 colour=tex2D(_MainTex,i.uv).rgb;
            float2 t=_MainTex_TexelSize.xy;
            half ao=(tex2D(_OcclusionTex,i.uv+float2(t.x,t.y)).r+tex2D(_OcclusionTex,i.uv-float2(t.x,t.y)).r+tex2D(_OcclusionTex,i.uv+float2(t.x,-t.y)).r+tex2D(_OcclusionTex,i.uv+float2(-t.x,t.y)).r)*.25;
            colour*=ao;
            // Gentle filmic shoulder retains paint/sky highlights instead of clipping to white.
            colour*=1.08;
            colour=saturate((colour*(2.51*colour+.03))/(colour*(2.43*colour+.59)+.14));
            half luma=dot(colour,half3(.2126,.7152,.0722));
            colour=lerp(luma.xxx,colour,.96);
            float2 edge=i.uv*(1-i.uv);colour*=lerp(.96,1,saturate(edge.x*edge.y*20));
            return half4(colour,1);
        }
        ENDCG
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment contact
            #pragma target 3.0
        ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment grade
            #pragma target 3.0
        ENDCG }
    }
    Fallback Off
}
