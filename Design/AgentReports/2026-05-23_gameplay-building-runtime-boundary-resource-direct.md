# WarlineCapture Handoff

Lane: Gameplay

Task: Move BuildingRuntimeBoundarySystem resource sell request completion and faction resource summary publication off BuildingPlacementSystem.

Files changed:
- Assets/Game/Scripts/Systems/BuildingRuntimeBoundarySystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/AgentReports/2026-05-23_gameplay-building-runtime-boundary-resource-direct.md

Contracts touched:
- BuildingRuntimeBoundarySystem no longer accepts or references BuildingPlacementSystem.
- Resource sell requests drain through FactionResourceSystem.DrainFactionResource.
- Faction resource summary publication reads through FactionResourceSystem.TryGetFactionResourceEconomy.
- Runtime building counts in boundary summaries read through BuildingRuntimeQuerySystem.
- GameplayArchitectureContractTests now blocks BuildingRuntimeBoundarySystem from depending on BuildingPlacementSystem for resource request completion or summary publication.

User-visible behavior:
- No intended gameplay behavior change.
- Resource sell requests and faction resource summaries keep the same behavior while routing through the resource/query ownership systems.

Validation run:
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-boundary-resource-direct.log
- git diff --check
- rg -n "BuildingPlacementSystem|buildingPlacement\.|SellFactionResources" Assets/Game/Scripts/Systems/BuildingRuntimeBoundarySystem.cs

Validation result:
- Passed. Unity EditMode GameplayArchitectureContractTests reported 89 total, 89 passed, 0 failed in /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151769422647530.xml.
- Passed. git diff --check reported no whitespace errors.
- Passed. BuildingRuntimeBoundarySystem has no remaining direct BuildingPlacementSystem, buildingPlacement, or SellFactionResources references.

Known gaps:
- BuildingPlacementSystem still keeps compatibility facade methods such as TryGetFactionResourceEconomy and SellFactionResources for older callers and UI-facing paths.
- BuildingRuntimeBoundarySystem remains a broad boundary orchestrator; future work can split resource, production, and spawn request processors if the file starts growing again.

Cross-lane impacts:
- AI economy/resource sell boundary no longer depends on placement facade internals.
- Faction resource ownership is more explicit and easier to validate.

Next recommended task:
- Review remaining BuildingPlacementSystem facade methods and migrate external callers one domain slice at a time, starting with the resource facade methods if no UI path still needs them.
