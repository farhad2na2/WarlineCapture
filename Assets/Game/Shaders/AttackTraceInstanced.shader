Shader "Game/AttackTraceInstanced"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 0.8, 0.25, 1)
        _CoreIntensity("Core Intensity (HDR)", Range(1, 8)) = 4
        _GlowIntensity("Glow Intensity (HDR)", Range(0.5, 6)) = 1.6
        _TailExponent("Tail Falloff", Range(1, 12)) = 5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            // Premultiplied edge pigment remains legible over sand; the core is emissive.
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR0;
                float4 traceParams : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float _CoreIntensity;
                float _GlowIntensity;
                float _TailExponent;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _TraceColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _TraceParams)
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);

                Varyings output;
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positionInputs.positionCS;
                output.uv = input.uv;
                output.color = UNITY_ACCESS_INSTANCED_PROP(Props, _TraceColor);
                output.traceParams = UNITY_ACCESS_INSTANCED_PROP(Props, _TraceParams);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float density = input.traceParams.x;
                float travel = input.traceParams.y;
                float span = input.traceParams.z;
                float coordinate = input.uv.y * density;
                float head = lerp(span * 0.8, 1.0, travel);
                float behind = density <= 1.0 ? head - coordinate : frac(travel - coordinate);
                float t = behind / span; // nose = 0, tail = 1
                float side = abs(input.uv.x * 2.0 - 1.0);
                float width = lerp(0.92, 0.045, saturate(t));
                float nose = smoothstep(-0.19, 0.035, t);
                float body = (1.0 - smoothstep(0.72, 1.05, t)) * nose;
                float edge = (1.0 - smoothstep(width * 0.50, width, side)) * body;
                float core = (1.0 - smoothstep(width * 0.08, width * 0.48, side)) * body;
                half3 rim = input.color.rgb * half3(0.72, 0.46, 0.32);
                half3 hot = lerp(input.color.rgb, half3(1, 0.97, 0.82), 0.8);
                half alpha = saturate(edge * input.color.a);
                half3 rgb = rim * alpha + hot * core * (_CoreIntensity * 0.72);
                // Rocket-style direct fire gets a short, scalloped smoke wake.
                float puff = 0.72 + 0.23 * cos(t * 27.0);
                float wake = input.traceParams.w * smoothstep(0.35, 0.7, t) *
                    (1.0 - smoothstep(1.45, 2.35, t)) * (1.0 - smoothstep(0.15, puff, side));
                half smokeAlpha = wake * 0.65 * (1.0 - alpha);
                rgb += lerp(half3(0.31,0.28,0.24), half3(0.77,0.70,0.58), 1.0-side) * smokeAlpha;
                return half4(rgb, saturate(alpha + smokeAlpha));
            }
            ENDHLSL
        }
    }
}
