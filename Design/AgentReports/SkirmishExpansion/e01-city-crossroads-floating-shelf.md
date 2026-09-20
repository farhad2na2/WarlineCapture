# E0.1 City Crossroads floating shelf

Date: 2026-09-20
Baseline: `codex/m03-radar-warning` `61d7fcd12f767a03ea0eac4f04e90c51971b3420`
Head: `16b0dc867956010c1dd6378c874a07dc1001f972`
PR: https://github.com/farhad2na2/WarlineCapture/pull/19
Workflow path: pull request
Task: collapse the City Crossroads raised-ground / floating-edge defect so authored visible grade and surface/foundation data agree.

## Diagnosis

City Crossroads reuses the dense-city physical source (`opmap.skirmish.desert_base_01`, scene GUID `dad0bd13fb20943dfb2f881cbe225f05`) and the shared bake `Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset`.

Dense-city visual grading archived interior hill/dune GameObjects so the retained city base sits at authored grade (~0.01 m). The runtime entity scene no longer contains `Ground_Hill` / `SandDunes` objects. The shared surface blob was not rebaked. Three plateaus therefore remain in the City Crossroads playable window at about 5.85–6.2 m, with a one-cell cliff onto 0.01 m grade:

| Shelf | BBox (x,z) | Mean height | Notes |
|---|---|---:|---|
| Northern base / civic north | (959,663)–(1026,710) | 5.86 m | Immediately south of player staging (1020,750). Sample (1010,710)=5.859 m vs (1011,710)=0.009 m |
| Civic centre | (1008,480)–(1067,557) | 6.01 m | Civic scenery south of the boulevard |
| Civic east | (1050,572)–(1105,642) | 6.14 m | East of the civic hall |

Surface types on those plateaus mix Terrain, Road and Blocked. Movement masks still allow ground travel, so units and foundations sample the stale height and float above the visible city grade. This is a geometry/surface disagreement, not a shadow-only artefact.

Real mountains were left alone. The Skirmish 1 test peak at (768,550) stays at 9.10 m, and the (766,550) 8×8 mountain footprint remains illegal for placement.

## Fix

`MapSurfaceFloatingShelfCorrection` runs once when the compact (or regular single-layer) surface blob is created. It flood-fills high plateaus and flattens only components that:

- contain 64–6000 cells
- have mean height ≥ 4.5 m
- have ≥ 5 % of cells dropping ≥ 3 m onto grade < 1.5 m
- have ≤ 22 % gradual (0.3–1.5 m) neighbour drops

Flattened cells take the median neighbouring grade height and an upright normal. On-disk surface bytes and frozen M1 source hashes are unchanged; the published runtime sample is corrected for every mode that loads this bake.

A fourth stale city shelf at (533,331)–(592,407) matches the same class and is corrected with the same pass. It is outside the City Crossroads playable window.

## Pinned hashes

| Path | Role | SHA-256 / identity |
|---|---|---|
| `Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset` | Shared bake, unchanged bytes | `1402d769704008e254563ff7ecda835294db83afc2cee6d5bb456987f0392b4d` GUID `12f517deb32ab49698acbfdaf7c3eac7` |
| `Assets/Game/Scripts/Components/MapSurfaceFloatingShelfCorrection.cs` | Correction | `a1dafcb6057c6fa06b9ad86863526bb710a178e5845b82ea82e29b01318fdb79` |
| `Assets/Game/Scripts/Configs/MapSurfaceDataAsset.cs` | Load hook | `ed922291f83299d64a33e9c01ce8c61defcf582f967eb83aa5347fa17044a06f` |
| `Assets/Game/Scripts/Editor/CityCrossroadsFloatingShelfValidation.cs` | Focused gate | `49efcab0062cf1476ed5f7001eb1e48217768adaa6218fe7e703c69ad9909b01` |
| `Assets/Tests/Editor/MapSurfaceFloatingShelfCorrectionTests.cs` | Tests | `7c04422d000dbd9a664b94b22ca2baf5cd4093d138356c14c462441017d829a5` |
| `Assets/Game/Configs/Skirmish/CityCrossroads/OperationMap_CityCrossroads.asset` | CC map, unchanged | `4a8735f11b58f825512c27d57ab37662a33d5f7da1733f00398021ecf9426b90` |
| `Assets/Game/Resources/SkirmishCityCrossroads.asset` | CC preset, unchanged | `7fd1694010a1057dfcf53fa584047fbf97abc7c5075d1d8efcefeafa60739e88` |
| `Assets/Game/Configs/Skirmish/CityCrossroads/InitialForces.asset` | CC forces, unchanged | `8002a61b8ea995b25ce3c8d3144bfba26ad4ae6ef7e6aa36f7f11bb9d04d569c` |

Surface metadata still recorded on the CC map: `contentHash=1402d769…`, `runtimeBlobHash=60a44d9bef6cfd87859429865716a462`. Those describe the on-disk payload, which this change does not rewrite.

## Before / after (northern shelf, looking across the start camera)

Legend: `.` < 0.3 m (authored grade), `@` 5.5–6.2 m shelf, `X` > 6.2 m.

Camera A, start / mid pitch: position `(1020, 70, 680)`, look-at `(1020, 0, 735)`, FOV 50. The shelf occupies the lower third of the frame, just south of the player barracks.

Camera B, low east edge: `(1040, 12, 720)` looking at `(1000, 0, 690)`.

Camera C, low west edge: `(960, 8, 690)` looking at `(1010, 0, 700)`.

Camera D, civic south: `(1035, 18, 500)` looking at `(1035, 0, 540)`.

Before, z 710–690, x 990–1040:

```
 710 .....@@@@@@@@@@@@@@@@..............................
 709 .....@@@@@@@@@@@@@@@@..............................
 708 @@@@@@@@@@@@@@@@@@@@@..............................
 707 @@@@@@@@@@@@@@@@@@@@@..............................
 706 @@@@@@@@@@@@X@@X@@@@@..............................
 …
 690 @@@@@@XXX@XXXX@@@@@@@..............................
```

After the load-time correction the same window is authored grade:

```
 710 ...................................................
 …
 690 ...................................................
```

Unity Game-view screenshots were not captured here: this cloud workspace has no licensed Unity Editor. QA should shoot cameras A–D in Play Mode on City Crossroads seed 104729 before merge.

## Shared-mode impact

The correction runs for every consumer of `Match_Map_MapSurfaceData.asset`: City Crossroads, Desert Base / Skirmish 1, and M1–M5 maps that bind the same bake. Campaign configs were not rewritten. Mountain cells used by Skirmish 1 placement tests stay raised. Roads that sat on the stale plateaus now sample the same grade as the visible city carriageway.

## Validation

Focused tests added:

- synthetic shelf collapse
- gradual mountain preservation
- isolated spike ignore
- live City Crossroads / Skirmish 1 cells after blob load

Editor executeMethod: `Game.Editor.CityCrossroadsFloatingShelfValidation.RunFocusedValidation`
Pass marker: `[CityCrossroadsFloatingShelfValidation] result=Passed cases=10`

Saves were not opened. No profile or Quick Game override was used.

Not run in this environment: Unity compile, Play Mode, EN/FA matches, recruitment camera, exchange, terminal voices, device certification. Those remain for the Unity lane.

## Residual risk

A later official rebake of the shared surface should replace this load-time pass. Until then, do not treat the on-disk payload heights as the runtime heights for the four flattened shelves.
