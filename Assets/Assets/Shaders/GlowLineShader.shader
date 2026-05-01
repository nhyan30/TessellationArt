Shader "Custom/GlowLineMeshShader"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (0, 1, 0.85, 1)
        _DepthColor ("Depth Color", Color) = (0, 0.35, 0.3, 1)
        _Brightness ("Brightness", Range(1, 10)) = 3
        _AnimSpeed ("Animation Speed", Range(0, 10)) = 1.5
        _AnimIntensity ("Animation Intensity", Range(0, 0.5)) = 0.12
    }

    SubShader
    {
        Tags { "Queue"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // 👈 fade per vertex
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float fade : TEXCOORD1;
            };

            float4 _MainColor;
            float4 _DepthColor;
            float _Brightness;
            float _AnimSpeed;
            float _AnimIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.fade = v.color.r; // 👈 pass fade
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float edge = 1.0 - abs(i.uv.y * 2.0 - 1.0);
                edge = pow(saturate(edge), 0.6);

                float pulse = sin(_Time.y * _AnimSpeed);
                float anim = 1.0 + pulse * _AnimIntensity;

                float4 col = lerp(_DepthColor, _MainColor, i.fade) * _Brightness * anim;
                col.a = i.fade * edge * anim;

                return col;
            }
            ENDCG
        }
    }
}