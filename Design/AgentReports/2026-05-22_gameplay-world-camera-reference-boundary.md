# Lane
Gameplay

# Task
Migrate `WorldCamera` access from direct `InitialUnitsRuntimeState` usage into a managed runtime camera-reference boundary.

# Files changed
- `Assets/Game/Scripts/Components/RuntimeCameraReferenceComponent.cs`
- `Assets/Game/Scripts/Systems/RuntimeCameraReferenceSystem.cs`
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/Systems/UnitModelSpawnSystem.cs`
- `Assets/Game/Scripts/Systems/UnitRenderBudgetSystem.cs`
- `Assets/Tests/Editor/RuntimeCameraReferenceSystemTests.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`

# Contracts touched
- Added `RuntimeCameraReferenceComponent` as the managed ECS component for the `Camera` object reference.
- Added `RuntimeCameraReferenceSystem` as the sole production compatibility bridge for `InitialUnitsRuntimeState.WorldCamera`.
- Added architecture contract coverage blocking production direct access to `InitialUnitsRuntimeState.WorldCamera` outside the camera-reference boundary.
- Updated architecture docs to clarify Unity object references must not be added to unmanaged runtime state components.

# User-visible behavior
No intended behavior change. Runtime camera consumers still resolve the same world camera, but through a managed ECS camera-reference boundary instead of static global state.

# Validation run
- `git diff --check`
- Unity EditMode `RuntimeCameraReferenceSystemTests`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `BattleHudGameplayBridgeConnectionTests`

# Validation result
- `git diff --check`: passed.
- `RuntimeCameraReferenceSystemTests`: passed 3/3.
- `GameplayArchitectureContractTests`: passed 41/41.
- `BattleHudGameplayBridgeConnectionTests`: passed 6/6.
- There are no focused edit-mode tests for `UnitModelSpawnSystem` or `UnitRenderBudgetSystem`; this slice validates them through compile coverage and contract/smoke tests.

# Known gaps
- `InitialUnitsRuntimeState.WorldCamera` remains as legacy compatibility storage behind `RuntimeCameraReferenceSystem`.
- Diagnostics/log state and transport boarding diagnostics still use legacy static runtime state.
- Runtime rendering should be checked in-scene because the migrated consumers are visual systems.

# Cross-lane impacts
- QA should verify unit model LOD spawning and render budget behavior in a live scene with a world camera set by `GameBootstrap`.
- Performance should watch for camera-reference query overhead in `UnitModelSpawnSystem` and `UnitRenderBudgetSystem`; both cache their query, and reads are limited to managed component lookup.

# Next recommended task
Migrate diagnostics/log state next: `VerboseAILogs`, `AILog`, and related runtime diagnostics should move to an ECS diagnostics/logging boundary or shell-injected logging service.
