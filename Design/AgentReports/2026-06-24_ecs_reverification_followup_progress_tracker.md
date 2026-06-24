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
| 2 - `RequireForUpdate` Closure | P0 | Pending | 0% | Support | Stage 0 | No live unguarded systems except documented intentional exceptions. |
| 3 - UI Shell Boundary Structural Safety | P1 | Pending | 0% | Support | Stage 0 | No unsafe structural `EntityManager` creation path in `OnUpdate`, or one-shot path isolated and documented. |
| 4 - Bootstrap Split Unblock And Continue | P1 | Pending | 0% | Support | Stages 1-3 preferred | Bootstrap guardrail passes and `MatchBootstrapSystem` is below target or has remaining split plan. |
| 5 - Burst Coverage Closure | P1 | Pending | 0% | Support | Stages 1-2 preferred | Hot `ISystem`s are Bursted or managed exceptions are documented. |
| 6 - Singleton And ECB Query Closure | P1 | Pending | 0% | Support | Stage 2 preferred | Current per-frame `GridConfig`/ECB singleton findings closed. |
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
| Status | Pending |
| Progress | 0% |
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

- [ ] Acceptance scan returns only ledgered exceptions.
- [ ] No startup/bootstrap system is accidentally prevented from creating required entities.
- [ ] Unity compile passes.
- [ ] Menu-to-match startup smoke still passes if practical.

**Validation Commands**

```bash
rg -l "partial struct .*ISystem|: ISystem" Assets/Game/Scripts \
  | xargs -I{} sh -c 'rg -q "RequireForUpdate|RequireMatchingQueriesForUpdate" "{}" || echo "{}"'
```

**Notes**

- Pending.

---

## Stage 3 - UI Shell Boundary Structural Safety

| Field | Value |
|---|---|
| Status | Pending |
| Progress | 0% |
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

- [ ] No unsafe repeated structural creation path remains in `OnUpdate`.
- [ ] If a one-shot path remains, it is self-disabled, documented, and ledgered.
- [ ] UI shell focused tests pass if available.
- [ ] Unity compile passes.

**Validation Commands**

```bash
rg -n "CreateEntity|AddComponentData|AddBuffer|SetComponentData" Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs
```

**Notes**

- Pending.

---

## Stage 4 - Bootstrap Split Unblock And Continue

| Field | Value |
|---|---|
| Status | Pending |
| Progress | 0% |
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

- [ ] Guardrail test passes.
- [ ] `MatchBootstrapSystem.cs` line count decreases meaningfully.
- [ ] Remaining responsibilities are listed if target is not reached in one pass.
- [ ] Target line count is below `600`, or the remainder is split into scheduled follow-up stages with exact owners.
- [ ] Unity compile passes.

**Validation Commands**

```bash
wc -l Assets/Game/Scripts/Composition/MatchBootstrapSystem.cs
rg -n "transform\.Find|GameObject\.Find|FindObjectOfType|FindAnyObjectByType" Assets/Game/Scripts/Composition
```

**Notes**

- Pending.

---

## Stage 5 - Burst Coverage Closure

| Field | Value |
|---|---|
| Status | Pending |
| Progress | 0% |
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

- [ ] Hot systems are Bursted or have managed-exception ledger entries.
- [ ] `UnitGridMovementSystem.OnUpdate` Burst status is explicitly verified.
- [ ] Unity compile passes.
- [ ] Burst-related test/compile warnings are resolved.

**Validation Commands**

```bash
rg -l "partial struct .*ISystem|: ISystem" Assets/Game/Scripts \
  | xargs -I{} sh -c 'rg -q "BurstCompile" "{}" || echo "{}"'
```

**Notes**

- Pending.

---

## Stage 6 - Singleton And ECB Query Closure

| Field | Value |
|---|---|
| Status | Pending |
| Progress | 0% |
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

- [ ] No repeated same-frame `GridConfig` singleton/entity reads remain in hot paths.
- [ ] No unsafe per-frame ECB singleton query sites remain.
- [ ] Any remaining read is ledgered with reason.
- [ ] Unity compile passes.

**Validation Commands**

```bash
rg -n "GetSingleton<GridConfig>|GetSingletonEntity<GridConfig>" Assets/Game/Scripts
rg -n "GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>" Assets/Game/Scripts
```

**Notes**

- Pending.

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
