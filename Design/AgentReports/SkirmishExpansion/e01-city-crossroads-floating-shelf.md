# E0.1 City Crossroads floating shelf

Date: 2026-09-20
Baseline: `codex/m03-radar-warning` `61d7fcd12f767a03ea0eac4f04e90c51971b3420`
Head: `0fb60f4b4d797ca3f71f599b42ac1361da53eeaa`
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

The cancelled-run “2048-wide stride remap” diagnosis was independently rejected: compact index `x + y * 2048` is already correct. A decoder offset bug can invent false shelves; the live payload shelves sit at the coordinates above.

Real mountains were left alone. The Skirmish 1 test peak at (768,550) stays at 9.10 m, and the (766,550) 8×8 mountain footprint remains illegal for placement.

## Fix

`MapSurfaceFloatingShelfCorrection` runs once when the compact (or regular single-layer) surface blob is created. It flood-fills high plateaus and flattens only components that:

- contain 64–6000 cells
- have mean height ≥ 4.5 m
- have ≥ 5 % of cells dropping ≥ 3 m onto grade < 1.5 m
- have ≤ 22 % gradual (0.3–1.5 m) neighbour drops

Flattened cells take the median neighbouring grade height and an upright normal. A second pass then absorbs leftover 3.0–3.99 m rim cells that still drop ≥ 2.5 m onto that grade. Those rims sat just under the 4 m flood threshold and would otherwise remain as a one-cell floating edge.

On-disk surface bytes and frozen M1 source hashes are unchanged; the published runtime sample is corrected for every mode that loads this bake.

A fourth stale city shelf at (533,331)–(592,407) matches the same class and is corrected with the same pass. It is outside the City Crossroads playable window.

Independent Python decode of the live compact payload (v3 / encoding 1, 2048×1024, minHeight −5.31082, step 0.01) reproduced the C# pass:

- 11,952 cells flattened (10,739 shelf + 1,213 rim)
- 7,700 of the original shelf cells sit in the CC playable window (900–1300, 260–875)
- after correction: 0 cells ≥ 4 m and 0 ≥ 3 m cliffs onto grade in that window
- mountain (768,550) remains 9.10 m; 8×8 at (766,550) stays illegal
- 8×8 lots at (800,600), (831,596), (1020,730), (1020,740) and (1100,400) stay legal under the 8° / 0.5 m / 0.75 m placement gates
- previously illegal shelf lots (1000,690) and (1024,520) become legal once flattened
- road / blocked type bits are unchanged; 4,283 roadish cells only change height/normal

## Pinned hashes

| Path | Role | SHA-256 / identity |
|---|---|---|
| `Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset` | Shared bake, unchanged bytes | `1402d769704008e254563ff7ecda835294db83afc2cee6d5bb456987f0392b4d` GUID `12f517deb32ab49698acbfdaf7c3eac7` |
| `Assets/Game/Scripts/Components/MapSurfaceFloatingShelfCorrection.cs` | Correction + rim absorb | `072b259ac19d63f11975a795d98cdd7c86233666a6e12d8cd06684041f791a56` |
| `Assets/Game/Scripts/Configs/MapSurfaceDataAsset.cs` | Load hook | `ed922291f83299d64a33e9c01ce8c61defcf582f967eb83aa5347fa17044a06f` |
| `Assets/Game/Scripts/Editor/CityCrossroadsFloatingShelfValidation.cs` | Focused gate | `ab90085482425d0f2d369c55a7601ad2532f570db3e2a1e45bb04051ca2eb9f7` |
| `Assets/Tests/Editor/MapSurfaceFloatingShelfCorrectionTests.cs` | Tests | `e29e066aa873f2b730be6516e5fc340f6a85f9af19c94d4201d1871e77c026ee` |
| `Assets/Game/Configs/Skirmish/CityCrossroads/OperationMap_CityCrossroads.asset` | CC map, unchanged | `4a8735f11b58f825512c27d57ab37662a33d5f7da1733f00398021ecf9426b90` |
| `Assets/Game/Resources/SkirmishCityCrossroads.asset` | CC preset, unchanged | `7fd1694010a1057dfcf53fa584047fbf97abc7c5075d1d8efcefeafa60739e88` |
| `Assets/Game/Configs/Skirmish/CityCrossroads/InitialForces.asset` | CC forces, unchanged | `8002a61b8ea995b25ce3c8d3144bfba26ad4ae6ef7e6aa36f7f11bb9d04d569c` |

Surface metadata still recorded on the CC map: `contentHash=1402d769…`, `runtimeBlobHash=60a44d9bef6cfd87859429865716a462`. Those describe the on-disk payload, which this change does not rewrite.

## Before / after

Colour heightfields and camera-matched surface renders (not Unity Game-view screenshots) were generated from the live bake plus the same correction the runtime now applies.

Camera A, start / mid pitch: position `(1020, 70, 680)`, look-at `(1020, 0, 735)`, FOV 50. Matches `camera.skirmish.city_crossroads.start`. The 5.86 m shelf occupied the lower third of the frame, just south of the player barracks.

Camera B, low east edge: `(1040, 12, 720)` looking at `(1000, 0, 690)`.

Camera C, low west edge: `(960, 8, 690)` looking at `(1010, 0, 700)`.

Camera D, civic south: `(1035, 18, 500)` looking at `(1035, 0, 540)`.

Evidence files (walkthrough artefacts):

| File | SHA-256 |
|---|---|
| `cc_playable_height_before_after.png` | `63e92e89dcccf325a1ed8ae8e7827044d6437eda632daf7059140095562dd7e1` |
| `cc_northern_shelf_before_after.png` | `3d099f59542fdf45b4985d26e50c1c15addde29d50a5dcc6f309133484d16af5` |
| `cc_civic_shelves_before_after.png` | `b66d993ed02208ec747ffbef3ee9390681bcd88c9f337087226b2971aa2229a4` |
| `cc_cameras_before_after_sheet.png` | `aee70573b7641868e15e5904f09a1916a5fffc876f28081d7f1bbb94f5d6c465` |
| `s1_mountain_before_after.png` | `6d208690586c6da4163e01727a76310aeb2aca8b9a3a696041376740d64d7e16` |
| `cc_flattened_cells_overlay.png` | `12666606a3211bf4a4a6f0cd7f8ace96445aa8b398b68a47fe1f6351580c0e19` |

After the load-time correction the northern 710–690 / 990–1040 window is authored grade. Civic 3.9 m leftover rims at (1035,482) and (1028,480) also sit on grade. The Skirmish 1 mountain pair is byte-identical before/after.

Unity Game-view screenshots were not captured here: this cloud workspace has no licensed Unity Editor. QA should still shoot cameras A–D in Play Mode on City Crossroads seed 104729 before merge.

## Shared-mode impact

The correction runs for every consumer of `Match_Map_MapSurfaceData.asset`: City Crossroads, Desert Base / Skirmish 1, and M1–M5 maps that bind the same bake. Campaign configs were not rewritten. Mountain cells used by Skirmish 1 placement tests stay raised. Roads that sat on the stale plateaus now sample the same grade as the visible city carriageway.

## Validation

Focused tests added:

- synthetic shelf collapse
- just-under-threshold rim absorb
- gradual mountain preservation
- isolated spike ignore
- live City Crossroads / Skirmish 1 cells after blob load, including civic rim (1035,482)

Editor executeMethod: `Game.Editor.CityCrossroadsFloatingShelfValidation.RunFocusedValidation`
Pass marker: `[CityCrossroadsFloatingShelfValidation] result=Passed cases=11`

Independent offline decode of the published bake plus the C# thresholds reproduced all eleven focused cases. Saves were not opened. No profile or Quick Game override was used.

Not run in this environment: Unity compile, Play Mode, EN/FA matches, recruitment camera, exchange, terminal voices, device certification. Those remain for the Unity lane. Placement/selection/preview/recruitment/exchange/voice behaviour was not re-executed in Editor; only the surface heights those systems sample were rechecked.

## Residual risk

A later official rebake of the shared surface should replace this load-time pass. Until then, do not treat the on-disk payload heights as the runtime heights for the four flattened shelves. A few 2–3 m civic pads remain; they are below the remnant threshold and are not the reported 5.85 m floating shelf.
