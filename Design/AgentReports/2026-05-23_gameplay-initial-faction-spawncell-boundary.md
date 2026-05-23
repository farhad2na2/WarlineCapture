# WarlineCapture Handoff

## Lane
Gameplay

## Task
Continue architecture refactoring while preserving the restored 60 FPS baseline. Extract configured faction spawn-cell lookup out of `GameBootstrap` without touching the per-frame runtime loop.

## Files changed
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/Systems/InitialFactionSpawnCellSystem.cs`
- `Assets/Game/Scripts/Systems/InitialFactionSpawnCellSystem.cs.meta`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/gamebootstrap_responsibility_audit.md`

## Contracts touched
- Gameplay SOLID/ECS contract now states that configured faction spawn-cell resolution is owned by `InitialFactionSpawnCellSystem`.
- Bootstrap responsibility audit now marks `TryGetConfiguredFactionSpawnCell` as migrated out of `GameBootstrap`.
- Architecture contract tests now reject reintroducing `TryGetConfiguredFactionSpawnCell` as a `GameBootstrap` method and require the new boundary to remain instance-scoped.

## User-visible behavior
No intended user-visible behavior change. AI startup and initial camera focus still receive the same configured faction spawn-cell resolver, but the lookup now lives behind `InitialFactionSpawnCellSystem`.

## Validation run
- `git diff --check -- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Game/Scripts/Systems/InitialFactionSpawnCellSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/gamebootstrap_responsibility_audit.md`
- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `GameplayArchitectureContractTests`

## Validation result
- `git diff --check`: passed.
- `GameplayArchitectureContractTests`: passed 65/65. Results: `/private/tmp/warlinecapture-initial-faction-spawncell-architecture.xml`.

## Known gaps
- Did not run a gameplay FPS capture because this slice does not touch `Update`, `LateUpdate`, `OnGUI`, rendering, city spawning, selection, building placement, or diagnostics sampling.
- `GameBootstrap` still owns the legacy managed runtime update list by design; the contract keeps that extraction paused until a focused FPS regression contract exists.
- Broad scene lookup/UI runtime binding remains the next audited bootstrap debt.

## Cross-lane impacts
- No Art/UI source changes.
- QA can treat this as architecture-only unless gameplay start or camera focus regression is observed.

## Next recommended task
Replace `GameBootstrap` broad scene lookup/UI runtime binding with explicit scene references or a startup binding boundary, but keep it startup-only and validate FPS before and after if any runtime update path is touched.
