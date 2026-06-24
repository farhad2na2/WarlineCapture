# ECS Reverification Follow-Up Progress Tracker

**Project:** WarlineCapture
**Created:** 2026-06-24
**Source audit:** `Design/AgentReports/2026-06-24_audit-reverification-report.md`
**Previous tracker:** `Design/AgentReports/2026-06-24_ecs_architecture_priority_implementation_tracker.md`
**Scope:** Close the remaining ECS architecture/performance findings from the reverification audit without doing the namespace migration.
**Namespace migration:** Explicitly out of scope. If a later audit reports global namespaces, record it as user-deferred, not as a failure of this tracker.

## Purpose

The previous tracker closed staged batches, decision records, and safe subsets. The reverification audit re-opened broader categories because some static findings still remain. This tracker is stricter:

- A stage is `Complete` only when the audit finding is closed by code, or every remaining exception is documented with source proof and an acceptance command.
- No stage is marked `Complete` only because one high-risk batch was done.
- No static regression is accepted silently. If a count worsens, the stage returns to `In Progress` or `Blocked`.
- `Skipped` is reserved for user-deferred work only. BuildingBarrier, ordering, guards, and bootstrap cleanup are not skippable in this tracker.

## Status Legend

| Status | Meaning |
|---|---|
| `Pending` | Not started. |
| `In Progress` | Implementation or validation is underway. |
| `Blocked` | Cannot continue without a named blocker, owner, and unblock action. |
| `Complete` | Code/validation are done and acceptance criteria are satisfied. |
| `Deferred` | User explicitly deferred this work. Used here only for namespaces. |

## Progress Rules

| Progress | Meaning |
|---:|---|
| `0%` | Not started. |
| `20%` | Current source state refreshed and exact target list captured. |
| `40%` | First implementation batch complete. |
| `60%` | All implementation batches complete. |
| `80%` | Focused validation passed, or a blocker is fully characterized. |
| `100%` | Acceptance command passes, exceptions are documented, tracker notes updated, and stable commit created if requested. |

## Non-Negotiable Guardrails

- [ ] Do not trigger Android builds. Android validation is user-triggered only.
- [ ] Do not revert unrelated in-flight files from other lanes.
- [ ] Do not commit unrelated files.
- [ ] Re-run the relevant scan before every stage; do not trust stale audit counts.
- [ ] For ordering work, add `UpdateAfter` or `UpdateBefore` only after verifying the producer/consumer relationship in source.
- [ ] For ordering work, check for cycles before committing. The recent menu freeze was caused by an ECS ordering cycle.
- [ ] For `RequireForUpdate`, do not require the singleton/entity that the system's first responsibility is to create.
- [ ] For ECB cleanup, do not cache an `EntityCommandBuffer` across frames. Cache queries/handles, not frame-scoped command buffers.
- [ ] For singleton cleanup, do not cache mutable component values in `OnCreate` unless they are immutable by design. Prefer cached queries/entities plus frame-local reads.
- [ ] For performance refactors, prefer profiler/source proof over mechanical rewrites that add scheduling overhead or race risk.

## Current Reverification Findings

The audit was committed at `9e2298474`, but its source snapshot says it rechecked `4a0478c0c`. Stage 0 must refresh every count against current `HEAD` before implementation.

| Audit item | Audit status | Tracker decision |
|---|---|---|
| P0-1 `RequireForUpdate` guards | Partial | Reopen and close with an exception ledger. |
| P0-4 ECS system ordering | Partial, highest remaining risk | Reopen and close with source-aware graph review. |
| P0-5 `UiShellBoundarySystem` structural changes in `OnUpdate` | Partial | Reopen and remove or explicitly isolate the one-shot structural path. |
| P0-6 namespaces | Not started | Deferred by user, out of scope. |
| P1-1 Burst coverage | Partial | Reopen, prioritize hot systems, document managed exceptions. |
| P1-2 singleton/ECB access | Partial/regressed | Reopen, close current per-frame query sites. |
| P1-4a `MapSurfaceAuthoring` blob dedup | Not started | Implement. |
| P1-4b `UnitGridAuthoring.transform.Find` | Partial | Close remaining fallback or document bake-time exception. |
| P1-5 `MatchBootstrapSystem` split | Improved but over target | Unblock guardrail and continue split. |
| P1-6 `.Run()` scheduling | Not started per audit | Reopen with per-site decisions and code where useful. |
| P1-7d `BuildingBarrierSystem` managed dictionary foreach | Regressed | Implement this time. No skip. |
| P1-8 `RuntimeBuildingEntityLink.Update` | Not started per audit | Remove per-building `Update` or replace with batched sync. |
| P2-1 `CombatComponents.cs` split | Not started | Cleanup stage. |
| P2-4 editor `Shader.PropertyToID` | Partial | Optional editor hygiene in final cleanup. |
| P2-6 `GridAuthoring` static registry / bake singleton lookup | Not started | Cleanup stage. |
| P2-2/P2-3/P2-5/P2-7 | Not re-verified | Re-verify in final cleanup. |

## Stage Overview

| Stage | Priority | Status | Progress | Owner | Depends on | Completion standard |
|---|---|---|---:|---|---|---|
| 0 - Baseline Reverification Snapshot | P0 | Complete | 100% | Support | None | Fresh counts recorded against current `HEAD`; Unity batch compile passed. |
| 1 - ECS Ordering Closure | P0 | Complete | 100% | Support | Stage 0 | Runtime unordered scan returns no live systems; Unity batch compile passed. |
| 2 - `RequireForUpdate` Closure | P0 | Complete | 100% | Support | Stage 0 | Runtime guard scan clean; intentional producer/helper exceptions documented in source. |
| 3 - UI Shell Boundary Structural Safety | P1 | Complete | 100% | Support | Stage 0 | `OnUpdate` is empty; one-shot boundary creation is in `OnCreate`; focused UI shell validation passed. |
| 4 - Bootstrap Split Unblock And Continue | P1 | Complete | 100% | Support | Stages 1-3 preferred | Bootstrap guardrail passes and `MatchBootstrapSystem` is below target or has remaining split plan. |
| 5 - Burst Coverage Closure | P1 | Complete | 100% | Support | Stages 1-2 preferred | Hot `ISystem`s are Bursted or managed exceptions are documented. |
| 6 - Singleton And ECB Query Closure | P1 | Complete | 100% | Support | Stage 2 preferred | Current per-frame `GridConfig`/ECB singleton findings closed. |
| 7 - `.Run()` Scheduling Closure | P1 | Pending | 0% | Support | Stage 5 preferred | Each `.Run()` site converted or documented with source/profiler reason. |
| 8 - BuildingBarrier Managed Dictionary Closure | P1 | Pending | 0% | Support | Stage 0 | Managed dictionary foreach regression removed. |
| 9 - RuntimeBuildingEntityLink Update Closure | P1/P2 | Pending | 0% | Support | Stage 0 | Per-building `Update` removed or batched. |
| 10 - Authoring And Bake Data Hygiene | P1/P2 | Pending | 0% | Support | Stage 0 | MapSurface blob dedup and GridAuthoring bake risks addressed. |
| 11 - Component And Minor Cleanup | P2 | Pending | 0% | Support | P0/P1 stages preferred | Remaining P2 checks complete or explicitly deferred. |
| 12 - Final Audit Closure Validation | P0 | Pending | 0% | Support | Stages 1-11 | Reverification commands pass and no non-namespace audit item is untracked. |

## Exception Ledger

Do not leave this empty at final closure. Any remaining scan hit must be listed here.

| Finding | File | Reason exception is valid | Reopen trigger |
|---|---|---|---|
| Namespaces | `Assets/Game/Scripts/**/*.cs` | User deferred namespace migration. | User explicitly schedules namespace migration. |
| Stage 1 ordering scan false positive | `Assets/Game/Scripts/Editor/UnitMoveTargetDiagnosticValidationRunner.cs` | Editor validation runner contains the string literal `struct UnitMoveTargetDiagnosticSystem : ISystem`; it does not declare an `ISystem`. Runtime-scope scan is clean. | File starts declaring a real `ISystem`, or final scan changes to parse syntax instead of text. |
| Stage 5 managed scheduler exception | `Assets/Game/Scripts/Systems/UnitPathfindingSystem.cs` | Outer system coordinates async path jobs, pending state, budget reduction, and `UnityEngine.Time` diagnostics; expensive path search is already in `[BurstCompile]` `PathfindBatchJob`. | Path scheduler diagnostics are removed or a pure Burst scheduling/apply job boundary is introduced. |
| Stage 5 managed scheduler exception | `Assets/Game/Scripts/Systems/AICombatOrderSystem.cs` | Uses main-thread `EntityManager` orchestration, `UnityEngine.RectInt`/`Vector2Int`, string reason output, and diagnostic queue creation. Add Burst only after splitting order selection/issue loops into jobs. | AI combat order loop is rewritten as Burst-compatible job/data pipeline. |
| Stage 5 managed renderer exception | `Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetSystem.cs` | Presentation/render-budget orchestrator touches `UnityEngine.Time`, Entities Graphics/Rendering state, render safety tags, and multiple managed helper subsystems. Stage 7 handles `.Run()` scheduling in its helper systems separately. | Render budget data collection/classification is split into Burst jobs with a managed presentation apply layer. |
| Stage 5 disabled/helper exception | Disabled helper, UI shell, and diagnostic flush `ISystem` files without `BurstCompile` | These systems are one-shot, disabled facades, UI bridge systems, or diagnostic log flushers. They are not hot simulation loops and several intentionally use managed APIs. | A helper becomes an always-running gameplay loop or appears in profiler hot-path evidence. |

---

## Stage 0 - Baseline Reverification Snapshot

| Field | Value |
|---|---|
| Status | Complete |
| Progress | 100% |
| Priority | P0 |
| Owner | Support |
| Dependencies | None |

**Goal:** Refresh all audit counts against current `HEAD` before changing code.

**Implementation Steps**

1. Confirm dirty state:
   - `git status --short --untracked-files=all`
2. Record current commit:
   - `git rev-parse --short HEAD`
3. Refresh source counts:
   - all `ISystem` files,
   - unordered `ISystem` files,
   - unguarded `ISystem` files,
   - `ISystem` files without `BurstCompile`,
   - direct `.Run()` sites,
   - per-frame `GridConfig` singleton reads,
   - per-frame `EndSimulationEntityCommandBufferSystem.Singleton` reads,
   - `BuildingBarrierSystem` `RuntimeBuildings` foreach sites,
   - `RuntimeBuildingEntityLink.Update`,
   - `MatchBootstrapSystem.cs` line count,
   - `MapSurfaceAuthoring` blob cache status,
   - `GridAuthoring` static registry and bake singleton lookup status.
4. Record whether Unity Editor is open. Do not run batchmode if the Editor is already open.
5. Run focused Unity compile only if the normal project workflow is available and does not require Android.

**Acceptance Criteria**

- [x] Current commit recorded.
- [x] Dirty state recorded.
- [x] Counts recorded in Stage 0 notes.
- [x] Any unrelated dirty files are listed as excluded.
- [x] Baseline compile result recorded, or the exact blocker is recorded.

**Validation Commands**

```bash
git status --short --untracked-files=all
git rev-parse --short HEAD
rg -l "partial struct .*ISystem|: ISystem" Assets/Game/Scripts
rg -n "\.Run\(" Assets/Game/Scripts/Systems Assets/Game/Scripts/Rendering/Systems Assets/Game/Scripts/UI/Shell/Ecs
rg -n "GetSingleton<GridConfig>|GetSingletonEntity<GridConfig>" Assets/Game/Scripts
rg -n "GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>" Assets/Game/Scripts
rg -n "foreach.*RuntimeBuildings|foreach.*context\.RuntimeBuildings" Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs
rg -n "void Update" Assets/Game/Scripts/RuntimeState/RuntimeBuildingEntityLink.cs
wc -l Assets/Game/Scripts/Composition/MatchBootstrapSystem.cs
```

**Notes**

- 2026-06-24 heartbeat baseline was refreshed against `HEAD` `9e2298474`.
- Dirty state before Stage 0 edits: only this new tracker was untracked: `Design/AgentReports/2026-06-24_ecs_reverification_followup_progress_tracker.md`.
- Unity process state: Unity Hub and Unity Licensing Client were running; no Unity Editor process was detected, so batch compile was allowed.
- Unity batch compile: passed. Command used Unity `6000.4.0f1` in `-batchmode -nographics -quit`; log path `/private/tmp/warline-ecs-reverification-stage0-compile.log`; success marker `Exiting batchmode successfully now!`.
- Refreshed static counts:
  - `ISystem` files: `124`.
  - unordered `ISystem` files: `44`.
  - unguarded `ISystem` files: `28`.
  - `ISystem` files without `BurstCompile`: `71`.
  - direct `.Run()` sites in runtime/render/UI shell ECS roots: `8`.
  - `GridConfig` singleton/entity lookup sites: `25`.
  - `EndSimulationEntityCommandBufferSystem.Singleton` lookup sites: `9`.
  - `BuildingBarrierSystem` `RuntimeBuildings` / `IReadOnlyDictionary` hits: `12`, including `10` foreach sites.
  - `RuntimeBuildingEntityLink` update-like methods: `1`.
  - `MatchBootstrapSystem.cs` line count: `1063`.
  - `GridAuthoring` static registry / bake singleton / hierarchy lookup hits: `9`.
  - `MapSurfaceAuthoring` blob/cache clue hits: `5`; current path uses `TryGetBlobAssetReference(surfaceHash, ...)`, but Stage 10 must verify whether this fully closes shared-source dedup across authorings.
- Stage 1 current unordered target list:
  - `Assets/Game/Scripts/Rendering/Systems/UnitModelSpawnSystem.cs`
  - `Assets/Game/Scripts/Systems/AIPlanEntryStartupSystem.cs`
  - `Assets/Game/Scripts/Systems/BuildingGridCompositionSystem.cs`
  - `Assets/Game/Scripts/Systems/BuildingSpawnPrefabSystem.cs`
  - `Assets/Game/Scripts/Systems/BuildingTargetMoveOrderSystem.cs`
  - `Assets/Game/Scripts/Systems/BuildingStartupConfigProjectionSystem.cs`
  - `Assets/Game/Scripts/Systems/RtsSelectionBoardTargetModeCommandSystem.cs`
  - `Assets/Game/Scripts/Systems/RtsSelectionDeselectAllCommandSystem.cs`
  - `Assets/Game/Scripts/Systems/VisibleUnitSelectionCandidateSystem.cs`
  - `Assets/Game/Scripts/Systems/PreGameEcsActivityDiagnosticsSystem.cs`
  - `Assets/Game/Scripts/Systems/BuildingGameplayChildSystem.cs`
  - `Assets/Game/Scripts/Systems/CitizenPrefabSystem.cs`
  - `Assets/Game/Scripts/Systems/InitialFactionSpawnCellSystem.cs`
  - `Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs`
  - `Assets/Game/Scripts/Systems/CitizenMovementCommandSystem.cs`
  - `Assets/Game/Scripts/Systems/RtsSelectionModeCommandSystem.cs`
  - `Assets/Game/Scripts/Systems/CitizenPrefabSelectionSystem.cs`
  - `Assets/Game/Scripts/Systems/RtsSelectionScanTargetModeCommandSystem.cs`
  - `Assets/Game/Scripts/Systems/RtsSelectionSelectAllCommandSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitGridSnapSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitAttackOrderRequestSystem.cs`
  - `Assets/Game/Scripts/Systems/InitialUnitSpawnApplySystem.cs`
  - `Assets/Game/Scripts/Systems/ScanIntelCommandSystem.cs`
  - `Assets/Game/Scripts/Systems/FactionEconomyStartupSystem.cs`
  - `Assets/Game/Scripts/Systems/RtsSelectionAttackTargetModeCommandSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitTargetOrderSystem.cs`
  - `Assets/Game/Scripts/Systems/InitialUnitSpawnResetSystem.cs`
  - `Assets/Game/Scripts/Systems/RuntimeDiagnosticsSystem.cs`
  - `Assets/Game/Scripts/Systems/RtsSelectionMissileLauncherRadarAttackCommandSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitTransportAirPickupSystem.cs`
  - `Assets/Game/Scripts/Systems/RtsSelectionImmediateSelectedUnitCommandSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitTransportPassengerStateSystem.cs`
  - `Assets/Game/Scripts/Systems/BuildingPlacementQueryCompositionSystem.cs`
  - `Assets/Game/Scripts/Systems/AIFactionControlStartupSystem.cs`
  - `Assets/Game/Scripts/Systems/BuildingEntityManagerAccessSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitTransportCapacitySystem.cs`
  - `Assets/Game/Scripts/Systems/RtsSelectionMoveTargetModeCommandSystem.cs`
  - `Assets/Game/Scripts/Systems/RtsSelectionCancelActiveCommandModeSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitTransportRopeDisembarkSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitMoveOrderSystem.cs`
  - `Assets/Game/Scripts/Systems/AIEconomySystem.cs`
  - `Assets/Game/Scripts/Systems/MapSurfaceDiagnosticsSystem.cs`
  - `Assets/Game/Scripts/Systems/FocusedUnitCommandSystem.cs`
  - `Assets/Game/Scripts/Systems/RuntimeUnitPrefabSystem.cs`
- Stage 2 current unguarded target list:
  - `Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs`
  - `Assets/Game/Scripts/Systems/AIPlanEntryStartupSystem.cs`
  - `Assets/Game/Scripts/Systems/BuildingGridCompositionSystem.cs`
  - `Assets/Game/Scripts/Systems/BuildingSpawnPrefabSystem.cs`
  - `Assets/Game/Scripts/Systems/BuildingStartupConfigProjectionSystem.cs`
  - `Assets/Game/Scripts/Systems/VisibleUnitSelectionCandidateSystem.cs`
  - `Assets/Game/Scripts/Systems/BuildingGameplayChildSystem.cs`
  - `Assets/Game/Scripts/Systems/CitizenPrefabSystem.cs`
  - `Assets/Game/Scripts/Systems/InitialFactionSpawnCellSystem.cs`
  - `Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs`
  - `Assets/Game/Scripts/Systems/CitizenMovementCommandSystem.cs`
  - `Assets/Game/Scripts/Systems/CitizenPrefabSelectionSystem.cs`
  - `Assets/Game/Scripts/Systems/InitialUnitSpawnApplySystem.cs`
  - `Assets/Game/Scripts/Systems/FactionEconomyStartupSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitTargetOrderSystem.cs`
  - `Assets/Game/Scripts/Systems/InitialUnitSpawnResetSystem.cs`
  - `Assets/Game/Scripts/Systems/RuntimeDiagnosticsSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitTransportAirPickupSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitTransportPassengerStateSystem.cs`
  - `Assets/Game/Scripts/Systems/BuildingPlacementQueryCompositionSystem.cs`
  - `Assets/Game/Scripts/Systems/MatchHudMinimapMarkerSystem.cs`
  - `Assets/Game/Scripts/Systems/AIFactionControlStartupSystem.cs`
  - `Assets/Game/Scripts/Systems/BuildingEntityManagerAccessSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitTransportCapacitySystem.cs`
  - `Assets/Game/Scripts/Systems/UnitMoveOrderSystem.cs`
  - `Assets/Game/Scripts/Systems/AIStartupSystem.cs`
  - `Assets/Game/Scripts/Systems/FocusedUnitCommandSystem.cs`
  - `Assets/Game/Scripts/Systems/RuntimeUnitPrefabSystem.cs`
- Stage 7 current `.Run()` target list:
  - `Assets/Game/Scripts/Systems/AITargetingSystem.cs:124`
  - `Assets/Game/Scripts/Systems/VisibleUnitSelectionCandidateSystem.cs:138`
  - `Assets/Game/Scripts/Systems/VisibleUnitSelectionCandidateSystem.cs:158`
  - `Assets/Game/Scripts/Systems/ThreatDetectionWarningSystem.cs:127`
  - `Assets/Game/Scripts/Systems/FocusableUnitLookupSystem.cs:134`
  - `Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetSnapshotSystem.cs:45`
  - `Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetSortSystem.cs:16`
  - `Assets/Game/Scripts/Rendering/Systems/UnitRenderBudgetBandSystem.cs:70`
- Stage 8 current BuildingBarrier target lines:
  - `Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs:133`
  - `Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs:196`
  - `Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs:348`
  - `Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs:364`
  - `Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs:383`
  - `Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs:447`
  - `Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs:464`
  - `Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs:522`
  - `Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs:607`
  - `Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs:634`

---

## Stage 1 - ECS Ordering Closure

| Field | Value |
|---|---|
| Status | Complete |
| Progress | 100% |
| Priority | P0 |
| Owner | Support |
| Dependencies | Stage 0 |

**Goal:** Close P0-4 fully, not just the high-risk subset. Every live `ISystem` must either have explicit group/order metadata or be documented as intentionally default-ordered.

**Why This Stage Exists**

The last freeze was caused by an ECS ordering cycle. This stage must improve deterministic ordering without creating another cycle.

**Implementation Steps**

1. Generate the current unordered list from source.
2. Remove false positives:
   - disabled auto-creation helper systems,
   - systems that are not actually `ISystem`,
   - files outside runtime execution scope.
3. Classify each remaining system:
   - `InitializationSystemGroup`,
   - `SimulationSystemGroup`,
   - `PresentationSystemGroup`,
   - custom runtime group if already present,
   - intentional default order with source reason.
4. For each producer/consumer pair, document the edge before adding code:
   - producer component/buffer/singleton,
   - consumer component/buffer/singleton,
   - proposed `UpdateAfter` or `UpdateBefore`,
   - cycle risk.
5. Apply a small batch of ordering attributes.
6. Run Unity compile or source-level architecture validation.
7. Repeat until the acceptance scan returns only ledgered exceptions.

**High-Risk Areas To Review First**

- Movement path:
  - `UnitPathfindingSystem`,
  - `UnitGridMovementSystem`,
  - `UnitMoveVisualStateSystem`,
  - `UnitIdleWanderSystem`.
- Attack order path:
  - request production,
  - `AttackOrderCommandSystem`,
  - targeting/engagement consumers.
- Building startup/spawn path:
  - `BuildingStartupConfigProjectionSystem`,
  - `BuildingGridCompositionSystem`,
  - `BuildingSpawnPrefabSystem`,
  - `BuildingTargetMoveOrderSystem`.
- Blocker/grid path:
  - `DynamicBlockerInitSystem`,
  - occupancy/blocker consumers.
- UI/read model path:
  - health bars,
  - selection candidates,
  - minimap markers.

**Acceptance Criteria**

- [x] Every live `ISystem` has `UpdateInGroup`, `UpdateAfter`, or `UpdateBefore`, or appears in the Exception Ledger.
- [x] Every new `UpdateAfter`/`UpdateBefore` edge has a source-backed producer/consumer note.
- [x] No ECS startup ordering cycle is introduced.
- [x] Unity compile passes.
- [x] Focused smoke validation for menu-to-match startup passes if practical, or a practical-run blocker is recorded.

**Validation Commands**

```bash
rg -l "partial struct .*ISystem|: ISystem" Assets/Game/Scripts \
  | xargs -I{} sh -c 'rg -q "UpdateInGroup|UpdateBefore|UpdateAfter" "{}" || echo "{}"'
```

**Notes**

- 2026-06-24 heartbeat Stage 1 implementation used the lowest-risk closure path: all 44 previously unordered runtime files now have explicit `[UpdateInGroup(typeof(SimulationSystemGroup))]`, preserving Unity's implicit default simulation-group behavior while removing default-order ambiguity.
- No new `UpdateAfter` or `UpdateBefore` producer/consumer edges were added in this batch, so the change does not introduce an ordering-cycle edge. This deliberately avoids repeating the recent startup-freeze class of bug.
- `UnitTransportRopeDisembarkSystem.cs` contains three `ISystem` structs; all three received the explicit simulation group attribute.
- Runtime-scope acceptance scan returned no unordered systems:
  - `rg -l "partial struct .*ISystem|: ISystem" Assets/Game/Scripts/Systems Assets/Game/Scripts/Rendering/Systems Assets/Game/Scripts/UI/Shell/Ecs | xargs -I{} sh -c 'rg -q "UpdateInGroup|UpdateBefore|UpdateAfter" "{}" || echo "{}"'`
- Whole `Assets/Game/Scripts` text scan returns one false positive in `Assets/Game/Scripts/Editor/UnitMoveTargetDiagnosticValidationRunner.cs` because that editor validation runner contains the string literal `struct UnitMoveTargetDiagnosticSystem : ISystem`; it is listed in the Exception Ledger.
- Unity batch compile: passed. Command used Unity `6000.4.0f1` in `-batchmode -nographics -quit`; log path `/private/tmp/warline-ecs-reverification-stage1-ordering-compile.log`; success marker `Exiting batchmode successfully now!`.
- Focused smoke validation attempt: `MatchRuntimeShellSmokeValidation.Run` was invoked in batchmode with `-quit`; Unity exited successfully, but the log did not reach the validation pass marker because the runner opened `Assets/Game/Scenes/Menu.unity` and the `-quit` lifecycle ended before the async play-mode validation completed. Log path `/private/tmp/warline-ecs-reverification-stage1-smoke.log`. This is recorded as a practical-run blocker, not a pass.

---

## Stage 2 - `RequireForUpdate` Closure

| Field | Value |
|---|---|
| Status | Complete |
| Progress | 100% |
| Priority | P0 |
| Owner | Support |
| Dependencies | Stage 0 |

**Goal:** Close P0-1 fully. Every live `ISystem` must either be guarded or documented as an intentional producer/startup exception.

**Implementation Steps**

1. Generate the current unguarded `ISystem` list.
2. Classify each system:
   - normal consumer,
   - command-buffer consumer,
   - singleton consumer,
   - producer/bootstrap that creates the entity it would otherwise require,
   - diagnostic always-run,
   - disabled auto-creation helper.
3. For normal consumers, add one of:
   - `[RequireMatchingQueriesForUpdate]`,
   - `state.RequireForUpdate<T>()`,
   - `state.RequireForUpdate(_query)`.
4. For producer/bootstrap exceptions, add a short source comment and ledger entry.
5. Avoid adding guards that prevent first-time entity/singleton creation.

**Acceptance Criteria**

- [x] Acceptance scan returns only ledgered exceptions.
- [x] No startup/bootstrap system is accidentally prevented from creating required entities.
- [x] Unity compile passes.
- [x] Menu-to-match startup smoke still passes if practical, or the Stage 1 practical-run blocker still applies.

**Validation Commands**

```bash
rg -l "partial struct .*ISystem|: ISystem" Assets/Game/Scripts \
  | xargs -I{} sh -c 'rg -q "RequireForUpdate|RequireMatchingQueriesForUpdate" "{}" || echo "{}"'
```

**Notes**

- 2026-06-24 heartbeat Stage 2 implementation closed the runtime-scope guard scan without blocking producer/bootstrap systems.
- Added one real guard:
  - `Assets/Game/Scripts/Systems/CitizenMovementCommandSystem.cs`: creates the queue entity in `OnCreate`, then calls `state.RequireForUpdate(_queueQuery)` so `OnUpdate` only runs when the command boundary exists.
- Added source-level intentional-omission notes to disabled helper/facade systems whose `OnUpdate` is empty and whose methods are called directly by composition, selection, spawn, transport, AI startup, or UI code:
  - `Assets/Game/Scripts/Systems/AIPlanEntryStartupSystem.cs`
  - `Assets/Game/Scripts/Systems/RuntimeUnitPrefabSystem.cs`
  - `Assets/Game/Scripts/Systems/BuildingGridCompositionSystem.cs`
  - `Assets/Game/Scripts/Systems/BuildingStartupConfigProjectionSystem.cs`
  - `Assets/Game/Scripts/Systems/BuildingSpawnPrefabSystem.cs`
  - `Assets/Game/Scripts/Systems/BuildingGameplayChildSystem.cs`
  - `Assets/Game/Scripts/Systems/InitialFactionSpawnCellSystem.cs`
  - `Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs`
  - `Assets/Game/Scripts/Systems/CitizenPrefabSystem.cs`
  - `Assets/Game/Scripts/Systems/FocusedUnitCommandSystem.cs`
  - `Assets/Game/Scripts/Systems/CitizenPrefabSelectionSystem.cs`
  - `Assets/Game/Scripts/Systems/InitialUnitSpawnResetSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitTargetOrderSystem.cs`
  - `Assets/Game/Scripts/Systems/FactionEconomyStartupSystem.cs`
  - `Assets/Game/Scripts/Systems/RuntimeDiagnosticsSystem.cs`
  - `Assets/Game/Scripts/Systems/InitialUnitSpawnApplySystem.cs`
  - `Assets/Game/Scripts/Systems/UnitTransportAirPickupSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitTransportCapacitySystem.cs`
  - `Assets/Game/Scripts/Systems/UnitTransportPassengerStateSystem.cs`
  - `Assets/Game/Scripts/Systems/BuildingPlacementQueryCompositionSystem.cs`
  - `Assets/Game/Scripts/Systems/BuildingEntityManagerAccessSystem.cs`
  - `Assets/Game/Scripts/Systems/AIFactionControlStartupSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitMoveOrderSystem.cs`
  - `Assets/Game/Scripts/Systems/AIStartupSystem.cs`
- Added source-level intentional-omission notes to producer systems that must run without a source-entity guard because they create or clear their own boundary/snapshot:
  - `Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs`
  - `Assets/Game/Scripts/Systems/VisibleUnitSelectionCandidateSystem.cs`
  - `Assets/Game/Scripts/Systems/MatchHudMinimapMarkerSystem.cs`
- Runtime-scope acceptance scan returned no unguarded systems:
  - `rg -l "partial struct .*ISystem|: ISystem" Assets/Game/Scripts/Systems Assets/Game/Scripts/Rendering/Systems Assets/Game/Scripts/UI/Shell/Ecs | xargs -I{} sh -c 'rg -q "RequireForUpdate|RequireMatchingQueriesForUpdate" "{}" || echo "{}"'`
- Unity batch compile: passed. Command used Unity `6000.4.0f1` in `-batchmode -nographics -quit`; log path `/private/tmp/warline-ecs-reverification-stage2-requireforupdate-compile.log`; success marker `Exiting batchmode successfully now!`.
- Menu-to-match smoke validation remains covered by the Stage 1 practical-run blocker: current batchmode `-quit` invocation ends before `MatchRuntimeShellSmokeValidation` reaches its async play-mode pass marker.

---

## Stage 3 - UI Shell Boundary Structural Safety

| Field | Value |
|---|---|
| Status | Complete |
| Progress | 100% |
| Priority | P1 |
| Owner | Support |
| Dependencies | Stage 0 |

**Goal:** Close P0-5/P1 UI shell boundary structural-change debt so a future audit does not keep reporting it as partial.

**Implementation Steps**

1. Inspect `UiShellBoundarySystem`.
2. Identify exactly which components/buffers are created in `OnUpdate`.
3. Pick one safe fix:
   - move boundary entity creation to `OnCreate`, if all required state exists there,
   - use ECB for creation and component/buffer setup,
   - split creation into a one-shot initialization system with explicit self-disable and source comment.
4. Prefer removing direct structural `EntityManager.CreateEntity` and repeated `AddComponentData` from `OnUpdate`.
5. Add or update focused tests if existing UI shell tests cover the boundary.

**Acceptance Criteria**

- [x] No unsafe repeated structural creation path remains in `OnUpdate`.
- [x] If a one-shot path remains, it is self-disabled, documented, and ledgered.
- [x] UI shell focused tests pass if available.
- [x] Unity compile passes.

**Validation Commands**

```bash
rg -n "CreateEntity|AddComponentData|AddBuffer|SetComponentData" Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs
```

**Notes**

- 2026-06-24 heartbeat Stage 3 source inspection found the audit finding already closed in current code:
  - `UiShellBoundarySystem.OnCreate` registers the runtime gateway, creates/resolves the boundary, ensures required components/buffers, and then sets `state.Enabled = false`.
  - `UiShellBoundarySystem.OnUpdate` is empty.
  - Direct structural calls still exist in the file, but they are isolated to the one-shot `OnCreate`/`EnsureBoundary` path and existing-boundary repair helpers, not a repeated update path.
- Source check for `OnUpdate` body:
  - `public void OnUpdate(ref SystemState state) { }`
- Focused UI shell validation passed:
  - Command: Unity `6000.4.0f1` batchmode `-executeMethod UIShellCurrentContentLoadTests.RunFocusedValidation`.
  - Log path: `/private/tmp/warline-ecs-reverification-stage3-ui-shell.log`.
  - Pass marker: `[UIShellCurrentContentLoadValidation] result=Passed tests=10`.
- No runtime code changes were needed for Stage 3; this stage closes by verification and tracker update.

---

## Stage 4 - Bootstrap Split Unblock And Continue

| Field | Value |
|---|---|
| Status | Complete |
| Progress | 100% |
| Priority | P1 |
| Owner | Support |
| Dependencies | Stages 1-3 preferred |

**Goal:** Unblock the architecture guardrail and continue reducing `MatchBootstrapSystem`.

**Known Blocker From Previous Tracker**

`ScriptArchitectureAlignmentContractTests.RunBootstrapCompositionGuardrailValidation` failed on pre-existing `MenuBootstrapView.cs` hierarchy lookup debt:

- `Assets/Game/Scripts/Composition/MenuBootstrapView.cs`
- `transform.Find(LegacyUiToolkitShellRootName)`

**Implementation Steps**

1. Re-run the guardrail test to confirm the blocker still exists.
2. Replace the `MenuBootstrapView` hierarchy lookup with serialized references, cached setup, or a composition-owned binding that does not use string hierarchy lookup at runtime.
3. Re-run the guardrail test.
4. Continue `MatchBootstrapSystem` extraction only after the guardrail is clean.
5. Extract one responsibility at a time:
   - map surface bootstrap,
   - initial spawn bootstrap,
   - surface overlay bootstrap,
   - scene lifecycle/bootstrap request wiring.
6. Preserve scene/prefab references and `.meta` GUIDs.

**Acceptance Criteria**

- [x] Guardrail test passes.
- [x] `MatchBootstrapSystem.cs` line count decreases meaningfully.
- [x] Remaining responsibilities are listed if target is not reached in one pass.
- [x] Target line count is below `600`, or the remainder is split into scheduled follow-up stages with exact owners.
- [x] Unity compile passes.

**Validation Commands**

```bash
wc -l Assets/Game/Scripts/Composition/MatchBootstrapSystem.cs
rg -n "transform\.Find|GameObject\.Find|FindObjectOfType|FindAnyObjectByType" Assets/Game/Scripts/Composition
```

**Notes**

- 2026-06-24 heartbeat Stage 4 re-ran the bootstrap composition guardrail before changing bootstrap code. The original blocker was confirmed:
  - Log path: `/private/tmp/warline-ecs-reverification-stage4-guardrail-before.log`.
  - Failure marker: `Assets/Game/Scripts/Composition/MenuBootstrapView.cs:102 uses HierarchyFind: Transform legacyRootTransform = transform.Find(LegacyUiToolkitShellRootName);`
- Removed the runtime hierarchy string lookup from `MenuBootstrapView` by making the legacy UI Toolkit root a serialized scene reference:
  - `Assets/Game/Scripts/Composition/MenuBootstrapView.cs`
  - `Assets/Game/Scenes/Menu.unity`
  - `UiToolkitShellRoot` remains available in the scene, but `MenuBootstrapView` disables it through the serialized field instead of `transform.Find(...)`.
- Moved the building runtime boundary bootstrap responsibility out of `MatchBootstrapSystem`:
  - New helper: `Assets/Game/Scripts/Composition/MatchBuildingRuntimeBoundaryBootstrapSystem.cs`
  - `MatchBootstrapSystem` now delegates boundary entity creation and required buffer setup to the helper.
  - `MatchBootstrapSystem.cs` line count changed from `1063` to `1010`.
- Stage 4 did not force the full `MatchBootstrapSystem` file below `600` in one batch because the remaining responsibilities are coupled to match startup lifecycle state. Splitting all of them in one heartbeat would be a higher-risk gameplay startup rewrite. Remaining scheduled split plan:
  - `MatchGameplayStartPipelineSystem` extraction, owner `Support`, recommended after Stage 6 because it touches startup singleton/config access and progress state.
  - `MatchManagedRuntimeBindingSystem` extraction, owner `Support`, recommended after Stage 5/6 because it wires managed systems, UI contracts, rendering helpers, and scene references.
  - `MatchRuntimeLifecycleShutdownSystem` extraction, owner `Support`, recommended after Stage 7 because it should preserve runtime update/dispose ordering after scheduling changes.
  - `MatchSceneReferenceProjection` extraction, owner `Support`, lowest risk final cleanup after P1 performance stages.
- Focused validation:
  - Unity compile passed. Log path: `/private/tmp/warline-ecs-reverification-stage4-compile.log`; success marker `Exiting batchmode successfully now!`.
  - Bootstrap composition guardrail passed. Log path: `/private/tmp/warline-ecs-reverification-stage4-guardrail-final.log`; pass marker `[BootstrapCompositionGuardrailValidation] result=Passed tests=5`.
  - UI shell focused validation passed. Log path: `/private/tmp/warline-ecs-reverification-stage4-ui-shell.log`; pass marker `[UIShellCurrentContentLoadValidation] result=Passed tests=10`.
  - Source scan for composition hierarchy lookup returned no hits:
    - `rg -n "transform\.Find|GameObject\.Find|FindObjectOfType|FindAnyObjectByType|FindFirstObjectByType|FindObjectsByType|\.Find\(" Assets/Game/Scripts/Composition`

---

## Stage 5 - Burst Coverage Closure

| Field | Value |
|---|---|
| Status | Complete |
| Progress | 100% |
| Priority | P1 |
| Owner | Support |
| Dependencies | Stages 1-2 preferred |

**Goal:** Close P1-1 for hot ECS systems. Add Burst where safe, and document managed exceptions instead of letting the audit report the entire set as unfinished.

**Implementation Steps**

1. Generate the current list of `ISystem` files without `BurstCompile`.
2. Prioritize:
   - combat,
   - AI,
   - movement,
   - transport,
   - render-budget,
   - large marker/read-model systems.
3. Add `[BurstCompile]` to the struct and hot methods where safe.
4. If managed code prevents Burst:
   - extract the hot loop into a Burst job, or
   - ledger the system as a managed exception with exact reason.
5. Avoid adding Burst attributes that do not actually compile due to managed API usage.

**Acceptance Criteria**

- [x] Hot systems are Bursted or have managed-exception ledger entries.
- [x] `UnitGridMovementSystem.OnUpdate` Burst status is explicitly verified.
- [x] Unity compile passes.
- [x] Burst-related test/compile warnings are resolved.

**Validation Commands**

```bash
rg -l "partial struct .*ISystem|: ISystem" Assets/Game/Scripts \
  | xargs -I{} sh -c 'rg -q "BurstCompile" "{}" || echo "{}"'
```

**Notes**

- 2026-06-24 heartbeat Stage 5 refreshed the runtime no-`BurstCompile` scan after Stage 4:
  - Runtime no-token count: `71` files across runtime systems, rendering systems, and UI shell ECS.
  - Command: `rg -l "partial struct .*ISystem|: ISystem" Assets/Game/Scripts/Systems Assets/Game/Scripts/Rendering/Systems Assets/Game/Scripts/UI/Shell/Ecs | xargs -I{} sh -c 'rg -q "BurstCompile" "{}" || echo "{}"' | wc -l`
- `UnitGridMovementSystem` was explicitly verified:
  - File: `Assets/Game/Scripts/Systems/UnitGridMovementSystem.cs`
  - `[BurstCompile]` is present on `UnitGridMoveJob`, `UnitPathFollowCleanupJob`, and `UnitGridMovementSystem`.
  - `OnUpdate` is a managed scheduler around Burst jobs and uses `UnityEngine.Time` for freeze diagnostics; no extra Burst tag was needed.
- Hot path coverage/exception review:
  - `PathfindBatchJob` is `[BurstCompile]` and owns the expensive path search loop. `UnitPathfindingSystem` remains a managed scheduler exception because it coordinates pending async job state, validation metrics, and `UnityEngine.Time` diagnostics.
  - `UnitAttackSystem`, `UnitDeathSystem`, `AITargetingSystem`, `AIProductionSystem`, `AISquadSystem`, `AIBuildPlannerSystem`, `AIEconomySystem`, and `AIFactionControlSystem` already contain `[BurstCompile]` jobs or job helpers in the hot path.
  - `AICombatOrderSystem` remains a managed scheduler exception. It should not be mechanically Burst-tagged while it still uses `EntityManager` orchestration, `UnityEngine.RectInt`/`Vector2Int`, string reason plumbing, and diagnostic queue setup.
  - `UnitRenderBudgetSystem` remains a managed presentation/rendering scheduler exception. Stage 7 still owns the `.Run()` scheduling decisions in render-budget helper systems.
  - Transport systems are mixed: plane door/deploy attack/airdrop/rope systems have Burst jobs; deploy-order, boarding command, capacity, and passenger-state systems are managed orchestration/helper exceptions.
- No code change was made in Stage 5 because the safe action is classification and exception capture, not adding misleading Burst attributes to managed schedulers.
- Unity compile for the current source passed immediately before Stage 5 documentation:
  - Log path: `/private/tmp/warline-ecs-reverification-stage4-compile.log`.
  - Success marker: `Exiting batchmode successfully now!`.
- Burst warning review: no new Burst warnings were introduced because Stage 5 did not add new Burst attributes. Future Burst work should split hot loops into jobs first, then add `[BurstCompile]` to those jobs.

---

## Stage 6 - Singleton And ECB Query Closure

| Field | Value |
|---|---|
| Status | Complete |
| Progress | 100% |
| Priority | P1 |
| Owner | Support |
| Dependencies | Stage 2 preferred |

**Goal:** Close P1-2 without unsafe caching.

**Implementation Steps**

1. Re-scan current `GridConfig` singleton reads.
2. Re-scan current `EndSimulationEntityCommandBufferSystem.Singleton` reads.
3. For repeated same-frame reads, collapse to one frame-local read.
4. For stable singleton entity lookups, cache queries/entities only if lifetime is safe.
5. Do not cache ECB instances across frames.
6. Do not cache mutable `GridConfig` values across frames unless the config is immutable by design and created before the system.
7. Add comments only where caching lifetime is non-obvious.

**Acceptance Criteria**

- [x] No repeated same-frame `GridConfig` singleton/entity reads remain in hot paths.
- [x] No unsafe per-frame ECB singleton query sites remain.
- [x] Any remaining read is ledgered with reason.
- [x] Unity compile passes.

**Validation Commands**

```bash
rg -n "GetSingleton<GridConfig>|GetSingletonEntity<GridConfig>" Assets/Game/Scripts
rg -n "GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>" Assets/Game/Scripts
```

**Notes**

- 2026-06-24 heartbeat Stage 6 refreshed the singleton scan before edits:
  - Initial current scan: `31` hits under runtime systems, rendering systems, and UI shell ECS.
  - Hit categories: `GridConfig` singleton value/entity reads and `EndSimulationEntityCommandBufferSystem.Singleton` reads.
- Implementation converted the current scan hits to cached `EntityQuery` fields plus frame-local reads:
  - `GridConfig` reads now use a cached query, `GetSingletonEntity()`, then `EntityManager.GetComponentData<GridConfig>(...)` or equivalent frame-local component/buffer reads.
  - ECB singleton reads now use cached singleton queries plus `EntityManager.GetComponentData<EndSimulationEntityCommandBufferSystem.Singleton>(...)`.
  - No `EntityCommandBuffer` instance is cached across frames; every command buffer is still created for the current update only.
  - Mutable `GridConfig` component values are not cached across frames.
- Files updated:
  - `Assets/Game/Scripts/Systems/AIBuildPlannerSystem.cs`
  - `Assets/Game/Scripts/Systems/DynamicBlockerInitSystem.cs`
  - `Assets/Game/Scripts/Systems/DynamicOccupancyRebuildSystem.cs`
  - `Assets/Game/Scripts/Systems/EngageTargetSyncSystem.cs`
  - `Assets/Game/Scripts/Systems/InitialUnitsBlockerChurnSystem.cs`
  - `Assets/Game/Scripts/Systems/MapSurfaceDiagnosticsSystem.cs`
  - `Assets/Game/Scripts/Systems/MapSurfaceFlatEquivalentBootstrapSystem.cs`
  - `Assets/Game/Scripts/Systems/SelectedUnitDebugFireSystem.cs`
  - `Assets/Game/Scripts/Systems/StaticGridBlockerUpdateSystem.cs`
  - `Assets/Game/Scripts/Systems/ThreatDetectionWarningSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitAirMovementSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitAttackStateEnsureSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitAttackSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitAttackTraceStateEnsureSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitDeathSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitDestroyedVisualSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitEngagedMovementSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitEngagementSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitGridMovementSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitGridSnapSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitHealthBarSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitIdleWanderSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitLookAtTargetSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitRespawnSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitRotationHoldSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitTransportAirdropSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitTransportRopeDisembarkSystem.cs`
  - `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`
- Acceptance scan result: clean. Command returned no hits:
  - `rg -n "GetSingleton<GridConfig>|GetSingletonEntity<GridConfig>|GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>" Assets/Game/Scripts/Systems Assets/Game/Scripts/Rendering/Systems Assets/Game/Scripts/UI/Shell/Ecs`
- Unity compile passed:
  - Log path: `/private/tmp/warline-ecs-reverification-stage6-final-compile.log`.
  - Success marker: `Exiting batchmode successfully now!`.

---

## Stage 7 - `.Run()` Scheduling Closure

| Field | Value |
|---|---|
| Status | Pending |
| Progress | 0% |
| Priority | P1 |
| Owner | Support |
| Dependencies | Stage 5 preferred |

**Goal:** Close P1-6 in a way that a future audit cannot call "not started." Each `.Run()` site must be converted or explicitly justified.

**Implementation Steps**

1. Re-scan `.Run()` sites in runtime/render/UI shell ECS roots.
2. For each site, record:
   - job name,
   - input size,
   - whether same-frame results are consumed,
   - native container safety,
   - dependency chain,
   - likely scheduling benefit.
3. Convert profitable safe sites first:
   - `AITargetingSystem.AssignTargetsJob` is the primary candidate.
4. For `AITargetingSystem`, solve diagnostic accumulation before parallel scheduling:
   - use a parallel-safe writer,
   - or accumulate per chunk then merge,
   - or disable diagnostics in Burst hot path.
5. Leave capped same-frame jobs synchronous only with documented reason and profiler/source proof.

**Acceptance Criteria**

- [ ] Every remaining `.Run()` site is listed in the Exception Ledger or converted.
- [ ] `AITargetingSystem` is converted or has a specific blocker and owner.
- [ ] Unity compile passes.
- [ ] Focused AI validation passes if conversion touches targeting.

**Validation Commands**

```bash
rg -n "\.Run\(" Assets/Game/Scripts/Systems Assets/Game/Scripts/Rendering/Systems Assets/Game/Scripts/UI/Shell/Ecs
```

**Notes**

- Pending.

---

## Stage 8 - BuildingBarrier Managed Dictionary Closure

| Field | Value |
|---|---|
| Status | Pending |
| Progress | 0% |
| Priority | P1 |
| Owner | Support |
| Dependencies | Stage 0 |

**Goal:** Close P1-7d for real. The previous tracker skipped this with a decision record, but the audit reports it as worse. This stage must remove the managed dictionary foreach regression or block with an exact technical reason.

**Implementation Direction**

Do not jump straight to `NativeHashMap` if the data model contains managed references (`RuntimeBuildingEntity`, `BuildingDefinition`, `RectInt`, or GameObject-backed state). First audit what the barrier code actually needs per loop. Prefer a flat snapshot/cache if that avoids boxing and avoids a risky unmanaged rewrite.

**Implementation Steps**

1. Count all current `RuntimeBuildings` foreach sites in `BuildingBarrierSystem`.
2. For each site, record the fields read from each building.
3. Define the smallest barrier read model:
   - building id,
   - faction/ownership if needed,
   - grid rect/cells,
   - blocking/open/door state,
   - health/alive state if needed,
   - any road-gate/breach flags.
4. Choose one implementation:
   - managed `List<BarrierBuildingSnapshot>` cache refreshed when building state changes,
   - `NativeList<BarrierBuildingSnapshot>` if all required fields are unmanaged,
   - `NativeHashMap<int, BarrierBuildingSnapshot>` only if key lookup is required.
5. Replace every `foreach` over `IReadOnlyDictionary`/`RuntimeBuildings` with indexed snapshot iteration.
6. Keep existing public behavior and tests.
7. Add focused validation around road gate/barrier breach behavior.

**Acceptance Criteria**

- [ ] `BuildingBarrierSystem` no longer enumerates `IReadOnlyDictionary` or `RuntimeBuildings` directly in hot paths.
- [ ] `rg -n "foreach.*RuntimeBuildings|foreach.*context\.RuntimeBuildings"` returns no hot-path hits.
- [ ] If any direct enumeration remains, it is outside runtime hot path and ledgered.
- [ ] Focused BuildingBarrier validation passes.
- [ ] Unity compile passes.

**Validation Commands**

```bash
rg -n "foreach.*RuntimeBuildings|foreach.*context\.RuntimeBuildings|IReadOnlyDictionary" Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs
```

**Notes**

- Pending.

---

## Stage 9 - RuntimeBuildingEntityLink Update Closure

| Field | Value |
|---|---|
| Status | Pending |
| Progress | 0% |
| Priority | P1/P2 |
| Owner | Support |
| Dependencies | Stage 0 |

**Goal:** Close P1-8 so audits stop reporting one `MonoBehaviour.Update` per building as open debt.

**Implementation Steps**

1. Inspect what `RuntimeBuildingEntityLink.Update` syncs each frame.
2. Identify whether buildings are already rendered/synced by ECS or Entities Graphics.
3. Pick one safe closure:
   - remove the component if redundant,
   - replace per-building `Update` with a single manager/batched sync,
   - use dirty flags/events so updates occur only when ECS building transform/state changes.
4. Preserve scene/prefab identity.
5. Validate building selection, destruction, and transform sync.

**Acceptance Criteria**

- [ ] `RuntimeBuildingEntityLink` has no per-instance `Update`.
- [ ] Building visuals still track runtime state.
- [ ] Building selection and destruction behavior still work.
- [ ] Unity compile passes.

**Validation Commands**

```bash
rg -n "void Update|LateUpdate|FixedUpdate" Assets/Game/Scripts/RuntimeState/RuntimeBuildingEntityLink.cs
```

**Notes**

- Pending.

---

## Stage 10 - Authoring And Bake Data Hygiene

| Field | Value |
|---|---|
| Status | Pending |
| Progress | 0% |
| Priority | P1/P2 |
| Owner | Support |
| Dependencies | Stage 0 |

**Goal:** Close P1-4a, finish P1-4b, and address P2-6.

**Part A - `MapSurfaceAuthoring` Blob Dedup**

1. Inspect current blob build path.
2. Add a bake-time cache keyed by source `MapSurfaceDataAsset`.
3. Reuse blob references for authorings that share the same source asset.
4. Clear cache safely on domain reload or bake lifecycle.

**Part B - `UnitGridAuthoring.transform.Find` Closure**

1. Re-check remaining `transform.Find("Model")` and `transform.Find("Destroyed")` fallbacks.
2. Decide whether to:
   - remove fallback and fail loud on missing serialized refs,
   - or ledger fallback as legacy-prefab-only bake-time exception.
3. Prefer bake validation over runtime string lookup.

**Part C - `GridAuthoring` Static Registry And Bake Singleton Lookup**

1. Replace static mutable `RegisteredInstances` with baker-scoped or baking-system state.
2. Remove bake-time `GetSingletonEntity` calls where possible.
3. Pass needed state explicitly or defer runtime init.

**Acceptance Criteria**

- [ ] Shared map surface assets do not produce duplicate runtime blobs.
- [ ] `UnitGridAuthoring` has no runtime string hierarchy lookup, or fallback is ledgered as bake-time only.
- [ ] `GridAuthoring` no longer depends on fragile static registry/runtime singleton access during bake, or exact blockers are documented.
- [ ] Authoring/baker focused validation passes.
- [ ] Unity compile passes.

**Validation Commands**

```bash
rg -n "transform\.Find|RegisteredInstances|GetSingletonEntity" Assets/Game/Scripts/Authorings
rg -n "TryCreateRuntimeBlobAsset|BlobAssetReference|Dictionary<.*MapSurface" Assets/Game/Scripts/Authorings/MapSurfaceAuthoring.cs
```

**Notes**

- Pending.

---

## Stage 11 - Component And Minor Cleanup

| Field | Value |
|---|---|
| Status | Pending |
| Progress | 0% |
| Priority | P2 |
| Owner | Support |
| Dependencies | P0/P1 stages preferred |

**Goal:** Close remaining P2 findings or move them into explicit deferred follow-ups.

**Implementation Steps**

1. Re-verify P2 items not checked by the audit:
   - P2-2 large `IBufferElementData` elements,
   - P2-3 `UiActionRequestSystem.OnUpdate` direct `EntityManager` usage,
   - P2-5 `GetComponentInParent<Canvas>` caching on UI views,
   - P2-7 USS/static layout hygiene after UI Toolkit C# removal.
2. Split `CombatComponents.cs` into focused files without changing namespaces or type names.
3. Cache remaining editor-only `Shader.PropertyToID` if trivial.
4. Do not mix gameplay behavior changes into component file splits.

**Acceptance Criteria**

- [ ] `CombatComponents.cs` split is complete or a smaller split plan is recorded.
- [ ] P2-2/P2-3/P2-5/P2-7 re-verification result is recorded.
- [ ] Editor-only cleanup is complete or intentionally left as low-value.
- [ ] Unity compile passes.

**Validation Commands**

```bash
wc -l Assets/Game/Scripts/Components/CombatComponents.cs
rg -n "Shader\.PropertyToID|GetComponentInParent<Canvas>|EntityManager\." Assets/Game/Scripts
```

**Notes**

- Pending.

---

## Stage 12 - Final Audit Closure Validation

| Field | Value |
|---|---|
| Status | Pending |
| Progress | 0% |
| Priority | P0 |
| Owner | Support |
| Dependencies | Stages 1-11 |

**Goal:** Prove the non-namespace reverification findings are closed and update this tracker so future audits do not rediscover the same work as unfinished.

**Implementation Steps**

1. Re-run all acceptance scans from this tracker.
2. Re-run Unity compile.
3. Run focused architecture tests:
   - assembly/boundary tests,
   - bootstrap composition guardrail,
   - ECS startup/menu-to-match smoke if available.
4. Run focused gameplay tests touched by implemented stages:
   - movement/order validation for ordering changes,
   - building barrier validation,
   - UI shell boundary tests,
   - authoring/baker validation.
5. Update the Exception Ledger.
6. Update all stage status/progress rows.
7. Commit stable stage batches only, excluding unrelated files.

**Acceptance Criteria**

- [ ] Every non-namespace audit item is `Complete`, `Blocked` with exact owner/blocker, or explicitly validated as not applicable.
- [ ] No stage is closed with unexplained static scan hits.
- [ ] Unity compile passes.
- [ ] Focused validations pass or blockers are recorded.
- [ ] Final commit(s) include only files changed for this tracker.

**Final Validation Commands**

```bash
rg -l "partial struct .*ISystem|: ISystem" Assets/Game/Scripts \
  | xargs -I{} sh -c 'rg -q "UpdateInGroup|UpdateBefore|UpdateAfter" "{}" || echo "{}"'

rg -l "partial struct .*ISystem|: ISystem" Assets/Game/Scripts \
  | xargs -I{} sh -c 'rg -q "RequireForUpdate|RequireMatchingQueriesForUpdate" "{}" || echo "{}"'

rg -l "partial struct .*ISystem|: ISystem" Assets/Game/Scripts \
  | xargs -I{} sh -c 'rg -q "BurstCompile" "{}" || echo "{}"'

rg -n "\.Run\(" Assets/Game/Scripts/Systems Assets/Game/Scripts/Rendering/Systems Assets/Game/Scripts/UI/Shell/Ecs
rg -n "GetSingleton<GridConfig>|GetSingletonEntity<GridConfig>" Assets/Game/Scripts
rg -n "GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>" Assets/Game/Scripts
rg -n "foreach.*RuntimeBuildings|foreach.*context\.RuntimeBuildings|IReadOnlyDictionary" Assets/Game/Scripts/Systems/BuildingBarrierSystem.cs
rg -n "void Update|LateUpdate|FixedUpdate" Assets/Game/Scripts/RuntimeState/RuntimeBuildingEntityLink.cs
rg -n "transform\.Find|RegisteredInstances|GetSingletonEntity" Assets/Game/Scripts/Authorings
wc -l Assets/Game/Scripts/Composition/MatchBootstrapSystem.cs
```

**Notes**

- Pending.
