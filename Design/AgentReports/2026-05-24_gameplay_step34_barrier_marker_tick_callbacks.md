Lane
Gameplay

Task
Step 4 of BuildingPlacementSystem retirement: move road barrier door and marker-refresh tick callbacks out of BuildingPlacementSystem wrappers.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/buildingplacement_retirement_audit.md

Contracts touched
- BuildingPlacementSystem retirement audit now freezes the facade at 2322 lines.
- Architecture tests now require marker-refresh ticks to be wired from composition to BuildingPlacementRedirectSystem.
- Architecture tests now require road barrier door ticks to be wired from composition to BuildingBarrierSystem.
- Architecture tests reject FlushPendingMarkerRefresh and UpdateRoadBarrierDoors runtime tick wrappers returning to BuildingPlacementSystem.

User-visible behavior
No intended gameplay behavior change. Marker visibility refreshes and road barrier door updates still run from the same managed runtime tick path.

Validation run
- git diff --check -- Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystem.cs Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/buildingplacement_retirement_audit.md
- wc -l Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- rg -n "\b(?:internal|private)\s+void\s+(UpdateRoadBarrierDoors|FlushPendingMarkerRefresh)\s*\(" Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- rg -n "\bBuildingPlacementSystem\b" Assets/Game/Scripts -g '*.cs'
- Unity batchmode focused architecture validation on /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - /private/tmp/warline-step34-barrier-marker-tick-architecture-final.log

Validation result
- Passed: git diff whitespace check.
- Passed: BuildingPlacementSystem.cs reduced from 2334 to 2322 lines.
- Passed: removed marker-refresh and road barrier door runtime tick wrapper methods from BuildingPlacementSystem.
- Passed: production BuildingPlacementSystem references remain limited to BuildingPlacementSystem.cs and BuildingGameplayCompositionSystem.cs.
- Passed: Unity batchmode compile exited 0 with normal package resolution and no C# compiler errors.
- Limited: Unity did not emit a test-results XML for the focused test run. The invalid -noUpm retry was rejected as a signal because it disables package resolution and produces project-wide missing package errors.

Known gaps
- BuildingGameplayCompositionSystem still adapts BuildingPlacementSystem into the tick source.
- Pointer/selection click flow, diagnostics, runtime registry access, and several context factories are still sourced from BuildingPlacementSystem.
- Existing editor validation tests still construct BuildingPlacementSystem directly.

Cross-lane impacts
- Other lanes should not add marker-refresh or barrier-door runtime tick wrappers back to BuildingPlacementSystem.
- Other lanes should keep production BuildingPlacementSystem references inside the allowed composition boundary until the facade is retired.

Next recommended task
Step 5: extract pointer/selection click flow from the runtime tick source into a narrow input/selection tick boundary, then update the architecture contract so BuildingPlacementSystem no longer exposes those tick callbacks.
