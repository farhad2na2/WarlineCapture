Lane
Gameplay

Task
Step 1 of BuildingPlacementSystem retirement: audit and freeze the remaining facade surface before continuing deletion work.

Files changed
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/Architecture/buildingplacement_retirement_audit.md

Contracts touched
- Gameplay SOLID/ECS architecture contract now requires the BuildingPlacementSystem retirement audit and states that facade line/member counts may only decrease.
- Added architecture tests that freeze allowed production facade references, production construction, test construction allowlist, line count, and public/internal member declaration count.

User-visible behavior
No gameplay behavior change. This is an architecture guardrail/documentation step only.

Validation run
- git diff --check -- Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/buildingplacement_retirement_audit.md Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- wc -l Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- rg -n "\bBuildingPlacementSystem\b" Assets/Game/Scripts -g '*.cs'
- rg -n "new\s+BuildingPlacementSystem\s*\(" Assets/Game/Scripts Assets/Tests/Editor -g '*.cs'
- Unity batchmode compile/test attempts on /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - /private/tmp/warline-step31-bps-surface-architecture.log
  - /private/tmp/warline-step31-bps-surface-architecture-rerun.log
  - /private/tmp/warline-step31-bps-surface-architecture-editmode.log
  - /private/tmp/warline-step31-bps-surface-architecture-automated.log

Validation result
- Passed: git diff whitespace check.
- Passed: BuildingPlacementSystem.cs remains 2348 lines.
- Passed: production facade references are isolated to BuildingPlacementSystem.cs, BuildingGameplayCompositionSystem.cs, and BuildingPlacementRuntimeTickContextSystem.cs.
- Passed: production construction remains isolated to BuildingGameplayCompositionSystem.cs.
- Unity batchmode completed script refresh/compile without C# compiler errors in the checked logs, but Unity did not emit the requested test-results XML for the new focused test run.

Known gaps
- Focused Unity Test Runner XML was not produced despite batchmode exiting 0; next implementation step should rerun `GameplayArchitectureContractTests` once the runner emits results again.
- The facade remains at 2348 lines with 135 public/internal declarations; this step freezes that debt, it does not reduce it.

Cross-lane impacts
- Other lanes should not add direct BuildingPlacementSystem production references or new editor tests that construct the facade.

Next recommended task
Step 2: replace BuildingPlacementRuntimeTickContextSystem.Create(BuildingPlacementSystem) with a narrow context source so runtime tick context assembly no longer depends on the facade.
