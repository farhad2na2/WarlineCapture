Shader "Game/Environment/CampaignWater"
{
    Properties
    {
        [MainColor] _BaseColor("Deep Water Color", Color) = (.24,.38,.42,1)
        _ShallowColor("Shallow Bank Color", Color) = (.31,.52,.49,1)
        _DepthFalloff("Bank To Deep Distance (m)", Range(.5,20)) = 4.5
        _Smoothness("Smoothness", Range(0,1)) = .48
        _RippleStrength("Ripple Strength", Range(0,.2)) = .075
        _FlowSpeed("Flow Speed (m/s)", Range(0,2)) = .35
        _FoamColor("Contact Foam Color", Color) = (.80,.84,.80,1)
        _FoamWidth("Contact Foam Width (m)", Range(0,2)) = .55
        _FoamStrength("Contact Foam Strength", Range(0,1)) = 0
        _SkyColor("Sky Reflection Color", Color) = (.55,.70,.78,1)
        _SkyReflection("Sky Reflection Strength", Range(0,1)) = 0
        _ShoreDistance("Terrain Shore Distance", 2D) = "white" {}
        _ShoreRect("Shore World Rect", Vector) = (0,0,1,1)
        _ShoreStrength("Terrain Shore Strength", Range(0,1)) = 0
        _BankColors("Use Authored Banks", Float) = 0
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
            TEXTURE2D(_ShoreDistance); SAMPLER(sampler_ShoreDistance);
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor,_ShallowColor,_FoamColor,_SkyColor;
                half _Smoothness, _RippleStrength, _BankColors, _FoamStrength, _SkyReflection;
                float _DepthFalloff, _FlowSpeed, _FoamWidth, _ShoreStrength;
                float4 _ShoreRect;
            CBUFFER_END
            // Channel UV0: x across (0..1), y along (0..1), zw object-space width and length.
            // Channel UV1: xyz object-space flow axis, w closed-edge flags
            // (1 across min, 2 across max, 4 along min, 8 along max, 16 ends are headwalls).
            // Channel UV2: xyz object-space across axis.
            // Meshes without channel UVs (width 0) keep the uniform deep surface.
            struct Attributes {float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; float4 channel:TEXCOORD0; float4 flow:TEXCOORD1; float4 side:TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half fog:TEXCOORD2;float4 channel:TEXCOORD3;float4 flow:TEXCOORD4;half4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            float WaterHash(float2 p){p=frac(p*float2(123.34,345.45));p+=dot(p,p+34.345);return frac(p.x*p.y);}
            float WaterNoise(float2 p)
            {
                float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(WaterHash(i),WaterHash(i+float2(1,0)),f.x),lerp(WaterHash(i+float2(0,1)),WaterHash(i+float2(1,1)),f.x),f.y);
            }
            Varyings WaterVertex(Attributes v)
            {
                Varyings o=(Varyings)0;UNITY_SETUP_INSTANCE_ID(v);UNITY_TRANSFER_INSTANCE_ID(v,o);
                VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;
                o.color=v.color;
                // Shared meshes are scaled per renderer, so channel sizes are converted to world metres here.
                float flowScale=length(TransformObjectToWorldDir(v.flow.xyz,false));
                float sideScale=length(TransformObjectToWorldDir(v.side.xyz,false));
                o.channel=float4(v.channel.xy,v.channel.z*sideScale,v.channel.w*flowScale);
                float3 axis=TransformObjectToWorldDir(v.flow.xyz+float3(1e-5,0,0));axis.y=0;axis=normalize(axis+float3(1e-5,0,0));
                // Neighbouring tiles may be mirrored; one world-facing axis keeps their ripples continuous.
                axis*=abs(axis.z)>=abs(axis.x)?sign(axis.z):sign(axis.x);
                o.flow=float4(axis,v.flow.w);o.normalWS=TransformObjectToWorldNormal(v.normalOS);o.fog=ComputeFogFactor(p.positionCS.z);return o;
            }
            half4 WaterFragment(Varyings i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half bank=i.color.a*_BankColors;
                float width=i.channel.z,len=i.channel.w;half channelled=step(.01,width);
                float across=i.channel.x*width,alongLocal=i.channel.y*len;
                float2 p=i.positionWS.xz;
                float2 axis=i.flow.xz,side=float2(axis.y,-axis.x);
                // World projection keeps flow continuous across neighbouring tiles that share meshes.
                float along=dot(p,axis);
                // Distance to the nearest stone lip; the authored lip covers the outer band itself.
                float flags=floor(i.flow.w+.5);
                half4 lipped=half4(fmod(flags,2),fmod(floor(flags/2),2),fmod(floor(flags/4),2),fmod(floor(flags/8),2));
                float lip=min(.36,min(width,len)*.06);
                float endInset=lerp(lip,1.15,step(15.5,flags));
                float4 sides=float4(across-lip,width-across-lip,alongLocal-endInset,len-alongLocal-endInset);
                float4 open=(1-lipped)*1e4;
                float nearest=max(min(min(sides.x+open.x,sides.y+open.y),min(sides.z+open.z,sides.w+open.w)),0);
                float edge=lerp(1e4,nearest,channelled);
                if(_ShoreStrength>.001)
                {
                    float2 shoreUv=(p-_ShoreRect.xy)/_ShoreRect.zw;
                    float inRect=step(0,shoreUv.x)*step(0,shoreUv.y)*step(shoreUv.x,1)*step(shoreUv.y,1);
                    float shore=SAMPLE_TEXTURE2D(_ShoreDistance,sampler_ShoreDistance,shoreUv).r*16;
                    edge=min(edge,lerp(1e4,shore,_ShoreStrength*inRect));
                }
                half edged=max(channelled,_ShoreStrength);
                float t=_Time.y,tl=t*.18;
                // Channel ripples stretch along the flow and drift downstream; other water keeps world ripples.
                float2 q=lerp(p,float2(dot(p,side)*1.4,(along-t*_FlowSpeed)*.6),channelled);
                q*=lerp(1,2.8,channelled);
                float a=dot(q,float2(.71,.23))+tl,b=dot(q,float2(-.31,.53))-tl*.73;
                float c=dot(q,float2(.37,1.9))+t*.41;
                float2 rippleLocal=float2(cos(a)+cos(b)*.45+cos(c)*.22*channelled,sin(a*.67)+sin(b)*.55)*_RippleStrength;
                float2 ripple=lerp(rippleLocal,rippleLocal.x*side+rippleLocal.y*axis,channelled);
                half calmEdge=lerp(1,smoothstep(0,1.2,edge),edged);
                half3 normal=NormalizeNormalPerPixel(i.normalWS+float3(ripple.x,0,ripple.y)*(1-bank)*(.45+.55*calmEdge));
                float variation=sin(dot(p,float2(.037,.029)))*sin(dot(p,float2(-.019,.041)));
                half depth=smoothstep(0,1,saturate(edge/_DepthFalloff));
                half3 water=lerp(_ShallowColor.rgb,_BaseColor.rgb,depth)*(1+variation*.075);
                InputData d=(InputData)0;d.positionWS=i.positionWS;d.normalWS=normal;
                d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);d.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
                d.bakedGI=SampleSH(normal);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);d.shadowMask=half4(1,1,1,1);
                // Broken foam hugs the stone contact line and shimmers slowly with the flow.
                float foamNoise=WaterNoise(float2(dot(p,side)*1.7,(along-t*_FlowSpeed*.6)*.9))*.65+WaterNoise(p*3.1+t*.2)*.35;
                half foam=(1-smoothstep(0,max(_FoamWidth,.001),edge))*smoothstep(.28,.62,foamNoise+.25*(1-saturate(edge/max(_FoamWidth,.01))))*_FoamStrength*edged;
                SurfaceData s=(SurfaceData)0;s.albedo=lerp(water,_FoamColor.rgb,foam);s.alpha=1;
                half fresnel=pow(1-saturate(dot(normal,d.viewDirectionWS)),3);
                half crest=smoothstep(.70,.98,sin(a+sin(b)*.4));
                // Top-down cameras see little Fresnel, so keep a floor of sky reflection on open water.
                half sky=_SkyReflection*(.28+fresnel*.72)*depth*(1-foam);
                s.emission=lerp(_BaseColor.rgb,water,channelled)*.14+half3(.20,.29,.34)*(fresnel*.12+crest*.025)+_SkyColor.rgb*sky*.35;
                s.albedo=lerp(s.albedo,i.color.rgb,bank);
                s.emission*=1-bank;
                s.specular=lerp(lerp(half3(.28,.31,.31),half3(.10,.11,.11),foam),half3(.04,.04,.04),bank);
                s.smoothness=lerp(lerp(_Smoothness,.25,foam),.10,bank);s.normalTS=half3(0,0,1);s.occlusion=1;
                half4 color=UniversalFragmentBlinnPhong(d,s);color.rgb=MixFog(color.rgb,i.fog);color.a=1;return color;
            }
            ENDHLSL
        }
    }
}
