// =============================================================================
// RoundedRect.shader — Rounded rectangle mask for icons
// =============================================================================
Shader "VisionSpace/RoundedRect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Radius ("Corner Radius", Range(0, 0.5)) = 0.15
        _Smoothness ("Edge Smoothness", Range(0, 0.05)) = 0.01
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            float4 _Color;
            float _Radius;
            float _Smoothness;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float roundedRectSDF(float2 p, float2 b, float r)
            {
                float2 d = abs(p) - b + r;
                return min(max(d.x, d.y), 0.0) + length(max(d, 0.0)) - r;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;

                float2 uv = i.uv - 0.5;
                float dist = roundedRectSDF(uv, float2(0.5, 0.5), _Radius);
                float alpha = 1.0 - smoothstep(-_Smoothness, _Smoothness, dist);

                col.a *= alpha;
                return col;
            }
            ENDCG
        }
    }
}
