Shader "WarlineCapture/Markers/SelectionObjectOutline"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.05, 0.88, 1.0, 0.95)
        _EmissionColor ("Emission Color", Color) = (0.05, 0.88, 1.0, 1.0)
        _OutlineWidth ("Outline Width", Range(0.001, 0.25)) = 0.035
        _OutlineAlpha ("Outline Alpha", Range(0, 1)) = 0.88
        _RimAlpha ("Rim Alpha", Range(0, 1)) = 0.34
        _RimPower ("Rim Power", Range(0.25, 8)) = 2.2
        _ScanStrength ("Scan Strength", Range(0, 1)) = 0.16
        _ScanSpeed ("Scan Speed", Float) = 0.28
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+5"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "SelectionExpandedHull"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Front
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
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float _OutlineWidth;
                float _OutlineAlpha;
                float _RimAlpha;
                float _RimPower;
                float _ScanStrength;
                float _ScanSpeed;
            CBUFFER_END

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
            UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)
                UNITY_DOTS_INSTANCED_PROP(float, _OutlineWidth)
                UNITY_DOTS_INSTANCED_PROP(float, _OutlineAlpha)
                UNITY_DOTS_INSTANCED_PROP(float, _RimAlpha)
                UNITY_DOTS_INSTANCED_PROP(float, _RimPower)
                UNITY_DOTS_INSTANCED_PROP(float, _ScanStrength)
                UNITY_DOTS_INSTANCED_PROP(float, _ScanSpeed)
            UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

            static float4 unity_DOTS_Sampled_BaseColor;
            static float4 unity_DOTS_Sampled_EmissionColor;
            static float unity_DOTS_Sampled_OutlineWidth;
            static float unity_DOTS_Sampled_OutlineAlpha;
            static float unity_DOTS_Sampled_RimAlpha;
            static float unity_DOTS_Sampled_RimPower;
            static float unity_DOTS_Sampled_ScanStrength;
            static float unity_DOTS_Sampled_ScanSpeed;

            void SetupDOTSSelectionObjectOutlineMaterialPropertyCaches()
            {
                unity_DOTS_Sampled_BaseColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor);
                unity_DOTS_Sampled_EmissionColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _EmissionColor);
                unity_DOTS_Sampled_OutlineWidth = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _OutlineWidth);
                unity_DOTS_Sampled_OutlineAlpha = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _OutlineAlpha);
                unity_DOTS_Sampled_RimAlpha = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _RimAlpha);
                unity_DOTS_Sampled_RimPower = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _RimPower);
                unity_DOTS_Sampled_ScanStrength = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _ScanStrength);
                unity_DOTS_Sampled_ScanSpeed = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _ScanSpeed);
            }

            #undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
            #define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSSelectionObjectOutlineMaterialPropertyCaches()

            #undef _BaseColor
            #define _BaseColor unity_DOTS_Sampled_BaseColor
            #undef _EmissionColor
            #define _EmissionColor unity_DOTS_Sampled_EmissionColor
            #undef _OutlineWidth
            #define _OutlineWidth unity_DOTS_Sampled_OutlineWidth
            #undef _OutlineAlpha
            #define _OutlineAlpha unity_DOTS_Sampled_OutlineAlpha
            #undef _RimAlpha
            #define _RimAlpha unity_DOTS_Sampled_RimAlpha
            #undef _RimPower
            #define _RimPower unity_DOTS_Sampled_RimPower
            #undef _ScanStrength
            #define _ScanStrength unity_DOTS_Sampled_ScanStrength
            #undef _ScanSpeed
            #define _ScanSpeed unity_DOTS_Sampled_ScanSpeed
            #endif

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS = input.positionOS.xyz + normalize(input.normalOS) * _OutlineWidth;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float scan = pow(saturate(1.0 - abs(frac(input.positionWS.y * 0.22 + _Time.y * _ScanSpeed) - 0.5) * 7.0), 2.0) * _ScanStrength;
                float3 color = _BaseColor.rgb + _EmissionColor.rgb * (0.7 + scan);
                return half4(color, saturate(_BaseColor.a * _OutlineAlpha + scan * 0.16));
            }
            ENDHLSL
        }

        Pass
        {
            Name "SelectionRim"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
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
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float _OutlineWidth;
                float _OutlineAlpha;
                float _RimAlpha;
                float _RimPower;
                float _ScanStrength;
                float _ScanSpeed;
            CBUFFER_END

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
            UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)
                UNITY_DOTS_INSTANCED_PROP(float, _OutlineWidth)
                UNITY_DOTS_INSTANCED_PROP(float, _OutlineAlpha)
                UNITY_DOTS_INSTANCED_PROP(float, _RimAlpha)
                UNITY_DOTS_INSTANCED_PROP(float, _RimPower)
                UNITY_DOTS_INSTANCED_PROP(float, _ScanStrength)
                UNITY_DOTS_INSTANCED_PROP(float, _ScanSpeed)
            UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

            static float4 unity_DOTS_Sampled_BaseColor;
            static float4 unity_DOTS_Sampled_EmissionColor;
            static float unity_DOTS_Sampled_OutlineWidth;
            static float unity_DOTS_Sampled_OutlineAlpha;
            static float unity_DOTS_Sampled_RimAlpha;
            static float unity_DOTS_Sampled_RimPower;
            static float unity_DOTS_Sampled_ScanStrength;
            static float unity_DOTS_Sampled_ScanSpeed;

            void SetupDOTSSelectionObjectOutlineMaterialPropertyCaches()
            {
                unity_DOTS_Sampled_BaseColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor);
                unity_DOTS_Sampled_EmissionColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _EmissionColor);
                unity_DOTS_Sampled_OutlineWidth = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _OutlineWidth);
                unity_DOTS_Sampled_OutlineAlpha = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _OutlineAlpha);
                unity_DOTS_Sampled_RimAlpha = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _RimAlpha);
                unity_DOTS_Sampled_RimPower = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _RimPower);
                unity_DOTS_Sampled_ScanStrength = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _ScanStrength);
                unity_DOTS_Sampled_ScanSpeed = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _ScanSpeed);
            }

            #undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
            #define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSSelectionObjectOutlineMaterialPropertyCaches()

            #undef _BaseColor
            #define _BaseColor unity_DOTS_Sampled_BaseColor
            #undef _EmissionColor
            #define _EmissionColor unity_DOTS_Sampled_EmissionColor
            #undef _OutlineWidth
            #define _OutlineWidth unity_DOTS_Sampled_OutlineWidth
            #undef _OutlineAlpha
            #define _OutlineAlpha unity_DOTS_Sampled_OutlineAlpha
            #undef _RimAlpha
            #define _RimAlpha unity_DOTS_Sampled_RimAlpha
            #undef _RimPower
            #define _RimPower unity_DOTS_Sampled_RimPower
            #undef _ScanStrength
            #define _ScanStrength unity_DOTS_Sampled_ScanStrength
            #undef _ScanSpeed
            #define _ScanSpeed unity_DOTS_Sampled_ScanSpeed
            #endif

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalize(normalInputs.normalWS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionInputs.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float rim = pow(saturate(1.0 - dot(normalWS, viewDirWS)), _RimPower);
                float scan = pow(saturate(1.0 - abs(frac(input.positionWS.y * 0.32 + _Time.y * _ScanSpeed) - 0.5) * 8.0), 2.0) * _ScanStrength;
                float alpha = saturate(rim * _RimAlpha + scan * 0.08);
                float3 color = _BaseColor.rgb + _EmissionColor.rgb * (rim + scan);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
