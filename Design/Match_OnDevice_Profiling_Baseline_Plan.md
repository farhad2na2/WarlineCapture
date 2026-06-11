# Match On-Device Profiling Baseline Plan

Date: 2026-06-11
Target: Android, 60 FPS (16.6 ms frame budget; plan against ~14 ms game budget to leave OS/driver headroom)
Status: Plan only — no code changes yet

## Goal

Produce a trustworthy, repeatable performance baseline of the Match scene on a real Android device, classify the bottleneck (CPU sim / CPU render / GPU / sync points / GC), and output a data-ranked optimization backlog. All existing measurements are editor offscreen renders and must not be compared against device numbers.

## Phase 0 — Preparation (~half day)

1. **Build config for profiling.** Development Build + Autoconnect Profiler, IL2CPP, **ARM64 only** (project currently enables ARMv7+ARM64; ARMv7 only adds noise and hurts Burst). Vulkan-first API order is already set — keep it, but log `SystemInfo.graphicsDeviceType` at boot to confirm the device didn't fall back to GLES3.
2. **Frame rate unlock.** Verify `Application.targetFrameRate = 60` (or display refresh) is set explicitly at boot. Unity on Android defaults to 30 — without this the entire baseline is invalid.
3. **Benchmark mode (only code addition required).** A boot flag / scriptable scenario that makes runs repeatable:
   - fixed RNG seed, fixed map, AI vs AI (existing AI control modes + balance probe infra can likely drive this),
   - scripted camera route (pan/zoom path with timestamps),
   - auto-CSV dump of the metrics below. The existing `PerformanceDiagnosticsSystem` already records draw calls, batches, SetPass, tris, and top system markers — extend its output to file + on-screen overlay instead of building anything new.
4. **Device sheet.** Record SoC (Adreno vs Mali decides GPU tooling), RAM, screen refresh rate, OS version, starting temperature.

## Phase 1 — Capture matrix (~1 day)

Run each scenario ≥3 times, cold device (let it cool between runs):

| # | Scenario | What it isolates |
|---|----------|------------------|
| S1 | Early game, camera still, few units | Best case / fixed overhead |
| S2 | Mid game, ~50% unit cap moving across map | Pathfinding + movement systems |
| S3 | Max battle: both factions at unit cap fighting | Combat, VFX, impostors, worst case |
| S4 | Continuous camera pan/zoom over full map | LOD churn, impostor switching, model spawn budget |
| S5 | HUD stress: large multi-selection, build drawer open, minimap | uGUI canvas rebuilds, UI query systems |
| S6 | S3 looped 15 min | Thermal throttling, FPS-over-time decay, battery temp |

Tooling per capture:

- **Unity Profiler over USB** (`adb forward`): main-thread breakdown (SimulationSystemGroup vs PresentationSystemGroup vs UI), job worker utilization, sync-point stalls (main-thread `ToEntityArray` copies will show as waits), GC alloc per frame.
- **Unity Frame Debugger (remote):** draw call composition — who owns the draw calls (terrain chunks, buildings, impostor batches, UI).
- **GPU profiler:** Android GPU Inspector (Adreno/Mali), or Snapdragon Profiler / Arm Performance Studio depending on chipset. RenderDoc for Android as fallback (Vulkan). Capture S1 and S3.
- **Unity Memory Profiler:** snapshot at S1 and S3 — texture/mesh footprint, managed heap growth.
- **Match scene load time** measured cold from app launch.

## Baseline sheet — metrics per scenario

- Frame time avg / p50 / p95 / p99; FPS-over-time curve (S6)
- Main thread ms: simulation systems (top 10 markers), presentation, UI canvas rebuild
- Render thread ms; draw calls / SetPass / batches / triangles
- GPU frame ms; fragment vs vertex bound; bandwidth/overdraw counters
- GC allocs per frame (target: 0 in steady state) and spike frequency
- Job worker occupancy %; count and cost of main-thread sync points
- Memory: total, textures, meshes, managed heap; APK size; scene load seconds

## Phase 2 — Analysis and decision (~half day)

Classify the dominant bottleneck and route to the matching backlog:

- **Main-thread sim bound** → Burst/job conversion of top-cost systems; eliminate `ToEntityArray` main-thread copies; batch structural changes via ECB.
- **Render thread / draw-call bound** → instancing/batching consolidation, impostor distance thresholds, building/terrain chunk merging.
- **GPU bound** → shadowmap resolution/distance, render scale, VFX overdraw, MSAA decision.
- **GC spikes** → strip runtime logging/string interpolation first (cheapest fix).
- **Thermal decay (S6)** → reduce sustained load: frame-rate cap strategy, LOD budget tiers.

Deliverable: `Design/AgentReports/<date>_perf_match-android-baseline.md` with the data tables, bottleneck verdict, and a ranked P1 backlog where every item cites its measured cost.

## Risks / notes

- 60 FPS for an RTS at unit cap on mid-range Android is ambitious; the baseline will show whether 60 is realistic everywhere or only outside max battles (adaptive target is a valid outcome).
- One device = one data point. If only one device is available, treat its tier as the calibration floor and re-verify on a second tier later.
- Don't deep-profile and measure in the same run — Deep Profile distorts timings; use it only for call-stack hunts after the normal capture flags a system.
