// =============================================================================
// GlassPanel.shader — Optimized frosted glass for Quest 2
// =============================================================================
// This shader approximates frosted translucent glass without real-time blur.
// Uses gradient, edge highlight, subtle noise, and transparency.
// Designed for single-pass instanced rendering on Quest 2 URP.

Shader "VisionSpace/GlassPanel"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.08, 0.08, 0.12, 0.85)
        _EdgeColor ("Edge Highlight", Color) = (0.3, 0.5, 0.8, 0.2)
        _EdgeWidth ("Edge Width", Range(0, 0.1)) = 0.02
        _GradientStrength ("Gradient Strength", Range(0, 1)) = 0.15
        _GradientDirection ("Gradient Direction", Vector) = (0, 1, 0, 0)
        _NoiseScale ("Noise Scale", Range(0, 100)) = 40
        _NoiseStrength ("Noise Strength", Range(0, 0.1)) = 0.02
        _Glossiness ("Glossiness", Range(0, 1)) = 0.7
        _FresnelPower ("Fresnel Power", Range(0, 5)) = 2.5
        _FresnelStrength ("Fresnel Strength", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "GlassPanel"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EdgeColor;
                float _EdgeWidth;
                float _GradientStrength;
                float4 _GradientDirection;
                float _NoiseScale;
                float _NoiseStrength;
                float _Glossiness;
                float _FresnelPower;
                float _FresnelStrength;
            CBUFFER_END

            // Simple hash-based noise (no texture needed)
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // smoothstep

                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Base color
                half4 col = _BaseColor;

                // Gradient (top-to-bottom subtle brightening)
                float gradientFactor = dot(input.uv - 0.5, _GradientDirection.xy) + 0.5;
                col.rgb += _GradientStrength * gradientFactor * 0.5;

                // Subtle procedural noise (frost texture approximation)
                float n = noise(input.uv * _NoiseScale);
                col.rgb += (n - 0.5) * _NoiseStrength;

                // Edge highlight (border glow)
                float2 edgeDist = min(input.uv, 1.0 - input.uv);
                float edgeFactor = 1.0 - smoothstep(0, _EdgeWidth, min(edgeDist.x, edgeDist.y));
                col.rgb = lerp(col.rgb, _EdgeColor.rgb, edgeFactor * _EdgeColor.a);

                // Fresnel (edge-on reflection approximation)
                float fresnel = pow(1.0 - saturate(dot(input.normalWS, input.viewDirWS)), _FresnelPower);
                col.rgb += fresnel * _FresnelStrength;
                col.a += fresnel * _FresnelStrength * 0.3;

                col.a = saturate(col.a);

                return col;
            }
            ENDHLSL
        }
    }

    // Fallback for non-URP (built-in render pipeline)
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 normal : TEXCOORD1; float3 viewDir : TEXCOORD2; };

            float4 _BaseColor;
            float4 _EdgeColor;
            float _EdgeWidth;
            float _FresnelPower;
            float _FresnelStrength;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = _BaseColor;
                float2 edgeDist = min(i.uv, 1.0 - i.uv);
                float edge = 1.0 - smoothstep(0, _EdgeWidth, min(edgeDist.x, edgeDist.y));
                col.rgb = lerp(col.rgb, _EdgeColor.rgb, edge * _EdgeColor.a);
                float fresnel = pow(1.0 - saturate(dot(i.normal, i.viewDir)), _FresnelPower);
                col.rgb += fresnel * _FresnelStrength;
                return col;
            }
            ENDCG
        }
    }
}
