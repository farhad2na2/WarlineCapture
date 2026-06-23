# Architecture & Performance Remediation Progress Tracker

**Project:** WarlineCapture
**Tracker updated:** 2026-06-23
**Scope:** `Assets/Game/Scripts`, `Assets/Tests`, asmdefs, focused Unity validation.
**Purpose:** Track safe remediation work from the architecture/performance audit without mixing unrelated refactors or applying stale recommendations literally.

## Status Legend

| Status | Meaning |
|---|---|
| `Pending` | Not started. |
| `In Progress` | Currently being implemented or validated. |
| `Blocked` | Cannot continue until the blocker is resolved. |
| `Complete` | Implementation and required validation are done. |
| `Skipped` | Intentionally not implemented; reason documented. |

## Current Local Snapshot

| Item | Current value |
|---|---:|
| `Assets/Game/Scripts` C# files | 747 |
| C# files without namespace | 743 |
| Project/test asmdefs with empty `rootNamespace` | 16 |
| Files containing `ISystem` structs | About 125 |
| `ISystem` files still needing guard review | About 55 |
| Confirmed profiler baseline | No |
| `Game.Runtime -> Game.UI.Runtime` dependency | No |
| `Game.UI.Contracts` pure implementation state | Needs cleanup |

## Non-Negotiable Guardrails

- [ ] Do not trigger Android builds. Android builds remain user-triggered.
- [ ] Do not commit or revert unrelated in-flight files from other lanes.
- [ ] Re-check source before each implementation step; do not implement stale audit instructions literally.
- [ ] Do not add `RequireForUpdate` to a system whose job is to create the entity/query it would require.
- [ ] Do not cache ECS singleton component data in `OnCreate` unless the data is immutable and guaranteed to exist before system creation.
- [ ] Do not cache an `EntityCommandBuffer` across frames. Command buffers are frame-scoped.
- [ ] Do not move UI helper implementations into `Game.UI.Runtime` while `Game.Runtime` still references those helpers.
- [ ] Do not mix namespace migration with gameplay/performance behavior changes.

## Stage Overview

| Stage | Priority | Status | Owner | Depends on | Blocks | Validation status |
|---|---|---|---|---|---|---|
| 0 - Stabilize Baseline | P0 | Pending | TBD | None | All perf claims | Not run |
| 1 - Low-Risk Confirmed GC Fixes | P0 | Pending | TBD | Stage 0 preferred | Later perf comparison | Not run |
| 2 - Refresh ECS Guard Audit + Batch Guards | P0 | Pending | TBD | Stage 0 preferred | Stage 4, Stage 7 | Not run |
| 3 - UI Shell ECS Creation Safety | P0 | Pending | TBD | Stage 2 preferred | UI shell cleanup | Not run |
| 4 - ECS System Ordering | P1 | Pending | TBD | Stage 2 | Stage 7, bootstrap split work | Not run |
| 5 - Safe Singleton + ECB Usage Review | P1 | Pending | TBD | Stage 2 preferred | Hot-path singleton cleanup | Not run |
| 6 - `Game.UI.Contracts` Cleanup | P1 | Pending | TBD | Current architecture boundary | UI contracts purity | Not run |
| 7 - Burst + Parallelism | P1 | Pending | TBD | Stages 2, 4 | Hot ECS perf work | Not run |
| 8 - Runtime Building Transform Link Review | P1 | Pending | TBD | Stage 0 preferred | Building transform optimization | Not run |
| 9 - Namespace Migration | P2 | Pending | TBD | Separate refactor window | Namespace/rootNamespace cleanup | Not run |
| 10 - Structural Hygiene | P2 | Pending | TBD | Stages 0-9 as relevant | Long-term maintainability | Not run |

## Validation Ladder

- [ ] Static checks with targeted `rg` commands.
- [ ] Unity compile with zero errors.
- [ ] `ScriptArchitectureAlignmentContractTests` after asmdef or dependency-boundary changes.
- [ ] Focused EditMode tests for touched area.
- [ ] PlayMode smoke when runtime/bootstrap/UI shell code is touched.
- [ ] Profiler compare against baseline before claiming performance improvement.

---

## Stage 0 - Stabilize Baseline

| Field | Value |
|---|---|
| Status | Pending |
| Priority | P0 |
| Owner | TBD |
| Dependencies | None |
| Blocks | All measured performance claims |
| Validation status | Not run |

**Goal:** Establish a clean reference point before changing behavior.

**Checklist**

- [ ] Check `git status --short --untracked-files=no` and record unrelated dirty files.
- [ ] Run Unity compile before code changes.
- [ ] Run `ScriptArchitectureAlignmentContractTests`.
- [ ] Capture a 60 second Profiler baseline in Editor with a live match.
- [ ] Save raw capture as `ProfilerCaptures/baseline_2026-06-23.raw`.
- [ ] Record main-thread ms/frame, GC alloc/frame, and top hot systems in `Design/AgentReports/2026-06-23_perf_baseline_numbers.md`.

**Acceptance**

- [ ] Compile is clean before remediation.
- [ ] Architecture tests pass or known failures are documented.
- [ ] Baseline numbers exist before any performance claim is made.

**Notes**

- Pending.

---

## Stage 1 - Low-Risk Confirmed GC Fixes

| Field | Value |
|---|---|
| Status | Pending |
| Priority | P0 |
| Owner | TBD |
| Dependencies | Stage 0 preferred |
| Blocks | Later perf comparison |
| Validation status | Not run |

**Goal:** Remove obvious or confirmed managed allocations without changing gameplay order or architecture.

**Checklist**

- [ ] `TerrainLodHeightSwitch`: replace `Camera.allCameras` array access with `Camera.allCamerasCount` plus `Camera.GetCameraAt(i)`.
- [ ] Keep/correct the existing resolved-camera cache.
- [ ] `MissileTrailVfxView`: confirm in Profiler whether concrete `Dictionary<TKey,TValue>` foreach allocates.
- [ ] If `MissileTrailVfxView` allocates, replace with a no-allocation iteration approach.
- [ ] If `MissileTrailVfxView` does not allocate, leave it unchanged and mark skipped.
- [ ] Cache method-path `Shader.PropertyToID` calls as `static readonly int` fields where repeated.
- [ ] Cache `GetComponentInParent<Canvas>` in repeated UI input paths.

**Static checks**

- [ ] `rg "Camera\.allCameras[^C]" Assets/Game/Scripts`
- [ ] `rg "Shader\.PropertyToID" Assets/Game/Scripts/Systems Assets/Game/Scripts/Rendering Assets/Game/Scripts/UI`
- [ ] `rg "GetComponentInParent<Canvas>" Assets/Game/Scripts/UI`

**Acceptance**

- [ ] No gameplay order changes.
- [ ] MonoBehaviour ownership remains in UI/rendering assemblies.
- [ ] Visual smoke confirms terrain LOD still switches correctly.
- [ ] Combat smoke confirms missile trails still render if touched.

**Notes**

- Pending.

---

## Stage 2 - Refresh ECS Guard Audit, Then Add Guards in Batches

| Field | Value |
|---|---|
| Status | Pending |
| Priority | P0 |
| Owner | TBD |
| Dependencies | Stage 0 preferred |
| Blocks | Stage 4, Stage 7 |
| Validation status | Not run |

**Goal:** Stop systems from running when required data does not exist, without blocking bootstrap systems from creating that data.

**Checklist**

- [ ] Generate current list of `ISystem` files without `RequireForUpdate`, `[RequireMatchingQueriesForUpdate]`, or intentional-run comment.
- [ ] Classify each unguarded system as `Creator/bootstrap`, `Optional bridge/read model`, `Simulation hot path`, or `Diagnostic`.
- [ ] Add guards to first batch of 5-10 systems.
- [ ] Compile and smoke test first batch.
- [ ] Continue batches until reviewed list is resolved.
- [ ] Add `// intentionally runs every frame` only where truly required and throttled.

**Forbidden pattern**

Do not add this to systems that create the queried entity:

```csharp
state.RequireForUpdate(boundaryQuery);
```

That can prevent the system from ever running to create the boundary.

**Static checks**

- [ ] `rg -l "struct .*ISystem|struct .* : ISystem" Assets/Game/Scripts --type cs`
- [ ] For each file, check for `RequireMatchingQueriesForUpdate`, `RequireForUpdate`, or intentional-run comment.

**Acceptance**

- [ ] Creator/bootstrap systems are not blocked by requirements for entities they create.
- [ ] Optional bridge/read-model systems use count checks where appropriate.
- [ ] Hot simulation systems have clear required data guards.
- [ ] Compile passes after each batch.
- [ ] PlayMode smoke passes after runtime/bootstrap batches.

**Notes**

- Pending.

---

## Stage 3 - Fix UI Shell ECS Creation Without Blocking Bootstrap

| Field | Value |
|---|---|
| Status | Pending |
| Priority | P0 |
| Owner | TBD |
| Dependencies | Stage 2 preferred |
| Blocks | UI shell cleanup |
| Validation status | Not run |

**Files**

- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs`
- Related UI shell ECS tests.

**Goal:** Make shell ECS initialization safer without breaking first-run boundary creation.

**Checklist**

- [ ] Keep `UiShellBoundarySystem` in initialization ownership.
- [ ] Do not add `RequireForUpdate(boundaryQuery)` to `UiShellBoundarySystem`.
- [ ] Make boundary creation explicitly one-shot.
- [ ] Add defensive handling for duplicate boundary entities.
- [ ] Avoid `GetSingletonEntity` unless exactly one entity exists.
- [ ] Move repeated "ensure component/buffer exists" work out of steady path where practical.
- [ ] Avoid creating selection/building command queue entities deep inside active request processing in `UiActionRequestSystem`.
- [ ] Prefer initialization/boundary setup, or a one-shot ECB path followed by processing next frame.

**Acceptance**

- [ ] Shell boundary is created on a clean world.
- [ ] Duplicate boundary state cannot throw an unhelpful singleton exception.
- [ ] UI shell route still works.
- [ ] Loading progress still works.
- [ ] Armory category, build drawer, and match HUD read models still work.

**Validation**

- [ ] Unity compile.
- [ ] UI shell focused EditMode tests.
- [ ] Menu -> Match -> exit smoke.

**Notes**

- Pending.

---

## Stage 4 - ECS System Ordering, Producer/Consumer First

| Field | Value |
|---|---|
| Status | Pending |
| Priority | P1 |
| Owner | TBD |
| Dependencies | Stage 2 |
| Blocks | Stage 7, bootstrap split work |
| Validation status | Not run |

**Goal:** Make update order explicit only where order matters.

**Checklist**

- [ ] Build current producer/consumer table before editing.
- [ ] Prioritize path request/solve -> movement apply.
- [ ] Prioritize UI/action request -> command systems.
- [ ] Prioritize AI economy -> faction control -> build planner -> production -> squad -> targeting -> combat order.
- [ ] Prioritize building config/projection -> grid composition -> spawn prefab -> building commands.
- [ ] Prioritize transport board/deploy/pickup/drop chains.
- [ ] Add `[UpdateInGroup]` to systems with a clear group.
- [ ] Add `[UpdateAfter]` on consumers where a producer must run first.
- [ ] Avoid broad ordering attributes added only to satisfy a grep.

**Acceptance**

- [ ] Known producer/consumer pairs have explicit ordering.
- [ ] No default-order changes are made without a stated reason.
- [ ] Full match smoke has no movement, attack, transport, or AI regressions.

**Validation**

- [ ] Unity compile.
- [ ] PlayMode match lifecycle smoke.
- [ ] Focused tests for pathing, selection commands, and transport if touched.

**Notes**

- Pending.

---

## Stage 5 - Safe Singleton and ECB Usage Review

| Field | Value |
|---|---|
| Status | Pending |
| Priority | P1 |
| Owner | TBD |
| Dependencies | Stage 2 preferred |
| Blocks | Hot-path singleton cleanup |
| Validation status | Not run |

**Goal:** Reduce repeated singleton lookup cost only where it is safe and measured.

**Checklist**

- [ ] Do not implement blanket "cache all singletons in `OnCreate`" guidance.
- [ ] Cache `EntityQuery` in `OnCreate` where useful.
- [ ] Cache singleton entity only after query is known to have exactly one entity.
- [ ] Re-read component data when the component can change.
- [ ] Use `ComponentLookup<T>` with `.Update(ref state)` for repeated random access.
- [ ] Require ECB singleton if needed, but create command buffer per update.
- [ ] Optimize singleton lookups only when profiler shows they matter or they are in a hot loop.

**Acceptance**

- [ ] No stale cached `GridConfig` data if grid can change during boot/reset.
- [ ] No cached frame-scoped ECB.
- [ ] No startup exceptions from reading singleton data before it exists.

**Validation**

- [ ] Unity compile.
- [ ] Match start/exit/restart smoke.

**Notes**

- Pending.

---

## Stage 6 - Clean `Game.UI.Contracts` Without Reintroducing Runtime/UI Cycles

| Field | Value |
|---|---|
| Status | Pending |
| Priority | P1 |
| Owner | TBD |
| Dependencies | Current architecture boundary |
| Blocks | UI contracts purity |
| Validation status | Not run |

**Goal:** Keep contracts as DTOs/interfaces while preserving the current assembly direction.

**Known implementation-like types currently in contracts**

- [ ] `BattleHudRuntimeFeedbackBoundary`
- [ ] `UiShellRuntimeGateway` static facade and null implementation
- [ ] `NullMatchIntroStateQuery`
- [ ] `TacticalCommandFeedbackText`

**Checklist**

- [ ] Inventory references to each implementation-like type.
- [ ] For runtime callers, replace direct UI helper usage with contract-level commands, DTOs, or sink interfaces.
- [ ] Keep only pure interfaces, enums, and DTO/read models in `Game.UI.Contracts`.
- [ ] Move UI-only implementations to `Game.UI.Runtime` only after no runtime assembly references them.
- [ ] Design any gateway locator so it does not require `Game.Runtime -> Game.UI.Runtime`.
- [ ] Avoid mutable service-locator logic in the pure contracts leaf.
- [ ] Convert `TrySetLoadingProgress` to the same UI-to-ECS queue discipline as other shell writes, unless a direct write is explicitly documented as intentional.
- [ ] Add or update architecture tests to prevent regression.

**Acceptance**

- [ ] `Game.UI.Contracts` has no UnityEngine-dependent helper implementation unless documented as temporary.
- [ ] `Game.Runtime` does not reference `Game.UI.Runtime`.
- [ ] UI shell transitions still work.
- [ ] Loading progress still works.

**Validation**

- [ ] Unity compile.
- [ ] `ScriptArchitectureAlignmentContractTests`.
- [ ] UI shell and HUD feedback tests.
- [ ] Menu -> Match smoke.

**Notes**

- Pending.

---

## Stage 7 - Burst and Parallelism Only After Guards Are Stable

| Field | Value |
|---|---|
| Status | Pending |
| Priority | P1 |
| Owner | TBD |
| Dependencies | Stages 2 and 4 |
| Blocks | Hot ECS perf work |
| Validation status | Not run |

**Goal:** Improve hot ECS system CPU cost without fighting managed APIs or startup behavior.

**Checklist**

- [ ] Use profiler baseline to select hot systems.
- [ ] Add `[BurstCompile]` only to systems/methods that are Burst-compatible.
- [ ] If `OnUpdate` uses managed APIs, keep `OnUpdate` managed and extract hot loop into a Burst job.
- [ ] Do not hide `Debug.Log`, string formatting, managed components, or EntityManager managed calls inside Burst paths.
- [ ] Convert `.Run()` to `ScheduleParallel` only when dependencies and diagnostic collection are parallel-safe.
- [ ] Confirm Burst inspector has zero compile errors.

**Acceptance**

- [ ] Burst inspector has zero compile errors.
- [ ] Profiler shows improvement or no regression.
- [ ] Jobs debugger/race detection shows no introduced safety issues.

**Validation**

- [ ] Unity compile with Burst enabled.
- [ ] Focused PlayMode tests for touched systems.
- [ ] Profiler compare against baseline.

**Notes**

- Pending.

---

## Stage 8 - Runtime Building Transform Link Review

| Field | Value |
|---|---|
| Status | Pending |
| Priority | P1 |
| Owner | TBD |
| Dependencies | Stage 0 preferred |
| Blocks | Building transform optimization |
| Validation status | Not run |

**Goal:** Remove or reduce per-building MonoBehaviour `Update` cost without breaking visible buildings.

**Checklist**

- [ ] Profile `RuntimeBuildingEntityLink.Update` with realistic building counts.
- [ ] Confirm whether buildings are rendered by Entities Graphics.
- [ ] Confirm whether GameObjects are still required for colliders, anchors, selection, or UI.
- [ ] If GameObjects are required, prefer centralized sync or dirty-check early-out.
- [ ] If GameObjects are not required, remove link only after proving visuals, selection, blockers, and production still work.

**Acceptance**

- [ ] Building visuals remain visible and correctly positioned.
- [ ] Selection and production interactions still work.
- [ ] Update cost is reduced or documented as negligible.

**Validation**

- [ ] Match smoke with city/building placement/production.
- [ ] Profiler compare.

**Notes**

- Pending.

---

## Stage 9 - Namespace Migration as a Separate Architecture Refactor

| Field | Value |
|---|---|
| Status | Pending |
| Priority | P2 |
| Owner | TBD |
| Dependencies | Separate refactor window |
| Blocks | Namespace/rootNamespace cleanup |
| Validation status | Not run |

**Goal:** Add namespaces aligned with asmdefs without mixing with gameplay/performance changes.

**Namespace target checklist**

- [ ] `Game.Components` -> `Game.Components`
- [ ] `Game.Configs` -> `Game.Configs`
- [ ] `Game.Catalog.Contracts` -> `Game.Catalog.Contracts`
- [ ] `Game.Rendering.Contracts` -> `Game.Rendering.Contracts`
- [ ] `Game.UI.Contracts` -> `Game.UI.Contracts`
- [ ] `Game.Authoring` -> `Game.Authoring`
- [ ] `Game.Rendering` -> `Game.Rendering`
- [ ] `Game.UI.Runtime` -> `Game.UI.Runtime`
- [ ] `Game.UI.Toolkit` -> `Game.UI.Toolkit`
- [ ] `Game.UI.Shell.Ecs` -> `Game.UI.Shell.Ecs`
- [ ] `Game.UI.Shell.Contracts.Ecs` -> `Game.UI.Shell.Contracts.Ecs`
- [ ] `Game.Runtime` -> `Game.Runtime`
- [ ] `Game.Composition` -> `Game.Composition`
- [ ] `Game.Editor` -> `Game.Editor`
- [ ] `Game.Tests.Editor` -> `Game.Tests.Editor`
- [ ] `Game.Tests.PlayMode` -> `Game.Tests.PlayMode`

**Checklist**

- [ ] Verify Unity language version.
- [ ] Use file-scoped namespaces only if compiler supports them; otherwise use block-scoped namespaces.
- [ ] Update one leaf assembly at a time.
- [ ] Set that asmdef's `rootNamespace`.
- [ ] Compile after each assembly.
- [ ] Update tests and `InternalsVisibleTo` only when needed.
- [ ] Move inward from leaves to runtime/composition.

**Acceptance**

- [ ] No source files in `Assets/Game/Scripts` remain in global namespace except intentional files such as `AssemblyInfo.cs`.
- [ ] All project asmdefs have non-empty `rootNamespace`.
- [ ] Unity compile passes.
- [ ] Architecture tests pass.

**Notes**

- Pending.

---

## Stage 10 - Lower-Priority Structural Hygiene

| Field | Value |
|---|---|
| Status | Pending |
| Priority | P2 |
| Owner | TBD |
| Dependencies | Stages 0-9 as relevant |
| Blocks | Long-term maintainability |
| Validation status | Not run |

**Goal:** Improve maintainability after correctness/perf-sensitive work is stable.

**Checklist**

- [ ] Split `CombatComponents.cs` by domain after namespaces are in place.
- [ ] Review oversized `IBufferElementData` types and split rarely-read strings into blobs or side buffers only when runtime cost is proven.
- [ ] Clean `GridAuthoring` static bake-time state and runtime-world assumptions.
- [ ] Move static UI Toolkit layout rules to USS where they are not dynamic.
- [ ] Deduplicate bake-time blobs where repeated authoring assets produce identical blob data.

**Acceptance**

- [ ] No gameplay behavior change.
- [ ] Compile passes after each small refactor.
- [ ] Focused tests pass after each small refactor.

**Notes**

- Pending.

---

## Recommended Execution Order

1. [ ] Stage 0 - Stabilize baseline and current validation.
2. [ ] Stage 1 - Low-risk confirmed GC fixes.
3. [ ] Stage 2 - ECS guard audit and small batches.
4. [ ] Stage 3 - UI shell ECS initialization safety.
5. [ ] Stage 4 - Explicit ordering for confirmed producer/consumer chains.
6. [ ] Stage 5 - Singleton/ECB review only where safe and measured.
7. [ ] Stage 6 - UI contracts cleanup with bridge/adaptor design.
8. [ ] Stage 7 - Burst/parallelism on proven hot systems.
9. [ ] Stage 8 - Building transform link review.
10. [ ] Stage 9 - Namespace migration as its own refactor.
11. [ ] Stage 10 - Lower-priority structural hygiene.

## Merge Gate

- [ ] Unity compile has zero errors.
- [ ] No new architecture-boundary violations.
- [ ] No unrelated files are committed.
- [ ] Focused tests for touched code pass.
- [ ] Match smoke passes when runtime/bootstrap/UI shell code is touched.
- [ ] Performance claims include before/after profiler evidence.

