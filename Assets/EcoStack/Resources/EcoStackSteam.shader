Shader "EcoStack/Steam"
{
 Properties {_Color("Color",Color)=(0.7,0.75,0.78,0.2)}
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"}
 Pass {Tags {"LightMode"="SRPDefaultUnlit"}
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off
 Cull Off
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 float4 _Color;
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
 fixed4 frag(v2f i):SV_Target {float r=length(i.uv-.5)*2;float alpha=pow(saturate(1-r),2);return float4(_Color.rgb*i.color.rgb,alpha*_Color.a*i.color.a);}
 ENDCG
 }}
}
