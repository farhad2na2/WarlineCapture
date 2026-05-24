Lane
Gameplay

Task
Step 3 of BuildingPlacementSystem retirement: move runtime visual/resource tick and destroyed-building/combat sync tick callbacks out of BuildingPlacementSystem wrappers.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/buildingplacement_retirement_audit.md

Contracts touched
- BuildingPlacementSystem retirement audit now freezes the facade at 2334 lines.
- Architecture tests now require runtime visual/resource animation ticks to be wired from composition to BuildingRuntimeVisualSystem.
- Architecture tests now require destroyed-building/combat sync ticks to be wired from composition to BuildingCombatSystem.
- Architecture tests reject UpdateBuildingResourceVisuals, UpdateDestroyedBuildings, and SyncDestroyedRuntimeBuildingCombatEntities runtime tick wrappers returning to BuildingPlacementSystem.

User-visible behavior
No intended gameplay behavior change. Runtime resource animations and destroyed-building cleanup/combat sync still run in the same tick order.

Validation run
- git diff --check -- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/buildingplacement_retirement_audit.md
- wc -l Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- rg -n "\b(?:internal|private)\s+void\s+(UpdateBuildingResourceVisuals|UpdateDestroyedBuildings|SyncDestroyedRuntimeBuildingCombatEntities)\s*\(" Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- rg -n "RuntimeVisualSystem\.UpdateBuildingResourceVisuals|CombatSystem\.SyncDestroyedRuntimeBuildingCombatEntities|CombatSystem\.UpdateDestroyedBuildings" Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs
- Unity batchmode focused architecture validation attempt on /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - /private/tmp/warline-step33-visual-combat-tick-architecture.log

Validation result
- Passed: git diff whitespace check.
- Passed: BuildingPlacementSystem.cs reduced from 2348 to 2334 lines.
- Passed: removed runtime tick wrapper methods from BuildingPlacementSystem.
- Passed: BuildingGameplayCompositionSystem now wires runtime visual/resource ticks to BuildingRuntimeVisualSystem and destroyed-building/combat sync ticks to BuildingCombatSystem.
- Passed: Unity batchmode compile; no C# compiler errors in the focused validation log.
- Limited: Unity exited 0 but did not emit a fresh test-results XML, matching the runner behavior seen in prior steps.

Known gaps
- BuildingGameplayCompositionSystem still adapts BuildingPlacementSystem into the tick source.
- Barrier door ticks, marker-refresh ticks, pointer/selection click flow, diagnostics, and several context factories are still sourced from BuildingPlacementSystem.
- Existing editor validation tests still construct BuildingPlacementSystem directly.

Cross-lane impacts
- Other lanes should not add visual/combat tick wrapper methods back to BuildingPlacementSystem.

Next recommended task
Continue step 3 by moving marker-refresh and barrier-door tick callbacks to their owning systems from composition, then move pointer/selection click flow in a separate input-focused slice.
