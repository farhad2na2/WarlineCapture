Lane
Gameplay

Task
Extract `EnsureGameplaySystemsInitialized` construction and dependency binding out of `GameBootstrap` into an ECS-aligned startup system boundary.

Files changed
`Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
`Assets/Game/Scripts/Systems/GameplayFeatureStartupSystem.cs`
`Assets/Game/Scripts/Systems/GameplayFeatureStartupSystem.cs.meta`
`Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
`Design/Architecture/gamebootstrap_responsibility_audit.md`
`Design/AgentReports/2026-05-23_gameplay-feature-startup-system-extraction.md`

Contracts touched
Added `GameplayArchitectureContractTests.GameBootstrapMustDelegateGameplayFeatureStartup` to prevent runtime feature construction and startup dependency binding from drifting back into `GameBootstrap`.
Updated the bootstrap responsibility audit: the old `EnsureGameplaySystemsInitialized` startup construction is now owned by `GameplayFeatureStartupSystem`, while per-frame runtime update ownership remains intentionally in `GameBootstrap` until there is a focused FPS regression contract.

User-visible behavior
No intended runtime behavior change. Runtime city spawning, grid blocker spawning, decoration spawning, road/building dependency rebinding, and grid blocker debug-view binding are initialized in the same order as before.

Validation run
Unity EditMode validation in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
`GameplayArchitectureContractTests`

Validation result
Passed. Unity test XML reports 70 total, 70 passed, 0 failed at `/private/tmp/warlinecapture-gameplay-feature-startup-architecture.xml`.
`GameBootstrap.cs` is now 417 lines; `GameplayFeatureStartupSystem.cs` is 64 lines.

Known gaps
The per-frame managed update loop remains in `GameBootstrap` by design. The architecture contract still blocks re-extracting that loop without a focused FPS regression capture/contract.

Cross-lane impacts
No UI assets, scenes, prefabs, or design data changed. This is a startup composition refactor only.

Next recommended task
Extract shutdown/disposal order into a narrow startup/shutdown system only if the exact disposal order is preserved and the paused per-frame update loop remains untouched.
