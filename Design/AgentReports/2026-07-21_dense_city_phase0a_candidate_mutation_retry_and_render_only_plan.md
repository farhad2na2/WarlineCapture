# Phase 0A Candidate Mutation Retry And Render-Only Plan

Date: 2026-07-21  
Lane: Grok continuation after GPT 5.6 candidate-building handoff  
Tracker: `Design/Architecture/dense_city_editor_bake_hybrid_runtime_implementation_tracker.md`

## Status

`CandidateMutationBlockedByHeadlessLicensing`

Retried GPT's required candidate hierarchy transaction through the repository-approved wrapper. Unity never reached `executeMethod`. No candidate scene exists. Accepted sources remain byte-stable. Render-only copy planner scaffolding was added as non-mutating prep.

## Retry evidence

- Command: `Tools/CI/invoke_unity_macos.sh --timeout 1800 --log /private/tmp/dense-city-candidate-hierarchy.log -- -nographics -quit -executeMethod Game.Editor.OperationMapEntityPresentationCandidateSceneBuilder.CreateProtectedCandidateHierarchy`
- Log: `/private/tmp/dense-city-candidate-hierarchy.log`
- Failure: `Unsupported protocol version '1.18.1'` against `LicenseClient-farhad`, then timeout waiting for `LicenseClient-farhad-6000.5.2`
- Candidate path absent: `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/`
- No GUI Unity Editor was open to run the approved menus

## Inventory default-path restore

`CreateProtectedCandidateHierarchy` loads the dry-run inventory from `/private/tmp/warline-operation-map-entity-presentation-migration-inventory.json`. That default path had been overwritten by a stale `InventoryCompleteWithAmbiguities` report (`owners=0`). Restored the accepted inventory:

- inventory SHA-256: `a0b1c332ce715a5346785c0727cee9dad1b70f78e1895a2618df682edfa8c66d`
- summary SHA-256: `a77a191de4b4afbe12c31e7ffd549aeffffe3b3dfc7031a589b5645f89553788`
- result: `InventoryCompletePendingReview` with 9,090 migration owners

## Protected hashes still unchanged

- Accepted scene: `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`
- Accepted SubScene: `eff3ce6992d234c7438a321f0f9f552c2abebcc0a4738445014bc8f86579965d`

## Non-mutating render-only prep

Added `OperationMapRenderOnlyCandidateMigrationPlanner` plus focused tests. Bucket assignment uses only the authored `Map/<childFolder>/...` identity from inventory `nameHierarchyPath` through an explicit fail-closed table. Leaf object names, prefab filenames, proximity, and renderer shape are not used.

Dry-run against the restored accepted inventory (9,090/9,090 assigned, 0 unknown folders):

| Candidate `RenderOnly` bucket | Owner count |
|---|---|
| Terrain | 442 |
| RoadsAndBridges | 825 |
| Mountains | 13 |
| Vegetation | 928 |
| Props | 6,880 |
| Infrastructure | 0 |
| Horizon | 2 |

`Map/_UnmappedBuildings` (682) and `Map/_UnmappedVehicleSources` (293) are explicitly table-mapped to `Props` as static-package residual owners. They are not the 432/22 exact gameplay placement sources (those GlobalObjectIds are absent from the 9,090-owner set).

## Exact continuation when Unity can execute

1. Open `WarlineCapture` in a licensed normal Unity Editor (batchmode remains blocked), or fix headless licensing and re-run the wrapper.
2. Menu: `Tools > Warline Capture > Operation Map Migration > Create Protected Candidate Hierarchy` — require log `status=Created` and candidate GUID ≠ accepted SubScene GUID.
3. Menu: `Tools > Warline Capture > Operation Map Migration > Populate Candidate Gameplay Buildings` — require exactly 432 `OperationMapBuildingAuthoring` owners.
4. Re-check accepted scene/SubScene/static package hashes.
5. Implement/execute render-only owner copy using the planner buckets; keep production on `StaticSceneChunks`.

## Hard stops still in force

- Do not flip production to `EntityScene`.
- Do not mutate accepted source/static package/Addressables ownership.
- Do not terminate Unity Hub / reset IPC while Editors are running unless the owner explicitly requests stuck-environment recovery.
