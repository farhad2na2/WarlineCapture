# Phase 0A Candidate Hierarchy And Buildings Executed

Date: 2026-07-21  
Lane: Grok continuation  
Tracker: `Design/Architecture/dense_city_editor_bake_hybrid_runtime_implementation_tracker.md`

## Status

`CandidateHierarchyAndBuildingsCreated`

Stopped the stuck headless batch Unity, used the non-batchmode / GUI licensing path (Hub `open` did not stay resident in this environment), and executed both required candidate transactions. Production ownership is unchanged.

## Exact commands that worked

Headless `Tools/CI/invoke_unity_macos.sh` (`-batchmode`) still fails with `Unsupported protocol version '1.18.1'`. These succeeded:

```bash
/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity \
  -projectPath /Users/farhad/Projects/WarlineCapture \
  -logFile /private/tmp/dense-city-candidate-hierarchy-gui.log \
  -executeMethod Game.Editor.OperationMapEntityPresentationCandidateSceneBuilder.CreateProtectedCandidateHierarchy \
  -quit

/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity \
  -projectPath /Users/farhad/Projects/WarlineCapture \
  -logFile /private/tmp/dense-city-populate-buildings-gui.log \
  -executeMethod Game.Editor.OperationMapBuildingCandidateMigrationEditor.PopulateCandidateGameplayBuildings \
  -quit
```

## Results

- Candidate: `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_candidate.unity`
- Candidate GUID: `0f9ecd54a7f0f467fa35556af7d28f1d`
- Accepted SubScene GUID: `d50925a18e9164ce782536576cb833d8` (unchanged)
- Log: `status=Created` then `buildings=432 ... managedRuntimeBuildingEntity=0 productionCutover=0`
- Candidate YAML: 432 `OperationMapBuildingAuthoring`, 432 `IntactVisual`, 266 `DestroyedVisual`, 3 role root markers, 0 Collider/Rigidbody
- Accepted scene SHA-256: `f210e53df16b9dad1c96457b4e72c03260ac8ab215a3f4e959b97d54c904be88`
- Accepted SubScene SHA-256: `eff3ce6992d234c7438a321f0f9f552c2abebcc0a4738445014bc8f86579965d`
- Production scenes do not reference the candidate GUID

## Focused tests

`/private/tmp/dense-city-candidate-tests-gui4.xml` — **18/18 passed** after fixture fixes:

- canonical `opmap.skirmish.*` ids in authoring/candidate tests
- `JobHandle.Complete()` before destroying the destruction-visual system
- candidate builder SetUp uses `NewSceneMode.Single` to avoid untitled dirty-scene failures outside batchmode

## Next

1. Implement/execute copy of the 9,090 render-only owners into the candidate using `OperationMapRenderOnlyCandidateMigrationPlanner`
2. Bake candidate SubScene and validate gameplay/render-only/render-child counts
3. Keep production on `StaticSceneChunks` until Editor + Android acceptance
