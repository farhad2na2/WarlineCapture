# Phase 0A Candidate Render-Only Owners Copied

Date: 2026-07-21  
Lane: Grok continuation  
Tracker: `Design/Architecture/dense_city_editor_bake_hybrid_runtime_implementation_tracker.md`

## Status

`CandidateRenderOnlyOwnersCreated`

Copied all 9,090 accepted static render-only migration owners into the protected candidate SubScene. Production ownership and presentation mode are unchanged.

## Implementation

- `Assets/Game/Scripts/Editor/OperationMapRenderOnlyCandidateMigrationEditor.cs`
- Menu / executeMethod: `Game.Editor.OperationMapRenderOnlyCandidateMigrationEditor.PopulateCandidateRenderOnlyOwners`
- Bucket assignment via existing `OperationMapRenderOnlyCandidateMigrationPlanner` (authored `Map/<childFolder>` table only)

## Execution

Non-batchmode Unity (GUI licensing path):

- Log: `/private/tmp/dense-city-populate-render-only-gui.log`
- Result: `status=Created owners=9090 ... buildingsPreserved=432 productionCutover=0`
- Buckets: Horizon=2, Infrastructure=0, Mountains=13, Props=6880, RoadsAndBridges=825, Terrain=442, Vegetation=928

## Isolation checks

- Accepted scene SHA-256: `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`
- Accepted SubScene SHA-256: `eff3ce6992d234c7438a321f0f9f552c2abebcc0a4738445014bc8f86579965d`
- Candidate size grew to ~35 MiB; prior 432 building authorings preserved
- Production scenes do not reference candidate GUID `0f9ecd54a7f0f467fa35556af7d28f1d`

## Next

1. Bake the candidate SubScene and validate gameplay/render-only/render-child entity counts
2. Verify authored vehicle ECS path and eliminate duplicate visuals in candidate mode
3. Keep production on `StaticSceneChunks` until Editor + Android acceptance
