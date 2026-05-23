# WarlineCapture Handoff

## Lane
Gameplay

## Task
Respond to reported FPS regression after the startup-only gameplay feature initialization extraction.

## Files changed
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/gamebootstrap_responsibility_audit.md`
- `Design/AgentReports/2026-05-23_gameplay-feature-initialization-rollback.md`

## Contracts touched
- Removed the `GameplayFeatureInitializationSystem` ownership contract because the extracted boundary correlated with a runtime FPS regression.
- Bootstrap responsibility audit now records `EnsureGameplaySystemsInitialized` as paused in `GameBootstrap`.
- Architecture contract tests no longer require the extracted startup feature initialization boundary.

## User-visible behavior
Restores the pre-extraction startup initialization shape for runtime city spawning, runtime grid blockers, runtime decorations, and their dependency rebinding. This is intended to return the runtime behavior to the last user-confirmed 60 FPS state.

## Validation run
- `git diff --check -- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/gamebootstrap_responsibility_audit.md Design/AgentReports/2026-05-23_gameplay-feature-initialization-rollback.md`

## Validation result
- `git diff --check`: passed.
- `GameplayArchitectureContractTests`: passed 66/66. Results: `/private/tmp/warlinecapture-gameplay-feature-init-rollback-architecture.xml`.

## Known gaps
- The exact profiler marker responsible for the reported FPS drop is not identified yet.
- `EnsureGameplaySystemsInitialized` remains bootstrap debt by design until a profiler-backed extraction plan exists.
- No new architecture extraction should touch startup/runtime feature initialization until a focused runtime FPS capture is part of the acceptance criteria.

## Cross-lane impacts
- No Art/UI asset changes.
- No scene content changes.

## Next recommended task
Run a focused runtime FPS/profiler capture on the restored code path first. Continue architecture refactoring only in non-runtime-hot domains or with before/after profiler evidence.
