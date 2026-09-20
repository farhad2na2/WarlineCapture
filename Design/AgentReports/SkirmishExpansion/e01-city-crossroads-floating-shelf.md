# E0.1 City Crossroads floating shelf

Date: 2026-09-20
Baseline: `codex/m03-radar-warning` `61d7fcd12f767a03ea0eac4f04e90c51971b3420`
Head: (this commit)
PR: https://github.com/farhad2na2/WarlineCapture/pull/19
Workflow path: pull request
Task: remove the player-visible City Crossroads floating ground plate at the statue / base join, and make surface plus resident geometry agree there. Do not treat the earlier northern/civic height samples as sufficient.

## Diagnosis

City Crossroads reuses the dense-city physical source (`opmap.skirmish.desert_base_01`, scene GUID `dad0bd13fb20943dfb2f881cbe225f05`) and the shared bake `Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset`. Presentation is EntityScene + VirtualizedProxyPool.

Dense-city visual grading archived interior hill/dune GameObjects (`DenseCity_GradingArchive`). The retained city floor (`Ground_Round`) sits at authored grade (~0.01 m). The shared surface blob was not rebaked, so three unsupported plateaus remained in the playable window at about 5.85–6.2 m.

The first E0.1 pass flattened those surface cells at load time. Focused validation then Passed cases=11 on the old northern/civic sample cells. That was **not** the player-visible plate.

Mac Game View (Skirmish 2 City Crossroads, seed 104729) still showed a large flat tan/grey rectangle with sharp edges and a rectangular shadow:

- Camera A `(1020, 70, 680)` → `(1020, 0, 735)`: plate right of the concrete barrier, beside the civic cluster.
- Camera D `(1035, 18, 500)` → `(1035, 0, 540)`: plate beside the civic hall / statue-base join.

### What the visible plate actually is

The rectangle is **not** a leftover `Ground_Hill` / `Hill_Flat_Square` mesh and **not** a virtualized render-DB floor plate.

| Candidate | Result |
|---|---|
| Render-DB `Ground_Round` / square / hill meshes | Present, but placement Y ≤ 0.08 m in the City Crossroads window. No statue GUIDs. |
| Surface 5.85 m shelves | Already flattened at the old sample cells. Hall/plinth cells `(1031,540)`, `(1043,488)`, `(965,688)` are 0.009 m after the first pass. |
| Leftover 2.16 m surface skirts | Real, but they sit on the stamp edges, not under the statue-side plate. They do not match the large shadowed rectangle. |
| Virtualized gameplay-building placements in the three shelf AABBs | None (`semanticCategory` 1). Those boxes only have roof infrastructure (pipeline platforms). |

The plate is a **resident SubScene gameplay building**: `SM_Bld_Plinth_01` (Polygon Military, mesh GUID `414bf6d85af7c184192805761a93f425`, sand material `48843a2db7a47754f9c8d6ae390ebf4b`, `m_CastShadows: 1`). That mesh is **absent** from `OperationMapRenderDatabaseBakeConfig`, so virtualization does not strip it to a proxy. The intact renderer stays on the building entity at the stale hill Y.

Pinned statue-side geometry (world metres; grid origin is 1 m/cell):

| Role | Building | World (x, y, z) | Grid (x, z) | Authored Y |
|---|---|---|---|---:|
| Civic hall / camera D look-at | `Building_0020_SM_Bld_Hall_01` | (1031.03, 7.01, 539.84) | (1031, 540) | 7.01 m |
| Civic sand plate (camera D) | `Building_0078_SM_Bld_Plinth_01 (6)` | (1043.26, 6.20, 488.48) | (1043, 488) | 6.20 m |
| Northern sand plate (camera A) | `Building_0060_SM_Bld_Plinth_01 (6)` | (964.98, 6.20, 687.84) | (965, 688) | 6.20 m |
| East stamp sibling | `Building_0069_SM_Bld_Plinth_01 (6)` | (1082.46, 6.20, 640.92) | (1082, 641) | 6.20 m |

The same three archived-hill stamps host a cluster of shops, extra plinths, and two water tanks at 4.38–7.82 m. Shops that *are* in the render database still have gameplay `LocalTransform` at the stale Y; lowering those roots corrects selection/combat height. The sand plinths and hall are the meshes the player sees floating.

Synty `SM_Prop_Statue_*` objects in this SubScene sit west of the playable window (x ≤ 750). The “golden statue / base join” in the shots is the civic hall cluster at camera D, not those western statues.

## Fix

Two runtime-only corrections. On-disk surface bytes, the 230 MB entity presentation YAML, and frozen M1 source hashes are unchanged.

### 1. Resident building Y

`CityCrossroadsFloatingShelfBuildingCorrection` drops `OperationMapBuildingComponent` `LocalTransform` Y to authored grade (0.01 m) when:

- world XZ is inside one of the three conservative stamp unions
  - North: (952,646)–(1033,710)
  - Civic / statue-side: (1008,476)–(1084,562)
  - East: (1050,566)–(1123,653)
- Y is in [3.5, 9.5] m

`CityCrossroadsFloatingShelfBuildingCorrectionSystem` applies that Burst `IJobEntity` in `SimulationSystemGroup` before building destruction. Already-corrected buildings no-op because they fall out of the raised band. The mountain cell (768, 9.1, 550) is outside the AABBs.

### 2. Surface remnant skirts

`MapSurfaceFloatingShelfCorrection` remnant absorb now takes leftover 1.5–3.99 m rims that still drop ≥ 1.45 m onto the collapsed grade (was 3.0–3.99 m / 2.5 m). The 2.16 m skirts at (975,646)–(1033,667), (1061,498)–(1084,556) and (1099,572)–(1123,630) therefore follow the stamps. Mountain (768,550) stays 9.10 m.

## Pinned hashes

| Path | Role | SHA-256 / identity |
|---|---|---|
| `Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset` | Shared bake, unchanged bytes | `1402d769704008e254563ff7ecda835294db83afc2cee6d5bb456987f0392b4d` GUID `12f517deb32ab49698acbfdaf7c3eac7` |
| `Assets/Game/Scripts/Components/MapSurfaceFloatingShelfCorrection.cs` | Surface correction + 1.5 m rim absorb | `a7c39568afbe258fc6aa6486a0a1dac1199c70b6be6a83de3e75268a4d6ee4d4` |
| `Assets/Game/Scripts/Components/CityCrossroadsFloatingShelfBuildingCorrection.cs` | Statue-side building Y contract | `65250c70bc600adaf9bfb324d07aeeb657f593a4cde4ecb7c7ebbdc88ce53309` |
| `Assets/Game/Scripts/Systems/CityCrossroadsFloatingShelfBuildingCorrectionSystem.cs` | Runtime ECS pass | `f2098311289bc54dbd40e062957e61b8eb44a807ee4f3e9c0170f6c2b71a598c` |
| `Assets/Game/Scripts/Configs/MapSurfaceDataAsset.cs` | Load hook, unchanged | `ed922291f83299d64a33e9c01ce8c61defcf582f967eb83aa5347fa17044a06f` |
| `Assets/Game/Scripts/Editor/CityCrossroadsFloatingShelfValidation.cs` | Focused gate (15 cases) | `45dd4ca64719339e1fd5f3d82ac354509f9942168d2ed5d0ec3f149b6eab84cd` |
| `Assets/Tests/Editor/MapSurfaceFloatingShelfCorrectionTests.cs` | Tests | `a9543dfe4e5b2feded5d6ddd7fd0928ea42967c82e84debce04b9f57d4b57cc4` |
| `Assets/Game/Configs/Skirmish/CityCrossroads/OperationMap_CityCrossroads.asset` | CC map, unchanged | `4a8735f11b58f825512c27d57ab37662a33d5f7da1733f00398021ecf9426b90` |

Surface metadata still recorded on the CC map: `contentHash=1402d769…`, `runtimeBlobHash=60a44d9bef6cfd87859429865716a462`. Those describe the on-disk payload, which this change does not rewrite.

## Before / after

| Location | Surface raw → runtime | Building authored Y → runtime |
|---|---|---|
| Civic hall (1031, 540) | 6.439 → 0.009 m | 7.01 → 0.01 m |
| Civic plinth (1043, 488) | 6.119 → 0.009 m | 6.20 → 0.01 m |
| Northern plinth (965, 688) | 6.119 → 0.009 m | 6.20 → 0.01 m |
| North skirt (1000, 655) | ~2.16 → 0.009 m | (no plate root) |
| Civic skirt (1072, 520) | ~2.18 → 0.009 m | (no plate root) |
| East skirt (1110, 600) | ~2.18 → 0.009 m | (no plate root) |
| Mountain (768, 550) | 9.099 unchanged | n/a |

Camera A / D remain the QA shots. This cloud workspace has no licensed Unity Editor, so Game View was not recaptured here. The new gate pins the plate world/grid coordinates, asserts surface and building-Y contracts agree at grade, and stream-checks that the dense-city entity presentation still authors the three named buildings.

## Shared-mode impact

- **Surface remnant absorb** runs for every consumer of `Match_Map_MapSurfaceData.asset`: City Crossroads, Desert Base / Skirmish 1, and M1–M5 maps that bind the same bake. Only leftover 1.5–3.99 m rims beside already-collapsed unsupported shelves change. The Skirmish 1 mountain peak and its illegal 8×8 lot stay raised.
- **Building Y correction** is AABB-scoped to the three City Crossroads archived-hill stamps. Desert Base / M1–M5 gameplay west of x=900 is untouched. The shared dense-city entity scene YAML is not rewritten.
- Virtualized roof infrastructure (pipeline platforms at 7–13 m) is unchanged. Those are rooftop props, not the sand plate.
- Western Synty statues (x=408–750, y≈5.8–10.6 m) remain a residual risk on Desert Base views of archived hills; they are outside this City Crossroads plate.

Campaign configs, both small skirmish presets, and M1–M5 authored content were not rewritten.

## Validation

Focused tests added/extended:

- synthetic shelf collapse
- 3.7 m rim absorb
- **2.16 m leftover skirt absorb**
- gradual mountain preservation
- isolated spike ignore
- live City Crossroads / Skirmish 1 cells after blob load, including statue-side (1031,540), (1043,488), (965,688) and remnant skirts (1000,655), (1072,520), (1110,600)
- **statue-side hall/plinth `TryCorrect` plus mountain reject**

Editor executeMethod: `Game.Editor.CityCrossroadsFloatingShelfValidation.RunFocusedValidation`
Pass marker: `[CityCrossroadsFloatingShelfValidation] result=Passed cases=15`

The fifteen cases cover asset identity, the old northern/civic samples, the statue-side plate cells, remnant skirts, bases, mountain, placement legality, building-Y geometry, and a stream pin of the three named SubScene buildings.

Saves were not opened. No profile or Quick Game override was used.

Not run in this environment: Unity compile, Play Mode, EN/FA matches, Game View recapture, recruitment camera, exchange, terminal voices, device certification. Those remain for the Unity lane.

## Residual risk

A later official rebake of the shared surface plus a rebake of the dense-city entity presentation (lowering the resident plinth/hall roots in YAML) should replace these load-time / runtime passes. Until then, do not treat on-disk surface heights or authored building Y as the runtime values inside the three stamps.

A few small 1.5–3 m pads remain after remnant absorb (~246 cells, not adjacent enough to the collapsed seeds). They are not the statue-side plate.

Western Desert Base statues on archived hills were left scoped out of this City Crossroads fix.
