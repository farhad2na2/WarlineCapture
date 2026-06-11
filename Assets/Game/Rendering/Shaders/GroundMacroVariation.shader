// Ground macro variation shader for Synty atlas-mapped ground tiles.
// Samples the shared POLYGON atlas exactly like URP Lit, then layers
// world-space (XZ-projected) macro tint + close-up detail noise on top,
// so hundreds of ground tile meshes get continuous, seam-free variation
// without touching the atlas or the prop materials.
//
// Runtime toggle: VisualQualitySettingsSystem sets the global float
// _GroundVariationDisabled (0 = on, 1 = off). It defaults to 0, so the
// effect is visible in edit mode without any bootstrap.
Shader "Game/Environment/GroundMacroVariation"
{
    Properties
    {
        [MainTexture] _BaseMap("Atlas (Base Map)", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Smoothness("Smoothness", Range(0, 1)) = 0.2
        _SpecColor("Specular Color", Color) = (0.2, 0.2, 0.2, 1)

        [Header(Macro Variation World XZ)]
        _MacroTintA("Macro Tint A (dry dirt)", Color) = (0.82, 0.74, 0.58, 1)
        _MacroTintB("Macro Tint B (olive grass)", Color) = (0.72, 0.78, 0.55, 1)
        _MacroStrength("Macro Strength", Range(0, 1)) = 0.45
        _MacroScale("Macro Noise Scale (1/m)", Range(0.001, 0.2)) = 0.022
        _MacroContrastLow("Macro Mask Low", Range(0, 1)) = 0.25
        _MacroContrastHigh("Macro Mask High", Range(0, 1)) = 0.75

        [Header(Close Detail)]
        _DetailStrength("Detail Strength", Range(0, 0.3)) = 0.06
        _DetailScale("Detail Noise Scale (1/m)", Range(0.05, 4)) = 0.7
        _DetailFadeStart("Detail Fade Start (m)", Float) = 40
        _DetailFadeEnd("Detail Fade End (m)", Float) = 120

        [Header(Detail Textures World Projected)]
        _DesertDetailMap("Desert Detail (stones, thorn scrub)", 2D) = "gray" {}
        _DesertDetailNormal("Desert Detail Normal", 2D) = "bump" {}
        _GreenDetailMap("Green Patch Detail (scrub grass)", 2D) = "gray" {}
        _GroundDetailTiling("Detail Tiling (1/m)", Range(0.01, 2)) = 0.18
        _GroundDetailStrength("Detail Albedo Strength", Range(0, 1)) = 0.7
        _GroundDetailNormalStrength("Detail Normal Strength", Range(0, 2)) = 0.8
        _GreenPatchThreshold("Green Patch Threshold", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex GroundVertex
            #pragma fragment GroundFragment

            // URP keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_DesertDetailMap);
            SAMPLER(sampler_DesertDetailMap);
            TEXTURE2D(_DesertDetailNormal);
            TEXTURE2D(_GreenDetailMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Smoothness;
                half4 _SpecColor;
                half4 _MacroTintA;
                half4 _MacroTintB;
                half _MacroStrength;
                float _MacroScale;
                half _MacroContrastLow;
                half _MacroContrastHigh;
                half _DetailStrength;
                float _DetailScale;
                float _DetailFadeStart;
                float _DetailFadeEnd;
                float _GroundDetailTiling;
                half _GroundDetailStrength;
                half _GroundDetailNormalStrength;
                half _GreenPatchThreshold;
            CBUFFER_END

            // Global runtime toggle (0 = enabled, 1 = disabled). Globals default
            // to 0, so the effect is on in edit mode without any bootstrap code.
            float _GroundVariationDisabled;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 staticLightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 4);
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // --- Cheap world-space value noise -------------------------------
            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float Fbm(float2 p)
            {
                return ValueNoise(p) * 0.55
                     + ValueNoise(p * 2.13 + 5.2) * 0.30
                     + ValueNoise(p * 4.71 + 9.1) * 0.15;
            }
            // ------------------------------------------------------------------

            // Computes the world-projected variation multiplier and perturbs the
            // normal with the stone detail normal map. Detail textures are
            // selected by the same noise that places the green patches, so dry
            // areas get stones + thorn scrub and green patches get scrub grass.
            void ApplyGroundVariation(float3 positionWS, inout half3 albedo, inout half3 normalWS)
            {
                float2 p = positionWS.xz;
                half enabled = 1.0 - saturate(_GroundVariationDisabled);

                // Large-scale patches: blend between two earth tones.
                float patchMask = Fbm(p * _MacroScale);
                float tintSelect = Fbm(p * _MacroScale * 0.37 + 17.3);
                half3 tint = lerp(_MacroTintA.rgb, _MacroTintB.rgb, tintSelect);
                half w = _MacroStrength * smoothstep(_MacroContrastLow, _MacroContrastHigh, patchMask);
                half3 variation = lerp(half3(1, 1, 1), tint, w);

                // Fades: camera distance + slope (keeps cliffs from streaking).
                float dist = distance(positionWS, _WorldSpaceCameraPos);
                float distFade = 1.0 - saturate((dist - _DetailFadeStart) / max(_DetailFadeEnd - _DetailFadeStart, 0.001));
                float slopeFade = smoothstep(0.35, 0.65, normalWS.y);
                float detailFade = distFade * slopeFade;

                // Close-up brightness jitter.
                float jitter = ValueNoise(p * _DetailScale);
                variation *= 1.0 + (jitter - 0.5) * 2.0 * _DetailStrength * distFade;

                // World-projected detail textures (stored around mid-gray, so
                // *2 makes 0.5 neutral): stones/thorn in dry areas, scrub grass
                // inside the green patches.
                float2 duv = p * _GroundDetailTiling;
                half3 desertDetail = SAMPLE_TEXTURE2D(_DesertDetailMap, sampler_DesertDetailMap, duv).rgb;
                half3 greenDetail = SAMPLE_TEXTURE2D(_GreenDetailMap, sampler_DesertDetailMap, duv * 0.93).rgb;
                half greenMask = smoothstep(_GreenPatchThreshold, _GreenPatchThreshold + 0.25, tintSelect)
                               * smoothstep(0.2, 0.6, patchMask);
                half3 detailColor = lerp(desertDetail, greenDetail, greenMask);
                half detailWeight = _GroundDetailStrength * detailFade * enabled;
                variation *= lerp(half3(1, 1, 1), detailColor * 2.0, detailWeight);

                variation = lerp(half3(1, 1, 1), variation, enabled);
                albedo *= variation;

                // Stone detail normal, projected top-down (x -> world X, y -> world Z),
                // reduced inside green patches where stones are sparse.
                half normalScale = _GroundDetailNormalStrength * detailFade * enabled * (1.0 - greenMask * 0.6);
                half3 detailNormalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_DesertDetailNormal, sampler_DesertDetailMap, duv), normalScale);
                normalWS = normalize(half3(normalWS.x + detailNormalTS.x, normalWS.y, normalWS.z + detailNormalTS.y));
            }

            Varyings GroundVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);

                return output;
            }

            half4 GroundFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 atlas = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = atlas.rgb * _BaseColor.rgb;
                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                ApplyGroundVariation(input.positionWS, albedo, normalWS);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.alpha = 1;
                surfaceData.specular = _SpecColor.rgb;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = 1;

                half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = 1;
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

            #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half DepthOnlyFragment(Varyings input) : SV_Target
            {
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
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
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 DepthNormalsFragment(Varyings input) : SV_Target
            {
                return half4(normalize(input.normalWS), 0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Simple Lit"
}
