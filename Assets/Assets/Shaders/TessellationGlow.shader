Shader "Custom/TessellationGlow"
{
Properties
{
_Color ("Line Color", Color) = (1,1,1,1)
_GlowColor ("Glow Color", Color) = (0.6,0.8,1,0.3)
_GlowWidth ("Glow Width", Range(0, 0.01)) = 0.003
}
SubShader
{
Tags {
"RenderType"="Transparent"
"Queue"="Transparent"
}
Blend SrcAlpha OneMinusSrcAlpha
ZWrite Off
Cull Off
// Pass 1: Glow halo (thicker, transparent)
Pass
{
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct appdata
{
float4 vertex : POSITION;
};
struct v2f
{
float4 pos : SV_POSITION;
};
float4 _GlowColor;
float _GlowWidth;
v2f vert(appdata v)
{
v2f o;
// Slight outward offset for glow
float3 dir = normalize(v.vertex.xyz);
v.vertex.xyz += dir * _GlowWidth;
o.pos = UnityObjectToClipPos(v.vertex);
return o;
}
fixed4 frag(v2f i) : SV_Target
{
return _GlowColor;
}
ENDCG
}
// Pass 2: Core line (thin, bright)Pass
Pass
{
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct appdata
{
float4 vertex : POSITION;
};
struct v2f
{
float4 pos : SV_POSITION;
};
float4 _Color;
v2f vert(appdata v)
{
v2f o;
o.pos = UnityObjectToClipPos(v.vertex);
return o;
}
fixed4 frag(v2f i) : SV_Target
{
return _Color;
}
ENDCG
}
}
Fallback "Unlit/Color"
}