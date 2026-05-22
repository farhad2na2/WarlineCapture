# Lane
Gameplay

# Task
Migrate remaining production `PlayRequested` callers away from direct `InitialUnitsRuntimeState` access.

# Files changed
- `Assets/Game/Scripts/Environment/RuntimeCitySpawnerSystem.cs`
- `Assets/Game/Scripts/Tutorial/Assistant/AssistantContextProvider.cs`
- `Assets/Game/Scripts/UI/UnitAttackTraceSystem.cs`
- `Assets/Game/Scripts/UI/Shell/WarlineCaptureMatchResultFlow.cs`
- `Assets/Game/Scripts/Systems/AIBuildPlannerSystem.cs`
- `Assets/Game/Scripts/Systems/AICombatOrderSystem.cs`
- `Assets/Game/Scripts/Systems/AIEconomySystem.cs`
- `Assets/Game/Scripts/Systems/AIFactionControlSystem.cs`
- `Assets/Game/Scripts/Systems/AIProductionSystem.cs`
- `Assets/Game/Scripts/Systems/AISquadSystem.cs`
- `Assets/Game/Scripts/Systems/AITargetingSystem.cs`
- `Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs`
- `Assets/Game/Scripts/Systems/PreGameEcsActivityDiagnosticsSystem.cs`
- `Assets/Game/Scripts/Systems/ThreatDetectionWarningSystem.cs`
- `Assets/Game/Scripts/Systems/UnitPathfindingSystem.cs`
- `Assets/Game/Scripts/Systems/UnitRenderBudgetSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Assets/Tests/Editor/RuntimeGameplayStateTestHelper.cs`
- AI/threat/base-breach validation tests updated to seed runtime ECS state.
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`

# Contracts touched
- Added a contract test preventing production scripts from reading/writing `InitialUnitsRuntimeState.PlayRequested` outside `RuntimeGameplayStateSystem`.
- Updated the architecture contract to clarify managed callers use `RuntimeGameplayStateSystem`, while ECS `ISystem` callers read runtime singleton components directly.

# User-visible behavior
No intended gameplay behavior change. Play state still mirrors legacy static compatibility state, but gameplay systems now consume runtime ECS state instead of static global state.

# Validation run
- `git diff --check`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `RuntimeGameplayStateSystemTests`
- Unity EditMode `AI`
- Unity EditMode `AssistantContextProviderTests`
- Unity EditMode `ThreatWarningValidationTests`
- Unity EditMode `BattleHudGameplayBridgeConnectionTests`

# Validation result
- `git diff --check`: passed.
- `GameplayArchitectureContractTests`: passed 39/39.
- `RuntimeGameplayStateSystemTests`: passed 6/6.
- `AI`: passed 18/18.
- `AssistantContextProviderTests`: passed 7/7.
- `BattleHudGameplayBridgeConnectionTests`: passed 6/6.
- `ThreatWarningValidationTests`: gameplay/system tests passed 5/6; the remaining failure is `GameScene_TacticalWarningPanelIsWiredOnMenuView`, which failed because the CodexUnity1 copied scene YAML does not contain the expected `MenuView` block. The runtime threat detection tests affected by this migration passed.

# Known gaps
- `PlayerAutoModeEnabled`, `WorldCamera`, `VerboseAILogs`/`AILog`, and transport boarding diagnostics still use legacy static runtime state.
- Editor prefab builder code still writes `InitialUnitsRuntimeState.PlayRequested`; this report only covers production runtime scripts.

# Cross-lane impacts
- QA should verify play start, match result completion, AI economy/build/production/squad/target/combat loops, threat warnings, unit pathfinding, initial spawn, and runtime city spawning.
- Tooling/tests should seed `RuntimeGameplayStateComponent` when manually driving ECS systems outside `GameBootstrap`.

# Next recommended task
Migrate `PlayerAutoModeEnabled` into a runtime ECS singleton and update `GameBootstrap`/`MenuView`, then handle `WorldCamera` through a dedicated camera-reference boundary.
