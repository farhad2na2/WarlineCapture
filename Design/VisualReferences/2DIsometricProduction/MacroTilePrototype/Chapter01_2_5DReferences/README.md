# Chapter 1 2.5D Macro-Tile References

Date: 2026-05-05

These references explore the selected terrain direction for Chapter 1: premium 2.5D isometric macro tiles with gameplay metadata.

They are visual targets, not runtime assets. Use them to guide the first authored macro-tile batch, metadata sockets, and later Unity validation captures.

## Source Design

- `Design/SagaChapters/WarlineCapture_Saga_Chapter01_First_Response.md`
- `Design/WarlineCapture_MacroTile_Terrain_Production_Plan.md`
- `Design/WarlineCapture_2D_Isometric_Art_Bible.md`
- `Design/WarlineCapture_2D_Isometric_Production_Direction.md`

## Files

| File | Mission / Level Intent | Use |
|---|---|---|
| `CH01_M01_DistrictEdge_FirstContact_2_5D_Target.png` | M01 First Contact, district edge patrol intercept. | Civilian block, command pad, readable roads, cover, and patrol lanes. |
| `CH01_M02_ForwardPost_EstablishBase_2_5D_Target.png` | M02 Establish The Base, forward post. | Buildable foundations, base entrance, repair site, and defense sockets. |
| `CH01_M03_ConvoyApproach_RadarWarning_2_5D_Target.png` | M03 Radar Warning, convoy approach. | Main convoy lane, checkpoint, defense pads, and base breach boundary. |
| `CH01_M04_LandingZone_Airlift_2_5D_Target.png` | M04 Airlift, landing zone. | Open LZ pad, road junction, extraction edge zones, and side-street pressure lanes. |
| `CH01_M05_FortifiedNode_BreachAssault_2_5D_Target.png` | M05 Breach Assault, fortified node. | Outer approach, walls, breach point, flank routes, and empty enemy-core foundation. |

## Production Rules

- Treat these as target references for large authored macro tiles.
- Do not cut them into tiny repeating road tiles.
- Do not bake gameplay buildings, units, turrets, health bars, objectives, or UI into production terrain.
- Runtime entities should occupy pads/sockets defined by metadata.
- Any road, wall, pad, blocker, spawn, objective, or build position implied by the art must be represented explicitly in `MacroTileDefinition` metadata before gameplay integration.

## Next Use

1. Pick one Chapter 1 reference as the first vertical-slice map direction.
2. Derive a smaller production macro-tile set from it: straight, intersection, T-junction, base/plaza entrance.
3. Define connector and socket metadata before importing into Unity.
4. Build a 2-tile connection test and reject it if roads/pads do not visually align.
5. Build a 2x2 or 3x3 gameplay zoom capture only after the first connection test is clean.
