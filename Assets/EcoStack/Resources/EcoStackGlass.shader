Shader "EcoStack/Glass"
{
 Properties { _Color("Color",Color)=(0.4,0.85,0.95,0.10) }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent"}
 Pass {
 Tags {"LightMode"="SRPDefaultUnlit"}
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off
 Cull Off
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;};
 struct v2f {float4 vertex:SV_POSITION;};
 float4 _Color;
 v2f vert(appdata v) {v2f o;o.vertex=UnityObjectToClipPos(v.vertex);return o;}
 fixed4 frag(v2f i):SV_Target {return _Color;}
 ENDCG
 }
 }
}
