# WarlineCapture Handoff

Lane: Gameplay

Task: Move BuildingRuntimeBoundarySystem configured unit read-model and production summary publication off BuildingPlacementSystem.

Files changed:
- Assets/Game/Scripts/Systems/BuildingDefinitionSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeBoundarySystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/AgentReports/2026-05-23_gameplay-building-runtime-boundary-unit-readmodel-direct.md

Contracts touched:
- BuildingDefinitionSystem now keeps a unique configured unit prefab list from unit spawn prefab authoring.
- BuildingDefinitionSystem now exposes TryGetConfiguredUnitReadModel for unit display, price, requestability, and vehicle classification.
- BuildingRuntimeBoundarySystem now publishes BuildingConfiguredUnitReadModel through BuildingDefinitionSystem.
- BuildingRuntimeBoundarySystem now publishes BuildingRuntimeUnitProductionSummary through BuildingRuntimeQuerySystem.
- GameplayArchitectureContractTests now blocks BuildingRuntimeBoundarySystem from returning to BuildingPlacementSystem unit config and production count facade methods.

User-visible behavior:
- No intended gameplay behavior change.
- AI production read models and production summaries still publish the same unit ids, display names, prices, requestability, produced counts, and queued counts.

Validation run:
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-boundary-unit-readmodel-direct.log

Validation result:
- Passed. TestResults-639151766329772030.xml reports GameplayArchitectureContractTests 89/89 passed, 0 failed.

Known gaps:
- BuildingRuntimeBoundarySystem still calls BuildingPlacementSystem for resource sell requests.
- BuildingRuntimeBoundarySystem still calls BuildingPlacementSystem for faction resource economy snapshots in BuildingRuntimeFactionSummary.
- BuildingPlacementSystem still keeps compatibility facade methods for configured unit lookup and production count callers.

Cross-lane impacts:
- AI production now consumes unit configuration and production summaries whose publication no longer depends on BuildingPlacementSystem facade methods.
- Unit spawn prefab authoring must continue to flow through BuildingDefinitionSystem.RebuildSpawnablesLookup.

Next recommended task:
- Move BuildingRuntimeBoundarySystem resource sell request completion and faction resource summary publication off BuildingPlacementSystem by routing through FactionResourceSystem ownership.
