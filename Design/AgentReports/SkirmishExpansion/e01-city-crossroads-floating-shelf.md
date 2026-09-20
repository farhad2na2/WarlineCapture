# E0.1 City Crossroads floating shelf

Date: 2026-09-20
Baseline: `codex/m03-radar-warning` `61d7fcd12f767a03ea0eac4f04e90c51971b3420`
Head: `cursor/e01-cc-resident-plate-6b62` (off `cursor/e01-cc-floating-shelf-34c4` `13d277fb`)
PR: follow-up off https://github.com/farhad2na2/WarlineCapture/pull/19
Workflow path: pull request
Task: remove the player-visible City Crossroads floating ground plate at the statue / base join, and make surface plus resident geometry agree there. Do not treat the earlier northern/civic height samples or the building-Y-only pass as sufficient.

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

The first statue-side pass treated the plate as a **resident SubScene gameplay building** (`SM_Bld_Plinth_01`). That was incomplete. Mac Game View on tip `13d277fb` still showed the hard-edged tan plate after building `LocalTransform` Y was slammed to 0.01 m.

The player-visible rectangle is a **RenderOnly** leftover sand tile, not an `OperationMapBuilding`:

| Role | Name | World (x, y, z) after parent rotation | Notes |
|---|---|---|---|
| Civic plate (camera D) | `SM_Env_Ground_Square_01 (8)` | (1033.16, 5.85, 509.37) | scale ≈ (1.52, 1, 2.94); sand material; `m_IsActive: 1`; role RenderOnly |
| Civic golden statue | `SM_Prop_Statue_01 (2)` | (1026.72, 10.59, 523.35) | stands on the plate |
| Civic pedestal | `SM_Prop_Statue_Base_02` | (1026.63, 5.89, 523.35) | cylindrical plinth in the screenshot |
| Northern plate (camera A / base join) | `SM_Env_Ground_Square_01 (8)` | (985.87, 5.85, 697.94) | same scaled tile family |
| Northern statue | `SM_Prop_Statue_01 (2)` | (999.85, 10.59, 704.38) | player-base join |

`SM_Env_Ground_Square_01`, `SM_Bld_Plinth_01`, `SM_Bld_Hall_01`, and `SM_Prop_Statue_*` are **absent** from `OperationMapRenderDatabaseBakeConfig`. Virtualization does not convert them to proxies. They keep SubScene `MeshRenderer`s.

They are parented under the rotated dense-city map root (`m_LocalPosition` XZ is local, e.g. `(-96.07, 5.85, 333.16)`). A previous walk that added local+parent without rotation placed every statue at x ≤ 750 and discarded them. With the parent quaternion applied they sit inside the City Crossroads stamps.

`CityCrossroadsFloatingShelfBuildingCorrectionSystem` only queried `OperationMapBuildingComponent` `LocalTransform` as if it were world space. That moves gameplay building *roots* (hall/plinth selection height) but never the RenderOnly sand tile or statue. Children of those buildings have local Y = 0 and already follow their parent; the floating plate does not have that component.

Pinned statue-side geometry (world metres; grid origin is 1 m/cell):

| Role | Building | World (x, y, z) | Grid (x, z) | Authored Y |
|---|---|---|---|---:|
| Civic hall / camera D look-at | `Building_0020_SM_Bld_Hall_01` | (1031.03, 7.01, 539.84) | (1031, 540) | 7.01 m |
| Civic sand plate (camera D) | `Building_0078_SM_Bld_Plinth_01 (6)` | (1043.26, 6.20, 488.48) | (1043, 488) | 6.20 m |
| Northern sand plate (camera A) | `Building_0060_SM_Bld_Plinth_01 (6)` | (964.98, 6.20, 687.84) | (965, 688) | 6.20 m |
| East stamp sibling | `Building_0069_SM_Bld_Plinth_01 (6)` | (1082.46, 6.20, 640.92) | (1082, 641) | 6.20 m |

The same three archived-hill stamps host a cluster of shops, extra plinths, and two water tanks at 4.38–7.82 m. Shops that *are* in the render database still have gameplay `LocalTransform` at the stale Y; lowering those roots corrects selection/combat height. The sand plinths and hall are the meshes the player sees floating.

Synty `SM_Prop_Statue_*` objects looked western (x ≤ 750) only when parent rotation was ignored. After walking the full local-to-world matrix they sit on the civic and northern leftover sand tiles listed above. That is the golden statue Farhad photographed.

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

### 1b. Resident RenderOnly plate / statue Y (the Game View hole)

A second Burst pass now lowers **non-building** `LocalTransform` + `LocalToWorld` in the same three AABBs when:

- world XZ (from `LocalToWorld.Position`) is inside a stamp
- world Y is in [3.5, 11.5] m (statues sit at 10.59 m)
- **local** Y is also ≥ 3.5 m, so parented children with local Y ≈ 0 follow once and are not dropped twice

Those entities receive a uniform delta of `0.01 − 5.85 = −5.84` m (the leftover `Ground_Square` height). The civic tile lands on grade; the statue keeps ~4.75 m of pedestal height. Gameplay building roots stay on the slam-to-0.01 path so hall/plinth origins do not float.

Unparented additional render entities (`MaterialMeshInfo` without `LocalTransform` / `Parent` / proxy slot) get the same world-matrix Y delta.

Movers (`UnitMove`, `UnitAirComponent`), authored vehicles, virtualized proxy slots, and rooftop pipeline placements (render-DB WorldMatrix at 7–13 m) are excluded. Powerline proxies at 6.20 m around the northern base stay put.

### 2. Surface remnant skirts

`MapSurfaceFloatingShelfCorrection` remnant absorb now takes leftover 1.5–3.99 m rims that still drop ≥ 1.45 m onto the collapsed grade (was 3.0–3.99 m / 2.5 m). The 2.16 m skirts at (975,646)–(1033,667), (1061,498)–(1084,556) and (1099,572)–(1123,630) therefore follow the stamps. Mountain (768,550) stays 9.10 m.

## Pinned hashes

| Path | Role | SHA-256 / identity |
|---|---|---|
| `Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset` | Shared bake, unchanged bytes | `1402d769704008e254563ff7ecda835294db83afc2cee6d5bb456987f0392b4d` GUID `12f517deb32ab49698acbfdaf7c3eac7` |
| `Assets/Game/Scripts/Components/MapSurfaceFloatingShelfCorrection.cs` | Surface correction + 1.5 m rim absorb | `a7c39568afbe258fc6aa6486a0a1dac1199c70b6be6a83de3e75268a4d6ee4d4` |
| `Assets/Game/Scripts/Components/CityCrossroadsFloatingShelfBuildingCorrection.cs` | Building slam + resident world-space plate/statue delta | `a6245975b1c428590398aaa357fbcc9d61f7dcef6b9055b3c25b7bdd214c7f70` |
| `Assets/Game/Scripts/Systems/CityCrossroadsFloatingShelfBuildingCorrectionSystem.cs` | Runtime ECS pass (buildings + RenderOnly visuals) | `8573e82625ead3c1265e780a5578da06b4b10bedbf58e8decf27c9db9c23ebb8` |
| `Assets/Game/Scripts/Configs/MapSurfaceDataAsset.cs` | Load hook, unchanged | `ed922291f83299d64a33e9c01ce8c61defcf582f967eb83aa5347fa17044a06f` |
| `Assets/Game/Scripts/Editor/CityCrossroadsFloatingShelfValidation.cs` | Focused gate (18 cases) | `3cbeada4e5169c83ae5dc489bbe3d47d2d1b470850dd26853edacb7d78a96551` |
| `Assets/Tests/Editor/MapSurfaceFloatingShelfCorrectionTests.cs` | Tests | `fe71452a999525b66cce131c2448886297d98f57fc917197e4d922f25efb7170` |
| `Assets/Game/Configs/Skirmish/CityCrossroads/OperationMap_CityCrossroads.asset` | CC map, unchanged | `4a8735f11b58f825512c27d57ab37662a33d5f7da1733f00398021ecf9426b90` |

Surface metadata still recorded on the CC map: `contentHash=1402d769…`, `runtimeBlobHash=60a44d9bef6cfd87859429865716a462`. Those describe the on-disk payload, which this change does not rewrite.

## Before / after

| Location | Surface raw → runtime | Visible mesh authored Y → runtime |
|---|---|---|
| Civic `Ground_Square` (1033, 509) | already grade | 5.85 → 0.01 m (delta −5.84) |
| Civic statue (1027, 523) | n/a | 10.59 → 4.75 m |
| Civic statue base (1027, 523) | n/a | 5.89 → 0.05 m |
| Civic hall (1031, 540) | 6.439 → 0.009 m | 7.01 → 0.01 m (building slam) |
| Civic plinth (1043, 488) | 6.119 → 0.009 m | 6.20 → 0.01 m (building slam) |
| Northern `Ground_Square` (986, 698) | already grade | 5.85 → 0.01 m |
| Northern statue (1000, 704) | n/a | 10.59 → 4.75 m |
| Mountain (768, 550) | 9.099 unchanged | n/a |

Camera A / D remain the QA shots. Frame the civic statue at `(1026.7, 10.6, 523.4)` and the northern base-join statue at `(999.9, 10.6, 704.4)`. This cloud workspace has no licensed Unity Editor, so Game View was not recaptured here. The gate now also pins the RenderOnly tile/statue world coordinates and forbids double-correcting parented children.

## Shared-mode impact

- **Surface remnant absorb** runs for every consumer of `Match_Map_MapSurfaceData.asset`: City Crossroads, Desert Base / Skirmish 1, and M1–M5 maps that bind the same bake. Only leftover 1.5–3.99 m rims beside already-collapsed unsupported shelves change. The Skirmish 1 mountain peak and its illegal 8×8 lot stay raised.
- **Building Y correction** is AABB-scoped to the three City Crossroads archived-hill stamps. Desert Base / M1–M5 gameplay west of x=900 is untouched. The shared dense-city entity scene YAML is not rewritten.
- **Resident visual delta** uses the same AABBs and world-space tests. Only leftover RenderOnly tiles/statues/props with raised local Y move. Virtualized roof infrastructure (pipeline platforms at 7–13 m) and powerline proxies stay on render-DB `WorldMatrix`.
- Two Desert Base statues remain west of the CC window after rotation (`≈(552, 374)` and `≈(600, 363)`). They are outside this City Crossroads plate.

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
- **resident `Ground_Square` / golden-statue world-space delta, child no-op, mountain reject**

Editor executeMethod: `Game.Editor.CityCrossroadsFloatingShelfValidation.RunFocusedValidation`
Pass marker: `[CityCrossroadsFloatingShelfValidation] result=Passed cases=18`

The eighteen cases cover asset identity, the old northern/civic samples, the statue-side plate cells, remnant skirts, bases, mountain, placement legality, building-Y geometry, the RenderOnly sand-tile/statue contract, and stream pins of the hall/plinth plus `SM_Env_Ground_Square_01 (8)` / `SM_Prop_Statue_01 (2)` / `SM_Prop_Statue_Base_02`.

Saves were not opened. No profile or Quick Game override was used.

Not run in this environment: Unity compile, Play Mode, EN/FA matches, Game View recapture, recruitment camera, exchange, terminal voices, device certification. Those remain for the Unity lane.

## Residual risk

A later official rebake of the shared surface plus a rebake of the dense-city entity presentation (lowering the resident `Ground_Square` / statue / plinth / hall roots in YAML) should replace these load-time / runtime passes. Until then, do not treat on-disk surface heights, authored building Y, or parent-local XZ as the runtime Game View pose inside the three stamps.

A few small 1.5–3 m pads remain after remnant absorb (~246 cells, not adjacent enough to the collapsed seeds). They are not the statue-side plate.

Mac Game View on seed 104729 is still required before merge. Do not treat the 18-case gate as a substitute for framing the civic statue plate.
