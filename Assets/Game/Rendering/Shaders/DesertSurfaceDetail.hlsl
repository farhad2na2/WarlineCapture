#ifndef WARLINE_DESERT_SURFACE_DETAIL
#define WARLINE_DESERT_SURFACE_DETAIL

TEXTURE2D(_WarlineWindSand);
SAMPLER(sampler_WarlineWindSand);
float _WarlineDesertReady;
float4 _WarlineYards[6]; // XZ centre, then heading in the map plane
int _WarlineYardCount;

// The two wheel paths have a broad compressed strip and worn diagonal tread blocks.
float DesertWheelPrint(float crossTrack, float alongTrack)
{
    float rail = abs(crossTrack) - 1.12;
    float aa = clamp(fwidth(rail), .025, .34);
    float ribbon = 1 - smoothstep(.17 - aa, .23 + aa, abs(rail));
    float phase = (alongTrack + rail * sign(crossTrack) * .8) * 2.65;
    float tread = max(0, 1 - abs(frac(phase) * 2 - 1) * 1.65);
    tread = lerp(tread, .36, saturate(fwidth(phase) * 1.5));
    return ribbon * (.47 + .53 * tread);
}

half3 DesertSurfaceColor(float2 p, half3 color, half weight)
{
    if (_WarlineDesertReady < .5 || weight < .015) return color;
    float2 bend = float2(ValueNoise(p * .038 + 18), ValueNoise(p * .03 - 7)) - .5;
    float2 uv = (p + bend * 1.5) * .061;
    float fine = SAMPLE_TEXTURE2D(_WarlineWindSand, sampler_WarlineWindSand, uv).g - .62;
    float broad = SAMPLE_TEXTURE2D(_WarlineWindSand, sampler_WarlineWindSand,
        float2(uv.x * .985 - uv.y * .17, uv.x * .17 + uv.y * .985) * .34 + .37).g - .62;
    float2 drift = float2(dot(p, float2(.86, .51)) * .024, dot(p, float2(-.51, .86)) * .061);
    float ochre = smoothstep(.22, .78, ValueNoise(drift + bend * .42 + 43));
    half3 earth = lerp(half3(1.025, 1.0, .98), half3(1.23, 1.03, .79), ochre);
    half3 sand = color * earth * clamp(1 + fine * 2.10 + broad * 1.22, .78, 1.24);
    float mineral = smoothstep(.62, .79, ValueNoise(p * .033 + bend * .38 + 49));
    mineral *= smoothstep(.41, .69, ValueNoise(p * .12 + 81)) * saturate(.60 + fine * 7);

    float yardWeight = 0;
    float tracks = 0;
    for (int i = 0; i < min(6, _WarlineYardCount); ++i)
    {
        float4 yard = _WarlineYards[i];
        float2 d = p - yard.xy;
        float distanceSquared = dot(d, d);
        if (distanceSquared >= 66 * 66) continue;
        float fade = 1 - smoothstep(31, 66, sqrt(distanceSquared));
        yardWeight = max(yardWeight, fade);
        float x = dot(d, float2(yard.w, -yard.z));
        float y = dot(d, yard.zw);
        float2 circle = float2(x + 8, y - 4);
        float arc = DesertWheelPrint(length(circle) - 23.0, atan2(circle.y, circle.x) * 23);
        arc *= smoothstep(-17, -3, y) * (1 - smoothstep(14, 27, x));
        float curve = x - (11.0 + y * .31 + .0028 * y * y);
        float lane = DesertWheelPrint(curve, y + x * .12);
        // Overlapping yards share their wheel marks continuously across the apron.
        tracks = max(tracks, max(arc, lane) * fade);
    }
    sand = lerp(sand, sand * half3(1.10, 1.075, 1.085) + .022, mineral * (1-yardWeight));
    if (tracks > 0)
    {
        float abrasion = smoothstep(.18, .76, ValueNoise(p * .41 + 37) + fine * 2);
        sand *= 1 - tracks * (.29 + .28 * abrasion);
    }
    return lerp(color, sand, saturate(weight));
}

#endif
