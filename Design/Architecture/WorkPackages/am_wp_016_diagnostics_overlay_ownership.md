# AM-WP-016 - Diagnostics Overlay Ownership

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted, `AM-026` proves every runtime and serialized Diagnostics caller, `AM-028` accepts lifecycle ownership, `AM-063` accepts the production diagnostics inventory/build-availability contract, and `AM-034` dispatches this package.

Umbrella task: `AM-034`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, row `UI-020`.

## 1. Current Ownership And Risk

- `MenuDiagnosticsView.Update()` delegates every frame to `MenuDiagnosticsUiSystemHelper`, which accumulates FPS and rewrites the FPS label every `0.25 s` while the view is enabled.
- The direct helper subscribes to `Application.logMessageReceived`, replays `RuntimeLogBuffer`, maintains another queue/StringBuilder, and rebuilds/forces layout when the log panel opens.
- Separately, unmanaged `UiDiagnosticsReadModelSystem` also accumulates FPS, owns another static runtime-log queue/StringBuilder/subscription, writes `UiDiagnosticsOverlayComponent`, and has no proven production presentation consumer.
- `UiActionKind.ToggleDiagnosticsOverlay` and `CloseDiagnosticsOverlay` mutate the ECS overlay state, but no source caller was found in production C#; serialized callers remain unproven.
- `TryReadDiagnosticsOverlay()` can initialize missing ECS state during a read and converts log fixed strings when visible.
- The direct panel and dormant ECS path can therefore duplicate frame counting, log subscriptions, retained messages, formatting, and lifecycle state even when only one visible diagnostics surface exists.

Risks are diagnostics distorting the performance they report, hidden release-build work, duplicate log retention/subscriptions, write-on-read ECS behavior, stale static buffers, and treating diagnostic allocations as production UI debt or excluding them too broadly. This package must preserve useful development diagnostics while making build availability and cost explicit.

## 2. Accepted Future Ownership

- `AM-026` and `AM-063` prove all code and serialized callers, build symbols/configuration, visible surfaces, and retention requirements before choosing the canonical path.
- Exactly one owner supplies FPS/log data to the visible Diagnostics surface. The other direct/ECS path is removed or made provably dormant with zero update, subscription, formatting, and retained-buffer cost.
- `RuntimeLogBuffer` is the sole process-level log capture/bounded history unless current evidence proves it cannot satisfy the accepted surface. A second `Application.logMessageReceived` subscription or duplicate log queue is prohibited.
- Disabled Diagnostics performs no per-frame method call, FPS accumulation, log subscription, snapshot, formatting, string construction, layout rebuild, ECS update, or gateway read.
- When intentionally enabled, one measured owner samples FPS at the accepted interval and formats only when the displayed FPS bucket changes. Log text rebuilds only while the panel is visible and the bounded log version changes.
- Diagnostic history has one explicit capacity and overflow/drop policy; enabling a panel may replay the bounded snapshot once.
- Gateway reads are pure. Any retained ECS projection is versioned and created by explicit bootstrap/config ownership.
- Disable, view/root replacement, World replacement, subsystem registration, and application quit release every listener/reference idempotently.
- Development/editor instrumentation and player-visible production telemetry are classified and measured separately; disabled release diagnostics are compiled out or configuration-gated before work begins under `AM-066`.

No `SystemBase`, second poller, second log buffer, mutable unbounded static state, default-World lookup, broad manager/controller/provider/service type, or broad diagnostics exclusion is allowed.

## 3. Identity And Invalidation Contract

Minimum identity when enabled:

- diagnostics-enabled/build-availability generation;
- visible panel and FPS-label visibility state;
- FPS sampling window and displayed FPS bucket;
- canonical bounded log-buffer version, count, and overflow generation;
- log-panel open generation;
- World, boundary, view/root, and binding generations if ECS state remains;
- formatting/theme generation;
- explicit invalidation generation and reason.

Rules:

- Disabled state owns no active runtime subscription or update loop.
- Equal displayed FPS produces zero TMP write.
- Hidden log panel performs zero log formatting and layout work; a changed log version is applied once when visible or on next open.
- One log event enters one canonical buffer once. Re-enable/rebind cannot replay it into duplicate retained entries.
- Buffer capacity is fixed; overflow behavior is deterministic and observable without recursive logging.
- Read APIs never add components or mutate ECS.
- Enabled/disabled, open/close, World/root replacement, and subsystem registration invalidate once and cannot retain stale callbacks.
- Diagnostic cost is reported separately and never hidden by classifying active production work as tooling.

## 4. Exact File Allowlist

Production files allowed after dispatch:

- `Assets/Game/Scripts/UI/MenuDiagnosticsView.cs`
- `Assets/Game/Scripts/UI/MenuDiagnosticsUiSystemHelper.cs`
- `Assets/Game/Scripts/UI/RuntimeLogBuffer.cs`
- `Assets/Game/Scripts/UI/DiagnosticsFpsText.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiDiagnosticsReadModelSystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs` only for Diagnostics state/version disposition
- `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs` only for Diagnostics action/model disposition
- `Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs` only for Diagnostics contract disposition
- `Assets/Game/Scripts/UI/Shell/UiShellRuntimeGateway.cs` only for Diagnostics contract/lifecycle disposition
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Core.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.DefaultState.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.Contracts.cs` only for Diagnostics contract disposition
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs` only for Diagnostics cache/lifecycle disposition
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellStateSystem.cs` only for Diagnostics component/default disposition
- `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestDispatchSystemHelper.cs` only for proven Diagnostics action disposition
- `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs` only for proven Diagnostics state disposition

Test files allowed:

- `Assets/Tests/Editor/EcsBurstHotPathArchitectureTests.cs` only if the dormant ECS owner is removed or gated
- existing shell fake-gateway tests only when the Diagnostics interface is removed and the compile contract genuinely changes
- `Assets/Tests/Editor/MenuDiagnosticsLifecycleTests.cs` and its `.meta` if required
- `Assets/Tests/Editor/DiagnosticsOverlayArchitectureTests.cs` and its `.meta` if required
- `Assets/Tests/Editor/DisabledDiagnosticsPerformanceValidation.cs` and its `.meta` if required
- `Assets/Tests/Editor/EnabledDiagnosticsPerformanceValidation.cs` and its `.meta` if required

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_016_diagnostics_overlay_ownership_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_016_diagnostics_overlay_ownership_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-034`, `AM-063`, `AM-064`, `AM-066`, and `AM-068` tracker records and progress snapshot

Read-only dependencies:

- serialized Diagnostics buttons/surfaces in prefabs/scenes, build configuration/scripting symbols, runtime telemetry/crash reporting, UI visual-lock assets, and Editor/profiler capture tooling.

Hard exclusions:

- operation-map/static-map, FirstLaunch, audio, gameplay, scenes, prefabs, visual-lock art, packages, `ProjectSettings`, and unrelated performance-diagnostics owners;
- diagnostic visual design, colors, maximum visible entry count, stack-trace inclusion policy, user-visible actions, or production telemetry policy changes without explicit design/diagnostics-owner approval;
- any production path outside this allowlist without a reviewed amendment and active-owner handoff.

This package is serialized with other packages claiming shared shell gateway/contracts.

## 5. Characterization Matrix

Required before edits:

1. Editor, development player, and release player with Diagnostics disabled and enabled;
2. direct view present/absent, ECS component/system present/absent, and every code/serialized Toggle/Close caller;
3. FPS label visible/hidden, log panel closed/open, and no logs/normal/warning/error/exception bursts;
4. buffer below capacity, at capacity, overflow, duplicate messages, long stack traces, and recursive-log prevention;
5. enable/disable, view/root replacement, World/boundary replacement, subsystem registration, domain reload, and application quit;
6. equal FPS bucket, changed bucket, dropped frames, pause, focus loss, and time-scale changes;
7. gateway reads against present/missing state, proving no read-side mutation;
8. one canonical subscription/buffer and exact subscriber/entry counts through `100` lifecycle cycles.

Record active updates, sampling windows, log callbacks, canonical/duplicate buffer counts, retained characters/bytes, formatting calls, TMP writes, layout rebuilds, ECS writes, managed allocations, and CPU time.

## 6. Baseline And Acceptance Gates

Measure:

- `180` warmup plus `300` frames with Diagnostics disabled in Menu and Match;
- the same window with FPS-only enabled, panel open with unchanged logs, and bounded log bursts, reported separately;
- `100` enable/disable, open/close, view/root replacement, and World replacement cycles;
- capacity/overflow and release-build configuration independently.

Acceptance:

- exactly one proven visible Diagnostics data path, one canonical log subscription, and one bounded history owner remain;
- disabled Diagnostics performs zero recurring production-owned managed allocation, update calls, subscriptions, formatting, TMP writes, layout work, and ECS work;
- enabled unchanged FPS/log states meet their separately recorded CPU/allocation budgets and perform no equal-value writes;
- one new log version rebuilds visible text at most once; hidden panels do no formatting/layout work;
- capacity and overflow remain bounded and deterministic without recursive logging;
- gateway reads perform zero ECS structural changes;
- lifecycle stress leaves zero duplicate listeners, stale view references, duplicate entries, or retained World/boundary state;
- release-build availability follows the accepted `AM-066` policy and does not rely on runtime hiding after work has begun;
- focused tests, zero compiler errors, integrated architecture checks, and `git diff --check` pass;
- evidence binds baseline/implementation commit and tree, build symbols/configuration, source hashes, compressed logs, metrics, and focused review.

## 7. Maximum Slices And Rollback

At most three independently stable commits after caller/build-policy characterization:

1. canonical-owner decision, pure reads, build/enable gating, and removal/gating of duplicate FPS/log ownership;
2. one bounded log history/subscription plus changed-only FPS/log presentation;
3. lifecycle, overflow, disabled-release, and enabled/disabled performance acceptance.

Rollback if actionable logs disappear, error/exception stacks violate the accepted policy, controls stop working, overflow becomes silent/unbounded, disabled work remains active, enabled diagnostics distort beyond their budget, or the slice introduces `SystemBase`, another poller/buffer, mutable unbounded authority, protected-file edits, or non-allowlisted overlap.
