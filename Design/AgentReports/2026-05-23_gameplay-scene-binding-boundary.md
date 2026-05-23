# WarlineCapture Handoff

## Lane
Gameplay

## Task
Continue architecture refactoring while preserving the restored 60 FPS baseline. Move broad scene lookup and UI runtime binding out of `GameBootstrap`.

## Files changed
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/Systems/GameplaySceneBindingSystem.cs`
- `Assets/Game/Scripts/Systems/GameplaySceneBindingSystem.cs.meta`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/gamebootstrap_responsibility_audit.md`

## Contracts touched
- Gameplay SOLID/ECS contract now states that broad scene lookup and UI runtime binding are owned by `GameplaySceneBindingSystem`.
- Bootstrap responsibility audit now marks runtime grid blocker debug-view binding and assistant/command-controls binding as migrated out of `GameBootstrap`.
- Architecture contract tests now reject direct `Resources.FindObjectsOfTypeAll` usage and loaded-scene filtering helpers in `GameBootstrap`.

## User-visible behavior
No intended user-visible behavior change. Startup still binds match command controls, assistant runtime dependencies, and runtime grid blocker debug views, but `GameBootstrap` delegates that work to `GameplaySceneBindingSystem`.

## Validation run
- `git diff --check -- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Game/Scripts/Systems/GameplaySceneBindingSystem.cs Assets/Game/Scripts/Systems/GameplaySceneBindingSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/gamebootstrap_responsibility_audit.md`
- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `GameplayArchitectureContractTests`

## Validation result
- `git diff --check`: passed.
- `GameplayArchitectureContractTests`: passed 66/66. Results: `/private/tmp/warlinecapture-gameplay-scene-binding-architecture.xml`.

## Known gaps
- `GameplaySceneBindingSystem` still uses broad lookup internally as an interim startup boundary. The next improvement is replacing it with explicit scene references or authored binding config.
- Did not run a gameplay FPS capture because this slice does not touch `Update`, `LateUpdate`, `OnGUI`, rendering, selection, building placement, city spawning, or diagnostics sampling.
- `GameBootstrap` still owns legacy managed runtime update sequencing by contract until a focused FPS regression contract exists.

## Cross-lane impacts
- No Art/UI asset changes.
- UI/runtime binding behavior is intended to remain identical; UI should only need review if assistant or command-control binding regresses at startup.

## Next recommended task
Extract `EnsureGameplaySystemsInitialized` into a startup-only gameplay feature initialization boundary, without changing the per-frame update order. Validate with architecture tests and a quick manual FPS replay before touching any runtime update path.
