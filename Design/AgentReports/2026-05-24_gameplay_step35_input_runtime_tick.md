Lane
Gameplay

Task
Step 5 of BuildingPlacementSystem retirement: extract placement pointer and building-selection click frame flow from the runtime tick source.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementInputRuntimeTickSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementInputRuntimeTickSystem.cs.meta
- Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/buildingplacement_retirement_audit.md
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Gameplay SOLID/ECS contract now assigns placement pointer/click frame flow to BuildingPlacementInputRuntimeTickSystem.
- BuildingPlacementSystem retirement audit now freezes the facade at 2285 lines and 130 public/internal facade declarations.
- Architecture tests now require pointer polling, active-placement pointer updates, gameplay UI guards, unit command UI suppression, and building selection click routing to live in BuildingPlacementInputRuntimeTickSystem.
- Architecture tests reject the removed pointer/click runtime wrapper methods returning to BuildingPlacementSystem.

User-visible behavior
No intended gameplay behavior change. Active placement dragging, outline hiding while not in build mode, gameplay UI click guards, unit-command click suppression, and building selection click routing keep the same runtime order.

Validation run
- git diff --check -- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementInputRuntimeTickSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementInputRuntimeTickSystem.cs.meta Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/buildingplacement_retirement_audit.md Design/Architecture/gameplay_solid_ecs_contract.md
- wc -l Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- rg -n "\binternal\s+(?:void|bool)\s+(UpdateActivePlacementPointer|HidePlacementOutline|ShouldIgnoreBuildingSelectionThisFrame|SuppressNextWorldClick|HandleBuildingSelectionClick|IsPointerOverAnyGameplayUi|IsPointerOverUnitCommandUi)\s*\(" Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- rg -n "GamePointerInput.TryGetPrimaryPointer|HandleBuildingSelectionClick|ShouldIgnoreBuildingSelectionThisFrame|IsPointerOverAnyGameplayUi|IsPointerOverUnitCommandUi|SuppressNextWorldClick" Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementInputRuntimeTickSystem.cs
- Unity batchmode focused architecture validation on /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - /private/tmp/warline-step35-input-runtime-tick-architecture.log

Validation result
- Passed: git diff whitespace check.
- Passed: BuildingPlacementSystem.cs reduced from 2322 to 2285 lines.
- Passed: public/internal facade declarations reduced from 135 to 130, excluding the class declaration.
- Passed: removed pointer/click runtime wrapper methods from BuildingPlacementSystem.
- Passed: pointer/click frame flow moved out of BuildingPlacementRuntimeTickSystem and into BuildingPlacementInputRuntimeTickSystem.
- Passed: Unity batchmode compile exited 0 with normal package resolution and no C# compiler errors.
- Limited: Unity did not emit a test-results XML for the focused test run.

Known gaps
- BuildingGameplayCompositionSystem still constructs and adapts BuildingPlacementSystem.
- Runtime diagnostics still flow through the runtime tick source.
- Runtime registry access, definition/config initialization, spawn/production/resource context factories, lifecycle/session command wrappers, UI wrappers, and selection/interaction compatibility wrappers are still remaining facade debt.

Cross-lane impacts
- Other lanes should not add pointer/click runtime wrapper methods back to BuildingPlacementSystem.
- UI work that changes MainMenuPlayUI pointer guards should preserve the runtime getter path; the main menu is bound after building composition initialization.

Next recommended task
Step 6: extract runtime diagnostics wiring from the BuildingPlacementSystem tick source, then continue with runtime registry ownership/read access.
