Lane
Gameplay

Task
RoadBuildSystem refactor step 29: remove temporary architecture allowances after deleting the broad RoadBuildSystem shell.

Files changed
- Assets/Game/Scripts/Systems/RoadBuildCompositionSystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/Architecture/road_build_system_refactor_roadmap.md
- Design/AgentReports/2026-05-26_gameplay_road_build_step29_remove_allowances.md

Contracts touched
- Updated the gameplay SOLID/ECS contract so RoadBuildSystem.cs must not exist.
- Updated the road-build roadmap so step 29 is complete.
- Added architecture validation that rejects restoring RoadBuildSystem.cs, RoadBuildSystem.cs.meta, exact production RoadBuildSystem type references, or RoadBuildRuntimeStateSystem construction outside RoadBuildCompositionSystem.
- Renamed the temporary composition result field from RoadBuild to RoadState so the composition boundary no longer exposes a broad facade-style field name.

User-visible behavior
No intended runtime behavior change. This is an architecture guard and naming cleanup pass.

Validation run
- Unity batchmode in /Users/farhad/Projects/WarlineCapture-CodexUnity1
- Execute method: GameplayArchitectureContractTests.RunRoadBuildArchitectureBatchValidation
- Log: /private/tmp/warlinecapture-roadbuild-step29-architecture.log

Validation result
Passed: [RoadBuildArchitectureValidation] result=Passed methods=31

Known gaps
- RoadBuildRuntimeStateSystem remains a temporary state holder and still needs later split/removal.
- Serialized RoadBuildSystemConfig and RoadBuildSystemSceneConfigAsset names remain documented compatibility debt until a separate asset migration.
- Step 30 validation gate is still pending: runtime-city smoke, building placement smoke, bootstrap/menu play-button smoke, and focused performance diagnostics.

Cross-lane impacts
- Runtime city and building gameplay still use the extracted road boundaries; no source API changes expected outside gameplay.
- QA should use step 30 to confirm road generation, building road footprints, road build create/delete/rollback, menu play, and performance.

Next recommended task
Road-build roadmap step 30: run the full focused validation gate across architecture, runtime city, building placement, menu/bootstrap, and performance diagnostics.
