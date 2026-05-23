# WarlineCapture Gameplay Handoff

## Lane
Gameplay

## Task
Move fixed tactical AI disabling policy from `AIStartupSystem` into the mission startup boundary.

## Files changed
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/Systems/AIStartupSystem.cs`
- `Assets/Game/Scripts/Systems/MissionStartupSystem.cs`
- `Assets/Game/Scripts/Systems/MissionStartupSystem.cs.meta`
- `Assets/Game/Scripts/Campaign/Chapter01M01PlayableRuntime.cs`
- `Assets/Tests/Editor/AIStartupSystemValidationTests.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Assets/Tests/Editor/Campaign/Chapter01M01PlayableRuntimeTests.cs`
- `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/gamebootstrap_responsibility_audit.md`
- `Design/AgentReports/2026-05-23_gameplay-fixed-tactical-ai-policy-mission-boundary.md`

## Contracts touched
- `AIStartupSystem.Initialize` no longer accepts or applies fixed tactical mission state.
- `MissionStartupSystem.Initialize` now disables generic AI build, production, and squad plans when the active mission is fixed tactical.
- Architecture tests now reject fixed tactical policy inside `AIStartupSystem` and require the disabling path under `MissionStartupSystem`.
- The architecture contract now states that AI startup must not own mission-specific fixed tactical policy.

## User-visible behavior
- M01 fixed tactical missions still disable generic AI build/production/squad plans, but the decision now lives with mission startup instead of AI startup.
- Non-fixed-tactical startup still leaves generic AI plans enabled.

## Validation run
- `git diff --check -- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Game/Scripts/Systems/AIStartupSystem.cs Assets/Game/Scripts/Systems/MissionStartupSystem.cs Assets/Game/Scripts/Systems/MissionStartupSystem.cs.meta Assets/Game/Scripts/Campaign/Chapter01M01PlayableRuntime.cs Assets/Tests/Editor/AIStartupSystemValidationTests.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Assets/Tests/Editor/Campaign/Chapter01M01PlayableRuntimeTests.cs Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/gamebootstrap_responsibility_audit.md`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `AIStartupSystemValidationTests`
- Unity EditMode `Chapter01M01PlayableRuntimeTests`
- Unity EditMode `AI`

## Validation result
- `git diff --check`: passed.
- `GameplayArchitectureContractTests`: passed `60/60`.
- `AIStartupSystemValidationTests`: passed `1/1`.
- `Chapter01M01PlayableRuntimeTests`: passed `10/10`.
- `AI`: passed `33/33`.

## Known gaps
- `MissionStartupSystem` still owns this as managed startup policy, not as a pure ECS mission component/system.
- `GameBootstrap.TryGetConfiguredFactionSpawnCell` remains legacy fallback camera-focus debt.
- `AIStartupSystem` still owns AI config projection and default plan entry creation; a later slice can move default plan entries closer to config authoring.

## Cross-lane impacts
- No Art, UI, or PM source docs were modified for this slice.
- Existing unrelated dirty UI visual-lock files were not touched.

## Next recommended task
Extract AI default build and production fallback entries out of `AIStartupSystem` into config authoring or a narrower AI startup helper system, then continue shrinking `GameBootstrap` scene/UI binding debt.
