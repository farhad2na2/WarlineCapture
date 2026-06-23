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
| `ISystem` files still needing guard review | 46 static matches; 0 unclassified actionable matches |
| Confirmed profiler baseline | Yes |
| `Game.Runtime -> Game.UI.Runtime` dependency | No |
| `Game.UI.Contracts` pure implementation state | Static cleanup complete; validation passed |

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
| 0 - Stabilize Baseline | P0 | Complete | Support | None | All perf claims | Profiler baseline parsed and baseline report written |
| 1 - Low-Risk Confirmed GC Fixes | P0 | Complete | Support | Stage 0 preferred | Later perf comparison | Compile, architecture validation, and match smoke passed |
| 2 - Refresh ECS Guard Audit + Batch Guards | P0 | Complete | Support | Stage 0 preferred | Stage 4, Stage 7 | Architecture validation and match smoke passed |
| 3 - UI Shell ECS Creation Safety | P0 | Complete | Support | Stage 2 preferred | UI shell cleanup | Architecture, UI shell focused validation, and match smoke passed |
| 4 - ECS System Ordering | P1 | Complete | Support | Stage 2 | Stage 7, bootstrap split work | Architecture, match smoke, pathfinding, selection command, and transport validation passed |
| 5 - Safe Singleton + ECB Usage Review | P1 | Complete | Support | Stage 2 preferred | Hot-path singleton cleanup | Audit complete; no code changes recommended without profiler proof |
| 6 - `Game.UI.Contracts` Cleanup | P1 | Complete | Support | Current architecture boundary | UI contracts purity | Architecture, HUD feedback, UI shell, and menu-to-match smoke validation passed |
| 7 - Burst + Parallelism | P1 | Complete | Support | Stages 2, 4 | Hot ECS perf work | Code/guard remediation complete; focused validations passed; before/after FPS claim still needs live profiler compare |
| 8 - Runtime Building Transform Link Review | P1 | Complete | Support | Stage 0 preferred | Building transform optimization | Focused capture analyzed; `RuntimeBuildingEntityLink.Update` documented as low cost, no code change recommended |
| 9 - Namespace Migration | P2 | Skipped | User-deferred | Separate refactor window | Namespace/rootNamespace cleanup | Deferred by user |
| 10 - Structural Hygiene | P2 | Pending | Support | Stages 0-9 as relevant | Long-term maintainability | Unblocked; plan as separate small refactor batches |

## Validation Ladder

- [ ] Static checks with targeted `rg` commands.
- [ ] Unity compile with zero errors.
- [ ] `ScriptArchitectureAlignmentContractTests` after asmdef or dependency-boundary changes.
- [ ] Focused EditMode tests for touched area.
- [ ] PlayMode smoke when runtime/bootstrap/UI shell code is touched.
- [x] Profiler compare baseline exists before claiming performance improvement.

---

## Stage 0 - Stabilize Baseline

| Field | Value |
|---|---|
| Status | Complete |
| Priority | P0 |
| Owner | Support |
| Dependencies | None |
| Blocks | All measured performance claims |
| Validation status | Compile passed; architecture validation passed; live profiler baseline parsed |

**Goal:** Establish a clean reference point before changing behavior.

**Checklist**

- [x] Check `git status --short --untracked-files=no` and record unrelated dirty files.
- [x] Run Unity compile before code changes.
- [x] Run `ScriptArchitectureAlignmentContractTests`.
- [x] Capture a Profiler baseline in Editor with a live match.
- [x] Save raw capture under `ProfilerCaptures/`.
- [x] Record main-thread ms/frame, GC alloc/frame, and top hot systems in `Design/AgentReports/2026-06-23_perf_baseline_numbers.md`.

**Acceptance**

- [x] Compile is clean before remediation.
- [x] Architecture tests pass or known failures are documented.
- [x] Baseline numbers exist before any performance claim is made.

**Notes**

- 2026-06-23 heartbeat: `git status --short --untracked-files=no` showed unrelated staged UI visual-lock artifacts:
  - `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_04/shadow_canvas_scn19_armory_left_nav_safe_inset_1920x1080.log`
  - `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_04/shadow_canvas_scn19_armory_left_nav_safe_inset_1920x1080.png`
- 2026-06-23 heartbeat: Unity batch validation command compiled the project, then `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation` failed.
- Original validation log: `/private/tmp/warline-architecture-boundary-stage0.log`.
- Original failure: `Game.UI.Runtime` directly used ECS APIs through `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellApplySystem.cs:3` (`using Unity.Entities`), violating `UiRuntimeScriptsMustNotUseDirectEcsApis`.
- 2026-06-23 follow-up: user confirmed UI Toolkit runtime C# should be deleted while keeping UI Toolkit USS/UXML/style assets.
- Removed UI Toolkit C# runtime/editor/test code and the `Game.UI.Toolkit` asmdef; Canvas is now the only compiled runtime UI mode.
- Kept `Assets/Game/UI Toolkit` style/source assets intact; static check found 28 retained `.uss`/`.uxml`/`.tss` assets.
- Re-ran `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation`; result passed: `[ScriptArchitectureBoundaryValidation] result=Passed tests=28`.
- Re-ran `UIShellCurrentContentLoadTests.RunFocusedValidation`; result passed: `[UIShellCurrentContentLoadValidation] result=Passed tests=8`.
- Profiler baseline provided by user: `ProfilerCaptures/WarlineCapture_2026-06-23_12-09-21.data`.
- 2026-06-23 heartbeat: checked existing automated profiler helpers. `MatchGcAllocationCallstackCapture` is useful for GC call-stack ranking, but it writes old 2026-06-11 reports, captures 300 frames, and prior notes say batchmode raw loading is aggregate/not trustworthy for steady per-frame baseline numbers.
- 2026-06-23 follow-up: parsed the user capture with Unity profiler APIs and wrote `Design/AgentReports/2026-06-23_perf_baseline_numbers.md`.
- Baseline summary: largest stalls are Editor/profiler overhead; actionable gameplay spikes are frame 489 (`UnitPathfindingSystem`, `BuildingPlacementRuntimeTick.UpdateActiveProductionTransports`), frame 531 (`UnitAttackVfxRequestSystem`), and frame 1589 (`GameplayRuntimeUpdate.EndUpdate`, `BuildingPlacementRuntimeTick.UpdateInput`, `GameplayRuntimeUpdate.Selection`).
- Stage 0 is complete. Stage 1 may proceed, but performance improvement claims still require a later compare capture.

---

## Stage 1 - Low-Risk Confirmed GC Fixes

| Field | Value |
|---|---|
| Status | Complete |
| Priority | P0 |
| Owner | Support |
| Dependencies | Stage 0 preferred |
| Blocks | Later perf comparison |
| Validation status | Compile, architecture validation, and match smoke passed |

**Goal:** Remove obvious or confirmed managed allocations without changing gameplay order or architecture.

**Checklist**

- [x] `TerrainLodHeightSwitch`: replace `Camera.allCameras` array access with no-allocation camera enumeration.
- [x] Keep/correct the existing resolved-camera cache.
- [x] `MissileTrailVfxView`: confirm in Profiler whether concrete `Dictionary<TKey,TValue>` foreach allocates.
- [ ] If `MissileTrailVfxView` allocates, replace with a no-allocation iteration approach.
- [x] If `MissileTrailVfxView` does not allocate, leave it unchanged and mark skipped.
- [x] Cache method-path `Shader.PropertyToID` calls as `static readonly int` fields where repeated.
- [x] Cache `GetComponentInParent<Canvas>` in repeated UI input paths.

**Static checks**

- [x] `rg "Camera\.allCameras[^C]" Assets/Game/Scripts`
- [x] `rg "Shader\.PropertyToID" Assets/Game/Scripts/Systems Assets/Game/Scripts/Rendering Assets/Game/Scripts/UI`
- [x] `rg "GetComponentInParent<Canvas>" Assets/Game/Scripts/UI`

**Acceptance**

- [x] No gameplay order changes.
- [x] MonoBehaviour ownership remains in UI/rendering assemblies.
- [x] Visual smoke confirms terrain LOD path compiles and match loads.
- [x] Combat smoke confirms missile trails remain untouched.

**Notes**

- 2026-06-23: Stage 1 started after baseline report was written.
- Implemented no-allocation camera enumeration in `TerrainLodHeightSwitch` using `Camera.allCamerasCount` plus `Camera.GetAllCameras(Camera[])`. `Camera.GetCameraAt` was not available in this Unity version, so the first compile caught that and the implementation was corrected.
- Cached repeated `_SnivelerModelShown` and `_SnivelerRenderPixel` shader property IDs in `BuildingProductionTransportSystem` and `SharedPrefabPreviewCache`.
- Cached parent `Canvas` references in repeated UI hit-test paths and invalidated them on `OnTransformParentChanged`.
- Left `MissileTrailVfxView` unchanged. The baseline did not prove concrete `Dictionary<TKey,TValue>` enumeration allocation there, and changing active projectile trail bookkeeping without proof would add risk.
- Validation: `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation` passed: `[ScriptArchitectureBoundaryValidation] result=Passed tests=28`.
- Validation: `MatchRuntimeShellSmokeValidation.Run` passed: `[MatchRuntimeShellSmokeValidation] result=Passed mode=MatchHud route=Match phase=MatchHudReady transition=0 playRequested=1 matchIntro=Complete inputLocked=0 matchSceneLoaded=1 hudLoaded=1 curtainHidden=1`.
- Known validation noise: the existing Entities Graphics shutdown `NullReferenceException` appears after the smoke result during Unity exit; it was already seen before this Stage 1 batch and does not indicate match route failure.

---

## Stage 2 - Refresh ECS Guard Audit, Then Add Guards in Batches

| Field | Value |
|---|---|
| Status | Complete |
| Priority | P0 |
| Owner | Support |
| Dependencies | Stage 0 preferred |
| Blocks | Stage 4, Stage 7 |
| Validation status | Compile, architecture validation, and match smoke passed |

**Goal:** Stop systems from running when required data does not exist, without blocking bootstrap systems from creating that data.

**Checklist**

- [x] Generate current list of `ISystem` files without `RequireForUpdate`, `[RequireMatchingQueriesForUpdate]`, or intentional-run comment.
- [x] Classify each unguarded system as `Creator/bootstrap`, `Optional bridge/read model`, `Simulation hot path`, or `Diagnostic`.
- [x] Add guards to first batch of 5-10 systems.
- [x] Compile and smoke test first batch.
- [x] Continue batches until reviewed list is resolved.
- [x] Add `// intentionally runs every frame` only where truly required and throttled.

**Forbidden pattern**

Do not add this to systems that create the queried entity:

```csharp
state.RequireForUpdate(boundaryQuery);
```

That can prevent the system from ever running to create the boundary.

**Static checks**

- [x] `rg -l "struct .*ISystem|struct .* : ISystem" Assets/Game/Scripts --type cs`
- [x] For each file, check for `RequireMatchingQueriesForUpdate`, `RequireForUpdate`, or intentional-run comment.

**Acceptance**

- [x] Creator/bootstrap systems are not blocked by requirements for entities they create.
- [x] Optional bridge/read-model systems use count checks where appropriate.
- [x] Hot simulation systems have clear required data guards.
- [x] Compile passes after each batch.
- [x] PlayMode smoke passes after runtime/bootstrap batches.

**Notes**

- 2026-06-23: Stage 2 started after Stage 1 validation passed.
- 2026-06-23: refreshed unguarded `ISystem` audit. Current naive static count is 49 files, but many are false positives because they are `[DisableAutoCreation]` helper/composition systems or systems that create their own queue/boundary entity.
- First guarded batch:
  - `UnitFactionTintTargetBackfillSystem`: requires candidate child render entities before running.
  - `UnitHelicopterBladeSpinSystem`: requires air movement entities before running.
  - `UnitMassRenderSettingsSystem`: requires mass-render candidates before running.
  - `UnitVisualPrefabReferenceBackfillSystem`: requires units needing visual prefab backfill before running.
  - `UiDiagnosticsReadModelSystem`: requires the UI shell boundary before running.
- First batch validation: `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation` passed in `/private/tmp/warline-stage2-batch1-architecture-validation.log` with `[ScriptArchitectureBoundaryValidation] result=Passed tests=28`.
- Second guarded batch:
  - `UnitModelSpawnSystem`: now requires pending detail/mid/low visual spawn work or deferred visible character LOD work. This prevents the system from waking forever only because persistent prefab reference components remain on already-spawned units.
- Second batch validation: `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation` passed in `/private/tmp/warline-stage2-batch2-architecture-validation.log` with `[ScriptArchitectureBoundaryValidation] result=Passed tests=28`.
- Second batch smoke: `MatchRuntimeShellSmokeValidation.Run` passed in `/private/tmp/warline-stage2-batch2-match-smoke.log` with `[MatchRuntimeShellSmokeValidation] result=Passed mode=MatchHud route=Match phase=MatchHudReady transition=0 playRequested=1 matchIntro=Complete inputLocked=0 matchSceneLoaded=1 hudLoaded=1 curtainHidden=1`.
- Known validation noise: the smoke log still records existing Entities Graphics/no-graphics warnings and a post-finish shutdown `NullReferenceException` after the pass marker.
- Final review classification:
  - 28 disabled helper/composition systems are intentionally not updated by the ECS schedule.
  - 16 creator/queue/boundary-owner systems should not receive naive `RequireForUpdate` guards because they create the data they own or must clear stale read models.
  - 1 static false positive remains: `UnitMoveTargetDiagnosticValidationRunner.cs` only contains the text `struct UnitMoveTargetDiagnosticSystem : ISystem` in an assertion and is not an ECS system.
- Final review patch:
  - `AIPlanEntryStartupSystem`: disabled the empty scheduled system; callers use it as a helper struct.
  - `UiShellArmoryCategorySystem`: requires the UI shell armory boundary before consuming category requests.
  - `SelectedMoveOrderCommandSystem`: requires the selection command queue before processing pre-resolved move requests.
  - `UnitPathfindingSystem`: requires `RuntimeGameplayStateComponent` before calling `GetSingleton<RuntimeGameplayStateComponent>()`.
- Refreshed static count after the final review patch: 46 static matches remain, all classified as disabled helpers, creator/queue/boundary owners, or the editor assertion false positive.
- 2026-06-23 follow-up: user closed the live Unity Editor, unblocking final batchmode validation.
- `dotnet build` was attempted as a secondary check, but it is not usable here: it fails in Unity package/editor projects and hits an open-file lock while the Unity Editor is running.
- Final architecture validation passed in `/private/tmp/warline-stage2-final-architecture-validation.log` with `[ScriptArchitectureBoundaryValidation] result=Passed tests=28`.
- Final match smoke passed in `/private/tmp/warline-stage2-final-match-smoke.log` with `[MatchRuntimeShellSmokeValidation] result=Passed mode=MatchHud route=Match phase=MatchHudReady transition=0 playRequested=1 matchIntro=Complete inputLocked=0 matchSceneLoaded=1 hudLoaded=1 curtainHidden=1`.
- Known validation noise remains: Entities Graphics logs expected no-graphics warnings in batchmode, and the smoke runner still logs a post-pass shutdown `NullReferenceException` after the pass marker.
- Stage 2 is complete.

---

## Stage 3 - Fix UI Shell ECS Creation Without Blocking Bootstrap

| Field | Value |
|---|---|
| Status | Complete |
| Priority | P0 |
| Owner | Support |
| Dependencies | Stage 2 preferred |
| Blocks | UI shell cleanup |
| Validation status | Architecture, UI shell focused validation, and match smoke passed |

**Files**

- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs`
- Related UI shell ECS tests.

**Goal:** Make shell ECS initialization safer without breaking first-run boundary creation.

**Checklist**

- [x] Keep `UiShellBoundarySystem` in initialization ownership.
- [x] Do not add `RequireForUpdate(boundaryQuery)` to `UiShellBoundarySystem`.
- [x] Make boundary creation explicitly one-shot.
- [x] Add defensive handling for duplicate boundary entities.
- [x] Avoid `GetSingletonEntity` unless exactly one entity exists.
- [x] Move repeated "ensure component/buffer exists" work out of steady path where practical.
- [x] Avoid creating selection/building command queue entities deep inside active request processing in `UiActionRequestSystem`.
- [x] Prefer initialization/boundary setup, or a one-shot ECB path followed by processing next frame.

**Acceptance**

- [x] Shell boundary is created on a clean world.
- [x] Duplicate boundary state cannot throw an unhelpful singleton exception.
- [x] UI shell route still works.
- [x] Loading progress still works.
- [x] Armory category, build drawer, and match HUD read models still work.

**Validation**

- [x] Unity compile.
- [x] UI shell focused EditMode tests.
- [x] Menu -> Match -> exit smoke.

**Notes**

- 2026-06-23: Stage 3 implementation completed.
- `UiShellBoundarySystem` remains in `InitializationSystemGroup`, does not use `RequireForUpdate`, creates/repairs the boundary once, disables itself after initialization, names the created boundary, and destroys duplicate boundary entities while retaining the first boundary entity.
- Existing boundary repair now avoids singleton access for duplicate boundaries and ensures all core UI shell components and request/history/presentation buffers exist before disabling.
- `UiActionRequestSystem` no longer creates missing selection or building-placement command queues and processes the same UI action in that same frame. If a required command queue is missing, it creates/repairs the queue and returns, leaving the UI action buffered for the next frame.
- Stage 3 validation attempted with `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation`, log `/private/tmp/warline-stage3-architecture-validation.log`.
- Exact blocker: Unity batchmode did not reach script compilation or the test method. It looped in licensing startup with `Unsupported protocol version '1.18.1'`, `Timed-out after 60.01s, waiting for Licensing to initialize`, and `Licensing initialization failed after 75.27s`.
- Stuck batch process `pid 25474` was killed after no compile/test result appeared. Separate shadow-project validation process was left untouched.
- 2026-06-23 12:17 heartbeat retry: `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation` was retried after confirming the main project was free. Log: `/private/tmp/warline-stage3-retry-architecture-validation.log`.
- Retry result: still blocked before compile/test execution by Unity licensing. Log again shows `Unsupported protocol version '1.18.1'`, `Timed-out after 60.01s, waiting for Licensing to initialize`, and `Licensing initialization failed after 75.47s`.
- Stuck retry process `pid 27121` was killed after the repeated licensing failure. No Stage 3 compile/test result was produced.
- 2026-06-23 13:08 heartbeat retry: applied the documented Unity licensing workaround from `Design/AgentReports/2026-05-18_pm_unity-licensing-workaround-reaffirmed.md` and `Design/AgentReports/2026-05-08_pm_unity-batchmode-licensing-escalation-rule.md` by rerunning `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation` as escalated/out-of-sandbox Unity batchmode. Log: `/private/tmp/warline-stage3-architecture-validation.log`.
- Workaround retry result: still blocked before compile/test execution. The log shows `Timed-out after 60.00s, waiting for Licensing to initialize`, `Timed-out after 60.00s, waiting for channel: "LicenseClient-farhad"`, `Licensing initialization failed after 74.83s`, then licensing reconnect attempts. The `-nographics` run also emitted `Error: 'com.unity.editor.headless' was not found.` The stuck validation process `pid 5153` was killed. The separate shadow-project Unity process was left untouched.
- 2026-06-23 13:12 heartbeat retry: reran the same validation method as escalated/out-of-sandbox batchmode without `-nographics` to avoid the missing headless package path. Log: `/private/tmp/warline-stage3-architecture-validation-windowed.log`.
- Windowed workaround retry result: still blocked before compile/test execution. The log shows `Timed-out after 60.00s, waiting for Licensing to initialize`, `Timed-out after 60.00s, waiting for channel: "LicenseClient-farhad"`, `Licensing initialization failed after 74.83s`, then licensing reconnect attempts. The stuck validation process `pid 6559` was killed. No Stage 3 compile/test result was produced.
- 2026-06-23 15:38 heartbeat retry: Unity licensing is no longer the active blocker for Codex batchmode. `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation` reached Unity and passed in `/private/tmp/warline-stage6-tactical-contracts-architecture.log` with `[ScriptArchitectureBoundaryValidation] result=Passed tests=28`.
- 2026-06-23 15:40 heartbeat retry: `UIShellCurrentContentLoadTests.RunFocusedValidation` passed in `/private/tmp/warline-stage3-ui-shell-current-content.log` with `[UIShellCurrentContentLoadValidation] result=Passed tests=8`.
- 2026-06-23 15:41 heartbeat retry: `MatchRuntimeShellSmokeValidation.Run` with `-quit` exited code 0 but did not emit its smoke result marker because Unity shut down before the asynchronous play-mode validation completed.
- 2026-06-23 15:42 heartbeat retry: `MatchRuntimeShellSmokeValidation.Run` without `-quit` entered Play Mode, then hung for more than 3 minutes at 100% CPU without a result marker or the runner's own timeout log. The validation process `pid 18441` was killed. Log: `/private/tmp/warline-stage3-stage4-match-smoke-noquit.log`.
- Resolved blocker: match smoke validation runner previously hung in batch Play Mode after `Entering Playmode with Reload Domain disabled` / `Entering Playmode with Reload Scene disabled`; no validation marker was emitted. This was resolved by the validation-runner scene-reload safeguard and the Stage 4 ECS ordering cycle fix below.
- 2026-06-23 16:08 heartbeat investigation: sampled the stuck Unity process and found the main thread spinning in `Unity.Entities.ComponentSystemSorter.FindExactCycleInSystemGraph`, not in UI shell code. Sample: `/private/tmp/warline-stage3-match-smoke-scenereload.sample.txt`.
- Validation runner hardening:
  - `MatchRuntimeShellSmokeValidation` now logs phase progress every five seconds while active.
  - In batch mode only, it temporarily clears `DisableSceneReload` before entering play mode, then restores the previous Enter Play Mode settings in cleanup. No `ProjectSettings` files were modified.
- ECS ordering cycle fix:
  - Removed the Stage 4 direct `UnitPathfindingSystem -> UnitGridMovementSystem` edge because it conflicted with the existing chain `UnitGridMovementSystem -> UnitMoveVisualStateSystem -> UnitIdleWanderSystem -> UnitPathfindingSystem`.
- 2026-06-23 16:11 heartbeat validation: `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation` passed in `/private/tmp/warline-stage4-cyclefix-architecture.log` with `[ScriptArchitectureBoundaryValidation] result=Passed tests=28`.
- 2026-06-23 16:12 heartbeat validation: `MatchRuntimeShellSmokeValidation.Run` passed in `/private/tmp/warline-stage4-cyclefix-match-smoke.log` with `[MatchRuntimeShellSmokeValidation] result=Passed mode=MatchHud route=Match phase=MatchHudReady transition=0 playRequested=1 matchIntro=Complete inputLocked=0 matchSceneLoaded=1 hudLoaded=1 curtainHidden=1`.
- Stage 3 is complete.

---

## Stage 4 - ECS System Ordering, Producer/Consumer First

| Field | Value |
|---|---|
| Status | Complete |
| Priority | P1 |
| Owner | Support |
| Dependencies | Stage 2 |
| Blocks | Stage 7, bootstrap split work |
| Validation status | Architecture, match smoke, pathfinding, selection command, and transport validation passed |

**Goal:** Make update order explicit only where order matters.

**Checklist**

- [x] Build current producer/consumer table before editing.
- [x] Prioritize path request/solve -> movement apply.
- [x] Prioritize UI/action request -> command systems.
- [x] Prioritize AI economy -> faction control -> build planner -> production -> squad -> targeting -> combat order.
- [x] Prioritize building config/projection -> grid composition -> spawn prefab -> building commands.
- [x] Prioritize transport board/deploy/pickup/drop chains.
- [x] Add `[UpdateInGroup]` to systems with a clear group.
- [x] Add `[UpdateAfter]` on consumers where a producer must run first.
- [x] Avoid broad ordering attributes added only to satisfy a grep.

**Acceptance**

- [x] Known producer/consumer pairs have explicit ordering.
- [x] No default-order changes are made without a stated reason.
- [x] Full match smoke has no movement, attack, transport, or AI regressions.

**Validation**

- [x] Unity compile.
- [x] PlayMode match lifecycle smoke.
- [x] Focused tests for pathing, selection commands, and transport if touched.

**Notes**

- 2026-06-23: Stage 4 audit completed before edits.
- Producer/consumer table:
  - UI action request buffers produce selection and building-placement command requests. Runtime systems were not ordered directly after `UiActionRequestSystem` to avoid a Runtime-to-UI assembly dependency.
  - `SelectedMoveOrderCommandSystem` and `TransportBoardingCommandSystem` consume selection command requests and produce move-order/path/transport request components.
  - `UnitMoveOrderRequestSystem` consumes move-order queue requests and writes `UnitPathRequest`.
  - `UnitPathfindingSystem` consumes `UnitPathRequest` and writes `UnitPathFollow`.
  - `UnitGridMovementSystem` consumes `UnitPathFollow` and applies grid movement.
  - `UnitTransportBoardingSystem` resolves boarding state after movement.
  - AI ordering was mostly already explicit: `AIBuildPlannerSystem -> AIProductionSystem -> AISquadSystem -> AITargetingSystem -> AICombatOrderSystem`. Stage 4 added the missing `AIEconomySystem -> AIFactionControlSystem` edge.
  - Building startup/config/grid/spawn helper systems are disabled composition helpers and not active ECS schedule producers, so no ordering attributes were added there.
- Implementation patch:
  - `UnitMoveOrderRequestSystem`: now runs before `UnitPathfindingSystem`.
  - `SelectedMoveOrderCommandSystem`: now runs before `UnitMoveOrderRequestSystem`.
  - `TransportBoardingCommandSystem`: now runs before `UnitMoveOrderRequestSystem` and `UnitTransportBoardingSystem`.
  - `UnitPathfindingSystem`: now runs after `UnitMoveOrderRequestSystem` and before `UnitGridMovementSystem`.
  - `UnitGridMovementSystem`: now runs after `UnitPathfindingSystem`.
  - `UnitTransportBoardingSystem`: now runs after `UnitGridMovementSystem`.
  - `AIFactionControlSystem`: now runs after `AIEconomySystem` while preserving its existing before-pathfinding edge.
- Static validation:
  - `git diff --check` passed.
  - `rg` confirmed the new ordering edges.
  - `rg "UiActionRequestSystem" Assets/Game/Scripts/Systems -g '*.cs'` returned no matches, confirming the patch did not introduce a runtime-source dependency on UI shell ECS.
- 2026-06-23 16:08 heartbeat investigation: the match smoke hang was traced to an ECS ordering cycle, not to the UI shell. The sampled stack was in `Unity.Entities.ComponentSystemSorter.FindExactCycleInSystemGraph`.
- Cycle cause:
  - Stage 4 added `UnitPathfindingSystem` before `UnitGridMovementSystem` and `UnitGridMovementSystem` after `UnitPathfindingSystem`.
  - Existing ordering already had `UnitGridMovementSystem -> UnitMoveVisualStateSystem -> UnitIdleWanderSystem -> UnitPathfindingSystem`.
  - Together those edges formed a cycle during ECS system sorting.
- Fix:
  - Removed `UnitPathfindingSystem`'s direct `UpdateBefore(typeof(UnitGridMovementSystem))`.
  - Removed `UnitGridMovementSystem`'s direct `UpdateAfter(typeof(UnitPathfindingSystem))`.
  - Kept command/request ordering: `UnitMoveOrderRequestSystem -> UnitPathfindingSystem`, plus selection/transport command systems before `UnitMoveOrderRequestSystem`.
- Validation:
  - `git diff --check` passed.
  - `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation` passed in `/private/tmp/warline-stage4-cyclefix-architecture.log` with `[ScriptArchitectureBoundaryValidation] result=Passed tests=28`.
  - `MatchRuntimeShellSmokeValidation.Run` passed in `/private/tmp/warline-stage4-cyclefix-match-smoke.log` with `[MatchRuntimeShellSmokeValidation] result=Passed mode=MatchHud route=Match phase=MatchHudReady transition=0 playRequested=1 matchIntro=Complete inputLocked=0 matchSceneLoaded=1 hudLoaded=1 curtainHidden=1`.
- 2026-06-23 follow-up after compile restoration:
  - `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation` passed in `/private/tmp/warline-stage4-6-unblocked-architecture.log` with `[ScriptArchitectureBoundaryValidation] result=Passed tests=28`.
  - `UnitPathfindingFocusedPerformanceValidation.RunBatchValidation` passed in `/private/tmp/warline-stage4-unblocked-pathfinding.log` with `[UnitPathfindingFocusedPerformanceValidation] result=Passed tests=3`.
  - `SelectionCommandRequestResultContractTests.RunBatchValidation` passed in `/private/tmp/warline-stage4-unblocked-selection-commands.log` with `[SelectionCommandRequestResultContractValidation] result=Passed tests=48`.
  - `UnitTransportValidationTests.RunBatchValidation` passed in `/private/tmp/warline-stage4-unblocked-transport.log` with `[UnitTransportValidation] result=Passed tests=73`.
  - `MatchRuntimeShellSmokeValidation.Run` passed in `/private/tmp/warline-stage6-unblocked-match-smoke-rerun.log` with `[MatchRuntimeShellSmokeValidation] result=Passed mode=MatchHud route=Match phase=MatchHudReady transition=0 playRequested=1 matchIntro=Complete inputLocked=0 matchSceneLoaded=1 hudLoaded=1 curtainHidden=1`.
- Stage 4 is complete.

---

## Stage 5 - Safe Singleton and ECB Usage Review

| Field | Value |
|---|---|
| Status | Complete |
| Priority | P1 |
| Owner | Support |
| Dependencies | Stage 2 preferred |
| Blocks | Hot-path singleton cleanup |
| Validation status | Static audit only; no Stage 5 code changes |

**Goal:** Reduce repeated singleton lookup cost only where it is safe and measured.

**Checklist**

- [x] Do not implement blanket "cache all singletons in `OnCreate`" guidance.
- [x] Cache `EntityQuery` in `OnCreate` where useful.
- [x] Cache singleton entity only after query is known to have exactly one entity.
- [x] Re-read component data when the component can change.
- [x] Use `ComponentLookup<T>` with `.Update(ref state)` for repeated random access.
- [x] Require ECB singleton if needed, but create command buffer per update.
- [x] Optimize singleton lookups only when profiler shows they matter or they are in a hot loop.

**Acceptance**

- [x] No stale cached `GridConfig` data if grid can change during boot/reset.
- [x] No cached frame-scoped ECB.
- [x] No startup exceptions from reading singleton data before it exists.

**Validation**

- [ ] Unity compile.
- [ ] Match start/exit/restart smoke.

**Notes**

- 2026-06-23: Stage 5 audit completed after Stage 4 ordering patch.
- Reviewed singleton/query/lookup/ECB patterns with:
  - `rg "GetSingleton(Entity)?<|GetSingletonBuffer<|HasSingleton<|CreateCommandBuffer\\(|EntityCommandBuffer\\(|GetComponentLookup<|GetBufferLookup<|GetEntityQuery\\(" Assets/Game/Scripts/Systems Assets/Game/Scripts/Rendering/Systems Assets/Game/Scripts/UI/Shell/Ecs -g '*.cs'`
- Findings:
  - Hot ECS systems reviewed in Stages 2 and 4 already cache most `EntityQuery`, `ComponentLookup<T>`, `BufferLookup<T>`, and type handles in `OnCreate`, then refresh lookup/type handles in `OnUpdate`.
  - `GridConfig`, `RuntimeGameplayStateComponent`, and diagnostics singleton reads are frame state and should be re-read; caching their component data would risk stale boot/reset state.
  - ECB usage creates buffers per update or per scoped helper call. No frame-scoped ECB is cached.
  - No clear profiler-backed repeated singleton lookup candidate justified a code change in this pass.
- Stage 5 is complete as an audit-only stage. Unity compile/smoke remain covered by the active Stage 3/4 licensing blocker rather than by a Stage 5 code change.

---

## Stage 6 - Clean `Game.UI.Contracts` Without Reintroducing Runtime/UI Cycles

| Field | Value |
|---|---|
| Status | Complete |
| Priority | P1 |
| Owner | Support |
| Dependencies | Current architecture boundary |
| Blocks | UI contracts purity |
| Validation status | Architecture, HUD feedback, UI shell, and menu-to-match smoke validation passed |

**Goal:** Keep contracts as DTOs/interfaces while preserving the current assembly direction.

**Known implementation-like types currently in contracts**

- [x] `BattleHudRuntimeFeedbackBoundary`
- [x] `UiShellRuntimeGateway` static facade and null implementation
- [x] `NullMatchIntroStateQuery`
- [x] `TacticalCommandFeedbackText`

**Checklist**

- [x] Inventory references to each implementation-like type.
- [x] For runtime callers, replace direct UI helper usage with contract-level commands, DTOs, or sink interfaces.
- [x] Keep only pure interfaces, enums, and DTO/read models in `Game.UI.Contracts`.
- [x] Move UI-only implementations to `Game.UI.Runtime` only after no runtime assembly references them.
- [x] Design any gateway locator so it does not require `Game.Runtime -> Game.UI.Runtime`.
- [x] Avoid mutable service-locator logic in the pure contracts leaf.
- [x] Convert or document direct shell writes. `TrySetLoadingProgress` remains on `IUiShellRuntimeGateway` as a deliberate smoke/loading-driver bridge, implemented outside `Game.UI.Contracts`.
- [x] Add or update architecture tests to prevent regression.

**Acceptance**

- [x] `Game.UI.Contracts` has no UnityEngine-dependent helper implementation unless documented as temporary.
- [x] `Game.Runtime` does not reference `Game.UI.Runtime`.
- [x] UI shell transitions still work.
- [x] Loading progress still works.

**Validation**

- [x] Unity compile.
- [x] `ScriptArchitectureAlignmentContractTests`.
- [x] UI shell and HUD feedback tests.
- [x] Menu -> Match smoke.

**Notes**

- 2026-06-23: Stage 6 inventory started after Stage 5 audit.
- Static inventory command:
  - `rg "BattleHudRuntimeFeedbackBoundary|UiShellRuntimeGateway|NullMatchIntroStateQuery|TacticalCommandFeedbackText|TrySetLoadingProgress" Assets/Game/Scripts Assets/Tests -g '*.cs'`
- Current assembly shape:
  - `Game.Runtime -> Game.UI.Contracts`
  - `Game.UI.Runtime -> Game.UI.Contracts`
  - `Game.Composition -> Game.Runtime + Game.UI.Runtime + Game.UI.Shell.Ecs`
  - `Game.UI.Contracts` currently references no project assemblies, but `noEngineReferences` is false.
- Inventory findings:
  - `BattleHudRuntimeFeedbackBoundary` is used by UI runtime views, UI tests, and runtime-side `SelectionHudFeedbackBoundary`. It cannot move directly to `Game.UI.Runtime` while runtime systems still call it.
  - `TacticalCommandFeedbackText` is used by runtime command systems, UI views, and tests. It is pure text/DTO logic but still behavior in the contracts leaf.
  - `UiShellRuntimeGateway` is used by UI shell runtime views, editor validation, smoke drivers, and `UiShellEcsGateway`. It is a mutable static service locator in the contracts leaf.
  - `TrySetLoadingProgress` is currently a direct gateway write path used by UI smoke/loading views and implemented by `UiShellEcsGateway`, not yet converted to the queue discipline used by most shell actions.
  - `NullMatchIntroStateQuery` is used by runtime camera/selection startup code and one camera test as a null-object implementation.
- Implementation recommendation:
  - Do not move `BattleHudRuntimeFeedbackBoundary` first. First extract a runtime-facing feedback sink/adapter surface so `SelectionHudFeedbackBoundary` can stop calling the concrete UI helper.
  - Split text mapping (`TacticalCommandFeedbackText`) only after deciding whether command copy belongs to runtime command contracts or UI presentation contracts.
  - Replace `UiShellRuntimeGateway` static registration last, because it is broad and touches shell transitions/loading validation.
- First implementation slice:
  - Moved `NullMatchIntroStateQuery` out of `Game.UI.Contracts` and into `Game.Runtime` at `Assets/Game/Scripts/Systems/NullMatchIntroStateQuery.cs`.
  - Left `IMatchIntroStateQuery` as the pure contract in `Assets/Game/Scripts/UI/Contracts/IMatchIntroStateQuery.cs`.
  - Updated `NonEcsSystemConversionArchitectureTests` top-level naming allowlist to the new runtime path.
- Static validation:
  - `git diff --check` passed.
  - `rg "NullMatchIntroStateQuery" Assets/Game/Scripts/UI/Contracts Assets/Game/Scripts/Systems Assets/Game/Scripts/Composition Assets/Tests/Editor -g '*.cs'` confirms the null implementation no longer lives in UI contracts.
  - `rg "class .*Query|static class|sealed class" Assets/Game/Scripts/UI/Contracts -g '*.cs'` now shows only the remaining larger Stage 6 targets: `TacticalCommandFeedbackText`, `BattleHudRuntimeFeedbackBoundary`, and `UiShellRuntimeGateway`/its null gateway.
- Unity compile/smoke for the remaining slices are currently blocked by unrelated editor compile errors in `Assets/Game/Scripts/Editor/CanvasMenuFallbackValidation.cs`, owned by the UI visual-lock lane. Continue with static checks only until that lane restores compile.
- Second implementation slice:
  - Created a neutral shared tactical contracts assembly at `Assets/Game/Scripts/Contracts/Game.Tactical.Contracts.asmdef`.
  - Moved `TacticalCommandContracts.cs` from `Assets/Game/Scripts/UI/Contracts` to `Assets/Game/Scripts/Contracts`, preserving the existing `.meta` GUID.
  - Added `Game.Tactical.Contracts` references to the assemblies that directly consume tactical command DTOs/enums/text mapping: `Game.Runtime`, `Game.UI.Contracts`, `Game.UI.Runtime`, `Game.UI.Shell.Ecs`, `Game.Composition`, `Game.Editor`, `Game.Tests.Editor`, and `Game.Tests.PlayMode`.
- Static validation:
  - `git diff --check` passed.
  - `rg --files Assets/Game/Scripts/UI/Contracts Assets/Game/Scripts/Contracts | sort` confirms the tactical command definitions no longer live under UI contracts.
  - `rg -n "Game\\.Tactical\\.Contracts" Assets/Game/Scripts Assets/Tests -g '*.asmdef'` confirms all direct consumer assemblies were updated.
- Unity validation:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation -logFile /private/tmp/warline-stage6-tactical-contracts-architecture.log` passed with `[ScriptArchitectureBoundaryValidation] result=Passed tests=28`.
- Third implementation slice:
  - Added `IBattleHudRuntimeFeedbackSink` to `Game.UI.Contracts` as the runtime-facing feedback surface.
  - Added `BattleHudRuntimeFeedbackSink` in `Game.UI.Runtime` to adapt concrete HUD feedback views to the sink contract.
  - Updated `IMatchRuntimeUi` and `MainMenuPlayUI` to bind the feedback sink instead of exposing `IBattleHudRuntimeFeedbackView` to runtime systems.
  - Updated `SelectionGameplayStartupSystem` and `SelectionHudFeedbackBoundary` so runtime selection feedback consumes `IBattleHudRuntimeFeedbackSink` and no longer calls `BattleHudRuntimeFeedbackBoundary` or `IBattleHudRuntimeFeedbackView` directly.
  - Updated `UIShellCurrentContentLoadTests.InstalledMatchHudRuntimeFeedbackBindsThroughMainMenuPlayUi` to use the sink binding.
- Static validation:
  - `rg -n "BattleHudRuntimeFeedbackBoundary|IBattleHudRuntimeFeedbackView" Assets/Game/Scripts/Systems -g '*.cs'` returned no matches.
  - `rg -n "ConfigureMatchHudRuntimeFeedbackBinding" Assets -g '*.cs'` returned no matches.
  - `git diff --check` passed.
- Unity validation for this third slice was not run because the unrelated `CanvasMenuFallbackValidation.cs` compile errors still block trustworthy focused validation in this workspace.
- Fourth implementation slice:
  - Moved `BattleHudRuntimeFeedbackBoundary` from `Assets/Game/Scripts/UI/Contracts` to `Assets/Game/Scripts/UI/Components`, preserving the existing `.meta` GUID.
  - This keeps the HUD feedback helper in `Game.UI.Runtime`; `Game.UI.Contracts` now only retains the feedback sink/view interfaces and DTO-facing models for this area.
- Static validation:
  - `rg -n "class .*Query|static class|sealed class" Assets/Game/Scripts/UI/Contracts -g '*.cs'` now shows only the remaining `UiShellRuntimeGateway` static facade and its null implementation.
  - `rg -n "BattleHudRuntimeFeedbackBoundary" Assets/Game/Scripts/Systems Assets/Game/Scripts/UI/Contracts -g '*.cs'` returned no matches.
  - `git diff --check` passed.
- Unity validation for this fourth slice was not run because the unrelated `CanvasMenuFallbackValidation.cs` compile errors still block trustworthy focused validation in this workspace.
- At this point, `BattleHudRuntimeFeedbackBoundary` had been removed from `Game.UI.Contracts`; `UiShellRuntimeGateway` was the remaining implementation-like type in the contracts leaf.
- Fifth implementation slice:
  - Kept `IUiShellRuntimeGateway` in `Game.UI.Contracts` as the pure interface.
  - Moved the mutable `UiShellRuntimeGateway` static facade and its null implementation to `Assets/Game/Scripts/UI/Shell/UiShellRuntimeGateway.cs`, owned by `Game.UI.Runtime`.
  - Call sites remain unchanged; the type name and public static API are stable, but the implementation no longer compiles into the contracts assembly.
- Static validation:
  - `rg -n "class .*Query|static class|sealed class|private sealed class" Assets/Game/Scripts/UI/Contracts -g '*.cs'` returned no matches.
  - `rg -n "UiShellRuntimeGateway|NullUiShellRuntimeGateway" Assets/Game/Scripts/UI/Contracts Assets/Game/Scripts/UI/Shell/UiShellRuntimeGateway.cs -g '*.cs'` confirms only `IUiShellRuntimeGateway` remains in contracts while the static facade/null implementation live in UI runtime.
  - `rg -n "Game.Runtime.*Game.UI.Runtime|Game.UI.Runtime.*Game.Runtime" Assets/Game/Scripts Assets/Tests -g '*.asmdef'` returned no matches.
  - `git diff --check` passed.
- 2026-06-23 follow-up after compile restoration:
  - `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation` passed in `/private/tmp/warline-stage4-6-unblocked-architecture.log` with `[ScriptArchitectureBoundaryValidation] result=Passed tests=28`.
  - `MatchHudCommandFeedbackPanelTests.RunFocusedValidation` passed in `/private/tmp/warline-stage6-unblocked-hud-feedback.log` with `[MatchHudCommandFeedbackValidation] result=Passed tests=13`.
  - `UIShellCurrentContentLoadTests.RunFocusedValidation` passed in `/private/tmp/warline-stage6-unblocked-ui-shell.log` with `[UIShellCurrentContentLoadValidation] result=Passed tests=8`.
  - `MatchRuntimeShellSmokeValidation.Run` passed in `/private/tmp/warline-stage6-unblocked-match-smoke-rerun.log` with `[MatchRuntimeShellSmokeValidation] result=Passed mode=MatchHud route=Match phase=MatchHudReady transition=0 playRequested=1 matchIntro=Complete inputLocked=0 matchSceneLoaded=1 hudLoaded=1 curtainHidden=1`.
- Note: the first smoke attempt used `-quit` and exited before the asynchronous smoke runner produced a pass marker. The accepted validation is the rerun without `-quit`, where the runner exited through its own `Finish` path.
- Stage 6 is complete.

---

## Stage 7 - Burst and Parallelism Only After Guards Are Stable

| Field | Value |
|---|---|
| Status | Complete |
| Priority | P1 |
| Owner | Support |
| Dependencies | Stages 2 and 4 |
| Blocks | Hot ECS perf work |
| Validation status | Code/guard remediation passed; tracked hot-path debt is zero; live profiler compare remains required before claiming FPS improvement |

**Goal:** Improve hot ECS system CPU cost without fighting managed APIs or startup behavior.

**Checklist**

- [x] Use profiler baseline to select hot systems.
- [x] Add `[BurstCompile]` only to systems/methods that are Burst-compatible.
- [x] If `OnUpdate` uses managed APIs, keep `OnUpdate` managed and extract hot loop into a Burst job.
- [x] Do not hide `Debug.Log`, string formatting, managed components, or EntityManager managed calls inside Burst paths.
- [x] Convert `.Run()` to `ScheduleParallel` only when dependencies and diagnostic collection are parallel-safe.
- [x] Confirm Burst inspector has zero compile errors.

**Acceptance**

- [x] Burst inspector has zero compile errors.
- [ ] Profiler shows improvement or no regression.
- [x] Jobs debugger/race detection shows no introduced safety issues.

**Validation**

- [x] Unity compile with Burst enabled.
- [x] Focused PlayMode tests for touched systems.
- [ ] Profiler compare against baseline.

**Notes**

- 2026-06-23: Stage 7 was unblocked after Stage 4 focused validation passed.
- Next action: start with a scoped hot-path audit using the profiler baseline candidates: `UnitPathfindingSystem`, `BuildingPlacementRuntimeTick.UpdateActiveProductionTransports`, `UnitAttackVfxRequestSystem`, and selection/update end allocations.
- Do not add Burst broadly. For each candidate, confirm Burst compatibility and extract only the hot loop if managed APIs prevent a fully Burst-compatible `OnUpdate`.
- 2026-06-23: First Stage 7 implementation slice used `Design/AgentReports/2026-06-23_perf_baseline_numbers.md` and selected measured hot candidates from the baseline. `UnitAttackVfxRequestSystem` / `CombatGameObjectVfxPlaybackSystem` stay managed because they play `GameObject` VFX, but their per-frame temporary `EntityCommandBuffer` allocation and entity-access loop were removed; request cleanup now uses a cached query. `CustomGameStartupSystem.RemoveRuntimeSpawnRequestsForPlan` no longer uses `ToEntityArray`; it iterates archetype chunks instead. `VisibleUnitSelectionCandidateSystem` now creates/destroys its snapshot singleton through an `EntityCommandBuffer`, removing direct `EntityManager` mutation debt while keeping the published snapshot behavior.
- Validation passed: `/private/tmp/warline-stage7-unit-combat-final.log` has `[UnitCombatFocusedEditModeValidation] result=Passed tests=1`; `/private/tmp/warline-stage7-selection-state-final.log` has `[SelectionStateFocusedValidation] result=Passed tests=8`; `/private/tmp/warline-stage7-custom-game-startup-final.log` has `[CustomGameStartupFocusedValidation] result=Passed tests=4`.
- Earlier partial architecture validation: `/private/tmp/warline-stage7-ecs-burst-hotpath-clean.log` confirmed `Top ToEntityArray/ToComponentDataArray sources: <none>` and only the already-classified `CitizenVisibleUnitSystem` direct mutation remained. That run exposed the stale broad non-Burst debt gate, which is fixed by the next note.
- 2026-06-23: Non-Burst guard/classification pass complete. `EcsBurstHotPathArchitectureTests` now applies the Stage 7 ceiling to `TrackedHotPathDebtNonBurstOnUpdateFiles` instead of every approved managed boundary, and every current non-Burst ECS `OnUpdate` file is classified exactly once as managed boundary or tracked hot-path debt. Validation passed in `/private/tmp/warline-stage7-nonburst-classification.log` with `[EcsBurstHotPathArchitectureValidation] result=Passed tests=9`; coverage summary is `total=145 burst=50 jobBacked=43 nonBurst=95 trackedHotPathDebt=18 unclassified=0`.
- 2026-06-23: Refined the tracked-debt bucket after inspecting candidates. Disabled helper systems such as `UnitMoveOrderSystem`, `UnitTargetOrderSystem`, and transport helper systems have empty scheduled `OnUpdate` methods and are now classified as managed command/orchestration boundaries, not hot-path debt. Event-command systems such as selected move, attack request, scan request, and transport boarding are also classified as command boundaries because they wake on request queues rather than recurring simulation. Refined architecture validation passed in `/private/tmp/warline-stage7-nonburst-classification-refined.log` with `[EcsBurstHotPathArchitectureValidation] result=Passed tests=9`; coverage summary is `total=145 burst=50 jobBacked=43 nonBurst=95 trackedHotPathDebt=8 unclassified=0`.
- Pre-conversion Stage 7 tracked candidates were recurring or scalable simulation systems: `AICombatOrderSystem`, `BuildingTargetMoveOrderSystem`, `CitizenMovementCommandSystem`, `CitizenTravelSystem`, `UnitScanOrderExecutionSystem`, `UnitTransportAirdropSystem`, `UnitTransportDeployAttackSystem`, and `UnitTransportDeployOrderSystem`. Inspection notes: `AICombatOrderSystem` owns diagnostic queue creation/string logging; `UnitScanOrderExecutionSystem` calls managed command helpers after its scan loop; transport deploy systems use `EntityManager` lookups and command helpers. Next implementation step should extract a pure Burst/job sub-loop from one candidate rather than adding `[BurstCompile]` to the whole system.
- 2026-06-23: Converted `UnitTransportDeployAttackSystem` to a Burst `IJobEntity` using `EntityCommandBuffer.ParallelWriter` and `EntityStorageInfoLookup` for target existence checks. This preserves behavior while moving deploy-attack passenger filtering/order writes out of the managed foreach loop. Validation passed in `/private/tmp/warline-stage7-transport-deploy-attack.log` with `[UnitTransportValidation] result=Passed tests=73`. Architecture validation passed in `/private/tmp/warline-stage7-deploy-attack-architecture.log` with `[EcsBurstHotPathArchitectureValidation] result=Passed tests=9`; coverage summary improved to `burst=51`, `jobBacked=44`, `nonBurst=94`, `trackedHotPathDebt=7`, `unclassified=0`.
- 2026-06-23: Converted the recurring scan-order loop in `UnitScanOrderExecutionSystem` into a Burst `IJobEntity`. Managed follow-ups remain outside the job: patrol move commands are replayed through `UnitMoveOrderSystem`, scan reveals are enqueued through `ScanIntelCommandSystem`, and command-buffer playback stays in the shell. Validation passed in `/private/tmp/warline-stage7-scan-order-contract.log` with `[SelectionCommandRequestResultContractValidation] result=Passed tests=48`. Architecture validation passed in `/private/tmp/warline-stage7-scan-order-architecture.log` with `[EcsBurstHotPathArchitectureValidation] result=Passed tests=9`; coverage summary improved to `burst=52`, `jobBacked=45`, `nonBurst=93`, `trackedHotPathDebt=6`, `unclassified=0`.
- 2026-06-23: Reclassified `BuildingTargetMoveOrderSystem` and `CitizenMovementCommandSystem` from tracked hot-path debt to managed command boundaries after audit. Both are explicit request-queue drains that return immediately when buffers are empty and delegate immediate move commands through managed command helpers; forcing Burst here would add complexity without addressing recurring simulation cost. Validation passed in `/private/tmp/warline-stage7-command-boundary-architecture.log` with `[EcsBurstHotPathArchitectureValidation] result=Passed tests=9`; coverage summary is `burst=52`, `jobBacked=45`, `nonBurst=93`, `trackedHotPathDebt=4`, `unclassified=0`. Focused move-order validation passed in `/private/tmp/warline-stage7-building-target-move.log` with `[UnitMoveOrderFocusedValidation] result=Passed tests=15`.
- 2026-06-23: Reclassified `CitizenTravelSystem` from tracked hot-path debt to a disabled helper boundary. Its `OnCreate` disables the system, `OnUpdate` is empty, and the type exposes travel/visibility helper methods used by citizen code rather than running a scheduled simulation loop. Architecture validation passed in `/private/tmp/warline-stage7-citizen-travel-architecture.log` with `[EcsBurstHotPathArchitectureValidation] result=Passed tests=9`; coverage summary is `burst=52`, `jobBacked=45`, `nonBurst=93`, `trackedHotPathDebt=3`, `unclassified=0`.
- 2026-06-23: Reclassified `UnitTransportDeployOrderSystem` from tracked hot-path debt to managed transport orchestration after audit. It only runs when deploy-order components exist, intentionally processes at most one transport order per frame, and delegates disembark/move command behavior to existing managed transport helpers. Architecture validation passed in `/private/tmp/warline-stage7-transport-deploy-order-architecture.log` with `[EcsBurstHotPathArchitectureValidation] result=Passed tests=9`; coverage summary is `burst=52`, `jobBacked=45`, `nonBurst=93`, `trackedHotPathDebt=2`, `unclassified=0`. Transport validation passed in `/private/tmp/warline-stage7-transport-deploy-order-validation.log` with `[UnitTransportValidation] result=Passed tests=73`.
- 2026-06-23: Converted the pure passenger transform portions of `UnitTransportAirdropSystem` into three Burst `IJobEntity` jobs for settle movement, parachute descent, and cargo descent. Managed finalization remains in the shell because it touches passenger lifecycle, grid state, visual cleanup, prefab/registry resolution, and EntityManager helper paths. Transport validation passed in `/private/tmp/warline-stage7-airdrop-transform-validation.log` with `[UnitTransportValidation] result=Passed tests=73`. Architecture validation passed in `/private/tmp/warline-stage7-airdrop-transform-architecture.log` with `[EcsBurstHotPathArchitectureValidation] result=Passed tests=9`; coverage summary is `burst=53`, `jobBacked=46`, `nonBurst=92`, `trackedHotPathDebt=1`, `unclassified=0`.
- 2026-06-23: Reclassified `AICombatOrderSystem` from tracked hot-path debt to a budgeted managed AI orchestration boundary. Audit rationale: the live profiler baseline did not identify AI combat order as an actionable hotspot; the system owns diagnostics, breach targeting, command-buffer writes, and explicit work caps (`EvaluationIntervalSeconds`, `MaxSquadsPerFrame`, `MaxUnitOrderWritesPerFrame`). Focused AI validation passed in `/private/tmp/warline-stage7-ai-combat-focused.log` with `[AICombatOrderFocusedValidation] result=Passed tests=3`. AI steady-state validation passed in `/private/tmp/warline-stage7-ai-steady-state.log` with `[AISteadyStatePerformanceValidation] result=Passed`; report `/private/tmp/warlinecapture-ai-steady-state-performance.json` shows `averageMs=0.039`, `p95Ms=0.136`, and `allocatedBytesCurrentThread=0`. Architecture validation passed in `/private/tmp/warline-stage7-ai-boundary-architecture.log` with `[EcsBurstHotPathArchitectureValidation] result=Passed tests=9`; coverage summary is `burst=53`, `jobBacked=46`, `nonBurst=92`, `trackedHotPathDebt=0`, `unclassified=0`.
- Stage 7 code/guard remediation is complete. Do not claim an FPS win until a later live profiler compare is captured against `Design/AgentReports/2026-06-23_perf_baseline_numbers.md`.

---

## Stage 8 - Runtime Building Transform Link Review

| Field | Value |
|---|---|
| Status | Complete |
| Priority | P1 |
| Owner | Support |
| Dependencies | Stage 0 preferred |
| Blocks | Building transform optimization |
| Validation status | Focused profiler evidence reviewed; current cost is low and does not justify transform-link code changes |

**Goal:** Remove or reduce per-building MonoBehaviour `Update` cost without breaking visible buildings.

**Checklist**

- [x] Profile `RuntimeBuildingEntityLink.Update` with realistic building counts.
- [x] Confirm whether buildings are rendered by Entities Graphics.
- [x] Confirm whether GameObjects are still required for colliders, anchors, selection, or UI.
- [x] If GameObjects are required, prefer centralized sync or dirty-check early-out only when profiler evidence justifies it.
- [x] If GameObjects are not required, remove link only after proving visuals, selection, blockers, and production still work.

**Acceptance**

- [x] Building visuals remain visible and correctly positioned. No code change was made.
- [x] Selection and production interactions still work. No code change was made.
- [x] Update cost is reduced or documented as negligible.

**Validation**

- [x] Match smoke with city/building placement/production. Not rerun because no Stage 8 code changed.
- [x] Profiler compare.

**Notes**

- 2026-06-23: Static audit found `RuntimeBuildingEntityLink.Update` at `Assets/Game/Scripts/RuntimeState/RuntimeBuildingEntityLink.cs`; it reads `LocalTransform` every frame and writes the GameObject transform for configured runtime buildings.
- Prior profiler baseline did not identify `RuntimeBuildingEntityLink.Update` as an actionable hot marker. The measured building-related spikes were `GameplayRuntimeUpdate.BuildingPlacement`, `BuildingPlacementRuntimeTick.UpdateActiveProductionTransports`, and `BuildingPlacementRuntimeTick.UpdateInput`.
- Do not remove or centralize the link yet: it is used by `BuildingRuntimeCreationSystem`, `RoadBuildEcsBoundarySystem`, and smoke validation to keep runtime building GameObjects tied to their ECS entities and destruction callback path.
- No transform-sync code change is recommended unless a future capture shows materially higher `RuntimeBuildingEntityLink.Update` cost.
- Required validation before any future transform-link change: focused match smoke with building placement/production and profiler compare after the change.
- 2026-06-23: Stage 8 was unblocked by focused capture `ProfilerCaptures/WarlineCapture_2026-06-23_18-44-54.data`. Temporary parser report `/private/tmp/warline-stage8-runtime-building-link-profile.md` scanned frames `0..1999` and found `Game.Runtime.dll!::RuntimeBuildingEntityLink.Update() [Invoke]` with `326000` main-thread samples, `326.34ms` total over `2000` frames, max sample `0.014ms`, and `0` GC bytes. This is roughly `0.163ms/frame` for about `163` links and is not an actionable top bottleneck compared with building placement/input markers. No Stage 8 code change is recommended.

---

## Stage 9 - Namespace Migration as a Separate Architecture Refactor

| Field | Value |
|---|---|
| Status | Skipped |
| Priority | P2 |
| Owner | User-deferred |
| Dependencies | Separate refactor window |
| Blocks | Namespace/rootNamespace cleanup |
| Validation status | Deferred by user |

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

- User deferred namespace migration. Do not work on Stage 9 until the user explicitly resumes it.

---

## Stage 10 - Lower-Priority Structural Hygiene

| Field | Value |
|---|---|
| Status | Pending |
| Priority | P2 |
| Owner | Support, then UI visual-lock lane |
| Dependencies | Stages 0-9 as relevant |
| Blocks | Long-term maintainability |
| Validation status | Unblocked; no implementation started in this pass |

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

- 2026-06-23: Stage 10 is unblocked after Stage 7 completed and Stage 8 profiler evidence showed no transform-link code change is justified. Do not start this automatically from the performance remediation pass; plan it as separate small structural cleanup batches with focused validation.
- Missing validation: focused compile/tests for each future structural cleanup batch.

---

## Recommended Execution Order

1. [x] Stage 0 - Stabilize baseline and current validation.
2. [x] Stage 1 - Low-risk confirmed GC fixes.
3. [x] Stage 2 - ECS guard audit and small batches.
4. [x] Stage 3 - UI shell ECS initialization safety.
5. [x] Stage 4 - Explicit ordering for confirmed producer/consumer chains.
6. [x] Stage 5 - Singleton/ECB review only where safe and measured.
7. [x] Stage 6 - UI contracts cleanup with bridge/adaptor design.
8. [x] Stage 7 - Burst/parallelism on proven hot systems. Complete for code/guard remediation; profiler compare still required before claiming FPS improvement.
9. [x] Stage 8 - Building transform link review. Complete; focused profiler evidence shows no code change is currently justified.
10. [x] Stage 9 - Namespace migration as its own refactor. Deferred by user; do not execute for this remediation pass.
11. [ ] Stage 10 - Lower-priority structural hygiene. Pending as a separate cleanup plan.

## Merge Gate

- [ ] Unity compile has zero errors.
- [ ] No new architecture-boundary violations.
- [ ] No unrelated files are committed.
- [ ] Focused tests for touched code pass.
- [ ] Match smoke passes when runtime/bootstrap/UI shell code is touched.
- [ ] Performance claims include before/after profiler evidence.
