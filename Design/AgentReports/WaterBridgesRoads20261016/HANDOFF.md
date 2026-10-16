> Completed continuation: see [COMPLETION.md](COMPLETION.md). The following handoff is preserved as historical context; its pause, outstanding gaps and no-scene-edit restriction were superseded by the resumed task and explicit owner approval.

# Handoff: Water, Bridges and Road-Join Visual Upgrade (2026-10-16)

## Where the work lives

- Worktree: `/Volumes/T7/Projects/WarlineCapture-CampaignVisualAudit`
- Branch: `codex/water-bridges-roads-20261016` (based on `74433052b`). Nothing is committed yet.
- Draft copies of the two edited source files are mirrored in `/private/tmp/wb-draft/`. They are temporary; the worktree is the source of truth.
- The worktree stash `codex-water-bridges-preserve-fighter-camera-20261016` holds unrelated fighter-camera work that was preserved before this branch was checked out. Do not drop it.
- Approved mockups (the visual target) are in the main checkout: `/Volumes/T7/Projects/WarlineCapture/Design/MapVariants/Beautify/WaterBridges/water-bridges-mockup-v01-{player-view,canal-end,port}.jpg`.

## What the owner asked for and approved

1. Visual improvements to the Campaign maps. The owner chose:
   - **render-only** scope: shaders, textures, decals and prop regrouping, with no geometry or scene changes;
   - water and bridges first;
   - mockups first.
2. The mockups were approved ("yes I approve") for all water maps:
   - AshLinePort;
   - CH05M02TrustUnderFire;
   - DenseCity canal;
   - CH05M01CitywideAlert, CH05M03NetworkCollapse, CH05M04LastCorridor and CH05M05CommandNode.
3. Added request: "fix the dirt road to asfalt road transitions. currently the look bad and straght line, it should be gradual and convincing."
4. At the last message the owner said **"wait"**. Do not start new Unity runs or broaden scope without checking with the owner first.

## Hard rules (from AGENTS.md; must keep)

- Unity on macOS only runs through the repository wrapper, with GUI licensing. Never use `-batchmode`, never invoke the Unity binary directly, and never reset IPC or kill Unity/Hub processes.
- `ProjectSettings/ProjectVersion.txt` has CRLF line endings, so the wrapper cannot resolve the Editor version by itself (it exits with 127). Always pass `--unity` explicitly. The shared file was deliberately left unedited; tell the owner about it.

```bash
cd /Volumes/T7/Projects/WarlineCapture-CampaignVisualAudit
Tools/CI/invoke_unity_macos.sh --unity /Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/MacOS/Unity \
  --timeout 7200 --log "$PWD/Design/AgentReports/WaterBridgesRoads20261016/validation/apply-NN.log" -- \
  -quit -executeMethod Game.Editor.MapVariants.OtherMapSurfacePolish.ApplyWaterAndRoadBlend
```

- Render-only discipline:
  - Mesh assets are edited in place (same GUID).
  - Only map-owned materials are edited (`MapOwnedMaterial`).
  - Shared templates are never touched, for example `DenseCity/WornAsphalt.mat` and the `Landscape/*_Water.mat` files.
  - Every run verifies that each source scene's SHA-256 is unchanged and fails if any edited material is shared across maps.
- Campaign map reuse policy: no new terrain, roads, bridges or map derivatives.
- Keep failed and superseded evidence. Report these gates separately: visual review, automated checks, normal-input playthrough, and device acceptance.
- Another agent's Unity 6000.5.2f1 batch process on `WarlineCapture-P2` may be running. Leave it alone.

## Files changed

| File | Change |
| --- | --- |
| `Assets/Game/Rendering/Shaders/CampaignWater.shader` | Channel-aware water (see below). The legacy path for meshes without UVs is unchanged. |
| `Assets/Game/Rendering/Shaders/GroundMacroVariation.shader` | Road-join blend block (`_RoadBlend*` properties). It also suppresses the old `_RoadTerminalStrength` straight-edged dust wherever the blend is present. |
| `Assets/Game/Scripts/Editor/MapPrototypes/Variants/OtherMapSurfacePolish.WaterRoadBlend.cs` (new) | Census and apply pipeline, captures, reports, and road-blend mask generation. |
| `Assets/Game/Art/MapBeautify/CampaignVisualFixes20261016/RoadBlend/<map>.png` (new) | Per-map RGBA linear masks. |
| Map-owned `.mat` files under `CampaignSurfaces/<map>/` and `CampaignVisualDetails/Refinement/{Ground,Water}/` | New property values only. |
| `Refinement/WaterMeshes/Bank_*.asset` and `CanalEndsV2/<map>_*` meshes | UV0, UV1 and UV2 channel data added in place. |
| `ProjectSettings/ProjectAuditorSettings.asset` | Untracked, created by Unity and unrelated. Do not commit it as part of this work. |

Entry points:

- `OtherMapSurfacePolish.CensusWaterAndRoadJoins` is read-only and produces the `before` captures.
- `OtherMapSurfacePolish.ApplyWaterAndRoadBlend` produces the `after` captures.
- The pass marker is `[WaterRoadBlend] result=Passed mode=<before|after> ... crossMapMaterials=0 sourceScenes=unchanged`.

### Water shader design

- **Mesh channels on the bank meshes:**
  - UV0 = (across 0..1, along 0..1, width, length);
  - UV1 = (flow axis xyz, edge flags: 1 across-min, 2 across-max, 4 along-min, 8 along-max, 16 headwall ends);
  - UV2 = (side axis xyz).
- **World-scale conversion:** the vertex shader converts width and length to world metres with `length(TransformObjectToWorldDir(axis,false))`, because the meshes are scaled (about 50 local units ≈ 6 m). The flow axis is canonicalised to a world-positive direction so adjacent pieces match.
- **Look:**
  - a shallow-to-deep gradient driven by distance to the nearest lipped edge;
  - a contact foam line;
  - flowing ripples along the channel;
  - a small sky reflection.
- **Per-material values** set by `ApplyWaterRoadMap`:
  - `_DepthFalloff`: 7 for the harbour, 1.6 for canals;
  - `_FlowSpeed`: .08 for the harbour, .3 for canals;
  - `_FoamStrength` .5, `_FoamWidth` .35, `_SkyReflection` .12.

### Road-join design

- **Join detection:** a DirtRoad end, with lateral extent no more than 1.3 × its depth, that touches Asphalt, Pad_M01_Asphalt or Sidewalk bounds. 19 joins were found across 6 maps.
- **Mask channels:**
  - R = 0.5 + u/16, where u is the signed distance from the seam in metres, positive into the pavement;
  - G = join zone, kept within about 4 m of the seam so joins on opposite sides of a narrow road never overlap;
  - B = wheel paths;
  - A = damp ground near water.
- **Shared boundary:** both materials evaluate one noisy boundary, `(.15+(Fbm*2-1)*.6+(fine-.5)*.45)*zone`, so a single ragged edge crosses the mesh seam continuously.
- **Paved side shows:**
  - a darker crumbled lip;
  - a soft dust haze over about 1.5 m;
  - tyre dust streaks over about 4 m;
  - sparse gravel.
- **Earth side shows:**
  - the dirt texture handing over to the shared sand colour about 3 m before the seam, so the mesh line disappears;
  - small asphalt fragments;
  - slightly darker wheel ruts.
- **Colours:** `_RoadBlendSand` (.72,.665,.56) was matched to the rendered dirt-road colour; `_RoadBlendAsphalt` is (.23,.25,.28).
- **Editability gate:** a join is stamped only when every ground material on both its dirt renderer and its paved renderer is editable. Otherwise the dirt side draws an asphalt edge with no asphalt beside it.

## Run history (logs in `validation/`, superseded captures in `captures/<run>/`)

| Run | Result | Notes |
| --- | --- | --- |
| census-01, census-02 | Wrapper exit 127, no log | Caused by the CRLF version issue. Fixed by passing `--unity`. |
| census-03 | Passed | Produced the `before` captures. |
| apply-01 | Failed | Masks were null on materials. Loading was hardened (fallback load, throw on null). |
| apply-02 to apply-04 | Superseded | Water was too pale and seamed, and the road blend showed two independent edges. |
| apply-05 | Failed | Darkened whole dirt strips (paved and earth guards were mixed up). |
| apply-06 | Passed, superseded | Large sand blobs and muddy smears across the crossroad. Cause: the zone reached 7 m, so north and south stamps overlapped. |
| apply-07 | Passed, superseded | Dithered dust read as a camouflage pattern. |
| apply-08 | Passed, best so far | Close to realistic. Defects: gravel and chips in lattice-aligned dotted rows, and a dark ragged vertical strip on DenseCity (a half-editable join). |
| **apply-09** | **Passed, captures not yet visually reviewed** | Fixes the lattice (rotated, warped scatter domain), skips non-editable joins, and suppresses the terminal dust inside the blend zone. All 7 DenseCity joins were skipped as `join-not-editable`, as expected. |

## Immediate next steps

1. apply-09 passed: `validation/apply-09.log` line 9919 has the pass marker, and the source scenes are unchanged.
2. Inspect the road joins with before/after crops; the capture size is 1600×1000:
   - `AshLinePort-*-road-join-0`;
   - `CH05M02TrustUnderFire-*-road-join-0`;
   - `CityEdgeAirfield-*-road-join-0`;
   - `RefineryDistrict-*-road-join-0`;
   - `CH04M04GroundedSignal-*-road-join-0`;
   - `DenseCity-*-road-join-0`.

   Confirm three things:
   - no dotted rows;
   - no DenseCity strip;
   - no straight seam or dark band at the old dirt-road end.

   The PIL crop used: `crop((440,250,1160,750))` per side; AshLinePort close-up `crop((656,219,1000,500))`.
3. If the result is good:
   - run `Game.Editor.OperationMapRenderDatabaseBuilder.RunSourceParityValidation` through the wrapper and confirm the packed DenseCity content hash is unchanged;
   - commit on the branch;
   - show the owner the before/after images embedded as markdown with absolute paths. The owner asked to be shown the images directly.

## Known gaps to report to the owner

- **DenseCity road joins get no new transition.** Its asphalt uses the shared template `WornAsphalt.mat`. A map-owned copy would require a scene reference change, which is outside the render-only scope. This needs an owner decision.
- **Role-conflict materials are skipped.** Some materials are used as both earth and paved; they are listed in each `<map>-after.json` under `skipped`.
- **The AshLinePort harbour has no visible shoreline effect.** The water rectangle's edges lie under the curved beach terrain, so edge distance never reaches the visible shore. A fix would be a render-only shore-distance mask for the water material, generated from terrain height versus water level, similar to the road mask.
- **The wide overview captures render water darker and some objects black.** This is specific to the overview camera; the shader's legacy path is identical.

## Remaining approved scope, not started

- **Bridge dressing (render-only):**
  - lamps, abutments and wing walls, supports with shadow, and a sandbag post;
  - group render-only children (Transform, MeshFilter, MeshRenderer, LODGroup only) under existing `SM_Prop_` roots and register them in `RefinementProtection.NewPresentationRoots`, following the pattern in `CampaignDestinations.cs` and `CampaignRoadFinish.cs`;
  - DenseCity additions need canonical owners and render-ledger reconciliation;
  - existing bridges come from `BuildBridgeCell` in `MapVariantBuilder.Roads.cs`, which uses SM_Env_Bridge_01/02 with railings and SM_Env_Bridge_Support_01.
- **Bank and canal-end dressing:** reeds, damp sand (partly done through mask channel A) and quay bollards. Moor boats only if the scene hashes stay safe.
- **Final gate:**
  - native captures compared against the mockups;
  - report visual review, automated checks, normal-input playthrough (not done) and device acceptance (not done) separately.
