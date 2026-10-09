// Shared-world district surfaces, sampled only by opted-in landscape materials.
// RGB masks: concrete service yards, limestone paving, terracotta courtyards.
half SurfaceJoint(float2 uv, float width)
{
    float2 edge=min(frac(uv),1-frac(uv));
    float2 aa=max(fwidth(uv),.002);
    return 1-min(smoothstep(width,width+aa.x,edge.x),smoothstep(width,width+aa.y,edge.y));
}
void ApplyDistrictSurface(float3 world, inout half3 albedo, inout half3 normalWS)
{
    if(_DistrictSurfaceStrength < .001) return;
    float2 uv=(world.xz-_DistrictSurfaceRect.xy)/_DistrictSurfaceRect.zw;
    half3 weights=saturate(SAMPLE_TEXTURE2D(_DistrictSurfaceMap,sampler_DistrictSurfaceMap,uv).rgb + _DistrictSurfaceConstant.rgb) * _DistrictSurfaceStrength;
    weights*=smoothstep(.85,.98,normalWS.y);
    half total=saturate(weights.r+weights.g+weights.b);
    if(total < .001) return;
    float2 p=world.xz;
    float wear=ValueNoise(p*.19+39), grain=ValueNoise(p*2.2+71);
    float stain=smoothstep(.62,.83,ValueNoise(p*.47+104))*smoothstep(.5,.72,ValueNoise(p*.12));
    // Large service slabs: muted cement, expansion joints and local wear.
    float2 slab=p/3.8;
    float slabTone=Hash21(floor(slab));
    half3 concrete=lerp(half3(.43,.43,.40),half3(.60,.59,.53),slabTone*.65+wear*.35);
    concrete*=1-.19*SurfaceJoint(slab,.009);
    concrete*=1-stain*.22;
    // Sparse hairline fractures stay inside individual slabs; edge dust softens joints.
    float2 slabLocal=frac(slab);
    float crackSeed=Hash21(floor(slab)+17.6);
    float crackPath=.22+crackSeed*.49+(slabLocal.x-.5)*.16+sin(slabLocal.x*11+crackSeed*19)*.025;
    float crackAA=max(fwidth(slabLocal.y),.001);
    float fracture=(1-smoothstep(.0015,.0015+crackAA,abs(slabLocal.y-crackPath)))
        *step(.79,crackSeed)*smoothstep(.12,.23,slabLocal.x)*(1-smoothstep(.72,.9,slabLocal.x));
    concrete*=1-fracture*.22;
    float jointDust=(1-smoothstep(.01,.10,min(min(slabLocal.x,1-slabLocal.x),min(slabLocal.y,1-slabLocal.y))))*wear;
    concrete=lerp(concrete,half3(.58,.54,.46),jointDust*.16);
    // Staggered limestone sets keep residential ground warmer than the base.
    float2 set=p/float2(.85,.52);set.x+=fmod(floor(set.y),2)*.5;
    half3 stone=lerp(half3(.46,.42,.34),half3(.65,.59,.48),Hash21(floor(set)));
    stone*=1-.20*SurfaceJoint(set,.025);
    // Occasional settled or chipped sets break the machine-perfect paving pattern.
    stone=lerp(stone,albedo,step(Hash21(floor(set)+71.9),.035)*.48);
    // Selected inner courtyards have faded terracotta, not brightly colored carpets.
    float2 tile=p/.58;
    half3 clay=lerp(half3(.39,.27,.20),half3(.58,.43,.32),Hash21(floor(tile))*.65+wear*.35);
    clay*=1-.23*SurfaceJoint(tile,.02);
    half3 surface=(concrete*weights.r+stone*weights.g+clay*weights.b)/max(dot(weights,half3(1,1,1)),.001);
    surface*=.94+grain*.10;
    albedo=lerp(albedo,surface,total);
    normalWS=normalize(lerp(normalWS,half3(0,1,0),total*.85));
}
