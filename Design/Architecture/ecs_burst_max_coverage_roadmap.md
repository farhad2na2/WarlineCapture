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

- Checklist progress: `2 / 86 complete (2.3%)`.
- In progress: `0 / 86`.
- Remaining open: `84 / 86`.
- Phase progress: `0 / 8 phases complete; 1 in progress; 7 not started`.
- ECS `OnUpdate` total: `72`.
- Burst coverage: `39 / 72 (54.2%)`.
- Job coverage: `32 / 72 (44.4%)`.
- Non-Burst ECS systems: `33`.
- Unclassified non-Burst ECS systems: `10` until Phase 0 expands guardrail scope and Phase 1 classifies rendering/UI systems.
- Counting rule: only task lines that start with a checkbox marker count toward checklist progress. Phase `Status:` lines are informational and do not count.

## Non-Burst Inventory

Gameplay hot-path debt already tracked by the previous roadmap:
- `AIBuildPlannerSystem`
- `AICombatOrderSystem`
- `AIEconomySystem`
- `AIProductionSystem`
- `AISquadSystem`
- `UnitAttackSystem`
- `UnitDeathSystem`
- `UnitPathfindingSystem`
- `UnitTransportBoardingSystem`

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

Status: [~]

Purpose:
Make the denominator honest before converting more systems.

Implementation steps:
- [x] Create this roadmap document.
- [x] Record the current static baseline for ECS `OnUpdate`, Burst, jobs, and non-Burst systems.
- [ ] Update architecture audit roots to include `Systems`, `Rendering/Systems`, and `UI/Shell/Ecs`.
- [ ] Add ECS `OnUpdate` denominator reporting to the guardrail output.
- [ ] Add Burst coverage count and percentage reporting.
- [ ] Add job-backed count and percentage reporting.
- [ ] Add `UnclassifiedNonBurstEcsOnUpdateMustBeZero`.
- [ ] Split non-Burst classifications into managed diagnostics, managed bootstrap/startup, managed presentation/render bridge, managed UI shell, managed debug input, and tracked conversion candidate.
- [ ] Set initial guardrail floors to `BurstEcsOnUpdateFloor = 39` and `JobBackedEcsOnUpdateFloor = 32`.
- [ ] Run focused architecture guardrail validation.
- [ ] Update this progress snapshot with the verified checklist count and classification count.

Acceptance checks:
- No ECS `OnUpdate` file is invisible to the audit.
- Every non-Burst ECS `OnUpdate` file is classified exactly once.
- Guardrail output prints counts and percentages used by this roadmap.

## Phase 1: Classify Remaining Non-Burst Systems

Status: [ ]

Purpose:
Decide which systems must be converted and which intentionally remain managed.

Implementation steps:
- [ ] Classify the 9 gameplay hot-path debt systems as full conversion, split conversion, or non-convertible with reason.
- [ ] Classify the 7 rendering non-Burst systems as full conversion, split conversion, presentation boundary, or diagnostic boundary.
- [ ] Classify the 3 UI shell ECS systems as UI boundary or conversion candidate.
- [ ] Record one disposition for every non-Burst ECS `OnUpdate` system.
- [ ] Prioritize conversion candidates by runtime impact and gameplay risk.
- [ ] Update guardrail classification dictionaries to match the disposition list.

Acceptance checks:
- Unclassified non-Burst ECS count is `0`.
- Conversion candidates are prioritized before implementation begins.
- Managed boundaries have concrete reasons, not generic exemptions.

## Phase 2: AI Hot-Path Burst Split

Status: [ ]

Purpose:
Move recurring AI scoring and order-selection loops into Burst-compatible jobs while keeping diagnostics and high-level orchestration managed.

Implementation steps:
- [ ] Inspect `AISquadSystem` data flow, queries, diagnostics, and command writes.
- [ ] Convert pure squad membership, grouping, and target-scoring loops to Burst-compatible jobs.
- [ ] Inspect `AICombatOrderSystem` command selection, target scoring, and diagnostics.
- [ ] Convert AI combat target/order scoring to Burst-compatible jobs and emit ECS command requests.
- [ ] Inspect `AIBuildPlannerSystem` authored policy reads and construction request path.
- [ ] Convert building candidate scoring to Burst-compatible data passes while keeping request issuing managed where needed.
- [ ] Inspect `AIEconomySystem` resource summary and request-buffer policy.
- [ ] Convert resource summary/scoring to Burst-compatible data passes.
- [ ] Inspect `AIProductionSystem` queue/build policy and production request path.
- [ ] Convert production candidate scoring to Burst-compatible data passes.
- [ ] Keep diagnostics, authored policy text, strings, and config assets out of Burst paths.
- [ ] Run AI steady-state focused validation and architecture guardrails.
- [ ] Ratchet Burst/job floors and update the roadmap snapshot after each completed AI slice.

Acceptance checks:
- AI steady-state behavior remains stable.
- No managed config, strings, logging, or GameObject APIs remain in Burst paths.
- AI p95/p99 timing is neutral or improved after warmup.

## Phase 3: Combat And Lifecycle Burst Split

Status: [ ]

Purpose:
Separate combat simulation from presentation, VFX, diagnostics, and destroyed-model handoff.

Implementation steps:
- [ ] Inspect `UnitAttackSystem` attack cadence, target validity, damage, VFX, and diagnostics.
- [ ] Convert attack cooldown, target validity, range, and damage calculations to Burst-compatible jobs.
- [ ] Emit VFX, projectile, and presentation requests as ECS data instead of doing managed work inside the simulation path.
- [ ] Inspect `UnitDeathSystem` death-state, cleanup, destroyed visual, and selection implications.
- [ ] Convert death-state evaluation and cleanup decisions to Burst-compatible jobs.
- [ ] Keep destroyed model and presentation handoff in managed boundary systems.
- [ ] Verify missile launcher, standard attack, and destroyed-building flows.
- [ ] Run focused combat, missile, and selection command validations.
- [ ] Ratchet Burst/job floors and update the roadmap snapshot.

Acceptance checks:
- One-shot missile and normal attack behavior remains correct.
- Death visuals and destroyed-building models still appear.
- No attack-order flicker, repeated damage, or stale target marker regression.

## Phase 4: Transport And Pathfinding Burst Split

Status: [ ]

Purpose:
Burst the data portions of transport and pathfinding while preserving native path-pool ownership and visual passenger handling.

Implementation steps:
- [ ] Inspect `UnitTransportBoardingSystem` capacity, passenger state, command completion, and visual hide/show dependencies.
- [ ] Convert capacity checks, passenger state transitions, and boarding eligibility to Burst-compatible jobs.
- [ ] Keep model hide/show and passenger drawer updates in managed boundaries.
- [ ] Inspect `UnitPathfindingSystem` orchestration, detached jobs, native containers, and dependency ownership.
- [ ] Preserve native path pool ownership and explicit job completion semantics.
- [ ] Ensure scheduled path jobs chain or complete before any main-thread reads of written data.
- [ ] Run transport boarding, board-all, unboard, long-distance move, and pathfinding focused validations.
- [ ] Ratchet Burst/job floors and update the roadmap snapshot.

Acceptance checks:
- Board, board-all, unboard, and passenger drawer flows remain correct.
- Long-distance move and group pathfinding stay stable.
- No `ObjectDisposedException`, invalid lookup, or job dependency runtime error occurs.

## Phase 5: Rendering ECS Split

Status: [ ]

Purpose:
Burst pure rendering decisions while keeping camera, prefab, model, shared-component, and diagnostics work managed.

Implementation steps:
- [ ] Inspect `UnitHelicopterBladeSpinSystem` blade references, transform writes, diagnostics, and string matching.
- [ ] Convert blade rotation data work to Burst-compatible jobs where entity hierarchy traversal can be represented safely.
- [ ] Keep helicopter diagnostics and source-key string matching managed.
- [ ] Inspect `UnitMassRenderSettingsSystem` render bounds, LOD patching, shared managed render settings, and structural tagging.
- [ ] Convert bounds and LOD patch planning to Burst-compatible jobs.
- [ ] Keep managed `RenderFilterSettings` shared-component mutation in a managed boundary.
- [ ] Inspect `UnitRenderBudgetSystem` camera shell and pure helper systems.
- [ ] Keep camera access and runtime camera reference handling in a managed shell.
- [ ] Confirm distance, sort, banding, classification, and visual-plan helpers remain Burst-compatible.
- [ ] Classify `UnitAttachedLightSystem` as managed presentation or split if a pure data loop is found.
- [ ] Classify `UnitFactionTintTargetBackfillSystem` as managed presentation or split if a pure data loop is found.
- [ ] Classify `UnitModelSpawnSystem` as managed presentation unless a safe pure data preparation pass exists.
- [ ] Run render-budget validation and runtime visual smoke.
- [ ] Ratchet Burst/job floors and update the roadmap snapshot.

Acceptance checks:
- Render-budget focused validation passes.
- Helicopter blade animation still works.
- LOD, visibility, light, tint, and model spawn behavior do not regress.
- No stale lookup is read after structural changes.

## Phase 6: Managed Boundary Lockdown

Status: [ ]

Purpose:
Make intentional managed code explicit so it is not confused with hidden hot-path debt.

Implementation steps:
- [ ] Confirm diagnostic flush systems stay managed and excluded from Burst targets.
- [ ] Confirm startup/bootstrap systems stay managed unless they contain recurring pure runtime loops.
- [ ] Confirm UI shell ECS systems stay managed because they bridge UI state.
- [ ] Confirm debug input systems stay managed.
- [ ] Confirm visual, prefab, and GameObject bridge systems stay managed unless a pure data pass is extracted.
- [ ] Add or update classification reasons for all intentionally managed systems.
- [ ] Add guardrail coverage that prevents a managed boundary from also being tracked hot-path debt.
- [ ] Update the progress snapshot with final managed-boundary count.

Acceptance checks:
- Managed boundaries are explicit and reviewed.
- No UI, prefab, camera, debug, or diagnostic code is forced into Burst.
- Remaining non-Burst systems are all intentional boundaries or documented non-convertible cases.

## Phase 7: Final Ratchet And Validation

Status: [ ]

Purpose:
Lock the achieved coverage into tests and prove runtime behavior did not regress.

Implementation steps:
- [ ] Re-run ECS audit and record final counts.
- [ ] Raise `BurstEcsOnUpdateFloor` to the final achieved count.
- [ ] Raise `JobBackedEcsOnUpdateFloor` to the final achieved count.
- [ ] Ensure unclassified non-Burst ECS count is `0`.
- [ ] Run architecture guardrails.
- [ ] Run AI steady-state validation.
- [ ] Run selection and move validation.
- [ ] Run attack and missile validation.
- [ ] Run transport boarding validation.
- [ ] Run pathfinding validation.
- [ ] Run render-budget validation.
- [ ] Run explicit full-editor validation.
- [ ] Run graphics-capable match runtime smoke.
- [ ] Compare p95, p99, max frame time, and GC allocation against the previous baseline.
- [ ] Record final report under `Design/AgentReports`.
- [ ] Mark this roadmap complete only if metrics show no recurring regression.
- [ ] Delete or replace the heartbeat when this roadmap is complete.

Acceptance checks:
- Burst/job coverage is maximized for safe ECS runtime systems.
- Remaining non-Burst ECS systems are intentional managed boundaries or documented non-convertible cases.
- Runtime and editor validation pass.
- Performance report documents improved or neutral p95/p99 and no recurring GC regression after warmup.

## Validation Commands

Use the main project when Unity is closed; use the shadow project only when the main project is locked.

Focused architecture guard:
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation -logFile /private/tmp/warline-ecs-burst-max-architecture.log`

Full editor validation:
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests -logFile /private/tmp/warline-ecs-burst-max-full-editor.log`

Static diff validation:
- `git diff --check`
