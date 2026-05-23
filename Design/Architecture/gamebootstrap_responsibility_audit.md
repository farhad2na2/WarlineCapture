# GameBootstrap Responsibility Audit

This audit maps the current responsibilities inside `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs` before refactoring it. `GameBootstrap` is legacy composition debt: it should shrink by domain slice, but it should not be split randomly or gain new gameplay policy while the migration is in progress.

## Target Responsibility

`GameBootstrap` should have one reason to change: scene/application composition changed.

Allowed long-term responsibilities:
- Read serialized scene references and config assets.
- Create shell/runtime services.
- Install feature modules.
- Wire dependencies between shell services and Unity object edges.
- Start or stop the application lifecycle.

Not allowed long-term:
- Mission-specific behavior.
- AI policy or AI plan mutation.
- Faction economy policy.
- Camera/framing policy.
- Unit spawning policy.
- UI route rules.
- Asset-resolution policy.
- Direct gameplay diagnostics logging.
- Per-frame gameplay update ownership.

## Current Responsibility Buckets

### Composition And Serialized References

Current owner:
- `GameBootstrap`

Current examples:
- Scene refs: `MenuView`, `Camera`, `Light`, `Volume`, runtime roots, tactical binder.
- Config refs: selection, road build, building placement, attack trace, city/decorations/blockers, day/night, faction visuals, strings, prefab preview, AI controller configs.
- Runtime shell object creation: `DayNightSystem`, `FactionVisualSettings`, `RoadBuildSystem`, `BuildingPlacementSystem`, `RTSSelectionSystem`, `UnitAttackTraceSystem`, `UnitImpostorRenderSystem`, `CitizenPopulationSystem`.

Target:
- Keep as bootstrap composition temporarily.
- Later split pure wiring into feature installers named `*Installer` only if that reduces `GameBootstrap` without moving gameplay policy into another shell class.

Validation:
- Existing `NewBootstrapRootFilesMustBeCompositionOnly`.
- Existing `NewBootstrapRootFilesMustUseInstallerOrServiceNaming`.

### Runtime Gameplay State Boundary

Current owner:
- `GameBootstrap` uses `RuntimeGameplayStateSystem` and `RuntimeCameraReferenceSystem`.

Target:
- Keep shell-owned runtime boundary access in bootstrap.
- Runtime gameplay systems should read ECS singleton components directly.

Validation:
- Existing runtime-state contract tests.

### AI Startup Policy And Plan Mutation

Migrated owner:
- `AIStartupSystem`
- `AIFactionControlStartupSystem`
- `AIPlanEntryStartupSystem`
- `AIPlanEntryStartupConfig` asset for default fallback ids

Former bootstrap debt:
- `LogAIConfigValidation`
- `EnsureFactionControlConfigInitialized`
- `EnsureAIBuildPlansInitialized`
- `EnsureAIProductionPlansInitialized`
- `EnsureAISquadPlansInitialized`
- `EnsureAITargetPrioritySettingsInitialized`
- `ShouldIncludeAIConfig`
- AI default build and production entries.

Target owner:
- `AIStartupSystem`
- `AIFactionControlStartupSystem` for faction-control config singleton and entry buffer writes
- `AIPlanEntryStartupSystem` for preferred/default build and production plan-entry buffer writes
- `AIPlanEntryStartupConfig` for authored default build and production fallback ids
- AI startup config components/buffers
- AI config baking/authoring path where appropriate

Target behavior:
- Bootstrap should publish AI config data or install/request AI startup only.
- An ECS startup system should create/update `FactionControlEntry`, `AIBuildPlan`, `AIProductionPlan`, `AISquadPlan`, `AITargetPrioritySetting`, and AI diagnostics.
- Mission-specific fixed tactical policy belongs to `MissionStartupSystem`, not AI startup.

Migration order:
1. Done: move AI startup data projection into `AIStartupSystem` without changing config semantics.
2. Done: move default AI build/production fallback entry writes into `AIPlanEntryStartupSystem`.
3. Done: move default AI build/production fallback ids into authored `AIPlanEntryStartupConfig`.
4. Done: move fixed tactical AI disabling into mission startup policy.
5. Done: move faction-control startup projection into `AIFactionControlStartupSystem`.
6. Leave bootstrap with one call to install/request AI startup.

Focused validation:
- `GameplayArchitectureContractTests`
- `AI`
- A small focused AI startup test that compares produced singleton/buffer data from existing configs.

### Faction Economy Startup Policy

Migrated owner:
- `FactionEconomyStartupSystem`

Former bootstrap debt:
- `EnsureFactionEconomiesInitialized`

Target owner:
- `FactionEconomyStartupSystem`
- `FactionEconomyConfigComponent` or AI startup config buffers

Target behavior:
- Bootstrap should not calculate starting money, sell prices, or income multipliers.
- ECS startup should initialize `FactionEconomy` and `FactionEconomyPolicy` from config data.

Migration order:
1. Done: extract economy startup projection into `FactionEconomyStartupSystem`.
2. Done: keep data identical to the current bootstrap output.
3. Done: add focused tests for enemy faction entries, disabled enemy indexes, and existing economy entities.

Focused validation:
- `AIEconomyValidationTests`
- `RuntimeDiagnosticsSystemTests` only if diagnostics state is touched.

### Fixed Tactical Mission Guardrails

Migrated owner:
- `MissionStartupSystem`

Former bootstrap debt:
- `chapter01TacticalBinder?.TryApplyActiveMission`
- `Chapter01M01PlayableRuntime.TryInitializeActiveMission`
- `ApplyM01ProductionSceneVisibility`
- `ApplyFixedTacticalMissionGuardrails`
- Fixed tactical AI disabling call.
- Runtime legacy visual root hiding for M01.

Target owner:
- `MissionStartupSystem`
- Mission config component/buffer
- Shell installer only for scene reference binding

Target behavior:
- Bootstrap should not know `M01` policy.
- Mission startup should decide active mission, legacy visual visibility, day/night guardrails, and mission-specific AI behavior.

Migration order:
1. Done: move M01 active mission initialization behind `MissionStartupSystem`.
2. Done: move legacy visual root enable/disable to `MissionStartupSystem`.
3. Done: move day/night mission guardrail to `MissionStartupSystem`.
4. Done: move fixed tactical AI disabling to mission startup.

Focused validation:
- M01 playmode/startup tests.
- Architecture tests that reject new mission-specific bootstrap methods.

### Camera And Framing Policy

Migrated owner:
- `MissionStartupSystem`
- `InitialFactionSpawnCellSystem`

Former bootstrap debt:
- `TryGetConfiguredFactionSpawnCell`
- `M01PlayableStartOrthographicSize`
- `M01PlayableCameraHeight`
- `FocusCameraOnConfiguredFactionBase`
- `FocusCameraOnM01CameraStart`
- `ApplyM01ProductionCameraPose`
- `ResolveM01ProductionOrthographicSize`
- `ApplyM01ProductionCameraPoseForCurrentAspect`
- `TryResolveM01ProductionFrameCenter`
- `IncludeM01FrameAnchor`
- `ApplyM01ProductionCameraPoseIfActive`
- `ClampM01CameraCenterToTacticalMap`

Target owner:
- `RtsCameraSystem`
- `MissionCameraSystem`
- `InitialFactionSpawnCellSystem` for configured faction spawn-cell lookup until spawn config is fully ECS-authored.
- `RuntimeCameraFocusRequestComponent`
- Mission camera config data

Target behavior:
- Bootstrap should provide the camera reference and start request only.
- Camera/framing systems should resolve mission framing and write camera/focus requests.

Migration order:
1. Done: move M01 camera constants into `MissionStartupSystem` as interim mission camera policy.
2. Done: move M01 frame anchor calculation into `MissionStartupSystem` as interim mission camera policy.
3. Done: move fallback faction-base focus behind `MissionStartupSystem` with a bootstrap-provided spawn resolver.
4. Done: move configured faction spawn-cell lookup out of `GameBootstrap` into `InitialFactionSpawnCellSystem`.
5. Remove direct camera transform writes from bootstrap.

Focused validation:
- Existing camera/runtime-state tests.
- M01 aspect/framing proof tests if available.

### Gameplay Feature Runtime Updates

Current owner:
- `GameBootstrap` legacy runtime loop, retained after the `GameplayRuntimeUpdateSystem` extraction regressed runtime FPS.

Former bootstrap debt:
- `Update` calls runtime systems manually.
- `LateUpdate` calls attack traces and impostors.
- `OnGUI` calls road build and selection GUI.
- `IsGameplayStartComplete`

Target owner:
- ECS systems where possible.
- Shell services only where Unity object lifecycle requires it.
- Feature installers for temporary managed systems.

Target behavior:
- Bootstrap should not own a long per-frame gameplay update list.
- Temporary managed systems should be grouped behind a composed shell feature until they are ECS-owned.

Migration order:
1. Paused: do not re-extract the managed runtime loop through a managed wrapper without a focused FPS regression capture/contract.
2. Move `EnsureGameplaySystemsInitialized` into a feature installer only if the change does not alter per-frame runtime behavior.
3. Continue moving managed runtime systems into ECS domain systems by existing domain migrations.

Focused validation:
- Playmode smoke tests.
- Performance regression contract tests/captures.

### Diagnostics And Performance Logging

Migrated owner:
- `PerformanceDiagnosticsSystem`

Former bootstrap debt:
- `FreezeDetect` direct `Debug.Log`.
- `FrameRateDiag` direct `Debug.Log`.
- `PerfDiag` direct `Debug.Log`.
- Profiler recorder setup and string formatting.

Target owner:
- ECS diagnostic event buffers or a shell-injected logging service.
- `PerformanceDiagnosticsSystem` or shell diagnostic service.

Target behavior:
- Bootstrap may install diagnostics.
- Bootstrap should not format or emit gameplay/performance diagnostics directly.

Migration order:
1. Done: move direct log emission to `PerformanceDiagnosticsSystem`.
2. Done: move profiler recorder collection to `PerformanceDiagnosticsSystem`.
3. Keep current message content stable until QA accepts the migration.

Focused validation:
- Performance regression contract.
- Source contract that direct bootstrap diagnostics do not grow.

### Broad Scene Lookup And UI Runtime Binding

Migrated owner:
- `GameplaySceneBindingSystem`

Former bootstrap debt:
- `BindRuntimeGridBlockerDebugViews`
- `BindGameplayUiRuntimeDependencies`
- `FindLoadedSceneComponent`
- `Resources.FindObjectsOfTypeAll`

Target owner:
- Explicit scene binders.
- Feature installers.
- UI views that expose serialized references only.

Target behavior:
- Bootstrap should not use broad scene searches to discover gameplay/UI collaborators.
- Scene objects should register through explicit references, authoring, or ECS data.

Migration order:
1. Done: move runtime grid blocker debug-view broad lookup into `GameplaySceneBindingSystem`.
2. Done: move assistant/command controls UI runtime binding broad lookup into `GameplaySceneBindingSystem`.
3. Done: add no-broad-lookup guardrails for `GameBootstrap`.
4. Later replace `GameplaySceneBindingSystem` broad lookup with explicit scene references or authored binding config.

Focused validation:
- UI/assistant runtime binding tests.
- Scene smoke tests.

## Guardrail Ratchet

Current guardrails should be extended so:
- The audit document must exist and list all target owner buckets.
- New domain-policy method names in `GameBootstrap` are rejected unless explicitly added to the legacy debt list as part of a reviewed migration.
- Direct bootstrap `Debug.Log*` diagnostics cannot grow.
- New bootstrap root files must remain composition-only and use installer/service/config naming.

Reducing existing debt should never require weakening these tests.

## Recommended Migration Order

1. Add this audit and contract ratchet tests.
2. Extract AI startup policy into `AIStartupSystem`.
3. Extract faction economy startup into `FactionEconomyStartupSystem`.
4. Extract fixed tactical mission startup into mission ECS data/system.
5. Extract M01 camera/framing into `MissionCameraSystem` and camera request components.
6. Move performance diagnostics into a diagnostics boundary.
7. Replace broad scene lookup binding with explicit scene references/installers.
8. Collapse `GameBootstrap` to composition plus lifecycle calls only.
