Shader "Game/Environment/CampaignBuildingWeathering"
{
    Properties
    {
        [MainTexture] _BaseMap("Existing Building Atlas", 2D) = "white" {}
        [MainColor] _BaseColor("Atlas Tint", Color) = (1,1,1,1)
        _Weathering("Weathering", Range(0,1)) = 0.5
        _DustColor("Deposited Dust", Color) = (.68,.61,.49,1)
        _RoofTint("Roof Repair Tone", Color) = (.40,.43,.42,1)
        _Smoothness("Smoothness", Range(0,1)) = .08
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex BuildingVertex
            #pragma fragment BuildingFragment
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
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor, _DustColor, _RoofTint;
                half _Weathering, _Smoothness;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS:POSITION; float3 normalOS:NORMAL;
                float2 uv:TEXCOORD0; float2 staticLightmapUV:TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0;
                half3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2;
                half fog:TEXCOORD3; float relativeHeight:TEXCOORD6; half3 vertexLighting:TEXCOORD7;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV,vertexSH,4);
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            float HashBuilding(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float NoiseBuilding(float2 p)
            {
                float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(HashBuilding(i),HashBuilding(i+float2(1,0)),f.x),lerp(HashBuilding(i+float2(0,1)),HashBuilding(i+1),f.x),f.y);
            }
            Varyings BuildingVertex(Attributes v)
            {
                Varyings o=(Varyings)0;UNITY_SETUP_INSTANCE_ID(v);UNITY_TRANSFER_INSTANCE_ID(v,o);
                VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(v.normalOS);
                o.relativeHeight=p.positionWS.y-TransformObjectToWorld(float3(0,0,0)).y;
                o.uv=TRANSFORM_TEX(v.uv,_BaseMap);o.fog=ComputeFogFactor(p.positionCS.z);
                OUTPUT_LIGHTMAP_UV(v.staticLightmapUV,unity_LightmapST,o.staticLightmapUV);OUTPUT_SH(o.normalWS,o.vertexSH);
                o.vertexLighting=VertexLighting(p.positionWS,o.normalWS);
                return o;
            }
            half4 BuildingFragment(Varyings i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half3 n=NormalizeNormalPerPixel(i.normalWS);
                half3 original=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
                float roof=smoothstep(.52,.91,n.y)*smoothstep(1.5,4,i.relativeHeight);
                float2 panel=i.positionWS.xz/float2(2.8,1.35);
                float seed=HashBuilding(floor(panel));
                float2 edge=min(frac(panel),1-frac(panel));
                float seam=1-smoothstep(.006,.022+max(fwidth(panel.x),fwidth(panel.y)),min(edge.x,edge.y));
                float patch=step(.79,seed)*roof;
                float wear=NoiseBuilding(i.positionWS.xz*.34);
                half3 weathered=lerp(original,_RoofTint.rgb,roof*.12+patch*.28);
                weathered*=1-roof*seam*.13;
                weathered=lerp(weathered,_DustColor.rgb,roof*(.05+wear*.10));
                float foot=(1-smoothstep(-.1,1.4,i.relativeHeight))*(1-saturate(n.y));
                weathered*=1-foot*(.055+wear*.055);
                InputData d=(InputData)0;d.positionWS=i.positionWS;d.normalWS=n;
                d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);d.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
                d.vertexLighting=i.vertexLighting;d.fogCoord=i.fog;d.bakedGI=SAMPLE_GI(i.staticLightmapUV,i.vertexSH,n);
                d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);d.shadowMask=SAMPLE_SHADOWMASK(i.staticLightmapUV);
                SurfaceData s=(SurfaceData)0;s.albedo=lerp(original,weathered,_Weathering);s.alpha=1;
                s.specular=half3(.12,.12,.12);s.smoothness=_Smoothness;s.normalTS=half3(0,0,1);s.occlusion=1;
                half4 color=UniversalFragmentBlinnPhong(d,s);color.rgb=MixFog(color.rgb,i.fog);color.a=1;return color;
            }
            ENDHLSL
        }
        UsePass "Game/Environment/GroundMacroVariation/ShadowCaster"
        UsePass "Game/Environment/GroundMacroVariation/DepthOnly"
        UsePass "Game/Environment/GroundMacroVariation/DepthNormals"
    }
}
