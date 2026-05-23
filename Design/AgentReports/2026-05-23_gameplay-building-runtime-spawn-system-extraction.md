Lane
Gameplay

Task
Extract runtime/manual building spawn orchestration from BuildingPlacementSystem into BuildingRuntimeSpawnSystem.

Files changed
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeSpawnSystem.cs
- Assets/Game/Scripts/Systems/BuildingRuntimeSpawnSystem.cs.meta
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-23_gameplay-building-runtime-spawn-system-extraction.md

Contracts touched
- Updated Design/Architecture/gameplay_solid_ecs_contract.md so runtime/manual building spawn orchestration, initial test roster spawn requests, runtime wall-run/segment spawn orchestration, runtime footprint queries, initial building origin search, and building-definition footprint cloning belong in BuildingRuntimeSpawnSystem.
- Added GameplayArchitectureContractTests coverage requiring BuildingPlacementSystem to delegate this runtime/manual spawn slice and preventing wall-run construction, wall spawn validation orchestration, runtime definition creation orchestration, initial origin search, and runtime fallback footprint policy from returning to the facade.

User-visible behavior
- No intended gameplay behavior change.
- Runtime/manual building spawn, runtime wall segment/run spawn, initial test roster spawn, and runtime footprint query APIs remain on BuildingPlacementSystem as stable facade methods.

Validation run
- git diff --check -- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs Assets/Game/Scripts/Systems/BuildingRuntimeSpawnSystem.cs Assets/Game/Scripts/Systems/BuildingRuntimeSpawnSystem.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md
- Unity 6000.4.0f1 batchmode EditMode tests in /Users/farhad/Projects/WarlineCapture-CodexUnity1:
  - testPlatform EditMode
  - testFilter GameplayArchitectureContractTests
  - logFile /private/tmp/warlinecapture-runtime-spawn-editmode.log

Validation result
- Focused diff check passed.
- GameplayArchitectureContractTests passed 82/82.
- Unity log included an initial licensing handshake warning, then completed with exit code 0.

Known gaps
- BuildingPlacementSystem is still large at 4301 lines.
- BuildingPlacementSystem still keeps public facade methods and a small CloneDefinitionWithFootprint wrapper for compatibility with placement commit delegates.
- Runtime spawn delegates still route through BuildingPlacementSystem for grid lookup, placement validation, visual creation, runtime registration, and owner-faction assignment until those remaining boundaries are migrated further.

Cross-lane impacts
- No scene, art, prefab, UI canvas, or ECS component schema changes.
- Architecture test expectations changed for runtime/manual building spawn ownership.

Next recommended task
Extract remaining runtime owner-faction assignment and gate friendly-pass faction update out of BuildingPlacementSystem into a smaller runtime ownership/faction system, or continue shrinking placement-validity/query glue if owner assignment should wait.
