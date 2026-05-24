# WarlineCapture Handoff

Lane: Gameplay

Task: Step 3 architecture migration: move CitizenPopulationSystem building read paths off BuildingPlacementSystem facade methods and onto BuildingRuntimeQuerySystem.

Files changed:
- Assets/Game/Scripts/Systems/CitizenPopulationSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-24_gameplay-citizen-building-query-boundary.md

Contracts touched:
- BuildingPlacementSystem now exposes an internal BuildingRuntimeQuerySystem composition seam and query context for managed systems still being migrated.
- CitizenPopulationSystem now reads building lists, building focus positions, destroyed state, refugee settings, and approach cells through BuildingRuntimeQuerySystem.
- GameplayArchitectureContractTests now blocks CitizenPopulationSystem from calling BuildingPlacementSystem facade read methods for those building read paths.
- gameplay_solid_ecs_contract.md now records that citizen population building read paths belong behind BuildingRuntimeQuerySystem.

User-visible behavior:
- No intended gameplay or visual behavior change.
- Citizen household assignment, refugee tent assignment, visibility, movement goals, danger fleeing, and displacement logic should produce the same results through the query boundary.

Validation run:
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-citizen-query-architecture.log
- git diff --check

Validation result:
- Passed. GameplayArchitectureContractTests reported 90 total, 90 passed, 0 failed in /Users/farhad/Projects/WarlineCapture-CodexUnity1/TestResults-639151836815947210.xml.
- Passed. git diff --check reported no whitespace errors.

Known gaps:
- CitizenPopulationSystem still keeps BuildingPlacementSystem as temporary compatibility debt for resource spending and configured unit prefab/entity resolution.
- The query context is still supplied by BuildingPlacementSystem while the runtime building registry and its query delegates are being retired from the facade.
- There are no dedicated CitizenPopulationSystem behavior tests in the repo; validation for this slice is compile plus architecture contract coverage.

Cross-lane impacts:
- Gameplay/architecture can continue retiring BuildingPlacementSystem facade reads without changing citizen behavior.
- Resource/economy work should migrate citizen refugee upkeep spending to FactionResourceSystem or a narrower resource request boundary in a separate slice.

Next recommended task:
- Step 4: migrate CitizenPopulationSystem resource spending and configured citizen prefab/entity resolution off BuildingPlacementSystem into the appropriate resource and prefab/definition boundaries.
