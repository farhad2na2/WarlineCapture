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

            // Additive: tracers add light into the scene instead of painting flat color.
            // Brightness is carried entirely in RGB (premultiplied), so Blend One One.
            Blend One One
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
                float2 traceParams : TEXCOORD1;
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
                output.traceParams = UNITY_ACCESS_INSTANCED_PROP(Props, _TraceParams).xy;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float dashDensity = max(1.0, input.traceParams.x);
                float scrollOffset = input.traceParams.y;

                // 0..1 within each tracer segment. The bright head is at phase ~1,
                // the tail fades behind it (toward phase 0).
                float dashPhase = frac(input.uv.y * dashDensity - scrollOffset);

                // Bullet-shaped streak: long faint tail rising steeply to a hot head,
                // with a quick rounded falloff at the very tip so it doesn't hard-clip.
                float tail = pow(dashPhase, _TailExponent);
                float tip = 1.0 - smoothstep(0.97, 1.0, dashPhase);
                float streak = tail * tip;

                // Cross-section: white-hot core in the middle, colored glow falling
                // off toward the quad edges. The streak also tapers: the tail is
                // thinner than the head.
                float side = abs(input.uv.x - 0.5) * 2.0;
                float taper = lerp(2.2, 1.0, streak); // tail effectively narrower
                float s = saturate(side * taper);
                float core = 1.0 - smoothstep(0.0, 0.30, s);
                float glow = 1.0 - smoothstep(0.0, 1.0, s);
                glow *= glow; // softer falloff

                // Fade the whole trace in at the muzzle and out at the target so the
                // line never hard-clips at either end.
                float endFade = smoothstep(0.0, 0.07, input.uv.y) * (1.0 - smoothstep(0.90, 1.0, input.uv.y));

                half3 traceRgb = input.color.rgb;
                // Hot core trends toward white regardless of trace color.
                half3 coreRgb = lerp(traceRgb, half3(1.0, 1.0, 1.0), 0.75) * _CoreIntensity;
                half3 glowRgb = traceRgb * _GlowIntensity;

                half3 rgb = (coreRgb * core + glowRgb * glow) * streak * endFade * input.color.a;
                return half4(rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
