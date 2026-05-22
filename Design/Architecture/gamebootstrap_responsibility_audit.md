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

Former bootstrap debt:
- `LogAIConfigValidation`
- `EnsureFactionControlConfigInitialized`
- `EnsureAIBuildPlansInitialized`
- `EnsureAIProductionPlansInitialized`
- `EnsureAISquadPlansInitialized`
- `EnsureAITargetPrioritySettingsInitialized`
- `DisableGenericAIPlansForFixedTacticalMission`
- `DisableAIBuildPlans`
- `DisableAIProductionPlans`
- `DisableAISquadPlans`
- `ShouldIncludeAIConfig`
- AI default build and production entries.

Target owner:
- `AIStartupSystem`
- AI startup config components/buffers
- AI config baking/authoring path where appropriate

Target behavior:
- Bootstrap should publish AI config data or install/request AI startup only.
- An ECS startup system should create/update `FactionControlEntry`, `AIBuildPlan`, `AIProductionPlan`, `AISquadPlan`, `AITargetPrioritySetting`, and AI diagnostics.

Migration order:
1. Done: move AI startup data projection into `AIStartupSystem` without changing config semantics.
2. Move default AI build/production fallback entries into config or ECS startup helpers.
3. Move fixed tactical AI disabling into mission startup policy.
4. Leave bootstrap with one call to install/request AI startup.

Focused validation:
- `GameplayArchitectureContractTests`
- `AI`
- A small focused AI startup test that compares produced singleton/buffer data from existing configs.

### Faction Economy Startup Policy

Current bootstrap debt:
- `EnsureFactionEconomiesInitialized`

Target owner:
- `FactionEconomyStartupSystem`
- `FactionEconomyConfigComponent` or AI startup config buffers

Target behavior:
- Bootstrap should not calculate starting money, sell prices, or income multipliers.
- ECS startup should initialize `FactionEconomy` and `FactionEconomyPolicy` from config data.

Migration order:
1. Extract economy startup projection into `FactionEconomyStartupSystem`.
2. Keep data identical to the current bootstrap output.
3. Add focused tests for player-auto and enemy faction entries.

Focused validation:
- `AIEconomyValidationTests`
- `RuntimeDiagnosticsSystemTests` only if diagnostics state is touched.

### Fixed Tactical Mission Guardrails

Current bootstrap debt:
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
1. Move M01 active mission initialization behind a mission startup request/component.
2. Move legacy visual root enable/disable to a mission scene binding or mission shell adapter.
3. Move day/night mission guardrail to mission startup data.
4. Move fixed tactical AI disabling to mission startup.

Focused validation:
- M01 playmode/startup tests.
- Architecture tests that reject new mission-specific bootstrap methods.

### Camera And Framing Policy

Current bootstrap debt:
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
- `TryGetConfiguredFactionSpawnCell`

Target owner:
- `RtsCameraSystem`
- `MissionCameraSystem`
- `RuntimeCameraFocusRequestComponent`
- Mission camera config data

Target behavior:
- Bootstrap should provide the camera reference and start request only.
- Camera/framing systems should resolve mission framing and write camera/focus requests.

Migration order:
1. Move M01 camera constants into mission camera config.
2. Move M01 frame anchor calculation into `MissionCameraSystem`.
3. Move fallback faction-base focus into camera request data.
4. Remove direct camera transform writes from bootstrap.

Focused validation:
- Existing camera/runtime-state tests.
- M01 aspect/framing proof tests if available.

### Gameplay Feature Runtime Updates

Current bootstrap debt:
- `Update` calls runtime systems manually.
- `LateUpdate` calls attack traces and impostors.
- `OnGUI` calls road build and selection GUI.
- `EnsureGameplaySystemsInitialized`
- `IsGameplayStartComplete`

Target owner:
- ECS systems where possible.
- Shell services only where Unity object lifecycle requires it.
- Feature installers for temporary managed systems.

Target behavior:
- Bootstrap should not own a long per-frame gameplay update list.
- Temporary managed systems should be grouped behind a composed shell feature until they are ECS-owned.

Migration order:
1. Group temporary managed update calls behind a feature runtime shell.
2. Move gameplay-complete readiness into ECS startup/readiness components.
3. Continue moving managed runtime systems into ECS domain systems by existing domain migrations.

Focused validation:
- Playmode smoke tests.
- Performance regression contract tests/captures.

### Diagnostics And Performance Logging

Current bootstrap debt:
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
1. Move direct log emission to a diagnostics boundary.
2. Move profiler recorder collection into a shell diagnostic service.
3. Keep current message content stable until QA accepts the migration.

Focused validation:
- Performance regression contract.
- Source contract that direct bootstrap diagnostics do not grow.

### Broad Scene Lookup And UI Runtime Binding

Current bootstrap debt:
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
1. Replace broad lookup for runtime grid blocker debug views with explicit scene binding.
2. Replace assistant/command controls lookup with scene bootstrap references or installer config.
3. Add no-broad-lookup guardrails for new bootstrap code.

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
