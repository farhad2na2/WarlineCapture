# WarlineCapture Handoff

Lane: Gameplay

Task: Move AICombatOrderSystem base-breach resolution off the managed BuildingPlacementSystem bridge.

Files changed:
- Assets/Game/Scripts/Components/CombatComponents.cs
- Assets/Game/Scripts/Systems/AICombatOrderSystem.cs
- Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeEntitySystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeOwnershipSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/AgentReports/2026-05-23_gameplay-ai-combat-runtime-building-info.md

Contracts touched:
- Added RuntimeBuildingCombatInfo as the ECS runtime building combat fact used by AI combat.
- BuildingRuntimeEntitySystem now attaches RuntimeBuildingCombatInfo to runtime building combat entities.
- BuildingRuntimeOwnershipSystem keeps RuntimeBuildingCombatInfo owner/building id in sync with runtime ownership assignment.
- AICombatOrderSystem now resolves base-breach target, gate/wall preference, open-breach detection, and breach approach cells from ECS runtime building combat data instead of BuildingPlacementRuntimeComponent or BuildingPlacementSystem.
- GameplayArchitectureContractTests now blocks AICombatOrderSystem from reintroducing the managed BuildingPlacementSystem bridge.

User-visible behavior:
- No intended gameplay behavior change.
- AI squads should still redirect to enemy gates/walls before attacking interior targets, then stop redirecting once a breach is opened.

Validation run:
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-ai-combat-runtime-info.log
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter BaseBreachValidationTests -logFile /private/tmp/warlinecapture-ai-combat-basebreach.log
- git diff --check
- rg -n "BuildingPlacementRuntimeComponent|BuildingPlacementSystem|buildingPlacement" Assets/Game/Scripts/Systems/AICombatOrderSystem.cs

Validation result:
- Passed. GameplayArchitectureContractTests reported 90 total, 90 passed, 0 failed in /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151775963264630.xml.
- Passed. BaseBreachValidationTests reported 15 total, 15 passed, 0 failed in /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151776508298200.xml.
- Passed. git diff --check reported no whitespace errors.
- Passed. AICombatOrderSystem has no remaining BuildingPlacementRuntimeComponent, BuildingPlacementSystem, or buildingPlacement references.

Known gaps:
- BuildingPlacementRuntimeComponent cannot be deleted yet because InitialUnitsSpawnSystem still uses it for initial resources, runtime building/wall spawning, prefab resolution, building counts, and air-platform spawn points.
- Runtime building combat metadata is ECS-owned now, but initial base spawning still needs its own ECS request/read-model migration before the managed bridge can be removed.

Cross-lane impacts:
- AI combat no longer depends on managed placement runtime state.
- Runtime building combat entities now carry explicit ECS metadata for owner faction, footprint, and wall/gate classification.

Next recommended task:
- Migrate InitialUnitsSpawnSystem away from BuildingPlacementRuntimeComponent by routing initial resources through FactionResourceSystem ownership and initial building/base creation through BuildingRuntimeSpawnRequest or a dedicated InitialBaseSpawnRequest boundary.
