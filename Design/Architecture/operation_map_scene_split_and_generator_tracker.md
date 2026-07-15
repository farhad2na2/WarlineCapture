# Operation Map Scene Split And Generator Implementation Tracker

Date: 2026-07-14
Status: Shared-foundation implementation in progress; map-delivery direction pending R&D
Workflow path: serial validated commits directly on `main`
Design sources: `../3D_SingleMap_Gameplay_Direction.md`, `../Level_And_Mission_Content_Plan.md`, `../3D_Operation_Map_Texture_Mask_Workflow.md`, `../M01_FirstContact_Production_Contract.md`, `gameplay_solid_ecs_contract.md`, `performance_regression_contract.md`

## Objective

Replace the current single hardcoded Match-map assumption with a reusable, config-driven operation-map pipeline:

```text
Mission -> ScenarioSetup -> OperationMapDefinition -> OperationMap scene/subscene
                                                       |
                                                       +-> static presentation manifest/chunks

Menu shell -> Match.unity runtime shell -> selected OperationMap loaded additively
```

The current large desert/base map must remain playable and become the first reusable operation map. It must not be deleted, flattened into a giant mesh, or copied together with its generated presentation chunks. `Match.unity` becomes a stable runtime shell only after the existing map has a validated map-specific bake, load, camera, minimap, authoring-conversion, and rollback path.

## Current Shared-Foundation Scope: 2026-07-15

Implementation is temporarily restricted to work that is required by both an editor-authored map direction and a runtime scene-based map direction. Runtime scene-based map R&D is proceeding separately. No delivery-direction-specific implementation may begin until that R&D is accepted or rejected and this tracker is updated with the selected direction.

| Tracker Area | Current Disposition |
|---|---|
| Phase 0 baseline, ownership inventory, and rollback | Active; shared by both directions. |
| Phase 1 typed map/scenario identity and metadata contracts | Active only where independent of asset production and loading technology. |
| Phase 2 ownership of the current map's existing static presentation | Active only as required to preserve and split the current baked map safely. Future-map presentation generation remains undecided. |
| Phase 3 current-map compatibility registration | Active; shared by both directions. |
| Phase 4 non-destructive ownership split from `Match.unity` | Active; this is the primary implementation objective. |
| Phase 5 readiness, one-active-map, failure-unwind, and teardown contracts | Contract work only. Concrete Addressables or runtime-scene loading/unloading implementation is later. |
| Phase 6 bounds, grid, surface, blocker, camera, minimap, runway, helipad, and movement metadata | Active; shared by both directions. |
| Phase 7 and Phase 9 scenario/map ids and map-neutral mission anchors | Shared contract work only; physical map rollout remains later. |
| Phase 10 parity, architecture, ECS, memory, FPS, GC, camera, minimap, and rollback validation | Active when triggered by shared-foundation changes. Addressables/build-layout checks remain later. |
| Phase 2A local Addressables packaging | Later, only if the selected direction requires it. |
| Phase 8 editor-time texture/mask generator | Later, only if the editor-authored direction is selected. |
| Phase 11 remote content migration | Later and independently gated. |

The checklist count remains unchanged so deferred work is not lost or misreported as complete. Items marked later must not be implemented, checked off, or used to block the shared `Match.unity` ownership split.

### Execution Labels While Map-Direction R&D Is Open

These labels are normative. A task without a `Shared now` disposition must not be implemented during the current workstream.

| Label | Meaning |
|---|---|
| `Shared now` | Required whether maps remain editor-authored scenes or become runtime scene-based maps. May be implemented now. |
| `Shared contract now` | Loader/generator-neutral data, readiness, failure, or teardown contract only. No concrete loading or content-production implementation. |
| `Later - direction decision` | Depends on the selected map production/loading direction. Do not implement yet. |
| `Later - editor direction only` | Editor generator or editor-authored future-map production. Implement only if that direction is accepted. |
| `Later - delivery` | Addressables, remote delivery, concrete loading/unloading, and packaging. Implement after the map direction is selected. |

The current implementation allowlist is therefore limited to:

1. All Phase 0 reproducibility, ownership classification, validation baseline, and rollback work.
2. Phase 1 typed ids and loader-neutral map/scenario metadata contracts; asset-provider, Addressables, and concrete scene-reference fields remain later.
3. Phase 2 compatibility work needed to preserve the current map's existing static-presentation ownership during the split; future-map generation remains later.
4. Phase 3 registration of the current map by stable id without choosing or implementing a loader.
5. Phase 4's non-destructive ownership split of the existing `Match.unity` map and its current bake products.
6. Phase 5 ECS/readiness/failure/teardown contracts only; every concrete load/unload step remains later.
7. Phase 6 loader-neutral bounds, surface, grid, blocker, camera, minimap, runway, helipad, and movement metadata.
8. Phase 10 validation only where exercised by the shared work above.

Explicitly out of scope until the R&D decision: Phase 2A Addressables, concrete Phase 5 loading/unloading, future physical M01/Skirmish maps, the entire Phase 8 editor generator, runtime map generation, all-map packaging, and Phase 11 remote content. Phases 7 and 9 may retain design ids and anchor requirements, but no physical future-map implementation may begin.

## Audit Result: 2026-07-14

The original draft had the right product direction but understated the current bake/runtime coupling. The live map is not only `Match.unity`; it is the following owned set:

| Current Contract | Audited Value |
|---|---|
| Canonical source scene | `Assets/Game/Scenes/Match.unity` |
| ECS subscene | `Assets/Game/Scenes/Match/MatchSubScene.unity` |
| Static presentation manifest | `Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationManifest.asset` |
| Manifest schema | `1` |
| Canonical dependency hash | `0a587783351110d16353575d15d1b5cd` |
| Presentation content hash | `9eebc7c8aa774d5f505cb684099d133a` |
| Chunk size | `32` world units |
| Generated chunk scenes | `514` |
| Manifest source entries | `16,542` |
| Integrity ledger | `Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationSceneIntegrity.json` |
| Bake owner | `StaticMapPresentationBaker` |
| Runtime stream owner | `MenuBootstrapCompositionSystemHelper` through `StaticMapPresentationStreamer` |
| Android build owner | `StaticMapAndroidBuildSceneResolver` |
| Match reference binder | `MatchSceneView` |

These values are an inspection snapshot, not an accepted Phase 0 baseline. Phase 0 must reproduce them through Unity validation and record the current dependency state before scene edits.

### Corrections Made By This Audit

- Do not duplicate `Assets/Game/GeneratedStaticMapPresentation`. Generated chunks and the integrity ledger are manifest-owned outputs, not reusable source assets.
- Do not extract the map before the baker, manifest ownership, scene wiring, Android build resolver, and streamer can select a map-specific manifest.
- First register the current map as a logical compatibility operation map while it still lives in `Match.unity`; this creates no scene or bake churn.
- Generalize static presentation ownership per operation map before creating the extracted scene. A bake for one map must never delete or rewrite another map's outputs.
- A duplicated source scene must receive a new scene GUID. Shared meshes, materials, textures, prefabs, and any assets that are moved rather than duplicated must retain their existing `.meta` GUIDs.
- `MapBuildingPlacementConfig`, `MapVehiclePlacementConfig`, map-surface references, lightmaps/probes, `MatchSubScene`, and hierarchy-path-based authoring conversion are part of map ownership and must be migrated with explicit parity checks.
- Invalid operation-map ids fail closed with a typed reason. The current map is an explicit compatibility id, not a silent fallback that hides bad scenario data.
- The texture/mask operation-map generator produces canonical source/metadata inputs. The static presentation baker consumes a reviewed canonical map and produces streamable render chunks. Their output roots and cleanup ownership remain separate.

## Non-Negotiable Safety Rules

- Do not destructively move, delete, or strip map content from `Assets/Game/Scenes/Match.unity` or `Assets/Game/Scenes/Match/MatchSubScene.unity` until the extracted operation map passes parity and rollback gates.
- Do not copy, hand-edit, or rename files under `Assets/Game/GeneratedStaticMapPresentation`; use the authoritative baker only.
- Do not let a new per-map bake overwrite the current schema-v1 manifest, integrity ledger, or 514 chunk scenes during the compatibility period.
- Preserve Unity `.meta` GUIDs for moved assets. Create a new GUID for a duplicated scene or asset; never copy one `.meta` file to two live paths.
- Keep current map baking deterministic. An identical second bake must report zero scene writes and zero stale deletions.
- Keep manifest cleanup fail-closed and restricted to paths previously owned by the same operation-map manifest.
- Keep `Match.unity` as the enabled base match scene until the operation-map load route is accepted. Load operation maps additively through the existing scene lifecycle.
- Do not combine all missions into one giant scene or include every mission's generated chunks in every build without an explicit build-content policy and size evidence.
- Do not introduce managers, controllers, facades, service locators, broad replacement shells, or new updating `MonoBehaviour` loops.
- Keep runtime gameplay policy in ECS data and systems. Scene references may use a non-updating serialized-reference view; loading, authoring, baking, UI, and Unity-object projection remain narrow managed boundaries.
- Keep Campaign map generation editor-time, deterministic, reviewed, and committed. Runtime procedural generation is out of scope for this tracker.
- Do not begin player-facing M01 implementation while the hold in `../M01_FirstContact_Production_Contract.md` remains active.

## Direct-Main Serial Workflow

Operation-map work now runs serially in `/Users/farhad/Projects/WarlineCapture-Clone` without secondary clones, Git worktrees, or pull requests.

- Work on one dependency-ready shared-foundation slice at a time directly on `main`.
- Keep the slice allowlist narrow and do not mix scene extraction, shell cutover, generated presentation output, or unrelated gameplay changes.
- Run `git diff --check`, affected focused tests, compile checks, architecture/naming guardrails, and Unity/device validation according to risk before committing.
- Review the complete local diff and record truthful commands, results, logs, risks, and untested paths in this tracker or its linked evidence report.
- Commit and push only stable validated changes. Do not mark unresolved ownership evidence or deferred direction-specific work complete.
- Preserve independently revertable commits for scene extraction and later shell cutover even though both are integrated directly through `main`.

Historical PR references in the validation log remain immutable audit evidence for work completed before this workflow changed; they do not require future slices to use PRs.

## Current Map Disposition

The current large map becomes the first reusable operation map through two stages.

| Stage | Identity / Path | Rule |
|---|---|---|
| Compatibility | `opmap.skirmish.desert_base_01` points to the current `Match.unity` map roots and schema-v1 manifest | No scene duplication, no map-content movement, no bake churn. |
| Extracted target | `Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity` plus an optional map-owned subscene | Created only after per-map bake ownership exists; generated outputs live under a map-specific generated root. |
| Match shell target | `Assets/Game/Scenes/Match.unity` | Keeps camera/runtime bootstrap and shell references; no longer owns map geometry or map-specific authored placements after cutover. |

Primary use remains Skirmish, air/armor sandbox, fuel/logistics validation, and base-defense testing. The map may support multiple `ScenarioSetup` definitions. It should not become M01 merely because it already exists; M01 uses a dedicated smaller map unless visual and tutorial-readability evidence explicitly accepts a bounded reuse.

## Accepted Operation-Map Portfolio, Generation, And Distribution Decision

Accepted by the product owner on 2026-07-14. The production model is a hybrid editor-generated pipeline, not full runtime map generation:

- Generate deterministic terrain, masks, roads, vegetation candidates, reserve zones, and base-layout candidates in the Editor from versioned inputs.
- Review and adjust every Campaign/Operation map before accepting its canonical source scene.
- Bake map surface/height data, blockers, ECS subscenes, static presentation chunks, combined meshes, minimap data, runways, helipads, mission anchors, and camera bounds before shipping.
- Keep runtime variation data-driven through `ScenarioSetup`: objectives, factions, deployments, encounters, resources, weather/time profile, optional decorations, and mission events may vary without rebuilding physical topology.
- Reserve any future runtime procedural generation for a separately designed, constrained Skirmish-only tile contract. It must not replace authored Campaign/Operation maps or bypass the accepted bake, path, camera, aircraft, minimap, package-size, and QA gates in this tracker.

This direction preserves deterministic mission scripting and the current mobile render/runtime architecture. Full runtime generation would either lose or require runtime replacements for the current ECS scene bake, static presentation chunks, combined static meshes, immutable surface/height blobs, blocker/path data, minimap raster, runway/aircraft paths, cinematic anchors, and pre-release visual validation.

### Current-Map Size Evidence And Twelve-Map Planning Estimate

The following repository measurements are a local planning snapshot, not an accepted release budget:

| Current map-owned source/bake category | Measured size |
|---|---:|
| `Assets/Game/Scenes/Match.unity` | `52.62 MiB` |
| `Assets/Game/Scenes/Match/` subscene, reflection probes, and lighting data | `1.85 MiB` |
| `Assets/Game/GeneratedStaticMapPresentation/` non-meta manifest/chunk output | `57.15 MiB` |
| `Assets/Game/GeneratedCombinedMeshes/` generated mesh assets | `50.97 MiB` |
| `Match_Map_MapSurfaceData.asset` | `4.79 MiB` |
| Current map building/vehicle placement configs | `0.22 MiB` |
| **Approximate map-specific source plus checked-in bake output** | **`167.60 MiB`** |

At equal complexity, twelve physical operation maps would therefore occupy approximately `1.96 GiB` under `Assets` before `.meta` overhead, imported `Library` artifacts, platform bake caches, and build intermediates. The additional eleven maps beyond the current one would add approximately `1.80 GiB` of source plus checked-in bake output.

The current Android APK provides additional player-build evidence:

- compressed APK file length: `442,968,453` bytes;
- current `MatchSubScene` Entities stream: `9,265,580` bytes;
- current `MatchSubScene` content archive: `65,536,588` bytes;
- all `525` packaged Unity level files, including the runtime scenes and `514` static presentation chunks: `3,034,322` compressed bytes in aggregate.

Shared meshes, materials, textures, prefabs, shaders, animation data, audio, and UI must remain globally referenced and packaged once. Map-specific source scenes, surface data, probes, combined meshes, ECS streams/content archives, manifests, and static chunks remain incremental. Until a representative second map is built and compared against a one-map release artifact, use **`80-110 MB` compressed per map** and **approximately `100 MB` per map** as the package-planning range and midpoint. On the current `443 MB` APK baseline, bundling eleven additional current-scale maps is estimated to produce a complete APK of approximately **`1.32-1.65 GB`**. Unique map lightmaps, texture sets, or duplicated shared art can exceed this range and are prohibited without explicit evidence and approval.

The accepted staged delivery direction is:

- **Initial integrated build:** bundle every approved operation map in the application as local Addressables content. The game must be fully playable offline and must not require a remote catalog, content download, or Google Play Asset Delivery package.
- Keep one independently addressable pack boundary per map even while every pack is local. Do not flatten all maps into one bundle and do not replace stable Addressables keys with direct scene paths.
- Resolve shared global art from one explicit shared dependency group so bundling all maps does not duplicate common meshes, materials, textures, animation data, audio, or shaders.
- Load only the selected map's source scene, metadata, manifest, surface data, and nearby presentation chunks. Bundling all maps on disk does not permit loading all heavyweight map content into memory.
- **Later distribution milestone:** move selected per-map groups from local Build/Load paths to remote HTTPS/CDN paths without changing scenario data, operation-map ids, gameplay ECS contracts, or scene-loading call sites.
- Keep Google Play Asset Delivery out of the initial implementation. It may be evaluated later as a delivery adapter, but Addressables remains the canonical content identity and loading layer.
- Establish authoritative one-map, two-map, and all-approved-maps package costs before treating the all-bundled layout as a production store release. The current `1.32-1.65 GB` twelve-map estimate is acceptable only as a planning warning, not as release approval.

## Normative Addressables Packaging And Delivery Contract

This section is the implementation specification for operation-map content. It distinguishes the accepted **local all-bundled milestone** from the later **remote content milestone**. Implementing local groups must not prematurely add download UI, network retries, remote catalogs, or a second delivery backend.

### Audited Baseline And Configuration Gap

Repository inspection on 2026-07-14 found Addressables package `3.1.0`, only the existing local groups, an undefined `Remote.LoadPath`, no configured remote catalog paths, and no Google Play Asset Delivery package. No project setting was changed by this documentation audit.

| Concern | Current repository state | Required initial state | Later remote state |
|---|---|---|---|
| Canonical loading API | Addressables is installed but operation maps are not grouped | Addressables for every operation-map scene and heavyweight asset | Unchanged |
| Map content location | Existing project-local content | Local BuildPath/LoadPath included with the player | Selected map groups use versioned HTTPS/CDN BuildPath/LoadPath |
| Catalog | Existing local catalog behavior | Local catalog; no network update on startup | Remote catalog plus hash, checked only from a non-match shell state |
| Availability | Content present in application | Every catalog-approved map is available offline | Built-in maps remain local; remote maps have explicit availability/download state |
| Android delivery | No accepted asset-delivery package | Normal player/AAB content produced by Addressables | CDN first; Play Asset Delivery only after a separate accepted proof |
| Build orchestration | No operation-map-specific build contract | Explicit Addressables content build before player build | Full-build and content-update pipelines with archived content state |

### Stable Content Identity

The following identities are stable across local and remote delivery. Changing a map from bundled to downloadable changes content placement and catalog metadata, not gameplay identity or asset addresses.

| Identity | Required format | Example |
|---|---|---|
| Operation-map id | `opmap.<mode-or-chapter>.<slug>` | `opmap.skirmish.desert_base_01` |
| Content-pack id | `opmap-pack.<mode-or-chapter>.<slug>` | `opmap-pack.skirmish.desert_base_01` |
| Address prefix | `operation-map/<operation-map-id>/` | `operation-map/opmap.skirmish.desert_base_01/` |
| Pack label | `operation-map-pack-<sanitized-slug>` | `operation-map-pack-skirmish-desert-base-01` |
| Content version | monotonically increasing positive integer plus immutable content hash | `7` plus SHA-256/hash string |
| Presentation partition id | deterministic map-relative region id | `region-04-07` |

Required addresses are:

- `operation-map/catalog`
- `operation-map/<id>/definition`
- `operation-map/<id>/source-scene`
- `operation-map/<id>/map-surface`
- `operation-map/<id>/static-manifest`
- `operation-map/<id>/minimap-raster`
- `operation-map/<id>/building-placements` when required by compatibility data
- `operation-map/<id>/vehicle-placements` when required by compatibility data
- `operation-map/<id>/presentation/<chunk-id>` for streamable presentation scenes

The optional ECS subscene is not assigned an independent public address until the Entities packaging proof below passes. Generated Entity Scene stream/content archive files must never be hand-addressed, copied, renamed, or uploaded independently from Unity's supported scene dependency chain.

### Group Topology And Bundle Partitioning

Use these exact logical groups. `<slug>` is the sanitized operation-map id suffix and is stable once published.

| Group | Initial Build/Load path | Bundle mode | Contents |
|---|---|---|---|
| `Operation Maps - Catalog` | Local / Local | Pack Together | Small `OperationMapCatalogConfig` and small definition/config records only. No map manifest, scene chunks, textures, or surface payloads. |
| `Operation Maps - Shared` | Local / Local | Pack Together by asset role when necessary | Shared binary art used by multiple maps. Assets enter this group only after dependency analysis proves reuse. |
| `Operation Map - Local - <slug> - Core` | Local / Local | Pack Together | One map's source scene, definition, surface data, static manifest, minimap raster, placement configs, and map-exclusive core dependencies. Unity may place scene and non-scene assets in separate bundles. |
| `Operation Map - Local - <slug> - Presentation` | Local / Local | Pack Together By Label | Map-specific streamable presentation chunk scenes partitioned into deterministic regions. |

Do not put every map in one Addressables group. Do not use `Pack Separately` for hundreds of chunk scenes because bundle/file overhead would grow with every chunk. Presentation chunks use `Pack Together By Label` with exactly one partition label per chunk, targeting bounded regional bundles of approximately 16 to 32 chunk scenes; the final region size is accepted from measured load latency, memory, and bundle-count evidence.

Required labels are:

- `operation-map`
- `operation-map-local` for the initial milestone
- one `operation-map-pack-<slug>` label per map
- one role label from `operation-map-role-definition`, `operation-map-role-source-scene`, `operation-map-role-metadata`, `operation-map-role-presentation`, or `operation-map-role-entities`
- one `operation-map-partition-<slug>-<region-id>` label on each presentation chunk scene

Every Addressables entry has one stable address, one pack label, and one role label. Presentation chunks additionally have exactly one partition label. Editor validation rejects missing, duplicate, foreign-map, or multiple-partition labels.

### Initial Local Bundle Schema

- Use LZ4 bundle compression to preserve acceptable local random-access/load behavior. Do not use uncompressed bundles in a player and do not select LZMA without measured transition evidence.
- Enable bundle CRC and cache validation according to the project's accepted platform profile. Hash-derived bundle naming is required so stale artifacts cannot masquerade as current content.
- Keep `Unique Bundle IDs` disabled for the local milestone. The later remote workflow may retain that setting only if catalog updates are restricted to a shell state with no loaded operation-map handles.
- Addressables content is built explicitly before the player build. CI fails closed when the catalog, bundles, build layout, or operation-map validation report is absent or stale; it must not silently depend on Editor Fast Mode.
- The all-bundled player contains every map approved by `OperationMapCatalogConfig`, but the runtime still retains handles only for one selected map and its bounded presentation working set.
- No remote request, download-size query, dependency download, catalog update, or cache-removal command runs in the initial local-only gameplay path.
- Remote content must never introduce scripts, assemblies, MonoScripts, or code-dependent serialized types not already present in the installed player.

### Shared Dependency And Duplication Rules

- A shared mesh, material, texture, shader, animation clip, audio clip, or prefab is assigned to `Operation Maps - Shared` only when at least two map groups depend on the same GUID and explicit sharing reduces measured duplication.
- Map-generated assets such as combined meshes, map surface data, minimap rasters, probes/lightmaps, manifests, integrity ledgers, and presentation chunks remain map-owned even if filenames are similar.
- A shared binary asset GUID may not be copied into a map-owned generated folder. Maps reference the canonical asset and preserve its `.meta` GUID.
- Addressables Analyze and Build Layout inspection are mandatory. CI reports duplicated dependency bytes by GUID and fails when an unapproved shared dependency is duplicated across operation-map bundles.
- Sharing must not create a global bundle so large that loading one map retains unrelated maps. If an asset family is large and only conditionally shared, split it by stable role/family rather than creating one universal content bundle.

### Local Load, Activation, And Release Lifecycle

1. The Match shell initializes Addressables once through the existing composition lifecycle.
2. Composition loads `operation-map/catalog`, resolves one validated catalog entry, and verifies schema, operation-map id, local inclusion, content version, and content hash before loading a scene.
3. `OperationMapSceneLoadingSceneSystemHelper` loads `operation-map/<id>/source-scene` additively and retains its `AsyncOperationHandle<SceneInstance>` for the entire scene lifetime.
4. Scene activation is immediate in the initial implementation. Do not use `activateOnLoad: false`; a deferred scene activation blocks Addressables async operation processing behind it.
5. Selected core assets are loaded lazily by stable address and their handles are retained while the scene/map consumers use them.
6. Static presentation uses a handle-owning implementation of the existing scene API boundary. It loads chunk scenes by stable chunk address and releases each retained scene handle only after the streamer has drained it.
7. Readiness is published only after scene identity, ECS authored content, surface data, metadata, canonical-renderer ownership, and required presentation preload belong to the same transition generation.
8. Teardown marks readiness false, quiesces map consumers, drains chunk scenes, unbinds presentation, disposes map-owned blobs, unloads the source scene through its retained handle, releases asset handles, and clears generation state.
9. Every successful or failed operation releases each acquired handle exactly once. Releasing a failed handle remains required.

Synchronous `WaitForCompletion` is prohibited. Gameplay systems never inspect an `AsyncOperationHandle`, call Addressables, or poll local bundle state.

### Later Remote Migration Contract

The remote milestone is intentionally deferred, but its migration path is fixed now:

- Clone/move selected `Operation Map - Local - <slug> - Core/Presentation` groups to `Operation Map - Remote - <slug> - Core/Presentation` schemas with remote BuildPath/LoadPath. Addresses, pack labels, partition labels, operation-map ids, and gameplay config links do not change.
- Keep at least one recovery/tutorial map and the Match shell local. A missing network must never make the application unable to enter its menu or a supported offline mode.
- Enable a remote catalog and upload its catalog JSON, hash, and bundles atomically to a versioned HTTPS/CDN release root. Catalog update checks occur only in Menu or another shell state where no operation-map handle is loaded.
- Archive `addressables_content_state.bin` for every platform and published full content build. Content updates are generated only from the exact archived state for that release line.
- Query `GetDownloadSizeAsync(packLabel)` before download. Cached/current content reports zero. Perform disk-headroom validation before starting.
- Download with `DownloadDependenciesAsync(packLabel)` and publish byte-based progress from `GetDownloadStatus`, dirty/version-gated to UI at no more than 4 Hz.
- Permit one content operation at a time. Retry only typed transient network failures, with bounded timeout and retry policy; hash/schema/version failures require a new catalog/content release rather than blind retry.
- Verify required addresses, pack label, content version, expected hash, and zero remaining download bytes before marking the map available.
- Cancellation is cooperative: stop activation, release operation handles, and report a typed canceled result. Do not claim partially downloaded cache bytes were removed.
- Remove a pack only when that map is inactive and no related handle remains. Use `ClearDependencyCacheAsync(packLabel)` and report that future play requires redownload.
- A catalog update is never applied during a match. Catalog changes are processed before loading content or after all operation-map scenes/assets have been unloaded.
- Google Play Asset Delivery remains a future adapter decision. It requires a separate package/API/device proof and Play test-track evidence; it must not fork gameplay identity or scene-loading policy.

### Entities Subscene Packaging Gate

Before any operation map is declared valid, prove on Editor and Android that loading its Addressable source scene also resolves and loads the expected Entity Scene stream/content archive through Unity's supported dependency chain. The validation records source-scene GUID, subscene GUID, Entities scene hash, stream/archive sizes, entity counts, and readiness time.

If a remote proof later fails, do not hand-copy generated Entities files and do not invent a custom runtime archive loader. Keep that map local until a supported Entities/Addressables packaging path is designed, tested, and documented.

### Typed Failure Contract

| Failure code | Trigger | Initial local behavior | Later remote behavior |
|---|---|---|---|
| `InvalidMapId` | Catalog has no exact id | Reject transition; remain in shell | Same |
| `MapNotIncluded` | Entry exists but is excluded from this build/catalog | Reject with actionable diagnostic | Offer availability/download route only when remote metadata is valid |
| `CatalogSchemaMismatch` | Catalog schema unsupported | Fail closed | Fail closed; do not download |
| `ContentVersionMismatch` | Definition, pack, manifest, or catalog versions disagree | Fail closed and report build defect | Refresh catalog only from shell, then fail closed if still incompatible |
| `ContentHashMismatch` | Expected and resolved hashes differ | Fail closed and report corrupt/stale build | Clear only the inactive affected cache, then bounded redownload |
| `MissingRequiredAddress` | Definition/core/scene/manifest/surface address absent | Fail closed and report build defect | Fail before activation; do not partially start match |
| `SceneLoadFailed` | Addressable scene operation fails | Release acquired handles and return to shell | Same plus typed network/cache cause |
| `EntitiesContentNotReady` | Expected subscene content does not become ready | Abort activation and unload map | Same; never bypass authored ECS content |
| `InsufficientStorage` | Later download lacks required headroom | Not applicable | Refuse download before transfer |
| `NetworkUnavailable` | Later remote map is not cached and network is unavailable | Not applicable | Keep local maps playable and expose typed offline status |
| `OperationCanceled` | User/session cancels pending remote operation | Not applicable | Release handles, do not activate, preserve truthful cache state |

Control flow uses typed enums/reason codes. Diagnostic strings are bounded and produced only on transition/result changes, never every frame.

### Performance, Memory, And Storage Budgets

- Steady-state Addressables/map orchestration: `0 B/frame` managed allocation after readiness.
- Active heavyweight content: one operation map, one full map surface blob, one static manifest/index, and a bounded presentation chunk working set.
- Concurrent content operations: one. Concurrent source-map loads: one unless a separately measured transition budget accepts overlap.
- Local map transition progress publication: dirty/versioned, maximum 10 Hz. Later remote byte-progress publication: maximum 4 Hz.
- Presentation partition target: approximately 16 to 32 chunk scenes per bundle, accepted only after bundle-count, chunk-latency, peak-memory, and Android storage evidence.
- Provisional compressed incremental map budget: `80-110 MB`; warn at `100 MB` and require explicit approval above `110 MB` until measured budgets replace these values.
- Later remote download disk headroom: at least the greater of twice the reported download size or reported size plus `256 MiB`, unless platform evidence establishes a stricter policy.
- No all-map manifest preload, no broad Addressables label load for heavyweight content, no synchronous completion, and no per-frame catalog/address lookup.

### Build, CI, And Release Artifacts

Every all-bundled build produces and archives:

- Addressables catalog and hash;
- local bundle directory and checksums;
- Addressables Build Layout report;
- operation-map catalog validation report;
- per-map address/label/group/partition manifest;
- duplicate-dependency report with byte totals by GUID;
- per-map compressed bundle bytes and all-maps aggregate bytes;
- Entities stream/content archive identities and sizes;
- player APK/AAB size, installed size, startup/load time, peak/retained memory, and sustained device performance evidence.

CI performs an explicit Addressables content build followed by the player build. Editor validation must include `Use Existing Build` or the equivalent real-bundle path; Fast Mode alone is insufficient. A later remote pipeline additionally archives the platform-specific content-state file, remote catalog/hash, uploaded bundle checksums, content-update report, and CDN release root.

Official implementation references:

- [Unity Addressables remote content](https://docs.unity3d.com/Packages/com.unity.addressables@3.1/manual/remote-content-intro.html)
- [Unity Addressables scene loading](https://docs.unity3d.com/Packages/com.unity.addressables@3.1/manual/LoadingScenes.html)
- [Unity Addressables bundle packing](https://docs.unity3d.com/Packages/com.unity.addressables@3.1/manual/PackingGroupsAsBundles.html)
- [Unity Addressables operation handles](https://docs.unity3d.com/Packages/com.unity.addressables@3.1/manual/AddressableAssetsAsyncOperationHandle.html)
- [Unity Addressables content updates](https://docs.unity3d.com/Packages/com.unity.addressables@3.1/manual/ContentUpdateWorkflow.html)
- [Android Play Asset Delivery](https://developer.android.com/guide/playcore/asset-delivery)
- [Android Play Asset Delivery testing](https://developer.android.com/guide/playcore/asset-delivery/test)

## Target Ownership Contract

### Match Runtime Shell

`Match.unity` owns the world camera, match lighting/post-processing boundary, match bootstrap reference binder, input/HUD integration references, and session lifecycle. `MatchSceneView` remains a serialized-reference binder and forwards Unity lifecycle into existing composition; it does not choose map policy or search scenes.

### Operation Map

An operation-map source scene owns canonical map geometry, map lights/probes that are intentionally map-specific, map authoring roots, map surface, map-specific ECS subscene references, and one non-updating serialized-reference view when required. That view may expose references but must contain no `Update`, `LateUpdate`, gameplay decisions, scene search, or service-location behavior.

`OperationMapDefinition` owns typed metadata and references:

- stable operation-map id and schema/content version;
- canonical source scene and optional subscene reference;
- static presentation manifest reference;
- map bounds, camera bounds, grid size, surface/height metadata, and blocker/path metadata;
- minimap projection and planning/battle camera ids;
- typed objective, spawn, deployment, build, civilian, hostile, base, resource, runway, helipad, lane, and debug anchors;
- map-owned building/vehicle placement config references where the compatibility path still needs them.

Large blocked-cell and height datasets must be represented by immutable blobs or chunked/compressed assets suitable for Burst/ECS consumption. Do not copy multi-million-cell managed arrays into runtime read models.

### Scenario Setup

`ScenarioSetup` owns mission/skirmish policy: operation-map id, starting units/buildings/resources, enemy setup, objectives, stars, rewards, allowed catalog, command restrictions, ARIA hooks, encounters, and feature gates. It does not own scene paths or inspect scene objects.

### Static Map Presentation

The existing static presentation pipeline remains the render optimization path. It must evolve from one global hardcoded manifest into map-scoped manifests with:

- operation-map id;
- canonical source scene GUID/path and dependency hash;
- map-specific output root and integrity ledger;
- deterministic chunk ids, bounds, source identity, and content hash;
- stale cleanup restricted to that manifest's prior owned paths;
- build inclusion controlled by a validated operation-map catalog/content policy.

`MenuBootstrapCompositionSystemHelper` may continue owning the existing streamer lifecycle. Map selection should supply the resolved manifest and camera to the existing boundary rather than introduce another broad per-frame owner.

## Technical Architecture Contract

Design status: normative implementation contract, audited 2026-07-14. Types marked **planned** do not exist yet. Any implementation that needs a different type name, assembly dependency, or ownership direction must update and re-audit this section before code is merged. Adding a similarly named parallel abstraction is not an acceptable shortcut.

This technical-design extension does not change checklist progress. Implementation remains at 0% until the checklist items below are completed and validated.

### Architecture Decision Summary

- Use the existing assemblies. Do not add an `OperationMap`, `MapRuntime`, or other catch-all asmdef unless a measured dependency problem proves that a new bounded assembly is necessary.
- `Game.Configs` owns small authoring/config assets and stable Addressables references. It must not reference `Game.Rendering` because `Game.Rendering` already references `Game.Configs`.
- `Game.Components` owns unmanaged ECS state, enums, buffers, and immutable operation-map blob layouts. It must contain no `UnityEngine.Object`, Addressables handle, scene object, managed collection, or gameplay presentation state.
- `Game.Runtime` consumes active map ECS data and immutable blobs. It must not load scenes/assets, inspect hierarchies, or reference the concrete `StaticMapPresentationManifest` type.
- `Game.Rendering` continues to own the concrete static-presentation manifest and rendering data. It must not choose missions, scenarios, or scene transitions.
- `Game.Composition` is the only runtime assembly allowed to resolve config references into concrete Unity scenes, Addressables assets, map views, map-surface blobs, and static-presentation bindings.
- `Game.Editor` owns scene splitting, generation, baking, catalog/build resolution, and deterministic validation. Generated runtime code or runtime map generation is out of scope.
- Scene and Addressables work is a managed Unity boundary. Do not create an empty `ISystem` wrapper around `SceneManager`, `Addressables`, `AsyncOperationHandle`, or `MonoBehaviour` references merely to claim ECS coverage.
- Immutable map lookup and gameplay math must remain Burst-compatible. Existing gameplay `ISystem`/jobs read active map components, `MapSurfaceBlob`, and operation-map metadata directly; they do not call managed config or composition helpers.
- No new `Manager`, `Controller`, `Facade`, `Service`, `ServiceLocator`, broad `Provider`, or updating `MonoBehaviour` is permitted.

### Required Assembly Dependency Direction

`A -> B` means assembly A may depend on assembly B. It does not authorize a new reference unless the responsibility table requires it.

```text
Game.Editor
  -> Game.Composition
     -> Game.Runtime
        -> Game.Components
        -> Game.Configs
        -> Game.Rendering.Contracts
     -> Game.Rendering
        -> Game.Components
        -> Game.Configs
        -> Game.Rendering.Contracts
     -> Game.Configs
        -> Game.Components
        -> Game.Catalog.Contracts
     -> Game.Authoring
  -> Game.Runtime
  -> Game.Rendering
  -> Game.Configs
  -> Game.Components
```

The concrete constraints are:

| Assembly | Operation-map responsibility | Allowed new dependencies | Forbidden dependency/behavior |
|---|---|---|---|
| `Game.Catalog.Contracts` | Stable catalog-facing ids/enums only if an existing contract cannot represent them. | None by default. | Unity objects, scene paths, Addressables, map blobs, gameplay systems. |
| `Game.Components` | Active-map ECS state, request/result buffers, readiness, bounds, metadata blob layouts. | Existing Unity Collections/Entities/Mathematics only. | `Game.Configs`, `Game.Rendering`, `Game.Composition`, `UnityEngine.Object`, managed arrays/lists/dictionaries. |
| `Game.Configs` | `OperationMapDefinition`, catalog, scenario setup, small serializable metadata, lazy heavyweight asset references. | Existing `Game.Components`, `Game.Catalog.Contracts`, Addressables. | A reference to `Game.Rendering` or a typed `StaticMapPresentationManifest` field; runtime scene loading; per-frame policy. |
| `Game.Rendering.Contracts` | Only a pure-data rendering contract if concrete rendering must cross an assembly boundary. | Existing Unity Collections only. | Config ownership, scene loading, Unity object manifests. Do not add a contract solely to mirror every manifest field. |
| `Game.Rendering` | `StaticMapPresentationManifest` schema and render data. | Existing `Game.Components`, `Game.Configs`, rendering dependencies. | Mission/scenario selection, Addressables orchestration, shell lifecycle. |
| `Game.Runtime` | Burst-compatible consumers of active map state, bounds, anchors, surface/path data. | Existing components/config/contracts dependencies. | Concrete rendering manifest, Addressables, hierarchy search, scene load/unload, managed cache ownership. |
| `Game.Composition` | Resolve catalog/config once, load/unload Unity content, bind scene view/surface/presentation, publish typed ECS state. | Existing configs, runtime, rendering, Addressables, Entities. | Gameplay decisions, map generation, a new update loop, broad global object discovery. |
| `Game.Authoring` | Optional ECS bakers only after the extracted map/subscene authoring route proves it needs them. | Existing components/configs. | Runtime loading and presentation policy. |
| `Game.Editor` | Deterministic generation, split, bake, validation, build inclusion. | Existing runtime assemblies plus Addressables Editor. | Player runtime dependencies or hand-editing generated chunk scenes. |
| `Game.Tests.Editor` / `Game.Tests.PlayMode` | Contract, ownership, lifecycle, deterministic output, allocation, and visual/playable probes. | Test-only references. | Production behavior hidden in test helpers. |

No planned runtime assembly may reference `Game.Editor`. `Game.Components`, `Game.Configs`, `Game.Runtime`, and `Game.Rendering` may not reference `Game.Composition`.

### Existing Types To Reuse Or Extend

| Existing type | Kind / assembly | Required disposition |
|---|---|---|
| `Game.Composition.MatchSceneView` | `sealed MonoBehaviour`, `Game.Composition` | Keep as shell binder/lifecycle forwarder. Remove map-specific serialized fields only after extracted-map parity passes. Do not make it choose a map or search loaded scenes. |
| `Game.Composition.MatchSceneReferenceSceneSystemHelper` | managed `sealed class`, `Game.Composition` | Keep for locating the shell once. Do not broaden it to locate arbitrary operation-map content every frame. |
| `Game.Composition.MatchStartSceneSystemHelper` | managed `sealed class`, `Game.Composition` | Extend its start gate to require accepted operation-map readiness; do not duplicate match-start state. |
| `Game.Runtime.SceneLifecycleSceneSystemHelper` | managed `sealed class`, `Game.Runtime` | Preserve Menu/Match shell lifecycle. Operation-map Addressables lifetime is a separate narrow composition boundary because arbitrary map assets and concrete manifests do not belong in this helper. |
| `Game.Composition.MapSurfaceRuntimeBootstrapSceneSystemHelper` | managed `sealed class`, `Game.Composition` | Reuse for the selected map's existing `MapSurfaceDataAsset`. It remains the sole owner/disposer of the persistent runtime `MapSurfaceBlob`. Do not copy surface cells into a second operation-map blob. |
| `Game.Components.MapSurfaceComponent` / `MapSurfaceBlob` | unmanaged ECS component/blob, `Game.Components` | Remain authoritative for heights, layered surfaces, roads, bridges, movement masks, and connectivity. Operation-map metadata references this active surface indirectly through ECS readiness. |
| `Game.Rendering.StaticMapPresentationManifest` | `sealed ScriptableObject`, `Game.Rendering` | Advance through an explicit schema migration to map identity and ownership. Do not move it into configs or load all manifests with the catalog. |
| `Game.Composition.StaticMapPresentationManifestIndex` | internal static class, `Game.Composition` | Validate and build the in-memory chunk index once per bind. Add operation-map identity/schema checks without adding per-frame allocations. |
| `Game.Composition.StaticMapPresentationStreamer` | managed `sealed class`, `Game.Composition` | Rebind to the selected manifest/camera. Preserve queue capacity 64, bounded scene-state checks (maximum 16 per update), drain-before-unload behavior, and no per-frame collection creation. |
| `Game.Editor.StaticMapPresentationBaker` | static editor class, `Game.Editor` | Refactor to accept a map-scoped bake input while retaining the current hardcoded menu command as a compatibility entry point. |
| `Game.Editor.StaticMapAndroidBuildSceneResolver` | static editor class, `Game.Editor` | Retain during migration, then delegate to catalog-selected map manifests. Never include every discovered generated scene. |
| `Game.Editor.StaticMapPresentationSceneWiring` | static editor class, `Game.Editor` | Wire a selected map's manifest explicitly and preserve current compatibility wiring until cutover. |
| `Game.Runtime.TacticalMapDefinition` | existing `sealed ScriptableObject`, `Game.Runtime` | Treat as legacy/prototype tactical-map data. Do not extend it into the 3D operation-map scene owner and do not make `OperationMapDefinition` inherit from it. Migrate only still-required M01 ids/anchors through an explicit editor conversion or scenario adapter, then deprecate it after parity. |

### Approved Planned Runtime Types

#### Config And Catalog Types

| Planned type | C# kind | Namespace / assembly | Planned file | Responsibility and constraints |
|---|---|---|---|---|
| `OperationMapDefinition` | `sealed ScriptableObject` | `Game.Configs` / `Game.Configs` | `Assets/Game/Scripts/Configs/OperationMapDefinition.cs` | Small canonical metadata for one map. Stores id/schema/content version, bounds/camera/minimap/anchors, and lazy `AssetReference` values for source scene, optional heavy metadata, map surface, placement configs, and static manifest. No concrete rendering type and no per-frame method. |
| `OperationMapCatalogConfig` | `sealed ScriptableObject` | `Game.Configs` / `Game.Configs` | `Assets/Game/Scripts/Configs/OperationMapCatalogConfig.cs` | Ordered catalog entries and staged delivery policy. The initial catalog includes every approved map as `BuiltInLocal`; heavyweight map data remains lazy. Composition builds one lookup at transition/launch, not in gameplay updates. |
| `OperationMapCatalogEntryConfig` | `[Serializable] struct` | `Game.Configs` / `Game.Configs` | same file | Stable map id, pack id, definition `AssetReference`, `OperationMapContentPackConfig`, inclusion flag, and sort/display metadata. It contains no scene path and no direct heavyweight object. |
| `OperationMapContentPackConfig` | `[Serializable] struct` | `Game.Configs` / `Game.Configs` | `Assets/Game/Scripts/Configs/OperationMapConfigModels.cs` | Delivery kind, stable pack label, content version/hash, minimum compatible app/content schema, expected compressed/download bytes, and optional remote release id. Initial entries are local; remote fields remain empty. |
| `ScenarioSetupConfig` | `sealed ScriptableObject` | `Game.Configs` / `Game.Configs` | `Assets/Game/Scripts/Configs/ScenarioSetupConfig.cs` | Mission/skirmish policy keyed by scenario id and operation-map id. Owns starting state, objectives, rewards, restrictions, feature gates, and ARIA hooks; owns no scene path or hierarchy reference. |
| `OperationMapBoundsConfig` | `[Serializable] struct` | `Game.Configs` / `Game.Configs` | `Assets/Game/Scripts/Configs/OperationMapConfigModels.cs` | World, camera, and playable bounds with finite-value validation. |
| `OperationMapCameraConfig` | `[Serializable] struct` | `Game.Configs` / `Game.Configs` | same file | Stable camera id, transform, projection settings, and clamp policy. |
| `OperationMapMinimapConfig` | `[Serializable] struct` | `Game.Configs` / `Game.Configs` | same file | Stable minimap id, projection origin/size, orientation, and lazy cached-raster reference. |
| `OperationMapAnchorConfig` | `[Serializable] struct` | `Game.Configs` / `Game.Configs` | same file | Stable anchor id, `OperationMapAnchorKind`, position/rotation/radius, faction/lane metadata. It is config data, not an ECS buffer element. |
| `OperationMapAnchorKind` | `enum : byte` | `Game.Components` / `Game.Components` | `Assets/Game/Scripts/Components/OperationMapComponents.cs` | Closed typed anchor taxonomy: spawn, objective, deployment, build, civilian, hostile, base, resource, runway, helipad, lane, camera, minimap, and debug. |
| `OperationMapDeliveryKind` | `enum : byte` | `Game.Components` / `Game.Components` | same file | `BuiltInLocal` and `RemoteAddressables`. Add `AndroidAssetPack` only after a separately accepted Play Asset Delivery proof. This enum describes availability; gameplay does not branch on it. |

All heavyweight fields in `OperationMapDefinition` must be lazy references. In particular, the schema-v1 compatibility manifest currently contains 16,542 source entries and mesh/material references; loading every map manifest with the catalog would violate memory and transition budgets.

Managed config ids are serialized as `string` for Unity authoring. `OperationMapCatalogValidator` validates non-empty canonical format, ordinal uniqueness, and UTF-8 capacity. Composition converts them once into `FixedString64Bytes` when publishing ECS state. Gameplay code never compares raw managed strings or scene paths.

Minimum serialized field ownership is fixed as follows:

| Asset | Required small fields | Required lazy/heavy references |
|---|---|---|
| `OperationMapDefinition` | operation-map id, schema version, content version/hash, bounds, camera records, minimap projection, anchor records, generation metadata hash | canonical source scene, optional subscene/heavy metadata, `MapSurfaceDataAsset`, concrete static manifest, cached minimap raster, compatibility building/vehicle placement configs |
| `OperationMapCatalogConfig` | catalog schema/version, ordered `OperationMapCatalogEntryConfig` records, all-bundled inclusion policy | No manifest, scene chunk, surface payload, texture, or mesh list. |
| `OperationMapCatalogEntryConfig` | map id, pack id, definition address/reference, inclusion flag, delivery kind, content version/hash, expected byte metadata | No source scene object, manifest, surface payload, chunk list, or downloaded-state mutation. |
| `ScenarioSetupConfig` | scenario/mission/map ids, starting-state ids, objective/reward/restriction/feature-gate data, ARIA hook ids | Scenario-specific heavy encounter/config assets only when required; no source scene, subscene, manifest, hierarchy path, or map renderer reference. |

Use ordinary `UnityEngine.AddressableAssets.AssetReference` fields for cross-assembly heavyweight references when the concrete type would create an assembly cycle. Resolve and type-check those handles in `Game.Composition`; editor validation confirms that each referenced asset has the expected concrete type before a build.

#### ECS State And Immutable Metadata

| Planned type | C# kind | Namespace / assembly | Responsibility and field contract |
|---|---|---|---|
| `OperationMapRootComponent` | `IComponentData` tag | `Game.Components` / `Game.Components` | Identifies the single operation-map lifecycle entity. Exactly one while a match world exists. |
| `OperationMapQueueComponent` | `IComponentData` | same | Monotonic last request id; no managed state. |
| `OperationMapLoadStateComponent` | `IComponentData` | same | Active request id, `OperationMapLoadStatusKind`, progress, generation, busy flag, and readiness bit mask. |
| `ActiveOperationMapComponent` | `IComponentData` | same | `FixedString64Bytes` operation-map/scenario/mission ids, schema/content version, and generation. Published only after activation succeeds. |
| `OperationMapBoundsComponent` | `IComponentData` | same | Burst-readable world/playable/camera min/max values. No `UnityEngine.Bounds` or `Rect`. |
| `OperationMapMetadataComponent` | `IComponentData` | same | `BlobAssetReference<OperationMapBlob>` and metadata hash. Its owning composition helper must dispose the blob exactly once during teardown/world disposal. |
| `OperationMapReadinessComponent` | `IComponentData` | same | Versioned source-scene, subscene, surface, authored-conversion, manifest, and required-preload flags. Match start reads one component instead of polling Unity objects. |
| `OperationMapLoadRequestElement` | `IBufferElementData` | same | Request kind, request id, `FixedString64Bytes` map/scenario ids, and activation flag. `Element` is used because this is an ECS dynamic-buffer element. |
| `OperationMapLoadResultElement` | `IBufferElementData` | same | Request/status/result code, ids, progress/generation, and `FixedString128Bytes` diagnostic message. Bounded history; old results are removed. |
| `OperationMapLoadRequestKind` | `enum : byte` | same | `Load`, `Unload`, `Switch`, `Retry`. |
| `OperationMapLoadStatusKind` | `enum : byte` | same | `None`, `Resolving`, `LoadingScene`, `LoadingSubScene`, `BindingMetadata`, `PreloadingPresentation`, `Ready`, `Draining`, `Unloading`, `Failed`. |
| `OperationMapLoadResultCode` | `enum : byte` | same | Typed accepted/duplicate/invalid-id/missing-asset/stale-content/load/bind/preload/unload failures; no string parsing for control flow. |
| `OperationMapBlob` | blob root `struct` | same | Immutable map-level anchors, camera records, minimap projection, logical lanes, and small lookup metadata. It must not duplicate `MapSurfaceBlob` cells, samples, connections, blocker grids, meshes, textures, or manifest source entries. |
| `OperationMapAnchorBlob` | blob element `struct` | same | `FixedString64Bytes` id, kind, transform/radius, faction/lane values. Linear lookup is acceptable for small anchor sets; add a sorted index only after measured evidence. |
| `OperationMapCameraBlob` | blob element `struct` | same | Stable camera id plus Burst-readable transform/projection/clamp data. |
| `OperationMapMinimapBlob` | blob value `struct` | same | Projection math and cached-raster identity only; no `Texture`, `Sprite`, or managed array. |

The operation-map blob is deliberately small and immutable. `MapSurfaceBlob` remains the sole large height/path/surface dataset. Mutable blockers and occupancy remain in their existing ECS owners rather than being copied into either blob.

The following ECS types are reserved for the deferred remote milestone and must **not** be implemented for the initial local-only build. They exist in this contract so later work extends the same data boundary instead of inventing a service/provider/controller shell:

| Deferred planned type | C# kind | Namespace / assembly | Responsibility and field contract |
|---|---|---|---|
| `OperationMapContentStateComponent` | `IComponentData` | `Game.Components` / `Game.Components` | One selected pack's availability, operation id, byte totals, progress basis points, version, generation, and `OperationMapContentStatusKind`. No managed handle or URL. |
| `OperationMapContentRequestElement` | `IBufferElementData` | same | Bounded request id, pack id, map id, request kind, expected version, and activation intent. |
| `OperationMapContentResultElement` | `IBufferElementData` | same | Bounded result history with request id, pack id, result code, byte totals, generation, and one bounded diagnostic. |
| `OperationMapContentRequestKind` | `enum : byte` | same | `CheckAvailability`, `Download`, `Cancel`, `Remove`, `RefreshCatalog`. |
| `OperationMapContentStatusKind` | `enum : byte` | same | `Unknown`, `BuiltIn`, `Checking`, `Available`, `Downloading`, `Removing`, `Unavailable`, `Failed`. |
| `OperationMapContentResultCode` | `enum : byte` | same | Typed success, canceled, offline, insufficient-storage, incompatible-version, hash, catalog, download, remove, busy, and active-map failures. |

#### Runtime And Composition Types

| Planned type | C# kind | Namespace / assembly | Planned file | Responsibility and lifecycle |
|---|---|---|---|---|
| `OperationMapSceneView` | `sealed MonoBehaviour` with no update methods | `Game.Composition` / `Game.Composition` | `Assets/Game/Scripts/Composition/OperationMapSceneView.cs` | Serialized references owned by the loaded map scene: map root, `MapSurfaceAuthoring`, map-owned placement configs, optional subscene reference, and source identity. Getters/binding only; no policy, hierarchy search, singleton, or self-registration loop. |
| `OperationMapSceneReferenceSceneSystemHelper` | managed `sealed class` | `Game.Composition` / `Game.Composition` | `Assets/Game/Scripts/Composition/OperationMapSceneReferenceSceneSystemHelper.cs` | Resolve exactly one `OperationMapSceneView` from the newly loaded scene's root list once per transition, using a reused/pre-sized list. Reject zero or multiple views. Never scan all loaded objects every frame. |
| `OperationMapSceneLoadingSceneSystemHelper` | managed `sealed class` | `Game.Composition` / `Game.Composition` | `Assets/Game/Scripts/Composition/OperationMapSceneLoadingSceneSystemHelper.cs` | Own catalog resolution result, Addressables scene/asset handles, one transition state machine, typed ECS request/result publication, failure rollback, and deterministic unload. Called by existing composition lifecycle; it introduces no `MonoBehaviour.Update`. |
| `StaticMapPresentationAddressablesSceneApi` | managed `sealed class` implementing the existing presentation scene API contract | `Game.Composition` / `Game.Composition` | `Assets/Game/Scripts/Composition/StaticMapPresentationAddressablesSceneApi.cs` | Initial local milestone owner for presentation chunk `AsyncOperationHandle<SceneInstance>` values. Maps stable chunk addresses to retained handles, enforces one load/unload per chunk, and releases every handle during drain/disposal. It does not own camera policy or a new update loop. |
| `OperationMapRuntimeBootstrapSceneSystemHelper` | managed `sealed class` | `Game.Composition` / `Game.Composition` | `Assets/Game/Scripts/Composition/OperationMapRuntimeBootstrapSceneSystemHelper.cs` | Convert small config metadata to one persistent `OperationMapBlob`, publish active metadata/bounds/readiness components, coordinate the existing map-surface bootstrap, and dispose only blobs it owns. |
| `OperationMapMetadataUtility` | Burst-compatible static class | `Game.Runtime` / `Game.Runtime` | `Assets/Game/Scripts/Systems/OperationMapMetadataUtility.cs` | Pure lookup/clamp/projection helpers over components/blob refs. No cached managed collections, Unity object access, logging, or hidden state. |

`OperationMapContentDeliverySceneSystemHelper` is a deferred remote-milestone managed `sealed class` in `Game.Composition`, planned at `Assets/Game/Scripts/Composition/OperationMapContentDeliverySceneSystemHelper.cs`. It will own catalog-check, size-query, download, cancellation, and cache-removal handles and publish the reserved content ECS state. It is not part of the initial all-local implementation and must not be created as an empty forwarding shell.

No new recurring operation-map `ISystem` is approved solely for loading. Scene/Addressables transitions are infrequent managed work and already require Unity APIs. New gameplay systems are allowed only when they own real data-parallel behavior. Such a type must be a `struct : ISystem`, use `[BurstCompile]` on `OnCreate`/`OnUpdate` where supported, declare `RequireForUpdate`, schedule jobs only when work exists, and be added to the Burst hot-path classification tests.

Existing Burst/jobified camera, minimap, movement, placement, aircraft, and objective systems should consume `ActiveOperationMapComponent`, `OperationMapBoundsComponent`, `OperationMapMetadataComponent`, and the existing `MapSurfaceComponent`; they must not receive a new managed map provider.

The lifecycle state machines remain nested and non-duplicated:

- `SceneLifecycleSceneSystemHelper` and `SceneLifecycle*` ECS data own only the outer Menu/Match shell transition.
- `OperationMapSceneLoadingSceneSystemHelper` and `OperationMap*` ECS data own selected content inside an already loaded Match shell.
- `MatchStartSceneSystemHelper` is the single join point: it waits for both the Match shell and the selected operation-map generation to be ready.
- Do not add operation-map ids to `SceneLifecycleSceneId`; that enum is a small shell-scene identity, not a content catalog.

### Approved Planned Editor Types

| Planned type | C# kind / assembly | Responsibility |
|---|---|---|
| `StaticMapPresentationBakeInput` | internal `readonly struct`, `Game.Editor` | Map id, source scene/root identity, output root, manifest path, integrity path, chunk size, and compatibility flags passed to `StaticMapPresentationBaker`. |
| `OperationMapCatalogValidator` | internal static class, `Game.Editor` | Validate ids, lazy references, unique scene/manifest ownership, scenario links, bounds, anchors, hashes, and shipping policy. |
| `OperationMapSceneSplitBuilder` | internal static class, `Game.Editor` | AssetDatabase-safe staged extraction of classified map roots; preserves source assets and Unity `.meta` GUIDs and never edits generated chunk scenes directly. |
| `OperationMapGenerationInput` | internal `readonly struct`, `Game.Editor` | Reviewed source pack paths, map id, seed/version, generation bounds, output ownership, and dry-run policy. |
| `OperationMapGenerationResult` | internal `readonly struct`, `Game.Editor` | Written/reused/stale counts, hashes, metadata bytes, validation result, and exact output paths. |
| `OperationMapTextureMaskGenerator` | internal static class, `Game.Editor` | Deterministically converts reviewed texture/mask inputs into canonical metadata and source-scene inputs. It never runs in a player and never writes static presentation chunks. |
| `OperationMapSourceSceneBuilder` | internal static class, `Game.Editor` | Deterministically creates/updates canonical source scene/subscene content from generation output while referencing shared assets. |
| `OperationMapAndroidBuildSceneResolver` | internal static class, `Game.Editor` | Resolves only catalog-approved local map source/chunk scenes and rejects stale, missing, duplicate, unapproved, or foreign-owned outputs. The existing resolver delegates here after compatibility tests pass. |
| `OperationMapAddressablesLayoutValidator` | internal static class, `Game.Editor` | Validates exact group names, local/remote schema, addresses, pack/role/partition labels, one-map ownership, partition size, shared dependencies, Entities scene linkage, and catalog inclusion. |
| `OperationMapAddressablesBuildReport` | internal `readonly struct`, `Game.Editor` | Immutable report model for per-map bundle bytes, partition count, duplicate bytes/GUIDs, required address coverage, Entities artifacts, and all-maps aggregate. It owns no build behavior. |

Editor production files should remain below the current source-growth ratchet (preferably below 500 lines each). Split generation, ownership, integrity, scene construction, and build resolution by responsibility instead of creating one large generator shell.

### Load, Activation, And Teardown Sequence

One operation-map transition follows this order:

1. Mission selection publishes one `OperationMapLoadRequestElement` containing typed ids only.
2. `OperationMapSceneLoadingSceneSystemHelper` resolves `OperationMapCatalogConfig` once and validates the selected `OperationMapDefinition` before any load begins.
3. The helper marks readiness false, drains/unbinds prior static presentation when switching, and unloads the prior map before loading another large map unless an accepted transition explicitly permits overlap.
4. The helper loads the selected source scene additively and retains the Addressables scene handle.
5. `OperationMapSceneReferenceSceneSystemHelper` resolves exactly one `OperationMapSceneView` from that scene and validates source identity.
6. The optional map ECS subscene is loaded/awaited through the accepted Entities path; authored conversion readiness is recorded.
7. Heavy assets are loaded lazily: selected `MapSurfaceDataAsset`, concrete `StaticMapPresentationManifest`, cached minimap raster, and compatibility placement configs only when required.
8. `OperationMapRuntimeBootstrapSceneSystemHelper` creates the small metadata blob once, invokes the existing map-surface bootstrap once, and publishes bounds/active/readiness ECS data.
9. The existing `StaticMapPresentationStreamer` binds the selected manifest and world camera, then reaches required preload readiness through bounded updates.
10. `MatchStartSceneSystemHelper` permits `BeginGameplay` only when all required readiness flags belong to the same transition generation.
11. During gameplay, config assets and scene handles are not polled by gameplay systems. ECS consumers read immutable active components/blobs.
12. Teardown disables readiness first, stops map-dependent gameplay work, completes/coordinates outstanding map consumers, drains presentation, clears map-owned ECS state, disposes owned blobs, unloads subscene/source scene, releases Addressables handles, and publishes a typed result.

A failure at any stage unwinds only resources acquired by that transition generation. It must not suppress canonical renderers, retain a stale active-map id, leak a blob/handle, or silently fall back to the large compatibility map in production.

### Data Lifetime And Memory Rules

| Data | Owner | Creation/load frequency | Disposal/release rule |
|---|---|---|---|
| Catalog lookup | `OperationMapSceneLoadingSceneSystemHelper` | Once per shell/session or when catalog version changes. | Clear on shell/world disposal. No rebuild per frame. |
| Definition/scenario assets | Addressables/composition transition | Selected entries only; small catalog metadata may remain resident. | Release selected handles after teardown unless an explicit cache budget retains them. |
| Static manifest/index | Addressables + existing streamer | Selected map only; index built once per bind. | Drain/unbind, release handle, clear arrays/sets before next map. |
| `OperationMapBlob` | `OperationMapRuntimeBootstrapSceneSystemHelper` | Once per successful map activation. | Dispose exactly once after map consumers are quiescent or during world disposal. |
| `MapSurfaceBlob` | existing `MapSurfaceRuntimeBootstrapSceneSystemHelper` | Once per selected map. | Use its existing owner-tagged disposal path; never dispose from operation-map metadata code. |
| Generator/source metadata | `Game.Editor` | Editor command only. | No player runtime copy beyond selected compressed assets/blob data. |
| Static chunk scenes | existing streamer/scene API | Camera-proximity bounded. | Drain before source map unload; no second streamer. |

### Performance And Allocation Budgets

- Steady-state operation-map orchestration must allocate `0 B/frame` managed memory after readiness. No LINQ, iterator allocation, string formatting, `ToArray`, hierarchy query, dictionary rebuild, or Addressables lookup in a gameplay frame.
- Loading may allocate managed objects required by Unity/Addressables, but each allocation source must be transition-scoped and absent from post-load profiling. Record peak transition memory and retained memory separately.
- Keep exactly one active large `MapSurfaceBlob` and one small `OperationMapBlob`. Do not duplicate the 2024x2024 (or future equivalent) surface/grid payload into config arrays, UI read models, minimap data, or operation metadata.
- Do not retain more than one full static manifest/index during normal gameplay. For switches, drain/release the old manifest before loading the next unless a measured transition budget explicitly permits overlap.
- Preserve the streamer's queue capacity of 64 and at most 16 scene-state checks per update. Any change requires focused streamer tests and profiler evidence.
- Root-scene enumeration is transition-only and uses reused/pre-sized storage. `FindObjectsByType`, `Resources.FindObjectsOfTypeAll`, repeated `GetComponentsInChildren`, and scene-wide searches are prohibited after bind.
- Map ids, scenario ids, anchor ids, and reason codes in ECS use bounded `FixedString64Bytes`/`FixedString128Bytes`. Editor validation rejects UTF-8 values that exceed capacity instead of truncating them.
- Anchor, camera, and minimap lookups are immutable. Use direct indices or small linear scans first; introduce a native hash map only when profiling proves lookup cost matters and its lifetime can be persistent/no-GC.
- Do not schedule a job every frame merely to copy unchanged map metadata. Jobs run only for genuine bulk generation or gameplay work and use dependency-correct persistent/read-only data.
- Minimap raster/projection is generated or loaded once, then marker updates remain dirty/version-gated. The compact minimap must not render or process the whole operation map every frame.
- Camera clamps and ground/air clearance use bounds and existing surface/blob queries. No repeated physics sweep is permitted as the primary map query path.
- Generated source/chunk scenes reference shared meshes/materials/textures. The generator may partition renderers, but it must not clone shared binary art or combine the entire map into a unique giant mesh.
- Each stable slice records Editor frame time/GC and, at rollout gates, Android sustained FPS, CPU/GPU frame time, loaded/peak memory, APK size, and installed size against `performance_regression_contract.md`.

### Naming And Source-Shape Guardrails

- Bare `*System` names are reserved for ECS `ISystem`/`SystemBase` implementations. Managed Unity boundaries use approved suffixes such as `SceneSystemHelper` and `CompositionSystemHelper`.
- `Element` suffix means `IBufferElementData`; `Component` means `IComponentData`; `Blob` means an immutable blob layout; `Config` means serialized authoring data; `View` means a non-updating serialized-reference boundary.
- Do not introduce `OperationMapManager`, `OperationMapController`, `OperationMapFacade`, `OperationMapService`, `OperationMapProvider`, `OperationMapLoaderSystem`, or `OperationMapRuntime` as replacement shells.
- Do not hide scene loading behind a static global singleton. The owner is explicit composition state created/disposed with the match shell.
- One public production type per file unless tightly coupled serializable config/blob records remain small and the established local file pattern supports grouping.
- New production files must stay inside the owning asmdef root and should stay below 500 lines. A source-growth baseline update is not approval for a broad class.
- Any unavoidable non-Burst `ISystem.OnUpdate` must be explicitly classified in `EcsBurstHotPathArchitectureTests`; the preferred outcome is no new non-Burst map system.

### Required Technical Validation Matrix

| Validation area | Required test/runner ownership |
|---|---|
| Config/catalog/schema | Add `OperationMapConfigValidationTests` in `Game.Tests.Editor`: duplicate/oversized ids, missing lazy refs, invalid bounds, duplicate anchors, stale hashes, unresolved scenarios, catalog shipping policy. |
| Addressables local layout | Add `OperationMapAddressablesLayoutValidationTests` in `Game.Tests.Editor`: exact groups/paths, stable addresses, one pack/role label, presentation partition labels, every approved entry local, no remote path requirement, and no heavyweight catalog preload. |
| Addressables build output | Validate real local bundles with Build Layout evidence: required addresses, group ownership, duplicate dependency bytes/GUIDs, bounded partition counts, catalog/hash presence, and per-map/all-map byte reports. Fast Mode is not evidence. |
| Addressables handle lifetime | Extend helper/streamer tests with fake operations and PlayMode probes for retained source/chunk handles, failed-handle release, drain ordering, sequential maps, duplicate requests, and zero leaked handles after teardown. |
| Entities scene packaging | Add Editor and Android probes proving an Addressable operation-map source scene resolves its expected subscene stream/content archive and reaches matching entity/readiness counts. |
| Deferred remote delivery | When Phase 11 starts, add availability/size/download/cancel/remove/catalog-update tests, offline and storage failures, content-state archive validation, and device/CDN update evidence. No remote test is required for the initial local milestone. |
| Assembly direction | Extend `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation` and run `RunBroadShellValidation` so configs cannot reference rendering/composition and runtime cannot reference composition/editor. |
| Naming/managed exceptions | Run `NonEcsSystemConversionArchitectureTests.RunFocusedValidation`; add every planned helper to the narrow reason-suffix path, not an allowlisted forbidden shell. |
| Burst/system classification | Run `EcsBurstHotPathArchitectureTests.RunFocusedValidation`, `RunTypeHandleValidation`, and `RunNoBurstISystemClassificationValidation`. |
| Source size | Run `ProductionSourceGrowthArchitectureTests.RunFocusedValidation`; do not accept a baseline increase until responsibility splitting is reviewed. |
| Static presentation | Extend existing manifest index, streamer, ownership, integrity, rollback, structural, no-op, stale cleanup, scene wiring, and Android resolver tests for two independently owned maps. |
| Scene lifecycle | Add `OperationMapSceneLoadingSceneSystemHelperTests` in `Game.Tests.Editor` using injected/fake scene and asset operations: load, duplicate, failure unwind, drain, unload, retry, sequential switch, and generation mismatch. |
| Runtime activation | Add `OperationMapLifecyclePlayModeTests` in `Game.Tests.PlayMode`: one root, readiness ordering, active ids, surface/blob publication, teardown, second load, and no stale ECS data. |
| Allocation/performance | Add a focused post-readiness allocation probe and extend match performance validation with operation-map state markers. Acceptance is 0 B/frame from map orchestration and no regression to streamer bounds. |
| Current-map parity | Existing current-map bake/launch, map-authored conversion, source hiding, camera, minimap, movement, aircraft, and Android probes remain mandatory before and after shell extraction. |

At the end of every stable implementation slice, run at minimum `git diff --check`, the affected focused tests, the four architecture runners named above where relevant, and a Unity compile. Scene, Addressables, serialized-reference, or visual behavior is not considered validated by text inspection alone.

## Performance And Build Contract

- Keep static presentation chunked and streamable. Do not combine a whole operation map into one mesh or one always-loaded scene.
- Reuse source meshes, materials, textures, and prefabs across maps; generated map scenes reference shared assets and must not clone binary assets.
- Load only chunks near the current camera and retain the existing bounded scene-operation work per frame.
- Drain presentation chunks before unloading a map. Restore/suppress canonical renderers transactionally through the existing ownership path.
- Cache map-id resolution and immutable metadata. Do not perform broad scene searches, `Resources.FindObjectsOfTypeAll`, hierarchy walks, or asset loads in gameplay hot paths.
- Precompute minimap projection/raster data. Marker updates remain dirty/version-gated and must not rebuild the full map each frame.
- Camera clamp and ground/surface queries use map metadata/blobs or existing ECS surface data, not repeated physics scans.
- Include every map explicitly approved by `OperationMapCatalogConfig` in the initial local Addressables build. Discovery by folder/name is prohibited: unapproved future maps and stale chunks must never enter Android builds automatically.
- Measure per-map loaded memory, peak transition memory, chunk load latency, sustained FPS, draw calls, triangles, GC, APK size, and installed size.
- Keep the accepted release/device gates in `performance_regression_contract.md`; a visually correct map migration cannot waive performance or package-size failures.

## Risk Register

| Priority | Risk | Required Mitigation |
|---|---|---|
| P0 | A second map bake deletes or rewrites the current 514 scenes. | Per-map output roots, manifest identity, integrity ledgers, and ownership tests precede extraction. |
| P0 | Android build resolver includes stale chunks or unapproved future maps. | Catalog-driven all-approved-map inclusion with exact manifest/hash/integrity validation and package-size evidence. |
| P0 | Bundling all approved maps creates one monolithic bundle or duplicates shared art. | Per-map Core/Presentation groups, bounded presentation partitions, explicit shared group, Build Layout analysis, and duplicate-byte CI gates. |
| P0 | The all-bundled twelve-map player exceeds practical Android store/install budgets. | Treat all-local as the initial integration milestone, measure one/two/all-map artifacts, and complete Phase 11 remote migration before production release if accepted budgets fail. |
| P0 | `Match.unity` is stripped before the new map is loadable. | Compatibility registration first; shell stripping is a later atomic cutover with rollback. |
| P0 | Scene duplication changes GlobalObjectIds and invalidates presentation/authoring references. | New manifest and map-owned placement configs are baked from the duplicated source; compare source-path and entity parity. |
| P1 | Map-authored buildings/vehicles duplicate, disappear, or use wrong ownership. | Move map-specific configs with the operation map and validate conversion plus source hiding. |
| P1 | `MatchSubScene`, lightmaps, probes, or map surface remain coupled to the shell. | Classify every dependency before movement and add scene-reference validation. |
| P1 | Gameplay starts before the selected map/subscene/presentation is ready. | One explicit readiness gate covering source scene, subscene, metadata, and required presentation preload. |
| P1 | Transition temporarily holds two large maps in memory. | Drain/unload old presentation before loading the next map where route design permits; record peak memory. |
| P2 | Generator metadata creates managed memory/GC pressure. | Immutable Burst-readable blobs, chunking/compression, and no per-frame conversion. |

## Progress Summary

Overall implementation progress: 7% (13/177 checklist items complete).

Progress is checklist-based. Each checkbox below counts as one item. Update this summary and the validation log in the same stable implementation commit.

| Phase | Status | Complete | Total | Progress | Notes |
|---|---|---:|---:|---:|---|
| 0. Reproducible baseline and rollback | In progress / shared | 11 | 12 | 92% | Required by both directions before scene edits. |
| 1. Operation-map and scenario data contracts | In progress / shared subset | 2 | 12 | 17% | Typed identity and metadata only; delivery-specific references remain later. |
| 2. Per-map static presentation ownership | Not started / compatibility subset | 0 | 14 | 0% | Preserve the current baked map safely; future-map generation remains undecided. |
| 2A. Local Addressables packaging foundation | Later / direction-specific | 0 | 20 | 0% | Do not implement before the map-delivery direction is selected. |
| 3. Current-map compatibility registration | Not started / shared | 0 | 10 | 0% | Registers the current map without choosing a loader. |
| 4. Non-destructive scene ownership split | Not started / shared priority | 0 | 14 | 0% | Primary objective: separate current map ownership from `Match.unity`. |
| 5. Runtime selection, loading, and teardown | Contract-only / shared subset | 0 | 14 | 0% | Readiness/failure/teardown contracts only; concrete loading is later. |
| 6. Metadata, camera, minimap, and movement binding | Not started / shared | 0 | 12 | 0% | Shared bounds, surface, grid, blockers, camera, minimap, runway, and helipad metadata. |
| 7. M01 operation-map slice | Later / shared contracts only | 0 | 10 | 0% | Map-neutral ids/anchors may proceed; physical rollout remains gated. |
| 8. Editor-time texture/mask generator | Later / editor direction only | 0 | 12 | 0% | Implement only if the editor-authored map direction is selected. |
| 9. Mission and Skirmish scenario rollout | Later / shared contracts only | 0 | 10 | 0% | Scenario-to-map identity may proceed; physical rollout remains later. |
| 10. Full validation and all-bundled rollout | Shared validation subset only | 0 | 21 | 0% | Run shared parity/performance gates; Addressables/build-layout gates are later. |
| 11. Deferred remote content migration | Later / direction-specific | 0 | 16 | 0% | Remote delivery remains independently gated. |

## Phase 0: Reproducible Baseline And Rollback

**Execution: `Shared now` for every checklist item.**

- [x] Capture the exact Unity scene setup for `Match.unity` and `MatchSubScene.unity`.
- [x] Reproduce the current manifest schema, canonical path/hash, content hash, chunk size, chunk count, and source count through an authoritative Unity probe.
- [x] Hash the manifest, integrity ledger, every generated scene, and every generated `.meta` file.
- [x] Record the authoritative bake, scene-wiring, structural-validation, and Android build-resolution commands and log paths.
- [x] Inventory every `MatchSceneView` serialized reference and classify it as shell-owned, map-owned, shared config, or temporary compatibility data. Evidence: `../AgentReports/2026-07-15_opmap-004_phase0_ownership_baseline.md`; accepted decisions: `../AgentReports/2026-07-16_operation_map_shell_root_ownership_decisions.md`.
- [x] Inventory root objects under `Match.unity` and classify map geometry, map authoring, camera, lighting, bootstrap, and runtime-only ownership. Evidence and accepted decisions are recorded in the same reports above.
- [x] Inventory `Match_MapBuildingPlacement_Config` and `Match_MapVehiclePlacement_Config`, including hierarchy-path and source-hiding assumptions. Evidence: `../AgentReports/2026-07-15_opmap-006_phase0_placement_ownership.md`; accepted decisions: `../AgentReports/2026-07-16_operation_map_placement_ownership_decisions.md`.
- [x] Inventory minimap projection, camera clamp, initial camera, full-map bounds, and objective-focus sources. Evidence: `../AgentReports/2026-07-15_opmap-007_phase0_camera_minimap_ownership.md`; accepted decisions: `../AgentReports/2026-07-15_operation_map_camera_minimap_ownership_decisions.md`.
- [x] Inventory ground-height, map-surface, grid, blockers, terrain, runway, and helipad metadata sources. Evidence: `../AgentReports/2026-07-15_opmap-008_phase0_navigation_metadata_ownership.md`; accepted decisions: `../AgentReports/2026-07-16_operation_map_navigation_metadata_ownership_decisions.md`.
- [x] Inventory static presentation streamer, canonical-renderer suppression, teardown, and Android build-scene ownership. See `../AgentReports/2026-07-15_operation_map_static_presentation_ownership.md`.
- [ ] Capture current Editor launch, Android launch, load time, loaded memory, APK/installed size, sustained FPS, draw, and GC evidence accepted for comparison.
- [x] Write an exact rollback recipe that restores scene setup, manifest/integrity files, generated scenes, scene references, configs, and build settings. See `operation_map_scene_split_rollback_recipe.md`.

Exit criteria:

- Current behavior and outputs are reproducible from commands, not inferred from repository inspection.
- The migration can detect unintended scene, manifest, generated-output, config, or build-content changes.
- No scene, config, baker, manifest, or generated output changes occur in this phase.

## Phase 1: Operation-Map And Scenario Data Contracts

**Execution: mixed.** Typed ids and loader-neutral metadata are `Shared now`; delivery-provider references and concrete scene-loading fields are `Later - direction decision`.

- [x] Approve canonical operation-map ids: `opmap.<mode-or-chapter>.<slug>`. See `operation_map_and_scenario_identity_contract.md`.
- [x] Approve canonical scenario ids: `scenario.<chapter>.<mission>.<slug>` and `scenario.skirmish.<slug>`. See the same identity contract.
- [ ] Add the planned `Game.Configs.OperationMapDefinition` `ScriptableObject` without storing hot runtime policy or heavyweight direct asset references in it.
- [ ] Add the planned `Game.Configs.ScenarioSetupConfig` `ScriptableObject`, or update this technical contract to an already accepted concrete config type before implementation.
- [ ] Add lazy source-scene, optional subscene/heavy metadata, static presentation manifest, map-surface, and map-content version references without introducing a `Game.Configs -> Game.Rendering` dependency.
- [ ] Add `OperationMapBoundsConfig`, `OperationMapCameraConfig`, `OperationMapMinimapConfig`, and planning/battle/minimap ids.
- [ ] Add typed ids for spawn, objective, deployment, build, civilian, hostile, base, resource, runway, helipad, lane, and debug anchors.
- [ ] Add map bounds, camera bounds, grid, surface/height, and blocker/path metadata references.
- [ ] Add source identity, schema version, content hash, and generated-metadata hash fields.
- [ ] Add `Game.Configs.OperationMapCatalogConfig`, resolved once by composition at launch or match transition with no hot-path asset search.
- [ ] Add validation for unique ids, missing assets, invalid bounds, duplicate anchors, stale hashes, and unresolved scenario-to-map references.
- [ ] Update architecture docs with the exact `Mission -> ScenarioSetup -> OperationMapDefinition -> scene/subscene/manifest` ownership chain.

Exit criteria:

- A scenario resolves one validated operation map through typed data.
- Runtime code does not need hardcoded scene paths or raw object names to choose a map.

## Phase 2: Per-Map Static Presentation Ownership

**Execution: compatibility subset is `Shared now`.** Preserve and separate the current map's accepted bake ownership. Generalized future-map generation is `Later - direction decision`.

- [ ] Introduce `Game.Editor.StaticMapPresentationBakeInput` carrying operation-map id, source scene, source map root, output root, manifest path, integrity path, and chunk size.
- [ ] Refactor `StaticMapPresentationBaker` so the current hardcoded constants remain only a compatibility entry point during migration.
- [ ] Advance the manifest schema to include operation-map id and canonical scene GUID/path identity with an explicit migration test.
- [ ] Use one generated output root per operation map.
- [ ] Namespace generated scene names by operation map so chunk coordinates cannot collide across maps.
- [ ] Use one integrity ledger per operation map.
- [ ] Restrict stale cleanup to paths owned by the same map id, manifest, and output-root contract.
- [ ] Keep transaction journaling and rollback scoped to the active map's mutable files and `.meta` files.
- [ ] Exclude every generated operation-map output root from canonical source dependency hashing.
- [ ] Preserve zero-write/zero-delete behavior on an identical second bake for each map.
- [ ] Add a two-map test proving a bake of map B cannot modify or delete map A outputs.
- [ ] Make scene wiring validate the selected map's manifest rather than one global manifest path.
- [ ] Make Android build resolution validate a catalog-selected manifest set and reject stale/unowned chunk scenes.
- [ ] Extend ownership, integrity, rollback, structural parity, build resolver, and no-op reuse tests for multiple maps.

Exit criteria:

- The current map still produces its accepted output through the compatibility entry point.
- Two operation maps can coexist without shared mutable generated ownership.
- An identical rebake writes no scenes and deletes no scenes.

## Phase 2A: Local Addressables Packaging Foundation

**Execution: `Later - delivery` for every checklist item.**

**Later / direction-specific:** do not implement this phase while runtime scene-based map R&D is unresolved.

- [ ] Approve the exact `Operation Maps - Catalog`, `Operation Maps - Shared`, and per-map Local Core/Presentation group contract from this document.
- [ ] Add `OperationMapCatalogEntryConfig`, `OperationMapContentPackConfig`, and `OperationMapDeliveryKind` with every initial approved entry set to `BuiltInLocal`.
- [ ] Create the small local catalog group without any heavyweight map scene, manifest, surface, texture, mesh, or chunk dependency.
- [ ] Create the explicit local shared group and admit assets only from measured cross-map GUID dependency evidence.
- [ ] Create one local Core group per approved operation map using stable map-pack identity and local Build/Load paths.
- [ ] Create one local Presentation group per approved operation map using stable map-pack identity and local Build/Load paths.
- [ ] Assign and validate every required stable address without using direct scene paths in gameplay/composition policy.
- [ ] Assign and validate exactly one map-pack label and one role label per operation-map entry.
- [ ] Partition presentation chunk scenes by deterministic region labels, targeting 16 to 32 chunks per bundle pending measured acceptance.
- [ ] Configure LZ4, CRC/cache policy, hash-derived bundle naming, and explicit content build settings for operation-map groups.
- [ ] Add `OperationMapAddressablesLayoutValidator` for groups, paths, addresses, labels, partitions, catalog inclusion, and one-map ownership.
- [ ] Add `OperationMapAddressablesBuildReport` output for per-map bytes, aggregate bytes, partition counts, required addresses, Entities artifacts, and duplicate dependencies.
- [ ] Add an explicit CI/editor Addressables content-build step that fails when local content, catalog, layout report, or operation-map validation is absent or stale.
- [ ] Load the selected source scene by stable Addressables address, retain its scene handle for the full map lifetime, and release it through deterministic teardown.
- [ ] Add `StaticMapPresentationAddressablesSceneApi` so local presentation chunks use stable addresses and retained handles through the existing bounded streamer.
- [ ] Add focused fake-operation and PlayMode tests for source/chunk handle ownership, duplicate requests, failed-handle release, drain ordering, and sequential maps.
- [ ] Prove that an Addressable source scene resolves its expected Entities subscene stream/content archive in Editor and Android without hand-addressing generated files.
- [ ] Run Addressables Analyze and Build Layout checks and fail on unapproved cross-map duplicate dependency bytes/GUIDs.
- [ ] Produce clean one-map, representative two-map, and all-approved-maps local artifacts with APK/AAB, installed-size, bundle, Entities, memory, and load-time deltas.
- [ ] Validate every approved map launches offline from real local bundles on Editor and Android with no remote catalog, download query, network call, or remote helper implementation.

Exit criteria:

- Every catalog-approved map is bundled locally but remains an independently addressable map pack.
- Runtime loads only the selected map and bounded presentation partitions, not all maps or manifests.
- Real-bundle Editor and Android evidence proves address coverage, Entities linkage, deterministic handle release, shared-dependency deduplication, and accepted package/load budgets.
- No deferred remote content ECS type/helper or download UI is implemented in this phase.

## Phase 3: Current-Map Compatibility Registration

**Execution: `Shared now` only when registration remains loader-neutral.**

- [ ] Confirm the logical id `opmap.skirmish.desert_base_01` or record the approved replacement.
- [ ] Register that id against the current `Match.unity`, `MatchSubScene.unity`, current manifest, and current map-specific configs without moving files.
- [ ] Create the compatibility `OperationMapDefinition` with exact current bounds, grid/surface references, camera ids, minimap id, and map anchors.
- [ ] Record current building/vehicle placement counts and source paths as map-owned compatibility data.
- [ ] Keep the current schema-v1 manifest readable during the map-specific schema transition.
- [ ] Launch the existing match through the compatibility operation-map id with no visual or gameplay behavior change.
- [ ] Run the current map bake twice and prove accepted parity followed by zero writes/deletes.
- [ ] Validate map-authored building, vehicle, aircraft, runway, helipad, blocker, and source-hiding behavior.
- [ ] Validate Android build-scene resolution includes only the compatibility map's accepted chunks.
- [ ] Update this tracker with exact paths, hashes, counts, commands, logs, and screenshots.

Exit criteria:

- The current map is addressable by operation-map id before extraction.
- Existing match behavior, baking, streaming, and Android inclusion remain stable.

## Phase 4: Non-Destructive Scene Ownership Split

**Execution: `Shared now`.** This phase separates the existing map from the Match shell without selecting how future maps are generated or delivered.

- [ ] Finalize the shell-owned versus map-owned dependency inventory from Phase 0.
- [ ] Create `Assets/Game/Scenes/OperationMaps/Skirmish/` and its Unity folder `.meta` files through Unity/AssetDatabase-safe tooling.
- [ ] Create `opmap_skirmish_desert_base_01.unity` as a staged duplicate with a new scene GUID; do not copy generated chunks or their `.meta` files.
- [ ] Add `Game.Composition.OperationMapSceneView` as a non-updating serialized-reference view only if direct scene references require it.
- [ ] Keep the original `Match.unity` fully functional until the extracted map passes all parity gates.
- [ ] Move/copy only classified map roots into the staged operation-map scene while shared binary assets remain referenced, not duplicated.
- [ ] Assign or create the map-owned subscene without breaking the original `MatchSubScene` compatibility path.
- [ ] Generate map-specific building and vehicle placement configs from the staged scene rather than reusing stale hierarchy assumptions blindly.
- [ ] Bind map surface, grid, blockers, lightmaps/probes, runways, helipads, bounds, and metadata from the staged scene.
- [ ] Bake a map-specific presentation manifest/chunk set from the staged operation-map scene.
- [ ] Validate source GlobalObjectIds, hierarchy paths, authored conversion counts, canonical renderer suppression, and entity parity.
- [ ] Remove map roots and map-specific references from `Match.unity` only in the atomic cutover commit after staged parity passes.
- [ ] Validate the stripped `Match.unity` remains a functional runtime shell and the extracted scene contains no shell bootstrap/HUD policy.
- [ ] Keep a revertable checkpoint that restores the original scene, configs, subscene reference, manifest binding, and build settings.

Exit criteria:

- The current large map exists as a separate operation-map scene with its own manifest and metadata.
- `Match.unity` is a runtime shell only after the new route is proven.
- No binary source art is duplicated and no unrelated generated map is changed.

## Phase 5: Runtime Selection, Loading, And Teardown

**Execution: `Shared contract now` only.** Define readiness, ownership, failure-unwind, and teardown contracts. Every concrete Addressables or runtime-scene loading/unloading implementation is `Later - delivery`.

- [ ] Add the planned operation-map root/queue/state/active/bounds/metadata/readiness ECS components and request/result buffers, carrying mission, scenario, and operation-map ids as bounded fixed strings.
- [ ] Resolve the operation-map catalog entry before beginning the match load transition.
- [ ] Load the selected canonical operation-map scene additively through `OperationMapSceneLoadingSceneSystemHelper`, called by the existing composition lifecycle with no new update-loop `MonoBehaviour`.
- [ ] Load and await the selected map's optional ECS subscene through the accepted Entities scene path.
- [ ] Resolve exactly one `OperationMapSceneView` through `OperationMapSceneReferenceSceneSystemHelper` only after scene load succeeds.
- [ ] Use `OperationMapRuntimeBootstrapSceneSystemHelper` to publish active ids, bounds, one small immutable `OperationMapBlob`, and readiness while reusing the existing `MapSurfaceBlob` owner.
- [ ] Bind the existing static presentation streamer to the selected manifest and world camera.
- [ ] Gate `BeginGameplay` on source scene, subscene, metadata, authored conversion, and required presentation preload readiness.
- [ ] On exit/switch, stop map gameplay, drain presentation chunks, and then unload map/subscene content in a deterministic order.
- [ ] Restore canonical renderer ownership correctly on failure, teardown, or compatibility fallback.
- [ ] Clear map-specific ECS entities, singleton/buffer data, cached anchors, and scene references before a later map loads.
- [ ] Reject missing/stale map ids with typed reason codes; do not silently load the current map in production.
- [ ] Add tests for valid load, missing id, stale manifest, interrupted load, teardown, retry, and sequential map switching.
- [ ] Validate no new update-loop `MonoBehaviour`, manager, controller, facade, service locator, or broad shell is introduced.

Exit criteria:

- `Match.unity` can start one selected operation map by config and unload it cleanly.
- Failure is explicit and leaves no partially loaded map or suppressed renderer state.

## Phase 6: Metadata, Camera, Minimap, And Movement Binding

**Execution: `Shared now` where the binding consumes loader-neutral active-map metadata.**

- [ ] Bind camera clamp bounds from active operation-map metadata.
- [ ] Bind planning, battle, and initial camera transforms from typed camera ids/anchors.
- [ ] Bind minimap projection and cached raster data from the active map.
- [ ] Bind objective focus/jump to typed operation-map anchor ids.
- [ ] Bind ARIA `Show Me` and camera intents to typed operation-map anchors.
- [ ] Bind friendly/hostile deployment and spawn anchors.
- [ ] Bind runway and helipad anchors for taxi, takeoff, return, and landing behavior.
- [ ] Bind blocker/path/build metadata to movement and placement validation.
- [ ] Bind ground-height/surface metadata for soldiers, vehicles, and aircraft clearance.
- [ ] Add validation for every anchor required by each active scenario.
- [ ] Add editor/debug overlays for bounds, cameras, anchors, blockers, lanes, surface samples, and minimap extents.
- [ ] Add mobile-safe caching and allocation tests for minimap, camera, surface, and anchor lookups.

Exit criteria:

- Camera, minimap, objectives, spawns, movement, aircraft, and placement use active map data.
- Gameplay systems do not depend on raw scene object names for map-critical behavior.

## Phase 7: M01 Operation-Map Slice

**Execution: `Later - direction decision` for physical map work.** Design ids and loader-neutral anchor requirements may remain documented, but no M01 map asset implementation begins now.

Implementation blocker: M01 planning and scaffolding may be studied, but M01 implementation and player-facing integration must not begin until `../M01_FirstContact_Production_Contract.md` releases its FirstLaunch Phase 10R / Gate 9R hold.

- [ ] Reconfirm the M01 implementation hold before changing player-facing routes or assets.
- [ ] Use the dedicated `opmap.ch01.district_edge_01` unless accepted evidence approves bounded reuse of the current map.
- [ ] Create the M01 operation-map source scene/subscene only after the hold is released.
- [ ] Create M01 `OperationMapDefinition` metadata with required bounds and surface/path data.
- [ ] Add `camera.ch01.m01.planning`, battle camera data, and `minimap.ch01.m01.projection`.
- [ ] Add M01 objective, spawn, patrol lane, tutorial move target, and camera-focus anchors.
- [ ] Add M01 `ScenarioSetup` with rifle squad, enemy patrol, restrictions, objectives, rewards, and ARIA hooks.
- [ ] Validate direct select/move/attack/objective behavior against map metadata.
- [ ] Validate camera bounds, minimap, objective jump, ARIA focus, and result/replay rebuild.
- [ ] Record gate release evidence and M01 visual/playable acceptance before player-facing integration.

Exit criteria:

- M01 has a dedicated, readable map contract after the hold is released.
- M01 does not require unrelated base, fuel, building, or current-big-map systems.

## Phase 8: Editor-Time Texture/Mask Generator

**Execution: `Later - editor direction only` for every checklist item.** Do not implement this phase unless the editor-authored map direction is selected after R&D.

- [ ] Define the reviewed map-pack folder/manifest contract for base visual, blocker mask, height mask, tree/rock masks, and generation seed/version.
- [ ] Add the editor-only `OperationMapTextureMaskGenerator` entry point and its `OperationMapGenerationInput`/`OperationMapGenerationResult` value types under existing tooling conventions.
- [ ] Generate/update operation-map metadata without writing static presentation outputs directly.
- [ ] Generate blocker/grid metadata deterministically.
- [ ] Generate compressed/chunked height and surface samples deterministically.
- [ ] Generate tree, rock, and decoration placement candidates deterministically.
- [ ] Generate reserve zones, lanes, and connectivity metadata.
- [ ] Generate debug overlays for blockers, height, anchors, reserve zones, lanes, and camera bounds.
- [ ] Generate canonical source scene/subscene content through `OperationMapSourceSceneBuilder` in deterministic chunks while referencing shared assets.
- [ ] Preserve generated source/metadata file and `.meta` stability on an identical generation.
- [ ] Validate connected playable zones, blocked outer belts, clear build reserves, and map bounds.
- [ ] Log source hashes, seed/version, written/reused/stale counts, metadata size, scene count, and validation results.

Exit criteria:

- Reviewed map packs produce deterministic canonical operation-map source and metadata.
- The static presentation baker can consume the result without shared cleanup ownership or no-op churn.

## Phase 9: Mission And Skirmish Scenario Rollout

**Execution: `Later - direction decision` for physical map rollout.** Loader-neutral scenario/map ids may remain documented only.

- [ ] Add a Skirmish/sandbox `ScenarioSetup` for `opmap.skirmish.desert_base_01`.
- [ ] Gate build, scan, support, aircraft, fuel logistics, resource exchange, and fabrication per scenario.
- [ ] Define objectives and star goals in scenario data, not scene branches.
- [ ] Define starting units, buildings, and resources in scenario data.
- [ ] Define enemy setup, AI profile ids, and encounter references in scenario data.
- [ ] Define Campaign rewards, unlocks, and consequences in scenario data.
- [ ] Publish UI read models for active mission/scenario/map identity.
- [ ] Validate objective/reward/feature references against operation-map anchors and catalogs.
- [ ] Add one preserved-current-map Skirmish probe and sequential reload probe.
- [ ] Add Campaign/M01 probes only after the M01 hold is released.

Exit criteria:

- Multiple scenarios can reuse one operation map without modifying its scene.
- Scenario data, not scene contents alone, controls game rules and starting state.

## Phase 10: Full Validation And All-Bundled Rollout

**Execution: mixed.** Validation triggered by current shared work is `Shared now`; Addressables build-layout, all-map packaging, and delivery validation are `Later - delivery`.

- [ ] Run `git diff --check` and scoped asset/meta integrity checks.
- [ ] Run `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation`/`RunBroadShellValidation`, `NonEcsSystemConversionArchitectureTests.RunFocusedValidation`, `EcsBurstHotPathArchitectureTests` focused/type-handle/non-Burst classification runners, and `ProductionSourceGrowthArchitectureTests.RunFocusedValidation`.
- [ ] Run compile validation with zero new errors.
- [ ] Run `OperationMapConfigValidationTests`, operation-map lifecycle/helper tests, and operation-map scenario/catalog/config validation.
- [ ] Run current and multi-map static presentation ownership, integrity, rollback, structural, no-op, and stale-cleanup tests.
- [ ] Run scene-reference validation for the shell, current map, subscenes, manifests, configs, and M01 only when allowed.
- [ ] Run original current-map launch before extraction and compatibility launch after each migration phase.
- [ ] Run extracted-current-map load, gameplay, teardown, retry, and sequential reload validation.
- [ ] Run camera bounds, initial camera, minimap, objective, and ARIA anchor validation.
- [ ] Run movement, surface grounding, aircraft clearance, runway/helipad, blockers, and building placement validation.
- [ ] Run map-authored building/vehicle/aircraft conversion and source-hiding regression validation.
- [ ] Run editor generator deterministic/no-op and connectivity validation.
- [ ] Run Editor performance comparison for load time, frame time, draw, triangles, memory, and GC.
- [ ] Produce clean one-map, representative two-map, and all-approved-maps release artifacts; record the exact incremental APK/AAB, installed-size, Entities stream/archive, static-presentation, combined-mesh, shared-dependency, and aggregate cost.
- [ ] Replace the provisional `80-110 MB` compressed per-map planning range with an accepted measured budget before approving broader map production.
- [ ] Validate every approved operation map from real local Addressables bundles, including stable identity, shared dependency deduplication, offline launch, content-version mismatch, load failure unwind, teardown, and sequential switching.
- [ ] Run Android build-scene inclusion, APK/installed size, startup, memory, sustained FPS, and thermal validation.
- [ ] Verify every catalog-approved local operation map and only those maps are packaged; no unapproved map, stale generated scene, foreign-owned chunk, or remote dependency is included.
- [ ] Capture accepted screenshots for top-down, oblique, low-ground, minimap, bounds, and map transition states.
- [ ] Update `README.md`, `Design/README.md`, this percentage table, and exact command/log evidence.
- [ ] For every stable slice, use the serial direct-main workflow, provide commit-bound validation evidence, self-review the complete diff, and keep scene extraction and shell cutover in independently revertable commits.

Exit criteria:

- The current large map is preserved and selectable as a separate operation map.
- `Match.unity` is a reusable shell with no hidden dependency on the current map.
- Per-map generation and static presentation baking are deterministic and no-op-safe.
- Build inclusion is explicit and Android package/performance evidence remains acceptable.
- M01 and future missions have a validated path without putting every level in one scene.

## Phase 11: Deferred Remote Content Migration

**Execution: `Later - delivery` for every checklist item.**

**Later / direction-specific:** this phase remains out of scope regardless of shared-foundation progress.

This phase starts only after the all-bundled implementation is stable and the product owner explicitly requests remote delivery. It does not block the initial all-local milestone, but it is the accepted path when package/install budgets require downloadable maps.

- [ ] Reconfirm the remote-delivery product gate, select which maps remain local, and record measured package/storage evidence for the migration.
- [ ] Approve HTTPS/CDN ownership, authentication/public-read policy, immutable release-root naming, retention, rollback, and regional availability requirements.
- [ ] Configure staging and production remote BuildPath/LoadPath profiles plus remote catalog/hash paths without changing stable map addresses or labels.
- [ ] Archive platform-specific `addressables_content_state.bin`, catalog/hash, checksums, Build Layout, and operation-map build report for every published full build.
- [ ] Move selected map Core/Presentation groups to remote schemas while preserving operation-map ids, pack ids, addresses, role labels, and partition labels.
- [ ] Add the reserved `OperationMapContent*` ECS components, buffers, and enums without managed handles, URLs, or per-frame string policy.
- [ ] Add `OperationMapContentDeliverySceneSystemHelper` as the single managed owner for catalog-check, size, download, cancellation, and removal handles through the existing composition lifecycle.
- [ ] Restrict catalog checks/updates to Menu or another shell state with no loaded operation-map scene, asset, manifest, or chunk handles.
- [ ] Add download-size and disk-headroom preflight with typed insufficient-storage results before any transfer starts.
- [ ] Add one-at-a-time dependency download with byte-based dirty/versioned progress publication capped at 4 Hz.
- [ ] Add bounded timeout/retry for typed transient failures and cooperative cancellation that never reports partial cache bytes as removed.
- [ ] Verify pack version/hash, required stable addresses, compatible schema/app version, and zero remaining download bytes before availability becomes ready.
- [ ] Add inactive-pack removal through dependency-cache clearing and reject removal while any related map/asset/scene handle is active.
- [ ] Validate offline behavior: local recovery/tutorial maps remain playable, cached remote maps remain usable, and uncached maps expose typed unavailable status.
- [ ] Add full-content and content-update CI paths from exact archived state, atomic upload ordering, catalog rollback, checksum verification, and stale-release cleanup policy.
- [ ] Validate install, first download, interruption, retry, update, rollback, removal, redownload, offline launch, memory, load time, FPS, and thermal behavior on Android/CDN; evaluate Play Asset Delivery only as a separate adapter proof.

Exit criteria:

- Selected maps move from local to remote delivery without changing mission/scenario data, gameplay ECS contracts, stable addresses, or map-loading call sites.
- Catalog/content updates cannot invalidate an active match and every handle/cache transition is deterministic and typed.
- At least one local recovery map remains playable with no network.
- CDN/device evidence meets package, storage, reliability, performance, and rollback budgets before remote delivery becomes production policy.

## Validation Log

| Date | Slice | Commands / Evidence | Result | Notes |
|---|---|---|---|---|
| 2026-07-14 | Initial tracker creation | `git diff --check` | Passed | Documentation only; no scene, code, prefab, bake, or asset migration. |
| 2026-07-14 | Architecture and bake audit | Repository inspection of current baker, manifest, integrity ledger, streamer, Android resolver, `MatchSceneView`, map placement configs, M01 hold, and map workflow; `git diff --check` | Passed | Corrected migration order, current 514-chunk/16,542-source baseline snapshot, per-map ownership requirement, build-size risk, scene GUID rules, and M01 gating. No Unity validation claimed. |
| 2026-07-14 | Technical architecture contract | Inspected active asmdefs, `SceneLifecycle*`, `MapSurfaceComponent`/`MapSurfaceBlob`, `MapSurfaceRuntimeBootstrapSceneSystemHelper`, `StaticMapPresentationManifest`/index/streamer/baker, `TacticalMapDefinition`, and architecture runner entry points; verified 138 checklist items; `git diff --check -- Design/Architecture/operation_map_scene_split_and_generator_tracker.md` | Passed | Added exact planned type kinds, namespaces, files, assembly direction, lifecycle/data ownership, managed-versus-ISystem boundary, blob reuse, no-GC/performance budgets, naming rules, and technical validation matrix. Documentation only; no Unity validation claimed. |
| 2026-07-14 | Portfolio generation and package-size decision | Measured current map source scene, subscene/probes, map surface, placement configs, 514-chunk static-presentation output, generated combined meshes, current Android APK, Entities stream/content archive, and packaged Unity levels; `git diff --check -- Design/Architecture/operation_map_scene_split_and_generator_tracker.md` | Passed | Recorded editor-generated/reviewed/baked maps as the production direction, runtime variation through scenarios, runtime procedural generation as a future constrained Skirmish-only concern, and provisional twelve-map source/package estimates. Its earlier starter/download split was superseded by the all-local acceptance below; the size evidence remains valid. Documentation only; estimates are not accepted release measurements. |
| 2026-07-14 | Product direction acceptance | Product-owner decision in this task; tracker accounting audit (`177` checklist items; Phase 2A `20`; Phase 10 `21`; Phase 11 `16`) | Accepted | Hybrid editor generation, reviewed canonical maps, downstream bakes, scenario-driven runtime variation, and every approved map bundled as an independent local Addressables pack are documented initial scope. HTTPS/CDN delivery is a deferred migration that preserves stable identity. Implementation remains deferred until an explicit start request; Phase 0 has not started. |
| 2026-07-14 | Addressables packaging and staged delivery specification | Inspected installed Addressables `3.1.0`, current local-only group/settings baseline, official Unity Addressables scene/bundle/handle/content-update guidance, and Android Play Asset Delivery guidance; audited all phase totals (`177`); `git diff --check -- Design/Architecture/operation_map_scene_split_and_generator_tracker.md` | Passed | Added normative local group/address/label/partition schemas, handle and Entities-content gates, build/CI artifacts, typed failures, exact planned type ownership, 20-step all-local foundation, and 16-step deferred CDN migration. Documentation only; no Unity process, project setting, scene, code, Addressables asset, or build output was changed. |
| 2026-07-14 | Pull request workflow adoption | `agent_pull_request_review_merge_workflow.md`, `.github/pull_request_template.md`, README activation/grandfathering rules, scoped authority search, checklist recount (`177`), and `git diff --check origin/main...HEAD` | Pending PR review | Declares all operation-map implementation as new PR work, one stable slice per task branch/worktree, with implementation-agent and independent coordinator ownership. This documentation PR does not self-accept or merge the change. |
| 2026-07-15 | `opmap-005` static-map presentation refresh evidence | PR `#5`; no-op Android-target bakes; focused EditMode `35 / 35`; APK `442,924,488` bytes; retained report `2026-07-15_opmap-005_static_map_presentation_refresh.md`; merge `98a28b687d858118548ed6ae38037b61f4377f08` | Passed; evidence-only | Build #106's exact revision and workspace state were not retained, so its root cause remains unknown. The accepted evidence proves only that the stale-manifest failure did not reproduce from the recorded clean baseline and that no tracked source/product asset changed. No Phase 0 checkbox closed. |
| 2026-07-15 | `opmap-004` shell/map ownership evidence | PR `#6`; focused EditMode `26 / 26`; two byte-identical probe runs; committed report SHA-256 `e1080bd9...3c49b`; architecture gates `9 / 9`, `31 / 31`, `1 / 1`; merge `98cfe8cedb3c7d18a14819759bb0d5e51c202264` | Passed; decisions remain | Deterministic read-only evidence records `28` `MatchSceneView` fields, `16` Match roots, and `3` MatchSubScene roots. The report intentionally remains `NeedsDecision`; ownership checklist rows stay open until every Mixed/Unresolved row has an accepted decision. |
| 2026-07-15 | Build `#110` cross-platform canonical hash repair | PR `#11`; focused EditMode `64 / 64`; canonical hash changed from `0a587783351110d16353575d15d1b5cd` to `db252d7b61b87458dafbd30acb8a5559`; all `514` chunk scene/meta pairs and content hash remained byte-identical; merge `f850f3a5bdc6b3f4ffa8ad7ab453c07611fb5e8a` | Passed locally; Windows rerun pending | Canonical text dependency hashing now normalizes line endings, binary bytes remain exact, and Jenkins pins LF before sparse materialization. Build #110 failed because its Windows checkout could materialize different line endings than the macOS bake. The Android/Jenkins acceptance row remains open until a post-merge Windows build succeeds. |
| 2026-07-15 | `opmap-006` placement ownership evidence | PR `#9`; focused EditMode `54 / 54`; two byte-identical probe runs; committed report SHA-256 `115270bd...f759`; architecture gates `9 / 9`, `31 / 31`, `1 / 1`; merge `2873e4dd7b2f1f5a5727c1de81bd9c86f97dc60d` | Passed; decisions remain | Deterministic evidence covers all `451` building and `29` vehicle placement entries. Duplicate hierarchy source paths are grouped and remain explicitly Mixed/Unresolved rather than claiming false one-to-one source-hiding ownership. The placement checklist row stays open pending accepted decisions. |
| 2026-07-15 | Shared navigation metadata ownership evidence | Direct-main shared-foundation slice; schema `2`; focused EditMode `36 / 36`; two byte-identical probe runs; committed report SHA-256 `7eedc224...300f1`; architecture gates `9 / 9`, `31 / 31`, `1 / 1` | Passed; four decisions remain | Deterministic read-only evidence covers `15` authorities and `15` exact compiled consumer type/member identities, including fixed-wing runway initialization and grid movement. Authority results are `7 MapOwned`, `4 SharedConfig`, `3 Mixed`, and `1 Unresolved`; only Mixed/Unresolved rows use `DecisionRequired`, so the Phase 0 navigation checkbox remains open. A broader diagnostic architecture run retained seven unrelated pre-existing failures and is not acceptance evidence for this slice. |
| 2026-07-15 | Shared-only scope clarification and Phase 0 evidence accounting | `2026-07-14_opmap-002_phase0_baseline_probe.md`; `2026-07-15_opmap-005_static_map_presentation_refresh.md`; `git diff --check` | Passed | Marked the four accepted reproducibility/evidence items complete: exact scene setup, authoritative manifest/count reproduction, full manifest/integrity/generated-file hashing, and recorded bake/wiring/structural/Android resolver commands and logs. Added normative execution labels so Addressables, concrete loading, future physical maps, editor/runtime generators, packaging, and remote delivery cannot start while map-direction R&D remains open. No scene, config, manifest, generated output, or runtime source changed. |
| 2026-07-15 | Shared scene-split rollback recipe | `operation_map_scene_split_rollback_recipe.md`; ownership-path audit; `git diff --check` | Passed | Added a loader/generator-neutral rollback contract based on an immutable pre-cutover SHA, an exact cutover path ledger, an atomic revert range, byte/GUID restoration, authoritative probe parity, no-op rebakes, focused tests, Editor gameplay parity, and risk-triggered Android validation. No project asset or runtime source changed. |
| 2026-07-15 | Shared static-presentation ownership decision | `../AgentReports/2026-07-15_operation_map_static_presentation_ownership.md`; exact type/member audit; source SHA-256 inventory; `git diff --check` | Passed | Classified map products and canonical renderers as `MapOwned`; reusable indexing, streaming, suppression transaction, and teardown as `ShellOwned`/`SharedConfig`; and current direct Match wiring, hardcoded baker binding, and Android resolver as `TemporaryCompatibility`. No unresolved ownership remains in this row and no loader/generator direction was selected. |
| 2026-07-15 | Shared camera/minimap ownership decisions | `../AgentReports/2026-07-15_opmap-007_phase0_camera_minimap_ownership.json`; `../AgentReports/2026-07-15_operation_map_camera_minimap_ownership_decisions.md`; `git diff --check` | Passed | Resolved all five `Mixed` and two `Unresolved` evidence rows: scenarios own semantic intent, maps own bounds/anchors, shell systems own camera/minimap/ARIA policy, and the config camera override is temporary compatibility. Full-map projection is required to clamp inside canonical map bounds. This closes ownership only; behavior work remains tracked in shared phases. |
| 2026-07-16 | Shared navigation metadata ownership decisions | `../AgentReports/2026-07-15_opmap-008_phase0_navigation_metadata_ownership.json`; `../AgentReports/2026-07-16_operation_map_navigation_metadata_ownership_decisions.md`; `git diff --check` | Passed | Resolved three `Mixed` and one `Unresolved` row. Maps own immutable grid/surface/authored blocker and authored runway metadata; runtime ECS owns mutable occupancy/blocker state; shared building definitions own prefab-local runway metadata; runtime systems publish both runway sources through one typed contract. Behavior work remains tracked in shared phases. |
| 2026-07-16 | Shared Match shell/root ownership decisions | `../AgentReports/2026-07-15_opmap-004_phase0_ownership_baseline.json`; `../AgentReports/2026-07-16_operation_map_shell_root_ownership_decisions.md`; root-reference audit; `git diff --check` | Passed | Resolved the final four rows: day/night policy is `SharedConfig`; bare unreferenced Start/End roots are map-scoped `TemporaryCompatibility`; initial-unit authoring is a scenario/map `Mixed` contract. All 28 MatchSceneView fields, 16 Match roots, and 3 MatchSubScene roots now have accepted ownership. No scene move or shell stripping is authorized by this inventory decision. |
| 2026-07-16 | Shared placement ownership decisions | `../AgentReports/2026-07-15_opmap-006_phase0_placement_ownership.json`; `../AgentReports/2026-07-16_operation_map_placement_ownership_decisions.md`; `git diff --check` | Passed | Classified both placement configs and authoring roots as `MapOwned`, with runtime spawn/hiding/teardown retained as shell policy. The 54 duplicate source-path groups are accepted `TemporaryCompatibility`; cutover must use complete placement and old/new object identity and fail on zero/multiple/reused candidates. All 451 building and 29 vehicle entries now have accepted disposition. |
| 2026-07-16 | Shared operation-map/scenario identity contract | `operation_map_and_scenario_identity_contract.md`; design-id cross-check; checklist accounting; `git diff --check` | Passed | Approved bounded lowercase ASCII operation-map and scenario grammars, immutability, ordinal comparison, namespace separation, scenario-to-map cardinality, and explicit separation from paths, GUIDs, Addressables, bundles, display text, versions, and generator seeds. Documentation only. |

## Open Decisions

| Topic | Current Recommendation | Decision Gate |
|---|---|---|
| Current large map id | `opmap.skirmish.desert_base_01` | Approve in Phase 3 before catalog data is committed. |
| Extracted source path | `Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity` | Confirm after Phase 0 dependency classification. |
| Current map subscene | Make it map-owned if its authored ECS content is map-specific; keep only truly shared runtime ECS setup in shell composition. | Decide from Phase 0 inventory, not filename alone. |
| Manifest schema migration | Keep schema-v1 compatibility while introducing map id/source GUID and map-scoped outputs in a new schema. | Approve with multi-map ownership and no-op tests in Phase 2. |
| Runtime loading | Direction unresolved. Keep `Match.unity` shell separation, one-active-map, readiness, failure-unwind, and teardown contracts loader-independent while runtime scene-based R&D is evaluated. | Select and document the concrete loader before implementing Phase 2A or Phase 5 loading/unloading. |
| Remote migration trigger | Bundle every catalog-approved map locally for the initial milestone. Move selected stable per-map groups to HTTPS/CDN only after all-local stability and measured package/install evidence justify it. | Explicit Phase 11 product gate; no remote implementation during Phase 2A/10. |
| M01 source | Dedicated `opmap.ch01.district_edge_01`. | Reconfirm after FirstLaunch releases the M01 hold. |
| Generator | Direction unresolved. The editor texture/mask generator remains a later option; runtime scene-based map R&D is separate and must prove architecture, determinism, memory, and performance viability. | Update this tracker only after R&D selects a direction; do not implement Phase 8 meanwhile. |
