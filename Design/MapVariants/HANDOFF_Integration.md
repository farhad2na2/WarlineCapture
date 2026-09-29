# Map Variants — Integration Handoff

> **Preparation update (2026-09-29):** Before integration, follow
> [HANDOFF_Map_Preparation.md](HANDOFF_Map_Preparation.md). It specifies dense-city-style entity ownership,
> intact/destroyed cleanup, surfaces, blockers and validation, and corrects the simplified guidance below.
> In particular, `Reserved` is not a gameplay-blocker flag, bridge anchors need deck heights, and the medium
> maps' full world bounds do not fit the simulation grid. The current checkout has 18 mission definitions,
> not 17. See [Integration_Assessment_2026-09-29.md](Integration_Assessment_2026-09-29.md) for mission recommendations.
> After map preparation, use [HANDOFF_Existing_Mission_Rework.md](HANDOFF_Existing_Mission_Rework.md)
> for the seven existing-mission migrations and regression of the eleven retained missions.

Status: **visual prototypes, not integrated.** No mission, catalog, Addressables group or test references these
scenes yet. They were built to break the repetition of the 17 campaign missions, which all frame small windows of
the same dense-city subscene (`opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity`).

Commits: `ea6cd3e63` (builder + four maps), `22a529558` (Demo-scene ground, dressing and lighting).

## What exists

| Map | Scene | World rect (x, z, w, h) | Playable rect | Seed | Objects | Mission fit (from comics) |
|---|---|---|---|---|---|---|
| Refinery District | `Assets/Game/Scenes/MapPrototypes/Variants/MapVariant_RefineryDistrict.unity` | 0, 0, 1400, 1200 | 400, 300, 600, 400 | 40302 | 11,066 | CH02 M02 Supply Line, CH02 M04 Power Relay, CH04 M03 Split Front |
| Ash Line Port | `…/MapVariant_AshLinePort.unity` | 0, 0, 1400, 1200 | 400, 300, 600, 400 | 20502 | 9,492 | CH02 M05 Route Reopened, CH02 M02 Supply Yard, CH01 M03 Convoy Approach |
| City-Edge Airfield | `…/MapVariant_CityEdgeAirfield.unity` | 0, 0, 1400, 1200 | 400, 300, 600, 400 | 10404 | 11,084 | CH01 M04 Airlift, CH04 M01 Air Corridor, CH01 M02 Forward Post |
| Frontier | `…/MapVariant_Frontier.unity` | 0, 0, 2400, 1500 | **176, 176, 2048, 1024** | 90101 | 25,678 | Port, refinery and airfield sectors in one theatre |

Per map, in the repo:

- `Design/MapVariants/<MapId>/*.png`: top-down, battle-camera (51.6° pitch / 55° FOV) and hero captures.
- `Design/MapVariants/<MapId>/audit.json`: audit result (all counters are 0 on the committed build).
- `Assets/Game/Art/MapPrototypes/Variants/<MapId>/MapVariant_<MapId>.layout.json`: world/playable rects and
  **suggested mission zones and anchors** (spawn, chokepoints, objectives) with centers and sizes. Each zone also
  exists in the scene as a `Zone_<id>` marker under the `Zones` layer.
- `Assets/Game/Art/MapPrototypes/Variants/<MapId>/Ground/*.asset`: baked ground chunk meshes (5 m cells).

Scenes, ground meshes and captures are Git LFS files (see `.gitattributes`).

## Scene structure

The single root is `Map_<MapId>`, with one child per `MapVariantLayer`: `Ground, Roads, City, Industrial, Military,
Props, Vehicles, Vegetation, Backdrop, Zones`, plus `Lighting` (sun, RenderSettings and a `Military_Demo` global
volume copied from `Assets/Game/Scenes/Demo.unity`). Everything is a plain static-batched GameObject prefab
instance. There are **no colliders, NavMesh, entity subscenes, grid blockers or `MapSurfaceData`**. Gameplay
truth has to be generated during integration (see below).

## Rebuilding

The build is deterministic from source and seed. Use the menu `Game/Map Variants/Build <Map>`, or on macOS (see `AGENTS.md`):

```bash
Tools/CI/invoke_unity_macos.sh --reuse --timeout 1800 --log /private/tmp/mv.log -- \
  -executeMethod Game.Editor.MapVariants.MapVariantMenu.BuildFrontier
# pass marker: [MapVariants] result=Passed map=Frontier ...
```

Methods: `BuildRefineryDistrict`, `BuildAshLinePort`, `BuildCityEdgeAirfield`, `BuildFrontier`. Each run rewrites the
scene, the ground meshes, the layout JSON, `audit.json` and the captures. It throws (`result=Failed`) if the audit
finds any floating, buried, detached, overlapping, on-road, drifted or out-of-bounds object.

Code: `Assets/Game/Scripts/Editor/MapPrototypes/Variants/` (editor-only, `Game.Editor` assembly).

- `MapVariantBuilder*.cs`: occupancy grid (0.5 m), height field, placement API, roads (reuses
  `RoadVisualVariantSystem` tile selection), environment, audit and capture.
- `MapVariantKits.cs`: curated prefab lists. `MapVariant<Map>.cs`: one layout file per map.
- `MapVariantBuilder.Compose.cs` → `DesertDressing`: the final pass that fills open sand with Demo-scene dressing.

## How campaign maps are wired today

1. `MissionDefinition_*` and `ScenarioSetup_*` (under `Assets/Game/Configs/Missions|Scenarios/ChapterNN/`) name a
   logical `operationMapId`, e.g. `opmap.ch01.district_edge_01`.
2. `Assets/Game/Configs/OperationMaps/ChapterNN/OperationMap_*.asset` (`Game.Configs.OperationMapDefinition`)
   holds `bounds` (world / playable / camera), `cameras`, `minimap`, `anchors`, `gridMetadata` (2048×1024, 1 m cells),
   `surfaceMetadata` (`MapSurfaceData`, 2,097,152 cells), `navigationMetadata.authoredSubSceneGuid`, and a
   `sourceBinding` to the physical map `opmap.skirmish.desert_base_01`, pinned by identity and content hashes.
3. The physical source is the dense-city authoring scene, its entity-presentation subscene and the runtime binding
   scene under `Assets/Game/GeneratedOperationMaps/RuntimeBinding/`, loaded through Addressables.
4. Hash-freezing tests guard this chain, e.g. `M01FirstContactMapSourceBindingTests`,
   `OperationMapCurrentCompatibilityDefinitionTests`, `OperationMapAddressablesLayoutBuilderTests`,
   `OperationMapDenseCityPackedRuntimeParityPlayModeTests`.

Related docs: `Design/3D_Terrain_Map_One_Go_Workflow.md` (coordinate contract, chunked ground and dressing pass),
`Design/3D_Operation_Map_Texture_Mask_Workflow.md` (blocker, height and density rules).

## Recommended integration path

Do **not** rebind the existing logical maps or edit the dense-city source; the frozen-hash tests will (correctly)
fail. Add each variant as a new physical source instead:

1. **Coordinates.** The runtime grid is origin 0, 2048×1024, 1 m cells. Frontier's playable rect is offset by
   (176, 176): translate the `Map_Frontier` root by (−176, 0, −176) or add a builder origin offset, so the playable area
   maps to grid (0,0)–(2048,1024). Everything outside is backdrop. The medium maps fit inside the grid unchanged, or
   can be framed with small playable/camera bounds the way missions frame the dense city today.
2. **Physical source scenes.** Produce an authoring scene and an entity-presentation subscene per variant, following
   the dense-city candidate pipeline (`DenseCityCandidateAuthoringTransaction` and related), plus the runtime binding scene.
3. **Gameplay data.** Generate from builder data, not from the decorative meshes:
   - Heights: `MapVariantHeightField.Sample` matches the ground mesh exactly (same triangulation). Bake it into a
     `MapSurfaceData` asset.
   - Blockers: every `MapVariantPlacement` with `Reserved == true` has an exact footprint (`Center`, `HalfSize`,
     `Yaw`). Roads (`RoadCells`), pads (`Surface == true`), the runway slab and the bridge decks are walkable.
     Water cells (height below `WaterLevel`, −0.8) are blocked except under bridges.
   - Dressing (grass, pebbles, sand edges) is visual only and should not block.
   Serialising `Placements`/`RoadCells` to JSON at build time is the simplest bridge into the existing generators.
4. **OperationMapDefinition.** Create a new logical definition per mission and a new physical definition per
   variant. Take bounds, cameras, minimap and anchors from the zones in `MapVariant_<Map>.layout.json`. Register
   them in the chapter catalog and the Addressables layout.
5. **Missions.** Point the chosen `MissionDefinition`/`ScenarioSetup` at the new logical map ID. Re-place spawns,
   objectives and defense anchors from the zone JSON.
6. **Validation** (per `AGENTS.md`): wrapper-run edit-mode tests, a runtime parity check, then a complete
   normal-input mission playthrough (ARIA, result, return) and on-device checks. Report each gate separately.

## Known limitations / open items

- **Performance is not measured.** Frontier has about 25.7k GameObjects (about 10k of them are dressing). Expect to
  convert the dressing to GPU instancing or entity baking (see the "Hybrid Ground-Chunk And GPU-Instanced Dressing
  Pass" section of the one-go workflow) and to cull backdrop on mobile.
- The lighting copies the Demo scene. The mission runtime may apply its own lighting or volume; decide which wins.
- The water is a Unity plane with `MapVariantKits.WaterMaterial`; there are no shoreline effects.
- Buildings come from the PolygonMilitary village/city kits. They were not checked against the destructible-building
  and attachment pipeline used by the dense city (`DenseCityBuilding*`).
- The Frontier sea, and parts of the medium maps' outer rings, are deliberately non-playable backdrop.
- The shadow workspace `/Users/farhad/Projects/WarlineCapture-MapVariants` (APFS clone) was used for the builds. It is
  disposable; all source lives in this repo.
