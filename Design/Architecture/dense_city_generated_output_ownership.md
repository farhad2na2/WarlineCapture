# Dense City Generated Output Ownership And Rollback

Status: Generated-output ownership contract

This document assigns one producer, persistence policy, and rollback path to every generated-output family used by the dense-city candidate workflow. It supplements `dense_city_author_workflow.md`, `dense_city_generated_authored_ownership.md`, and `operation_map_authored_ecs_workflow.md`.

An output is not an authoring source. Regeneration begins from accepted scenes, protected configs, source prefabs/materials, deterministic generator code, and persistent authored overrides. Candidate scenes, generated assets, reports, captures, bundles, and caches must never be hand-edited to make a gate pass.

## Ownership Classes

| Class | Persistence | Rollback rule |
|---|---|---|
| Candidate asset | Tracked, reviewable, replaceable | The owning transaction restores the pre-run bytes on failure. After a successful but rejected run, revert the complete owner set with version control or regenerate it from accepted inputs. |
| Generated support asset | Tracked when accepted, replaceable | Regenerate with its named producer. If the producer has no transaction journal, inspect the diff and use version control to restore the previous accepted set. |
| Evidence artifact | Tracked only when it represents accepted evidence | Invalidate or regenerate through its validator. Never repair a report, manifest, hash, or image manually. |
| Runtime-content artifact | Untracked under `Library` | The runtime-content transaction restores prior output on failure. Delete or rebuild the isolated cache after success; it is never committed. |
| Transient artifact | Untracked log, cache, or temporary transaction state | Let the owning command clean it. After an interrupted run, remove only the exact known transient path after confirming no command owns it. |
| Frozen rollback artifact | Tracked and protected | Candidate tools may read and hash it but must not mutate it. Retirement requires the separately authorized cleanup item. |

## Authoring Candidate Outputs

| Output | Exact owner/producer | Path | Failure rollback | Rejection or invalidation |
|---|---|---|---|---|
| Dense candidate operation-map scene | `DenseCityCandidateAuthoringTransaction` | `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_dense_city_authoring_candidate.unity` | Hierarchy creation removes partial candidate copies. Realization snapshots the scene to a temporary file and restores its bytes on any exception. | Recreate from the accepted operation-map scene, or revert the candidate scene and its `.meta` together. Never copy it over the accepted scene. |
| Dense candidate EntityScene authoring scene | `DenseCityCandidateAuthoringTransaction` | `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity` | Hierarchy creation removes partial candidate copies. Realization and the DOTS-material pass snapshot and restore scene bytes on failure. | Recreate from the accepted EntityScene candidate, or revert the scene and its `.meta` together. |
| Map-bake generated hierarchy | `RuntimeCityRAndDEditModeBuilder` through `DenseCityCandidateAuthoringTransaction` | `Generated_GiantDenseMiddleEasternCity_MapBakeSource` inside the dense candidate operation-map scene | The candidate-scene snapshot restores the complete pre-run hierarchy. | Replace the complete marked root pair; never preserve or patch a child. |
| Entity-presentation generated hierarchy | `DenseCityPresentationReplayTransaction` through `DenseCityCandidateAuthoringTransaction` | `Generated_GiantDenseMiddleEasternCity_EntityPresentation` inside the dense candidate EntityScene | The candidate-scene snapshot restores the complete pre-run hierarchy. | Replace the complete marked root pair; never preserve or patch a child. |
| Surface-proxy mesh assets | `DenseCitySurfaceProxyBuilder` through candidate realization | `Assets/Game/GeneratedOperationMaps/DenseCity/opmap.skirmish.desert_base_01/Candidate/<generation-hash>/SurfaceProxies` | The existing folder is moved to the exact sibling `SurfaceProxies__TransactionBackup`; failure deletes partial output and moves the backup back. Success deletes the backup. | Regenerate from immutable surface records. Revert the whole generation-hash folder if accepted output is rejected. A leftover `__TransactionBackup` means the transaction did not close and must be investigated before another run. |
| Candidate DOTS sky material | `DenseCityCandidateAuthoringTransaction.ApplyCandidateMaterialCompatibilityBatch` | `Assets/Game/GeneratedOperationMaps/DenseCity/opmap.skirmish.desert_base_01/Candidate/SharedMaterials/DenseCity_SkyBox_DOTS.mat` | The candidate EntityScene is journaled, but the material asset itself is not covered by a complete file transaction. | Regenerate deterministically from `Assets/PolygonMilitary/Materials/Misc/SkyBox.mat`, or revert the material and `.meta` with version control. Treat any material-only diff after a failed run as uncommitted output requiring review. |
| Candidate shared-material folder | `DenseCityCandidateAuthoringTransaction` material-compatibility ownership | `Assets/Game/GeneratedOperationMaps/DenseCity/opmap.skirmish.desert_base_01/Candidate/SharedMaterials` | No folder-wide transaction journal exists. | Re-run the compatibility producer or revert the complete folder with version control. Do not claim automatic rollback for this folder. |
| Dense facade/shop material library | `DenseCityBuildingMaterialLibrary.CreateOrUpdate` | `Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/dense_city_building_materials` | No transaction journal exists; the current realization path only calls `LoadExisting`. | Update only through the library producer, validate every material/texture diff, and revert the complete folder with version control on rejection. It is shared generator input, not a disposable candidate child. |
| Legacy generated road-mesh folder | `DenseMiddleEasternCityEditModeBuilder` cleanup owner | `Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/dense_city_roads` | The builder deletes this legacy folder before road realization and does not journal it. | Do not place authored content here. Restore a previously accepted legacy set only from version control if rollback requires it; current dense output must not depend on it as persistent authoring. |

The realization transaction also snapshots `SkirmishDesertBase_MapWideCity_Config.asset` through its protected placement-config path and verifies count and SHA-256 after saving. That config is a protected input, not generated output; mutation causes rollback and rejection.

## Candidate Definition And Runtime Binding

| Output | Exact owner/producer | Path | Failure rollback | Rejection or invalidation |
|---|---|---|---|---|
| Existing-map candidate SubScene | `OperationMapEntityPresentationMigrationEditor` and `OperationMapEntitySceneCandidateBakeAll` | `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_candidate.unity` | Candidate Bake All journals the scene and `.meta` and restores both on failure. | Re-run candidate migration/Bake All from accepted sources, or revert the scene and `.meta` together. |
| Existing-map candidate definition | `OperationMapEntitySceneCandidateAddressablesLayoutBuilder` | `Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_EntityScene_Candidate.asset` | The layout builder and candidate Bake All journal the asset and `.meta`. | Regenerate through the candidate layout builder. |
| Existing-map thin runtime binding | `OperationMapEntitySceneCandidateAddressablesLayoutBuilder` | `Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_entity_scene_runtime.unity` | The layout builder and candidate Bake All journal the scene and `.meta`. | Regenerate through the candidate layout builder; never edit renderer or collider content into it. |
| Dense candidate definition | `OperationMapEntitySceneCandidateAddressablesLayoutBuilder.BuildDenseCityCandidateEntitySceneAddressablesLayout` | `Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset` | The layout builder journals the asset, `.meta`, runtime binding, `.meta`, and layout report as one file transaction. | Re-run the dense candidate layout builder, or revert that complete transaction set. |
| Dense thin runtime binding | Same dense layout builder | `Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity` | Same transaction as the dense definition. | Regenerate with the dense layout builder; never promote it by manually editing production references. |
| Candidate Addressables layout | `OperationMapEntitySceneCandidateAddressablesLayoutBuilder` and planner | Candidate definition/runtime binding plus tracked layout report; production `Assets/AddressableAssetsData` is protected | Candidate outputs roll back as a transaction; a protected-production snapshot rejects any Addressables mutation. | Re-run the appropriate candidate layout builder. There is no candidate-owned production Addressables group to clean up. |

The production operation-map definition, production thin binding, accepted map/SubScene, map surface, minimap, and `Assets/AddressableAssetsData` are protected inputs. None belongs to the candidate rollback set.

## Isolated Runtime Content

| Output | Exact owner/producer | Path | Failure rollback | Successful-run disposition |
|---|---|---|---|---|
| Dense candidate Addressables bundles/catalog | `OperationMapDenseCityCandidateRuntimeContentBuilder` | `Library/OperationMapDenseCityRuntimeContent/Addressables` | `DenseRuntimeContentOutputTransaction` moves any prior directory into `Library/OperationMapDenseCityRuntimeContentTransactions/<guid>/dense-addressables` and restores it on failure. | Retain only as local validation cache; rebuild from the exact candidate revision and never commit it. |
| Dense candidate Entities archives/catalog | Same runtime-content builder | `Library/OperationMapDenseCityRuntimeContent/Entities` | The same transaction snapshots and restores the prior directory on failure. | Retain only as local validation cache; never commit it. |
| Shared macOS Addressables build output used temporarily | Same runtime-content builder | `Library/com.unity.addressables/aa/OSX` | The transaction moves the prior shared output aside, builds into the empty path, publishes a copy into the dense output, then restores the prior shared output. Disposal rolls all three output directories back if commit is not reached. | The original shared output must be restored even after success; the report must retain `sharedOutputRestored=1`. |
| Temporary Addressables settings and builder asset | Same runtime-content builder | `Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/DenseCityRuntimeContentBuildTemp` | `finally` deletes the folder and forces an AssetDatabase refresh. | Must be absent after success. Never commit it. |
| Runtime-content transaction backups | `DenseRuntimeContentOutputTransaction` | `Library/OperationMapDenseCityRuntimeContentTransactions/<guid>` | Dispose restores all captured directories, then deletes the transaction root. | Must be absent after a completed run. A leftover GUID folder is interrupted-run evidence; inspect it before exact-path cleanup. |
| Runtime parity binary | `OperationMapDenseCityRuntimeParityManifestWriter` | `Library/OperationMapDenseCityRuntimeParity/dense_candidate_runtime_parity.bin` | No accepted-source mutation occurs; failure leaves only transient evidence. | Delete or regenerate from the exact candidate revision. Never commit it. |

The runtime-content builder separately journals its tracked JSON report and production Addressables settings file, and verifies protected production files/directories after the build. It does not authorize a production cutover.

## Tracked Evidence Outputs

The following families are generated evidence. Their file names are stable report identities, not editable documentation:

| Evidence family | Owner | Current paths | Rollback/invalidation |
|---|---|---|---|
| Candidate Bake All | `OperationMapEntitySceneCandidateBakeAll` | `Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_bake_all.{json,md}` | Bake All journals candidate assets; failed report output is rolled back. Regenerate both files from the same run. |
| Candidate ECS bake | `OperationMapEntityPresentationCandidateBakeValidator` | `Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_bake_validation.json` and `2026-07-24_dense_city_generated_candidate_bake_validation.json` | A new Bake All invalidates dependent evidence before postflight. Regenerate; do not carry a report across a candidate fingerprint change. |
| Shared art, transform parity, and presentation budget | Their named probes/validators | `2026-07-21_dense_city_phase0a_shared_art_ownership.{json,md}`, `2026-07-21_dense_city_phase0a_transform_parity.json`, `2026-07-24_dense_city_generated_transform_parity.json`, `2026-07-22_dense_city_presentation_budget.json` | Regenerate in dependency order. Budget evidence is explicitly invalidated at Bake All start and must not be accepted while invalidated. |
| Candidate layout | `OperationMapEntitySceneCandidateAddressablesLayoutBuilder` | `2026-07-21_dense_city_phase0a_candidate_entityscene_addressables_layout.{json,md}` and `2026-07-24_dense_city_candidate_entityscene_addressables_layout.json` | The layout builder journals its report with the definition and binding outputs. Re-run the correct accepted-map or dense profile. |
| Candidate runtime content | Accepted-map or dense runtime-content builder | `2026-07-21_dense_city_phase0a_candidate_runtime_content.json` and `2026-07-24_dense_city_candidate_runtime_content.json` | The builder journals the report. Reject it if its candidate/report fingerprints do not match the revision under test. |
| Dense identity and runtime parity manifests | Identity backfill and runtime parity manifest writers | `2026-07-24_dense_city_generated_identity_backfill.json` and `2026-07-24_dense_city_runtime_parity_manifest.json` | Regenerate from the exact candidate. The tracked JSON summary and transient binary are one logical evidence set. |
| Fixed-camera baseline and runtime captures | `OperationMapEntityPresentationFixedCameraParityValidator` and packed runtime parity capture | `2026-07-24_dense_city_editor_fixed_camera_baseline.json`, `2026-07-24_dense_city_runtime_fixed_camera_parity.json`, and their matching folders under `Design/AgentReports/Captures` | Replace the report and its complete image set together. Reject partial, mixed-revision, blank, or manually edited images. |
| Phase 0/0A inventories and historical review captures | Their dated probe/capture command or explicitly named human review | Dated `Design/AgentReports/2026-07-21_dense_city_phase0*` files and `Design/AgentReports/Captures/GeneratedScenes/*dense_city*` | These are provenance/baseline evidence. Do not overwrite them with current candidate output; add new dated evidence when a new acceptance gate requires it. |

Any evidence file not listed individually above follows the same rule when it lives under a dated dense-city report/capture family: the code or documented capture procedure that declares its schema/path owns it, and version control is the rollback mechanism after a successful run. A report without a reproducible owner is historical prose, not machine acceptance evidence.

## Logs, Test Results, And Build Caches

- Windows Unity logs and XML results live under `%TEMP%`; macOS logs/results live under `/private/tmp`. The wrapper invocation owns them. They are referenced by the tracker but never committed.
- Unity `Library`, `Temp`, `Logs`, Addressables build caches, Entities caches, and test-result scratch output are disposable local state.
- Do not delete an active transaction directory, Unity cache, or wrapper log while Unity owns it.
- A cache rebuild is not rollback of a tracked candidate asset. Restore tracked assets first, then regenerate caches.

## Frozen Static Rollback Ownership

`Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01` is the frozen production rollback package. It includes the static manifest, integrity evidence, minimap, and generated chunk scenes used by the current `StaticSceneChunks` production path.

Candidate and dense-city producers may read and hash this root, but they may not regenerate, prune, relabel, overwrite, or delete it. The candidate Bake All protected snapshot includes this directory. Removal from production labels/build ownership and deletion of tracked static output remain a separate unchecked cleanup step requiring accepted Editor and Android evidence plus rollback archive/hash evidence.

The sibling `dense_city_building_materials` and legacy `dense_city_roads` folders are generator support outputs, not part of the map-specific frozen rollback root; their weaker transaction coverage is documented above and does not permit mutation of `desert_base_01`.

## Recovery Decision

When a run fails or output is rejected:

1. Stop before another producer overwrites the evidence.
2. Identify the exact owner row in this document.
3. Let an active transaction finish its rollback.
4. Verify protected accepted sources, production Addressables, and the frozen rollback root are unchanged.
5. Restore only the named candidate/output set through its journal or version control.
6. Remove only exact transient paths after confirming no Unity process owns them.
7. Fix the accepted input, generator, or validator; regenerate through the named owner.
8. Commit candidate assets and their matching accepted evidence together only after all claimed gates actually ran.

Never widen rollback to the repository, accepted source scene, production Addressables, or frozen static package to conceal an output-owner failure.
