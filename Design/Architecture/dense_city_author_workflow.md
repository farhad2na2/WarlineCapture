# Dense City Author Workflow

Status: Candidate-only `EntityScene + VirtualizedProxyPool` workflow accepted through Android Phase 9; production remains `StaticSceneChunks + ResidentEntities` until the tracker authorizes cutover.

This is the reproducible author path for changing persistent overrides, regenerating the dense city, validating it, baking the candidate package, reviewing evidence, and preparing device acceptance. It does not authorize edits to accepted/frozen generated assets or production Addressables ownership.

## 1. Before Editing

1. Keep Unity Hub open and signed in.
2. Use Unity `6000.5.2f1`, matching `ProjectSettings/ProjectVersion.txt`.
3. Confirm the working tree and preserve unrelated changes.
4. Read `AGENTS.md` and use only its platform-approved Unity wrapper.
5. Confirm production still uses `StaticSceneChunks`. Do not run a production cutover as part of ordinary dense-city authoring.

The protected inputs are:

- accepted map: `Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity`;
- accepted entity-authoring source: `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_candidate.unity`;
- generator config: `Assets/Game/Configs/OperationMaps/Skirmish/SkirmishDesertBase_MapWideCity_Config.asset`;
- legacy building-placement input: `Assets/Game/Configs/Scene/Match_MapBuildingPlacement_Config.asset`;
- generated virtualized database input: `Assets/Game/GeneratedOperationMapEntityPresentationCandidate/VirtualizedPresentation/OperationMapRenderDatabaseBakeConfig.asset` (generated output, never hand-edited).

Never edit generated children as a way to preserve a change. Regeneration owns and replaces them.

## 2. Edit Persistent Overrides

1. Open the accepted map.
2. Make persistent handmade corrections only beneath the approved `AuthoredCityOverrides` domain or another already-approved authored owner.
3. Keep typed spawn, deployment, camera, minimap, runway, helipad, objective, building, and vehicle authoring outside disposable generated roots.
4. Give every new override a stable identity and finite transform/extent.
5. Save the accepted source only after confirming the edit is intentional.

If a desirable change currently exists beneath a generated root, promote it into approved authored ownership first. Do not hand-edit either candidate scene or depend on an object name/category heuristic.

## 3. Clear And Regenerate Safely

The accepted candidate workflow recreates candidate ownership from protected inputs; it does not clear or rebuild the accepted source scene in place.

1. Run `Game > Maps > Skirmish Desert Base > Create Dense City Candidate Hierarchy`.
2. Run `Game > Maps > Skirmish Desert Base > Realize Dense City Candidate`.
3. Run `Game > Maps > Skirmish Desert Base > Apply Dense City Candidate DOTS Materials`.

These commands own only:

- `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_dense_city_authoring_candidate.unity`;
- `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity`;
- candidate-generated artifacts under `Assets/Game/GeneratedOperationMaps/DenseCity`.

For a disposable local preview only, the `RuntimeCityRAndDMapView` inspector exposes `Clear Generated City` and `Build Giant Dense City`. Do not save that preview over an accepted source/candidate asset. The transactional candidate commands above are the acceptance path.

On any rejection:

- stop and read the exact Unity log error;
- do not bypass readiness, physics, ownership, overlap, parity, or budget gates;
- verify protected source hashes and the legacy placement count/hash remain unchanged;
- rerun from candidate hierarchy creation only after fixing the cause.

## 4. Validate Before Bake All

Run these menu validations in order:

1. `Game > Operation Maps > EntityScene Migration > Validate Accepted Source Physics`
2. `Game > Operation Maps > EntityScene Migration > Validate Entity Presentation Readiness`
3. `Game > Operation Maps > EntityScene Migration > Validate Dense City Bake Readiness`
4. `Game > Operation Maps > EntityScene Migration > Bake And Validate Dense City Candidate`
5. `Game > Operation Maps > EntityScene Migration > Validate Candidate Runtime Physics`
6. `Game > Operation Maps > EntityScene Migration > Validate Candidate Presentation Budget`

Acceptance requires:

- exactly one valid generated root per required role and one shared generation contract;
- no prohibited collider/Rigidbody beneath generated, source, binding, or entity-authoring ownership;
- no unclassified renderer or renderer beneath a proxy root;
- exact authored/generated identity, matrix, and transformed-bounds ownership;
- no managed runtime companion for generated buildings;
- no protected-content mutation or overlap;
- current deterministic presentation-budget evidence.

## 5. Bake All

Run:

`Game > Operation Maps > EntityScene Migration > Bake All Candidate EntityScene`

This candidate-only transaction invalidates stale success evidence before mutation, captures candidate-owned files, revalidates protected production state, and rolls candidate outputs back on failure.

Do not substitute `Game > Operation Maps > Bake Current Map (All)` for this step while production remains `StaticSceneChunks`. That command follows the current production definition and may publish the legacy static presentation.

After a failed Bake All:

1. Confirm the report says `CandidateBakeAllFailed`.
2. Confirm rollback was applied when candidate-owned bytes changed.
3. Confirm protected production validation passed after rollback.
4. Do not reuse a stale budget, transform-parity, or package-success report.

### 5.1 Accepted Virtualized Candidate Output

The accepted candidate revision selects `OperationMapRenderResidencyMode.VirtualizedProxyPool` only on `OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset`. Production remains `StaticSceneChunks + ResidentEntities` with `productionCutover=0`.

The deterministic generation/bake chain owns:

1. exact renderer/material eligibility inventory;
2. prototype and compound-part recipes;
3. logical placements and spatial cells;
4. measured-envelope fixed pool capacity;
5. transactional `OperationMapRenderDatabaseBakeConfig` and matching report;
6. one `OperationMapVirtualizedPresentationAuthoring` root in the candidate EntityScene authoring scene;
7. one baked `OperationMapRenderDatabaseBlob`, one shared `RenderMeshArray`, and `7,784` hierarchy-free fixed proxy slots whose `MaterialMeshInfo` starts disabled.

The accepted schema-1 database has content hash `bfb350f0c8d1474aa05252dc04c87eede4c1210adcee9c92dcdbecc35897896e`, ordering hash `a43040ee38b9e8cfe752f1e52848cfa523e453fa2ebcde0cadc4142510d79318`, and serialized config SHA-256 `047e07cc77d3a576df6c2ee25c78f93bdebdb57d3c1ffda2da6424a24ffc2a06`. It contains `88` meshes, `24` materials, `9,107` prototypes, `12,293` parts, `40,460` placements, `1,934` cells, `60,922` cell-placement indices, four policy buckets, and `7,784` fixed slots. Source reconciliation is `76,517 = 61,925` eligible logical rows `+ 14,592` resident exceptions. The packed candidate strips `61,783` eligible physical renderers, retains `14,017` unique resident render-owner rows, contains zero packed eligible source rows, and reproduces packed fingerprint `23bebb4580aef31d7cd666b7f300f207bc8ccca1df5ce732d132b703400a655a`.

Run the tracker-required focused, direct-bake, packed lifecycle/parity, and package commands through the checked repository wrappers. Never bypass the wrapper, edit the generated config/report, inflate capacity, or enable production from this author workflow.

## 6. Review Reports

Review and retain the exact-revision evidence:

- candidate Bake All:
  - `Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_bake_all.json`
  - `Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_bake_all.md`
- candidate bake validation:
  - `Design/AgentReports/2026-07-24_dense_city_generated_candidate_bake_validation.json`
- transform parity:
  - `Design/AgentReports/2026-07-24_dense_city_generated_transform_parity.json`
- runtime parity manifest summary:
  - `Design/AgentReports/2026-07-24_dense_city_runtime_parity_manifest.json`
- presentation budget:
  - `Design/AgentReports/2026-07-22_dense_city_presentation_budget.json`
- virtualized logical database and deterministic packed output:
  - `Design/AgentReports/2026-07-28_dense_city_render_virtualization_database.json`
  - `Design/AgentReports/2026-07-30_dense_city_render_virtualization_pilot_enabled.json`
  - `Design/AgentReports/2026-08-08_dense_city_render_virtualization_two_run_bake_all.json`
  - `Design/AgentReports/2026-08-08_dense_city_render_materialized_parity.json`

Reject the revision if a report is missing, stale, failed, from a different source fingerprint, or records production cutover unexpectedly. Logs under temporary folders are validation evidence, not tracked project artifacts.

## 7. Device Test

Candidate device acceptance passed on Samsung `SM-S918B` / `R5CTC1J02VB` for package/source head `f6accf53e7cca54c6a5df632891b31558c62be2b`; that evidence does not authorize production cutover. Any later candidate or production revision that changes packed content must repeat the risk-required package/device gates from its exact checked revision.

1. Build local Addressables through the approved Unity wrapper.
2. Build APK and AAB through `BuildScript.BuildAndroid`, passing `-buildType APK` or `-buildType AAB`.
3. Install on the declared reference device with network disabled.
4. Exercise launch, Deploy entry, map load/readiness, camera traversal, navigation, placement, targeting, aircraft clearance, minimap, runway/helipad anchors, representative building destruction, menu return, and a second full load/unload cycle.
5. Capture package/installed/bundle bytes, duplicate dependencies, load timings, peak/retained memory, CPU/GPU frame distributions, draw/SetPass calls, visible renderers, triangles, shadows, Entities Graphics batches, GC, thermal behavior, and sustained FPS.
6. Compare Android transform/bounds and fixed-camera evidence to the accepted Editor manifests.
7. Reject any network dependency, authoring hierarchy in the player, current-map static manifest/chunk ownership after cutover, lifecycle leak, moved/missing/duplicated visual, orphaned destroyed-state attachment, or budget regression.

The accepted final candidate thermal route records `59.980` average FPS, `59.997` p10 FPS, `16.672/16.670/16.740 ms` average/p95/p99 frame time, CPU `11.568/13.218 ms` average/p95, GPU `13.849/14.001 ms` average/p95, `0 B/frame` managed allocation, zero battery drain, and thermal status `0` before and after its `120.007 s` sample. Evidence is under `Build/AndroidDenseCandidate/Evidence/2026-08-09-vrp097-final-thermal-f6accf53e-cool-rerun2`.

Do not mark Android or production cutover accepted from an Editor-only pass.

## 8. Commit Discipline

At each stable accepted step:

1. inspect `git status`;
2. stage only files owned by that step;
3. exclude Unity settings churn, temporary logs, build caches, and unrelated user assets;
4. update the implementation tracker with exact commands/logs/results;
5. commit and push the bounded step.
