Lane
Gameplay

Task
BuildingGameplaySystem refactor roadmap step 18: move UI read methods behind BuildingUiQuerySystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Game/Scripts/Systems/BuildingUiContextSystem.cs
- Assets/Game/Scripts/Systems/BuildingUiQuerySystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/building_gameplay_system_refactor_roadmap.md
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-26_gameplay_building_gameplay_step18_ui_read_methods.md

Contracts touched
- BuildingGameplaySystem refactor roadmap now marks step 18 complete and records the 1742-line transition ceiling.
- Gameplay SOLID/ECS contract now requires building UI read methods to route through BuildingUiQuerySystem, not direct placement query or production request reads in BuildingGameplaySystem.
- Focused architecture validation now includes BuildingUiReadMethodsMustRouteThroughUiQuerySystem.

User-visible behavior
- No intended gameplay or UI behavior change.
- Selected-building flags, active-building flags, status text, label, display name, description, health, preview prefab, and selected-building production affordability now flow through BuildingUiQuerySystem.
- Existing BuildingGameplaySystem compatibility reads remain temporarily available but delegate to the UI query boundary.

Validation run
- git diff --check
- Unity batch architecture validation: GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation
- Unity EditMode validation: BuildingRuntimeBoundaryValidationTests

Validation result
- Passed.
- Architecture validation log: /private/tmp/warlinecapture-building-gameplay-arch-step18.log, [BuildingGameplayArchitectureValidation] result=Passed methods=20.
- Runtime boundary results: /private/tmp/warlinecapture-building-runtime-boundary-step18.xml, total=1 passed=1 failed=0.
- Unity emitted the known non-blocking licensing/Xcode plist warnings during batch startup, but both validation runs exited successfully.

Known gaps
- BuildingGameplaySystem.cs still exists as temporary roadmap debt and is 1742 lines after this transition.
- BuildingUiQuerySystem still receives scalar delegates from the temporary shell context source; later steps must move menu binding/context ownership off the shell.

Cross-lane impacts
- UI/HUD callers should continue using the existing query/read-model path.
- No art, map, AI, economy, or scene changes were made.

Next recommended task
- Continue with building_gameplay_system_refactor_roadmap.md step 19: move menu binding off the shell.
