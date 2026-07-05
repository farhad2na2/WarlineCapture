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
| ECS `Object.Instantiate` | Audit counted 15 open | Refreshed Phase 6 scan separates 14 ECS entity-prefab instantiates from 27 GameObject `Object.Instantiate` presentation/probe/material clones. No current `Object.Instantiate` line owns gameplay entity creation. |
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
| Checklist complete | 107 / 135 |
| Checklist percent complete | 79.3% |
| Current phase | Phase 9 - TransportBoardingCommandSystem Decomposition |
| Quick wins complete | 6 / 6 |
| Current target | Phase 9 inventory slice documented the current `TransportBoardingCommandSystem` responsibilities, test coverage, extraction risks, and recommended decomposition order at `Design/AgentReports/2026-07-05_transport_boarding_command_system_phase9_inventory.md`. The first five low-risk extractions are complete: transport boarding diagnostic description/queue plumbing now lives in stateless `TransportBoardingDiagnosticSystemHelper`, passenger classification/capacity implementation now lives in stateless `TransportBoardingCapacitySystemHelper`, reusable approach/ring-cell search now lives in stateless `TransportBoardingApproachSystemHelper`, plane-ramp disembark/rollout cell search now lives in the same stateless approach helper, and command-routing predicates/result-buffer/result-element mapping now live in stateless `TransportBoardingCommandRoutingSystemHelper` behind stable command-system wrappers. The next low-risk non-device target is command request dispatch or boarding-order planning extraction; the next device-dependent target remains the controlled 10-minute Android foreground soak. |
| Compiler status | Latest Phase 9 command-routing helper extraction slice passed `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, and `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` with 0 errors. Unity 6.5.2 focused validation last passed through the fetched `Tools/CI/invoke_unity_macos.sh` licensing workaround on a temp project copy: `/private/tmp/warline-arch-followup-composition-helper-ledger-only-validation-post-second-rebase.log` records `[RuntimeCompositionHelperLedgerValidation] result=Passed tests=1`. |
| Android baseline status | Device profile selected: USB device `R4M7PZEQZ58T59ZH`, Xiaomi `24090RA29G`, codename `malachite`, MediaTek `MT6878`, Android `16`. Current-branch profiler APK built successfully at `Build/AndroidProfiler/WarlineCapture-Profiler.apk` (`471M` after the auto-start validation build), installed with `adb install -r`, launched with Unity extras `-warlineAutoStartMatch -warlineProfilerMarkers`, and reached Match automatically. Unity-only log artifact `/private/tmp/warline-arch-followup-autostart-android-unity.log` records `[UiShellRoute] submitted validation Match auto-start request`, deferred Match scene load/start requests, and live Match diagnostics with `playRequested=1 simulationActive=1`. Representative live steady samples: `[FrameRateDiag] fps `51.9..52.4`, avg frame `19.1..19.3 ms`, GPU `17.7..18.2 ms`, `drawCalls=77..78`, `batches=0`, `setPass=48..49`, `tris=765400..765402`, `verts=1479357..1479361`, `units=280`, `models=107`. First saved-profiler steady report `Design/AgentReports/2026-07-05_perf_WarlineCapture_current_branch_android_11-19-56_steady_summary.md` scans frames `300..1999`: avg `16.64 ms`, p95 `17.57 ms`, p99 `18.30 ms`, p95 CPU active `9.03 ms`, p95 GPU `5.74 ms`, and `84,780` GC bytes. Thermal snapshot after the first capture: thermal status `0`, current HAL CPU/GPU `37.455C`, battery `32.0C`, skin `34.126C`, cooling device values `0`. A later timer-based soak kept PID `4773` alive after 600 seconds but only retained about 8 minutes of Unity diagnostic rows in logcat; the controlled live-stream retry was invalid because the Android task was removed externally after about 1 minute. The full log `/private/tmp/warline-arch-followup-10min-stream-all.log` shows MIUI recents/task-removal paths (`ProcessSceneCleaner.handleSwipeKill`, `removeTask`) rather than a Unity fatal exception or native crash. The full 10-minute run remains open. |
| GC baseline status | Latest completed 2026-07-05 08:23:32 UTC battle-state Match GC capture: 300 requested frames after 180 warmup frames, 301 scanned profiler frames, 6,473 raw GC.Alloc samples / 407,277 raw bytes, 0 player-relevant samples / 0 player-relevant bytes after editor/tooling/diagnostic filtering, and 6,473 diagnostic samples / 407,277 diagnostic bytes. Runtime allocation probe assertion passed. `UIShellEcsPresentationSystem.Update`, `MenuBootstrapView.Update`, top-level gameplay runtime phases, selection subphases, `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance`, `BuildingProductionRuntimeTick.UpdateActiveProductionTransports`, and `TransportBoardingCommandSystem` all reported `0` direct bytes. The remaining raw selection rows are classified as probe-contradicted Mono JIT attribution until a direct runtime probe contradicts that. |
| Burst coverage status | Runtime inventory for Phase 5 scope: 124 `ISystem` files under `Systems`, `Rendering/Systems`, and `UI/Shell/Ecs`; 71 runtime files missing `[BurstCompile]`. Conservative classification complete: 0 immediately Burst-eligible, 24 managed edge, 8 presentation-only, 39 needs refactor. `EcsBurstHotPathArchitectureTests.NoBurstISystemFilesMustBeClassified` now guards against new unclassified no-Burst runtime `ISystem` files. |
| Mobile URP status | `Mobile_RPAsset` shadow distance changed from 240 m to 90 m and cascades from 4 to 2; HDR, MSAA 2x, render scale 0.8, and soft shadows remain unchanged pending visual/Android baseline |
| ECS instantiate status | Phase 6 classification report refreshed at `Design/AgentReports/2026-07-03_ecs-instantiate-ownership-classification.md`: 41 instantiate-like call lines across Systems/Rendering/Environment/UI ECS scan roots, split into 14 ECS `EntityManager`/`ECB.Instantiate` prefab spawns/projections that should remain ECS-owned and 27 GameObject `Object.Instantiate` presentation/probe/material clones. Current GameObject clone classification: 0 gameplay entity spawns, 21 visual/presentation spawns, 5 metadata/probe instantiates, and 1 environment material clone. First implementation slice already added a narrow presentation-edge pool for `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance`; ECS gameplay request/commit ownership is unchanged. |
| Fuel/Oil drift status | Drift reduced further: production/conversion math is no longer embedded in `FactionResourceCompositionSystemHelper`, runtime-created building combat entities carry their runtime building ids, resource buildings now have a per-building ECS storage mirror, a disabled Burst-capable ECS tick/apply surface is validated, managed production applies through that ECS surface, production plus hauler updates refresh the live ECS storage mirror for resource-capable buildings, hauler load/unload storage mutation routes through `BuildingResourceHaulerTransferEcsSystem`, both live production/conversion and live hauler mutation now prefer the live ECS storage component before mirroring back to runtime-building state, hauler bridge source-storage reads and fuel-source selection now prefer the live ECS storage component, resource visual production flags now prefer the live ECS storage component, faction summary/sell-drain paths now prefer live ECS storage too, selected-building resource storage display reads live ECS storage when available, selected unit/hauler cargo display is covered by a focused ECS `UnitResourceHauler` read-model regression, and faction-summary publish invalidation now hashes live ECS storage so ECS-only storage changes refresh the HUD/read-model buffer. Header/resource summary paths remain read-only display consumers. |
| Validation status | Phase 0 inventory completed; Phase 1 config edited; Phase 2 release OnGUI strip implemented; Phase 3 release diagnostic system gate implemented; Phase 4 GC quick wins completed; Phase 5 Burst inventory/classification report added and guardrail implemented; Phase 6 instantiate classification report added, first placement-visual pooling slice implemented, and remaining intentional ECS/entity vs GameObject presentation/probe instantiate calls refreshed; Phase 8 Fuel/Oil behavior safety-net tests added, production math extracted into a pure system-helper surface, ECS storage preference added across production, haulers, faction summaries, selection display, resource visuals, and publish invalidation, production/runtime helper profiler markers standardized, first AttackVfx request-processing slice completed with focused request-resolution tests, post-AttackVfx battle capture completed, selection attribution probe added, batchmode building-runtime diagnostic logging gated by the existing diagnostic flag, editor-only building visual allocation probe added, editor-only production transport/drop-visual allocation probe added, editor-only transport-boarding allocation probe added, diagnostic logging rows filtered out of player-relevant GC attribution, editor-only top-level gameplay runtime phase allocation probe added, top-level phase-probe battle capture rerun passed, RtsCamera query-cache hot-path migration passed focused camera validation plus battle GC recapture, probe-contradicted Mono JIT attribution rows are now excluded from player-relevant GC tables only when the direct selection probe records `0` bytes, and the runtime `CompositionSystemHelper` ledger guardrail is added to prevent new managed gameplay composition helper drift without explicit review. Phase 9 `TransportBoardingCommandSystem` inventory is documented; diagnostic formatting/queue plumbing, passenger classification/capacity implementation, reusable approach/ring-cell search, ramp disembark/rollout search, and command-routing result plumbing are extracted without behavior changes while public command-system wrappers remain stable. Fetched origin/main `0639e86d9` brought the Unity licensing workaround scripts; raw Unity licensing still fails, but `Tools/CI/invoke_unity_macos.sh` reaches project execution. The broad bootstrap guardrail run now exposes existing hierarchy lookup debt in `MatchBootstrapCompositionSystemHelper`, `StaticMapChunkBatchingPresentationSystemHelper`, and `MainMenuPlayUI`; the ledger guardrail was validated separately on a warmed temp copy because the main project was open in the editor. Android device selection passed through `adb devices -l` plus `adb shell getprop`, the current-branch Android profiler APK build passed, the first current-branch Android profiler capture/export passed, Android-vs-reference comparison is recorded, saved-capture render-counter export reruns passed, live runtime render-counter recorder resolution now falls back through Unity's available recorder list when exact names are invalid, the fresh APK rebuild/install for that diagnostics change passed, the validation-only Match auto-start hook routes through existing ECS shell/match-start queues, and the fresh Android APK run proved live Match diagnostics with non-invalid draw-call evidence. The attempted 10-minute soak on 2026-07-05 is intentionally not counted as passed: one timer run lacked a full retained stream artifact, and the live-stream retry was externally task-removed before completion. Latest validations passed: source guard string checks for Board preview/Board All plus command-routing/approach/ramp wrappers, `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, and `git diff --check`. |
| Still wrong / next iteration | No visual defects are known from code/compile validation, but manual visual smoke checks for placement-preview pooling, command behavior, transport delivery visuals, and combat VFX timing are still pending. Fuel/Oil still keeps runtime-building storage mirrors for compatibility. Editor battle GC has no player-relevant rows after direct-probe filtering. The first current-branch Android saved-profiler baseline is only `2,000` profiler frames, not a full 10-minute/thermal-soak run. Live render diagnostics now expose draw calls and the other scene counters, but `batches=0` on this Unity/player path; treat batches as captured but not useful for decisions unless a later Unity profiler/UI source exposes a nonzero batch counter. Phase 7 still needs a controlled 10-minute thermal-soak capture with the device left foregrounded and the live log stream running through the full window before HDR/soft-shadow tier ownership can be resolved. Phase 9 still needs behavior-preserving extraction slices; the next low-risk slice is command request dispatch or boarding-order planning extraction. |

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
- ECS instantiate inventory: original audit found `15` runtime `Object.Instantiate` call lines under `Assets/Game/Scripts/Systems`; refreshed Phase 6 scan across Systems/Rendering/Environment/UI ECS roots finds 14 ECS entity-prefab instantiates plus 27 GameObject `Object.Instantiate` presentation/probe/material clone lines, with 0 GameObject gameplay entity spawns.
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
- [x] Update tracker with remaining intentional non-runtime or editor-only instantiates.

## Phase 7 - Android Ground Truth Baseline
Required before deeper tuning decisions.

- [x] Select one mid-tier Android target device profile.
- [x] Build IL2CPP Android with the mobile render config.
- [ ] Run a 10-minute match/session capture.
- [x] Record p50, p95, and worst-frame CPU/GPU frame times.
- [x] Record thermal state and throttling symptoms.
- [x] Record draw calls, batches, triangles, and steady-state GC.
- [x] Compare Android results against editor assumptions and update priorities.
- [x] Add deterministic validation-only Match auto-start entry path for Android diagnostics.

### Phase 7 Device Profile

- Selected device: USB serial `R4M7PZEQZ58T59ZH`, Xiaomi `24090RA29G`.
- Device codename: `malachite`.
- SoC/platform: MediaTek `MT6878`.
- OS: Android `16` from `ro.build.version.release`.
- ADB note: device inspection requires running ADB outside the Codex sandbox because the local smartsocket listener cannot bind inside the sandbox.
- Reference-only prior capture: `Design/AgentReports/2026-07-04_perf_WarlineCapture_12-15-11_steady_state_summary.md` captured `/Users/farhad/Projects/WarlineCapture-Clone/ProfilerCaptures/WarlineCapture_2026-07-04_12-15-11.data`, with avg `24.71 ms`, p50 `22.43 ms`, p95 `39.06 ms`, p99 `50.24 ms`, max `68.27 ms`, and `3,523,148` total GC bytes over `1,700` scanned frames. Use it as context only; Phase 7 still needs a current-branch build/capture artifact.
- Current-branch profiler APK build: `Build/AndroidProfiler/WarlineCapture-Profiler.apk`, `405M` on disk, Unity-reported size `3736467585` bytes, `28` warnings, build log `/private/tmp/warline-arch-followup-android-profiler-apk-build.log`.

### Phase 7 Current-Branch Android First Baseline

- Install: `adb install -r Build/AndroidProfiler/WarlineCapture-Profiler.apk` completed with `Success`.
- Launch: package `com.warlinecapture.game`, activity `com.unity3d.player.UnityPlayerGameActivity`, Unity profiler args `-profiler-enable -profiler-log-file /sdcard/Android/data/com.warlinecapture.game/files/WarlineCapture_2026-07-05_11-19-56.data -profiler-capture-frame-count 2000`.
- Device capture artifact: `/sdcard/Android/data/com.warlinecapture.game/files/WarlineCapture_2026-07-05_11-19-56.data.raw`.
- Local raw capture: `ProfilerCaptures/WarlineCapture_2026-07-05_11-19-56.data.raw`, `341,346,417` bytes, intentionally ignored from git.
- Full report: `Design/AgentReports/2026-07-05_perf_WarlineCapture_current_branch_android_11-19-56_summary.md`, frames `1..2000`, avg `16.78 ms`, p50 `16.64 ms`, p95 `17.52 ms`, p99 `18.27 ms`, max `282.20 ms` from startup frame `1`, p95 CPU active `6.78 ms`, p95 GPU `5.53 ms`, total GC `12,853,044` bytes.
- Steady report: `Design/AgentReports/2026-07-05_perf_WarlineCapture_current_branch_android_11-19-56_steady_summary.md`, frames `300..1999`, avg `16.64 ms`, p50 `16.64 ms`, p95 `17.57 ms`, p99 `18.30 ms`, max `20.89 ms`, p95 CPU active `9.03 ms`, p95 GPU `5.74 ms`, total GC `84,780` bytes.
- Thermal/battery snapshot after the capture: battery level `100%`, USB powered, battery temperature `32.0C`; Android thermal status `0`; current HAL temperatures CPU/GPU `37.455C`, skin `34.126C`, battery `32.0C`; cooling devices reported `0`, so no throttling symptom was visible in the sampled state.
- Limitation: this is a first current-branch `2,000`-frame profiler baseline. It does not replace the pending 10-minute match/session capture or a longer thermal-soak validation.

### Phase 7 Android 10-Minute Soak Attempt Notes

- 2026-07-05 timer attempt: launched the auto-start profiler APK with `-warlineAutoStartMatch -warlineProfilerMarkers`, confirmed early Match diagnostics with `playRequested=1 simulationActive=1`, then waited for a 600-second timer. `adb shell pidof com.warlinecapture.game` returned PID `4773` after the timer, but the post-run logcat dump retained only about 8 minutes of steady `[FrameRateDiag]` rows (`07-05 14:15:14.962` through `14:23:37.565`, 251 frame rows, 50 render rows), so this is not a complete 10-minute diagnostic artifact.
- 2026-07-05 live-stream retry: cleared logcat, captured pre-run thermal/battery snapshots, then streamed `adb logcat -v time -s Unity` to `/private/tmp/warline-arch-followup-10min-steady-stream-unity.log`. The stream captured only 31 steady `[FrameRateDiag]` rows from `14:25:48` through `14:26:48` before the Android game task was no longer running. The full device log `/private/tmp/warline-arch-followup-10min-stream-all.log` shows MIUI task-removal/recents paths around `14:29:00` (`ProcessSceneCleaner.handleSwipeKill`, `removeTask`) and no captured Unity `FATAL EXCEPTION`, `SIGSEGV`, `SIGABRT`, or native crash marker.
- Retry-stream thermal state moved from current HAL CPU/GPU `61.292C`, battery `41.0C`, skin `49.399C` before the stream to CPU/GPU `42.544C`, battery `37.0C`, skin `38.599C` afterward; cooling devices remained `0`.
- Result: invalid for the Phase 7 10-minute gate. Keep `Run a 10-minute match/session capture` open until a controlled foregrounded run completes with a live log stream covering the full window.

### Phase 7 Android Baseline Comparison

Compared against reference-only `Design/AgentReports/2026-07-04_perf_WarlineCapture_12-15-11_steady_state_summary.md` from `WarlineCapture-Clone`:

| Metric | Reference-only steady | Current-branch steady | Direction |
|---|---:|---:|---:|
| Avg frame | `24.71 ms` | `16.64 ms` | `32.7%` lower |
| P50 frame | `22.43 ms` | `16.64 ms` | `25.8%` lower |
| P95 frame | `39.06 ms` | `17.57 ms` | `55.0%` lower |
| P99 frame | `50.24 ms` | `18.30 ms` | `63.6%` lower |
| Max steady frame | `68.27 ms` | `20.89 ms` | `69.4%` lower |
| Frames over budget | `1634/1700` | `797/1700` | much lower, but still often just over 16.667 ms |
| Avg CPU active | `17.61 ms` | `8.00 ms` | `54.6%` lower |
| P95 CPU active | `18.14 ms` | `9.03 ms` | `50.2%` lower |
| Avg GPU time | `20.41 ms` | `5.31 ms` | `74.0%` lower |
| Total GC allocated | `3,523,148 bytes` | `84,780 bytes` | `97.6%` lower |

- The current branch is close to a 60 FPS/v-sync-limited profile in the captured steady window. High `WaitForTargetFPS` and render-thread wait markers are expected in this context and should not be treated as gameplay hot paths.
- The prior reference capture had much heavier simulation, presentation, render, and GC costs. Current branch no longer justifies broad helper migration based on that stale reference capture.
- Remaining decision: collect longer thermal/draw-call evidence before changing HDR, soft shadows, MSAA, or render-scale ownership. The current numbers do not force an immediate quality downgrade.

### Phase 7 Saved-Capture Render Counters

- Exporter update: `Assets/Game/Scripts/Editor/ProfilerCaptureSummaryExporter.cs` now reads `Draw Calls Count`, `Batches Count`, `SetPass Calls Count`, `Triangles Count`, and `Vertices Count` from `RawFrameDataView` and adds a `Render Counters` table to generated reports.
- Full report rerun: `/private/tmp/warline-arch-followup-current-android-profiler-export-rerun.log` regenerated `Design/AgentReports/2026-07-05_perf_WarlineCapture_current_branch_android_11-19-56_summary.md`.
- Steady report rerun: `/private/tmp/warline-arch-followup-current-android-profiler-steady-export-rerun.log` regenerated `Design/AgentReports/2026-07-05_perf_WarlineCapture_current_branch_android_11-19-56_steady_summary.md`.
- Steady saved-capture counters: draw calls avg/p95/max `0/0/0`, batches `0/0/0`, SetPass calls `12/13/13`, triangles `983/985/985`, vertices `1967/1971/1971`.
- Interpretation: SetPass, triangle, and vertex counters are available in the saved profiler capture, but draw calls and batches are not surfaced by these counter names in the saved Android capture. Keep the Phase 7 render-counter checklist item open until draw calls and batches are captured by a live `ProfilerRecorder`/diagnostic log path, Unity profiler UI export, or another reliable counter source.

### Phase 7 Live Render Counter Diagnostics

- Runtime diagnostics owner: `Assets/Game/Scripts/Systems/PerformanceDiagnosticsSystemHelper.cs`.
- Existing architecture path preserved: the frame-rate/slow-frame/render-scene diagnostics continue to emit through the existing runtime diagnostics helper; no new gameplay owner, `Boundary`, `Presenter`, UI Toolkit, or MonoBehaviour polling path was added.
- Live recorder hardening: render counters now try exact Unity counter names first, then enumerate Unity's available `ProfilerRecorderHandle` list for render-category aliases. This handles Unity-version/platform counter-name drift before live logs degrade to invalid `-1` values.
- Control observation: the already-installed pre-hardening APK emitted live match diagnostics such as `drawCalls=-1 batches=-1 setPass=47 tris=759431 verts=1462734`, confirming the old live path could see set-pass/triangle/vertex counters but not draw calls/batches.
- Fresh APK validation: Unity 6.5.2 built `Build/AndroidProfiler/WarlineCapture-Profiler.apk` from the hardened code in `/private/tmp/warline-arch-followup-live-render-counter-apk-build.log` (`Build Finished, Result: Success`, `26` warnings, 0 errors) and `adb install -r Build/AndroidProfiler/WarlineCapture-Profiler.apk` returned `Success`.
- Current limitation: the fresh APK clean launch reached the startup/menu path and did not emit match `[FrameRateDiag]` / `[RenderSceneDiag]` samples, so this slice makes the live diagnostic capture path more reliable but does not itself satisfy Phase 7's draw-call/batch evidence requirement. The open validation remains a live Android log/sample showing non-invalid draw-call and batch values during a match.
- Validation: `git diff --check`, `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, Unity 6.5.2 Android profiler APK build `/private/tmp/warline-arch-followup-live-render-counter-apk-build.log`, and `adb install -r Build/AndroidProfiler/WarlineCapture-Profiler.apk` passed with 0 errors.

### Phase 7 Deterministic Match Auto-start

- Runtime owner: `Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs`.
- Entry controls: command-line arg `-warlineAutoStartMatch` or environment variable `WARLINE_AUTO_START_MATCH=1`.
- Architecture path preserved: the hook submits one ECS `UiShellRouteRequestComponent` with `UiShellRouteIntent.EnterMatch` / `UIRoute.Match`, so the existing shell loading, scene lifecycle, and `MatchStartSceneSystemHelper` queues still own the transition and gameplay start.
- Scope guard: this is validation-only, one-shot, and inactive in normal launches unless the explicit arg/env flag is present. It does not add UI Toolkit, a new `Boundary`/`Presenter`, a runtime hierarchy lookup, a MonoBehaviour gameplay loop, or a parallel scene-start implementation.
- Validation passed: `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly` and `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`.
- Android validation passed: Unity 6.5.2 built `Build/AndroidProfiler/WarlineCapture-Profiler.apk` in `/private/tmp/warline-arch-followup-autostart-apk-build.log`, `adb install -r Build/AndroidProfiler/WarlineCapture-Profiler.apk` returned `Success`, and `adb shell "am start -n com.warlinecapture.game/com.unity3d.player.UnityPlayerGameActivity --es unity '-warlineAutoStartMatch -warlineProfilerMarkers'"` launched the app with the validation flags.
- Evidence: `/private/tmp/warline-arch-followup-autostart-android-unity.log` records `[UiShellRoute] submitted validation Match auto-start request`, deferred Match scene load/start requests, and Match runtime `[FrameRateDiag]` / `[RenderSceneDiag]` samples. Representative samples show `playRequested=1 simulationActive=1`, `drawCalls=77..78`, `batches=0`, `setPass=48..49`, `tris=765400..765402`, `verts=1479357..1479361`, `units=280`, and `models=107`.

## Phase 8 - Managed Helper Hot-path Migration
Measured architecture work. Do not boil the 309-helper ocean.

- [x] Freeze new managed-helper gameplay ownership in review/architecture scripts.
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
- [x] Add an editor-only production transport/drop-visual allocation probe before migrating production transport visuals.
- [x] Add an editor-only transport boarding allocation probe before starting `TransportBoardingCommandSystem` decomposition.
- [x] Filter diagnostic `PerformanceDiagnosticsSystemHelper.LogNoStackTrace` rows out of player-relevant GC attribution before choosing the next hot-path migration.
- [x] Add an editor-only top-level gameplay runtime phase allocation probe before changing selection/building-placement behavior.
- [x] Rerun battle GC capture with the top-level gameplay runtime phase allocation probe once Unity licensing is available.
- [x] Cache `RtsCameraRequestSystem` hot-path queries after battle GC identified `TryGetGridConfig` as the next measured allocation source.
- [x] Filter probe-contradicted Mono JIT selection-runtime attribution rows out of player-relevant GC tables only when direct selection runtime probes record `0` bytes.
- [ ] Migrate BuildingPlacement/transports only after current allocation and frame-time data confirms priority.
- [ ] Migrate Selection updates after Selection allocation/frame-time data is captured.
- [ ] Keep Canvas/MonoBehaviour code as serialized-reference visual binders.
- [ ] Add focused tests for each migrated hot path.
- [x] Re-run GC and frame-time capture after every hot-path migration.

## Phase 9 - TransportBoardingCommandSystem Decomposition
Structural work after quick wins and measured baselines.

- [x] Inventory current internal phases of `TransportBoardingCommandSystem`.
- [x] Extract diagnostic formatting and diagnostic queue plumbing into stateless `TransportBoardingDiagnosticSystemHelper`.
- [x] Extract passenger classification and transport capacity implementation into stateless `TransportBoardingCapacitySystemHelper` behind existing public wrappers.
- [x] Extract reusable approach and ring-cell search implementation into stateless `TransportBoardingApproachSystemHelper` behind existing public wrappers.
- [x] Extract plane-ramp disembark and rollout cell search implementation into stateless `TransportBoardingApproachSystemHelper`.
- [x] Extract command-routing predicates and command-result mapping into stateless `TransportBoardingCommandRoutingSystemHelper` behind existing public wrappers.
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
- 2026-07-05: Refreshed the Phase 6 instantiate ownership report after the Android soak gate became foreground-device blocked. The scan now explicitly separates 14 ECS `EntityManager`/`ECB.Instantiate` prefab spawns/projections from 27 GameObject `Object.Instantiate` presentation/probe/material clones across Systems/Rendering/Environment/UI ECS roots. Current GameObject clone classification remains 0 gameplay entity spawns, 21 visual/presentation spawns, 5 metadata/probe instantiates, and 1 environment material clone. This closes the tracker documentation item for remaining intentional instantiates without changing runtime behavior.
- 2026-07-03: Completed the attack-target cached-query cleanup by adding query-based `RtsSelectionAttackTargetModeCommandSystem` entry points and routing live selection command flush/startup checks through the cached command queue, runtime state, and selected-tag queries. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, Unity 6.5.2 compile log `/private/tmp/warline-arch-followup-attack-target-cache-compile.log`, and Match GC capture log `/private/tmp/warline-arch-followup-gc-attack-target-cache.log` passed. The GC report no longer includes `RtsSelectionAttackTargetModeCommandSystem.ProcessPendingRequests` in searched/top rows. The latest player-relevant total is `20,796` samples / `1,642,133` bytes, with current recurring rows grouped under `SelectionGameplayStartupSystemHelper.UpdateSelectionRuntimePhases`.
- 2026-07-03: Cached the building runtime boundary entity inside `BuildingRuntimeProcessingCompositionSystemHelper` so the simulation tick reuses a validated world/entity instead of asking the singleton query for the same boundary every frame. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, Unity 6.5.2 compile log `/private/tmp/warline-arch-followup-building-boundary-cache-compile.log`, and Match GC capture log `/private/tmp/warline-arch-followup-gc-building-boundary-cache.log` passed. The report records `12,779` raw samples / `903,267` raw bytes, only `3` player-relevant samples / `272` bytes after filtering, and `Runtime allocation probe assertion: Passed`; raw rows are dominated by `System.Text.StringBuilder` stacks containing `Unity.AI.MCP.Editor.Bridge`, so they are treated as capture-tooling contamination until the capture path isolates MCP traffic.
- 2026-07-03: Split the GC capture report tables into player-relevant, editor/tooling, and raw allocation rows so Unity AI/MCP/Tracing logging remains visible without being mistaken for gameplay cleanup priority. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, Unity 6.5.2 compile log `/private/tmp/warline-arch-followup-gc-report-tooling-table-compile.log`, and Match GC capture log `/private/tmp/warline-arch-followup-gc-report-tooling-table.log` passed. The report records `13,241` raw samples / `1,402,131` raw bytes, only `7` player-relevant samples / `736` bytes after filtering, `13,234` editor/tooling samples / `1,401,395` bytes, and `Runtime allocation probe assertion: Passed`.
- 2026-07-04: Re-ran battle-state Match GC capture after the AttackVfx request-resolution migration. The first battle retry failed because Unity AI/MCP relay logged a WebSocket connection error during play mode, so `MatchGcAllocationCallstackCapture` now ignores that editor-tooling connection error without suppressing project errors. The report classifier also treats `Burst.Compiler` and `Unity.Relay.Editor` hierarchy/callstack rows as editor/tooling. `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, and Unity 6.5.2 battle capture `/private/tmp/warline-arch-followup-gc-battle-tooling-filter.log` passed. `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture-battle.md` records `6,630` raw samples / `420,990` raw bytes, `5,691` player-relevant samples / `355,518` bytes, `939` editor/tooling samples / `65,472` bytes, and `Runtime allocation probe assertion: Passed`. `UnitAttackVfxRequestSystem` and `CombatGameObjectVfxPlaybackSystem` do not appear in the top rows; the next measured rows are selection/building/transport runtime lanes.
- 2026-07-04: Added an editor-only allocation probe to `SelectionGameplayStartupSystemHelper.UpdateSelectionRuntimePhases` and its command flush, input, focused read model, panel, tactical camera, marker preview, and camera subphases. The Unity 6.5.2 battle capture `/private/tmp/warline-arch-followup-selection-probe-battle.log` passed and showed `0` selection-probe bytes across `300` selection updates, so the remaining selection hierarchy rows are treated as profiler/report attribution until contradicted by a focused runtime probe. Removed the batchmode bypass from `BuildingPlacementRuntimeTickDiagnosticsSystemHelper.LogIfSlow`, so building-runtime slice diagnostics now require the existing diagnostic flag in automated captures. The follow-up battle capture `/private/tmp/warline-arch-followup-building-diag-gate-battle.log` passed, `BuildingRuntimeSliceDiag` / `LogStringToConsole` disappeared from searched/top rows, and the report records `7,720` raw samples / `529,549` raw bytes, `6,613` player-relevant samples / `445,585` bytes, `1,107` editor/tooling samples / `83,964` bytes, plus `Runtime allocation probe assertion: Passed`. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, and Unity 6.5.2 compile `/private/tmp/warline-arch-followup-selection-probe-compile.log` also passed.
- 2026-07-04: Added an editor-only allocation probe for `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance` and surfaced it in `MatchGcAllocationCallstackCapture` beside the existing UI shell, menu bootstrap, and selection probes. The player/runtime path now calls the core visual creation method directly; only `UNITY_EDITOR` builds wrap the probe and `System.GC.GetAllocatedBytesForCurrentThread()` reads. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, and `git diff --check` passed. Unity 6.5.2 battle capture first failed in sandbox because UPM could not create `/tmp/Unity-Upm-*.sock`, and the escalated `-nographics` retry hit the known `LicenseClient-farhad` unsupported-protocol / missing-headless-package loop. The documented non-headless batchmode workaround then passed in `/private/tmp/warline-arch-followup-building-visual-probe-windowed-battle.log` and updated `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture-battle.md`: `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance` reported `0` bytes across `0` create calls, so visual building instantiation is not recurring in this measured battle window.
- 2026-07-04: Added an editor-only allocation probe for `BuildingProductionRuntimeTick.UpdateActiveProductionTransports`, production transport instance acquire/create, and transport drop-visual acquire/create. This keeps the probe diagnostic-only under `UNITY_EDITOR`; player builds do not call `System.GC.GetAllocatedBytesForCurrentThread()`. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, and Unity 6.5.2 battle capture `/private/tmp/warline-arch-followup-production-transport-dropvisual-probe-windowed-battle.log` passed. The updated report records `0` probe bytes across `124` active transport updates, `0` transport acquire calls, and one drop-visual create/acquire call with `0` thread-allocation bytes, so production transport visual creation is not the current recurring allocation target; `TransportBoardingCommandSystem` is the next measured recurring row to classify.
- 2026-07-04: Added an editor-only allocation probe for `TransportBoardingCommandSystem.OnUpdate` and its public command-intent processing entry point before starting any Phase 9 decomposition. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, and Unity 6.5.2 battle capture `/private/tmp/warline-arch-followup-transport-boarding-probe-windowed-battle.log` passed. The report records `0` probe bytes across `300` boarding-system updates, `0` handled updates, and `0` command calls; the apparent transport-boarding GC row is `PerformanceDiagnosticsSystemHelper.LogNoStackTrace` frame-rate diagnostic logging attributed through Unity hierarchy paths, so it is not a boarding gameplay allocation target.
- 2026-07-04: Filtered diagnostic `PerformanceDiagnosticsSystemHelper.LogNoStackTrace` rows out of player-relevant Match GC attribution while keeping them visible in the editor/tooling/diagnostic table. `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `git diff --check`, and Unity 6.5.2 battle capture `/private/tmp/warline-arch-followup-diagnostic-log-filter-windowed-battle.log` passed. The report records `6,460` raw samples / `397,921` raw bytes, `5,521` player-relevant samples / `332,449` bytes, `939` editor/tooling/diagnostic samples / `65,472` bytes, and `Runtime allocation probe assertion: Passed`; the remaining measured target is `SelectionGameplayStartupSystemHelper.UpdateSelectionRuntimePhases` allocation attribution around line 423.
- 2026-07-04: Added an editor-only top-level gameplay runtime allocation probe around `GameplayRuntimeUpdateCompositionSystemHelper` phase markers for RuntimeCity, RuntimeGridBlockers, RuntimeDecorations, RoadBuild, BuildingPlacement, Selection, DayNight, CitizenPopulation, MainMenu, LoadingGate, and EndUpdate. This is diagnostic-only under `UNITY_EDITOR` and does not add a gameplay owner. `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, and `git diff --check` passed. Unity 6.5.2 battle capture `/private/tmp/warline-arch-followup-gameplay-runtime-phase-probe-windowed-battle.log` was blocked before project execution by the known licensing/headless loop (`Unsupported protocol version '1.18.1'`, missing `com.unity.editor.headless`) and was stopped, so the fresh report data for this probe is still pending.
- 2026-07-05: Re-ran Unity 6.5.2 battle-state Match GC capture with the top-level gameplay runtime allocation probe after licensing became available. `/private/tmp/warline-arch-followup-gameplay-runtime-phase-probe-retry-windowed-battle.log` passed and updated `Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture-battle.md`. The report records `7,756` raw samples / `502,467` raw bytes, `6,805` player-relevant samples / `435,981` bytes, `951` editor/tooling/diagnostic samples / `66,486` bytes, and `Runtime allocation probe assertion: Passed`. Top-level gameplay runtime phases and selection subphases all reported `0` direct bytes, so the next measured migration target is the recurring `RtsCameraRequestSystem.TryGetGridConfig` allocation path. Follow-up validation passed: `git diff --check`, `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, and `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`.
- 2026-07-05: Cached `RtsCameraRequestSystem` camera queue, grid config, and tactical-follow pose queries on the existing system instance, removing the repeated hot-path `EntityManager.CreateEntityQuery(...)` calls without changing camera request ownership or behavior. Validation passed: `git diff --check`, `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, Unity 6.5.2 focused camera validation `/private/tmp/warline-arch-followup-rts-camera-query-cache-validation.log` with `[RtsCameraFocusedValidation] result=Passed tests=29`, and battle GC capture `/private/tmp/warline-arch-followup-rts-camera-query-cache-battle.log`. The battle report records `4,539` raw/player-relevant samples / `229,108` raw/player-relevant bytes, `0` editor/tooling/diagnostic samples, and no remaining `RtsCameraRequestSystem.TryGetGridConfig` searched/top rows.
- 2026-07-05: Classified probe-contradicted Mono JIT selection-runtime attribution rows out of player-relevant GC tables only when the direct selection runtime allocation probe reports `0` bytes, keeping those raw rows visible under editor/tooling/diagnostic attribution. This avoids migrating gameplay code against profiler attribution contradicted by direct probes. Validation passed: `git diff --check`, `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, and Unity 6.5.2 battle capture `/private/tmp/warline-arch-followup-probe-contradicted-jit-filter-battle.log`. The battle report records `6,473` raw samples / `407,277` raw bytes, `0` player-relevant samples / `0` player-relevant bytes, `6,473` editor/tooling/diagnostic samples / `407,277` diagnostic bytes, and `Runtime allocation probe assertion: Passed`.
- 2026-07-05: Started Phase 7 Android ground-truth baseline by selecting the connected target profile. `adb devices -l` found `R4M7PZEQZ58T59ZH` (`24090RA29G`, `malachite`), and `adb shell getprop` confirmed Xiaomi / MediaTek `MT6878` / Android `16`. Existing on-device report `Design/AgentReports/2026-07-04_perf_WarlineCapture_12-15-11_steady_state_summary.md` is reference-only because it was captured from `WarlineCapture-Clone`; current-branch profiler APK build/install/capture remains pending.
- 2026-07-05: Built the current-branch IL2CPP ARM64 Android development profiler APK with Unity `6000.5.2f1`. Build log `/private/tmp/warline-arch-followup-android-profiler-apk-build.log` reports `Build Finished, Result: Success` and `Build succeeded. target=Android output=/Users/farhad/Projects/WarlineCapture/Build/AndroidProfiler/WarlineCapture-Profiler.apk size=3736467585 warnings=28`. The resulting APK exists at `Build/AndroidProfiler/WarlineCapture-Profiler.apk` and is `405M` on disk. Noted non-blocking warnings include Mobile Renderer Forward+ compatibility guidance, missing `metal`/`metal-objdump` tool messages during shader work, and deprecated Android legacy icon warnings.
- 2026-07-05: Installed the current-branch profiler APK on `R4M7PZEQZ58T59ZH`, launched `com.warlinecapture.game/com.unity3d.player.UnityPlayerGameActivity` with Unity profiler logging enabled, stopped after the requested `2,000` profiler-frame window, pulled `ProfilerCaptures/WarlineCapture_2026-07-05_11-19-56.data.raw`, and exported full plus steady reports. Full report `Design/AgentReports/2026-07-05_perf_WarlineCapture_current_branch_android_11-19-56_summary.md` scans frames `1..2000` with avg `16.78 ms`, p50 `16.64 ms`, p95 `17.52 ms`, p99 `18.27 ms`, max `282.20 ms` on startup frame `1`, p95 CPU active `6.78 ms`, p95 GPU `5.53 ms`, and `12,853,044` GC bytes. Steady report `Design/AgentReports/2026-07-05_perf_WarlineCapture_current_branch_android_11-19-56_steady_summary.md` scans frames `300..1999` with avg `16.64 ms`, p50 `16.64 ms`, p95 `17.57 ms`, p99 `18.30 ms`, max `20.89 ms`, p95 CPU active `9.03 ms`, p95 GPU `5.74 ms`, and `84,780` GC bytes. Filtered `adb shell dumpsys battery` and `adb shell dumpsys thermalservice` snapshots after capture recorded battery `100%`, battery temperature `32.0C`, thermal status `0`, current HAL CPU/GPU `37.455C`, skin `34.126C`, and no active cooling-device throttling value.
- 2026-07-05: Compared the current-branch Android steady report against the reference-only `WarlineCapture-Clone` steady report. Current branch improved avg frame time by `32.7%`, p50 by `25.8%`, p95 by `55.0%`, p99 by `63.6%`, max steady frame by `69.4%`, avg CPU active by `54.6%`, p95 CPU active by `50.2%`, avg GPU time by `74.0%`, and total steady GC by `97.6%`. Frames over budget dropped from `1634/1700` to `797/1700`, and the remaining frame time is close enough to the 60 FPS boundary that a longer thermal/draw-call capture should precede more render-quality cuts.
- 2026-07-05: Extended the editor-only profiler summary exporter to include saved-capture render counters and regenerated the current Android full/steady reports. Validation passed: `git diff --check`, `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `/private/tmp/warline-arch-followup-current-android-profiler-export-rerun.log`, and `/private/tmp/warline-arch-followup-current-android-profiler-steady-export-rerun.log`. The steady saved-capture counter table records SetPass avg/p95/max `12/13/13`, triangles `983/985/985`, vertices `1967/1971/1971`, but draw calls and batches are `0`, so Phase 7 still needs a reliable draw-call/batch capture source.
- 2026-07-05: Hardened the existing `PerformanceDiagnosticsSystemHelper` live render-counter recorder setup so draw calls, batches, SetPass calls, triangles, and vertices try exact counter names first and then fall back through Unity's available render-category `ProfilerRecorderHandle` list. This keeps the existing diagnostics owner and avoids a new gameplay/UI polling path. Validation passed: `git diff --check`, `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, and `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`. Follow-up Android validation built and installed a fresh profiler APK from the hardened code (`/private/tmp/warline-arch-followup-live-render-counter-apk-build.log`, `adb install -r Build/AndroidProfiler/WarlineCapture-Profiler.apk`), but the clean reinstall launched into startup/menu and did not emit match diagnostics. The stale pre-hardening APK control run emitted live match diagnostics with `drawCalls=-1` / `batches=-1` and valid SetPass/triangle/vertex values, so Phase 7 still needs a deterministic match-start entry path before the live draw-call/batch evidence can be closed.
- 2026-07-05: Added and validated a deterministic Match auto-start path for Android diagnostics. `MenuBootstrapCompositionSystemHelper` now accepts `-warlineAutoStartMatch` or `WARLINE_AUTO_START_MATCH=1`, then submits a one-shot ECS `UiShellRouteRequestComponent` using the existing `EnterMatch` route. No new gameplay owner, UI Toolkit, `Boundary`/`Presenter`, hierarchy lookup, or MonoBehaviour gameplay loop was added. Validation passed: `git diff --check`, `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, Unity 6.5.2 Android profiler APK build `/private/tmp/warline-arch-followup-autostart-apk-build.log`, `adb install -r Build/AndroidProfiler/WarlineCapture-Profiler.apk`, and Android Unity log validation `/private/tmp/warline-arch-followup-autostart-android-unity.log`. The log records `[UiShellRoute] submitted validation Match auto-start request`, deferred Match scene load/start requests, and live Match `[FrameRateDiag]` / `[RenderSceneDiag]` samples with `playRequested=1 simulationActive=1`, `drawCalls=77..78`, `batches=0`, `setPass=48..49`, `tris=765400..765402`, and `verts=1479357..1479361`.
- 2026-07-05: Attempted the longer Android 10-minute soak twice and intentionally left the Phase 7 gate open. The first timer run reached Match and remained alive after 600 seconds, but Android logcat retained only about 8 minutes of steady diagnostics in `/private/tmp/warline-arch-followup-10min-soak-unity.log`. The second run used a live Unity log stream to `/private/tmp/warline-arch-followup-10min-steady-stream-unity.log`, but the Android task was removed externally after about one minute; full log `/private/tmp/warline-arch-followup-10min-stream-all.log` shows MIUI recents/task removal (`ProcessSceneCleaner.handleSwipeKill`, `removeTask`) rather than a Unity fatal exception or native crash. No checklist item was closed from this attempt.
- 2026-07-05: Extracted transport boarding diagnostic description and queue plumbing from `TransportBoardingCommandSystem` into stateless `TransportBoardingDiagnosticSystemHelper`, leaving command validation, boarding mutation, movement requests, deploy, rope, and airdrop ownership unchanged. Added the Unity `.meta` for the new helper. Validation passed: `git diff --check`, `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, and `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`.
- 2026-07-05: Extracted transport passenger classification and capacity/occupancy implementation from `TransportBoardingCommandSystem` into stateless `TransportBoardingCapacitySystemHelper`, keeping the existing public command-system wrappers and test-visible Board preview/Board All call strings stable. Boarding command validation, approach planning, movement mutation, deploy, rope, and airdrop logic remain in the command system. Validation passed: source guard string checks for Board preview and Board All, `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, and `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`.
- 2026-07-05: Extracted reusable transport approach, air-pickup approach, footprint reservation, and disembark ring-cell search implementation from `TransportBoardingCommandSystem` into stateless `TransportBoardingApproachSystemHelper`, keeping the existing public command-system wrappers stable. Cargo-plane ramp-specific scoring and rollout planning remain in the command system for the next smaller slice. Validation passed: source guard string checks for Board preview/Board All plus approach wrappers, `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, and `git diff --check`.
- 2026-07-05: Extracted plane-ramp disembark and rollout cell search implementation from `TransportBoardingCommandSystem` into stateless `TransportBoardingApproachSystemHelper`, leaving actual passenger removal, visibility, transport buffers, movement orders, rope, deploy, and airdrop mutation in the existing command-system path. Validation passed: ramp helper source guard strings, `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`, and `git diff --check`.
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
- Known unresolved: visual/playmode smoke validation is pending, HDR/soft-shadow tier ownership is still unresolved, the current Android baseline is only a first `2,000`-frame run rather than a full 10-minute/thermal-soak capture, draw-call/batch counters are unavailable from the saved-capture exporter path, Fuel/Oil still keeps runtime-building storage mirrors for compatibility, and Phase 5 still has no truthful blind Burst candidates.
- Next iteration: add or enable a live Android render-counter capture path for draw calls and batches, then either run a longer Android thermal-soak capture or close Phase 7 with documented limits before moving back to render-tier ownership.
