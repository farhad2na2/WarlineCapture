# WarlineCapture Handoff

Lane: Gameplay

Task: Step 4 architecture migration: move CitizenPopulationSystem resource spending and configured citizen prefab/entity resolution off BuildingPlacementSystem facade methods.

Files changed:
- Assets/Game/Scripts/Systems/CitizenPopulationSystem.cs
- Assets/Game/Scripts/Systems/CitizenResourceSystem.cs
- Assets/Game/Scripts/Systems/CitizenResourceSystem.cs.meta
- Assets/Game/Scripts/Systems/CitizenPrefabSystem.cs
- Assets/Game/Scripts/Systems/CitizenPrefabSystem.cs.meta
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-24_gameplay-citizen-resource-prefab-boundary.md

Contracts touched:
- Added CitizenResourceSystem for citizen upkeep dollar spending.
- Added CitizenPrefabSystem for configured citizen unit prefab lookup and prefab entity resolution.
- BuildingPlacementSystem now exposes internal composition contexts for citizen resource and prefab boundaries instead of CitizenPopulationSystem calling facade methods directly.
- GameplayArchitectureContractTests now blocks CitizenPopulationSystem from calling BuildingPlacementSystem.TrySpendDollars, TryResolveConfiguredUnitPrefabEntity, and TryResolveConfiguredUnitSpawnPrefab.
- gameplay_solid_ecs_contract.md now records citizen upkeep and citizen prefab/entity ownership.

User-visible behavior:
- No intended gameplay or visual behavior change.
- Refugee upkeep still spends from the same dollar pool.
- Visible citizen spawning still resolves the same configured civilian unit prefabs and ECS prefab entities.

Validation run:
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-citizen-step4-architecture.log
- git diff --check

Validation result:
- Passed. GameplayArchitectureContractTests reported 90 total, 90 passed, 0 failed in /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639152076787624190.xml.
- Passed. git diff --check reported no whitespace errors.

Known gaps:
- BuildingPlacementSystem still supplies the temporary composition contexts because it still owns the dollar pool, building definition system, spawn prefab system, and entity queries.
- There are no dedicated CitizenPopulationSystem behavior tests in the repo; validation for this slice is compile plus architecture contract coverage.

Cross-lane impacts:
- Gameplay/architecture can continue retiring BuildingPlacementSystem by moving the backing dollar pool, definition lookup, and spawn prefab query composition to non-facade runtime boundaries.
- Economy/resource work can later decide whether citizen upkeep should remain a dollar spend or become a formal faction economy request.

Next recommended task:
- Step 5: move the backing dollar pool and unit prefab registry composition out of BuildingPlacementSystem, then update CitizenResourceSystem/CitizenPrefabSystem contexts to come from that non-facade boundary.
