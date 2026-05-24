Lane
Gameplay

Task
Step 2 of BuildingPlacementSystem retirement: remove BuildingPlacementRuntimeTickContextSystem's direct dependency on the BuildingPlacementSystem facade.

Files changed
- Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextSystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/Architecture/buildingplacement_retirement_audit.md

Contracts touched
- BuildingPlacementRuntimeTickContextSystem now consumes a narrow Source struct instead of BuildingPlacementSystem.
- Architecture contract and retirement audit now allow production BuildingPlacementSystem references only in BuildingPlacementSystem.cs and BuildingGameplayCompositionSystem.cs.
- Architecture tests now reject any BuildingPlacementSystem reference inside BuildingPlacementRuntimeTickContextSystem.

User-visible behavior
No intended gameplay behavior change. Runtime tick behavior still flows through the same production/resource/boundary/input callbacks; only the dependency shape changed.

Validation run
- git diff --check -- Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextSystem.cs Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/buildingplacement_retirement_audit.md
- rg -n "\bBuildingPlacementSystem\b" Assets/Game/Scripts -g '*.cs'
- rg -n "BuildingPlacementSystem" Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextSystem.cs
- wc -l Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Unity batchmode focused architecture validation attempt on /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - /private/tmp/warline-step32-runtime-tick-source-architecture-rerun.log

Validation result
- Passed: git diff whitespace check.
- Passed: BuildingPlacementRuntimeTickContextSystem has zero BuildingPlacementSystem references.
- Passed: production facade references are now isolated to BuildingPlacementSystem.cs and BuildingGameplayCompositionSystem.cs.
- Passed: BuildingPlacementSystem.cs stayed at the frozen 2348 lines.
- Passed: Unity batchmode compile after the fix; no C# compiler errors in the rerun log.
- Limited: Unity exited 0 but did not emit a fresh test-results XML for the focused Test Runner pass.

Known gaps
- BuildingGameplayCompositionSystem still adapts BuildingPlacementSystem into the runtime tick Source.
- BuildingPlacementSystem still exposes the callback/context surface used by composition.
- Existing editor validation tests still construct BuildingPlacementSystem directly.

Cross-lane impacts
- Other lanes must not add BuildingPlacementSystem references outside the two allowed production files.
- UI/menu code should continue using BuildingUiCommandSystem, BuildingUiQuerySystem, and BuildingPlacementInteractionSystem rather than the facade.

Next recommended task
Step 3: move remaining tick callbacks by domain, starting with runtime visual/resource marker updates and destroyed-building/combat sync, so BuildingGameplayCompositionSystem stops sourcing those callbacks from BuildingPlacementSystem.
