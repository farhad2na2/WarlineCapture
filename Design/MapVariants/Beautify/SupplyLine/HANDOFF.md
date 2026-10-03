# Supply Line map beautify pilot — handoff

## Goal
The Refinery District map (missions CH02M02 Supply Line, CH02M04, CH04 Split Front, CH04 Steel Push) looks flat: dark-brown slab pads, light-brown constant ground and sparse dressing. The user approved the visual direction in `supplyline-beautify-mockup-v01.jpg`. Pilot scope covers eight items:
- textured concrete pads;
- two-tone ground patches;
- edge wear;
- story vignettes at the edges;
- a hero set piece;
- FX life (flares, smoke, dust);
- runtime lighting;
- vegetation clusters.

## Where the work is
- **Worktree:** `/Users/farhad/Projects/WarlineCapture-Beautify`, branch `map-beautify/supply-line`, based on `main` `df126e074`.
- **Isolation:** the worktree has its own `Library` folder, so it never touches the main project Editor.
- **Editor:** a worktree Editor may still be open (launched with the wrapper, log `/private/tmp/beautify-editor-2.log`). Check with `unity status --project-path <worktree>`. Other agents use the main project; do not touch their Editor.

## Code (written, compiles)
- **`Assets/Game/Scripts/Editor/MapPrototypes/Variants/MapVariantBeautify.cs`**: render-only paint and props pass. It is called from `MapVariantPreparedCandidateBuilder.Build` after the classification loop and before `PersistGround`. It reports to `PreparedCandidateOutput.presentation`. It contains:
  - concrete slabs with grout, sand drift, bay lines and oil stains on pads;
  - ground patches, worn tracks, sand drifts and pad edge dressing;
  - dead-space debris props and palm groves.
- **Placement rules:** tall props only on statically blocked cells, unreachable pockets or outside the union of mission playable bounds. Clearance circles keep objectives and spawns clear.
- **`MapVariantBuilder.RecordPlacements`**: new flag. Props placed with it off do not enter `b.Placements`, so the semantic hash is unchanged.
- **`MapVariantAtmosphere.cs`**: adds an `Atmosphere` root to `RuntimeBinding.unity`, called at the end of `MapVariantCandidateRuntimeBuilder.CreateBinding`. It holds:
  - flare fire and smoke on refinery stacks;
  - a horizon plume;
  - burning wrecks;
  - blowing dust.
  Legacy Synty particle materials are converted to URP Particles/Unlit copies in `Assets/Game/Art/MapBeautify/FX/`.
- **`MapVariantBeautifyPipeline.cs`**: public entry points.
  - `CaptureBefore` / `CaptureAfter` render six mission-camera views to `Design/MapVariants/Beautify/SupplyLine/{before|after}-*.png`.
  - `RebuildRefinery` rebuilds the prepared map, **throws if `semanticHash` != `2d4fd91a415a68f0299a4075e37730ecd7b746e94e5a2a1e2a6cd7baca687778`** (pinned by the mission configs and runtime systems), builds the runtime content, then captures "after".

## Status
| Gate | State |
|---|---|
| Compile | Passed (worktree Editor, no `error CS`) |
| "Before" captures | Done: `before-*.png` (confirms the flat look) |
| Rebuild + hash check | **Not run yet.** The first try failed because the capture left an untitled scene open. That is fixed: the capture now ends on `Assets/Game/Scenes/Match.unity`, and the rebuild opens it first. The fix is not compiled yet. |
| "After" captures and visual review against the mockup | Pending |
| Normal-input Supply Line playthrough (ARIA, result, return) | Pending |
| Device / mobile performance | Pending. `Mobile_RPAsset` shadow distance 16 is a recommendation only; not changed. |

**Uncommitted, unreviewed worktree changes.** These appeared after Editor opens and the failed rebuild start:
- the Refinery `Definition.asset`, `Minimap.png`, `PreparedEntities.unity` and `RuntimeBinding.unity`;
- `PreparationUnitFixture.unity`;
- the TMP fallback font.

They are not intended edits. Discard them (`git checkout -- <files>`) before the rebuild unless the diff shows otherwise.

## Next steps
1. Open the worktree Editor only via the wrapper (no `-batchmode`):
   `Tools/CI/invoke_unity_macos.sh --project <worktree> --log /private/tmp/<new>.log --`
2. Run the rebuild. `/private/tmp/beautify_run.sh` refreshes the Editor, waits for compilation, fails on `error CS` and dispatches with `--reuse`:
   `/private/tmp/beautify_run.sh RebuildRefinery beautify-rebuild-02 3600`
   The pass marker is `[MapBeautify] rebuild result=Passed`. Each run needs a new log name.
3. If the hash check throws, a beautify object reached `b.Placements` or the grid. Fix it in `MapVariantBeautify`; do not change the pinned hash.
4. Compare `after-*.png` with `before-*.png` and the mockup. Tune densities and colours in `MapVariantBeautify` (pads, patches, tracks) and effect scales in `MapVariantAtmosphere`. Check for pink particles (material conversion) and props blocking lanes or objectives.
5. Run the existing focused validations for the four Refinery missions, then a normal-input Supply Line playthrough.
6. Commit the regenerated scene, minimap, definition and manifests on the branch. Report the visual, automated, playthrough and device gates separately.

## Constraints
- Protected source folders (`ProtectedHashes()`) must not gain files. New art goes in `Assets/Game/Art/MapBeautify/`.
- Particles are not allowed in the entity subscene, and the binding `mapRoot` must stay renderer-free. Effects therefore live only under `Atmosphere` in the binding scene.
