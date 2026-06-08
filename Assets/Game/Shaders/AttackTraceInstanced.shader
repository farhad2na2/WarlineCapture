Shader "Game/AttackTraceInstanced"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 0.8, 0.25, 1)
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

            Blend SrcAlpha OneMinusSrcAlpha
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
                float dashPhase = frac(input.uv.y * dashDensity - scrollOffset);

                float dashMask = smoothstep(0.02, 0.1, dashPhase) * (1.0 - smoothstep(0.42, 0.5, dashPhase));
                float side = abs(input.uv.x - 0.5) * 2.0;
                float widthMask = 1.0 - smoothstep(0.72, 1.0, side);

                half4 color = input.color;
                color.a *= dashMask * widthMask;
                return color;
            }
            ENDHLSL
        }
    }
}
