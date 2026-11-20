Shader "WarlineCapture/Markers/TacticalMarker"
{
    Properties
    {
        _BaseColor ("Stroke", Color) = (0.211765, 0.847059, 0.894118, 1)
        [Enum(Selection,0,Move,1,Attack,2)] _MarkerShape ("Shape", Float) = 0
        _MaxViewportSpan ("Maximum span in viewport heights (0 = uncapped)", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth test", Float) = 4
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "TacticalMarker"
            Tags { "LightMode" = "UniversalForward" }
            Blend One OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest [_ZTest]
            Offset -1, -1

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _MarkerShape;
                float _MaxViewportSpan;
                float _ZTest;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float ProjectedDistance(float2 cornerOS, float4 center, float aspect)
            {
                float4 corner = TransformObjectToHClip(float3(cornerOS, 0));
                float2 offset = corner.xy / max(corner.w, center.w * 0.1) - center.xy / center.w;
                return length(offset * float2(aspect, 1));
            }

            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 position = input.positionOS;
                if (_MaxViewportSpan > 0)
                {
                    float4 center = TransformObjectToHClip(float3(0, 0, 0));
                    if (center.w > 0.001)
                    {
                        float aspect = _ScreenParams.x / _ScreenParams.y;
                        float span = max(
                            max(ProjectedDistance(float2(-0.5, -0.5), center, aspect),
                                ProjectedDistance(float2( 0.5, -0.5), center, aspect)),
                            max(ProjectedDistance(float2(-0.5,  0.5), center, aspect),
                                ProjectedDistance(float2( 0.5,  0.5), center, aspect)));
                        // NDC radius equals diameter in viewport heights. Scale around the original anchor.
                        position *= min(1, _MaxViewportSpan / max(span, 0.001));
                    }
                }
                output.positionHCS = TransformObjectToHClip(position);
                output.uv = input.uv;
                return output;
            }

            float SegmentDistance(float2 p, float2 a, float2 b)
            {
                float2 segment = b - a;
                return length(p - a - segment * saturate(dot(p - a, segment) / dot(segment, segment)));
            }

            float SelectionDistance(float2 p)
            {
                float2 quadrant = abs(p);
                float2 sector = float2(max(quadrant.x, quadrant.y), min(quadrant.x, quadrant.y));
                float ring = abs(length(p) - 0.39);
                float ticks = SegmentDistance(sector, float2(0.35, 0), float2(0.47, 0));
                return min(ring, ticks);
            }

            float MoveDistance(float2 p)
            {
                float diamond = SegmentDistance(abs(p), float2(0, 0.43), float2(0.34, 0.09));
                float2 mirrored = float2(abs(p.x), p.y);
                float upperChevron = SegmentDistance(mirrored, float2(0, 0.21), float2(0.13, 0.08));
                float lowerChevron = SegmentDistance(mirrored, float2(0, 0.02), float2(0.13, -0.11));
                return min(diamond, min(upperChevron, lowerChevron));
            }

            float AttackDistance(float2 p)
            {
                float2 quadrant = abs(p);
                float horizontal = SegmentDistance(quadrant, float2(0.22, 0.40), float2(0.40, 0.40));
                float vertical = SegmentDistance(quadrant, float2(0.40, 0.22), float2(0.40, 0.40));
                float pip = length(p) - 0.014;
                return min(pip, min(horizontal, vertical));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = (input.uv - 0.5) * 1.25;
                float distance = _MarkerShape < 0.5 ? SelectionDistance(p)
                    : _MarkerShape < 1.5 ? MoveDistance(p)
                    : AttackDistance(p);

                // Convert the local distance to screen pixels so a foreshortened edge
                // gets the same stroke and keyline as the rest of the marker.
                float2 uvStep = float2(length(ddx(p)), length(ddy(p)));
                float localUnitsPerPixel = max(length(float2(ddx(distance), ddy(distance))),
                    max(0.00001, min(uvStep.x, uvStep.y) * 0.5));
                float screenScale = GetScaledScreenParams().y / 1080.0;
                float halfStroke = min(1.75 * screenScale, 0.083 / localUnitsPerPixel);
                float keyline = min(1.5 * screenScale, 0.045 / localUnitsPerPixel);
                float pixelDistance = distance / localUnitsPerPixel;
                float ink = saturate(halfStroke + 0.5 - pixelDistance);
                float coverage = saturate(halfStroke + keyline + 0.5 - pixelDistance);
                half3 keylineColor = half3(0.008, 0.013, 0.019);
                return half4((keylineColor * (coverage - ink) + _BaseColor.rgb * ink) * _BaseColor.a,
                    coverage * _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
