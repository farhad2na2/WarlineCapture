# WarlineCapture Handoff

Lane: Gameplay

Task: Step 9 - move runtime spawn/creation/ownership context construction out of `BuildingPlacementSystem`.

Files changed:
- `Assets/Game/Scripts/Systems/BuildingRuntimeContextSystem.cs`
- `Assets/Game/Scripts/Systems/BuildingRuntimeContextSystem.cs.meta`
- `Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/buildingplacement_retirement_audit.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

Contracts touched:
- Runtime spawn, runtime creation, runtime ownership, and runtime city-spawn context construction now belongs to `BuildingRuntimeContextSystem`.
- `BuildingPlacementSystem` no longer directly constructs `BuildingRuntimeSpawnSystem.Context`, `BuildingRuntimeCreationSystem.Context`, `BuildingRuntimeOwnershipSystem.Context`, or `BuildingRuntimeCitySpawnSystem.Context`.
- `BuildingPlacementSystem` exposes only a temporary `BuildingRuntimeContextSystem.Source` bundle while the facade is retired.
- Retirement audit maximum is now 2197 lines and 128 public/internal declarations.

User-visible behavior:
- No intended gameplay or UI behavior change.
- Runtime building spawn, wall spawn, ownership assignment, and city-spawn behavior still route through the same runtime systems.

Validation run:
- `git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs Assets/Game/Scripts/Systems/BuildingRuntimeContextSystem.cs Assets/Game/Scripts/Systems/BuildingRuntimeContextSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/buildingplacement_retirement_audit.md Design/Architecture/gameplay_solid_ecs_contract.md`
- Unity batchmode in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`:
  - `Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warline-step39-runtime-context-architecture.log -testResults /private/tmp/warline-step39-runtime-context-architecture.xml`

Validation result:
- `git diff --check` passed.
- Unity batchmode exited 0 and compiled without `error CS`, `Scripts have compiler errors`, or `Compilation failed`.
- Unity did not emit `/private/tmp/warline-step39-runtime-context-architecture.xml`; this is recorded as compile/static-architecture validation, not a confirmed NUnit result.
- Existing unrelated editor obsolete API warnings remain.

Known gaps:
- `BuildingPlacementSystem` still owns runtime/manual building and wall spawn wrapper methods.
- `BuildingPlacementSystem` still owns production/resource/hauler context factories, active placement command wrappers, placement context wiring, UI compatibility wrappers, and selection/interaction compatibility wrappers.
- `BuildingGameplayCompositionSystem` still constructs `new BuildingPlacementSystem()` until the final facade replacement stage.

Cross-lane impacts:
- None expected for UI or art.
- Validation clone was updated only for focused compile/architecture validation.

Next recommended task:
- Step 10: move production request, production update, production transport, and hauler context factories out of `BuildingPlacementSystem`.
