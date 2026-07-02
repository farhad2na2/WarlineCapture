# Architecture & Performance Audit — Follow-up

**Date:** 2026-07-02 · **Baseline:** builds on the 2026-06-18 audit + 2026-06-23 implementation plan (stages 0–8 complete) and the 2026-06-11 GC callstack captures. All numbers below re-measured against today's `main` (`37c035a70`, Unity 6000.5.2f1).

## Scorecard: prior findings, current state

| Prior finding | Then | Now | Status |
|---|---|---|---|
| A1: managed SystemBase dominance | 248 SystemBase / 112 ISystem | **24 / 141** | ✅ Fixed |
| P1: jobs on main thread (`.Run()`) | 88% `.Run()` | **0 `.Run()`, 33 `ScheduleParallel`** | ✅ Fixed |
| P3: managed `IComponentData` classes | 10 | **0** | ✅ Fixed |
| A5: Mono scripting backend | Mono | **IL2CPP (Android/iOS/Standalone)** | ✅ Fixed |
| A7: runInBackground | 0 | **1** | ✅ Fixed |
| A2: assembly graph clean | ✅ | Still clean; contracts layer intact post-namespace-migration | ✅ Holding |
| P6: Burst coverage | 44% of ISystem files missing Burst | **58% missing (72 of 125)** | ⚠️ Regressed (new ISystem files added without Burst) |
| P2: `Object.Instantiate` inside ECS systems | 15 | **15** | ❌ Open |
| P5: god system | TransportBoardingCommandSystem 3,875 lines | **4,022 lines** | ❌ Grew |
| P9: mobile shadow overkill | dist 240, 4 cascades | **Unchanged: dist 240, 4 cascades, soft shadows, HDR, MSAA 2x** | ❌ Open |
| P7: change filters underused | 1 usage | ~unchanged | ❌ Open |

The remediation program was real and effective on the ECS side. What remains is concentrated in **the managed composition layer, rendering config, and process**.

---

## CRITICAL issues

### C1 — The managed composition layer is the frame-time and GC hot path (architecture + perf)
- **Scale:** 309 files use the `*SystemHelper` pattern — plain C# classes ticked from `MatchSceneView.Update/LateUpdate/OnGUI` → `MatchBootstrapCompositionSystemHelper` ([MatchSceneView.cs:96](Assets/Game/Scripts/Composition/MatchSceneView.cs)). This is a second, non-Burst, single-threaded engine beside ECS.
- **Measured (2026-06-23 capture, editor):** `GameplayRuntimeUpdate.Selection` 1,637 ms total + **6.4 MB GC** over the capture; `GameplayRuntimeUpdate.BuildingPlacement` 869 ms + **10.5 MB GC**; gameplay spike frames of 83–85 ms dominated by managed paths (`UpdateActiveProductionTransports` 25.9 ms, `UnitAttackVfxRequestSystem` 42.8 ms). At the 33 ms mobile budget, one such spike = 3 dropped frames.
- **GC:** last full callstack baseline (2026-06-11): ~58 KB/frame steady-state, all top contributors under `MatchSceneView.Update`. Stage-1 "low-risk fixes" landed, but no post-fix callstack capture exists — current steady-state alloc rate is **unknown**.

### C2 — Mobile URP config contradicts the project's own 30 FPS Android target
`Assets/Settings/Mobile_RPAsset.asset`: shadowDistance **240 m**, **4 cascades**, soft shadows on, HDR on, MSAA 2x, renderScale 0.8. For a top-down RTS camera viewing ~50–120 m, this is shadow/bandwidth budget burned for invisible quality — on tiled mobile GPUs, HDR+MSAA+4-cascade shadows is the classic thermal-collapse recipe. Flagged in June (P9), still unfixed. This is the single cheapest big win in the project.

### C3 — Zero Android ground truth
Every measurement to date is Mac-editor. The baseline doc itself says "Android validation: not run." IL2CPP is configured but there is no device capture, no device frame-time distribution, no thermal profile. The entire performance program is calibrated against the wrong hardware; editor-relative improvements may or may not translate.

---

## QUICK WINS (days, mostly config/mechanical)

1. **Mobile render settings** (C2): shadowDistance 240→80–100, cascades 4→2, decide soft-shadows/HDR per device tier (VisualQualityConfig already exists as the switch point). Config-only change, measurable immediately.
2. **Burst the 72 uncovered ISystem files** — mechanical `[BurstCompile]` passes where the body allows; biggest cheap CPU win under IL2CPP.
3. **Strip the OnGUI chain from release builds**: `MatchSceneView.OnGUI` → bootstrap → `PerformanceDiagnosticsSystemHelper` ([MatchBootstrapCompositionSystemHelper.cs:258](Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs)). IMGUI ticks cost even when idle; wrap in `#if DEVELOPMENT_BUILD || UNITY_EDITOR`.
4. **Audit the 25 interpolated `Debug.Log($"...")` calls in Systems/** — ensure the enable-gate runs *before* string construction (known pattern from the GC plan); wrap diag loggers in `[Conditional]` helpers. Also fixes the "diag noise at error level" console spam.
5. **Pool or relocate the 15 `Object.Instantiate` calls inside ECS systems** (P2) — move to presentation helpers with pooling, or entity prefabs.
6. **Re-run the GC callstack capture** (30 min) to re-baseline after the June fixes — without it, C1 work can't be prioritized honestly.

---

## LONG-TERM issues

1. **Dual-architecture drift (C1's root):** ECS for sim + 309 managed helpers for orchestration/presentation. The helpers are where the biggest files live (4,022 / 1,824 / 1,622 / 1,568-line files), where GC lives, and where single-thread time goes. Left alone, every new feature (Fuel/Oil landed this week) defaults into the managed layer because it's easier.
2. **God classes:** `TransportBoardingCommandSystem` (4,022 lines, still growing), `BuildingSpawnCompositionSystemHelper` (1,824), `UiShellEcsGateway` (1,561). Decompose along their internal phase/state-machine seams before they calcify further.
3. **`Game.Runtime` monolith:** ~400 of 830 runtime files in one assembly (`Systems/` alone is 339 files). Compile-time and ownership costs grow quadratically with team/agent count. The contracts layering is already clean — extend it by splitting Systems into domain asmdefs (Combat, Buildings, Transport, Selection/Camera, Pathfinding).
4. **Content scale has no draw-call story on device:** Match.unity = 56 MB, 17k prefab instances, ~8k renderers under `Props` alone, all GameObjects (static batching on, dynamic off). SRP-batcher compatibility of the Synty material set and actual batch counts on Android are unverified. Distance-LOD placeholder materials render flat white/blue (visible if any zoomed-out mode ever ships).
5. **No perf regression gate in CI:** `performance_regression_contract.md` exists; Jenkinsfile runs builds+tests but no profiling. New gameplay code merges unmeasured.
6. **Day/night lighting floor:** night phase goes near-black (known since June 12) — readability and QA-screenshot hazard, plus full realtime shadowing runs all cycle.

---

## RECOMMENDATIONS

**Architecture**
- Freeze the managed-helper pattern for *new* systems: new gameplay logic lands as Burst ISystem + (if needed) a thin presentation helper. Enforce by review checklist + the existing `Tools/Architecture` scripts in CI.
- Give the helper layer a real contract: single `ITickable` registry with per-helper profiler markers (the `GameplayRuntimeUpdate.*` markers are a good start — make them universal) and a **0 B/frame allocation playmode test** (run 300 ScenarioLab frames, assert no managed allocs) so C1 can't regress silently.
- Migrate the top-3 measured helper hot paths (Selection, BuildingPlacement/transports, AttackVfx) into jobs/ISystem first — they carry most of the measured cost; don't boil the whole 309-file ocean.
- Split `Game.Runtime` by domain incrementally (one domain per PR); keep Contracts assemblies as the only cross-domain currency.

**Performance**
- Do the mobile URP tier pass (quick win #1) and capture **one Android baseline** the same week — a mid-tier device, 10-minute session per the targets doc, frame-time p95 + thermals. Everything else calibrates against that.
- Add a weekly (or per-merge) ScenarioLab headless capture to CI with budget assertions per the regression contract; fail on p95 breach or steady-state GC > 0.
- Then work the measured spike list in order: AttackVfx (42.8 ms), pathfinding spike frame (30 ms), production transports (25.9 ms), selection updates.

**Priority order if only three things happen:** (1) mobile URP settings + Android baseline capture, (2) GC re-baseline + zero-alloc gate on the composition layer, (3) Burst coverage pass. All three are days, not weeks, and each pays permanently.
