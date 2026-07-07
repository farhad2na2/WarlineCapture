# Performance / Architecture Re-audit Follow-up Tracker

## Goal
Turn the 2026-07-06 re-audit and work order into a fast, measurable implementation plan. Start with the remaining quick wins, especially Burst coverage, then move into visual verification and measured decomposition work. Keep every slice behavior-preserving unless a phase explicitly calls out a user-approved visual/config choice.

## Sources
- Re-audit status: `Design/AgentReports/2026-07-06_audit_reaudit-status.md`
- Work order: `Design/AgentTasks/2026-07-06_perf_architecture_followup_worklist.md`
- Prior completed tracker: `Design/Architecture/architecture_performance_audit_followup_tracker.md`
- Accepted performance baseline: `Design/Architecture/performance_regression_accepted_baseline.json`

## Current Read

### Resolved Or Controlled
- Critical managed-helper GC/spike finding is controlled by Android measurements and CI-style p95/GC gates.
- Mobile URP overkill was fixed, but the current settings may be too aggressive visually.
- Android ground-truth capture exists and should remain the deciding performance evidence.
- ECS `Object.Instantiate` finding was resolved as a false-positive ownership issue with classification and guardrails.
- `Game.Runtime` domain splitting has a validated first pattern with `Game.Runtime.Pathfinding`.

### Still Worth Doing
- Burst coverage is the main untouched quick win: re-audit reports 74 of 131 `ISystem` files missing `[BurstCompile]`.
- Remaining interpolated diagnostic logs in `Systems` should be gated before string construction.
- Mobile render quality needs visual sign-off because renderScale `0.5` and shadow distance `16` may be too soft/flat.
- `TransportBoardingCommandSystem` is improved but still large.
- `SelectionHudFeedbackUiSystemHelper` is growing and needs inventory before it becomes the next god file.
- Night light readability floor is still open.
- Android draw/batch counters remain unresolved, but GPU time says this is optional unless GPU cost rises.

## Global Guardrails
- Work on `main`; do not create branches.
- One work package per stable commit. Push after compiler/validation gates pass.
- Do not weaken existing p95, GC, accepted-baseline, instantiate, architecture, or namespace/domain guardrails.
- Do not modify the pathfinding hot path (`PathfindBatchJob`, `UnitPathfindingScheduleSystem`, `UnitPathfindingSystem`, `UnitPathGridSnapshotSystem`) in this tracker unless a later user-approved task explicitly targets pathfinding.
- No UI Toolkit.
- No new `Boundary` or `Presenter` class names.
- No new MonoBehaviour gameplay `Update` loops.
- No parallel gameplay logic.
- New gameplay/projection logic should be Burst-capable `ISystem` where practical.
- Canvas and MonoBehaviour code stays serialized-reference binding, button-event, scene bootstrap, camera, or visual-state application only.
- Preserve Unity `.meta` files.
- Preserve scene/prefab bindings unless the phase explicitly says a prefab/config update is part of the work.
- User visual sign-off is required for mobile visual-quality changes and night-light final values.
- Do not grow the active checklist total silently. New findings go to the optional backlog or a separate tracker unless the user explicitly expands scope.

## Progress Snapshot

| Field | Status |
|---|---|
| Checklist complete | 27 / 67 active, 0 skipped |
| Checklist percent complete | 40.3% active |
| Optional backlog | 0 / 5, not counted in active total |
| Current phase | Phase 3 - Mobile Visual Quality Verification |
| Current target | Prepare current mobile visual-quality capture plan and identify required screenshots/perf evidence before changing defaults. |
| Quick wins status | Baseline sync complete; Burst inventory and guardrail ledger updated and validated; log-gating inventory complete with no hoist targets; mobile visual verification next; night floor not started. |
| Burst status | Refreshed inventory: 130 runtime `ISystem` files, 57 with `[BurstCompile]`, 73 without `[BurstCompile]`, 0 unclassified. `TacticalFollowAttackCinematicSystem` is now explicitly classified as managed camera cinematic orchestration. |
| Mobile visual status | Current mobile tier is performance-safe but may be over-corrected visually; screenshots and user sign-off required before changing tier defaults. |
| Transport status | Phase 9 continuation is not part of the quick-win start; keep pending until Burst/log/visual gates are handled. |
| Selection HUD status | Inventory first; no extraction until method clusters and validation coverage are mapped. |
| Compiler status | Phase 0 baseline green. After rebase to `c218b124a`, `Game.Runtime.csproj` and `Game.Tests.Editor.csproj` build with 0 errors; `Game.Runtime.csproj` needed the fetched `TacticalFollowAttackCinematicHelper.cs` include synced. |
| Validation status | Phase 0 `git diff --check` passed before rebase. Phase 1 rebased validation: `git diff --check` passed, `Game.Tests.Editor.csproj` passed with 0 errors, `Game.Runtime.csproj` passed with 0 errors, Unity no-Burst classification validation passed with `noBurst=73 classified=73 unclassified=0`. Phase 2 inventory found no code-change hoist targets. |
| Still wrong / next iteration | Fetched/rebased onto `origin/main` at `c218b124a` after the Burst inventory slice. No low-risk `Burstable as-is` no-Burst systems were found. Phase 2 found 26 direct interpolated Debug calls, all already gated, editor/development-only, warning/error, or intentional one-shot startup markers. Next iteration should start Phase 3 visual capture planning without changing mobile defaults before sign-off. |

## Phase 0 - Baseline Sync
No behavior changes. Establish the current state so Burst/log work is measured and reproducible.

- [x] Confirm working tree state and branch: branch is `main`; tree has only this uncommitted tracker document.
- [x] Fetch/rebase only if needed before the first implementation commit: fetched and rebased onto `origin/main` at `c218b124a` with autostash after upstream advanced.
- [x] Confirm Unity version and current generated project files after latest main: `ProjectVersion.txt` reports `6000.5.2f1 (eb73d3b415a1)`.
- [x] Run `git diff --check` as baseline.
- [x] Run `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`: passed, 0 errors, 6 warnings.
- [x] Run `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`: passed, 0 errors, 9 warnings.
- [x] Run `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`: passed, 0 errors, 11 warnings.
- [x] Update this progress snapshot with baseline compiler status.

## Phase 1 - Burst Coverage Quick Win
First real work package. Inventory before edits, then add only safe Burst coverage.

- [x] Refresh current `ISystem` inventory under `Assets/Game/Scripts`.
- [x] Count current files missing `[BurstCompile]`: 73 / 130 runtime `ISystem` files currently have no `[BurstCompile]`.
- [x] Create `Design/AgentReports/<date>_burst_coverage_inventory.md`: `Design/AgentReports/2026-07-07_burst_coverage_inventory.md`.
- [x] Classify every missing-Burst `ISystem` as `Burstable as-is`, `Burstable after local refactor`, `Managed edge`, `Presentation only`, or `Needs refactor`.
- [x] Add a one-line reason for every opt-out.
- [x] Add `[BurstCompile]` to `Burstable as-is` systems, including lifecycle methods where local convention supports it: no safe `Burstable as-is` files found in the refreshed no-Burst set.
- [x] Apply only low-risk local refactors for `Burstable after local refactor`; defer anything that changes ownership or behavior: no local-refactor Burst candidates found in this slice.
- [x] Do not force Burst onto managed/UI/presentation edge systems.
- [x] Add or update an architecture guardrail so new no-Burst `ISystem` files fail validation unless listed in the explicit opt-out ledger.
- [x] Run Unity architecture validation and scan editor log for Burst/compiler errors: `EcsBurstHotPathArchitectureTests.RunNoBurstISystemClassificationValidation` passed in `/private/tmp/warline-reaudit-burst-classification.log`.
- [x] Run focused performance validations touching changed systems: not applicable for this slice because only the architecture classification ledger and docs changed; no runtime gameplay/Burst system code changed.
- [x] Re-run `RunPerformanceRegressionBaseline` and record before/after p95 and GC bytes in the Burst inventory report: not applicable for this slice because no runtime code or Burst attribute changed; deferred to the next runtime-affecting quick-win slice.

## Phase 2 - Diagnostic Log Allocation Quick Win
Mechanical hygiene after Burst. Do not delete useful diagnostics.

- [x] Refresh `Debug.Log($"...")` inventory under `Assets/Game/Scripts/Systems`.
- [x] Create `Design/AgentReports/<date>_systems_interpolated_log_gate_inventory.md`: `Design/AgentReports/2026-07-07_systems_interpolated_log_gate_inventory.md`.
- [x] Classify each log as already gated, needs gate hoist, editor/development-only, warning/error, or intentional always-on.
- [x] Hoist enable checks before string construction for gameplay-path diagnostics: no hoist targets found; recurring diagnostics already guard before interpolation.
- [x] Route editor/development-only diagnostics through conditional helpers where cleaner than inline guards: no code change needed; existing `UNITY_EDITOR` and editor/development-only system gates are already explicit.
- [x] Preserve warnings/errors needed for failure diagnosis.
- [x] Run steady-state GC gate and record whether player-relevant bytes remain within budget: not applicable for this inventory-only slice because no runtime code changed.

## Phase 3 - Mobile Visual Quality Verification
P0 quality/performance check. This phase has a user visual sign-off gate before changing defaults.

- [ ] Capture current mobile settings at three fixed device viewpoints: gameplay zoom, max zoom-out, and night phase.
- [ ] Prepare a recommended visual tier variant in config/assets without committing it as default.
- [ ] Recommended starting point: renderScale `0.7-0.8`, shadowDistance `40-60`, 1-2 cascades, still no HDR/MSAA unless evidence supports it.
- [ ] Capture the same three viewpoints for the recommended tier.
- [ ] Run an Android profiler capture for the recommended tier.
- [ ] Compare p95, p99, CPU active, GPU, GC, and screenshots against current baseline.
- [ ] Present side-by-side screenshots and perf delta for user decision.
- [ ] If approved, wire the selected tier through `VisualQualityConfig.asset`; if rejected, document that current mobile tier remains intentional.

## Phase 4 - Night Light Readability Floor
Small config/system feature with user visual sign-off.

- [ ] Inventory current day/night config and runtime owner.
- [ ] Add config fields for minimum sun and/or ambient intensity floors; do not hardcode final values.
- [ ] Clamp deepest-night lighting through the existing day/night owner.
- [ ] Start with a conservative test range around 25-35% of noon values.
- [ ] Capture before/after deepest-night screenshots.
- [ ] Present screenshots for user decision.
- [ ] Commit only the approved floor values.
- [ ] Run match smoke and visual-quality relevant validation.

## Phase 5 - TransportBoardingCommandSystem Continuation
Measured structural work after quick wins. Follow the existing phase-9 inventory exactly.

- [ ] Read `Design/AgentReports/2026-07-05_transport_boarding_command_system_phase9_inventory.md`.
- [ ] Pin partial-unload / remaining-passenger behavior with tests before extraction.
- [ ] Identify the disembark-routing owner set and public seams.
- [ ] Extract `ProcessDisembarkTransportRequest` and related `TryDisembarkTransport*` routing without changing gameplay ownership.
- [ ] Extract plane-ramp and ring-cell helper responsibilities only after tests pass.
- [ ] Run `TransportBoardingPerformanceValidation`.
- [ ] Run full transport validation.
- [ ] Update tracker/report with file-size delta and behavior-preservation notes.

## Phase 6 - SelectionHudFeedbackUiSystemHelper Inventory And First Extraction
Prevent the next god helper. Inventory first, extract only one low-risk owner set.

- [ ] Create `Design/AgentReports/<date>_selection_hud_feedback_helper_inventory.md`.
- [ ] Map method clusters and current responsibilities.
- [ ] Identify existing HUD, feedback, command, and UI shell tests covering each cluster.
- [ ] Identify missing tests before extraction.
- [ ] Choose one low-risk owner set for extraction.
- [ ] Extract without adding `Boundary`, `Presenter`, UI Toolkit, or gameplay ownership.
- [ ] Run HUD feedback and UI shell focused validations.
- [ ] Update inventory with completed extraction and remaining owner sets.

## Phase 7 - Transport / Selection Follow-up Validation Sweep
Close the implementation-heavy phases with targeted regression checks.

- [ ] Run Unity architecture validation.
- [ ] Run match shell smoke validation.
- [ ] Run accepted-baseline performance regression validation.
- [ ] Run steady-state GC budget validation.
- [ ] Run focused validations touched by phases 5 and 6.
- [ ] Update this tracker with final command/log evidence.
- [ ] Commit and push only after the validation sweep is clean.
- [ ] List remaining optional work without increasing active checklist total.

## Optional Backlog - Not Counted In Active Total
Only promote these to active checklist items with explicit user approval or new performance evidence.

- [ ] Android GPU profiler or RenderDoc capture to close the device batching question.
- [ ] Additional `Game.Runtime` domain splits beyond the first `Game.Runtime.Pathfinding` pattern.
- [ ] Change-filter pass if fresh profiling shows avoidable per-frame component reads.
- [ ] Broader managed-helper migration beyond Transport and Selection HUD.
- [ ] Mobile HDR/MSAA experiment if visual sign-off asks for higher quality and GPU headroom remains.

## Suggested Execution Order
1. Phase 0 baseline sync.
2. Phase 1 Burst coverage quick win.
3. Phase 2 interpolated log gating.
4. Phase 3 mobile visual-quality verification.
5. Phase 4 night light floor.
6. Phase 5 TransportBoarding continuation.
7. Phase 6 Selection HUD inventory/extraction.
8. Phase 7 validation sweep.

## Validation Commands
Use the project-standard Unity wrapper when Unity validation is needed:

```bash
Tools/CI/invoke_unity_macos.sh --timeout 300 --log /private/tmp/warline-reaudit-architecture.log -- -quit -executeMethod ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation
```

Baseline local builds:

```bash
git diff --check
dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly
dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly
dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly
```

Performance gates to run after relevant implementation phases:

```bash
Tools/CI/invoke_unity_macos.sh --timeout 600 --log /private/tmp/warline-reaudit-performance-baseline.log -- -quit -executeMethod Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline
Tools/CI/invoke_unity_macos.sh --timeout 600 --log /private/tmp/warline-reaudit-gc-baseline.log -- -quit -executeMethod Game.Editor.MatchGcAllocationCallstackCapture.RunSteadyState
```
