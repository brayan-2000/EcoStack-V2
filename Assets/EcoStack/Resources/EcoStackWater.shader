Shader "EcoStack/Water"
{
 Properties {_Color("Color",Color)=(0.04,0.62,0.71,1)}
 SubShader {
 Tags {"RenderType"="Opaque"}
 Pass {
 Tags {"LightMode"="SRPDefaultUnlit"}
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;};
 struct v2f {float4 vertex:SV_POSITION;float3 world:TEXCOORD0;};
 float4 _Color;
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
 fixed4 frag(v2f i):SV_Target{
 float waves=sin(i.world.x*3+_Time.y*1.3)*sin(i.world.z*2-_Time.y);
 float foam=pow(saturate(waves),12);
 return float4(_Color.rgb*(0.85+waves*.15)+float3(.45,.65,.65)*foam,1);
 }
 ENDCG
 }
 }
}
