Lane
Gameplay

Task
Step 7 of BuildingPlacementSystem retirement: move runtime building count/dictionary read access out of facade properties and route composition through RuntimeBuildingSystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/buildingplacement_retirement_audit.md
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Gameplay SOLID/ECS contract now states runtime building count/dictionary read access belongs in RuntimeBuildingSystem.
- BuildingPlacementSystem retirement audit now freezes the facade at 2279 lines and 128 public/internal facade declarations.
- Architecture tests now require BuildingGameplayCompositionSystem to use the RuntimeBuildingSystem registry boundary for count and dictionary reads.
- Architecture tests reject RuntimeBuildingCount, RuntimeBuildings, and _runtimeBuildings facade read surfaces returning to BuildingPlacementSystem.

User-visible behavior
No intended gameplay behavior change. Production ticks, diagnostics, and runtime boundary publication still read the same runtime building registry.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/buildingplacement_retirement_audit.md Design/Architecture/gameplay_solid_ecs_contract.md
- wc -l Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- rg -n "private\s+IReadOnlyDictionary<int, RuntimeBuildingData>|internal\s+int\s+RuntimeBuildingCount|internal\s+IReadOnlyDictionary<int, RuntimeBuildingData>\s+RuntimeBuildings|_runtimeBuildings" Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Unity batchmode focused architecture validation on /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - /private/tmp/warline-step37-runtime-registry-architecture.log

Validation result
- Passed: git diff whitespace check.
- Passed: BuildingPlacementSystem.cs reduced from 2281 to 2279 lines.
- Passed: public/internal facade declarations reduced from 129 to 128, excluding the class declaration.
- Passed: RuntimeBuildings and RuntimeBuildingCount facade properties removed.
- Passed: BuildingGameplayCompositionSystem now uses RuntimeBuildingSystem.Buildings and RuntimeBuildingSystem.Count through the registry boundary.
- Passed: Unity batchmode compile exited 0 with normal package resolution and no C# compiler errors.
- Limited: Unity did not emit a test-results XML for the focused test run.

Known gaps
- BuildingGameplayCompositionSystem still obtains RuntimeBuildingSystem through the temporary BuildingPlacementSystem facade.
- Many remaining BuildingPlacementSystem context factories still pass RuntimeBuildingSystem.Buildings internally until those factories migrate out.
- Definition/config initialization, spawn/production/resource context factories, lifecycle/session command wrappers, UI wrappers, and selection/interaction compatibility wrappers remain facade debt.

Cross-lane impacts
- Other lanes should not add RuntimeBuildings or RuntimeBuildingCount facade properties back to BuildingPlacementSystem.
- New runtime building count/dictionary consumers should depend on RuntimeBuildingSystem, BuildingRuntimeQuerySystem, or ECS boundary buffers instead of the facade.

Next recommended task
Step 8: move definition/config initialization and configured spawnable/unit registry read access out of the facade into managed composition and BuildingDefinitionSystem-facing context wiring.
