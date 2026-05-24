Lane
Gameplay

Task
Step 14: split remaining building UI read/query surface out of BuildingUiCommandSystem and keep command actions in BuildingUiCommandSystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingUiQuerySystem.cs
- Assets/Game/Scripts/Systems/BuildingUiCommandSystem.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/UI/MenuView.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md

Contracts touched
- Updated the gameplay SOLID/ECS architecture contract so BuildingUiCommandSystem owns menu/camp command actions, camp request validation, modal actions, and placement/session UI commands.
- Updated the contract so BuildingUiQuerySystem owns friendly pending-production reads, produced-unit reads, selected-building display/health/preview reads, minimap building flags, visible-building checks, live-unit preview lookup, and UI progress shaping.
- Strengthened GameplayArchitectureContractTests so BuildingUiCommandSystem cannot regain read-model query delegates.

User-visible behavior
- No intended behavior change.
- MenuView still renders selected-building, minimap, request countdown, health, portrait, and visible-selection UI, but it now routes read paths through BuildingUiQuerySystem and commands through BuildingUiCommandSystem.

Validation run
- git diff --check
- Unity EditMode: GameplayArchitectureContractTests in /Users/farhad/Projects/WarlineCapture-CodexUnity1
- Unity EditMode: BuildingUiQuerySystemTests in /Users/farhad/Projects/WarlineCapture-CodexUnity1

Validation result
- git diff --check passed.
- GameplayArchitectureContractTests passed: 92/92.
- BuildingUiQuerySystemTests passed: 3/3.

Known gaps
- BuildingPlacementSystem remains at 2,579 lines; this step changed ownership boundaries rather than reducing facade line count.
- BuildingPlacementSystem still exposes compatibility UI query wrappers for older callers. MenuView is no longer using those wrappers, so the next cleanup can remove or internalize unused wrappers after reference audit.

Cross-lane impacts
- UI lane should treat BuildingUiQuerySystem as the building UI read boundary and BuildingUiCommandSystem as the building UI command boundary.
- No art, scene, or gameplay balance data changed.

Next recommended task
- Audit remaining BuildingPlacementSystem public UI/query compatibility wrappers and remove the wrappers that no non-test caller uses. Then move any still-needed wrappers into narrower query/command systems before renaming BuildingPlacementSystem to a temporary facade.
