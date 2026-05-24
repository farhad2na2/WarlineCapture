Lane
Gameplay

Task
Step 6 of BuildingPlacementSystem retirement: extract building runtime tick diagnostics out of the BuildingPlacementSystem tick-source surface.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickDiagnosticsSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickDiagnosticsSystem.cs.meta
- Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/buildingplacement_retirement_audit.md
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Gameplay SOLID/ECS contract now assigns building runtime tick diagnostics threshold, enablement, timing normalization, and log formatting to BuildingPlacementRuntimeTickDiagnosticsSystem.
- BuildingPlacementSystem retirement audit now freezes the facade at 2281 lines and 129 public/internal facade declarations.
- Architecture tests now require BuildingPlacementRuntimeTickDiagnosticsSystem to own BuildingPlacementDiag formatting and slow-frame threshold policy.
- Architecture tests reject diagnostics flags, threshold accessors, and BuildingPlacementDiag formatting returning to BuildingPlacementSystem.

User-visible behavior
No intended gameplay behavior change. Building placement runtime tick diagnostics remain disabled by default and keep the same threshold and log shape when enabled in code.

Validation run
- git diff --check -- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickDiagnosticsSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickDiagnosticsSystem.cs.meta Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/buildingplacement_retirement_audit.md Design/Architecture/gameplay_solid_ecs_contract.md
- wc -l Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- rg -n "EnableBuildingPlacementDiagnostics|DiagnosticsEnabled|DiagnosticsFreezeLogThresholdSeconds|\[BuildingPlacementDiag\]|FreezeLogThresholdSeconds" Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickDiagnosticsSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementRuntimeTickContextSystem.cs Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/buildingplacement_retirement_audit.md
- Unity batchmode focused architecture validation on /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - /private/tmp/warline-step36-runtime-tick-diagnostics-architecture.log

Validation result
- Passed: git diff whitespace check.
- Passed: BuildingPlacementSystem.cs reduced from 2285 to 2281 lines.
- Passed: public/internal facade declarations reduced from 130 to 129, excluding the class declaration.
- Passed: BuildingPlacementSystem no longer exposes building tick diagnostics enabled/threshold properties and no longer owns BuildingPlacementDiag formatting.
- Passed: Unity batchmode compile exited 0 with normal package resolution and no C# compiler errors.
- Limited: Unity did not emit a test-results XML for the focused test run.

Known gaps
- BuildingGameplayCompositionSystem still constructs and adapts BuildingPlacementSystem.
- Runtime building count still comes from BuildingPlacementSystem until runtime registry ownership/read access moves out.
- Runtime registry access, definition/config initialization, spawn/production/resource context factories, lifecycle/session command wrappers, UI wrappers, and selection/interaction compatibility wrappers are still remaining facade debt.

Cross-lane impacts
- Other lanes should not add building runtime tick diagnostics flags, thresholds, or log formatting back to BuildingPlacementSystem.
- Performance diagnostics changes should route building placement runtime tick-specific logs through BuildingPlacementRuntimeTickDiagnosticsSystem.

Next recommended task
Step 7: move runtime registry ownership/read access out of the facade, starting with a narrow runtime registry/read model boundary so runtime building count and dictionary access no longer come from BuildingPlacementSystem.
