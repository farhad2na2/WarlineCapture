Lane
Gameplay

Task
RoadBuildSystem refactor roadmap step 17: move soldier-base and building command compatibility paths to the building gameplay interaction boundary.

Files changed
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementInteractionSystem.cs
- Assets/Game/Scripts/Systems/BuildingPlacementInteractionContextSystem.cs
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md

Contracts touched
- RoadBuild architecture validation now includes RoadBuildBuildingCommandsMustDelegateToBuildingInteraction.
- BuildingPlacementInteractionSystem and BuildingPlacementInteractionContextSystem now expose ExitBuildMode.
- RoadBuild roadmap step 17 marked Complete.

User-visible behavior
- No intended user-visible behavior change.
- Soldier-base placement, building placement confirm/cancel, selected-building unit creation, building deletion, and building selection clear now route through BuildingPlacementInteractionSystem when called from the road shell.

Validation run
- git diff --check -- Assets/Game/Scripts/Systems/RoadBuildSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementInteractionSystem.cs Assets/Game/Scripts/Systems/BuildingPlacementInteractionContextSystem.cs Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/road_build_system_refactor_roadmap.md
- rg old RoadBuild building command fallback tokens in Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step17-architecture.log

Validation result
- Passed.
- Unity RoadBuild architecture validation: [RoadBuildArchitectureValidation] result=Passed methods=19.
- RoadBuildSystem.cs is 1495 lines.

Known gaps
- RoadBuildSystem still contains legacy building storage and helper methods; step 17 only moved command ownership. Step 18 is the storage extraction.
- RuntimeBuildingEntityLink still references RoadBuildSystem; that is tracked for the later road-to-building compatibility callback step.

Cross-lane impacts
- Building gameplay interaction context gained ExitBuildMode. Existing callers continue using the same building gameplay implementation.

Next recommended task
RoadBuild roadmap step 18: move legacy runtime building storage out of road build.
