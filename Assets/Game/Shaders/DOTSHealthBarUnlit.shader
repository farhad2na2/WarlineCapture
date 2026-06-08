Shader "Game/DOTS Health Bar (Unlit)"
{
    Properties
    {
        _Fill("Fill", Range(0,1)) = 1
        _LowColor("Low Color", Color) = (1,0.2,0.2,1)
        _HighColor("High Color", Color) = (0.2,1,0.2,1)
        _BackgroundColor("Background", Color) = (0,0,0,0.6)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Fill;
                float4 _LowColor;
                float4 _HighColor;
                float4 _BackgroundColor;
            CBUFFER_END

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
            UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                UNITY_DOTS_INSTANCED_PROP(float, _Fill)
            UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

            static float unity_DOTS_Sampled_Fill;

            void SetupDOTSHealthBarMaterialPropertyCaches()
            {
                unity_DOTS_Sampled_Fill = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Fill);
            }

            #undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
            #define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSHealthBarMaterialPropertyCaches()

            #undef _Fill
            #define _Fill unity_DOTS_Sampled_Fill
            #endif

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // Billboard: always face camera so the bar is readable at an angle.
                float3 centerWS = TransformObjectToWorld(float3(0, 0, 0));

                float3 camRightWS = normalize(mul((float3x3)UNITY_MATRIX_I_V, float3(1, 0, 0)));
                float3 camUpWS = normalize(mul((float3x3)UNITY_MATRIX_I_V, float3(0, 1, 0)));

                // Preserve object scale (ignore object rotation for the billboard plane).
                float3 axisX = mul((float3x3)GetObjectToWorldMatrix(), float3(1, 0, 0));
                float3 axisY = mul((float3x3)GetObjectToWorldMatrix(), float3(0, 1, 0));
                float sx = max(1e-5, length(axisX));
                float sy = max(1e-5, length(axisY));

                float3 offsetWS = camRightWS * (input.positionOS.x * sx) + camUpWS * (input.positionOS.y * sy);
                float3 posWS = centerWS + offsetWS;

                output.positionHCS = TransformWorldToHClip(posWS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float fill = saturate(_Fill);

                float4 fillCol = lerp(_LowColor, _HighColor, fill);
                float4 bgCol = _BackgroundColor;

                // Single-quad bar: left side = fill, right side = background.
                float isFill = step(input.uv.x, fill);
                float4 col = lerp(bgCol, fillCol, isFill);

                return (half4)col;
            }
            ENDHLSL
        }
    }
}
