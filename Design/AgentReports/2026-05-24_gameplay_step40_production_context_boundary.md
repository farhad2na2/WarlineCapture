# WarlineCapture Handoff

Lane: Gameplay

Task: Step 10 - move production/update/transport/resource-hauler context construction out of `BuildingPlacementSystem`.

Files changed:
- `Assets/Game/Scripts/Systems/BuildingProductionContextSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingProductionContextSystem.cs.meta`
- `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/buildingplacement_retirement_audit.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

Contracts touched:
- Production request, production queue, production update, production transport, production transport bridge, and resource-hauler bridge context construction now belongs to `BuildingProductionContextSystem`.
- `BuildingPlacementSystem` no longer directly constructs those production/hauler context structs.
- `BuildingPlacementSystem` exposes only a temporary `BuildingProductionContextSystem.Source` bundle while the facade is retired.
- Retirement audit maximum is now 2153 lines and 127 public/internal declarations.

User-visible behavior:
- No intended gameplay or UI behavior change.
- Unit production, transport delivery, and resource-hauler behavior still route through the same owning production/resource systems.

Validation run:
- `git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs Assets/Game/Scripts/Systems/BuildingProductionContextSystem.cs Assets/Game/Scripts/Systems/BuildingProductionContextSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/buildingplacement_retirement_audit.md Design/Architecture/gameplay_solid_ecs_contract.md`
- Unity batchmode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
  - First run: `/private/tmp/warline-step40-production-context-architecture.log`
  - Rerun after explicit delegate wrappers: `/private/tmp/warline-step40-production-context-architecture-rerun.log`

Validation result:
- `git diff --check` passed.
- First Unity run caught delegate-type conversion errors in `BuildingProductionContextSystem`; fixed by explicitly wrapping delegates where context delegate types differ.
- Unity rerun exited 0 and compiled without `error CS`, `Scripts have compiler errors`, or `Compilation failed`.
- Unity did not emit `/private/tmp/warline-step40-production-context-architecture-rerun.xml`; this is recorded as compile/static-architecture validation, not a confirmed NUnit result.
- Existing unrelated editor obsolete API warnings remain.

Known gaps:
- `BuildingPlacementSystem` still exposes temporary production and runtime context source bundles.
- Runtime resource/unit prefab context wiring, active placement/session command wrappers, runtime/manual spawn wrappers, placement wiring, UI wrappers, and selection/interaction wrappers remain.
- `BuildingGameplayCompositionSystem` still constructs `new BuildingPlacementSystem()` until the final facade replacement stage.

Cross-lane impacts:
- None expected for UI or art.
- Validation clone was updated only for focused compile/architecture validation.

Next recommended task:
- Step 11: move runtime resource and unit prefab context wiring out of `BuildingPlacementSystem`.
