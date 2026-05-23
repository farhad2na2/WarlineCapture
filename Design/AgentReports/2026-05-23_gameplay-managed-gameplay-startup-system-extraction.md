Lane
Gameplay

Task
Extract managed system construction and dependency wiring out of `GameBootstrap.Awake` into an ECS-aligned startup system boundary.

Files changed
`Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
`Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs`
`Assets/Game/Scripts/Systems/ManagedGameplayStartupSystem.cs.meta`
`Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
`Design/Architecture/gamebootstrap_responsibility_audit.md`
`Design/Architecture/gameplay_solid_ecs_contract.md`
`README.md`
`Design/AgentReports/2026-05-23_gameplay-managed-gameplay-startup-system-extraction.md`

Contracts touched
Added `GameplayArchitectureContractTests.GameBootstrapMustDelegateManagedGameplayStartup` to prevent managed gameplay construction from drifting back into `GameBootstrap`.
Renamed the new bootstrap-root naming guard to `NewBootstrapRootFilesMustUseCompositionBoundaryNaming` and removed `Installer` from the accepted naming suffixes.
Updated the bootstrap responsibility audit, architecture contract, and README to use ECS-aligned startup system language instead of feature-installer language.

User-visible behavior
No intended runtime behavior change. This is a composition refactor: the same managed systems are constructed, wired, and assigned back to the bootstrap fields.

Validation run
Unity EditMode validation in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
`GameplayArchitectureContractTests`

Validation result
Passed. Unity test XML reports 68 total, 68 passed, 0 failed at `/private/tmp/warlinecapture-managed-gameplay-startup-architecture-final.xml`.
`GameBootstrap.cs` is now 436 lines; `ManagedGameplayStartupSystem.cs` is 96 lines.

Known gaps
`GameBootstrap` still owns lifecycle order, runtime bridge calls, and remaining Start/Update orchestration. Those should be extracted only behind focused contracts so we do not repeat the earlier performance-risk pattern.

Cross-lane impacts
None for UI, Art, or Design. No scene, prefab, or user-facing tuning assets were changed.

Next recommended task
Extract the remaining `Start()` UI/menu startup binding into a narrow ECS-aligned startup system, with a contract that preserves current binding order and keeps UI View classes as serialized-reference-only bridges.
