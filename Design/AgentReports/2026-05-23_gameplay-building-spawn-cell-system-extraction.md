Lane
Gameplay

Task
Extract remaining spawn-cell perimeter helper algorithms from BuildingPlacementSystem into BuildingSpawnCellSystem.

Files changed
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/Systems/BuildingSpawnCellSystem.cs
- Assets/Game/Scripts/Systems/BuildingSpawnCellSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-23_gameplay-building-spawn-cell-system-extraction.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md so spawn-cell perimeter search helpers belong in BuildingSpawnCellSystem.
- Updated GameplayArchitectureContractTests to require BuildingSpawnCellSystem ownership and to prevent FindSpawnCellAdjacentToBuilding, TryReservePerimeterCell, and TryAddPerimeterCandidate from returning to BuildingPlacementSystem.

User-visible behavior
- No intended gameplay behavior change.
- The extracted methods were not part of the current live spawn path in BuildingPlacementSystem; this removes facade-owned spawn-cell helper debt without changing runtime spawning.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingSpawnCellSystem.cs Assets/Game/Scripts/Systems/BuildingSpawnCellSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 batchmode EditMode tests in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - testPlatform EditMode
  - testFilter GameplayArchitectureContractTests
  - logFile /private/tmp/warlinecapture-spawn-cell-editmode.log

Validation result
- Focused diff check passed.
- GameplayArchitectureContractTests passed 85/85.
- Unity completed with exit code 0.

Known gaps
- BuildingPlacementSystem is still a compatibility facade at 3029 lines.
- BuildingSpawnCellSystem currently owns the extracted perimeter helper algorithms; the live BuildingSpawnSystem path still uses its stricter spawn-cell methods.
- A later pass can either integrate BuildingSpawnCellSystem into the live spawn path or remove it if no future caller needs the legacy perimeter helper behavior.

Cross-lane impacts
- No scene, art, prefab, UI canvas, or ECS component schema changes.
- Architecture ownership changed for spawn-cell perimeter helper algorithms.

Next recommended task
Review remaining BuildingPlacementSystem wrappers and context creation to identify whether the facade can be split into a pure API facade plus smaller composition/context systems.
