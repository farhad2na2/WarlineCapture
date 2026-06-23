# Five SystemBase To Split ISystem Conversion Tracker

## Purpose

Track the remaining high-risk managed `SystemBase` boundaries that were intentionally left out of the completed post-roadmap prefab conversion track:

- `BuildingSpawnSystem`
- `BuildingProductionTransportBridgeSystem`
- `CitizenVisibleUnitSystem`
- `MapVehiclePlacementSpawnSystem`
- `CustomGameStartupSystem`

The goal is to retire these broad managed runtime gameplay owners by decomposing each one into focused unmanaged `ISystem` processors plus explicit passive managed boundaries. A target is not complete if it is merely converted into one large `ISystem` with the same responsibilities. Do not force `GameObject`, prefab, camera, UI, serialized config, or `UnityEngine.Object` ownership into unmanaged systems.

## Progress Snapshot

- Checklist progress: `29 / 126 complete (23.0%)`.
- In progress: `1`.
- Remaining open: `96`.
- Target `SystemBase` classes: `5`.
- Converted target classes to `ISystem` legacy metric: `0 / 5`.
- Retired broad `SystemBase` gameplay owners: `0 / 5`.
- Extracted focused ECS `ISystem` processors: `0 / 22 planned`.
- Split decomposition plans documented: `5 / 5`.
- Split into passive managed boundaries: `0 / 5`.
- Validation status: `git diff --check` passed; main-project `BuildingProductionRequestValidation` passed (`tests=21`); main-project `BuildingProductionCameraFocusValidation` passed (`tests=10`); main-project `BuildingUiQueryValidation` passed (`tests=5`); Integration P7-0318 moved `MapVehiclePlacementSpawnSystem` progress/random/clearance state into ECS `MapVehiclePlacementProgressState` and passed `UnitMovementBlockerValidation` (`/private/tmp/warline-phase7-integration-map-vehicle-placement-progress-state.log`), compile, inventory regeneration, `git diff --check`, and Phase 7 architecture guard.
- Current target system: `MapVehiclePlacementSpawnSystem` progress-state split complete; continue placement instantiation, blocker, result, and composition extraction.
- Counting rule: only checklist lines beginning with `- [ ]`, `- [x]`, or `- [~]` count toward checklist progress.

## Current Disposition

| Target | Current inheritance | Main blocker | Intended end state |
| --- | --- | --- | --- |
| `BuildingSpawnSystem` | `SystemBase` | Fallback `Instance.transform` spawn placement reads plus managed produced-unit list/slot fallbacks when no runtime boundary is available. | Retire as a broad spawn owner. Split into `BuildingProductionSpawnRequestSystem`, `BuildingProductionSlotReservationSystem`, `BuildingProductionPlacementSystem`, `BuildingHelipadSpawnSystem`, `BuildingUnitInstantiationSystem`, and `BuildingProducedUnitStateSystem`; preview/transform projection stays passive. |
| `BuildingProductionTransportBridgeSystem` | `SystemBase` | Uses produced-unit prefab/object data for footprint and focus decisions. | Retire as a direct bridge. Split into `BuildingProductionTransportRequestSystem`, `BuildingProductionTransportMovementSystem`, `BuildingProductionTransportFocusRequestSystem`, and `BuildingRunwayTransportSystem`; camera/UI application remains passive. |
| `CitizenVisibleUnitSystem` | `SystemBase` | Managed visible-citizen dictionaries and immediate entity tracking after instantiate. | Retire as same-frame dictionary owner. Split into `CitizenVisibleUnitSpawnRequestSystem`, `CitizenVisibleUnitInstantiateSystem`, `CitizenVisibleUnitMovementStateSystem`, and `CitizenVisibleUnitLifetimeSystem`; presentation state remains passive. |
| `MapVehiclePlacementSpawnSystem` | Plain direct helper | Instantiation, blocker reservation/cleanup, and completion results still run through the broad direct helper; config still has authored prefab fallback at the managed edge. | Retire as managed update wrapper. Progress/random/clearance state now lives in ECS `MapVehiclePlacementProgressState`; continue split into `MapVehiclePlacementProgressSystem`, `MapVehiclePlacementInstantiateSystem`, `MapVehiclePlacementBlockerSystem`, and `MapVehiclePlacementResultSystem`; config projection remains passive. |
| `CustomGameStartupSystem` | `SystemBase` | Serialized startup configs and prefab/atlas/impostor projection. | Retire as serialized config/runtime startup owner. Split into `CustomGameFactionStartupSystem`, `CustomGameBuildingStartupSystem`, `CustomGameUnitStartupSystem`, and `CustomGameStartupResultSystem`; serialized config, atlas, sprite, and impostor projection remain passive. |

## Updated Estimate

Scope correction:
This tracker now measures split-system decomposition, not five inheritance flips. The checklist denominator is expected to increase and the percentage may drop even though prior implementation work remains valid.

Estimated remaining focused engineering time:

| Target | Remaining work shape | Estimate |
| --- | --- | --- |
| `BuildingSpawnSystem` | Finish transform/fallback removal, then extract six focused production spawn processors and retire the broad owner. | 4-7 hours |
| `BuildingProductionTransportBridgeSystem` | Move produced-unit transport, runway movement, and focus requests to ECS data while keeping camera/UI passive. | 2-4 hours |
| `CitizenVisibleUnitSystem` | Replace same-frame dictionaries with request/state/lifetime systems and passive presentation state. | 4-7 hours |
| `MapVehiclePlacementSpawnSystem` | Move config progress, instantiation, blocker ownership, and completion results into ECS. | 4-6 hours |
| `CustomGameStartupSystem` | Split serialized config projection from faction, building, unit, and result startup processors. | 5-9 hours |
| Cross-cutting validation | Architecture guards, composition rewiring, and focused Unity/EditMode/PlayMode validation. | 3-6 hours |

Total remaining estimate: `22-39 focused engineering hours`.

Practical calendar estimate with Unity locks, shadow validation, and rework from failed validation: `2-4 working days` of automation time.

## Ground Rules

- Preserve gameplay behavior: unit production output, citizen visibility spawning, map vehicle placement, and custom-game initial setup must remain equivalent.
- Convert in small slices with focused validation after each slice.
- Add ECS data/request/result shape before changing runtime ownership.
- Keep managed prefab, serialized config, UI, sprite, atlas, camera, and `UnityEngine.Object` access outside unmanaged systems.
- Do not convert any target by preserving its broad responsibilities inside one large `ISystem`.
- Each extracted `ISystem` must own one clear runtime responsibility: request intake, validation/reservation, placement, instantiation, state publication, movement, lifetime, or result emission.
- Passive managed boundaries may project serialized or presentation data into ECS, but they must not make runtime gameplay spawn, placement, movement, or startup decisions after the split is complete.
- Converted ECS processors must not depend on `RuntimeBuildingEntity`, `GameObject`, `Transform`, `UnityEngine.Object`, `Dictionary<int, RuntimeBuildingEntity>`, `ProducedUnits`, `ProducedUnitSlots`, or `Instance.transform`.
- Preserve Unity `.meta` files when renaming or moving scripts.
- Do not add runtime `Object.Find*`, `GameObject.Find`, `Camera.main`, hierarchy string lookup, mutable static registries, or broad manager/controller/facade shells.
- Do not use gameplay naming escape hatches to hide managed ownership.

## Phase 0: Baseline Audit

Purpose:
Confirm every current managed dependency and write tests before moving behavior.

- [x] Record all direct call sites for the five target systems.
- [x] Record all serialized prefab/config inputs used by the five target systems.
- [x] Record all ECS component/buffer data already available for unit source keys, prefab entities, production slots, citizen prefabs, vehicle placement, and custom-game startup.
- [x] Record every managed fallback path that must remain outside unmanaged code.
- [x] Add a current-state grep snapshot for `SystemBase`, `GameObject`, `UnityEngine.Object`, `List<GameObject>`, and `Dictionary<..., GameObject>` in the five target files.
- [x] Run and record baseline `git diff --check`.
- [x] Run and record baseline `NonEcsSystemConversionArchitectureValidation`.
- [x] Run and record baseline `BuildingProductionRequestValidation`.
- [x] Run and record baseline `CitizenVisibleUnitFocusedValidation`.
- [x] Run and record baseline `UnitMovementBlockerValidation`.
- [x] Run and record baseline `CustomGameStartupFocusedValidation`.

Baseline audit notes:

Direct call sites:

- `BuildingSpawnSystem`: held by `BuildingGameplayCompositionSourceSystem`, included in `BuildingGameplayCompositionResultSystem`, invoked through `BuildingProductionRuntimeTickSystem`, `BuildingProductionTickCompositionSystemHelper`, `BuildingProductionCompositionSystemHelper`, and `BuildingRuntimeContextSystem.CreateBuildingSpawnContext`; focused tests call `ResolveProducedUnitFaction`.
- `BuildingProductionTransportBridgeSystem`: held by `BuildingGameplayCompositionSourceSystem`, passed through `BuildingProductionContextSystem` and `BuildingProductionTransportSystem`; focused production tests call `FocusNewestPlayerProducedUnit`.
- `CitizenVisibleUnitSystem`: constructed by `CitizenPopulationCompositionSystem` and directly constructed by `CitizenVisibleUnitSystemTests`.
- `MapVehiclePlacementSpawnSystem`: held by `BuildingGameplayCompositionSourceSystem`, invoked by `BuildingGameplayCompositionSystem` map placement update callbacks; blocker cleanup helpers are directly covered by `UnitMovementBlockerValidationTests`.
- `CustomGameStartupSystem`: resolved by `MatchBootstrapSystem` through `World.GetOrCreateSystemManaged<CustomGameStartupSystem>()`; focused tests resolve it through `GetOrCreateSystemManaged`.

Managed prefab/config inputs:

- `BuildingSpawnSystem`: `GetProductionPrefabDelegate`, `GameObject spawnUnitPrefab`, `ProducedUnitPrefabs` fallback map, and source-key derivation from managed prefab names.
- `BuildingProductionTransportBridgeSystem`: `GameObject spawnUnitPrefab` input and footprint resolution from managed prefab source key.
- `CitizenVisibleUnitSystem`: no direct `GameObject` grep hit in the target file, but it remains a managed same-frame presentation bridge because visible citizens are instantiated and tracked immediately.
- `MapVehiclePlacementSpawnSystem`: no direct `GameObject` grep hit in the target file after `vehicleSourceKey`, but config projection still derives missing keys from authored `VehiclePrefab`.
- `CustomGameStartupSystem`: `UnitSpawnPrefabs`, `GameObject firstUnitPrefab`, `TryResolveConvertedPrefabEntity(GameObject)`, impostor atlas lookup by prefab, building lookup key from prefab.

Existing ECS data already available:

- `UnitSourcePrefabKey` on prefab entities and produced units.
- `RuntimeBuildingEntity.ProducedUnitSourceKeys` for produced-unit gameplay lookup.
- `UnitRespawnPrefab` and prefab-entity component data for spawn/reset paths.
- `MapVehiclePlacementConfigEntry.vehicleSourceKey` for vehicle placement source-key execution.
- Building configured read models and initial spawn buffers used by `InitialUnitsSpawnSystem`.

Managed fallback paths to keep outside `ISystem` code:

- UI/live preview prefab fallback in `BuildingUiCompositionSystem` and build drawer presentation.
- Legacy `ProducedUnitPrefabs` mapping while UI Toolkit replacement is incomplete.
- Serialized custom-game config, unit registry, atlas, sprite, and impostor projection.
- Authored map vehicle prefab fallback for unrebaked configs.
- Same-frame visible-citizen presentation state until ECS visible-citizen state exists.

Current grep snapshot for the five target files:

- `SystemBase`: all five target files.
- `GameObject`: `BuildingSpawnSystem`, `BuildingProductionTransportBridgeSystem`, `CustomGameStartupSystem`.
- `UnityEngine.Object`: none by direct target-file grep.
- `List<GameObject>`: none by direct target-file grep.
- `Dictionary<..., GameObject>`: `BuildingSpawnSystem`.
- `MapVehiclePlacementSpawnSystem` and `CitizenVisibleUnitSystem` have no direct `GameObject` grep hits but are still managed boundary classes.

Baseline validation notes:

- Main project Unity validation was locked by an open editor and failed on two attempts.
- Shadow validation used `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Mirrored `Design/Architecture/five_systembase_to_isystem_conversion_tracker.md`, `Design/Architecture/non_ecs_to_ecs_system_conversion_roadmap.md`, and the missing shadow asset `Assets/Game/Configs/Scene/Match_MapVehiclePlacement_Config.asset` plus `.meta`.
- Passed: `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=8`.
- Passed: `[BuildingProductionRequestValidation] result=Passed tests=10`.
- Passed: `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.
- Passed: `[UnitMovementBlockerValidation] result=Passed`.
- Passed: `[CustomGameStartupFocusedValidation] result=Passed tests=1`.

## Phase 1: Shared ECS Data Contracts

Purpose:
Create shared ECS data so later phases do not pass managed prefab objects through converted systems.

- [x] Confirm `UnitSourcePrefabKey` is written for every unit prefab entity used by production, citizens, vehicles, and custom startup.
- [x] Add or verify a compact unit footprint read model keyed by source key or prefab entity.
- [x] Add or verify a production slot read model that stores unit source keys instead of `GameObject` prefab references.
- [x] Add or verify produced-unit runtime state stores entity plus source key, not only `ProducedUnitPrefabs`.
- [x] Add or verify citizen visible-unit state can be represented by entity, citizen id, source key, owner, and lifecycle flags.
- [x] Add or verify map vehicle placement entries have baked source key and prefab entity data.
- [x] Add or verify custom-game startup can read unit/building startup source keys from ECS buffers or baked config entities.
- [x] Add architecture validation that converted target systems cannot introduce `GameObject`, `UnityEngine.Object`, `List<GameObject>`, or managed prefab reverse lookup dependencies.

Phase 1 source-key coverage notes:

- `UnitGridAuthoring.Baker` writes `UnitSourcePrefabKey` from `authoring.gameObject.name` for every baked unit prefab entity.
- `BuildingSpawnSystem` copies the resolved production source key onto spawned live units as `UnitSourcePrefabKey`; the older managed `RuntimeBuildingEntity.ProducedUnitSourceKeys` spawn mirror has been removed.
- Citizen prefab selection now resolves source keys through `CitizenPrefabSystem`/`BuildingSpawnPrefabSystem`; citizen unit prefabs are `UnitGridAuthoring`-authored and therefore carry `UnitSourcePrefabKey` after baking.
- `MapVehiclePlacementConfigEntry.VehicleSourceKey` stores the serialized source key and falls back to `VehiclePrefab.name` only at the managed config edge for unrebaked configs.
- `CustomGameStartupSystem` still reads serialized `UnitSpawnPrefabs`, but those prefab entities are produced from `UnitGridAuthoring` prefabs and therefore have the same source-key component once baked.

Phase 1 footprint read-model notes:

- `UnitFootprint` is the compact ECS read model for unit dimensions and is keyed by prefab entity once a source key resolves to an entity.
- `UnitGridAuthoring.Baker` writes `UnitFootprint` onto every baked unit prefab entity from the resolved configured footprint.
- `BuildingProductionTransportBridgeSystem` already resolves the production source key through `BuildingSpawnPrefabSystem.TryGetSpawnUnitPrefabEntity` and reads `UnitFootprint` from the prefab entity.
- `MapVehiclePlacementSpawnSystem` already resolves placement source key through `RuntimeUnitPrefabSystem.TryResolveConfiguredUnitPrefabEntity` and reads `UnitFootprint` from the prefab entity.
- `InitialUnitsSpawnSystem` already reads `UnitFootprint` directly from the initial unit prefab entity when planning startup placement.
- No separate source-key-to-footprint table is needed for the current conversion path; later slices can use source key to prefab entity to `UnitFootprint`.

Phase 1 production slot read-model notes:

- Added `BuildingProductionSlotReadModel` as an ECS boundary buffer with building id, slot index, exact unit source key, and normalized unit id.
- `BuildingRuntimeBoundarySystem` publishes production slot rows from managed `BuildingDefinitionSystem`/authored production slots, keeping `GameObject` access at the passive boundary.
- `MatchBootstrapSystem` and `RuntimeGameplayStateTestHelper` now ensure the production slot buffer exists on the boundary entity.
- Added focused coverage in `BuildingRuntimeBoundaryValidationTests.RuntimeBoundaryPublishesProductionSlotSourceKeyReadModel`.
- Main project validation was locked twice; shadow validation used `/Users/farhad/Projects/WarlineCapture-CodexUnity1` after syncing `Assets/Game/Scripts`, editor tests, and tracker docs.
- Passed: `[BuildingRuntimeBoundaryValidation] result=Passed tests=6`.

Phase 1 produced-unit source-key state notes:

- `RuntimeBuildingEntity` still stores produced unit entities in `ProducedUnits`; produced source keys now live on spawned entities through `UnitSourcePrefabKey` and on the boundary through `BuildingProducedUnitReadModel`.
- `BuildingSpawnSystem` writes `UnitSourcePrefabKey` onto each spawned unit entity; the older `ProducedUnitSourceKeys` spawn mirror has been removed. `ProducedUnitPrefabs` remains only as a legacy UI/prefab fallback outside spawn execution.
- `BuildingProductionSystem.PruneProducedUnits` now has focused test coverage proving it removes stale `ProducedUnitSourceKeys` entries together with stale produced entities and legacy prefab entries.
- `BuildingRuntimeBoundarySystem` and `BuildingRuntimeQuerySystem` resolve produced-unit ids from `ProducedUnitSourceKeys` before falling back to `ProducedUnitPrefabs` or the entity `UnitSourcePrefabKey` component.
- Main project validation was locked twice; shadow validation used `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Passed: `[ProducedUnitSourceKeyStateValidation] result=Passed tests=1`.

Phase 1 citizen visible-unit ECS state notes:

- Added `CitizenVisibleUnitState` as an ECS component on visible citizen unit entities.
- `CitizenVisibleUnitState` carries citizen id, prefab source key, owner faction id, life state, status, current target building id, and current goal cell.
- `CitizenVisibleUnitSystem` writes/refreshes `UnitSourcePrefabKey` and `CitizenVisibleUnitState` when instantiating the visible citizen entity; the existing `VisibleCitizensById` managed dictionary remains in place for current behavior.
- `CitizenVisibleUnitSystemTests.SpawnVisibleCitizenProjectsPrefabAndQueuesCitizenMovement` now asserts the new ECS state.
- Main project validation was locked twice; shadow validation used `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Passed: `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`.

Phase 1 map vehicle placement read-model notes:

- Added `MapVehiclePlacementReadModel` as an ECS boundary buffer with placement index, source path, category, normalized vehicle source key, resolved prefab entity, footprint cells, faction id, and authored transform metadata.
- `MapVehiclePlacementSpawnSystem` now projects the managed `MapVehiclePlacementConfig` edge into `MapVehiclePlacementReadModel` on the `BuildingRuntimeBoundaryTag` entity before the existing spawn/clearance flow runs.
- Existing config fallback from authored `VehiclePrefab.name` remains only at the managed config edge for older baked assets that do not serialize `vehicleSourceKey`; the ECS row stores the normalized key and prefab entity result.
- `MatchBootstrapSystem` and `RuntimeGameplayStateTestHelper` now ensure the map vehicle placement buffer exists on the boundary entity.
- Added focused coverage in `UnitMovementBlockerValidationTests.MapVehiclePlacementReadModelProjectsSourceKeyAndPrefabEntityData`.
- Main project validation was locked twice; shadow validation used `/Users/farhad/Projects/WarlineCapture-CodexUnity1` after syncing touched source/test files.
- Passed: `[UnitMovementBlockerValidation] result=Passed`.

Phase 1 custom-game startup source-key notes:

- Verified the modern `CustomGameStartupSystem.Initialize(CustomGameStartupConfig)` path emits unit startup source keys through `CustomGameFactionUnitSourceSpawnEntry`.
- Verified modern custom-game building startup uses `InitialUnitsFactionBuildingSpawnEntry.PrefabLookupKey` instead of a managed prefab reference.
- Verified `CustomGameUnitSourceRegistryEntry` carries unit roster source keys and display names while keeping legacy prefab entity fields null on the managed-config path.
- Added focused coverage in `CustomGameStartupSystemTests.InitializeCreatesSourceKeyStartupBuffers`.
- Main project validation was locked twice; shadow validation used `/Users/farhad/Projects/WarlineCapture-CodexUnity1` after syncing the focused test file.
- Passed: `[CustomGameStartupFocusedValidation] result=Passed tests=2`.

Phase 1 architecture guard notes:

- Added `NonEcsSystemConversionArchitectureTests.ConvertedFiveSystemBaseTargetsStayFreeOfManagedPrefabDependencies`.
- The guard activates only after one of the five tracked target declarations implements `ISystem`, so current managed boundaries can continue shrinking in small slices.
- Once active for a converted target, the guard blocks `GameObject`, `UnityEngine.Object`, `List<GameObject>`, `Dictionary<..., GameObject>`, `ProducedUnitPrefabs`, `TryResolveConvertedPrefabEntity`, `GetPrefabName`, and `FindAtlasEntry` from that converted file.
- Main project validation was locked twice; shadow validation used `/Users/farhad/Projects/WarlineCapture-CodexUnity1` after syncing the architecture test file.
- Passed: `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`.

## Phase 2: BuildingSpawnSystem Conversion

Purpose:
Decompose production spawn execution into focused ECS processors that use source-key/entity data.

- [x] Replace `GetProductionPrefabDelegate` with a source-key or prefab-entity production slot accessor.
- [x] Replace production spawn preconditions that require `GameObject` with ECS source-key/prefab-entity checks.
- [x] Ensure spawn position, owner faction, production index, and runtime building id are carried by ECS request data.
- [x] Move `BuildingSpawnRandomState` into an ECS singleton or explicit caller-owned value.
- [x] Remove gameplay writes to `ProducedUnitPrefabs`; keep only source-key/entity writes in spawn execution.
- [x] Move any remaining preview-prefab mapping into `BuildingUiCompositionSystem` or a passive UI boundary.
- [~] Decompose the broad spawn execution owner into focused ECS processors instead of converting the existing class as-is.
- [ ] Extract production spawn request intake into `BuildingProductionSpawnRequestSystem`.
- [ ] Extract production slot occupancy and reservation decisions into `BuildingProductionSlotReservationSystem`.
- [ ] Extract ground placement and spawn-cell resolution into `BuildingProductionPlacementSystem`.
- [ ] Extract helipad selection and air-unit placement into `BuildingHelipadSpawnSystem`.
- [ ] Extract prefab entity instantiation and spawn component initialization into `BuildingUnitInstantiationSystem`.
- [ ] Extract produced-unit read-model publication and slot ownership rows into `BuildingProducedUnitStateSystem`.
- [ ] Move no-boundary transform fallback into a passive managed projection boundary or remove it after all callers provide boundary rows.
- [ ] Retire or rename the remaining `BuildingSpawnSystem` shell so it no longer owns runtime gameplay execution.
- [ ] Update composition call sites to use focused `SystemHandle` access for the split systems.
- [ ] Preserve focused tests that directly exercise produced-unit faction resolution.
- [ ] Run `BuildingProductionRequestValidation`.
- [ ] Run `BuildingGameplayCompositionRuntimeSmokeValidation`.
- [ ] Run `NonEcsSystemConversionArchitectureValidation`.

Phase 2 source-key spawn notes:

- Added `BuildingSpawnSystem.TryGetProductionSourceKeyDelegate` and wired `BuildingRuntimeContextSystem` to `BuildingDefinitionSystem.TryGetProductionSourceKey`.
- `BuildingDefinition.ProductionSlotDefinition` now carries `SpawnUnitSourceKey`; `BuildingDefinitionSystem` fills it from configured production slots and falls back to the legacy prefab lookup key only at the managed definition edge.
- `BuildingSpawnSystem.TrySpawnPlayerUnitNearBuilding` no longer requires `GetProductionPrefabDelegate`; it resolves the ECS prefab entity from the production source key first.
- Helicopter production placement now uses the production source key plus ECS prefab entity data instead of `BuildingProductionSystem.IsHelicopterUnitPrefab(GameObject)`.
- Spawned units always receive `UnitSourcePrefabKey`; the older `RuntimeBuildingEntity.ProducedUnitSourceKeys` spawn mirror has since been removed. `ProducedUnitPrefabs` is written only when a real legacy prefab object is still supplied.
- Added focused coverage in `BuildingProductionSystemTests.BuildingSpawnSystem_SpawnsSourceKeyOnlyProductionSlot`, proving source-key-only production slots spawn without a managed prefab delegate.
- Main project validation failed twice with Unity database write errors; shadow validation used `/Users/farhad/Projects/WarlineCapture-CodexUnity1` after syncing the focused production files.
- Passed: `[BuildingProductionRequestValidation] result=Passed tests=12`.

Phase 2 random-state notes:

- Moved production spawn random state ownership from `BuildingSpawnSystem` to `BuildingGameplayCompositionSourceSystem.BuildingSpawnRandomState`.
- `BuildingProductionTickCompositionSystemHelper` now passes random state through the existing get/set delegates against the composition source instead of the target spawn system.
- Removed the hidden-random `BuildingSpawnSystem.TryResolveAvailableFactionHelipadSpawn` overloads; callers now pass `ref uint randomState`.
- Threaded caller-owned random state through `BuildingProductionTransportSystem.TryEnsureActiveProductionTransport` and `BuildingProductionTransportBridgeSystem.TryResolveAvailableFactionHelipadSpawn` for air-self helipad resolution.
- Updated the initial-faction helipad smoke to pass explicit random state.
- Shadow validation used `/Users/farhad/Projects/WarlineCapture-CodexUnity1` because the main project had already failed both Unity database write attempts in this run.
- Passed: `[BuildingProductionRequestValidation] result=Passed tests=12`.
- Passed: `[InitialFactionBaseBuildingGameplaySmokeValidation] result=Passed`.

Phase 2 production spawn request notes:

- Added `BuildingProductionSpawnRequest` as an ECS boundary buffer carrying request id, runtime building id, production index, reserved production slot index, owner faction, override flags, unit source key, prefab entity, produced unit, spawn cell, and spawn world position.
- `MatchBootstrapSystem` and `RuntimeGameplayStateTestHelper` now ensure the production spawn request buffer exists on the runtime boundary entity.
- `BuildingRuntimeContextSystem.RuntimeSource` now carries `BuildingRuntimeBoundaryQuery`; `BuildingRuntimeCompositionSystem` wires it from `BuildingGameplayEcsQuerySystem`.
- `BuildingSpawnSystem.TrySpawnPlayerUnitNearBuilding` publishes a bounded completed request row after successful spawn execution while preserving the current managed execution path.
- `BuildingProductionSystemTests.BuildingSpawnSystem_SpawnsSourceKeyOnlyProductionSlot` now asserts the ECS request row fields alongside the spawned entity state.
- Main project validation failed twice with Unity database write errors; shadow validation used `/Users/farhad/Projects/WarlineCapture-CodexUnity1` after syncing the focused boundary/spawn files.
- Passed: `[BuildingProductionRequestValidation] result=Passed tests=12`.

Phase 2 managed prefab removal notes:

- Removed `BuildingSpawnSystem.GetProductionPrefabDelegate` and the `Context.GetProductionPrefab` field; production spawn execution now requires `BuildingDefinitionSystem.TryGetProductionSourceKey` and ECS prefab entity resolution.
- Removed `BuildingSpawnSystem` gameplay writes to `RuntimeBuildingEntity.ProducedUnitPrefabs`; spawned units keep `UnitSourcePrefabKey`, `ProducedUnits`, and the ECS `BuildingProductionSpawnRequest` row.
- `BuildingSpawnSystem` target-file grep now has no `GameObject`, `GetProductionPrefab`, `spawnUnitPrefab`, `GetUnitPrefabSourceKey`, or `ProducedUnitPrefabs[...]` hits.
- `BuildingUiQuerySystem.AddProducedUnitEntries` now resolves ready produced-unit preview prefabs through the existing passive `TryResolveLiveUnitPreviewPrefab` delegate when the legacy prefab map is empty.
- Added `BuildingUiQuerySystemTests.AddProducedUnitEntries_ResolvesReadyPrefabFromPassivePreviewDelegate` to cover source-key/ECS-only produced units in UI query output.
- Passed: `git diff --check`.
- Passed: main-project `[BuildingProductionRequestValidation] result=Passed tests=12`.
- Passed: main-project `[BuildingUiQueryValidation] result=Passed tests=4`.

Phase 2 spawn-owner conversion notes:

- Started the `BuildingSpawnSystem` owner conversion item by moving one read path to ECS boundary data first.
- Added an overload of `BuildingSpawnSystem.TryGetFactionProductionSpawnPoint` that reads `BuildingFactionProductionSpawnPointReadModel` from the runtime boundary entity before falling back to the managed `RuntimeBuildingEntity.Instance.transform` path.
- Existing callers without `EntityManager` continue using the legacy runtime-building fallback, so behavior is preserved while ECS-owned callers can avoid managed transform reads.
- Added `BuildingProductionSystemTests.BuildingSpawnSystem_ResolvesFactionProductionSpawnPointFromBoundaryReadModel`.
- Remaining blockers before the actual `ISystem` flip: `TryResolveSpawnPlacement` still depends on `RuntimeBuildingEntity.Instance.transform`, production slot occupancy still mutates `RuntimeBuildingEntity.ProducedUnitSlots`, and recent spawn reservations still live in a managed `List<RecentSpawnReservation>`.
- Passed: `git diff --check`.
- Passed: main-project `[BuildingProductionRequestValidation] result=Passed tests=13`.

Phase 2 recent-spawn reservation notes:

- Added `BuildingRecentSpawnReservation` as an ECS boundary buffer carrying cell, footprint size, and expiration time for short-lived spawn exclusion zones.
- `MatchBootstrapSystem` and `RuntimeGameplayStateTestHelper` now ensure the recent-reservation buffer exists on the runtime boundary entity.
- `BuildingSpawnSystem` now reserves, overlaps, prunes, and writes recent spawn reservations through `BuildingRecentSpawnReservation` when `TryGetRuntimeBoundaryEntity` is available; the old managed `List<RecentSpawnReservation>` remains only as fallback for call paths that do not expose the boundary yet.
- Added `BuildingProductionSystemTests.BuildingSpawnSystem_WritesRecentSpawnReservationToBoundaryBuffer` with a non-air source-key-only spawn.
- Remaining blockers before the actual `ISystem` flip: `TryResolveSpawnPlacement` still depends on `RuntimeBuildingEntity.Instance.transform`, and successful production still mutates `RuntimeBuildingEntity.ProducedUnits` and `ProducedUnitSlots`.
- Passed: `git diff --check`.
- Passed: main-project `[BuildingProductionRequestValidation] result=Passed tests=14`.

Phase 2 produced-unit read-model notes:

- Added `BuildingProducedUnitReadModel` as an ECS boundary buffer carrying runtime building id, production index, production slot index, owner faction, produced entity, and unit source key.
- `MatchBootstrapSystem` and `RuntimeGameplayStateTestHelper` now ensure the produced-unit read-model buffer exists on the runtime boundary entity.
- `BuildingSpawnSystem.TrySpawnPlayerUnitNearBuilding` now appends a produced-unit read-model row immediately after successful spawn initialization, in addition to preserving the current managed `RuntimeBuildingEntity.ProducedUnits` write.
- Extended `BuildingProductionSystemTests.BuildingSpawnSystem_SpawnsSourceKeyOnlyProductionSlot` to verify the produced-unit read-model row mirrors the spawned entity and source key.
- Remaining blockers before the actual `ISystem` flip: `TryResolveSpawnPlacement` still depends on `RuntimeBuildingEntity.Instance.transform`, and successful production still mutates `RuntimeBuildingEntity.ProducedUnits` and `ProducedUnitSlots`; later slices can migrate readers to `BuildingProducedUnitReadModel` before removing those managed writes.
- Passed: `git diff --check`.
- Passed: main-project `[BuildingProductionRequestValidation] result=Passed tests=14`.

Phase 2 production-slot read-model placement notes:

- Added `BuildingRuntimeId` to `BuildingFactionProductionSpawnPointReadModel` and populated it in `BuildingRuntimeBoundarySystem`.
- `BuildingSpawnSystem.TryResolveSpawnPlacement` now resolves regular production-slot spawn cell/world position from `BuildingFactionProductionSpawnPointReadModel` by runtime building id and slot index before falling back to `RuntimeBuildingEntity.Instance.transform`.
- Fixed recent-reservation buffer lookup so read/overlap paths do not add missing buffers after grid buffer native arrays have been captured; only the write path creates the buffer.
- Added `BuildingProductionSystemTests.BuildingSpawnSystem_UsesBoundarySpawnPointForProductionSlotPlacement`, which uses a runtime building without a `GameObject` instance and verifies the spawned unit lands on the ECS read-model cell.
- Remaining blockers before the actual `ISystem` flip: helicopter slot resolution and fallback placement still use managed runtime-building transforms, and successful production still mirrors into managed produced-unit collections/slots.
- Passed: `git diff --check`.
- Passed: main-project `[BuildingProductionRequestValidation] result=Passed tests=15`.

Phase 2 helicopter override slot read-model notes:

- `BuildingSpawnSystem.TryResolveProductionSlotAtCell` now resolves override helicopter slot ownership from `BuildingFactionProductionSpawnPointReadModel` before falling back to runtime-building marker transforms.
- Added `BuildingProductionSystemTests.BuildingSpawnSystem_UsesBoundarySpawnPointForOverrideHelicopterSlot`, which maps an override helicopter spawn cell to a helipad slot through the ECS boundary read model while the helipad has no `GameObject` instance.
- Remaining blockers before the actual `ISystem` flip: automatic helicopter helipad search still uses managed runtime-building transforms, fallback placement paths still read `Instance.transform`, and successful production still mirrors into managed produced-unit collections/slots.
- Passed: `git diff --check`.
- Passed: main-project `[BuildingProductionRequestValidation] result=Passed tests=16`.

Phase 2 automatic helicopter read-model notes:

- `BuildingSpawnSystem.TryResolveHelicopterSpawnForFaction` now resolves available helipad spawn slots from `BuildingFactionProductionSpawnPointReadModel` before falling back to runtime-building marker transforms.
- Added `BuildingProductionSystemTests.BuildingSpawnSystem_UsesBoundarySpawnPointForAutomaticHelicopterSpawn`, which spawns a helicopter through the automatic resolver using a helipad with no `GameObject` instance.
- The fallback transform path remains for worlds without boundary spawn-point rows; no-boundary fallback nearest-slot distance still uses the producer transform when available.
- Remaining blockers before the actual `ISystem` flip: fallback placement paths still read `Instance.transform`, no-boundary nearest-slot ranking can still read the producer transform, and successful production still mirrors into managed produced-unit collections/slots.
- Passed: `git diff --check`.
- Passed: main-project `[BuildingProductionRequestValidation] result=Passed tests=17`.

Phase 2 automatic helicopter source-position read-model notes:

- `BuildingSpawnSystem.TryResolveHelicopterSpawnForFactionFromReadModel` now resolves the producer source position from `BuildingFactionProductionSpawnPointReadModel` instead of reading `sourceBuilding.Instance.transform`.
- The automatic helicopter test now puts a far helipad row before the near helipad row and includes a source building spawn-point row, proving boundary-backed nearest-slot ranking uses ECS data rather than first-row order or producer `GameObject` state.
- Remaining blockers before the actual `ISystem` flip: no-boundary fallback placement/ranking paths still read runtime-building transforms, and managed produced-unit list/slot fallbacks remain only for contexts without a runtime boundary.
- Passed: `git diff --check`.
- Passed: main-project `[BuildingProductionRequestValidation] result=Passed tests=21`.

Phase 2 produced-slot read-model ownership notes:

- Added `ProductionSlotBuildingRuntimeId` to `BuildingProducedUnitReadModel` so cross-building slot ownership, such as a factory-produced helicopter occupying a helipad slot, is represented in ECS data.
- `BuildingSpawnSystem` now selects available regular production slots from `BuildingFactionProductionSpawnPointReadModel` before falling back to `BuildingProductionSlotSystem.TryGetAvailableProductionSpawnSlot`.
- `BuildingSpawnSystem` now checks slot occupancy through `BuildingProducedUnitReadModel` when `RuntimeBuildingEntity.ProducedUnitSlots` is absent, while still honoring the legacy array when present.
- Added `BuildingProductionSystemTests.BuildingSpawnSystem_UsesBoundarySpawnPointWithoutManagedSlotArray`, proving a production spawn can use a boundary slot row without `ProductionSpawnLocalPositions` or `ProducedUnitSlots`.
- Extended helicopter slot tests to assert `ProductionSlotBuildingRuntimeId` points at the helipad runtime id.
- Remaining blockers before the actual `ISystem` flip: fallback placement paths still read `Instance.transform`, nearest-slot ranking can still read the producer transform, and successful production still mirrors into managed produced-unit collections/source-key dictionaries for legacy readers.
- Passed: `git diff --check`.
- Passed: main-project `[BuildingProductionRequestValidation] result=Passed tests=18`.

Phase 2 managed fallback field removal notes:

- Removed the private managed `List<RecentSpawnReservation>` fallback and nested `RecentSpawnReservation` class from `BuildingSpawnSystem`.
- Recent spawn reservations now use only the `BuildingRecentSpawnReservation` boundary buffer; if a context has no runtime boundary, reservation reads/writes no-op instead of storing private system state.
- Removed the private `_spawnGroundingSystem` field; the stateless `MapSurfaceSpawnGrounding` readonly struct is now created at the single grounding call site.
- `BuildingSpawnSystem` now has no private managed fields or private managed fallback collection; remaining managed blockers are runtime-building dictionaries/objects passed through `Context`, fallback `Instance.transform` reads, and managed produced-unit list/source-key mirror writes.
- Passed: `git diff --check`.
- Passed: main-project `[BuildingProductionRequestValidation] result=Passed tests=18`.

Phase 2 produced-unit read-model reader migration notes:

- `BuildingProductionTransportBridgeSystem` now resolves the newest produced unit from `BuildingProducedUnitReadModel` before falling back to `RuntimeBuildingEntity.ProducedUnits`.
- `MoveNewestProducedUnitToCell`, `AlignNewestProducedUnitRotation`, and `FocusNewestPlayerProducedUnit` all use the shared newest-produced-unit helper, reducing reliance on spawn's managed produced-unit list mirror.
- `BuildingProductionTransportSystem.ConfigureNewestRunwayUnit` now uses the bridge helper instead of directly reading `RuntimeBuildingEntity.ProducedUnits`.
- Added `BuildingProductionSystemTests.FocusNewestPlayerProducedUnit_UsesProducedUnitReadModel`, which focuses a produced unit from the ECS boundary read model while the runtime building has no produced-unit list.
- Remaining blockers before the actual `ISystem` flip: `BuildingSpawnSystem` still writes managed `ProducedUnits` for legacy UI readers, and fallback spawn placement still reads runtime-building transforms.
- Passed: `git diff --check`.
- Passed: main-project `[BuildingProductionCameraFocusValidation] result=Passed tests=10`.

Phase 2 produced-unit count read-model notes:

- `BuildingRuntimeQuerySystem.Context` now carries a passive runtime-boundary entity getter, wired from `BuildingRuntimeContextSystem.RuntimeSource.BuildingRuntimeBoundaryQuery`.
- `BuildingRuntimeQuerySystem.CountRuntimeProducedUnitsForFaction` now counts live produced units from `BuildingProducedUnitReadModel` before falling back to `RuntimeBuildingEntity.ProducedUnits`.
- Added `BuildingProductionSystemTests.CountRuntimeProducedUnitsForFaction_UsesProducedUnitReadModel`, which counts a produced unit from the ECS read model while the runtime building has no managed produced-unit list.
- Remaining blockers before the actual `ISystem` flip: `BuildingSpawnSystem` still writes managed `ProducedUnits` for legacy UI readers, and fallback spawn placement still reads runtime-building transforms.
- Passed: `git diff --check`.
- Passed: main-project `[BuildingProductionRequestValidation] result=Passed tests=19`.

Phase 2 production summary read-model notes:

- `BuildingRuntimeBoundarySystem.PublishRuntimeUnitProductionSummaries` now publishes produced counts from `BuildingProducedUnitReadModel` before falling back to `RuntimeBuildingEntity.ProducedUnits`.
- The read-model path is selected per runtime building only when produced-unit rows exist for that building, preserving legacy produced-unit list behavior for older runtime state with no ECS rows.
- Added `BuildingProductionSystemTests.BuildingRuntimeBoundary_ProductionSummaryUsesProducedUnitReadModel`, which publishes a production summary while the runtime building has no managed produced-unit list.
- Remaining blockers before the actual `ISystem` flip: `BuildingSpawnSystem` still writes managed `ProducedUnits` for remaining legacy UI readers, and fallback spawn placement still reads runtime-building transforms.
- Passed: `git diff --check`.
- Passed: main-project `[BuildingProductionRequestValidation] result=Passed tests=20`.

Phase 2 produced-unit source-key mirror removal notes:

- `BuildingSpawnSystem.TrySpawnPlayerUnitNearBuilding` no longer creates or writes `RuntimeBuildingEntity.ProducedUnitSourceKeys`.
- Produced units still receive the ECS `UnitSourcePrefabKey` component and the boundary `BuildingProducedUnitReadModel` row, which now feed the non-UI count and summary readers.
- Updated `BuildingProductionSystemTests.BuildingSpawnSystem_SpawnsSourceKeyOnlyProductionSlot` to assert the ECS component and absent managed source-key dictionary.
- Remaining blockers before the actual `ISystem` flip: `BuildingSpawnSystem` still writes managed `ProducedUnits` for legacy UI readers, still writes managed `ProducedUnitSlots` for legacy slot occupancy mirrors, and fallback spawn placement still reads runtime-building transforms.
- Passed: `git diff --check`.
- Passed: main-project `[BuildingProductionRequestValidation] result=Passed tests=20`.

Phase 2 production-slot read-model reservation notes:

- `BuildingProductionSystem.QueueContext` now carries the passive runtime-boundary entity getter from `BuildingSpawnSystem.Context`.
- `TryQueuePlayerUnitFromBuilding` now treats live `BuildingProducedUnitReadModel` rows as occupied production slots before queueing another unit, so reservation no longer depends only on `RuntimeBuildingEntity.ProducedUnitSlots`.
- `BuildingSpawnSystem.TrySpawnPlayerUnitNearBuilding` now writes `ProducedUnitSlots` only as a fallback when the produced-unit read-model row cannot be published; boundary-backed spawns use `BuildingProducedUnitReadModel` for slot ownership.
- Updated production spawn tests to assert managed slot arrays remain `Entity.Null` when the ECS produced-unit read-model row is written.
- Added `BuildingProductionSystemTests.TryQueuePlayerUnitFromBuilding_UsesProducedUnitReadModelSlotOccupancy`.
- Remaining blockers before the actual `ISystem` flip: `BuildingSpawnSystem` still writes managed `ProducedUnits` for remaining legacy readers, and fallback spawn placement still reads runtime-building transforms.
- Passed: `git diff --check`.
- Passed: main-project `[BuildingProductionRequestValidation] result=Passed tests=21`.

Phase 2 produced-unit list fallback removal notes:

- `BuildingUiQuerySystem` now reads selected-building ready produced units from `BuildingProducedUnitReadModel` when a runtime boundary entity is present, and still uses the passive preview-prefab delegate for UI presentation.
- `BuildingSpawnSystem.TrySpawnPlayerUnitNearBuilding` now writes `RuntimeBuildingEntity.ProducedUnits` only as fallback when it cannot publish a produced-unit read-model row; boundary-backed spawns keep produced-unit ownership in ECS data.
- Updated boundary-backed production spawn tests to read the spawned entity from `BuildingProducedUnitReadModel` and assert `RuntimeBuildingEntity.ProducedUnits` remains unset.
- Added `BuildingUiQuerySystemTests.SelectedBuildingProducedUnits_ReadsProducedUnitReadModel`.
- Remaining blockers before the actual `ISystem` flip: fallback spawn placement paths still read runtime-building transforms, and managed produced-unit list/slot fallbacks remain only for contexts without a runtime boundary.
- Passed: `git diff --check`.
- Passed: main-project `[BuildingProductionRequestValidation] result=Passed tests=21`.
- Passed: main-project `[BuildingUiQueryValidation] result=Passed tests=5`.

## Phase 3: BuildingProductionTransportBridgeSystem Conversion

Purpose:
Split production transport focus and movement behavior into ECS processors without managed prefab/footprint dependence.

- [ ] Replace `GameObject spawnUnitPrefab` inputs with produced unit entity plus source key.
- [ ] Resolve unit footprint from ECS read-model data instead of prefab authoring.
- [ ] Convert focus-newest-produced-unit flow to read produced entity/source-key state.
- [ ] Route any camera/UI focus feedback through an existing managed presentation boundary.
- [ ] Ensure transport bridge behavior still respects build drawer open state.
- [ ] Extract produced-unit transport request selection into `BuildingProductionTransportRequestSystem`.
- [ ] Extract movement target assignment and path kickoff into `BuildingProductionTransportMovementSystem`.
- [ ] Extract focus request emission into `BuildingProductionTransportFocusRequestSystem`.
- [ ] Extract runway-specific rotation and movement behavior into `BuildingRunwayTransportSystem`.
- [ ] Keep actual camera/UI focus application in a passive presentation boundary.
- [ ] Retire or rename the remaining `BuildingProductionTransportBridgeSystem` shell so it no longer owns runtime gameplay execution.
- [ ] Update production transport composition to use ECS request/result data instead of direct managed bridge calls.
- [ ] Remove obsolete prefab source-key helper methods from this file after callers stop passing `GameObject`.
- [ ] Run `BuildingProductionRequestValidation`.
- [ ] Run focused production transport tests in `BuildingProductionSystemTests`.
- [ ] Run `BuildingGameplayCompositionRuntimeSmokeValidation`.
- [ ] Run `NonEcsSystemConversionArchitectureValidation`.

## Phase 4: CitizenVisibleUnitSystem Conversion

Purpose:
Split visible-citizen spawn, movement state, and lifetime tracking into entity-owned ECS processors while preserving visible citizen behavior.

- [ ] Replace managed visible-citizen dictionaries with ECS components or buffers keyed by citizen id.
- [ ] Convert visible-citizen spawn requests to carry citizen id, source key, target position, owner, and movement target.
- [ ] Extract visible-citizen request production into `CitizenVisibleUnitSpawnRequestSystem`.
- [ ] Instantiate citizen prefab entities through `CitizenVisibleUnitInstantiateSystem`.
- [ ] Write the spawned entity back into ECS visible-citizen state without same-frame managed dictionary mutation.
- [ ] Extract current target and movement status writes into `CitizenVisibleUnitMovementStateSystem`.
- [ ] Convert arrival and despawn checks into `CitizenVisibleUnitLifetimeSystem`.
- [ ] Keep any presentation-only state in a passive managed boundary.
- [ ] Update citizen population composition so it no longer constructs `new CitizenVisibleUnitSystem()`.
- [ ] Retire or rename the remaining `CitizenVisibleUnitSystem` shell so it no longer owns runtime gameplay execution.
- [ ] Remove managed prefab selection inputs from visible-citizen execution.
- [ ] Run `CitizenVisibleUnitFocusedValidation`.
- [ ] Run citizen population focused validation.
- [ ] Run `BuildingGameplayCompositionRuntimeSmokeValidation`.
- [ ] Run `NonEcsSystemConversionArchitectureValidation`.

## Phase 5: MapVehiclePlacementSpawnSystem Conversion

Purpose:
Split map vehicle placement progress, instantiation, blocker ownership, and completion results into ECS state.

- [x] Bake or project placement config into ECS buffers with source key, prefab entity, position, faction, and placement metadata.
- [x] Move placement progress fields into an ECS singleton or request buffer.
- [x] Move random/progress counters out of managed fields.
- [ ] Ensure runtime execution never reads `VehiclePrefab`; only bootstrap/config projection may derive source keys from authored prefabs.
- [ ] Keep map vehicle config projection in a passive managed boundary.
- [ ] Extract placement progress scanning into `MapVehiclePlacementProgressSystem`.
- [ ] Extract prefab entity instantiation and spawn component initialization into `MapVehiclePlacementInstantiateSystem`.
- [ ] Extract blocker reservation and cleanup into `MapVehiclePlacementBlockerSystem`.
- [ ] Extract placement completion/result publication into `MapVehiclePlacementResultSystem`.
- [ ] Retire or rename the remaining `MapVehiclePlacementSpawnSystem` wrapper so it no longer owns runtime gameplay execution.
- [ ] Update building gameplay composition to schedule the ECS placement split systems instead of invoking the managed update wrapper.
- [ ] Add validation that map vehicle placement entries with source keys spawn the same configured entities.
- [x] Run `UnitMovementBlockerValidation`.
- [ ] Run map vehicle placement focused validation or add one if missing.
- [ ] Run `BuildingGameplayCompositionRuntimeSmokeValidation`.
- [ ] Run `NonEcsSystemConversionArchitectureValidation`.

Phase 5 progress-state notes:

- P7-0318 added `MapVehiclePlacementProgressState` as ECS component state for placement queue completion, authoring-hidden completion, next placement index, last cleared blocker cells, and random state.
- `MapVehiclePlacementSpawnSystem` no longer derives from `SystemBase`; it remains a direct helper owned by building gameplay composition, so the broad execution owner is not retired yet.
- The existing `MapVehiclePlacementReadModel` continues to project source key, prefab entity, position, faction, footprint, and authored transform metadata into ECS buffers.
- Added `UnitMovementBlockerValidationTests.MapVehiclePlacementProgressStateTracksEmptyConfigCompletion` to prove empty-config completion and authoring-root hiding are tracked through ECS state.
- Passed: `[UnitMovementBlockerValidation] result=Passed` in `/private/tmp/warline-phase7-integration-map-vehicle-placement-progress-state.log`.
- Remaining blockers before Phase 5 completion: extract placement scanning, instantiation, blocker reservation/cleanup, completion/result publication, and composition scheduling into focused ECS processors, then retire or rename the direct helper.

## Phase 6: CustomGameStartupSystem Conversion

Purpose:
Split serialized config projection from startup gameplay data creation, then convert startup processing into focused ECS processors.

- [ ] Separate serialized custom-game config reading from gameplay startup ECS writes.
- [ ] Create baked/projected ECS startup data for initial unit source keys, initial building source keys, faction setup, spawn cells, and selected options.
- [ ] Move unit prefab registry projection to ECS buffers keyed by source key and prefab entity.
- [ ] Move building lookup keys to ECS startup buffers or blob data.
- [ ] Keep impostor atlas, sprite, and preview metadata projection in a managed presentation/config boundary.
- [ ] Replace `TryResolveConvertedPrefabEntity(GameObject prefab)` with source-key/prefab-entity lookup before startup processing.
- [ ] Extract faction setup and selected option writes into `CustomGameFactionStartupSystem`.
- [ ] Extract initial building startup creation into `CustomGameBuildingStartupSystem`.
- [ ] Extract initial unit startup creation into `CustomGameUnitStartupSystem`.
- [ ] Extract startup completion/result publication into `CustomGameStartupResultSystem`.
- [ ] Retire or rename the remaining `CustomGameStartupSystem` shell so it is only a passive config projection boundary, or remove it if projection moves elsewhere.
- [ ] Update `MatchBootstrapSystem` so it no longer calls `GetOrCreateSystemManaged<CustomGameStartupSystem>()` for runtime gameplay startup.
- [ ] Preserve legacy config compatibility by projecting old serialized configs before ECS startup processing.
- [ ] Run `CustomGameStartupFocusedValidation`.
- [ ] Run initial units spawn focused validation.
- [ ] Run custom-game PlayMode smoke if available.
- [ ] Run `NonEcsSystemConversionArchitectureValidation`.

## Phase 7: Final Cleanup And Guardrails

Purpose:
Remove stale managed paths after all five systems are converted or split.

- [ ] Remove obsolete `GameObject` prefab delegates from the converted target systems.
- [ ] Remove obsolete produced-unit `GameObject` gameplay maps or mark them UI-only if still needed for UI Toolkit transition.
- [ ] Remove obsolete managed-world lookup code for converted systems.
- [ ] Update architecture tests to enforce the final inheritance state of the five targets.
- [ ] Update architecture tests to enforce the split-system rule: no converted target can remain one broad gameplay `ISystem`.
- [ ] Add or update architecture tests so extracted ECS processors do not reference `RuntimeBuildingEntity`, `GameObject`, `Transform`, `UnityEngine.Object`, `ProducedUnits`, `ProducedUnitSlots`, or `Instance.transform`.
- [ ] Update this tracker progress snapshot with final counts.
- [ ] Update `non_ecs_to_ecs_system_conversion_roadmap.md` with a one-line completion pointer to this tracker.
- [ ] Run `rg "SystemBase|GameObject|UnityEngine.Object|List<GameObject>|Dictionary<.*GameObject" <five target files>` and document any remaining passive-boundary exceptions.
- [ ] Run `git diff --check`.
- [ ] Run final focused Unity validation set.
- [ ] Run a PlayMode smoke path that covers production, citizen visibility, map vehicle placement, and custom startup.

## Required Final Validation Set

- `git diff --check`
- `NonEcsSystemConversionArchitectureValidation`
- `BuildingProductionRequestValidation`
- `BuildingGameplayCompositionRuntimeSmokeValidation`
- `CitizenVisibleUnitFocusedValidation`
- `UnitMovementBlockerValidation`
- `CustomGameStartupFocusedValidation`
- Initial units spawn focused validation
- Relevant PlayMode smoke for custom-game startup and runtime production

If the main Unity project is locked, retry once, then mirror only touched files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1` and run the same focused validation there.

## Completion Criteria

- [ ] All five target responsibilities are no longer managed `SystemBase` runtime gameplay systems.
- [ ] Each target responsibility is split into focused ECS processors or explicitly documented passive managed boundaries.
- [ ] No target is completed by preserving the old broad responsibility set inside one large `ISystem`.
- [ ] Any remaining managed pieces are renamed or documented as passive config/UI/presentation boundaries.
- [ ] Converted `ISystem` code has no runtime `GameObject`, `UnityEngine.Object`, `List<GameObject>`, or prefab reverse-lookup dependency.
- [ ] Production still spawns correct units.
- [ ] Citizen visible-unit spawning still works.
- [ ] Map vehicle placement still spawns configured entities.
- [ ] Custom game startup still produces the same initial unit/building setup.
- [ ] Final validation set passes.
