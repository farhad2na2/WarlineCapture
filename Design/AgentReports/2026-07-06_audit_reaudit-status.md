# Re-audit — status of 2026-07-02 findings

**Date:** 2026-07-06 · Re-measured on `main` @ `f5da730b3` against the
[2026-07-02 audit](2026-07-02_audit_architecture-performance-followup.md).

## Critical issues — all three addressed

| Finding | Status | Evidence |
|---|---|---|
| **C1 — managed composition layer GC/spikes** | ✅ Largely addressed | Android steady-state GC ≈ **50 B/frame** (84,780 B over 1,700 frames; was ~58 KB/frame in the June editor capture). Zero-alloc regression gate enforced (`RunPerformanceRegressionBaseline`: 0-byte budget, passed at 740 units / 630 buildings) + editor P95 budget gate + accepted-baseline artifact (`Design/Architecture/performance_regression_accepted_baseline.json`). The helper-layer *architecture* remains (320 files, see "what's left"), but its measured cost is now controlled and gated. |
| **C2 — mobile URP overkill** | ✅ Fixed (possibly over-corrected) | `Mobile_RPAsset`: HDR off, MSAA off, renderScale **0.5**, shadowDistance **16**, 1 cascade, hard shadows. See open item #2 — verify visual quality didn't overshoot. |
| **C3 — no Android ground truth** | ✅ Fixed | Device captures 2026-07-04/05. Steady state: **60.1 FPS avg, P95 17.6 ms, P99 18.3 ms**; CPU active 8.0 ms, GPU 5.3 ms — `WaitForTargetFPS` (vsync idle) dominates the frame, i.e. real headroom. The "797/1700 frames over 16.67 ms" stat is vsync jitter, not work. |

## Quick wins

| Item | Status |
|---|---|
| URP mobile settings | ✅ Done |
| OnGUI chain in release | ✅ Done — `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` around `MatchSceneView.OnGUI` |
| GC re-baseline capture | ✅ Done — plus turned into a permanent budget gate |
| 15 `Instantiate` in systems | ✅ Resolved as designed — classification report (2026-07-03) shows 0 gameplay `Object.Instantiate`; 14 are legitimate ECS entity-prefab instantiates; architecture guardrail added. My original finding was a false positive, handled the right way. |
| Interpolated `Debug.Log` in Systems | 🟡 Partial — 21 remain (was 25), e.g. `[ResourceHauler]` in `BuildingResourceHaulerBridgeCompositionSystemHelper.cs:937+` |
| **Burst coverage** | ❌ Not addressed — **74 of 131** ISystem files still lack `[BurstCompile]` (was 72/125; new systems keep landing without it) |

## Long-term items

| Item | Status |
|---|---|
| God system decomposition | 🟡 In progress — `TransportBoardingCommandSystem` 4,022 → **3,062** lines; phase-9 inventory maps remaining extractions (tests-first, correctly). Watch: `SelectionHudFeedbackUiSystemHelper` grew to 1,982 lines. |
| `Game.Runtime` domain split | 🟡 Started — `Game.Runtime.Pathfinding` extracted; phase-10 tracker complete for its scope (138/138) with the split pattern documented for next slices. |
| CI perf regression gate | ✅ Done — p95 + GC budget gates against accepted baseline |
| Change-filter usage | ~unchanged (4 usages) — fine, low priority |
| Night light floor | ❌ Still open (readability at night phase) |
| Device draw-call story | 🟡 Unknown — the Android capture's classic render counters read ~0 (BRG/Entities Graphics doesn't report them); GPU 5.3 ms says it's fine in practice, but batching remains unmeasured directly. |

## What's left — prioritized

1. **Burst pass** (the one untouched quick win): add `[BurstCompile]` to the 74 uncovered
   ISystem files where the body allows; add a guardrail (like the instantiate one) so new
   ISystem files without Burst fail architecture validation with an explicit opt-out list.
2. **Visual sanity-check the aggressive mobile settings**: renderScale 0.5 and 16 m
   shadows may look noticeably soft/flat on 1080p+ phones. Capture device screenshots;
   if quality suffers, build the tier ladder (baseline keeps current settings; recommended
   tier ~0.7–0.8 scale, ~40–60 m shadows) driven by `VisualQualityConfig`.
3. **Finish TransportBoarding phase 9** per its inventory (pin partial-unload behavior with
   tests before extracting disembark routing).
4. **Gate the remaining 21 interpolated logs** (enable-check before string construction, or
   `[Conditional]` wrappers) — small, mechanical.
5. **Apply the god-file treatment to `SelectionHudFeedbackUiSystemHelper`** (1,982 lines and
   growing) before it becomes the next 4k-line file.
6. **Night light floor** — clamp minimum ambient/sun intensity in the day/night cycle.
7. Optional: one GPU-profiler (or RenderDoc) capture on device to close the batching
   question that the profiler counters can't see.

## Verdict

The 2026-07-02 audit is ~85% retired: all three criticals resolved or controlled, the
process gaps (device baseline, CI gates) permanently closed, and the two heavy long-term
refactors are in motion with sensible test-first discipline. Remaining work is one
mechanical performance pass (Burst), one quality verification (mobile visual tier), and
the continuation of already-planned decomposition phases.
