#ifndef GAME_ROAD_SHOULDER_MASK_INCLUDED
#define GAME_ROAD_SHOULDER_MASK_INCLUDED
TEXTURE2D(_StreetEdgeMap);
SAMPLER(sampler_StreetEdgeMap);

bool StreetEdgeSample(float3 world, out half4 field)
{
    field = 0;
    if (_StreetEdgeMode < 0.5) return false;
    float2 uv = (world.xz - _StreetEdgeRect.xy) / _StreetEdgeRect.zw;
    if (any(uv < 0) || any(uv >= 1)) return false;
    uint width, height;
    _StreetEdgeMap.GetDimensions(width, height);
    float2 texel = rcp(float2(width, height));
    float cellSize = _StreetEdgeLevels.z * 96.0;
    float lookupRows = round(_StreetEdgeRect.w / cellSize);
    float2 cell = (world.xz - _StreetEdgeRect.xy) / cellSize;
    float2 entryUV = (floor(cell) + 0.5) * texel;
    float3 entry = SAMPLE_TEXTURE2D_LOD(_StreetEdgeMap, sampler_StreetEdgeMap, entryUV, 0).rgb;
    if (entry.b < 0.5) return false;
    // Each 96-texel page carries a one-texel gutter sampled from its neighbour.
    float2 page = round(entry.rg * 255.0) * 98.0 + float2(0, lookupRows);
    float2 pageUV = (page + 1.0 + frac(cell)*96.0) * texel;
    field = SAMPLE_TEXTURE2D_LOD(_StreetEdgeMap, sampler_StreetEdgeMap, pageUV, 0);
    float roadY = _StreetEdgeLevels.x + field.a * _StreetEdgeLevels.y;
    // The receiving ground can sit below a render-only paved cap. Use the
    // same soil clearance as the bake, while keeping stacked roads independent.
    float tolerance = _StreetEdgeMode > 1.5 ? .48 : .89;
    return abs(roadY - world.y) < tolerance + _StreetEdgeLevels.y / 255.0;
}

float StreetChipNoise(float2 positionXZ)
{
    float2 cell=floor(positionXZ);
    float2 f=frac(positionXZ);
    f=f*f*(3.0-2.0*f);
    float4 h=frac((cell.x+float4(0,1,0,1))*.1031+(cell.y+float4(0,0,1,1))*.13787);
    h=frac(h*(h+19.19)*(h+7.13));
    return lerp(lerp(h.x,h.y,f.x),lerp(h.z,h.w,f.x),f.y);
}

void RevealStreetSidewalk(half4 field, float2 worldXZ)
{
    // Only confirmed at-grade overlaps are removed. The depth and shadow passes
    // use the same threshold so the real sidewalk can win the depth test.
    if (_StreetEdgeMode < 1.5) return;
    clip(0.90 - field.g);
    // The sand and asphalt of a legacy painted verge share this triangle.
    if (_StreetEdgeMode > 2.5) return;
    // Break the actual asphalt silhouette only where the survey found soil directly below.
    float distance=(field.r-.5)*6.;
    if (field.g>.10 && field.g<.70 && field.b>.30 && distance<1.10)
    {
        float chips = StreetChipNoise(worldXZ * .91);
        float aggregate = StreetChipNoise(worldXZ * 5.3 + chips) - .5;
        float grains = StreetChipNoise(worldXZ * 14.7) - .5;
        clip(distance - (.18 + .56 * chips + .37 * aggregate + .22 * grains));
    }
}
#endif
