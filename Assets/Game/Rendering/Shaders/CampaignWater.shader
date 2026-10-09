Shader "Game/Environment/CampaignWater"
{
    Properties
    {
        [MainColor] _BaseColor("Water Color", Color) = (.24,.38,.42,1)
        _Smoothness("Smoothness", Range(0,1)) = .48
        _RippleStrength("Ripple Strength", Range(0,.2)) = .075
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
            #pragma vertex WaterVertex
            #pragma fragment WaterFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #define _SPECULAR_COLOR 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Smoothness, _RippleStrength;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half fog:TEXCOORD2;UNITY_VERTEX_INPUT_INSTANCE_ID};
            Varyings WaterVertex(Attributes v)
            {
                Varyings o=(Varyings)0;UNITY_SETUP_INSTANCE_ID(v);UNITY_TRANSFER_INSTANCE_ID(v,o);
                VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;
                o.normalWS=TransformObjectToWorldNormal(v.normalOS);o.fog=ComputeFogFactor(p.positionCS.z);return o;
            }
            half4 WaterFragment(Varyings i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float2 p=i.positionWS.xz;float t=_Time.y*.18;
                // Continuous world-space ripples avoid a repeated pattern or seams at plane boundaries.
                float a=dot(p,float2(.71,.23))+t,b=dot(p,float2(-.31,.53))-t*.73;
                float2 ripple=float2(cos(a)+cos(b)*.45,sin(a*.67)+sin(b)*.55)*_RippleStrength;
                half3 normal=NormalizeNormalPerPixel(i.normalWS+float3(ripple.x,0,ripple.y));
                float variation=sin(dot(p,float2(.037,.029)))*sin(dot(p,float2(-.019,.041)));
                InputData d=(InputData)0;d.positionWS=i.positionWS;d.normalWS=normal;
                d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);d.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
                d.bakedGI=SampleSH(normal);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);d.shadowMask=half4(1,1,1,1);
                SurfaceData s=(SurfaceData)0;s.albedo=_BaseColor.rgb*(1+variation*.075);s.alpha=1;
                // Soft skylight and crest sheen keep the surface legible under warm desert grading.
                half fresnel=pow(1-saturate(dot(normal,d.viewDirectionWS)),3);
                half crest=smoothstep(.70,.98,sin(a+sin(b)*.4));
                s.emission=_BaseColor.rgb*.14+half3(.20,.29,.34)*(fresnel*.12+crest*.025);
                s.specular=half3(.28,.31,.31);s.smoothness=_Smoothness;s.normalTS=half3(0,0,1);s.occlusion=1;
                half4 color=UniversalFragmentBlinnPhong(d,s);color.rgb=MixFog(color.rgb,i.fog);color.a=1;return color;
            }
            ENDHLSL
        }
    }
}
