void ApplyStreetShoulder(float3 world, half up, inout half3 albedo)
{
    half4 field;
    if (!StreetEdgeSample(world, field)) return;
    RevealStreetSidewalk(field, world.xz);
    float distance = (field.r - 0.5) * 6.0;
    if (abs(distance) > 2.94 || field.b < 0.08 || up < 0.65) return;
    float2 p = world.xz;
    float drift = Fbm(p * 0.43 + float2(14.2, -21.7));
    float2 rotated = float2(dot(p, float2(.8,.6)), dot(p, float2(-.6,.8)));
    float broken = ValueNoise(rotated * 2.1 + drift * 1.7);
    float grains = ValueNoise(rotated * 8.7 + broken);
    float grit = smoothstep(.51,.82,grains) * (1.0 - smoothstep(.035,.20,max(fwidth(p.x), fwidth(p.y))));
    float width = lerp(.85,2.45,saturate(.75*drift + .38*broken));
    float ragged = distance - (drift-.5)*.85 - (broken-.5)*.42;
    float core = max(.34, min(.70, _StreetEdgeLevels.z*.63));
    float weight = (1.0-smoothstep(core,width,abs(ragged))) * smoothstep(.08,.58,field.b) * smoothstep(.65,.90,up);
    half3 gravel = lerp(half3(.235,.193,.143), half3(.400,.331,.239), drift*.65+broken*.35);
    gravel *= lerp(.88,1.12,broken);
    gravel = lerp(gravel,half3(.08,.078,.073),grit*(.24+.36*smoothstep(-1.,1.,ragged)));
    // Preserve the painted road model's local brightness across its existing verge.
    if (_StreetEdgeMode > 2.5) weight *= .52;
    albedo = lerp(albedo,gravel,weight);
}
