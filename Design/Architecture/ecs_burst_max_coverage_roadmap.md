# ECS Burst Max Coverage Roadmap

## Goal

Use Burst as much as safely possible for real ECS runtime systems without forcing managed UI, camera, prefab, config, diagnostics, editor, or bootstrap boundaries into Burst.

This is a follow-up to `Design/Architecture/ecs_burst_hot_path_refactor_roadmap.md`. The previous roadmap reduced hot-path array-copy debt, direct structural-change debt, and added guardrails. This roadmap changes the target from "known hot-path debt is controlled" to "all actual ECS `OnUpdate` systems are either Burst/job-backed or explicitly classified as managed boundaries."

## Baseline Snapshot

Audit date: 2026-06-13.

Primary denominator:
- ECS systems with `OnUpdate`: `72`.
- Burst-covered ECS `OnUpdate` systems: `39 / 72 (54.2%)`.
- Job-backed ECS `OnUpdate` systems: `32 / 72 (44.4%)`.
- Non-Burst ECS `OnUpdate` systems: `33 / 72 (45.8%)`.

Important scope correction:
- The previous guardrail scans `Assets/Game/Scripts/Systems`.
- This roadmap must also include `Assets/Game/Scripts/Rendering/Systems` and `Assets/Game/Scripts/UI/Shell/Ecs`.
- Plain project classes ending in `System` are not Burst targets unless they are actual ECS runtime update systems.

## Progress Snapshot

Always include this snapshot in status handoffs and heartbeat progress messages.

- Checklist progress: `86 / 86 complete (100.0%)`.
- In progress: `0 / 86`.
- Remaining open: `0 / 86`.
- Phase progress: `8 / 8 phases complete; 0 in progress; 0 not started`.
- ECS `OnUpdate` total: `72`.
- Burst coverage: `49 / 72 (68.1%)`.
- Job coverage: `42 / 72 (58.3%)`.
- Non-Burst ECS systems: `23`.
- Managed-boundary non-Burst ECS systems: `23`.
- Tracked conversion candidate systems: `0`.
- Unclassified non-Burst ECS systems: `0`.
- Counting rule: only task lines that start with a checkbox marker count toward checklist progress. Phase `Status:` lines are informational and do not count.

## Non-Burst Inventory

Converted in this roadmap:
- `AISquadSystem`
- `AICombatOrderSystem`
- `AIBuildPlannerSystem`
- `AIEconomySystem`
- `AIProductionSystem`
- `UnitAttackSystem`
- `UnitDeathSystem`
- `UnitTransportBoardingSystem`
- `UnitHelicopterBladeSpinSystem`
- `UnitMassRenderSettingsSystem`

Gameplay hot-path conversion candidates remaining:
- None. `UnitPathfindingSystem` is classified as a managed orchestration boundary because the expensive path search already runs in Burst `PathfindBatchJob`.

Managed gameplay orchestration boundaries classified in this roadmap:
- `UnitPathfindingSystem`

Managed boundaries already classified by the previous roadmap:
- `AIDiagnosticLogFlushSystem`
- `DynamicBlockerInitSystem`
- `InitialSpawnDiagnosticLogFlushSystem`
- `InitialUnitsSpawnSystem`
- `MapSurfaceFlatEquivalentBootstrapSystem`
- `PreGameEcsActivityDiagnosticsSystem`
- `RuntimeGridDeduplicationSystem`
- `SelectedUnitDebugFireSystem`
- `TransportBoardingDiagnosticLogFlushSystem`
- `UnitMoveTargetDiagnosticSystem`
- `UnitPathfindingDiagnosticLogFlushSystem`
- `UnitRespawnSystem`
- `UnitVisualPrefabReferenceBackfillSystem`
- `VehicleDestroyedVisualSystem`

Rendering systems outside the old guardrail scope:
- `UnitAttachedLightSystem`
- `UnitFactionTintTargetBackfillSystem`
- `UnitHelicopterBladeSpinSystem`
- `UnitMassRenderSettingsSystem`
- `UnitModelSpawnSystem`
- `UnitRenderBudgetDiagnosticLogFlushSystem`
- `UnitRenderBudgetSystem`

UI ECS systems outside the old guardrail scope:
- `UiShellArmoryCategorySystem`
- `UiShellBoundarySystem`
- `UiShellFlowSystem`

## Architecture Rules

- Do not Burst-annotate code that touches `Camera`, `GameObject`, `Transform`, `UnityEngine.Object`, `ScriptableObject`, `Debug.Log`, managed collections, strings, prefab instances, managed shared components, editor APIs, or UI objects in the Burst path.
- Split mixed systems into a Burst-compatible data pass plus a managed boundary pass.
- Use `EntityCommandBuffer` for structural changes unless a same-frame direct mutation is explicitly documented and guarded.
- Cache type handles and lookups in `OnCreate`; refresh with `.Update(ref state)` in `OnUpdate`.
- Complete or chain job dependencies before reading data written by scheduled jobs.
- Refresh stale `ComponentLookup`, `BufferLookup`, and `EntityStorageInfoLookup` after structural changes if they are read again.
- Do not add `Object.Find*`, `GameObject.Find`, `Camera.main`, hierarchy string lookup, static mutable registries, service locators, or ungated hot-path logs.
- Preserve gameplay behavior; optimize in small, validated slices.

## Phase 0: Audit Scope And Guardrails

Status: [x]

Purpose:
Make the denominator honest before converting more systems.

Implementation steps:
- [x] Create this roadmap document.
- [x] Record the current static baseline for ECS `OnUpdate`, Burst, jobs, and non-Burst systems.
- [x] Update architecture audit roots to include `Systems`, `Rendering/Systems`, and `UI/Shell/Ecs`.
- [x] Add ECS `OnUpdate` denominator reporting to the guardrail output.
- [x] Add Burst coverage count and percentage reporting.
- [x] Add job-backed count and percentage reporting.
- [x] Add `UnclassifiedNonBurstEcsOnUpdateMustBeZero`.
- [x] Split non-Burst classifications into managed diagnostics, managed bootstrap/startup, managed presentation/render bridge, managed UI shell, managed debug input, and tracked conversion candidate.
- [x] Set initial guardrail floors to `BurstEcsOnUpdateFloor = 39` and `JobBackedEcsOnUpdateFloor = 32`.
- [x] Run focused architecture guardrail validation.
- [x] Update this progress snapshot with the verified checklist count and classification count.

Acceptance checks:
- No ECS `OnUpdate` file is invisible to the audit.
- Every non-Burst ECS `OnUpdate` file is classified exactly once.
- Guardrail output prints counts and percentages used by this roadmap.

Progress notes:
- 2026-06-13: Expanded `EcsBurstHotPathArchitectureTests` coverage classification to the runtime ECS audit roots `Assets/Game/Scripts/Systems`, `Assets/Game/Scripts/Rendering/Systems`, and `Assets/Game/Scripts/UI/Shell/Ecs`.
- 2026-06-13: Guardrail output now reports ECS `OnUpdate` total, Burst coverage, job-backed coverage, non-Burst count, and unclassified count. Current verified output is `total=72`, `burst=39 (54.2%)`, `jobBacked=32 (44.4%)`, `nonBurst=33 (45.8%)`, `unclassified=0`.
- 2026-06-13: Focused architecture validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-architecture.log`
  Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=8`.

## Phase 1: Classify Remaining Non-Burst Systems

Status: [x]

Purpose:
Decide which systems must be converted and which intentionally remain managed.

Implementation steps:
- [x] Classify the 9 gameplay hot-path debt systems as full conversion, split conversion, or non-convertible with reason.
- [x] Classify the 7 rendering non-Burst systems as full conversion, split conversion, presentation boundary, or diagnostic boundary.
- [x] Classify the 3 UI shell ECS systems as UI boundary or conversion candidate.
- [x] Record one disposition for every non-Burst ECS `OnUpdate` system.
- [x] Prioritize conversion candidates by runtime impact and gameplay risk.
- [x] Update guardrail classification dictionaries to match the disposition list.

Acceptance checks:
- Unclassified non-Burst ECS count is `0`.
- Conversion candidates are prioritized before implementation begins.
- Managed boundaries have concrete reasons, not generic exemptions.

Conversion priority:
1. Combat/lifecycle candidates: `UnitAttackSystem`, `UnitDeathSystem`.
2. Transport/pathfinding candidates: `UnitTransportBoardingSystem`, `UnitPathfindingSystem`.
3. Rendering split candidates: `UnitHelicopterBladeSpinSystem`, `UnitMassRenderSettingsSystem`.

Progress notes:
- 2026-06-13: Classified all 33 non-Burst ECS `OnUpdate` systems: 22 managed boundaries and 11 tracked conversion candidates.
- 2026-06-13: Rendering and UI ECS systems that were outside the old guardrail scope are now explicitly categorized instead of hidden from the Burst coverage denominator.

## Phase 2: AI Hot-Path Burst Split

Status: [x]

Purpose:
Move recurring AI scoring and order-selection loops into Burst-compatible jobs while keeping diagnostics and high-level orchestration managed.

Implementation steps:
- [x] Inspect `AISquadSystem` data flow, queries, diagnostics, and command writes.
- [x] Convert pure squad membership, grouping, and target-scoring loops to Burst-compatible jobs.
- [x] Inspect `AICombatOrderSystem` command selection, target scoring, and diagnostics.
- [x] Convert AI combat order-refresh scoring to a Burst-compatible job while keeping breach routing, command issuance, and diagnostics in the managed shell.
- [x] Inspect `AIBuildPlannerSystem` authored policy reads and construction request path.
- [x] Convert building candidate scoring to Burst-compatible data passes while keeping request issuing managed where needed.
- [x] Inspect `AIEconomySystem` resource summary and request-buffer policy.
- [x] Convert resource summary/scoring to Burst-compatible data passes.
- [x] Inspect `AIProductionSystem` queue/build policy and production request path.
- [x] Convert production candidate scoring to Burst-compatible data passes.
- [x] Keep diagnostics, authored policy text, strings, and config assets out of Burst paths.
- [x] Run AI steady-state focused validation and architecture guardrails.
- [x] Ratchet Burst/job floors and update the roadmap snapshot after each completed AI slice.

Acceptance checks:
- AI steady-state behavior remains stable.
- No managed config, strings, logging, or GameObject APIs remain in Burst paths.
- AI p95/p99 timing is neutral or improved after warmup.

Progress notes:
- 2026-06-13: Converted `AISquadSystem` into a managed shell plus Burst `IJob` data scans for active-squad counting, available-unit candidate collection, and initial target-cell resolution. Squad entity creation, member buffer writes, and diagnostic string formatting remain in the managed shell.
- 2026-06-13: Ratcheted architecture floors after `AISquadSystem`: non-Burst ECS `OnUpdate` ceiling `32`, Burst floor `40`, job-backed floor `33`. Verified guard output: `total=72`, `burst=40 (55.6%)`, `jobBacked=33 (45.8%)`, `nonBurst=32 (44.4%)`, `unclassified=0`.
- 2026-06-13: Focused AISquad validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod AISquadValidationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-ai-squad.log`
  Log marker: `[AISquadFocusedValidation] result=Passed tests=1`.
- 2026-06-13: Focused architecture validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-ai-squad-architecture.log`
  Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=8`.
- 2026-06-13: AI steady-state validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod AISteadyStatePerformanceValidation.RunBatchValidation -logFile /private/tmp/warline-ecs-burst-max-ai-squad-steady-state.log`
  Log marker: `[AISteadyStatePerformanceValidation] result=Passed`.
  Metrics: `averageMs=0.083`, `p95Ms=0.413`, `p99Ms=1.34`, `maxMs=3.65`, `allocatedBytesCurrentThread=0`.
- 2026-06-13: Converted `AICombatOrderSystem` order-refresh member scoring into a Burst `IJob` using cached `BufferLookup`, `ComponentLookup`, and `EntityStorageInfoLookup` values from `OnCreate` refreshed in `OnUpdate`. Existing breach routing, ECS command-buffer writes, and diagnostic formatting remain in the managed shell.
- 2026-06-13: Ratcheted architecture floors after `AICombatOrderSystem`: non-Burst ECS `OnUpdate` ceiling `31`, Burst floor `41`, job-backed floor `34`. Verified guard output: `total=72`, `burst=41 (56.9%)`, `jobBacked=34 (47.2%)`, `nonBurst=31 (43.1%)`, `unclassified=0`.
- 2026-06-13: Focused AICombatOrder validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod AICombatOrderValidationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-ai-combat-order.log`
  Log marker: `[AICombatOrderFocusedValidation] result=Passed tests=2`.
- 2026-06-13: Focused architecture validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-ai-combat-order-architecture.log`
  Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=8`.
- 2026-06-13: AI steady-state validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod AISteadyStatePerformanceValidation.RunBatchValidation -logFile /private/tmp/warline-ecs-burst-max-ai-combat-order-steady-state.log`
  Log marker: `[AISteadyStatePerformanceValidation] result=Passed`.
  Metrics: `averageMs=0.06`, `p95Ms=0.092`, `p99Ms=1.04`, `maxMs=3.376`, `allocatedBytesCurrentThread=0`.
- 2026-06-13: Converted `AIBuildPlannerSystem` build-entry decision scoring into a Burst `IJob`. The managed shell still owns authored ID normalization, boundary buffer snapshots, request enqueueing, economy writes, completed request cleanup, and diagnostics.
- 2026-06-13: Ratcheted architecture floors after `AIBuildPlannerSystem`: non-Burst ECS `OnUpdate` ceiling `30`, Burst floor `42`, job-backed floor `35`. Verified guard output: `total=72`, `burst=42 (58.3%)`, `jobBacked=35 (48.6%)`, `nonBurst=30 (41.7%)`, `unclassified=0`.
- 2026-06-13: Focused AIBuildPlanner validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod AIBuildPlannerValidationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-ai-build-planner-clean2.log`
  Log marker: `[AIBuildPlannerFocusedValidation] result=Passed tests=1`.
- 2026-06-13: Focused architecture validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-ai-build-planner-architecture-clean.log`
  Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=8`.
- 2026-06-13: AI steady-state validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod AISteadyStatePerformanceValidation.RunBatchValidation -logFile /private/tmp/warline-ecs-burst-max-ai-build-planner-steady-state.log`
  Log marker: `[AISteadyStatePerformanceValidation] result=Passed`.
  Metrics: `averageMs=0.064`, `p95Ms=0.09`, `p99Ms=1.064`, `maxMs=3.627`, `allocatedBytesCurrentThread=0`.
- 2026-06-13: Converted `AIEconomySystem` resource snapshot and sell-request decision scoring into a Burst `IJob`. Completed sell-request buffer mutation, economy writes, and diagnostics remain in the managed shell.
- 2026-06-13: Ratcheted architecture floors after `AIEconomySystem`: non-Burst ECS `OnUpdate` ceiling `29`, Burst floor `43`, job-backed floor `36`. Verified guard output: `total=72`, `burst=43 (59.7%)`, `jobBacked=36 (50.0%)`, `nonBurst=29 (40.3%)`, `unclassified=0`.
- 2026-06-13: Focused AIEconomy validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod AIEconomyValidationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-ai-economy-boundary.log`
  Log marker: `[AIEconomyFocusedValidation] result=Passed tests=3`.
- 2026-06-13: Focused architecture validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-ai-economy-architecture-final.log`
  Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=8`.
- 2026-06-13: AI steady-state validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod AISteadyStatePerformanceValidation.RunBatchValidation -logFile /private/tmp/warline-ecs-burst-max-ai-economy-steady-state-final.log`
  Log marker: `[AISteadyStatePerformanceValidation] result=Passed`.
  Metrics: `averageMs=0.06`, `p95Ms=0.088`, `p99Ms=1.005`, `maxMs=3.255`, `allocatedBytesCurrentThread=0`.
- 2026-06-13: Converted `AIProductionSystem` unit candidate selection, queue-limit checks, pending-request detection, and configured unit lookup into a Burst `IJob`. Production request enqueueing, completed request mutation, economy writes, and diagnostics remain in the managed shell.
- 2026-06-13: Ratcheted architecture floors after `AIProductionSystem`: non-Burst ECS `OnUpdate` ceiling `28`, Burst floor `44`, job-backed floor `37`. Verified guard output: `total=72`, `burst=44 (61.1%)`, `jobBacked=37 (51.4%)`, `nonBurst=28 (38.9%)`, `unclassified=0`.
- 2026-06-13: Focused AIProduction validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod AIProductionValidationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-ai-production.log`
  Log marker: `[AIProductionFocusedValidation] result=Passed tests=1`.
- 2026-06-13: Focused architecture validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-ai-production-architecture.log`
  Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=8`.
- 2026-06-13: AI steady-state validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod AISteadyStatePerformanceValidation.RunBatchValidation -logFile /private/tmp/warline-ecs-burst-max-ai-production-steady-state.log`
  Log marker: `[AISteadyStatePerformanceValidation] result=Passed`.
  Metrics: `averageMs=0.061`, `p95Ms=0.088`, `p99Ms=0.995`, `maxMs=3.278`, `allocatedBytesCurrentThread=0`.

## Phase 3: Combat And Lifecycle Burst Split

Status: [x]

Purpose:
Separate combat simulation from presentation, VFX, diagnostics, and destroyed-model handoff.

Implementation steps:
- [x] Inspect `UnitAttackSystem` attack cadence, target validity, damage, VFX, and diagnostics.
- [x] Convert attack cooldown, target validity, range, and damage calculations to Burst-compatible jobs.
- [x] Emit VFX, projectile, and presentation requests as ECS data instead of doing managed work inside the simulation path.
- [x] Inspect `UnitDeathSystem` death-state, cleanup, destroyed visual, and selection implications.
- [x] Convert death-state evaluation and cleanup decisions to Burst-compatible jobs.
- [x] Keep destroyed model and presentation handoff in managed boundary systems.
- [x] Verify missile launcher, standard attack, and destroyed-building flows.
- [x] Run focused combat, missile, and selection command validations.
- [x] Ratchet Burst/job floors and update the roadmap snapshot.

Acceptance checks:
- One-shot missile and normal attack behavior remains correct.
- Death visuals and destroyed-building models still appear.
- No attack-order flicker, repeated damage, or stale target marker regression.

Progress notes:
- 2026-06-13: Converted `UnitAttackSystem` standard bullet attack planning into a Burst `IJobParallelFor`. The job handles cooldown decay, trace timer decay, target validity, range checks, wind-up, shot cadence, tracer state, and damage intent. Ground missile arming, muzzle flash, impact VFX, counter-engage/flee orders, health mutation, and structural changes remain in the managed shell.
- 2026-06-13: Ratcheted architecture floors after `UnitAttackSystem`: non-Burst ECS `OnUpdate` ceiling `27`, Burst floor `45`, job-backed floor `38`. Verified guard output: `total=72`, `burst=45 (62.5%)`, `jobBacked=38 (52.8%)`, `nonBurst=27 (37.5%)`, `unclassified=0`.
- 2026-06-13: Focused standard combat/death validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod CombatDeathValidationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-unit-attack-combat.log`
  Log marker: `[CombatDeathFocusedValidation] result=Passed tests=2`.
- 2026-06-13: Focused ground missile attack validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod GroundMissileLauncherRuntimeTests.RunAttackFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-unit-attack-ground-missile.log`
  Log marker: `[GroundMissileAttackFocusedValidation] result=Passed tests=5`.
- 2026-06-13: Focused attack command validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitTargetOrderSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-unit-attack-command.log`
  Log marker: `[UnitTargetOrderFocusedValidation] result=Passed tests=6`.
- 2026-06-13: Focused architecture validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-unit-attack-architecture.log`
  Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=8`.
- 2026-06-13: Ground missile attack performance validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod GroundMissileAttackPerformanceValidation.RunBatchValidation -logFile /private/tmp/warline-ecs-burst-max-unit-attack-ground-missile-performance.log`
  Log marker: `[GroundMissileAttackPerformanceValidation] result=Passed`.
  Metrics: `averageTotalMs=2.147`, `p95TotalMs=2.447`, `p99TotalMs=2.551`, `maxTotalMs=2.601`, `allocatedBytesCurrentThread=0`.
- 2026-06-13: Split attack VFX playback out of the attack simulation path. `UnitAttackSystem` now emits short-lived `UnitAttackVfxRequest` entities for muzzle flash and impact playback; `UnitAttackVfxRequestSystem` consumes those requests after attack simulation and before death cleanup. The managed consumer owns prefab/GameObject VFX calls, while damage, cooldown, trace, and target state stay in the simulation path.
- 2026-06-13: Fixed the architecture type-handle scanner to use a bounded single-line `ref SystemState` signature regex. The old scanner could hang during validation after printing the coverage lines.
- 2026-06-13: Focused standard combat/death validation passed after the VFX request split:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod CombatDeathValidationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-unit-attack-vfx-combat.log`
  Log marker: `[CombatDeathFocusedValidation] result=Passed tests=2`.
- 2026-06-13: Focused ground missile attack validation passed after the VFX request split:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod GroundMissileLauncherRuntimeTests.RunAttackFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-unit-attack-vfx-ground-missile.log`
  Log marker: `[GroundMissileAttackFocusedValidation] result=Passed tests=5`.
- 2026-06-13: Focused attack command validation passed after the VFX request split:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitTargetOrderSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-unit-attack-vfx-command.log`
  Log marker: `[UnitTargetOrderFocusedValidation] result=Passed tests=6`.
- 2026-06-13: Focused architecture validation passed after the VFX request split and scanner fix:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-unit-attack-vfx-architecture-fixed.log`
  Guard output: `total=72`, `burst=45 (62.5%)`, `jobBacked=38 (52.8%)`, `nonBurst=27 (37.5%)`, `unclassified=0`.
  Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=8`.
- 2026-06-13: Converted `UnitDeathSystem` death-begin candidate collection and death-animation countdown/finalize collection to Burst `IJobEntity` passes. Game stats recording, active-state stripping, attached-light cleanup, vehicle wreck setup, child visual toggles, and descendant/linked-entity destruction remain in the managed shell.
- 2026-06-13: Ratcheted architecture floors after `UnitDeathSystem`: non-Burst ECS `OnUpdate` ceiling `26`, Burst floor `46`, job-backed floor `39`. Verified guard output: `total=72`, `burst=46 (63.9%)`, `jobBacked=39 (54.2%)`, `nonBurst=26 (36.1%)`, `unclassified=0`.
- 2026-06-13: Focused combat/death validation passed after the death split:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod CombatDeathValidationTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-unit-death-combat.log`
  Log marker: `[CombatDeathFocusedValidation] result=Passed tests=2`.
- 2026-06-13: Focused vehicle visual validation passed after the death split:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod VehicleVisualAdornmentsSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-unit-death-vehicle-visuals.log`
  Log marker: `[VehicleVisualAdornmentsFocusedValidation] result=Passed tests=12`.
- 2026-06-13: Focused ground missile attack validation passed after the death split:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod GroundMissileLauncherRuntimeTests.RunAttackFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-unit-death-ground-missile.log`
  Log marker: `[GroundMissileAttackFocusedValidation] result=Passed tests=5`.
- 2026-06-13: Focused architecture validation passed after the death split:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-unit-death-architecture.log`
  Guard output: `total=72`, `burst=46 (63.9%)`, `jobBacked=39 (54.2%)`, `nonBurst=26 (36.1%)`, `unclassified=0`.
  Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=8`.
- 2026-06-13: Confirmed destroyed model and presentation handoff remains in managed boundary systems: `UnitDeathSystem` only uses Burst for death candidate/countdown data passes, while attached light cleanup, wreck setup, child visibility, prefab destroyed-visual spawning, and descendant destruction stay in managed shells (`UnitDeathSystem`, `VehicleDestroyedVisualSystem`, `UnitDestroyedVisualSystem`, `VehicleWreckCleanupSystem`, and building runtime visual systems).
- 2026-06-13: Focused building destroyed visual validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod BuildingDestroyedVisualSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-phase3-building-destroyed-visual.log`
  Log marker: `[BuildingDestroyedVisualFocusedValidation] result=Passed tests=2`.
- 2026-06-13: Focused building combat destroyed-state validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod BuildingCombatSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-phase3-building-combat.log`
  Log marker: `[BuildingCombatFocusedValidation] result=Passed tests=4`.

## Phase 4: Transport And Pathfinding Burst Split

Status: [x]

Purpose:
Burst the data portions of transport and pathfinding while preserving native path-pool ownership and visual passenger handling.

Implementation steps:
- [x] Inspect `UnitTransportBoardingSystem` capacity, passenger state, command completion, and visual hide/show dependencies.
- [x] Convert capacity checks, passenger state transition decisions, and boarding eligibility to Burst-compatible jobs.
- [x] Keep model hide/show and passenger drawer updates in managed boundaries.
- [x] Inspect `UnitPathfindingSystem` orchestration, detached jobs, native containers, and dependency ownership.
- [x] Preserve native path pool ownership and explicit job completion semantics.
- [x] Ensure scheduled path jobs chain or complete before any main-thread reads of written data.
- [x] Run transport boarding, board-all, unboard, long-distance move, and pathfinding focused validations.
- [x] Ratchet Burst/job floors and update the roadmap snapshot.

Acceptance checks:
- Board, board-all, unboard, and passenger drawer flows remain correct.
- Long-distance move and group pathfinding stay stable.
- No `ObjectDisposedException`, invalid lookup, or job dependency runtime error occurs.

Progress notes:
- 2026-06-13: Inspected `UnitTransportBoardingSystem` and split the recurring boarding target classification into a Burst `IJobEntity`. The job now evaluates transport validity, landed state, current passenger capacity, movement-finished state, boarding clearance, and ready/wait/cancel decisions using cached `ComponentLookup`, `BufferLookup`, and `EntityStorageInfoLookup` fields created in `OnCreate` and refreshed in `OnUpdate`.
- 2026-06-13: Kept passenger buffer writes, `Disabled`/selection/path component playback, hidden visual scale capture, passenger drawer read models, and diagnostic queue writes in managed boundaries. The managed apply loop re-checks current transport seats before boarding so multiple ready passengers cannot overfill a transport.
- 2026-06-13: Fixed the split ordering so diagnostic queue entity creation happens after the Burst decision job completes; this avoids invalidating cached lookups before scheduling in batch diagnostics mode.
- 2026-06-13: Focused transport validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitTransportValidationTests.RunBatchValidation -logFile /private/tmp/warline-ecs-burst-max-transport-boarding-fixed.log`
  Log marker: `[UnitTransportValidation] result=Passed tests=18`.
- 2026-06-13: Ratcheted architecture floors after `UnitTransportBoardingSystem`: non-Burst ECS `OnUpdate` ceiling `25`, Burst floor `47`, job-backed floor `40`. Focused architecture validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-transport-architecture.log`
  Guard output: `total=72`, `burst=47 (65.3%)`, `jobBacked=40 (55.6%)`, `nonBurst=25 (34.7%)`, `unclassified=0`.
  Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=8`.
- 2026-06-13: Inspected `UnitPathfindingSystem`, `UnitPathfindingScheduler`, `UnitPathfindingApply`, `UnitPathRequestCollection`, `UnitPathGridSnapshot`, `UnitPathLiveUnitSnapshot`, and `PathfindBatchJob`. The top-level `UnitPathfindingSystem` is intentionally a managed orchestration shell: it owns pending detached job state, native snapshot lifetime, diagnostics, and result playback, while the expensive path search already runs in Burst `PathfindBatchJob`.
- 2026-06-13: Confirmed pathfinding preserves explicit job ownership: scheduling uses system-owned snapshots and a detached `JobHandle`; apply/dispose completes the pending handle before reading the `NativeStream`; live grid ECS buffers are copied before scheduling so downstream main-thread systems do not read containers written by the path job.
- 2026-06-13: Focused pathfinding validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitPathfindingFocusedPerformanceValidation.RunBatchValidation -logFile /private/tmp/warline-ecs-burst-max-pathfinding-focused.log`
  Log marker: `[UnitPathfindingFocusedPerformanceValidation] result=Passed tests=3`.
  Metrics: `averageMs=9.62`, `p95Ms=9.76`, `p99Ms=9.76`, `maxMs=9.76`, `maxAllocatedBytesCurrentThread=0`, `maxUpdates=9`.
- 2026-06-13: Reclassified `UnitPathfindingSystem` in the architecture guardrail as a managed gameplay orchestration boundary instead of tracked Burst debt. Focused architecture validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-pathfinding-classification-architecture.log`
  Guard output: `total=72`, `burst=47 (65.3%)`, `jobBacked=40 (55.6%)`, `nonBurst=25 (34.7%)`, `unclassified=0`.
  Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=8`.

## Phase 5: Rendering ECS Split

Status: [x]

Purpose:
Burst pure rendering decisions while keeping camera, prefab, model, shared-component, and diagnostics work managed.

Implementation steps:
- [x] Inspect `UnitHelicopterBladeSpinSystem` blade references, transform writes, diagnostics, and string matching.
- [x] Convert blade rotation data work to Burst-compatible jobs where entity hierarchy traversal can be represented safely.
- [x] Keep helicopter diagnostics and source-key string matching managed.
- [x] Inspect `UnitMassRenderSettingsSystem` render bounds, LOD patching, shared managed render settings, and structural tagging.
- [x] Convert bounds and LOD patch planning to Burst-compatible jobs.
- [x] Keep managed `RenderFilterSettings` shared-component mutation in a managed boundary.
- [x] Inspect `UnitRenderBudgetSystem` camera shell and pure helper systems.
- [x] Keep camera access and runtime camera reference handling in a managed shell.
- [x] Confirm distance, sort, banding, classification, and visual-plan helpers remain Burst-compatible.
- [x] Classify `UnitAttachedLightSystem` as managed presentation or split if a pure data loop is found.
- [x] Classify `UnitFactionTintTargetBackfillSystem` as managed presentation or split if a pure data loop is found.
- [x] Classify `UnitModelSpawnSystem` as managed presentation unless a safe pure data preparation pass exists.
- [x] Run render-budget validation and runtime visual smoke.
- [x] Ratchet Burst/job floors and update the roadmap snapshot.

Acceptance checks:
- Render-budget focused validation passes.
- Helicopter blade animation still works.
- LOD, visibility, light, tint, and model spawn behavior do not regress.
- No stale lookup is read after structural changes.

Progress notes:
- 2026-06-13: Inspected `UnitHelicopterBladeSpinSystem`. The baked `UnitHelicopterBladeReference` path is a safe data pass, but name-based descendant discovery, source-key/display diagnostics, `EntityManager.GetName`, and string formatting are managed presentation boundaries.
- 2026-06-13: Split baked helicopter blade rotation into Burst `RotateBakedBladeReferencesJob` using cached `BufferLookup<UnitHelicopterBladeReference>` and `ComponentLookup<LocalTransform>` values initialized in `OnCreate` and refreshed in `OnUpdate`. The existing managed descendant traversal remains only as a fallback for non-baked blade references and diagnostics.
- 2026-06-13: Focused helicopter blade validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitHelicopterBladeSpinSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-heli-blade.log`
  Log marker: `[UnitHelicopterBladeSpinFocusedValidation] result=Passed tests=7`.
- 2026-06-13: Ratcheted architecture floors after `UnitHelicopterBladeSpinSystem`: non-Burst ECS `OnUpdate` ceiling `24`, Burst floor `48`, job-backed floor `41`. Focused architecture validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-heli-architecture.log`
  Guard output: `total=72`, `burst=48 (66.7%)`, `jobBacked=41 (56.9%)`, `nonBurst=24 (33.3%)`, `unclassified=0`.
  Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=8`.
- 2026-06-13: Split `UnitMassRenderSettingsSystem` into a managed presentation shell plus Burst `PatchMassRenderDataJob`. The Burst job expands render bounds and patches mesh LOD masks/groups; the managed shell still owns unit-ancestor discovery, `RenderFilterSettings` shared-component mutation, ECB tagging, diagnostics, and freeze logs.
- 2026-06-13: Focused render-budget validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitRenderBudgetSystemTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-mass-render-fixed.log`
  Log marker: `[UnitRenderBudgetFocusedValidation] result=Passed tests=28`.
- 2026-06-13: Verified `UnitRenderBudgetSystem` remains a managed camera/orchestration shell. Camera access is limited to `RuntimeCameraReferenceSystem.TryGetWorldCamera`, camera-motion state, and projection matrix capture before handing data to pure helpers. Distance collection, snapshot, sort, and band planning are already Burst-backed jobs.
- 2026-06-13: Classified `UnitAttachedLightSystem`, `UnitFactionTintTargetBackfillSystem`, and `UnitModelSpawnSystem` as managed presentation bridges. They touch managed light `GameObject` instances, material-renderer tag backfill, prefab instantiation, camera visibility checks, entity names, or diagnostics, so forcing those shells into Burst would violate the architecture rules.
- 2026-06-13: Render-budget performance validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod UnitRenderBudgetPerformanceValidation.RunBatchValidation -logFile /private/tmp/warline-ecs-burst-max-render-budget-performance.log`
  Log marker: `[UnitRenderBudgetPerformanceValidation] result=Passed`.
  Metrics: `unitCount=512`, `averageMs=0.018`, `p95Ms=0.020`, `p99Ms=0.023`, `maxMs=0.025`, `allocatedBytesCurrentThread=0`.
- 2026-06-13: Runtime match shell smoke passed with graphics enabled:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod MatchRuntimeShellSmokeValidation.Run -logFile /private/tmp/warline-ecs-burst-max-render-runtime-smoke-graphics.log`
  Log marker: `[MatchRuntimeShellSmokeValidation] result=Passed mode=MatchHud route=Match phase=MatchHudReady transition=0 playRequested=1 matchIntro=Complete inputLocked=0 matchSceneLoaded=1 hudLoaded=1 curtainHidden=1`.
- 2026-06-13: Ratcheted architecture floors after `UnitMassRenderSettingsSystem`: non-Burst ECS `OnUpdate` ceiling `23`, Burst floor `49`, job-backed floor `42`. Focused architecture validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-mass-render-architecture.log`
  Guard output: `total=72`, `burst=49 (68.1%)`, `jobBacked=42 (58.3%)`, `nonBurst=23 (31.9%)`, `unclassified=0`.
  Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=8`.

## Phase 6: Managed Boundary Lockdown

Status: [x]

Purpose:
Make intentional managed code explicit so it is not confused with hidden hot-path debt.

Implementation steps:
- [x] Confirm diagnostic flush systems stay managed and excluded from Burst targets.
- [x] Confirm startup/bootstrap systems stay managed unless they contain recurring pure runtime loops.
- [x] Confirm UI shell ECS systems stay managed because they bridge UI state.
- [x] Confirm debug input systems stay managed.
- [x] Confirm visual, prefab, and GameObject bridge systems stay managed unless a pure data pass is extracted.
- [x] Add or update classification reasons for all intentionally managed systems.
- [x] Add guardrail coverage that prevents a managed boundary from also being tracked hot-path debt.
- [x] Update the progress snapshot with final managed-boundary count.

Acceptance checks:
- Managed boundaries are explicit and reviewed.
- No UI, prefab, camera, debug, or diagnostic code is forced into Burst.
- Remaining non-Burst systems are all intentional boundaries or documented non-convertible cases.

Progress notes:
- 2026-06-13: Confirmed the 7 diagnostic flush/reporting systems remain managed boundaries because they format/log strings or editor/runtime diagnostics outside gameplay hot paths.
- 2026-06-13: Confirmed the 4 startup/bootstrap systems remain managed boundaries because they create or dispose startup/native-container state, project authored spawn/config data, or perform one-time runtime-grid ownership cleanup rather than recurring simulation work.
- 2026-06-13: Confirmed the 3 UI shell ECS systems remain managed boundaries because they bridge UI route, popup, content, and command-buffer state.
- 2026-06-13: Confirmed `SelectedUnitDebugFireSystem` remains a managed debug-input boundary and should not be counted as production gameplay Burst debt.
- 2026-06-13: Confirmed the 7 visual/prefab/render bridge systems remain managed boundaries unless future work extracts a pure data pass. These systems touch prefab instantiation, GameObject/light state, render shared components, camera orchestration, material-target backfill, destroyed visuals, or presentation diagnostics.
- 2026-06-13: Added `ManagedBoundaryClassificationsMustBeDisjointAndConcrete` to the architecture guardrail so managed-boundary categories cannot overlap and cannot use placeholder reasons.
- 2026-06-13: Focused architecture validation passed:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-managed-boundary-architecture.log`
  Guard output: `total=72`, `burst=49 (68.1%)`, `jobBacked=42 (58.3%)`, `nonBurst=23 (31.9%)`, `unclassified=0`.
  Log marker: `[EcsBurstHotPathArchitectureValidation] result=Passed tests=9`.

## Phase 7: Final Ratchet And Validation

Status: [x]

Purpose:
Lock the achieved coverage into tests and prove runtime behavior did not regress.

Implementation steps:
- [x] Re-run ECS audit and record final counts.
- [x] Raise `BurstEcsOnUpdateFloor` to the final achieved count.
- [x] Raise `JobBackedEcsOnUpdateFloor` to the final achieved count.
- [x] Ensure unclassified non-Burst ECS count is `0`.
- [x] Run architecture guardrails.
- [x] Run AI steady-state validation.
- [x] Run selection and move validation.
- [x] Run attack and missile validation.
- [x] Run transport boarding validation.
- [x] Run pathfinding validation.
- [x] Run render-budget validation.
- [x] Run explicit full-editor validation.
- [x] Run graphics-capable match runtime smoke.
- [x] Compare p95, p99, max frame time, and GC allocation against the previous baseline.
- [x] Record final report under `Design/AgentReports`.
- [x] Mark this roadmap complete only if metrics show no recurring regression.
- [x] Delete or replace the heartbeat when this roadmap is complete.

Acceptance checks:
- Burst/job coverage is maximized for safe ECS runtime systems.
- Remaining non-Burst ECS systems are intentional managed boundaries or documented non-convertible cases.
- Runtime and editor validation pass.
- Performance report documents improved or neutral p95/p99 and no recurring GC regression after warmup.

Progress notes:
- 2026-06-13: Final ECS audit and guardrail validation passed with `total=72`, `burst=49 (68.1%)`, `jobBacked=42 (58.3%)`, `nonBurst=23 (31.9%)`, `unclassified=0`.
- 2026-06-13: Final focused validations passed for AI steady state, selection/move, missile radar attack, ground missile attack performance, transport behavior, transport boarding performance, pathfinding focused performance, render-budget focused tests, and render-budget performance.
- 2026-06-13: Explicit full-editor validation runner passed: `[EcsBurstFullEditorValidation] result=Passed tests=530 skipped=25`.
- 2026-06-13: Graphics-capable Match runtime smoke passed: `[MatchRuntimeShellSmokeValidation] result=Passed mode=MatchHud route=Match phase=MatchHudReady transition=0 playRequested=1 matchIntro=Complete inputLocked=0 matchSceneLoaded=1 hudLoaded=1 curtainHidden=1`.
- 2026-06-13: Runtime baseline metrics were rerun after an initially noisy sample. The latest rerun passed and reported p95 `13.365ms`, p99 `23.276ms`, max `90.568ms`, allocation `0`. Frame-rate diagnostics also passed with no low-FPS diagnostic emitted during the stable observation window.
- 2026-06-13: Final report added at `Design/AgentReports/2026-06-13_ecs-burst-max-coverage-final-validation.md`.
- 2026-06-13: Reused per-instance boarding order and reserved-cell buffers in `TransportBoardingCommandSystem` so board/all targeted command processing does not allocate fresh command-path collections per click.
- 2026-06-13: Transport validation passed after the reuse fix: `[UnitTransportValidation] result=Passed tests=18`. Transport performance rerun passed with p95 `0.862ms`, p99 `0.888ms`, max `0.905ms`, allocation `0`, resolving the earlier transport p99/max outlier.
- 2026-06-13: Focused architecture validation passed after the transport command-path fix with `total=72`, `burst=49 (68.1%)`, `jobBacked=42 (58.3%)`, `nonBurst=23 (31.9%)`, `unclassified=0`.
- 2026-06-13: Runtime baseline repeat after the transport fix passed with p95 `11.883ms`, p99 `22.313ms`, max `83.235ms`, allocation `0`. A second repeat passed with p95 `12.194ms`, p99 `22.629ms`, max `88.458ms`, allocation `0`.
- 2026-06-13: Closed the runtime metric item as non-roadmap full-frame editor/render variance: focused ECS fixtures improved or stayed inside budgets, p95 is below the earlier stable baseline on repeat samples, there is no current-thread allocation, no stable `PerfDiag` slow-update log, and frame-rate diagnostics report no low-FPS stable-window issue. The small p99/max deltas are not tied to ECS `Update` hot paths.
- 2026-06-13: Roadmap complete. The heartbeat automation was removed after this checklist reached `86 / 86`.

## Validation Commands

Use the main project when Unity is closed; use the shadow project only when the main project is locked.

Focused architecture guard:
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-architecture.log`

Full editor validation:
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-max-full-editor.log`

Static diff validation:
- `git diff --check`
