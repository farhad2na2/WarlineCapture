# Work Order — Architecture/Performance Follow-up (post re-audit 2026-07-06)

**Source audits:** `Design/AgentReports/2026-07-02_audit_architecture-performance-followup.md`
(findings) and `Design/AgentReports/2026-07-06_audit_reaudit-status.md` (current status).
This document is self-contained; you do not need the audit conversation.

**State you inherit (do not re-derive):** Android steady state is 60.1 FPS avg / P95 17.6 ms
with ~50 B/frame GC. CI-style gates exist and pass: editor P95 budget gate, steady-state GC
budget gate (0-byte budget), accepted baseline at
`Design/Architecture/performance_regression_accepted_baseline.json`, produced by
`Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline`.

## Global guardrails (apply to every work package)

1. **Never weaken the gates.** The accepted baseline may only be re-accepted when metrics
   are equal or better; any regression needs explicit user sign-off in the PR/commit text.
2. After each work package: run Unity architecture validation, the match smoke validation,
   and the focused perf validations touching your area (`Assets/Tests/Editor/*PerformanceValidation.cs`).
3. **Do not modify** the pathfinding hot path (`PathfindBatchJob`, `UnitPathfindingScheduleSystem`,
   `UnitPathfindingSystem`, `UnitPathGridSnapshotSystem`) — already optimized and pinned.
4. One work package per commit/PR; update the relevant tracker the way phases 0–10 did
   (inventory first, statuses, no silent scope growth).
5. WP2 and WP6 have **user visual sign-off gates** — stop and present screenshots; do not
   self-approve visual changes.

---

## WP1 — Burst coverage pass + guardrail  *(P0, ~1–2 days, mechanical)*

**Why:** 74 of 131 `ISystem` files lack `[BurstCompile]`, and the ratio worsens as new
systems land un-Bursted. Under IL2CPP on Android this is the cheapest remaining CPU win,
and it is the only quick win from the 2026-07-02 audit that was never started.

**What:**
1. Inventory the uncovered files:
   ```bash
   for f in $(grep -rl ": ISystem" Assets/Game/Scripts --include="*.cs"); do
     grep -q "BurstCompile" "$f" || echo "$f"; done
   ```
2. Classify each file: (a) Burstable as-is, (b) Burstable after removing managed API use
   (`SystemAPI.ManagedAPI`, UnityEngine object access, string ops), (c) legitimately
   managed → goes on the opt-out list with a one-line reason. Write the classification to
   `Design/AgentReports/<date>_burst_coverage_inventory.md` **before** editing code.
3. Apply `[BurstCompile]` to the struct and to `OnCreate/OnUpdate/OnDestroy` for (a), fix
   and apply for (b) where the fix is local and low-risk; don't force (c).
4. Add an architecture guardrail mirroring the instantiate-ownership one (commit
   `3c085f373`): validation fails when an `ISystem` file lacks `[BurstCompile]` and is not
   in the explicit opt-out list. This stops the ratio regressing again.

**Acceptance:** coverage ≥ 90% of ISystem files (or opt-out documented per file); Burst
compiles clean (no `BC` errors in editor log); all gates + focused validations pass;
re-run `RunPerformanceRegressionBaseline` and record before/after p95 in the report.

---

## WP2 — Mobile visual-quality verification (possible over-correction)  *(P0, ~half day + user review)*

**Why:** the mobile URP fix went far: `Assets/Settings/Mobile_RPAsset.asset` now has
renderScale **0.5**, shadowDistance **16 m**, 1 cascade, hard shadows, HDR/MSAA off. At a
top-down RTS camera, 16 m shadows are effectively invisible and 0.5 render scale is soft
on 1080p+ phones. Meanwhile the device capture shows headroom (CPU 8 ms, GPU 5.3 ms at
60 FPS) — some of it can be spent back on image quality.

**What:**
1. Capture device screenshots of the Match scene at 3 fixed viewpoints (gameplay zoom,
   max zoom-out, night phase) with current settings.
2. Prepare a "recommended tier" variant: renderScale 0.7–0.8, shadowDistance 40–60 m,
   1–2 cascades; capture the same screenshots + an Android profiler capture with it.
3. Present both sets side-by-side to the user with the perf delta. **User decides.**
4. If approved, wire the two tiers through `Assets/Game/Rendering/VisualQualityConfig.asset`
   so device tier selects the URP asset (pattern already exists for Mobile/PC split).

**Acceptance:** user has seen and chosen; chosen tier's Android capture keeps P95 within
+1 ms of the current baseline; tiers selectable via config, not hand-edits.

---

## WP3 — Finish TransportBoardingCommandSystem phase 9  *(P1, multi-day, plan exists)*

**Why:** still 3,062 lines (down from 4,022). The remaining extraction map is already
written: `Design/AgentReports/2026-07-05_transport_boarding_command_system_phase9_inventory.md`.

**What:** follow that inventory exactly — **pin partial-unload / remaining-passenger
behavior with tests first**, then extract the disembark-routing owner set
(`ProcessDisembarkTransportRequest`, `TryDisembarkTransport*`, `TryFindTransportDisembarkCell`,
plane-ramp helpers, `CanPlaceDisembarkedFootprint`, `TryFindTransportRingCell`) into its
own owner class/system, same pattern as the earlier phase-9 slices.

**Acceptance:** new tests pass before AND after the move; `TransportBoardingPerformanceValidation`
passes; file under ~2,000 lines; tracker updated.

---

## WP4 — Gate remaining interpolated logs in Systems  *(P2, ~2–4 h, mechanical)*

**Why:** 21 `Debug.Log($"...")` sites remain in `Assets/Game/Scripts/Systems/` (e.g.
`BuildingResourceHaulerBridgeCompositionSystemHelper.cs:937+`, tag `[ResourceHauler]`).
Interpolation builds the string even when the log is disabled → managed allocations and
main-thread time in gameplay paths. The GC gate currently passes, so treat as hygiene, not
emergency — but new code copies existing patterns, so clean the patterns.

**What:** list with `grep -rn 'Debug.Log.*\$"' Assets/Game/Scripts/Systems --include="*.cs"`.
For each: if already inside an enable-flag check that short-circuits *before* string
construction, whitelist it in the report; otherwise either hoist the gate above the string
build or route through a `[System.Diagnostics.Conditional("UNITY_EDITOR")]` /
`("DEVELOPMENT_BUILD")` diagnostics helper. **Do not delete logs** — they are working
diagnostics.

**Acceptance:** every site either gated-before-build or whitelisted with reason; GC gate
still passes at 0-byte budget.

---

## WP5 — Pre-emptive decomposition inventory: SelectionHudFeedbackUiSystemHelper  *(P2, ~1 day)*

**Why:** 1,982 lines and the fastest-growing helper — the next
TransportBoardingCommandSystem if left alone. Catch it while it's one-third the size.

**What:** inventory FIRST, code later (house pattern from phase 9/10): map method clusters
into cohesive owner sets, write
`Design/AgentReports/<date>_selection_hud_feedback_helper_inventory.md` with per-set risk
notes and which existing HUD validations pin each behavior. Extract only the lowest-risk
owner set in this work package; leave the rest as tracked follow-ups.

**Acceptance:** inventory doc exists; one owner set extracted; HUD feedback + UI shell
validations pass; no behavior change.

---

## WP6 — Night light floor  *(P2, ~2–4 h + user review)*

**Why:** the 10-minute day/night cycle (config:
`Assets/Game/Configs/Scene/Game_DayNight_Config.asset`) drives the scene near-black at
night — a readability problem (units/UI unidentifiable) known since 2026-06-12 and still
open (no floor exists in the config or the cycle system).

**What:** add a minimum-intensity floor to the day/night system — clamp sun intensity
and/or ambient so night never drops below a readable level (start at ~25–35% of noon
values; make both floor values config fields in the DayNight config asset, not constants).
Capture a night-phase screenshot before/after and present to the user. **User decides the
final floor values.**

**Acceptance:** units and HUD readable at deepest night in the screenshot the user
approves; values live in the config asset.

---

## WP7 (optional) — Close the device-batching question  *(P3, ~half day)*

**Why:** the Android profiler's classic render counters read ~0 (Entities Graphics/BRG
doesn't report them), so batching efficiency was never directly measured. GPU time (5.3 ms)
says it's currently fine — only do this if convenient or if GPU time ever exceeds ~8 ms.

**What:** one Android GPU capture (Android GPU Inspector or RenderDoc on a supported
device) of a mid-battle frame; record draw/batch equivalents and the top-3 GPU passes into
a short report. No code changes unless the capture shows a specific dominant cost.

---

## Suggested order

WP1 → WP2 (both P0, independent) → WP4 (quick) → WP3 → WP5 → WP6 → WP7.
WP2 and WP6 end at user sign-off gates; schedule them where a user review is convenient.
