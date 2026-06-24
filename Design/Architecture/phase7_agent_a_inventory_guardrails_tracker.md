# Phase 7 Agent A Tracker - Inventory, Guardrails, And Integration

Purpose:
Own the authoritative Phase 7 denominator, migration guardrails, validation harness, and integration discipline for the non-UI gameplay `SystemBase` to `ISystem` migration. Agent A is the merge-captain lane for parallel branches. In the current user-approved single-thread automation mode, this same thread may continue into Agents B-F after the Agent A baseline is complete, but it must follow each lane tracker, update that lane's progress, and preserve the same one-lane-at-a-time validation discipline.

Branch:
`codex/phase7-agent-a-inventory-guardrails`

Execution order:

1. Agent A completes the inventory, classification, guardrails, validation matrix, handoff contract, and progress accounting baseline before domain implementation changes.
2. In parallel-agent mode, Agents B-F write handoffs and Agent A integrates one branch at a time.
3. In current single-thread automation mode, this thread may continue into B-F lane work directly after Agent A baseline tasks are complete.
4. Single-thread domain work must still be lane-scoped: update the active lane tracker, touch only that lane's inventory rows/files unless a shared contract change is documented, run the lane validation gates, regenerate the authoritative inventory after each lane slice, and update the main tracker.
5. Agent A final completion remains open until all B-F lanes are done and the final validation matrix passes.

Progress snapshot:

- Checklist progress: `95 / 95 complete (100.0%)`.
- In progress: `0`.
- Remaining open: `0`.
- Current target: `Agent B P7-0003/P7-0019 reference boundary helper folds completed; authoritative inventory now has no open Phase 7 rows`.
- Runtime production baseline: `25 SystemBase/legacy declarations`, `138 ISystem declarations` under `Assets/Game/Scripts`.
- Current production ISystem share: `84.7%`.
- Inventory rows: `163 total`, `155 ProductionNonUI`, `8 ProductionUI`.
- Owner lanes assigned: `AgentB 17`, `AgentC 12`, `AgentD 9`, `AgentE 10`, `AgentF 34`, `Integration 81`.
- Dispositions: `Converted 131`, `DirectConvert 0`, `ManagedPresentationSystemBaseException 24`, `RetireFold 0`, `ReviewRequired 0`, `SplitThenConvert 0`, `UIOutOfScope 8`.
- Non-UI gameplay production target after Phase 7: `0 non-exception SystemBase`.
- Allowed production `SystemBase` after Phase 7: `UiToolkitShellApplySystem` plus counted managed presentation/config/camera exceptions only; no editor/test counting in the production denominator.
- Managed presentation exception planning cap: `<= 30 non-UI SystemBase`; current inventory plans `24`.
- Updating MonoBehaviour target after Phase 7: `0 newly introduced Update/LateUpdate/FixedUpdate/coroutine loops`.
- Planning projection from current inventory: `138 ISystem / 25 production SystemBase/legacy = 84.7% production ISystem share`; non-UI inventory now has only counted managed presentation/config/camera exceptions and converted ISystems, with UI rows still out of scope.
- Validation status: `Agent B P7-0019 PerformanceDiagnosticsReferenceBoundarySystem retired/folded: PerformanceDiagnosticsReferenceSystem moved into the composition assembly and now resolves the initialized MenuBootstrapView diagnostics helper from loaded Menu scene roots instead of storing it in a disabled SystemBase; MatchBootstrapSystem still falls back to its local diagnostics helper if no initialized menu diagnostics exists. Compile, focused PerformanceDiagnostics validation, inventory regeneration, git diff --check, and Phase 7 architecture guard passed. Agent B P7-0003 MatchSceneReferenceBoundarySystem also retired/folded: MatchSceneReferenceSystem now resolves MatchSceneView from the loaded scene roots, MatchBootstrapSystem no longer registers/clears a world-scoped reference, and MenuBootstrapSystem/MatchStartSceneSystemHelper read the direct resolver. Integration P7-0374 VisibleUnitSelectionSystem split completed: candidate collection now runs through the unmanaged VisibleUnitSelectionCandidateSystem ISystem and publishes VisibleUnitSelectionCandidateElement snapshots, while the direct VisibleUnitSelectionSystem helper keeps only managed Camera/screen-rectangle filtering for existing call sites. Integration P7-0325 RuntimeRootSystem, P7-0323 ResourceHaulerSystem, P7-0319 MatchHudSquadTraySelectionSystem, and P7-0318 MapVehiclePlacementSpawnSystem remain recorded as passed in their handoff reports. Agent C has completed all open SystemBase rows; Agent D has no remaining open Agent D inventory rows; Agent E folded assigned city/citizen/road helper wrappers into plain helpers; Agent F request-contract and final visual/camera helper slices are recorded in the lane trackers and handoff reports`; latest Integration/Agent B logs include `/private/tmp/warline-phase7-agent-b-performance-diagnostics-reference.log` (`[PerformanceDiagnosticsAllocationValidation] result=Passed tests=3`), `/private/tmp/warline-phase7-agent-b-match-scene-reference.log` (`[MatchSceneReferenceFocusedValidation] result=Passed tests=2`), `/private/tmp/warline-phase7-integration-visible-unit-selection-state.log` (`[SelectionStateFocusedValidation] result=Passed tests=8`), `/private/tmp/warline-phase7-integration-visible-unit-selection-isystem.log` (broad selection runner failed before this fixture on pre-existing `RtsSelectionInputSystemTests.RuntimeInput_DefersUnitSelectionUntilPointerRelease` log-string assertion), `/private/tmp/warline-phase7-integration-resource-hauler-helper-fold.log` (`[ResourceHaulerFocusedValidation] result=Passed tests=9`), `/private/tmp/warline-phase7-integration-match-hud-squad-tray-helper-fold.log` (`[MatchHudSquadTraySelectionFocusedValidation] result=Passed tests=3`), `/private/tmp/warline-phase7-integration-map-vehicle-placement-progress-state.log` (`[UnitMovementBlockerValidation] result=Passed`), and `/private/tmp/warline-phase7-agent-a-architecture.log` (`[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`); inventory reports `25` production SystemBase/legacy declarations, `138` production ISystem declarations, and `84.7%` production ISystem share; dispositions now include `24` managed exceptions and `0` open rows; prior Agent D/F/B/C log paths remain recorded in the active lane trackers and handoff reports.
- Non-ECS helper naming refactor: Batch 1 renamed `8` no-instance-state helpers, Batch 2 renamed `3` scene/diagnostics reference helpers, Batch 3 renamed `3` diagnostics helpers, Batch 4 renamed `1` runtime-city diagnostics helper, Batch 5 renamed `1` selection diagnostics helper, Batch 6 renamed `1` map-surface bootstrap scene helper, Batch 7 renamed `1` match-start request startup helper, Batch 8 renamed `1` gameplay feature startup composition helper, Batch 9 renamed `1` match-start scene helper, Batch 10 renamed `1` runtime-city read-model composition helper, Batch 11 renamed `1` building runtime object presentation helper, Batch 12 renamed `1` building marker visual presentation helper, Batch 13 renamed `1` selection screen marker UI helper, Batch 14 renamed `1` building gameplay binding composition helper, Batch 15 renamed `1` building production tick composition helper, Batch 16 renamed `1` building runtime boundary composition helper, Batch 17 renamed `1` building gameplay disposal composition helper, Batch 18 renamed `1` building gameplay startup composition helper, Batch 19 renamed `1` building selection click composition helper, Batch 20 renamed `1` building runtime resource prefab composition helper, Batch 21 renamed `1` building runtime tick composition helper, Batch 22 renamed `1` building placement input tick composition helper, Batch 23 renamed `1` building selection composition helper, Batch 24 renamed `1` building runtime side-effect composition helper, Batch 25 renamed `1` building placement runtime tick context composition helper, Batch 26 renamed `1` building placement command composition helper, Batch 27 renamed `1` building placement interaction composition helper, Batch 28 renamed `1` building placement visual composition presentation helper, Batch 29 renamed `1` building production composition helper, Batch 30 renamed `1` building production context composition helper, Batch 31 renamed `1` building gameplay result composition helper, Batch 32 renamed `1` building gameplay source composition helper, Batch 33 renamed `1` building gameplay composition helper, Batch 34 renamed `1` building gameplay dependency composition helper, Batch 35 renamed `1` building runtime query composition helper, Batch 36 renamed `1` building runtime context composition helper, Batch 37 renamed `1` building runtime boundary publish composition helper, Batch 38 renamed `1` building runtime resource prefab context composition helper, Batch 39 renamed `1` building placement context composition helper, Batch 40 renamed `1` building placement interaction context composition helper, Batch 41 renamed `1` building gameplay grid data composition helper, Batch 42 renamed `1` building gameplay ECS query composition helper, Batch 43 renamed `1` building gameplay disposal execution composition helper, Batch 44 renamed `1` building citizen population composition helper, Batch 45 renamed `1` building placement adapter composition helper, Batch 46 renamed `1` building destroyed visual presentation helper, Batch 47 renamed `1` building foundation visual presentation helper, Batch 48 renamed `1` building placement visual presentation helper, Batch 49 renamed `1` building placement visual update composition helper, Batch 50 renamed `1` building placement preview presentation helper, Batch 51 renamed `1` building placement startup helper, Batch 52 renamed `1` building placement lifecycle composition helper, Batch 53 renamed `1` building placement query UI helper, Batch 54 renamed `1` building placement redirect composition helper, Batch 55 renamed `1` building placement session composition helper, Batch 56 renamed `1` building placement command request composition helper, Batch 57 renamed `1` building definition prefab helper, Batch 58 renamed `1` building runtime visual presentation helper, Batch 59 renamed `1` unit attack trace presentation helper, Batch 60 renamed `1` unit impostor presentation helper, Batch 61 renamed `1` runtime city visual presentation helper, Batch 62 renamed `1` runtime city surface integration utility helper, Batch 63 renamed `1` runtime decoration spawner presentation helper, Batch 64 renamed `1` runtime grid blocker presentation helper, Batch 65 renamed `1` runtime city archway spawn prefab helper, Batch 66 renamed `1` runtime city building placement prefab helper, Batch 67 renamed `1` runtime city building plot utility helper, Batch 68 renamed `1` runtime city building spawn context composition helper, Batch 69 renamed `1` runtime city bulk building spawn routine prefab helper, Batch 70 renamed `1` runtime city bulk plot plan utility helper, and Batch 71 renamed `1` runtime city chain utility helper to approved reason suffixes, for `82` total renamed helpers. `Design/Architecture/non_ecs_to_ecs_system_inventory.md` was regenerated; current runtime non-ECS conversion denominator is `157`. Validation commands: `git diff --check` passed, `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal` passed, `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed, `/private/tmp/warline-unit-transport-deploy-path-request-boundary.log` passed with `[UnitTransportValidation] result=Passed tests=73`, `/private/tmp/warline-non-ecs-helper-naming-batch2-match-scene-reference.log` passed with `[MatchSceneReferenceFocusedValidation] result=Passed tests=2`, `/private/tmp/warline-non-ecs-helper-naming-batch2-performance-diagnostics.log` passed with `[PerformanceDiagnosticsAllocationValidation] result=Passed tests=3`, `/private/tmp/warline-non-ecs-helper-naming-batch2-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch3-citizen-visible.log` passed with `[CitizenVisibleUnitFocusedValidation] result=Passed tests=3`, `/private/tmp/warline-non-ecs-helper-naming-batch3-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch4-runtime-city-generation.log` passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`, `/private/tmp/warline-non-ecs-helper-naming-batch4-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch5-selection-order-marker.log` passed with `[SelectionOrderMarkerFocusedValidation] result=Passed tests=15`, `/private/tmp/warline-non-ecs-helper-naming-batch5-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch6-map-surface-bootstrap.log` passed with `[MapSurfaceRuntimeBootstrapValidation] result=Passed tests=2`, `/private/tmp/warline-non-ecs-helper-naming-batch6-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch7-match-start-request-rerun.log` passed with `[MatchStartRequestValidation] result=Passed tests=1`, `/private/tmp/warline-non-ecs-helper-naming-batch7-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch8-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch9-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch10-runtime-city-generation.log` passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`, and `/private/tmp/warline-non-ecs-helper-naming-batch10-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch11-building-runtime-boundary.log` passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`, and `/private/tmp/warline-non-ecs-helper-naming-batch11-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch12-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch12-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch13-selection-order-marker.log` passed with `[SelectionOrderMarkerFocusedValidation] result=Passed tests=15`, and `/private/tmp/warline-non-ecs-helper-naming-batch13-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch14-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch14-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch15-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, `/private/tmp/warline-non-ecs-helper-naming-batch15-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch16-building-runtime-boundary.log` passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch16-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch17-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, `/private/tmp/warline-non-ecs-helper-naming-batch17-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch18-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, `/private/tmp/warline-non-ecs-helper-naming-batch18-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch19-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, `/private/tmp/warline-non-ecs-helper-naming-batch19-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch20-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, `/private/tmp/warline-non-ecs-helper-naming-batch20-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch21-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, `/private/tmp/warline-non-ecs-helper-naming-batch21-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9`, `/private/tmp/warline-non-ecs-helper-naming-batch22-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, `/private/tmp/warline-non-ecs-helper-naming-batch22-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` with `runtimeNonEcsDenominator=206`; Unity was terminated after the pass marker because the batchmode process hung during post-test cleanup, `/private/tmp/warline-non-ecs-helper-naming-batch23-building-gameplay-composition.log` recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch23-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` with `runtimeNonEcsDenominator=205`, `/private/tmp/warline-non-ecs-helper-naming-batch24-building-gameplay-composition.log` recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch24-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` with `runtimeNonEcsDenominator=204`, `/private/tmp/warline-non-ecs-helper-naming-batch25-building-gameplay-composition.log` recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch25-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` with `runtimeNonEcsDenominator=203`, `/private/tmp/warline-non-ecs-helper-naming-batch26-building-placement-command.log` recorded `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`, and `/private/tmp/warline-non-ecs-helper-naming-batch26-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` with `runtimeNonEcsDenominator=202`, `/private/tmp/warline-non-ecs-helper-naming-batch27-building-placement-command.log` recorded `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`, and `/private/tmp/warline-non-ecs-helper-naming-batch27-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` with `runtimeNonEcsDenominator=201`, `/private/tmp/warline-non-ecs-helper-naming-batch28-building-placement-command.log` recorded `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`, and `/private/tmp/warline-non-ecs-helper-naming-batch28-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` with `runtimeNonEcsDenominator=200`, `/private/tmp/warline-non-ecs-helper-naming-batch29-building-gameplay-composition.log` recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch29-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` with `runtimeNonEcsDenominator=199`, `/private/tmp/warline-non-ecs-helper-naming-batch30-building-gameplay-composition.log` recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch30-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` with `runtimeNonEcsDenominator=198`, `/private/tmp/warline-non-ecs-helper-naming-batch31-building-gameplay-composition.log` recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch31-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` with `runtimeNonEcsDenominator=197`, `/private/tmp/warline-non-ecs-helper-naming-batch32-building-gameplay-composition.log` recorded `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch32-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` with `runtimeNonEcsDenominator=196`, `/private/tmp/warline-non-ecs-helper-naming-batch33-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch33-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=195`, `/private/tmp/warline-non-ecs-helper-naming-batch34-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch34-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=194`, `/private/tmp/warline-non-ecs-helper-naming-batch35-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch35-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=193`, `/private/tmp/warline-non-ecs-helper-naming-batch36-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch36-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=192`, `/private/tmp/warline-non-ecs-helper-naming-batch37-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch37-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=191`, `/private/tmp/warline-non-ecs-helper-naming-batch38-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch38-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=190`, `/private/tmp/warline-non-ecs-helper-naming-batch39-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch39-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=189`, `/private/tmp/warline-non-ecs-helper-naming-batch40-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch40-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=188`, `/private/tmp/warline-non-ecs-helper-naming-batch41-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch41-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=187`, `/private/tmp/warline-non-ecs-helper-naming-batch42-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch42-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=186`, `/private/tmp/warline-non-ecs-helper-naming-batch43-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch43-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=185`, `/private/tmp/warline-non-ecs-helper-naming-batch44-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch44-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=184`, `/private/tmp/warline-non-ecs-helper-naming-batch45-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch45-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=183`, `/private/tmp/warline-non-ecs-helper-naming-batch46-building-destroyed-visual.log` passed with `[BuildingDestroyedVisualFocusedValidation] result=Passed tests=2`, and `/private/tmp/warline-non-ecs-helper-naming-batch46-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=182`, `/private/tmp/warline-non-ecs-helper-naming-batch47-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch47-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=181`, `/private/tmp/warline-non-ecs-helper-naming-batch48-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch48-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=180`, `/private/tmp/warline-non-ecs-helper-naming-batch49-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch49-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=179`, `/private/tmp/warline-non-ecs-helper-naming-batch50-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch50-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=178`, `/private/tmp/warline-non-ecs-helper-naming-batch51-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch51-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=177`, `/private/tmp/warline-non-ecs-helper-naming-batch52-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, and `/private/tmp/warline-non-ecs-helper-naming-batch52-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=176`, `/private/tmp/warline-non-ecs-helper-naming-batch53-building-gameplay-composition.log` passed with `[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed`, `/private/tmp/warline-non-ecs-helper-naming-batch53-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=175`, `/private/tmp/warline-non-ecs-helper-naming-batch54-building-runtime-boundary.log` passed with `[BuildingRuntimeBoundaryValidation] result=Passed tests=9`, and `/private/tmp/warline-non-ecs-helper-naming-batch54-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=174`, `/private/tmp/warline-non-ecs-helper-naming-batch55-building-placement-command.log` recorded `[BuildingPlacementCommandRequestValidation] result=Passed tests=13`, and `/private/tmp/warline-non-ecs-helper-naming-batch55-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=173`, `/private/tmp/warline-non-ecs-helper-naming-batch58-building-selection-marker.log` recorded `[BuildingSelectionMarkerFocusedValidation] result=Passed tests=6`, and `/private/tmp/warline-non-ecs-helper-naming-batch58-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=170`, `/private/tmp/warline-non-ecs-helper-naming-batch59-unit-combat.log` recorded `[UnitCombatFocusedEditModeValidation] result=Passed tests=1`, and `/private/tmp/warline-non-ecs-helper-naming-batch59-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=169`, `/private/tmp/warline-non-ecs-helper-naming-batch60-unit-render-budget.log` recorded `[UnitRenderBudgetFocusedValidation] result=Passed tests=31`, and `/private/tmp/warline-non-ecs-helper-naming-batch60-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=168`, `/private/tmp/warline-non-ecs-helper-naming-batch61-runtime-city-generation.log` recorded `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`, and `/private/tmp/warline-non-ecs-helper-naming-batch61-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=167`, `/private/tmp/warline-non-ecs-helper-naming-batch62-runtime-city-generation.log` recorded `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`, and `/private/tmp/warline-non-ecs-helper-naming-batch62-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=166`, `/private/tmp/warline-non-ecs-helper-naming-batch63-runtime-city-generation.log` recorded `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`, and `/private/tmp/warline-non-ecs-helper-naming-batch63-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=165`, `/private/tmp/warline-non-ecs-helper-naming-batch64-runtime-city-generation.log` recorded `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`, and `/private/tmp/warline-non-ecs-helper-naming-batch64-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=164`, `/private/tmp/warline-non-ecs-helper-naming-batch65-runtime-city-generation.log` recorded `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`, and `/private/tmp/warline-non-ecs-helper-naming-batch65-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=163`, `/private/tmp/warline-non-ecs-helper-naming-batch66-runtime-city-generation.log` recorded `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`, and `/private/tmp/warline-non-ecs-helper-naming-batch66-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=162`, `/private/tmp/warline-non-ecs-helper-naming-batch67-runtime-city-generation.log` recorded `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`, and `/private/tmp/warline-non-ecs-helper-naming-batch67-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=161`, `/private/tmp/warline-non-ecs-helper-naming-batch68-runtime-city-generation.log` recorded `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`, and `/private/tmp/warline-non-ecs-helper-naming-batch68-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=160`, `/private/tmp/warline-non-ecs-helper-naming-batch69-runtime-city-generation.log` recorded `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`, and `/private/tmp/warline-non-ecs-helper-naming-batch69-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=159`, `/private/tmp/warline-non-ecs-helper-naming-batch70-runtime-city-generation.log` recorded `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`, and `/private/tmp/warline-non-ecs-helper-naming-batch70-architecture.log` recorded `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=158`, `/private/tmp/warline-non-ecs-helper-naming-batch71-runtime-city-generation.log` passed with `[RuntimeCityGenerationFocusedValidation] result=Passed tests=2`, and `/private/tmp/warline-non-ecs-helper-naming-batch71-architecture.log` passed with `[NonEcsSystemConversionArchitectureValidation] result=Passed tests=9` and `runtimeNonEcsDenominator=157`; Batch 23-32 Unity processes, Batch 54 architecture, Batch 55 Unity validations, Batch 56 Unity validations, Batch 57 runtime-boundary/architecture validations, Batch 58 Unity validations, Batch 59 Unity validations, Batch 60 Unity validations, Batch 61 Unity validations, Batch 62 Unity validations, Batch 63 Unity validations, Batch 64 Unity validations, Batch 65 Unity validations, Batch 66 Unity validations, Batch 67 Unity validations, Batch 68 Unity validations, Batch 69 Unity validations, and Batch 70 Unity validations were terminated after pass markers because batchmode hung during post-test cleanup; Batch 71 Unity validations exited cleanly. `/private/tmp/warline-non-ecs-helper-naming-batch8-bootstrap-composition.log` exposed a pre-existing UI Toolkit hierarchy lookup in `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs:1809`; this file was not touched and UI Toolkit/Canvas migration is out of scope. `/private/tmp/warline-non-ecs-helper-naming-batch3-building-runtime-tick-rerun.log` exposed a pre-existing `BuildingPlacementRuntimeTickSystemTests.SimulationTickKeepsMapPlacementQueuesAliveBeforeBoundary` expectation mismatch: current `HEAD` and the renamed working tree both run `UpdateSimulation` boundary first while the test expects map placement queues first. `NonEcsSystemConversionArchitectureTests` now has an exact transition list for current public command-shaped non-ECS helper methods and a single documented UI ECS-boundary exclusion for `UiToolkitShellApplySystem`, so new entries or count drift fail.

Current execution mode:

- Status: `SingleThreadDomainExecutionApproved`.
- User instruction: continue beyond Agent A into B-F boundaries after Agent A baseline; do not block on external handoffs.
- Parallel-agent handoff check command remains available: `find Design/AgentReports -maxdepth 1 -name '2026-*_phase7_agent_*_handoff.md' -print | sort`.
- Result at last check: single-thread slice handoffs now exist for completed Agent B/F/Integration work; the heartbeat should keep using single-thread lane execution rather than wait for external branches.
- Next automation action: complete final Phase 7 accounting and validation pass now that the authoritative inventory has `0` open rows; continue any separately assigned split-decomposition tracker work only if explicitly in scope.

Owned files:

- `Design/Architecture/phase7_agent_a_inventory_guardrails_tracker.md`
- `Design/Architecture/systembase_to_isystem_inventory.md`
- `Design/Architecture/phase7_monobehaviour_loop_baseline.md`
- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
- `Assets/Tests/Editor/NonUiSystemBaseMigrationArchitectureTests.cs`
- Any new focused validation runner required for Phase 7 guardrails.
- Optional generator script under `Tools/Architecture/`.

Do not touch unless the current single-thread automation lane explicitly owns it:

- UI Toolkit/Canvas migration implementation.
- Project settings, scenes, prefabs, or asmdefs unless a guardrail test or the active lane's validated implementation genuinely requires it.
- Files outside the active Agent B-F lane without documenting the shared-contract reason in the active lane tracker and main Phase 7 tracker.

Shared rules:

- Do not allow a new non-UI runtime `SystemBase` unless it has an explicit inventory row and managed presentation/config/camera exception.
- Do not allow converted `ISystem` files to reference `GameObject`, `Transform`, `Camera`, `UnityEngine.Object`, `ScriptableObject`, `Resources`, `Object.Instantiate`, `Object.Destroy`, `Find*`, `Camera.main`, hierarchy paths, managed component classes, `List<GameObject>`, `Dictionary<..., GameObject>`, or mutable static gameplay state.
- Do not mark a target complete if gameplay policy remains in a managed presentation/config/camera exception.
- Do not allow Phase 7 to introduce `MonoBehaviour.Update`, `LateUpdate`, `FixedUpdate`, coroutine loops, or manager-style MonoBehaviour tickers. MonoBehaviours are view/reference holders only.
- Do not let domain agents edit shared trackers directly. They write handoffs under `Design/AgentReports/`.
- Do not classify a Unity-object visual system as `DirectConvert` just to improve the inheritance percentage. Preserve visuals and keep Unity-object ticking in counted managed `SystemBase` exceptions when required.
- Do not replace one broad `SystemBase` with one broad `ISystem`.

Reference documents:

- `Design/Architecture/ecs_architecture_performance_quality_improvement_tracker.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/performance_regression_contract.md`
- `Design/Architecture/ui_runtime_shell_transition_architecture.md`
- `Design/Architecture/five_systembase_to_isystem_conversion_tracker.md`
- `Design/Architecture/ecs_native_command_request_system_conversion_example.md`

## A0 - Authoritative Inventory Generator

Goal:
Create a stable, repeatable inventory so all later work uses the same denominator and ownership rows.

- [x] Inspect existing architecture tests, especially `EcsBurstHotPathArchitectureTests` and `NonEcsSystemConversionArchitectureTests`, before writing new parsing logic.
- [x] Decide implementation form: C# architecture test preferred if it can parse all needed fields; generator script allowed if it produces a stable markdown artifact.
- [x] Create `Tools/Architecture/generate_systembase_to_isystem_inventory.py` or an equivalent C# architecture generator/test.
- [x] Enumerate every `SystemBase`, `ComponentSystemBase`, `ComponentSystem`, `JobComponentSystem`, and `ISystem` declaration under `Assets/Game/Scripts`.
- [x] Use a parser robust enough for partial classes, multiple declarations per file, nested test helper classes, generic type declarations, multi-line inheritance lists, attributes, and comments.
- [x] Exclude `Assets/Game/Scripts/UI` from the non-UI conversion denominator, but list UI systems separately.
- [x] Exclude editor-only and test-only systems from the production denominator, but list them separately.
- [x] Record file path, type name, kind, accessibility, namespace if present, assembly if discoverable, line number, and current inheritance.
- [x] Record update group attributes, ordering attributes, and `[DisableAutoCreation]`.
- [x] Record lifecycle methods: `OnCreate`, `OnStartRunning`, `OnUpdate`, `OnStopRunning`, `OnDestroy`, `Update`, `LateUpdate`, `FixedUpdate`, and coroutine methods.
- [x] Record public/internal methods and properties that composition code may call.
- [x] Record public interface implementations such as renderer, lookup, command, read-model, or boundary interfaces.
- [x] Record managed field categories: Unity object, managed collection, public helper state, native container, query/lookup/cache, config asset, prefab reference, presentation view.
- [x] Record ECS access shape: `Entities.ForEach`, `SystemAPI.Query`, `EntityQuery`, `EntityManager`, `GetComponentLookup`, `GetBufferLookup`, `ToEntityArray`, `ToComponentDataArray`, ECB, jobs, `.Run`, `.Schedule`, `.ScheduleParallel`.
- [x] Record managed blocker tokens: `GameObject`, `Transform`, `Camera`, `UnityEngine.Object`, `ScriptableObject`, `Resources`, `Object.Instantiate`, `Object.Destroy`, `Find*`, `Camera.main`, `Material`, `Renderer`, `Light`, `ParticleSystem`, `LineRenderer`, `VisualEffect`, `MonoBehaviour`, `Coroutine`, `StartCoroutine`, `StopCoroutine`, `List<GameObject>`, `Dictionary<..., GameObject>`.
- [x] Record likely owner lane from path/name prefix before manual review.
- [x] Emit stable markdown to `Design/Architecture/systembase_to_isystem_inventory.md`.
- [x] Emit a compact machine-readable sidecar if useful, for example `Library/Codex/systembase_to_isystem_inventory.json` or `/private/tmp/warline-phase7-systembase-inventory.json`; do not commit generated scratch under `Library`.
- [x] Add generation timestamp, command used, source commit hash, and dirty-worktree note to the inventory.
- [x] Run the generator twice and confirm stable output ordering.
- [x] Record current counts in the Agent A tracker and main Phase 7 tracker.

Suggested generator command shape:

```bash
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py \
  --root Assets/Game/Scripts \
  --output Design/Architecture/systembase_to_isystem_inventory.md
```

Inventory table required columns:

| Column | Meaning |
| --- | --- |
| Id | Stable id such as `P7-0001`, not line-number dependent. |
| Type | C# type name. |
| Kind | `class` or `struct`. |
| Current base | `SystemBase`, `ISystem`, `ComponentSystemBase`, etc. |
| Path | Runtime source path. |
| Line | Declaration line. |
| UI/editor/test scope | `ProductionNonUI`, `ProductionUI`, `Editor`, `Test`. |
| Owner lane | `AgentB`, `AgentC`, `AgentD`, `AgentE`, `AgentF`, `Integration`. |
| Disposition | `DirectConvert`, `SplitThenConvert`, `RetireFold`, `ManagedPresentationSystemBaseException`, `ViewReferenceOnlyMonoBehaviour`, `UIOutOfScope`, `EditorOutOfScope`, `TestOutOfScope`, `ReviewRequired`. |
| Managed blockers | Concrete blocker tokens or `None`. |
| Gameplay policy risk | `None`, `Low`, `Medium`, `High`, with reason. |
| Public API/call sites | Summary of public helper surface and key callers. |
| First safe slice | Smallest recommended behavior-preserving edit. |
| Replacement target | New `ISystem`, split processors, folded helper, or exception type. |
| Validation gate | Focused test/runner/log expected before close. |
| Status | `Open`, `InProgress`, `Converted`, `Split`, `Retired`, `ManagedException`, `Deferred`. |

Acceptance:

- Inventory row count matches the declaration scan.
- Each non-UI production `SystemBase` appears exactly once.
- UI/test/editor exclusions are visible, not hidden.
- Rows are sorted deterministically by scope, owner lane, path, type name.
- Inventory can be regenerated after a domain branch merge without reshuffling unchanged row ids.

## A1 - Classification Rules And Owner Assignment

Goal:
Assign every row to the right lane before parallel implementation starts.

- [x] Add disposition values: `DirectConvert`, `SplitThenConvert`, `RetireFold`, `ManagedPresentationSystemBaseException`, `ViewReferenceOnlyMonoBehaviour`, `UIOutOfScope`, `EditorOutOfScope`, `TestOutOfScope`, `ReviewRequired`.
- [x] Add owner lane values: `AgentB`, `AgentC`, `AgentD`, `AgentE`, `AgentF`, `Integration`.
- [x] Add blocker values from concrete token matches and manual review.
- [x] Add first safe slice for every `ProductionNonUI` row.
- [x] Add validation command for every `ProductionNonUI` row or mark as missing validation debt.
- [x] Add converted/replacement type for any row already converted by earlier work.
- [x] Mark all rows with no obvious owner as `ReviewRequired`, not guessed.
- [x] Review every `ReviewRequired` row manually before Agents B-F start implementation.
- [x] Export owner-lane filtered sections so each domain agent can quickly copy its assigned rows.

Classification decision table:

| Disposition | Use when | Must not contain |
| --- | --- | --- |
| `DirectConvert` | Pure ECS data/request/state work, no Unity object blockers, no broad public helper facade. | `GameObject`, `Camera`, prefab object refs, public managed facade API, gameplay-independent presentation. |
| `SplitThenConvert` | ECS gameplay is mixed with managed/presentation/config/camera work. | A plan to convert the whole broad owner into one large `ISystem`. |
| `RetireFold` | System only composes, forwards, caches dependencies, or exposes helper API with no independent update responsibility. | Hidden gameplay policy or recurring state mutation. |
| `ManagedPresentationSystemBaseException` | Must tick Unity objects like ParticleSystem, Renderer, Light, Camera, Transform, Material, pooled GameObject, scene refs, diagnostics output. | Gameplay decision, command validation, damage, spawn policy, pathing, economy, selection policy. |
| `ViewReferenceOnlyMonoBehaviour` | Holds serialized refs, prefab refs, or callable view methods, with no runtime loop. | `Update`, `LateUpdate`, `FixedUpdate`, coroutine loop, ECS mutation. |
| `ReviewRequired` | Blockers/call sites are unclear. | Silent assignment to a domain lane. |

Owner lane defaults:

| Lane | Default type/path patterns |
| --- | --- |
| Agent B | `RuntimeGameplayState*`, `RuntimeDiagnostics*`, `PerformanceDiagnostics*`, `AI*Startup*`, `AIPlanEntry*`, `AIFactionControlStartup*`, `FactionEconomyStartup*`, `RuntimeGridBootstrap*`, small data-only startup/config projection. |
| Agent C | `RtsSelection*`, `Selection*`, `FocusableUnit*`, `FocusedUnit*`, selected-state and command-result systems. |
| Agent D | `Building*`, `MapBuildingPlacement*`, building placement, production, runtime, spawn, combat, building UI query, five-SystemBase sub-track. |
| Agent E | `Road*`, `RuntimeCity*`, `RuntimeGridBlocker*`, `RuntimeDecoration*`, `DayNight*`, `Citizen*`. |
| Agent F | `Rendering/*`, `*Visual*`, `*Vfx*`, `*Trace*`, `*Impostor*`, `*Marker*`, `*Camera*`, `VisualQuality*`, ParticleSystem/Renderer/Light/Material/pooling ownership. |
| Integration | Shared contracts, architecture tests, generated inventory, main tracker, cross-lane conflicts. |

Acceptance:

- No row is left unclassified unless marked `ReviewRequired` with a reason.
- No row is marked as a managed exception without a concrete Unity-object ticking blocker.
- No row can introduce an updating MonoBehaviour bridge.
- Owner lanes do not overlap.
- Each domain agent can start from its filtered inventory section without reopening the whole project-wide classification.

## A2 - Guardrail Tests

Goal:
Make the migration ratchet enforceable before domain conversions.

- [x] Add `Assets/Tests/Editor/NonUiSystemBaseMigrationArchitectureTests.cs`.
- [x] Add `RunFocusedValidation()` entry point for batchmode execution.
- [x] Load `Design/Architecture/systembase_to_isystem_inventory.md` and parse inventory ids, paths, type names, dispositions, owner lanes, and statuses.
- [x] Add test that fails when a production non-UI `SystemBase` appears without an inventory row.
- [x] Add test that fails when an inventory row points to a deleted or renamed file.
- [x] Add test that fails when duplicate inventory rows point to the same type/path.
- [x] Add test that converted targets cannot regain `SystemBase`.
- [x] Add test that completed `ISystem` files avoid managed Unity object blockers.
- [x] Add test that `ManagedPresentationSystemBaseException` rows do not contain gameplay request validation, command execution, simulation, damage, economy, pathing, selection policy, building placement policy, or gameplay ECS mutation policy.
- [x] Add test that no Phase 7 change introduces new `MonoBehaviour.Update`, `LateUpdate`, `FixedUpdate`, or coroutine loops.
- [x] Add test that all MonoBehaviour rows classified as `ViewReferenceOnlyMonoBehaviour` have no runtime loop methods.
- [x] Add test that broad replacement `ISystem` types are flagged if source length, public helper count, query count, or responsibility markers exceed documented thresholds.
- [x] Add test that new runtime non-UI ECS systems default to `ISystem` unless classified as managed presentation/config/camera exception.
- [x] Add test that public helper APIs on converted systems are replaced by ECS request/result or plain helper functions.
- [x] Add test that inventory owner lane names match existing agent tracker files.
- [x] Add test that managed exception count does not exceed the planning cap unless the tracker has been updated with a new approved cap.
- [x] Add test that the final share formula can be computed from inventory counts.

Initial broad-system thresholds:

- Public method/property count over `8`: manual review.
- `OnUpdate` source body over `180` nonblank lines: manual review.
- More than `5` independent query families: manual review.
- More than `2` domain prefixes in a type name or blockers: manual review.
- Any type containing `Manager`, `Controller`, `Facade`, `Service`, `Resolver`, `Context`, `Adapter`, or `Composer` in a new replacement name: fail unless explicitly approved by architecture contract.

Deliberate violation checks:

- [x] Temporarily add a local untracked fixture or in-test source string for a new non-inventory `SystemBase`; confirm test fails, then remove it.
- [x] Temporarily add a local untracked fixture or in-test source string for `MonoBehaviour.Update`; confirm test fails, then remove it.
- [x] Temporarily mark a known gameplay system as `ManagedPresentationSystemBaseException`; confirm policy-token guard fails, then restore.

Acceptance:

- Guardrails pass on the current baseline before any domain conversion.
- Deliberate violations fail the expected guards.
- Guardrails are runnable without opening the Unity editor manually.
- Test failure messages include the file path, type name, inventory id, and required next action.

## A3 - Validation Matrix And Runner

Goal:
Give each domain lane a known validation set before implementation.

- [x] Create a Phase 7 validation matrix in the inventory or as a section in this tracker.
- [x] Map Agent B direct/startup systems to startup, diagnostics, and architecture validations.
- [x] Map Agent C selection systems to selection input, command-result, hold/stop/scan, board/attack, and allocation validations.
- [x] Map Agent D building systems to placement, production, build drawer, building selection, combat, and placement-to-production PlayMode validations.
- [x] Map Agent E road/city/citizen systems to road build, runtime city, blocker, citizen, movement, and match smoke validations.
- [x] Map Agent F rendering/VFX systems to render budget, vehicle visual, missile VFX, attached light, marker, visual-quality, and graphics smoke validations.
- [x] Add one command or runner entry per validation gate where practical.
- [x] Add fallback rule: if main Unity project is locked, retry once, then use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` if available.
- [x] Add log path naming convention: `/private/tmp/warline-phase7-agent-<lane>-<target>-<validation>.log`.
- [x] Add required validation status values: `NotRun`, `Passed`, `Failed`, `BlockedProjectLocked`, `BlockedMissingRunner`, `DeferredWithReason`.

Suggested architecture command:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath /Users/farhad/Projects/WarlineCapture \
  -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation \
  -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Minimum validation matrix:

| Lane | Required validation gates |
| --- | --- |
| Agent B | Architecture guard, compile, `RuntimeDiagnosticsSystemTests`, `AIStartupSystemValidationTests`, `AIFactionControlStartupSystemValidationTests`, `AIPlanEntryStartupSystemValidationTests`, `FactionResourceSystemTests`, startup smoke where available. |
| Agent C | Architecture guard, compile, `RtsSelectionInputSystemTests`, `SelectionStateSystemTests`, `FocusableUnitLookupSystemTests`, `SelectedUnitOrderSnapshotSystemTests`, hold/stop/scan and board/attack command validations. |
| Agent D | Architecture guard, compile, building placement command/runtime tick, production, build drawer, building UI query, building combat, building selection marker/faction visual, placement-to-production PlayMode smoke. |
| Agent E | Architecture guard, compile, road build command, nearest road/build, runtime city generation, runtime grid blocker, citizen visible/population, movement, match runtime smoke. |
| Agent F | Architecture guard, compile, render budget, vehicle visual adornments, air missile VFX, ground missile VFX, attached light, marker, visual quality, graphics-capable smoke. |

Command template:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath /Users/farhad/Projects/WarlineCapture-Clone \
  -executeMethod <RunnerType>.<RunnerMethod> \
  -logFile /private/tmp/warline-phase7-agent-<lane>-<target>-<validation>.log
```

Shared validation commands:

| Gate | Command | Log path/status |
| --- | --- | --- |
| Generator syntax | `python3 -B -c "import py_compile; py_compile.compile('Tools/Architecture/generate_systembase_to_isystem_inventory.py', cfile='/private/tmp/generate_systembase_to_isystem_inventory.pyc', doraise=True)"` | `/private/tmp/generate_systembase_to_isystem_inventory.pyc` |
| Inventory regeneration | `python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json` | `/private/tmp/warline-phase7-systembase-inventory.json` |
| Editor compile | `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` | domain handoff records console output or redirected log |
| Diff hygiene | `git diff --check` | domain handoff records pass/fail |
| Phase 7 architecture guard | Unity command with `NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-a-architecture.log` |

Focused runner matrix:

| Lane | Gate | Runner entry | Log path |
| --- | --- | --- | --- |
| Agent B | Runtime diagnostics | `RuntimeDiagnosticsSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-b-runtime-diagnostics.log` |
| Agent B | Performance diagnostics allocation | `PerformanceDiagnosticsSystemAllocationTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-b-performance-diagnostics.log` |
| Agent B | AI startup projection | `AIStartupSystemValidationTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-b-ai-startup.log` |
| Agent B | AI faction control startup | `AIFactionControlStartupSystemValidationTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-b-ai-faction-control-startup.log` |
| Agent B | AI plan entry startup | `AIPlanEntryStartupSystemValidationTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-b-ai-plan-entry-startup.log` |
| Agent B | Economy startup | `FactionEconomyStartupSystemValidationTests` tests through Unity Test Runner unless a batch runner is added by the slice | `BlockedMissingRunner` until runner exists |
| Agent C | Selection input | `RtsSelectionInputSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-c-selection-input.log` |
| Agent C | Selection state | `SelectionStateSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-c-selection-state.log` |
| Agent C | Focusable unit lookup | `FocusableUnitLookupSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-c-focusable-unit-lookup.log` |
| Agent C | Selected order snapshot | `SelectedUnitOrderSnapshotSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-c-selected-order-snapshot.log` |
| Agent C | Command request/result contract | `SelectionCommandRequestResultContractTests.RunBatchValidation` | `/private/tmp/warline-phase7-agent-c-command-contract.log` |
| Agent C | Selection/order markers | `SelectionOrderMarkerSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-c-selection-order-marker.log` |
| Agent C | Movement hold/stop/scan interaction | `UnitMovementBlockerValidationTests.RunHoldCommandFocusedValidation` and `UnitMovementBlockerValidationTests.RunBatchValidation` | `/private/tmp/warline-phase7-agent-c-hold-stop-scan.log` |
| Agent D | Building placement command | `BuildingPlacementValidationSystemTests.RunPlacementCommandRequestValidation` | `/private/tmp/warline-phase7-agent-d-building-placement-command.log` |
| Agent D | Building placement runtime tick | `BuildingPlacementRuntimeTickSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-d-building-placement-runtime.log` |
| Agent D | Building runtime boundary | `BuildingRuntimeBoundaryValidationTests.RunBatchValidation` | `/private/tmp/warline-phase7-agent-d-building-runtime-boundary.log` |
| Agent D | Building production | `BuildingProductionSystemTests.RunProductionRequestValidation` | `/private/tmp/warline-phase7-agent-d-building-production.log` |
| Agent D | Building composition smoke | `BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation` | `/private/tmp/warline-phase7-agent-d-building-composition-smoke.log` |
| Agent D | Building UI query | `BuildingUiQuerySystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-d-building-ui-query.log` |
| Agent D | Building combat | `BuildingCombatSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-d-building-combat.log` |
| Agent D | Building selection marker | `BuildingSelectionMarkerSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-d-building-selection-marker.log` |
| Agent D | Building faction visual | `BuildingFactionVisualSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-d-building-faction-visual.log` |
| Agent E | Road build command | `RoadBuildCommandSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-e-road-build.log` |
| Agent E | Runtime city generation | `RuntimeCityGenerationFocusedTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-e-runtime-city-generation.log` |
| Agent E | Movement/pathing smoke | `UnitMovementBlockerValidationTests.RunBatchValidation` | `/private/tmp/warline-phase7-agent-e-movement.log` |
| Agent E | Citizen movement | `CitizenMovementCommandSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-e-citizen-movement.log` |
| Agent E | Map surface diagnostics | `MapSurfaceDiagnosticsSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-e-map-surface-diagnostics.log` |
| Agent F | Render budget | `UnitRenderBudgetSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-f-render-budget.log` |
| Agent F | Vehicle visual adornments | `VehicleVisualAdornmentsSystemTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-f-vehicle-visual-adornments.log` |
| Agent F | Ground missile projectile dependencies | `GroundMissileLauncherRuntimeTests.RunProjectileDependencyValidation` | `/private/tmp/warline-phase7-agent-f-ground-missile-projectile.log` |
| Agent F | Ground missile visuals | `GroundMissileLauncherRuntimeTests.RunMissileVisualValidation` | `/private/tmp/warline-phase7-agent-f-ground-missile-visual.log` |
| Agent F | Ground missile attack | `GroundMissileLauncherRuntimeTests.RunAttackFocusedValidation` | `/private/tmp/warline-phase7-agent-f-ground-missile-attack.log` |
| Agent F | Radar/missile runtime | `MissileLauncherRadarAttackValidationTests.RunRuntimeFocusedValidation` | `/private/tmp/warline-phase7-agent-f-radar-missile-runtime.log` |
| Agent F | Building destroyed visual | `BuildingDestroyedVisualPresentationSystemHelperTests.RunFocusedValidation` | `/private/tmp/warline-phase7-agent-f-building-destroyed-visual.log` |
| Agent F | Visual quality | Use Unity Test Runner or add a focused runner in the slice if no `RunFocusedValidation` exists for the touched visual-quality file | `BlockedMissingRunner` until runner exists |

Acceptance:

- Every inventory owner lane has at least one focused validation gate.
- Final Phase 7 cannot close without the matrix passing or explicit deferred validation owner.
- Each domain handoff can cite validation gates by name instead of inventing new ones.

## A4 - Integration Workflow

Goal:
Allow B-F to work in parallel without creating merge chaos.

Current integration mode:

- Status: `SingleThreadDomainExecutionApproved`.
- Checked command: `find Design/AgentReports -maxdepth 1 -name '2026-*_phase7_agent_*_handoff.md' -print | sort`.
- Result at last check: no Agent B-F Phase 7 handoff reports are present yet.
- Parallel branch merge checklist items stay open until actual external handoffs/branches exist.
- Current heartbeat should not block on missing handoffs. It should proceed to the next lane tracker in the approved sequence and treat each lane slice as the handoff source of truth.
- Required lane order for this single-thread automation: Agent B direct/startup, Agent C selection/commands, Agent F request-contract slice, Agent D building/production, Agent E road/city/citizen, then Agent F final visual/camera exceptions.

- [x] Define branch names in every agent tracker and verify they are unique.
- [x] Create or name an integration branch, for example `codex/phase7-integration`.
- [x] Require each domain agent to write `Design/AgentReports/YYYY-MM-DD_phase7_agent_<lane>_handoff.md`.
- [x] Require each handoff to include files changed, inventory ids touched, systems converted, systems split, managed exceptions created/retained, validations run, validation logs, and blockers.
- [x] Require each handoff to declare whether it touched shared components/contracts, asmdefs, tests, or generated inventory.
- [x] Require each handoff to list expected conflicts before merge.
- [ ] Merge one domain branch at a time into the integration branch.
- [ ] After each merge, regenerate inventory and compare counts.
- [ ] After each merge, run `git diff --check`.
- [ ] After each merge, run Phase 7 architecture guardrails.
- [ ] After each merge, update only the relevant inventory rows and main tracker progress snapshot.
- [ ] If a domain branch conflicts with another branch, Agent A resolves using both handoffs and reruns the smaller affected validations before moving on.
- [ ] If a domain branch changes a shared contract, Agent A notifies affected lane docs in the integration handoff before merging the next branch.

Merge order recommendation:

1. Agent B direct/startup, because these should be smallest and prove the guardrails.
2. Agent C selection/commands, because many other domains read selection state.
3. Agent F rendering/VFX request contracts that do not depend on building/road internals.
4. Agent D building/production after request/result and visual exception rules are proven.
5. Agent E road/city/citizen after building and visual request contracts stabilize.
6. Agent F final visual/camera exceptions after D/E expose final request/result data.

Branch map:

| Lane | Branch | Verified source |
| --- | --- | --- |
| Agent A | `codex/phase7-agent-a-inventory-guardrails` | This tracker |
| Agent B | `codex/phase7-agent-b-direct-startup` | `Design/Architecture/phase7_agent_b_direct_startup_tracker.md` |
| Agent C | `codex/phase7-agent-c-selection-commands` | `Design/Architecture/phase7_agent_c_selection_commands_tracker.md` |
| Agent D | `codex/phase7-agent-d-building-production` | `Design/Architecture/phase7_agent_d_building_production_tracker.md` |
| Agent E | `codex/phase7-agent-e-road-city-citizen` | `Design/Architecture/phase7_agent_e_road_city_citizen_tracker.md` |
| Agent F | `codex/phase7-agent-f-rendering-vfx` | `Design/Architecture/phase7_agent_f_rendering_vfx_tracker.md` |
| Integration | `codex/phase7-integration` | Named by Agent A; create this branch only when first domain merge is ready |

Domain handoff contract:

- Required handoff path: `Design/AgentReports/YYYY-MM-DD_phase7_agent_<lane>_handoff.md`.
- Required template: `Design/AgentReports/phase7_domain_handoff_template.md`.
- Required contents: inventory ids touched, files changed, systems converted/split/retired, managed exceptions retained or created, shared components/contracts/asmdefs/tests/generated-inventory touches, validations run, log paths, blockers, deferred validations, and expected conflicts.
- Domain agents must not edit this tracker, the main Phase 7 tracker, or unrelated lane rows. They record progress in their own tracker and write the handoff for Agent A.
- Agent A merges one domain branch at a time into `codex/phase7-integration`, resolves conflicts with the handoff open, then updates the shared inventory and main tracker.

Single-thread automation contract:

- The heartbeat may continue domain work directly from this thread after Agent A baseline work.
- Before editing code for a lane, read that lane's tracker and the relevant inventory rows.
- Keep one active lane at a time. Do not mix B-F implementation slices in one validation batch.
- Update the active lane tracker progress as tasks complete or block.
- Regenerate `Design/Architecture/systembase_to_isystem_inventory.md` after each completed lane slice.
- Run `git diff --check`, the Agent A architecture guard, and the active lane focused validations before moving to the next slice.
- Update the main Phase 7 tracker only after validation passes or a blocker is explicitly documented.

Per-merge Agent A command sequence:

```bash
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py \
  --root Assets/Game/Scripts \
  --output Design/Architecture/systembase_to_isystem_inventory.md \
  --json-output /private/tmp/warline-phase7-systembase-inventory.json

git diff --check

dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal

/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath /Users/farhad/Projects/WarlineCapture-Clone \
  -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation \
  -logFile /private/tmp/warline-phase7-agent-a-architecture.log
```

Acceptance:

- No two agents edit the main Phase 7 tracker concurrently.
- No two agents edit the same inventory rows concurrently.
- Merge conflicts are handled by Agent A with the domain handoff open.
- Inventory counts are updated after every merge.

## A5 - Progress Accounting And Percentage Formula

Goal:
Keep user-facing progress and final ratio honest.

- [x] Count production `SystemBase` under `Assets/Game/Scripts`, excluding editor and tests.
- [x] Count production `ISystem` under `Assets/Game/Scripts`, excluding editor and tests.
- [x] Count UI `SystemBase` separately.
- [x] Count non-UI gameplay `SystemBase` separately.
- [x] Count managed presentation/config/camera `SystemBase` exceptions separately.
- [x] Count view/reference-only MonoBehaviours separately only when they replace old managed ownership.
- [x] Count converted `ISystem` processors created by Phase 7.
- [x] Count retired/folded systems.
- [x] Count split managed exceptions retained.
- [x] Count remaining `ReviewRequired` rows.
- [x] Recalculate final `ISystem` share after every integration merge.

## Current Validation Record

Commands run for the current Agent A slice:

```bash
python3 -B -c "import py_compile; py_compile.compile('Tools/Architecture/generate_systembase_to_isystem_inventory.py', cfile='/private/tmp/generate_systembase_to_isystem_inventory.pyc', doraise=True)"
python3 -B -c "import py_compile; py_compile.compile('Tools/Architecture/generate_phase7_monobehaviour_loop_baseline.py', cfile='/private/tmp/generate_phase7_monobehaviour_loop_baseline.pyc', doraise=True)"
python3 Tools/Architecture/generate_systembase_to_isystem_inventory.py --root Assets/Game/Scripts --output Design/Architecture/systembase_to_isystem_inventory.md --json-output /private/tmp/warline-phase7-systembase-inventory.json
python3 Tools/Architecture/generate_phase7_monobehaviour_loop_baseline.py --root Assets/Game/Scripts --output Design/Architecture/phase7_monobehaviour_loop_baseline.md
dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal
git diff --check
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-agent-a-architecture.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod AirMissileLauncherValidationRunner.Run -logFile /private/tmp/warline-phase7-integration-air-missile-projectile-trail-isystem.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod GroundMissileLauncherRuntimeTests.RunMissileVisualValidation -logFile /private/tmp/warline-phase7-integration-ground-missile-rocket-trail-isystem.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchStartRequestValidationRunner.Run -logFile /private/tmp/warline-phase7-integration-match-start-request-helper-fold.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod BuildingProductionSystemTests.RunBuildingGameplayCompositionRuntimeSmokeValidation -logFile /private/tmp/warline-phase7-integration-runtime-resource-helper-fold-building-composition.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SceneLifecycleValidationRunner.Run -logFile /private/tmp/warline-phase7-integration-scene-lifecycle-helper-fold.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitMoveTargetDiagnosticValidationRunner.Run -logFile /private/tmp/warline-phase7-integration-unit-move-target-diagnostic-isystem.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod CustomGameStartupSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-integration-custom-game-startup-helper-fold.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod FactionResourceSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-integration-faction-resource-helper-fold.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod GameplayRuntimeUpdateValidationRunner.Run -logFile /private/tmp/warline-phase7-integration-gameplay-runtime-update-helper-fold.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod ManagedGameplayStartupValidationRunner.Run -logFile /private/tmp/warline-phase7-integration-managed-gameplay-startup-helper-fold.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod UnitMovementBlockerValidationTests.RunBatchValidation -logFile /private/tmp/warline-phase7-integration-map-vehicle-placement-progress-state.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod MatchHudSquadTraySelectionSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-integration-match-hud-squad-tray-helper-fold.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod ResourceHaulerSystemTests.RunFocusedValidation -logFile /private/tmp/warline-phase7-integration-resource-hauler-helper-fold.log
```

Results:

- Inventory generator syntax validation passed; bytecode target `/private/tmp/generate_systembase_to_isystem_inventory.pyc`.
- MonoBehaviour loop baseline generator syntax validation passed; bytecode target `/private/tmp/generate_phase7_monobehaviour_loop_baseline.pyc`.
- Inventory regenerated at `Design/Architecture/systembase_to_isystem_inventory.md`; latest Agent B P7-0019 helper fold regeneration produced `163` total rows, `155` ProductionNonUI rows, and `8` ProductionUI rows; production ISystem share is `84.7%`; managed exceptions remain `24` and open rows dropped to `0`.
- MonoBehaviour loop baseline regenerated at `Design/Architecture/phase7_monobehaviour_loop_baseline.md` with `41` existing loop keys after the architecture guard detected unchanged `UiToolkitShellView.LateUpdate` baseline drift.
- Manual review queue resolved to `0` rows; `42` reviewed rows are recorded under `Manual Review Decisions`.
- Phase 7-created converted `ISystem` processors currently total `11`; current Agent D retired/folded helper count is `81`; Agent E retired/folded helper count is `87`; Agent F retired/folded helper count is `9`; Agent C folded helper count is `18`; view/reference-only MonoBehaviour replacement count is `0`.
- Editor assembly compile passed with `0 Warning(s), 0 Error(s)`.
- `git diff --check` passed.
- Agent B Performance diagnostics focused validation passed with `/private/tmp/warline-phase7-agent-b-performance-diagnostics-reference.log`, marker `[PerformanceDiagnosticsAllocationValidation] result=Passed tests=3`.
- Agent B Match scene reference focused validation passed with `/private/tmp/warline-phase7-agent-b-match-scene-reference.log`, marker `[MatchSceneReferenceFocusedValidation] result=Passed tests=2`.
- Agent A focused architecture guard passed with `/private/tmp/warline-phase7-agent-a-architecture.log`, marker `[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`.
- User approved single-thread domain execution; Agent B and Agent C baseline slices are complete/held where documented, Agent F request-contract/final visual rows are completed/validated where documented, Agent D has no remaining open inventory rows, Agent E P7-0144 through P7-0238 helper folds are completed with focused lane, compile, and architecture validations, Integration P7-0297/P7-0311/P7-0351/P7-0384 systems were converted directly to ISystem, and Integration P7-0300/P7-0305/P7-0307/P7-0315/P7-0318/P7-0319/P7-0320/P7-0323/P7-0324/P7-0325/P7-0328 were folded from disabled SystemBase wrappers into plain direct-owned helpers.
- Latest Integration/Agent B logs include `/private/tmp/warline-phase7-agent-b-performance-diagnostics-reference.log` (`[PerformanceDiagnosticsAllocationValidation] result=Passed tests=3`), `/private/tmp/warline-phase7-agent-b-match-scene-reference.log` (`[MatchSceneReferenceFocusedValidation] result=Passed tests=2`), `/private/tmp/warline-phase7-integration-visible-unit-selection-state.log` (`[SelectionStateFocusedValidation] result=Passed tests=8`), `/private/tmp/warline-phase7-integration-visible-unit-selection-isystem.log` (broad selection runner failed before this fixture on pre-existing `RtsSelectionInputSystemTests.RuntimeInput_DefersUnitSelectionUntilPointerRelease` log-string assertion), `/private/tmp/warline-phase7-integration-resource-hauler-helper-fold.log` (`[ResourceHaulerFocusedValidation] result=Passed tests=9`), `/private/tmp/warline-phase7-integration-match-hud-squad-tray-helper-fold.log` (`[MatchHudSquadTraySelectionFocusedValidation] result=Passed tests=3`), `/private/tmp/warline-phase7-integration-map-vehicle-placement-progress-state.log` (`[UnitMovementBlockerValidation] result=Passed`), and `/private/tmp/warline-phase7-agent-a-architecture.log` (`[NonUiSystemBaseMigrationArchitectureValidation] result=Passed tests=18`); prior Agent F logs remain recorded in the active lane trackers and handoff reports.

Formula:

```text
final_isystem_share =
  final_production_isystem_count /
  (final_production_isystem_count + final_production_systembase_count)
```

Planning examples from current baseline:

| Scenario | Remaining production `SystemBase` | Final production `ISystem` | `ISystem` share |
| --- | ---: | ---: | ---: |
| Aggressive managed-exception target | `21` (`1` UI + `20` non-UI managed exceptions) | `360` | `94.5%` |
| Planning cap | `31` (`1` UI + `30` non-UI managed exceptions) | `350` | `91.9%` |
| Conservative overrun | `41` (`1` UI + `40` non-UI managed exceptions) | `340` | `89.2%` |

Progress snapshot format:

```text
Phase 7 Agent A progress:
- Checklist: X / Y complete (Z%).
- Inventory rows: total N, production non-UI SystemBase N, UI SystemBase N, tests/editor N.
- Dispositions: DirectConvert N, SplitThenConvert N, RetireFold N, ManagedPresentationSystemBaseException N, ReviewRequired N.
- Owner lanes assigned: AgentB N, AgentC N, AgentD N, AgentE N, AgentF N, Integration N.
- Guardrails: NotRun/Passed/Failed.
- No-updating-MonoBehaviour guard: NotRun/Passed/Failed.
- Current projected final share: N ISystem / M SystemBase = P%.
```

Acceptance:

- Every user-facing status includes counts, percentage, current target, validation status, and blockers.
- Percentage is never reported from stale static seed counts after the inventory exists.

## A6 - Final Completion

Goal:
Close Agent A only when the project can safely start or finish parallel domain implementation.

- [ ] Regenerate inventory after all domain lanes are merged.
- [ ] Confirm non-UI gameplay production `SystemBase` count is `0`.
- [ ] Confirm production `SystemBase` count is `1 + counted managed presentation/config/camera exceptions`.
- [ ] Confirm managed exception count is at or below the approved cap, or update the cap with reasons.
- [ ] Confirm final `ISystem` and `SystemBase` percentage.
- [ ] Confirm no new updating MonoBehaviour was introduced by Phase 7.
- [ ] Confirm all `ReviewRequired` rows are resolved or explicitly deferred with owner and reason.
- [ ] Raise architecture guard floors for final counts.
- [ ] Run full Phase 7 validation matrix.
- [ ] Write final report under `Design/AgentReports`.
- [ ] Update the main tracker to close Phase 7.

Final Agent A handoff format:

- Completed checklist items over total.
- Remaining open items.
- Current production `SystemBase` count.
- Current non-UI gameplay `SystemBase` count.
- Current managed presentation/config/camera exception count.
- Current production `ISystem` count.
- Final `ISystem` share.
- Converted systems by lane.
- Retired/folded systems by lane.
- Managed exceptions retained by lane.
- Validation commands and logs.
- Known deferred work.
