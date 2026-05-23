Lane
Gameplay

Task
Extract the managed runtime update loop from `GameBootstrap` into an ECS-aligned runtime update system.

Files changed
`Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
`Assets/Game/Scripts/Systems/GameplayRuntimeUpdateSystem.cs`
`Assets/Game/Scripts/Systems/GameplayRuntimeUpdateSystem.cs.meta`
`Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
`Design/Architecture/gamebootstrap_responsibility_audit.md`
`Design/Architecture/gameplay_solid_ecs_contract.md`
`Design/AgentReports/2026-05-23_gameplay-runtime-update-system-extraction.md`

Contracts touched
Replaced the old paused-update-loop guard with `GameplayArchitectureContractTests.GameBootstrapMustDelegateManagedRuntimeUpdateLoop`.
Updated mission/camera and contract assertions so mission runtime update and M01 production camera pose calls are expected inside `GameplayRuntimeUpdateSystem`, not `GameBootstrap`.
Updated the architecture contract and bootstrap responsibility audit to mark `GameplayRuntimeUpdateSystem` as the owner of managed `Update`, `LateUpdate`, `OnGUI`, and gameplay-start-complete orchestration.

User-visible behavior
No intended runtime behavior change. The update step order and performance diagnostic labels are preserved: menu input, mission runtime, road build, building placement, selection, mission camera, runtime city, blockers, decorations, day/night, citizen population, menu canvas, and main menu.

Validation run
Unity EditMode validation in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
`GameplayArchitectureContractTests`

Validation result
Passed. Unity test XML reports 70 total, 70 passed, 0 failed at `/private/tmp/warlinecapture-runtime-update-architecture-rerun.xml`.
`GameBootstrap.cs` is now 329 lines; `GameplayRuntimeUpdateSystem.cs` is 176 lines.

Known gaps
This validation is architecture-focused. It confirms compile and contract shape, not live FPS. Runtime FPS should still be checked in the main Unity editor because this area has previously been performance-sensitive.

Cross-lane impacts
No UI assets, scenes, prefabs, or design data changed. UI runtime calls still flow through existing `MenuView`, `MainMenuPlayUI`, and loaded-scene UI binding behavior.

Next recommended task
Run an in-editor gameplay FPS smoke pass with the existing `FreezeDetect`/`PerfDiag` logs enabled, then decide whether the extracted update orchestration needs a focused PlayMode performance contract.
