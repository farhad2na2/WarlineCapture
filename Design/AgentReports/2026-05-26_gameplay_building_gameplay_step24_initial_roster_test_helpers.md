Lane
Gameplay

Task
BuildingGameplaySystem refactor roadmap step 24: move initial roster and runtime test helpers out of BuildingGameplaySystem.

Files changed
- Assets/Game/Scripts/AssemblyInfo.cs
- Assets/Game/Scripts/AssemblyInfo.cs.meta
- Assets/Game/Scripts/Systems/BuildingGameplaySystem.cs
- Assets/Tests/Editor/BuildingGameplayTestHarness.cs
- Assets/Tests/Editor/RuntimeGameplayStateTestHelper.cs
- Assets/Tests/Editor/GameplayArchitectureContractTests.cs
- Design/Architecture/building_gameplay_system_refactor_roadmap.md
- Design/Architecture/gameplay_solid_ecs_contract.md
- Design/AgentReports/2026-05-26_gameplay_building_gameplay_step24_initial_roster_test_helpers.md

Contracts touched
- BuildingGameplay roadmap now marks step 24 complete and records the 1599-line BuildingGameplaySystem transition ceiling.
- Gameplay SOLID/ECS contract now requires initial roster spawn to live in BuildingRuntimeSpawnSystem / BuildingRuntimeSpawnCommandSystem, and editor-only runtime test helpers to live in BuildingGameplayTestHarness rather than BuildingGameplaySystem.
- Focused architecture validation now includes BuildingInitialRosterAndTestHelpersMustLiveInRuntimeSpawnAndEditorHarness.

User-visible behavior
- No intended gameplay behavior change.
- Runtime spawn behavior remains routed through BuildingRuntimeSpawnCommandSystem.
- Editor validation helpers still work through BuildingGameplayTestHarness, but BuildingGameplaySystem no longer exposes the initial roster or runtime test helper methods.

Validation run
- git diff --check
- Unity batch architecture validation: GameplayArchitectureContractTests.RunBuildingGameplayArchitectureBatchValidation
- Unity EditMode validation: BuildingRuntimeBoundaryValidationTests

Validation result
- Passed.
- Initial architecture validation caught a compile issue when the editor harness tried to access production internals directly.
- A temporary production helper attempt was rejected by the architecture guard because it introduced another production BuildingGameplaySystem reference; it was removed.
- Final architecture validation log: /private/tmp/warlinecapture-building-gameplay-arch-step24-final.log, [BuildingGameplayArchitectureValidation] result=Passed methods=26.
- Runtime boundary results: /private/tmp/warlinecapture-building-runtime-boundary-step24.xml, total=1 passed=1 failed=0.
- Unity emitted the known non-blocking licensing/Xcode plist warnings during batch startup, but final validation exited successfully.

Known gaps
- BuildingGameplaySystem.cs still exists as temporary roadmap debt and is now 1599 lines.
- Runtime spawn wrappers for normal runtime/manual building spawn still remain in BuildingGameplaySystem for later consumer migration.
- Visual, selection, combat, barrier, redirect, and context factory slices remain pending from step 25 onward.

Cross-lane impacts
- Editor tests can now see production internals through Assembly-CSharp-Editor internals visibility.
- No art, map, economy, UI layout, scene, or runtime balance changes were made.

Next recommended task
- Continue with building_gameplay_system_refactor_roadmap.md step 25: move visual instance and positioning helpers.
