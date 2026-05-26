Lane
Gameplay

Task
BuildingGameplaySystem refactor roadmap step 25: move visual helper wrappers out of BuildingGameplaySystem.

Files changed
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/building_gameplay_system_refactor_roadmap.md
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-26_gameplay_building_gameplay_step25_visual_helpers.md

Contracts touched
- BuildingGameplay roadmap now marks step 25 complete and records the 1583-line BuildingGameplaySystem transition ceiling.
- Gameplay SOLID/ECS contract now forbids BuildingGameplaySystem visual helper wrappers for instance creation, positioning, footprint centers, runtime visual initialization, marker refresh, and owner-faction visual tint.
- Focused architecture validation now includes BuildingVisualHelperWrappersMustStayOutOfBuildingGameplaySystem.

User-visible behavior
- No intended gameplay behavior change.
- Placement visual instance creation and positioning still route through BuildingPlacementVisualSystem.
- Runtime visual initialization and marker refresh still route through BuildingRuntimeVisualSystem.
- Owner-faction marker tint still routes through BuildingRuntimeOwnershipSystem.

Validation run
- git diff --check
- Unity batch architecture validation: GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation
- Unity EditMode validation: BuildingRuntimeBoundaryValidationTests

Validation result
- Passed.
- Architecture validation log: /private/tmp/warlinecapture-building-gameplay-arch-step25.log, [BuildingGameplayArchitectureValidation] result=Passed methods=27.
- Runtime boundary results: /private/tmp/warlinecapture-building-runtime-boundary-step25.xml, total=1 passed=1 failed=0.
- Unity emitted the known non-blocking licensing/Xcode plist warnings during batch startup, but both validation runs exited successfully.

Known gaps
- BuildingGameplaySystem.cs still exists as temporary roadmap debt and is now 1583 lines.
- Visual delegates are still wired by BuildingGameplaySystem context factories; context-factory removal is tracked for later roadmap steps.
- Selection, destruction/entity-link, combat/blocker, redirect/hauler, and context factory slices remain pending.

Cross-lane impacts
- No art, map, economy, UI layout, scene, or runtime balance changes were made.
- Building visual ownership is narrower, which reduces risk before future visual or selection work.

Next recommended task
- Continue with building_gameplay_system_refactor_roadmap.md step 26: move building selection and camera focus.
