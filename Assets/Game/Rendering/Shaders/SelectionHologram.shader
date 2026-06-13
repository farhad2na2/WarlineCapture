Shader "WarlineCapture/Markers/SelectionHologram"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.16, 0.85, 1.0, 1.0)
        _Color ("Legacy Color", Color) = (0.16, 0.85, 1.0, 1.0)
        _EmissionColor ("Emission Color", Color) = (0.16, 0.85, 1.0, 1.0)
        _AccentColor ("Accent Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Alpha ("Alpha", Range(0, 1)) = 0.72
        _PulseStrength ("Pulse Strength", Range(0, 2)) = 0.25
        _PulseSpeed ("Pulse Speed", Float) = 2.2
        _ScanStrength ("Scan Strength", Range(0, 2)) = 0.35
        _ScanSpeed ("Scan Speed", Float) = 0.45
        _EdgeSoftness ("Edge Softness", Range(0.001, 1)) = 0.22
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "SelectionHologram"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest LEqual
            Offset -1, -1

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _Color;
                float4 _EmissionColor;
                float4 _AccentColor;
                float _Alpha;
                float _PulseStrength;
                float _PulseSpeed;
                float _ScanStrength;
                float _ScanSpeed;
                float _EdgeSoftness;
            CBUFFER_END

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
            UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DOTS_INSTANCED_PROP(float4, _Color)
                UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)
                UNITY_DOTS_INSTANCED_PROP(float4, _AccentColor)
                UNITY_DOTS_INSTANCED_PROP(float, _Alpha)
                UNITY_DOTS_INSTANCED_PROP(float, _PulseStrength)
                UNITY_DOTS_INSTANCED_PROP(float, _PulseSpeed)
                UNITY_DOTS_INSTANCED_PROP(float, _ScanStrength)
                UNITY_DOTS_INSTANCED_PROP(float, _ScanSpeed)
                UNITY_DOTS_INSTANCED_PROP(float, _EdgeSoftness)
            UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

            static float4 unity_DOTS_Sampled_BaseColor;
            static float4 unity_DOTS_Sampled_Color;
            static float4 unity_DOTS_Sampled_EmissionColor;
            static float4 unity_DOTS_Sampled_AccentColor;
            static float unity_DOTS_Sampled_Alpha;
            static float unity_DOTS_Sampled_PulseStrength;
            static float unity_DOTS_Sampled_PulseSpeed;
            static float unity_DOTS_Sampled_ScanStrength;
            static float unity_DOTS_Sampled_ScanSpeed;
            static float unity_DOTS_Sampled_EdgeSoftness;

            void SetupDOTSSelectionHologramMaterialPropertyCaches()
            {
                unity_DOTS_Sampled_BaseColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor);
                unity_DOTS_Sampled_Color = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _Color);
                unity_DOTS_Sampled_EmissionColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _EmissionColor);
                unity_DOTS_Sampled_AccentColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _AccentColor);
                unity_DOTS_Sampled_Alpha = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Alpha);
                unity_DOTS_Sampled_PulseStrength = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _PulseStrength);
                unity_DOTS_Sampled_PulseSpeed = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _PulseSpeed);
                unity_DOTS_Sampled_ScanStrength = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _ScanStrength);
                unity_DOTS_Sampled_ScanSpeed = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _ScanSpeed);
                unity_DOTS_Sampled_EdgeSoftness = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _EdgeSoftness);
            }

            #undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
            #define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSSelectionHologramMaterialPropertyCaches()

            #undef _BaseColor
            #define _BaseColor unity_DOTS_Sampled_BaseColor
            #undef _Color
            #define _Color unity_DOTS_Sampled_Color
            #undef _EmissionColor
            #define _EmissionColor unity_DOTS_Sampled_EmissionColor
            #undef _AccentColor
            #define _AccentColor unity_DOTS_Sampled_AccentColor
            #undef _Alpha
            #define _Alpha unity_DOTS_Sampled_Alpha
            #undef _PulseStrength
            #define _PulseStrength unity_DOTS_Sampled_PulseStrength
            #undef _PulseSpeed
            #define _PulseSpeed unity_DOTS_Sampled_PulseSpeed
            #undef _ScanStrength
            #define _ScanStrength unity_DOTS_Sampled_ScanStrength
            #undef _ScanSpeed
            #define _ScanSpeed unity_DOTS_Sampled_ScanSpeed
            #undef _EdgeSoftness
            #define _EdgeSoftness unity_DOTS_Sampled_EdgeSoftness
            #endif

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv = saturate(input.uv);
                float2 centered = abs(uv - 0.5) * 2.0;
                float edge = max(centered.x, centered.y);
                float edgeFade = 1.0 - smoothstep(1.0 - _EdgeSoftness, 1.0, edge);

                float pulseWave = 0.5 + 0.5 * sin(_Time.y * _PulseSpeed);
                float pulse = 1.0 + pulseWave * _PulseStrength;

                float scanPhase = frac(uv.y + _Time.y * _ScanSpeed);
                float scanLine = pow(saturate(1.0 - abs(scanPhase - 0.5) * 8.0), 2.0) * _ScanStrength;

                float4 baseColor = _BaseColor * input.color;
                float3 emissive = _EmissionColor.rgb * pulse;
                float3 accent = _AccentColor.rgb * scanLine;
                float alpha = saturate(baseColor.a * _Alpha * edgeFade + scanLine * 0.18);

                return half4(baseColor.rgb + emissive + accent, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
