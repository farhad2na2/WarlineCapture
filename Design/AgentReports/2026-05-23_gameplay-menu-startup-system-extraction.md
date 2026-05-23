Lane
Gameplay

Task
Extract menu/UI startup binding out of `GameBootstrap.Start` into an ECS-aligned startup system boundary.

Files changed
`Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
`Assets/Game/Scripts/Systems/MenuStartupSystem.cs`
`Assets/Game/Scripts/Systems/MenuStartupSystem.cs.meta`
`Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
`Design/Architecture/gamebootstrap_responsibility_audit.md`
`Design/AgentReports/2026-05-23_gameplay-menu-startup-system-extraction.md`

Contracts touched
Added `GameplayArchitectureContractTests.GameBootstrapMustDelegateMenuStartupBinding` to prevent `MenuView` event subscription, menu view initialization, `MainMenuPlayUI` construction, menu dependency rebinding, and scene UI runtime binding from drifting back into `GameBootstrap`.
Updated `GameBootstrapMustDelegateBroadSceneLookupAndUiRuntimeBinding` so `MenuStartupSystem` may call `GameplaySceneBindingSystem` while `GameBootstrap` no longer calls the broad scene binding method directly.
Updated `Design/Architecture/gamebootstrap_responsibility_audit.md` to list `MenuStartupSystem` as the owner of menu/UI startup binding.

User-visible behavior
No intended runtime behavior change. The same menu view event, bootstrap-ready notification, main-menu construction, dependency rebinding, fallback rebinding, and loaded-scene UI dependency binding are preserved behind `MenuStartupSystem`.

Validation run
Unity EditMode validation in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
`GameplayArchitectureContractTests`

Validation result
Passed. Unity test XML reports 69 total, 69 passed, 0 failed at `/private/tmp/warlinecapture-menu-startup-architecture.xml`.
`GameBootstrap.cs` is now 416 lines; `MenuStartupSystem.cs` is 75 lines.

Known gaps
`GameBootstrap` still owns the lifecycle call sites for `BeginGameplay`, the per-frame managed update loop, and shutdown disposal order. The managed update loop remains intentionally paused because the architecture contract requires a focused FPS regression contract before re-extracting it.

Cross-lane impacts
No UI assets, scenes, or prefabs changed. UI lane should see the same startup binding behavior through the existing `MenuView` and loaded-scene UI bridge.

Next recommended task
Extract a narrow shutdown/disposal system only if it can preserve the exact disposal order and does not touch the paused per-frame runtime loop.
