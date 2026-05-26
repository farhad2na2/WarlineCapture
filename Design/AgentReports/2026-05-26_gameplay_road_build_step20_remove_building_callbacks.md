Lane: Gameplay

Task: RoadBuildSystem refactor step 20 - remove road-to-building compatibility callbacks.

Files changed:
- Assets/Game/Scripts/UI/RuntimeBuildingEntityLink.cs
- Assets/Game/Scripts/Systems/RoadBuildSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/road_build_system_refactor_roadmap.md
- Design/AgentReports/2026-05-26_gameplay_road_build_step20_remove_building_callbacks.md

Contracts touched:
- Added RoadBuild architecture contract coverage for step 20.
- RuntimeBuildingEntityLink must call BuildingPlacementInteractionSystem only.
- RoadBuildSystem must not expose or implement runtime building destruction callbacks.

User-visible behavior:
- No intended gameplay behavior change.
- Destroyed runtime buildings still route cleanup through the building interaction/combat boundary.

Validation run:
- git diff --check for RoadBuild step 20 files.
- Local forbidden-token scan for RoadBuildSystem fallback references in RuntimeBuildingEntityLink.
- Local forbidden-token scan for runtime building destruction callback ownership in RoadBuildSystem.
- Unity batch validation in WarlineCapture-CodexUnity1:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation -logFile /private/tmp/warlinecapture-roadbuild-step20-architecture.log

Validation result:
- Passed.
- Unity log: [RoadBuildArchitectureValidation] result=Passed methods=22
- RoadBuildSystem line count after step 20: 1308.

Known gaps:
- RoadBuildSystem still exists as a broad temporary shell.
- Runtime-city road generation APIs still depend on RoadBuildSystem.
- Building gameplay road footprint queries still need migration to RoadFootprintQuerySystem in later roadmap steps.

Cross-lane impacts:
- Building lane remains owner of runtime building destruction cleanup through BuildingPlacementInteractionSystem and BuildingCombatSystem.
- RuntimeBuildingEntityLink no longer has a road-controller fallback overload.

Next recommended task:
- Step 21: Create RoadRuntimeGenerationSystem for runtime-city-facing road generation commands.
