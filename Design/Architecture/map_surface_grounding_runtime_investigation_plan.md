# Map Surface Grounding Runtime Investigation Plan

## Problem

Soldiers visually float in some Match-scene areas: their real-time shadows remain on the ground while the soldier body is offset upward. The issue is visible with the normal gameplay camera after panning upward from the initial view. It must be solved at the map-surface/runtime-grounding layer, not hidden by changing camera angle, marker position, or disabling shadows.

## Current Hypothesis

The baked asset data can be correct while the running match still uses stale subscene-baked `MapSurfaceComponent` data.

Risky path:

1. `MapSurfaceAuthoring.BakedSurfaceData` points at `Match_Map_MapSurfaceData.asset`.
2. `MatchSubScene` can also contain a baked `MapSurfaceComponent`.
3. `MapSurfaceRuntimeBootstrapSceneSystemHelper.Ensure(World, MapSurfaceDataAsset)` currently returns early when it finds a non-owned baked surface.
4. That means runtime can keep stale subscene surface data and skip the freshly rebaked asset.
5. Scene overlays can also be attached to the wrong surface entity if the active surface entity is non-owned.

Runtime probing also found a second confirmed lift path: runtime runway overlays were using `visualBounds.max.y` as their surface height. In the failing lower soldier cluster this pushed soldiers from the true ground height `0.005` to the runway renderer top `0.790`, while nearby soldiers outside the overlay remained at `0.005`. That exactly matches the observed "soldier moves up, shadow stays down" symptom.

## Fix Strategy

When Match has authored baked surface data, runtime must make that asset the single active `MapSurfaceComponent` source. Subscene-baked data is allowed only as a fallback when no authored runtime asset exists.

## Steps

1. **Document hypothesis and plan.**  
   Save this file and keep progress visible.

2. **Fix surface ownership selection.**  
   Update `MapSurfaceRuntimeBootstrapSceneSystemHelper` so `Ensure(world, surfaceData)` always loads the current authored asset blob and publishes it to one active surface entity. It may reuse an existing subscene surface entity, but must replace its component data and tag it as runtime-owned.

3. **Fix overlay attachment.**  
   Ensure `PublishSceneOverlays` writes overlays onto the same active surface entity used by `UnitSurfaceTrackingSystem`.

4. **Remove stale duplicate surfaces.**  
   After runtime asset publication, remove all other `MapSurfaceComponent` entities. Dispose only blobs owned by runtime tags.

5. **Add focused runtime bootstrap test.**  
   Create a world with a stale non-owned surface plus an authored asset surface. Assert that `Ensure` replaces stale height data with the authored asset, tags the entity as runtime-baked, and leaves exactly one active surface.

6. **Keep bake regression tests.**  
   Preserve tests proving roads win, blockers do not become ground height, and accidental higher non-road meshes do not become infantry floor.

7. **Validate in shadow Unity.**  
   Copy changed files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`, run focused EditMode validation, rebake if needed, and run a runtime surface probe against the live match.

8. **Proof standard.**  
   Final proof must be either:
   - runtime data showing the visible bad soldier cell uses the corrected active surface, plus a gameplay camera screenshot that clearly shows soldiers and shadows; or
   - a clear statement that visual proof is still blocked, with the exact blocker.

## Progress

- [x] Step 1 documented.
- [x] Step 2 fixed.
- [x] Step 3 fixed.
- [x] Step 4 fixed.
- [x] Step 5 test added.
- [x] Step 6 preserved.
- [x] Step 7 validated.
- [ ] Step 8 reported.

## Validation Notes

- Shadow compile gate passed with no C# errors.
- `MapSurfaceRuntimeBootstrapValidation` passed: stale non-owned subscene surface data is replaced by the authored runtime surface asset and exactly one active surface remains.
- `MapSurfaceLayeredGridFocusedValidation` passed: 11 map-surface bake/pathing regressions still pass.
- `BuildingRuntimeSurfaceOverlayValidation` passed: runway overlay height uses the authored runway surface center rather than renderer-top bounds.
- Runtime cohort probe before runway-overlay fix: affected soldiers around the lower runway cluster were at `sampledHeight=0.7900` with `hasRuntimeOverlay=true`.
- Runtime cohort probe after runway-overlay fix: the same lower cluster reports `sampledHeight=0.0050`, `entityYMinusExpected=0.0000`, `runtimeOverlayCount=0`, and one active runtime-baked surface entity.
