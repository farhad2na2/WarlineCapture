# WarlineCapture Gameplay Handoff

Lane: Gameplay

Task: Extract AI startup projection from `GameBootstrap` into `AIStartupSystem`.

Files changed:
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/Systems/AIStartupSystem.cs`
- `Assets/Game/Scripts/Systems/AIStartupSystem.cs.meta`
- `Assets/Tests/Editor/AIStartupSystemValidationTests.cs`
- `Assets/Tests/Editor/AIStartupSystemValidationTests.cs.meta`
- `Assets/Tests/Editor/Campaign/Chapter01M01PlayableRuntimeTests.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/gamebootstrap_responsibility_audit.md`

Contracts touched:
- `GameBootstrap` now delegates AI config validation, faction economy/control projection, AI build/production/squad/target priority setup, and fixed tactical generic-plan disabling to `AIStartupSystem`.
- `AIStartupSystem` has no static runtime helper methods; startup mutation remains instance-scoped.
- Architecture contract now states that AI startup config projection belongs to `AIStartupSystem`, not `GameBootstrap`.
- Architecture tests now reject reintroducing the migrated AI startup methods into `GameBootstrap`.

User-visible behavior:
- Intended no behavior change.
- AI startup still reads the same serialized `AIControllerConfig` list from the scene bootstrap.
- AI startup still creates/updates the same ECS faction economy, faction control, build plan, production plan, squad plan, target priority, and diagnostic event data.
- Fixed tactical missions still disable generic AI build, production, and squad plans.

Validation run:
- `git diff --check -- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Game/Scripts/Systems/AIStartupSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Assets/Tests/Editor/Campaign/Chapter01M01PlayableRuntimeTests.cs Assets/Tests/Editor/AIStartupSystemValidationTests.cs Assets/Tests/Editor/AIStartupSystemValidationTests.cs.meta Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/gamebootstrap_responsibility_audit.md`
- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `GameplayArchitectureContractTests`
- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `AIStartupSystemValidationTests`
- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `FixedTacticalMissionGuardrail_DisablesGenericAIPlansOnlyWhenActive`
- Unity EditMode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`: `AI`

Validation result:
- Diff check passed.
- `GameplayArchitectureContractTests`: passed 59/59.
- `AIStartupSystemValidationTests`: passed 2/2.
- `FixedTacticalMissionGuardrail_DisablesGenericAIPlansOnlyWhenActive`: passed 1/1.
- `AI`: passed 34/34.

Known gaps:
- `AIStartupSystem` still consumes managed `AIControllerConfig` objects from bootstrap. A later migration should bake or project those configs into ECS config components/buffers before startup.
- Default AI build/production fallback IDs still live in the startup system; the audit tracks moving them into config data later.
- Fixed tactical AI disabling still runs through AI startup for now; the audit tracks moving mission-specific policy into mission startup.

Cross-lane impacts:
- No UI, Art, or PM files changed by this slice.
- Existing unrelated UI target-lock files and reports were left untouched.

Next recommended task:
- Extract mission-specific startup and camera/framing policy from `GameBootstrap` into a mission startup system or installer, keeping bootstrap as composition only.
