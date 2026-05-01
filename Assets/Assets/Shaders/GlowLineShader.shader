Shader "Custom/GlowLineShader"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (0, 1, 0.85, 1)
        _DepthColor ("Depth Color", Color) = (0, 0.35, 0.3, 1)
        _Fade ("Fade", Range(0, 1)) = 1
        _Brightness ("Brightness", Range(1, 10)) = 3
        _AnimSpeed ("Animation Speed", Range(0, 10)) = 1.5
        _AnimIntensity ("Animation Intensity", Range(0, 0.5)) = 0.12
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha One   // Additive blending → neon glow on dark backgrounds
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
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _MainColor;
            float4 _DepthColor;
            float  _Fade;
            float  _Brightness;
            float  _AnimSpeed;
            float  _AnimIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Soft edge across line width (v = 0→1, center = 0.5)
                float edge = 1.0 - abs(i.uv.y * 2.0 - 1.0);
                edge = pow(saturate(edge), 0.6);

                // --- SLIGHT ANIMATION ---
                // Oscillates between -1 and 1 over time
                float pulse = sin(_Time.y * _AnimSpeed);
                // Map to a subtle brightness/alpha multiplier
                float animMult = 1.0 + (pulse * _AnimIntensity);

                // Blend between depth colour and main colour based on fade
                float4 col = lerp(_DepthColor, _MainColor, _Fade) * _Brightness * animMult;
                col.a = _Fade * edge * animMult;

                return col;
            }
            ENDCG
        }
    }
}