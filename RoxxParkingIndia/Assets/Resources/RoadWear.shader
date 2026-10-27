Shader "Roxx/RoadWear"
{
    Properties { _Tint("Stain multiplier",Color)=(.65,.62,.58,.65) }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" }
        Blend DstColor Zero ZWrite Off Offset -1,-1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Tint;
            struct input { float4 vertex:POSITION;float2 uv:TEXCOORD0; };
            struct output { float4 vertex:SV_POSITION;float2 uv:TEXCOORD0; };
            output vert(input v){output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
            fixed4 frag(output i):SV_Target
            {
                float2 p=i.uv*2-1;
                float grain=noise(i.uv*7)*.65+noise(i.uv*23)*.35;
                float alpha=saturate((1-dot(p,p))*(grain+.2)*2)*_Tint.a;
                return float4(lerp(float3(1,1,1),_Tint.rgb,alpha),1);
            }
            ENDCG
        }
    }
}
