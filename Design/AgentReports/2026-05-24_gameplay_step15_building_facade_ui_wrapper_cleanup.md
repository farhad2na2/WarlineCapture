Lane
Gameplay

Task
Step 15: remove unused public building UI/query compatibility wrappers from BuildingPlacementSystem after MenuView moved to BuildingUiQuerySystem and BuildingUiCommandSystem.

Files changed
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Added a contract rule that BuildingPlacementSystem must not expose public building UI read/query or menu/camp command compatibility wrappers once MenuView binds to narrow UI systems.
- Updated architecture tests to enforce the removed public facade surface and to expect ownership in BuildingUiQuerySystem, BuildingUiCommandSystem, BuildingPlacementQuerySystem, and FactionResourceSystem.

User-visible behavior
- No intended behavior change.
- RoadBuild/selection-facing BuildingPlacementSystem hooks are preserved.
- MenuView remains bound to BuildingUiQuerySystem for reads and BuildingUiCommandSystem for commands.

Validation run
- git diff --check
- Unity EditMode: GameplayArchitectureContractTests in /Users/farhad/Projects/WarlineCapture-CodexUnity1

Validation result
- git diff --check passed.
- GameplayArchitectureContractTests passed: 92/92.

Known gaps
- BuildingPlacementSystem is still 2,451 lines.
- Remaining public surface includes live external hooks for RoadBuildSystem, RTSSelectionSystem, gameplay spawning/production, resource totals, runtime building queries, and direct build placement actions.

Cross-lane impacts
- UI lane should not call BuildingPlacementSystem for building UI reads or camp/menu commands. Use BuildingUiQuerySystem and BuildingUiCommandSystem.
- No scene, art, or balance data changed.

Next recommended task
- Migrate RoadBuildSystem and RTSSelectionSystem off direct BuildingPlacementSystem calls by introducing narrow placement/session and building-selection command/query boundaries. After those callers move, the facade can shrink further or be renamed as a temporary compatibility shell.
