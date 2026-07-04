# Architecture and Performance Audit Follow-up Tracker

## Goal
Turn `Design/AgentReports/2026-07-02_audit_architecture-performance-followup.md` into a fast, measurable implementation plan. Start with quick wins and low-risk fixes, then move into measured ECS/SOLID architecture work. Keep every slice behavior-preserving unless a later tracker item explicitly calls out a tuning change.

## Source
- Audit commit: `94ddc5d48 Arcitecture and Performance audit followup`
- Audit document: `Design/AgentReports/2026-07-02_audit_architecture-performance-followup.md`
- Audit baseline: Unity `6000.5.2f1`, main `37c035a70`

## Agreement Assessment

### Agreed
- Mobile URP settings are the first practical performance win. Shadow distance, cascade count, HDR, MSAA, and soft shadows are config-driven and should be tested immediately against the Android 30 FPS target.
- Android ground truth is mandatory. Editor-only measurements are useful for regressions, but mobile decisions need at least one real-device baseline.
- Burst coverage is a valid quick/medium pass, but only for Burst-eligible `ISystem` files.
- `MatchSceneView.OnGUI` and related diagnostics must be editor/development-only. Release builds should not tick IMGUI diagnostics.
- Interpolated diagnostic logs in hot paths should be gated before string construction.
- `Object.Instantiate` inside ECS systems is a real drift signal. These call sites should move to entity prefab instantiation, pooled presentation helpers, or other explicitly owned edges.
- The managed helper layer should be frozen for new gameplay logic. New gameplay should use Burst-capable `ISystem` ownership first, with thin Canvas/presentation helpers only at the edge.
- Hot-path helper migration must be measured-first. Selection, BuildingPlacement/transports, and AttackVfx are the right first candidates because they have measured cost.
- CI needs performance guardrails. A contract without an automated capture does not prevent regression.

### Qualified Before Implementation
- Do not rewrite all 309 `*SystemHelper` files. Freeze new drift now, then migrate the measured top hot paths.
- Do not blindly add `[BurstCompile]` everywhere. Classify each missing-Burst `ISystem` as `Burst eligible`, `Managed edge`, `Presentation only`, or `Needs refactor`.
- Do not split `Game.Runtime` before compiler health and performance baselines are stable. Assembly splitting is valuable, but it is not a quick win.
- Do not replace `Object.Instantiate` with parallel gameplay logic. Keep ECS as the gameplay owner and move only visual/presentation instantiation to explicit pooled edges.
- Do not tune gameplay balance while doing performance work. Config changes here are render/diagnostic/infrastructure unless separately approved.

## Current Baseline From Audit

| Area | Audit status | Tracker interpretation |
|---|---:|---|
| SystemBase to ISystem migration | 24 SystemBase / 141 ISystem | Major ECS direction is working. Continue guardrails. |
| Main-thread `.Run()` jobs | 0 `.Run()`, 33 `ScheduleParallel` | Good baseline. Preserve. |
| Managed `IComponentData` | 0 | Good baseline. Preserve. |
| Scripting backend | IL2CPP configured | Good baseline. Needs Android measurement. |
| Assembly graph | Clean | Preserve during future domain splits. |
| Burst coverage | 72 of 125 ISystem files missing Burst | Quick/medium pass after classification. |
| ECS `Object.Instantiate` | 15 open | Medium pass after inventory. |
| `TransportBoardingCommandSystem` | 4,022 lines | Structural issue. Defer until measured quick wins are complete. |
| Mobile shadows | 240 m, 4 cascades, soft shadows, HDR, MSAA 2x | First config quick win. |
| Change filters | Underused | Medium pass after Burst/config work. |
| Android device data | None | Required baseline before deeper tuning. |

## Rules
- No UI Toolkit.
- No new `Boundary` or `Presenter` class names.
- No new MonoBehaviour gameplay `Update` loops.
- No new managed helper gameplay ownership.
- New gameplay/projection logic should be Burst-capable `ISystem` where practical.
- Canvas and MonoBehaviour code remains serialized-reference binding, button-event, scene bootstrap, camera, or visual-state application only.
- Preserve existing scene/prefab/component bindings.
- Preserve Unity `.meta` files.
- Run `git diff --check` after each slice.
- If Unity compile is available, do not hand off known compiler errors as complete.

## Priority Strategy
1. Stabilize compiler and inventory counts.
2. Apply config and diagnostic quick wins.
3. Re-baseline GC and Android performance.
4. Add Burst where mechanically safe.
5. Move ECS `Object.Instantiate` call sites to proper ownership.
6. Migrate measured hot helper paths.
7. Split assemblies and add CI gates after behavior and performance are stable.

## Progress Snapshot

| Field | Status |
|---|---|
| Checklist complete | 85 / 122 |
| Checklist percent complete | 69.7% |
| Current phase | Phase 8 - Managed Helper Hot-path Migration |
| Quick wins complete | 6 / 6 |
| Current target | Selection allocation attribution has been measured with an editor-only subphase probe and classified as report/profiler attribution noise for this capture. The building-placement/runtime instantiate attribution now has an editor-only allocation probe on `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance`, reported through the existing Match GC capture. The next exact action is to rerun the Unity battle capture in a licensing-capable editor session and use the new probe row to classify whether remaining `Object.Internal_CloneSingleWithParent` rows are real runtime building visual creation, pooled reuse, or profiler/startup attribution. |
| Compiler status | Latest building-visual allocation-probe slice passed `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, and `git diff --check` with 0 errors. Unity 6.5.2 battle capture attempted in sandbox and escalated out-of-sandbox at `/private/tmp/warline-arch-followup-building-visual-probe-battle.log`; the escalated run entered the known `LicenseClient-farhad` unsupported protocol / missing headless package loop before project compile/capture started, so Unity-side validation is blocked rather than passed for this slice. |
| Android baseline status | Not started |
| GC baseline status | Latest completed 2026-07-04 16:43:58 UTC battle-state Match GC capture after selection probe and building-runtime diagnostic gating: 300 requested frames after 180 warmup frames, 1 scanned profiler frame from the Unity 6.5.2 raw profile loader, 7,720 raw GC.Alloc samples / 529,549 raw bytes, 6,613 player-relevant samples / 445,585 bytes after excluding editor/tooling rows, and 1,107 editor/tooling samples / 83,964 bytes. Runtime allocation probe assertion passed for `UIShellEcsPresentationSystem.Update`, `MenuBootstrapView.Update`, and the diagnostic-only selection subphase probe. `BuildingRuntimeSliceDiag` / `LogStringToConsole` no longer appears in searched/top rows after removing the batchmode diagnostic bypass. A new building visual allocation probe is implemented but not yet captured because Unity 6.5.2 validation is blocked by licensing/headless startup before project compile/capture starts. |
| Burst coverage status | Runtime inventory for Phase 5 scope: 124 `ISystem` files under `Systems`, `Rendering/Systems`, and `UI/Shell/Ecs`; 71 runtime files missing `[BurstCompile]`. Conservative classification complete: 0 immediately Burst-eligible, 24 managed edge, 8 presentation-only, 39 needs refactor. `EcsBurstHotPathArchitectureTests.NoBurstISystemFilesMustBeClassified` now guards against new unclassified no-Burst runtime `ISystem` files. |
| Mobile URP status | `Mobile_RPAsset` shadow distance changed from 240 m to 90 m and cascades from 4 to 2; HDR, MSAA 2x, render scale 0.8, and soft shadows remain unchanged pending visual/Android baseline |
| ECS instantiate status | Phase 6 classification report added at `Design/AgentReports/2026-07-03_ecs-instantiate-ownership-classification.md`: 20 runtime call lines across Systems/Rendering/Environment/UI ECS scan roots, 0 gameplay entity spawns, 17 visual/presentation spawns, 2 metadata/probe instantiates, and 1 environment material clone. First implementation slice added a narrow presentation-edge pool for `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance`; ECS gameplay request/commit ownership is unchanged. |
| Fuel/Oil drift status | Drift reduced further: production/conversion math is no longer embedded in `FactionResourceCompositionSystemHelper`, runtime-created building combat entities carry their runtime building ids, resource buildings now have a per-building ECS storage mirror, a disabled Burst-capable ECS tick/apply surface is validated, managed production applies through that ECS surface, production plus hauler updates refresh the live ECS storage mirror for resource-capable buildings, hauler load/unload storage mutation routes through `BuildingResourceHaulerTransferEcsSystem`, both live production/conversion and live hauler mutation now prefer the live ECS storage component before mirroring back to runtime-building state, hauler bridge source-storage reads and fuel-source selection now prefer the live ECS storage component, resource visual production flags now prefer the live ECS storage component, faction summary/sell-drain paths now prefer live ECS storage too, selected-building resource storage display reads live ECS storage when available, selected unit/hauler cargo display is covered by a focused ECS `UnitResourceHauler` read-model regression, and faction-summary publish invalidation now hashes live ECS storage so ECS-only storage changes refresh the HUD/read-model buffer. Header/resource summary paths remain read-only display consumers. |
| Validation status | Phase 0 inventory completed; Phase 1 config edited; Phase 2 release OnGUI strip implemented; Phase 3 release diagnostic system gate implemented; Phase 4 GC quick wins completed; Phase 5 Burst inventory/classification report added and guardrail implemented; Phase 6 instantiate classification report added and first placement-visual pooling slice implemented; Phase 8 Fuel/Oil behavior safety-net tests added, production math extracted into a pure system-helper surface, ECS storage preference added across production, haulers, faction summaries, selection display, resource visuals, and publish invalidation, production/runtime helper profiler markers standardized, first AttackVfx request-processing slice completed with focused request-resolution tests, post-AttackVfx battle capture completed, selection attribution probe added, batchmode building-runtime diagnostic logging gated by the existing diagnostic flag, and editor-only building visual allocation probe added. Latest validations passed: `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, and `git diff --check`. Unity 6.5.2 battle capture is blocked at licensing/headless startup in `/private/tmp/warline-arch-followup-building-visual-probe-battle.log`. |
| Still wrong / next iteration | No visual defects are known from code/compile validation, but manual visual smoke checks for placement-preview pooling, command behavior, and combat VFX timing are still pending. Fuel/Oil still keeps runtime-building storage mirrors for compatibility. Unity 6.5.2 raw profile loading currently collapses the last completed battle capture report to 1 scanned profiler frame while runtime probes still cover 300 updates; the next exact action is to rerun the battle capture from a licensing-capable Unity session and read the new `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance` probe row. Android baseline is still not captured, and HDR/soft-shadow tier ownership remains unresolved. |

## Phase 0 - Baseline And Inventory
Fast setup work. No behavior changes.

- [x] Confirm Unity compiler is green before performance/config changes.
- [x] Confirm exact Unity version and active mobile renderer asset path.
- [x] Re-run current `ISystem` inventory and Burst coverage count.
- [x] Re-run current `Object.Instantiate` inventory under `Assets/Game/Scripts`.
- [x] Re-run current `Debug.Log($"...")` inventory under `Assets/Game/Scripts/Systems`.
- [x] Re-run `OnGUI` call-chain inventory from `MatchSceneView` to diagnostics helpers.
- [x] Re-run `SystemBase` / `ISystem` counts and compare to audit.
- [x] Identify current `VisualQualityConfig` ownership and tier switching path.
- [x] Confirm Android build target, IL2CPP, and runInBackground settings still match audit.
- [x] Inventory the new Fuel/Oil feature files and identify which parts are gameplay simulation, ECS projection, UI display, and diagnostics.
- [x] Confirm whether oil production, oil capacity, fuel conversion, truck transfer, and refinery storage are owned by ECS systems/components rather than managed UI/helper state.
- [x] Update this progress snapshot with actual local counts.

## Phase 0A - Fuel/Oil Feature Drift Audit
Fast architecture check for the new resource feature called out by the audit as a likely managed-helper drift risk.

- [x] Trace oil pump production from config to runtime storage.
- [x] Trace transport truck oil/fuel load, unload, and capacity state.
- [x] Trace refinery oil input, fuel output, and dual-capacity state.
- [x] Trace match header oil/fuel totals and confirm they are read-only UI projections.
- [x] Trace selected unit/building resource panel data and confirm it is read-only UI projection.
- [x] Identify any managed helper that owns authoritative Fuel/Oil gameplay state.
- [x] Identify any Fuel/Oil update path that ticks from MonoBehaviour/Canvas instead of ECS.
- [x] Identify any Fuel/Oil `SystemBase`, non-Burst `ISystem`, or managed `IComponentData` drift.
- [x] Decide the corrective target for each drift: Burst `ISystem`, ECS component/buffer/config, or UI-only sink.
- [x] Update the tracker with the Fuel/Oil drift inventory and priority.

### Phase 0 Findings

- Unity version: `6000.5.2f1 (eb73d3b415a1)` from `ProjectSettings/ProjectVersion.txt`.
- Android/runtime setup: `runInBackground: 1`; Android scripting backend is IL2CPP; Android static batching is enabled and dynamic batching is disabled.
- Mobile render asset: `Assets/Settings/Mobile_RPAsset.asset` currently has HDR enabled, MSAA 2x, render scale `0.8`, main/additional shadow maps at `2048`, shadow distance `240`, cascade count `4`, and soft shadows enabled.
- Visual quality ownership: `Assets/Game/Rendering/VisualQualityConfig.asset`, `Assets/Game/Scripts/Configs/VisualQualityProfileAsset.cs`, and `Assets/Game/Scripts/Systems/VisualQualitySettingsSystem.cs` own tier render scale and directional shadow strength; shadow distance, cascade count, HDR, and MSAA do not appear tier-owned yet.
- ECS inventory: `125` `ISystem` files, `72` missing `[BurstCompile]`, `24` actual `SystemBase` classes, and `0` newly identified managed `IComponentData` drift in this inventory pass.
- ECS instantiate inventory: `15` runtime `Object.Instantiate` call lines under `Assets/Game/Scripts/Systems`.
- Diagnostics inventory: direct interpolated `Debug.Log($"...")` hot-path candidates were found in diagnostics/animation/startup helpers; ResourceHauler has additional gated interpolated logs and one warning.
- IMGUI diagnostics chain: `MatchSceneView.OnGUI()` calls `MatchBootstrapCompositionSystemHelper.OnGUI()`, with diagnostics work downstream.
- Unity MCP status: attempted after user re-approval, but Unity MCP still returns `Connection revoked`; fallback validation is editor-log and deterministic file inspection until the bridge accepts the client.

### Phase 0A Fuel/Oil Drift Findings

- ECS data exists for storage and projection: `BuildingRuntimeFactionSummary` carries faction oil/fuel totals and rates, while `UnitResourceHauler` / `UnitResourceHaulOrder` carry hauler cargo, capacity, phase, and resource kind.
- Header UI is read-only display: `UiShellEcsGateway.TryReadMatchHudHeader` reads `BuildingRuntimeFactionSummary` and formats separate oil/fuel strings for Canvas.
- Selected unit/building UI is read-only display: `FocusedUnitUiReadModelUiSystemHelper`, `SelectionHudFeedbackUiSystemHelper`, and `MatchHudSelectionPanelView` read ECS/runtime storage values into passenger/resource chips.
- Authoritative simulation drift exists in managed helpers: `FactionResourceCompositionSystemHelper.UpdateResourceProduction` mutates `StoredOilBarrels` and `StoredFuelBarrels` on runtime building objects for oil extraction and refinery conversion.
- Authoritative hauler transfer drift exists in managed helpers: `ResourceHaulerUtilitySystemHelper` and `BuildingResourceHaulerBridgeCompositionSystemHelper` mutate runtime building storage and `UnitResourceHauler` cargo during load/unload.
- Tick ownership drift: `BuildingProductionRuntimeTickCompositionSystemHelper` drives production through `UnityEngine.Time.deltaTime` and managed runtime-building dictionaries rather than a Burst-capable ECS simulation system.
- Corrective target: move oil pump production, refinery conversion, hauler load/unload, and storage mutation into ECS components/buffers processed by Burst-capable `ISystem` code where practical; keep Canvas header and selection-panel code as read-only visual sinks.

## Phase 1 - Mobile Render Config Quick Win
Lowest-risk large win. Config-only unless tier wiring is missing.

- [x] Inspect `Assets/Settings/Mobile_RPAsset.asset` and related renderer assets.
- [x] Set mobile shadow distance from 240 m to a first target of 90 m.
- [x] Set cascade count from 4 to 2 for the mobile tier.
- [ ] Decide soft shadow default per tier through `VisualQualityConfig`.
- [ ] Decide HDR default per tier through `VisualQualityConfig`.
- [x] Verify MSAA/renderScale are still intentional for the target device class.
- [x] Capture before/after config diff in this tracker.
- [ ] Run Unity compile and a lightweight scene smoke test.

### Phase 1 Mobile Render Config Diff

- `Assets/Settings/Mobile_RPAsset.asset`
  - `m_ShadowDistance`: `240` -> `90`
  - `m_ShadowCascadeCount`: `4` -> `2`
- Deferred until Android/visual baseline or tier wiring work:
  - `m_SupportsHDR`: remains `1`
  - `m_MSAA`: remains `2`
  - `m_RenderScale`: remains `0.8`
  - `m_SoftShadowsSupported`: remains `1`
  - `m_SoftShadowQuality`: remains `2`

## Phase 2 - Release Diagnostic Strip
Quick architecture/perf cleanup.

- [x] Inspect `MatchSceneView.OnGUI`.
- [x] Inspect `MatchBootstrapCompositionSystemHelper` diagnostics path.
- [x] Wrap IMGUI diagnostic tick path with `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- [x] Keep editor and development diagnostics available.
- [x] Confirm release/player builds do not include the IMGUI diagnostics path.
- [x] Run compile validation.
- [x] Update tracker with changed files and validation result.

### Phase 2 Release Diagnostic Strip Diff

- `Assets/Game/Scripts/Composition/MatchSceneView.cs`
  - Wrapped `OnGUI()` with `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- `Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs`
  - Wrapped public `OnGUI()` and `OnGuiRuntime(...)` with `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Runtime effect:
  - Editor and development builds keep road/selection IMGUI diagnostics.
  - Non-development player builds no longer include the match IMGUI diagnostic entry path.

## Phase 3 - Diagnostic Logging Allocation Cleanup
Quick GC cleanup in hot systems.

- [x] Inventory interpolated `Debug.Log($"...")` calls in `Assets/Game/Scripts/Systems`.
- [x] Group each call by always-on, gated diagnostic, warning/error, or temporary debug.
- [x] Move diagnostic enable gates before message construction.
- [x] Replace repeated diagnostic string interpolation with conditional helper calls where appropriate.
- [x] Preserve warnings/errors that are user-facing or required for failure diagnosis.
- [x] Run compile validation.
- [x] Update tracker with remaining intentional log call count.

### Phase 3 Diagnostic Logging Cleanup Diff

- `Assets/Game/Scripts/Systems/PreGameEcsActivityDiagnosticsSystem.cs`
  - Disabled the diagnostic-only ECS activity system outside `UNITY_EDITOR || DEVELOPMENT_BUILD`.
  - Release players no longer run the pre-game query/count/log path or construct its diagnostic interpolated strings.
- Reviewed remaining interpolated log families under `Assets/Game/Scripts/Systems`:
  - `ResourceHaulerUtilitySystemHelper` verbose logs are already behind `RuntimeConfig.VerboseResourceHaulerLogs`; warning logs are preserved.
  - `LoadingGateSystem`, performance diagnostics, and render/frame diagnostics already gate message construction behind state/interval/threshold checks.
  - No broad helper abstraction was added because the current safe quick win is compile-time release stripping for the always-diagnostic system; deeper log-helper churn would be riskier than useful without GC capture data.
- Remaining intentional log categories:
  - Warnings/errors required for failure diagnosis.
  - Editor/development diagnostics.
  - Threshold/cooldown diagnostics that are already gated before message construction.

## Phase 4 - GC Re-baseline And Allocation Gate
Measure before deeper managed-helper work.

- [x] Identify the existing GC callstack capture tool or recreate the documented command path.
- [x] Run a 300-frame steady-state ScenarioLab or match smoke capture.
- [x] Record managed allocations per frame after June fixes.
- [x] Identify top allocation call stacks.
- [x] Add or update a zero-alloc smoke assertion where practical.
- [x] Record any known unavoidable editor-only allocations separately from player allocations.
- [x] Update tracker with the new baseline and top three offenders.

### Phase 4 GC Capture Path

- Existing capture tool:
  - `Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs`
  - Execute method: `Game.Editor.MatchGcAllocationCallstackCapture.RunSteadyState`
  - Built-in capture shape: opens `Assets/Game/Scenes/Menu.unity`, routes into Match, warms up 180 frames, captures 300 frames with `Profiler.enableAllocationCallstacks`, then writes a report under `Design/AgentReports`.
  - Unity 6.5.2 Match scene load in batchmode exceeded the old 180s readiness timeout, so the editor-only capture timeout is now 360s.
- Working command shape:
  - Unity 6.5.2 windowed batchmode, escalated/out-of-sandbox, no `-quit`, execute method `Game.Editor.MatchGcAllocationCallstackCapture.RunSteadyState`.
  - This follows the project licensing workaround: run Unity outside the sandbox and let the execute method finish the asynchronous Play Mode capture before quitting.
- Current result:
  - Unity batchmode compile/capture reaches Menu, enters Play Mode, loads Match, completes warmup/capture, writes the GC report, and exits with `[MatchGcAllocationCallstackCapture] result=Passed frames=300`.
  - Shutdown still logs Unity editor preview-scene leak noise and Unity AI/MCP tracing noise. No compiler errors were found in the capture logs.

### Phase 4 GC Baseline - 2026-07-02

- Successful command:
  - Unity 6.5.2 windowed batchmode, escalated/out-of-sandbox, no `-quit`, execute method `Game.Editor.MatchGcAllocationCallstackCapture.RunSteadyState`.
  - Log: `/private/tmp/warline-architecture-audit-gc-steady-windowed-noquit.log`.
  - Result marker: `[MatchGcAllocationCallstackCapture] result=Passed frames=300`.
- Report:
  - `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture.md`
- Latest date in report: `2026-07-03 18:30:06 UTC`.
- Capture summary:
  - Requested frames: `300`.
  - Warmup frames before capture: `180`.
  - Profiler frame range: `0..300`.
  - Scanned frames with data: `301`.
  - GC.Alloc samples: `13,241`.
  - GC.Alloc bytes from hierarchy column: `1,402,131`.
  - GC.Alloc samples excluding editor/tooling rows: `7`.
  - GC.Alloc bytes excluding editor/tooling rows: `736`.
  - Editor/tooling GC.Alloc samples excluded from player-relevant rows: `13,234`.
  - Editor/tooling GC.Alloc bytes excluded from player-relevant rows: `1,401,395`.
  - Runtime allocation probe reports `UIShellEcsPresentationSystem.Update` and `MenuBootstrapView.Update` as `0 bytes / 0 allocating updates / 300 total updates`, and the capture now fails if either probe records allocation.
- Completed allocation fixes verified by rerun:
  - `UiDiagnosticsReadModelSystem` now rebuilds runtime diagnostics log text only while the diagnostics overlay is visible, and only on first visibility or log-version changes; `UiDiagnosticsReadModelSystem` / `BuildLogText()` no longer appears in the searched/top allocation sites.
  - `BuildingPlacementInputTickCompositionSystemHelper` now checks for pending UI placement commands before constructing `BuildingPlacementCommandCompositionSystemHelper` delegate-heavy contexts; `CreateContextSource()` no longer appears in the searched/top allocation sites.
  - `SelectionGameplayStartupSystemHelper` now caches the HUD feedback context, board preview delegates, tactical-follow camera context, pointer-target command context, focused-unit refresh delegate, selected-building callbacks, and selection-panel board/resource callbacks; `CreateTacticalFollowCameraContext()` and `CreatePointerTargetCommandContext()` no longer appear in the searched/top allocation sites.
  - `SelectionHudFeedbackUiSystemHelper` now caches the `SelectedUnitTag` query used by the runtime selection-panel refresh path; the public static summary query path remains available for focused tests.
  - `FocusedUnitUiReadModelUiSystemHelper` now reuses focused-unit label/description fixed strings while the focused entity is unchanged and builds health fixed strings from numeric values instead of managed interpolation.
  - `SelectionHudFeedbackUiSystemHelper` now skips reapplying the focused-unit selection-panel model and transport/passenger drawer model when the focused entity, order, health, board availability, and cargo/passenger summary are unchanged. This reduced total captured GC in the latest run, but did not improve the selection marker, so it remains under review rather than accepted as the final selection-lane fix.
  - `SelectionHudFeedbackUiSystemHelper` now avoids repeated hidden-panel apply calls when no selection is active and uses fixed-string vehicle/transport classification instead of managed source-key strings in the selection summary path.
  - `SelectionUiReadModelLookup` now uses fixed-string source-key prefix checks for vehicle/character classification in visible selection reads.
  - `MatchGcAllocationCallstackCapture` now reports raw totals and a compiler-thread-separated top allocation table so editor Burst compiler-thread noise does not hide player-relevant rows.
  - `SelectionGameplayStartupSystemHelper` now wraps selection runtime phases in profiler submarkers and reuses cached query handles for move-target, select-all, deselect-all, and immediate selected-unit command result flushing instead of creating temporary queries each live flush.
  - `RtsCameraSystem.TryGetCameraGroundBounds()` now avoids the `Mathf.Min/Max` params-array overload while computing four-corner camera bounds.
  - `FixedWingRunwayUnitUtility.IsFixedWingRunwayUnit()` now scans `FixedString64Bytes` in place instead of converting source keys to managed strings each frame; `FixedWingRunwayHomeInitializationSystem` no longer appears in the searched/top allocation rows.
  - `RtsSelectionCancelActiveCommandModeSystem.ProcessPendingRequests` now has a public cached-query overload for live helper callers, and `RtsSelectionCommandResultFlushCompositionSystemHelper.ProcessCancelActiveCommandModeRequests` routes through cached command/runtime queries instead of creating temporary queries each flush; this cancel-command frame no longer appears in the searched/top allocation rows.
  - `TacticalFollowCameraModeSystemHelper` now caches singleton queries for target, pose, request queue, mode, and UI read model entities on the helper instance instead of creating temporary query objects in `EnsurePoseEntity` and related singleton factories; `TacticalFollowCameraModeSystemHelper.EnsurePoseEntity` no longer appears in the searched/top allocation rows.
  - `RtsSelectionBoardTargetModeCommandSystem.ProcessPendingRequests` now exposes its cached-query overload for live helper callers, and `RtsSelectionCommandResultFlushCompositionSystemHelper.ProcessBoardTargetModeCommandRequests` routes through startup-owned command queue, runtime state, and selected-tag queries; `RtsSelectionBoardTargetModeCommandSystem.ProcessPendingRequests` no longer appears in the searched/top allocation rows.
  - `RuntimeGameplayStateSystem.TryGetStateEntity` now caches the runtime-state singleton entity per Unity world instead of creating a temporary query on each gameplay/camera state read; `RuntimeGameplayStateSystem.TryGetStateEntity` no longer appears in the searched/top allocation rows.
  - `RtsSelectionScanTargetModeCommandSystem.ProcessPendingRequests` now exposes its cached-query overload for live helper callers, and `RtsSelectionCommandResultFlushCompositionSystemHelper.ProcessScanTargetModeCommandRequests` routes through startup-owned command queue and runtime-state queries; `RtsSelectionScanTargetModeCommandSystem.ProcessPendingRequests` no longer appears in the searched/top allocation rows.
  - `MatchGcAllocationCallstackCapture` now treats the existing editor-only runtime allocation probes as an assertion: `UIShellEcsPresentationSystem.Update` and `MenuBootstrapView.Update` must remain at `0` captured bytes, or the GC capture exits failed after writing the report.
  - `MatchGcAllocationCallstackCapture` now excludes Burst compiler-thread rows and Unity AI/MCP editor tooling stacks from the player-relevant top allocation table while keeping them visible in separate editor/tooling and raw top allocation tables.
- Top recurring offender after these fixes:
  - The latest player-relevant table contains only `736` bytes / `7` samples with no managed call stack captured. The remaining large raw rows are bucketed as editor/tooling because their call stacks are Unity AI/MCP/Tracing logging, even when their hierarchy paths sit under gameplay profiler markers.
- Top editor/tooling allocation rows from the latest report:
  - Rank 1: `417,183` bytes / `1,907` samples / `1` frame, `UnityEngine.DebugLogHandler:Internal_Log_Injected`, parent hierarchy `EditorApplication.update: callback in Unity.AI.Toolkit.EditorTask`.
  - Rank 2: `216,018` bytes / `968` samples / `1` frame, `UnityEngine.DebugLogHandler:Internal_Log_Injected`, parent hierarchy `EditorApplication.update: callback in Unity.AI.Toolkit.EditorTask`.
  - Rank 3: `190,344` bytes / `1,647` samples / `36` frames, `UnityEngine.DebugLogHandler:Internal_Log_Injected`, thread-pool tooling/logging noise.
  - Rank 10: `27,198` bytes / `246` samples / `3` frames, parent hierarchy `EditorApplication.update: Unity.AI.MCP.Editor.Bridge.ProcessCommands`.
- Known editor/tooling allocations separated from gameplay cleanup target:
  - The capture report now separates editor/tooling allocations from raw totals. The latest run excludes `13,234` editor/tooling samples / `1,401,395` editor/tooling bytes from player-relevant rows.
  - Unity AI/MCP tracing stack logs appeared during batch shutdown and capture; these are editor/tooling noise, not player gameplay code unless the same site also appears in the player-relevant table.
- Next cleanup target:
  - Keep the GC capture workflow from confusing raw editor/tooling rows with player gameplay work; once player-relevant rows remain negligible, move to the Android baseline or the Fuel/Oil ECS drift correction.

## Phase 5 - Burst Coverage Pass
Mechanical pass, but only where correct.

- [x] Generate a list of `ISystem` files missing `[BurstCompile]`.
- [x] Classify each as `Burst eligible`, `Managed edge`, `Presentation only`, or `Needs refactor`.
- [ ] Add `[BurstCompile]` to Burst-eligible system structs.
- [ ] Add `[BurstCompile]` to eligible `OnCreate`, `OnUpdate`, and job methods.
- [x] Leave managed-edge systems un-Burst and document why.
- [ ] Compile after each small batch.
- [ ] Update the coverage count after each batch.
- [x] Add an architecture guardrail so new Burst-eligible `ISystem` files are not silently added without classification.

## Phase 6 - ECS Instantiate Ownership Cleanup
Medium pass. Must avoid parallel gameplay logic.

- [x] Inventory all `Object.Instantiate` calls under runtime ECS/system code.
- [x] Classify each call as gameplay entity spawn, visual VFX, UI/presentation, authoring/editor, or test-only.
- [ ] Convert gameplay spawns to entity prefab/ECB ownership where practical.
- [ ] Move visual GameObject creation to explicit pooled presentation helpers.
- [ ] Add pool lifetime cleanup for VFX/presentation objects.
- [ ] Keep ECS event/request ownership for the underlying gameplay action.
- [ ] Run focused tests for each converted call-site family.
- [ ] Update tracker with remaining intentional non-runtime or editor-only instantiates.

## Phase 7 - Android Ground Truth Baseline
Required before deeper tuning decisions.

- [ ] Select one mid-tier Android target device profile.
- [ ] Build IL2CPP Android with the mobile render config.
- [ ] Run a 10-minute match/session capture.
- [ ] Record p50, p95, and worst-frame CPU/GPU frame times.
- [ ] Record thermal state and throttling symptoms.
- [ ] Record draw calls, batches, triangles, and steady-state GC.
- [ ] Compare Android results against editor assumptions and update priorities.

## Phase 8 - Managed Helper Hot-path Migration
Measured architecture work. Do not boil the 309-helper ocean.

- [ ] Freeze new managed-helper gameplay ownership in review/architecture scripts.
- [x] Add Fuel/Oil production and hauler transfer behavior safety-net tests before ECS ownership migration.
- [x] Extract Fuel/Oil production/conversion math into a pure system-helper surface for ECS reuse.
- [x] Populate runtime-created building combat entity ids before ECS resource ownership uses combat entities.
- [x] Add a per-building ECS resource storage mirror for runtime-created resource buildings.
- [x] Add a disabled Burst-capable ECS resource-production tick/apply surface for explicit handoff validation.
- [x] Route the managed Fuel/Oil production apply step through the validated ECS tick surface without double-ticking.
- [x] Add a disabled Burst-capable ECS hauler transfer surface for explicit handoff validation.
- [x] Route live hauler load/revert/unload through the validated ECS transfer surface without double-owning state.
- [x] Make live hauler storage mutation prefer the combat entity `BuildingResourceStorageComponent` and mirror back for compatibility.
- [x] Make live production/conversion prefer the combat entity `BuildingResourceStorageComponent` and mirror back for compatibility.
- [x] Make faction resource summaries and sell/drain requests prefer the combat entity `BuildingResourceStorageComponent` and mirror drains back for compatibility.
- [x] Make selected-building resource storage display prefer the combat entity `BuildingResourceStorageComponent`.
- [x] Audit selected unit/hauler cargo display for ECS preference and add a focused regression for `UnitResourceHauler` oil/fuel cargo chips.
- [x] Make faction-summary publish invalidation prefer combat entity `BuildingResourceStorageComponent`.
- [x] Make hauler bridge source-storage reads prefer combat entity `BuildingResourceStorageComponent`.
- [x] Make resource-building visual production flags prefer combat entity `BuildingResourceStorageComponent`.
- [x] Make hauler fuel-source selection prefer combat entity `BuildingResourceStorageComponent`.
- [x] If Fuel/Oil drift is found, move authoritative production/storage/transfer/conversion state into ECS before broader helper migrations.
- [x] Add or standardize profiler markers on helper tick paths.
- [x] Start with AttackVfx if still measured as the largest spike.
- [x] Move AttackVfx request processing toward ECS/jobs while keeping visual spawning at the presentation edge.
- [x] Add an editor-only selection runtime subphase allocation probe before migrating selection updates.
- [x] Gate building-runtime slice diagnostics by the existing diagnostic flag in batchmode captures.
- [x] Add an editor-only building visual allocation probe before migrating building placement/runtime instantiate ownership.
- [ ] Migrate BuildingPlacement/transports only after current allocation and frame-time data confirms priority.
- [ ] Migrate Selection updates after Selection allocation/frame-time data is captured.
- [ ] Keep Canvas/MonoBehaviour code as serialized-reference visual binders.
- [ ] Add focused tests for each migrated hot path.
- [x] Re-run GC and frame-time capture after every hot-path migration.

## Phase 9 - TransportBoardingCommandSystem Decomposition
Structural work after quick wins and measured baselines.

- [ ] Inventory current internal phases of `TransportBoardingCommandSystem`.
- [ ] Split only along stable responsibility seams.
- [ ] Preserve production ECS ownership for boarding, movement, deploy, rope, and airdrop.
- [ ] Add tests before extracting each phase.
- [ ] Keep public command behavior unchanged.
- [ ] Re-run boarding ScenarioLab validation after each extraction.

## Phase 10 - Game.Runtime Domain Split
Longer-term compile and ownership improvement.

- [ ] Confirm compiler is clean and tests are stable before assembly splitting.
- [ ] Draft target domain asmdefs: Combat, Buildings, Transport, Selection/Camera, Pathfinding.
- [ ] Keep Contracts assemblies as the only cross-domain currency.
- [ ] Split one domain per PR/slice.
- [ ] Run full compile and focused tests after each split.
- [ ] Update architecture docs after each domain split.

## Phase 11 - CI Performance Regression Gate
Make the gains durable.

- [ ] Map `performance_regression_contract.md` requirements to concrete ScenarioLab captures.
- [ ] Add a headless or batchmode capture path for weekly/per-merge CI.
- [ ] Assert p95 frame-time budget.
- [ ] Assert steady-state GC budget.
- [ ] Store baseline artifacts for trend comparison.
- [ ] Fail CI on budget breach once baseline is accepted.

## Validation Log
- 2026-07-02: Created tracker from `Design/AgentReports/2026-07-02_audit_architecture-performance-followup.md`. No code/config changes made.
- 2026-07-02: Completed Phase 0/0A inventory by deterministic file inspection. Unity MCP still returned `Connection revoked` after retry, so MCP validation was not available.
- 2026-07-02: Applied conservative mobile render quick win: `Mobile_RPAsset` shadow distance `240 -> 90`, cascade count `4 -> 2`. No gameplay code changes.
- 2026-07-02: `git diff --check` passed. Unity 6.5.2 batchmode compile could not run because `/Users/farhad/Projects/WarlineCapture` is already open in another Unity instance. Editor log tail showed no current compiler errors, but this is not a full compile gate.
- 2026-07-02: Phase 2 release diagnostic strip implemented. `dotnet build Game.Composition.csproj --no-restore` passed with 0 errors and 16 existing Unity generated-project reference warnings.
- 2026-07-02: After the editor was closed, Unity 6.5.2 batchmode compile passed with no compiler errors. Phase 3 diagnostic release gate implemented. `git diff --check`, `dotnet build Game.Runtime.csproj --no-restore`, and Unity batchmode compile passed after the change.
- 2026-07-02: Phase 4 capture path identified as `Game.Editor.MatchGcAllocationCallstackCapture.RunSteadyState`. Batchmode GC capture attempt was blocked before project execution by Unity 6.5.2 licensing handshake failures, then the stuck process was killed. No project code or compile failure was observed in that capture attempt.
- 2026-07-02: Retried Unity with the documented licensing workaround: escalated/out-of-sandbox, windowed batchmode, and no `-nographics`. A first retry with `-quit` only opened `Menu.unity` then quit before the async capture. The corrected no-`-quit` command passed, loaded Match, warmed up, captured 300 frames, and wrote the current GC report.
- 2026-07-03: Phase 4 GC cleanup slice cached diagnostic-log rebuilds, building-placement command context creation, selection HUD feedback context, board preview delegates, tactical-follow camera context, and selection panel callbacks. Unity 6.5.2 compile passed after each selection edit. Match GC capture passed with `35,158` samples and `2,166,981` hierarchy-column bytes, down from the earlier `190,938` samples and `27,951,959` bytes baseline.
- 2026-07-03: Cached the selection pointer-target command context and selected-tag query, then reran Unity 6.5.2 compile and Match GC capture. Compile passed; capture passed with `35,021` samples and `1,892,651` hierarchy-column bytes. `CreatePointerTargetCommandContext()` no longer appears in searched/top allocation sites. Selection lane remains the top recurring source at `818,096` bytes / `16,645` samples across `299` frames, so this is not a complete selection-lane fix.
- 2026-07-03: Reused focused-unit label/description fixed strings and replaced focused health interpolation with numeric fixed-string append. Unity 6.5.2 compile passed; Match GC capture passed with `34,926` samples and `1,888,685` hierarchy-column bytes. Selection lane moved only slightly to `812,336` bytes / `16,525` samples, so the next fix must skip unchanged selection-panel/model refresh work rather than only caching more context.
- 2026-07-03: Added a focused selection-panel unchanged-state cache and transport/passenger panel key to avoid reapplying identical Canvas selection-panel models each frame. Unity 6.5.2 compile passed; Match GC capture passed with `34,567` samples and `1,829,437` hierarchy-column bytes. The total captured bytes improved, but the selection marker moved to `828,464` bytes / `16,861` samples, so the cache is not yet a proven selection-lane fix.
- 2026-07-03: Added hidden-panel apply guard and fixed-string vehicle/transport classification in the selection HUD/read-model path. `dotnet build Game.Editor.csproj --no-restore`, `dotnet build Game.Runtime.csproj --no-restore`, and Unity 6.5.2 batchmode compile passed with 0 compiler errors. The GC capture tool timeout was raised from 180s to 360s because Unity 6.5.2 Match scene deserialization exceeded the old timeout. Two Match GC captures then passed; one run exposed editor Burst compiler-thread noise at `12,630,812` bytes / `94,325` samples, so the report now includes compiler-thread-separated totals and top sites. The final capture passed with `33,372` samples and `1,914,559` hierarchy-column bytes, with `0` editor compiler-thread bytes and `693,680` bytes / `14,053` samples in the main-thread `GameplayRuntimeUpdate.Selection` row.
- 2026-07-03: Completed the tactical-follow cached-query cleanup for the latest top GC row by caching singleton queries for `TacticalFollowCameraModeSystemHelper` target, pose, request queue, mode, and UI read model entities. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, Unity 6.5.2 compile log `/private/tmp/warline-arch-followup-unity-compile-tacticalfollow-retry.log`, and Match GC capture log `/private/tmp/warline-arch-followup-gc-tacticalfollow-final.log` passed. The GC report no longer includes `TacticalFollowCameraModeSystemHelper.EnsurePoseEntity` in searched/top rows. `RtsSelectionBoardTargetModeCommandSystem.ProcessPendingRequests` is now the next measured allocation target.
- 2026-07-03: Completed the board-target cached-query cleanup by exposing the existing query-based `RtsSelectionBoardTargetModeCommandSystem.ProcessPendingRequests` overload to live helper callers and routing `RtsSelectionCommandResultFlushCompositionSystemHelper.ProcessBoardTargetModeCommandRequests` through cached command queue, runtime state, and selected-tag queries. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, Unity 6.5.2 compile log `/private/tmp/warline-arch-followup-unity-compile-boardtarget-final.log`, and Match GC capture log `/private/tmp/warline-arch-followup-gc-boardtarget-retry.log` passed. The first GC retry stuck in the known licensing loop; the documented workaround was applied by stopping the stuck batch process, clearing stale licensing clients, and rerunning escalated/windowed batchmode. The GC report no longer includes `RtsSelectionBoardTargetModeCommandSystem.ProcessPendingRequests` in searched/top rows. `RuntimeGameplayStateSystem.TryGetStateEntity` is now the next measured allocation target.
- 2026-07-03: Completed the runtime-state singleton entity cache cleanup by caching the `RuntimeGameplayStateComponent` backing entity per Unity world in `RuntimeGameplayStateSystem.TryGetStateEntity`. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, Unity 6.5.2 compile log `/private/tmp/warline-arch-followup-unity-compile-runtime-state.log`, and Match GC capture log `/private/tmp/warline-arch-followup-gc-runtime-state.log` passed. The GC report no longer includes `RuntimeGameplayStateSystem.TryGetStateEntity` in searched/top rows, and the measured excluded-editor-compiler total dropped to `17,267` samples / `1,299,916` bytes. `RtsSelectionScanTargetModeCommandSystem.ProcessPendingRequests` is now the next measured allocation target.
- 2026-07-03: Completed the scan-target cached-query cleanup by exposing the existing cached-query overload on `RtsSelectionScanTargetModeCommandSystem.ProcessPendingRequests` and routing `RtsSelectionCommandResultFlushCompositionSystemHelper.ProcessScanTargetModeCommandRequests` through cached command/runtime queries. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, Unity 6.5.2 compile log `/private/tmp/warline-arch-followup-unity-compile-scan-target.log`, and Match GC capture log `/private/tmp/warline-arch-followup-gc-scan-target.log` passed. The GC report no longer includes `RtsSelectionScanTargetModeCommandSystem.ProcessPendingRequests` in searched/top rows. The latest measured excluded-editor-compiler total is `18,016` samples / `1,282,698` bytes, and the next step is classifying startup-only `UnityEngine.Object.Instantiate` / Burst JIT rows versus recurring lanes.
- 2026-07-03: Completed the Phase 4 zero-allocation assertion by making `MatchGcAllocationCallstackCapture` fail when the existing `UIShellEcsPresentationSystem.Update` or `MenuBootstrapView.Update` editor allocation probes record bytes during the 300-frame Match capture, and by separating Unity AI/MCP editor tooling stacks from player-relevant allocation rows. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, Unity 6.5.2 compile logs `/private/tmp/warline-arch-followup-unity-compile-gc-assertion.log` and `/private/tmp/warline-arch-followup-unity-compile-tooling-filter.log`, plus Match GC capture log `/private/tmp/warline-arch-followup-gc-tooling-filter.log` passed. The report records `Runtime allocation probe assertion: Passed`, `5` player-relevant samples / `510` bytes after excluding editor/tooling rows, and keeps raw editor/tooling rows visible. The earlier large `UnityEngine.Object.Instantiate` rows share the `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance` stack and are classified as Phase 6 visual spawn ownership debt.
- 2026-07-03: Completed the Phase 6 instantiate ownership classification report and first pooling implementation slice. `BuildingPlacementVisualPresentationSystemHelper` now keeps a presentation-edge pool keyed by `BuildingDefinition`; building-placement cancel paths and wall-preview rebuilds return visuals to that pool, while committed runtime buildings keep their existing ownership. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, and Unity 6.5.2 compile log `/private/tmp/warline-arch-followup-placement-pool-compile.log` passed.
- 2026-07-03: Follow-up Match GC capture after placement-visual pooling passed in `/private/tmp/warline-arch-followup-gc-placement-pool.log` and updated `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture.md`. The report records `21,883` raw samples / `1,907,835` raw bytes, `21,629` player-relevant samples / `1,880,085` bytes, and `Runtime allocation probe assertion: Passed`. `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance` no longer appears in searched/top rows; the next recurring target is `RtsSelectionAttackTargetModeCommandSystem.ProcessPendingRequests`.
- 2026-07-03: Completed the attack-target cached-query cleanup by adding query-based `RtsSelectionAttackTargetModeCommandSystem` entry points and routing live selection command flush/startup checks through the cached command queue, runtime state, and selected-tag queries. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, Unity 6.5.2 compile log `/private/tmp/warline-arch-followup-attack-target-cache-compile.log`, and Match GC capture log `/private/tmp/warline-arch-followup-gc-attack-target-cache.log` passed. The GC report no longer includes `RtsSelectionAttackTargetModeCommandSystem.ProcessPendingRequests` in searched/top rows. The latest player-relevant total is `20,796` samples / `1,642,133` bytes, with current recurring rows grouped under `SelectionGameplayStartupSystemHelper.UpdateSelectionRuntimePhases`.
- 2026-07-03: Cached the building runtime boundary entity inside `BuildingRuntimeProcessingCompositionSystemHelper` so the simulation tick reuses a validated world/entity instead of asking the singleton query for the same boundary every frame. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, Unity 6.5.2 compile log `/private/tmp/warline-arch-followup-building-boundary-cache-compile.log`, and Match GC capture log `/private/tmp/warline-arch-followup-gc-building-boundary-cache.log` passed. The report records `12,779` raw samples / `903,267` raw bytes, only `3` player-relevant samples / `272` bytes after filtering, and `Runtime allocation probe assertion: Passed`; raw rows are dominated by `System.Text.StringBuilder` stacks containing `Unity.AI.MCP.Editor.Bridge`, so they are treated as capture-tooling contamination until the capture path isolates MCP traffic.
- 2026-07-03: Split the GC capture report tables into player-relevant, editor/tooling, and raw allocation rows so Unity AI/MCP/Tracing logging remains visible without being mistaken for gameplay cleanup priority. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, Unity 6.5.2 compile log `/private/tmp/warline-arch-followup-gc-report-tooling-table-compile.log`, and Match GC capture log `/private/tmp/warline-arch-followup-gc-report-tooling-table.log` passed. The report records `13,241` raw samples / `1,402,131` raw bytes, only `7` player-relevant samples / `736` bytes after filtering, `13,234` editor/tooling samples / `1,401,395` bytes, and `Runtime allocation probe assertion: Passed`.
- 2026-07-04: Re-ran battle-state Match GC capture after the AttackVfx request-resolution migration. The first battle retry failed because Unity AI/MCP relay logged a WebSocket connection error during play mode, so `MatchGcAllocationCallstackCapture` now ignores that editor-tooling connection error without suppressing project errors. The report classifier also treats `Burst.Compiler` and `Unity.Relay.Editor` hierarchy/callstack rows as editor/tooling. `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, and Unity 6.5.2 battle capture `/private/tmp/warline-arch-followup-gc-battle-tooling-filter.log` passed. `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture-battle.md` records `6,630` raw samples / `420,990` raw bytes, `5,691` player-relevant samples / `355,518` bytes, `939` editor/tooling samples / `65,472` bytes, and `Runtime allocation probe assertion: Passed`. `UnitAttackVfxRequestSystem` and `CombatGameObjectVfxPlaybackSystem` do not appear in the top rows; the next measured rows are selection/building/transport runtime lanes.
- 2026-07-04: Added an editor-only allocation probe to `SelectionGameplayStartupSystemHelper.UpdateSelectionRuntimePhases` and its command flush, input, focused read model, panel, tactical camera, marker preview, and camera subphases. The Unity 6.5.2 battle capture `/private/tmp/warline-arch-followup-selection-probe-battle.log` passed and showed `0` selection-probe bytes across `300` selection updates, so the remaining selection hierarchy rows are treated as profiler/report attribution until contradicted by a focused runtime probe. Removed the batchmode bypass from `BuildingPlacementRuntimeTickDiagnosticsSystemHelper.LogIfSlow`, so building-runtime slice diagnostics now require the existing diagnostic flag in automated captures. The follow-up battle capture `/private/tmp/warline-arch-followup-building-diag-gate-battle.log` passed, `BuildingRuntimeSliceDiag` / `LogStringToConsole` disappeared from searched/top rows, and the report records `7,720` raw samples / `529,549` raw bytes, `6,613` player-relevant samples / `445,585` bytes, `1,107` editor/tooling samples / `83,964` bytes, plus `Runtime allocation probe assertion: Passed`. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, and Unity 6.5.2 compile `/private/tmp/warline-arch-followup-selection-probe-compile.log` also passed.
- 2026-07-04: Added an editor-only allocation probe for `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance` and surfaced it in `MatchGcAllocationCallstackCapture` beside the existing UI shell, menu bootstrap, and selection probes. The player/runtime path now calls the core visual creation method directly; only `UNITY_EDITOR` builds wrap the probe and `System.GC.GetAllocatedBytesForCurrentThread()` reads. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, and `git diff --check` passed. Unity 6.5.2 battle capture first failed in sandbox because UPM could not create `/tmp/Unity-Upm-*.sock`; the escalated out-of-sandbox retry reached Unity startup but entered the known `LicenseClient-farhad` unsupported-protocol / missing-headless-package loop before project compile/capture started in `/private/tmp/warline-arch-followup-building-visual-probe-battle.log`, so Unity-side capture validation is blocked for this slice until a licensing-capable editor session is available.
- 2026-07-03: Added Fuel/Oil behavior safety-net tests before ECS ownership migration. `FactionResourceCompositionSystemHelperTests` now locks oil extraction clamps, refinery conversion clamps, full fuel storage no-conversion behavior, and destroyed-building skips. `ResourceHaulerUtilitySystemHelperTests` now locks fuel/oil cargo switching, blocked oil unload preservation, and matching-resource load reverts. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, Unity 6.5.2 focused validation `/private/tmp/warline-arch-followup-fuel-oil-faction-validation.log`, and Unity 6.5.2 focused validation `/private/tmp/warline-arch-followup-fuel-oil-hauler-validation.log` passed.
- 2026-07-03: Extracted oil extraction/refinery conversion math from `FactionResourceCompositionSystemHelper` into `BuildingResourceProductionSystemHelper`, a pure `Unity.Mathematics` system-helper surface suitable for reuse by a future Burst-capable ECS owner. The existing managed helper still owns applying results to runtime-building state, so no parallel gameplay owner was introduced. Unity import/compile `/private/tmp/warline-arch-followup-fuel-oil-systemhelper-import-compile.log`, `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, `/private/tmp/warline-arch-followup-fuel-oil-systemhelper-faction-validation.log`, and `/private/tmp/warline-arch-followup-fuel-oil-systemhelper-hauler-validation.log` passed.
- 2026-07-03: Wired the allocated runtime building id through `BuildingRuntimeCreationCompositionSystemHelper` into `BuildingRuntimeEntityCompositionSystemHelper.CreateBuildingCombatEntity`, so runtime-created building combat entities now populate `RuntimeBuildingCombatInfo.RuntimeBuildingId`. This keeps the next Fuel/Oil ECS ownership slice tied to existing runtime-building identity instead of creating a parallel owner. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, and Unity 6.5.2 batchmode compile `/private/tmp/warline-arch-followup-runtime-building-id-compile.log` passed with 0 compiler errors.
- 2026-07-03: Added `BuildingResourceStorageComponent` in the Components assembly and populated it on runtime-created resource building combat entities from definition metadata with zero stored barrels, matching existing runtime-building initial state. This is a mirror/prerequisite only; authoritative Fuel/Oil mutation still lives on the current managed runtime-building path until the ECS tick/apply handoff is validated. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, and Unity 6.5.2 batchmode compile `/private/tmp/warline-arch-followup-resource-storage-mirror-compile.log` passed.
- 2026-07-03: Added disabled `BuildingResourceProductionEcsSystem` as a Burst-capable `ISystem` tick/apply surface for resource storage, plus focused editor validation locking oil extraction clamp, refinery conversion clamp, and full-fuel no-conversion behavior through `BuildingResourceStorageComponent`. The system is `[DisableAutoCreation]` and sets `state.Enabled = false`, so this slice does not introduce a parallel production tick or gameplay owner. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, and escalated Unity 6.5.2 focused validation `/private/tmp/warline-arch-followup-resource-production-ecs-focused-escalated.log` passed. A sandboxed Unity attempt first hit the known licensing loop; the documented out-of-sandbox retry succeeded.
- 2026-07-03: Routed `FactionResourceCompositionSystemHelper.UpdateResourceProductionForBuilding` through `BuildingResourceProductionEcsSystem.ApplyTick` using a local `BuildingResourceStorageComponent`, then copied the result back to the existing runtime-building object. This preserves the single live runtime-building production owner and avoids a parallel ECS auto-tick while exercising the validated ECS tick surface from the current production path. Also repaired `GameplayRuntimeUpdateDebugFlags.cs.meta` with a `MonoImporter` block after Unity regenerated `Game.Runtime.csproj` without that tracked script. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, Unity 6.5.2 import/compile `/private/tmp/warline-arch-followup-debugflags-meta-compile.log`, `/private/tmp/warline-arch-followup-fuel-oil-ecs-handoff-faction-validation.log`, and `/private/tmp/warline-arch-followup-fuel-oil-ecs-handoff-ecs-validation.log` passed.
- 2026-07-03: Added disabled `BuildingResourceHaulerTransferEcsSystem` as a Burst-capable `ISystem` transfer surface over `BuildingResourceStorageComponent` and `UnitResourceHauler`, with focused editor validation for ECS load/unload calls. The system is `[DisableAutoCreation]` and disables itself on create, so this validates the ECS handoff surface without adding a parallel live hauler owner. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, and Unity 6.5.2 focused validation `/private/tmp/warline-arch-followup-hauler-transfer-ecs-validation.log` passed.
- 2026-07-04: Routed the live `ResourceHaulerUtilitySystemHelper` load/revert/unload mutation calls through `BuildingResourceHaulerTransferEcsSystem`, while keeping the existing runtime-building adapter/writeback path as the only live storage owner. This avoids parallel gameplay state and makes the production hauler path exercise the validated ECS transfer surface. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, and Unity 6.5.2 focused validation `/private/tmp/warline-arch-followup-hauler-live-ecs-transfer-validation.log` passed.
- 2026-07-04: Updated live hauler resource checks and mutations to prefer the runtime building combat entity `BuildingResourceStorageComponent` when present, commit load/revert/unload to that ECS component, and mirror the result back to the runtime-building object for compatibility. Runtime-building fallback remains for buildings without ECS storage. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, and Unity 6.5.2 focused validation `/private/tmp/warline-arch-followup-hauler-live-storage-prefer-ecs-validation.log` passed with `[ResourceHaulerFocusedValidation] result=Passed tests=18`.
- 2026-07-04: Updated live production/conversion ticks to prefer the runtime building combat entity `BuildingResourceStorageComponent` when present, commit extraction/conversion to that ECS component, and mirror the result back to the runtime-building object for compatibility. Runtime-building fallback remains for buildings without ECS storage. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, and Unity 6.5.2 focused validation `/private/tmp/warline-arch-followup-production-live-storage-prefer-ecs-validation.log` passed with `[BuildingResourceProductionEcsFocusedValidation] result=Passed tests=6`.
- 2026-07-04: Updated faction resource totals/economy/drain helper paths with ECS-backed runtime-building overloads, routed live faction summaries and resource sell/drain requests through the ECS-backed overloads, and kept runtime-building fallback/mirror compatibility. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, and Unity 6.5.2 focused validation `/private/tmp/warline-arch-followup-faction-resource-ecs-read-drain-validation.log` passed with `[FactionResourceFocusedValidation] result=Passed tests=12`.
- 2026-07-04: Updated selected-building resource storage display reads to prefer the selected runtime building combat entity `BuildingResourceStorageComponent` when present, with runtime-building fallback unchanged. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, and Unity 6.5.2 focused validation `/private/tmp/warline-arch-followup-selected-resource-display-ecs-validation.log` passed with `[FactionResourceFocusedValidation] result=Passed tests=13`.
- 2026-07-04: Audited selected unit/hauler cargo display. The Canvas selection panel reads `FocusedUnitUiReadModelComponent`, which is published from the live ECS `UnitResourceHauler` component via `SelectionUiReadModelLookup.TryGetFocusedUnitResourceCargoInfo`; added a focused regression that selected owned haulers show the `ResourceCargo` chip with separate oil/fuel values from ECS cargo. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, and Unity 6.5.2 focused validation `/private/tmp/warline-arch-followup-selection-hauler-cargo-validation.log` passed with `[SelectionSummaryFocusedValidation] result=Passed tests=12`.
- 2026-07-04: Faction-summary publish invalidation now prefers live ECS `BuildingResourceStorageComponent` values when hashing resource state, so ECS-only oil/fuel storage changes refresh the `BuildingRuntimeFactionSummary` buffer instead of waiting for runtime mirror sync. Added a focused regression in `BuildingProductionSystemTests` that mutates only ECS storage after the first publish and verifies the next faction-summary phase updates the summary buffer. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, and Unity 6.5.2 focused validation `/private/tmp/warline-arch-followup-faction-summary-signature-validation-windowed.log` passed with `[BuildingProductionRequestValidation] result=Passed tests=23`. A sandboxed headless Unity attempt first failed at UPM socket permissions, and the escalated headless retry entered the known 6.5 licensing/headless loop; the documented escalated non-headless batchmode workaround reached the tests and passed after fixing the editor-test setup.
- 2026-07-04: Hauler bridge loading source-storage reads now use `ResourceHaulerUtilitySystemHelper.GetStoredResource`, which prefers the runtime building combat entity `BuildingResourceStorageComponent` and falls back to runtime-building mirrors only when ECS storage is unavailable. Added `LiveEcsStorage_GetStoredResource_PrefersCombatEntityStorage` to lock stale runtime-mirror behavior. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, and `git diff --check` passed. Unity 6.5.2 focused validation attempt `/private/tmp/warline-arch-followup-hauler-stored-resource-ecs-validation.log` was blocked before project execution by licensing initialization failure and was stopped.
- 2026-07-04: Resource-building visual production flags now prefer live ECS `BuildingResourceStorageComponent` values when deciding whether oil pump/refinery animated parts are active, with runtime-building mirror fallback unchanged. Added `RuntimeResourceVisualsPreferEcsStorageForProductionState` to lock stale runtime-mirror behavior for presentation. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, and `git diff --check` passed. Unity 6.5.2 focused validation attempt `/private/tmp/warline-arch-followup-resource-visual-ecs-storage-validation.log` was blocked before project execution by licensing initialization failure and was stopped.
- 2026-07-04: Hauler fuel-source selection now uses a live ECS-storage overload of `ResourceHaulerUtilitySystemHelper.HasAvailableFuelForHauler`, so selected haulers choosing a storage destination do not treat a stale runtime fuel mirror as available fuel. Added `LiveEcsStorage_HasAvailableFuelForHauler_PrefersCombatEntityStorage` to lock ECS-preferred source availability. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, and `git diff --check` passed. Unity 6.5.2 focused validation attempt `/private/tmp/warline-arch-followup-hauler-fuel-source-ecs-validation.log` was blocked before project execution by licensing initialization failure and was stopped.

## Still Wrong / Next Iteration
- Known unresolved: visual/playmode smoke validation is pending, HDR/soft-shadow tier ownership is still unresolved, Android baseline is not captured, Fuel/Oil still keeps runtime-building storage mirrors for compatibility, and Phase 5 still has no truthful blind Burst candidates.
- Next iteration: either continue Fuel/Oil by removing one compatibility mirror dependency behind focused validation, or pause Fuel/Oil drift work and return to Android baseline capture.
