# Windows lane — read-only Editor-overhead diagnosis (2026-11-17)

Requested by the program lead after batch 3. The question is why the Release S004 match still spends ~90 ms per frame of `cpuMain` (108 ms) outside the game's measured `updateAvg` (18 ms). Nothing was changed and no Unity process was launched. Branch `missions/windows-lane` at `e3b1a0f78`.

Local `.meta` copies that Unity generated for `SkirmishTerminalFrozenState.cs` and `SkirmishTerminalFrozenStateTests.cs` (GUIDs 97d76e1e… and f01f0866…) were moved to `D:\Projects\WarlineCapture-program-logs\2026-11-17\windows-batch3-queue\local-generated-meta\` before merging. The committed GUIDs now win: `93f5eb1d2680a45109c286f6a9d81e96` and `d75cbf597bde242c1bbc125327358df7`.

## 1. Editor windows during Play

Source: `UserSettings/Layouts/default-6000.dwlt`, last written 2026-11-17 16:49 when batch 3's Editor exited. The front tab of each dock area is marked *.

| Dock area (w × h) | Tabs |
|---|---|
| 170 × 314 | **\*Hierarchy** (GameObject) ; Entities Hierarchy |
| 586 × 314 | Scene ; **\*Game** ; Test Runner |
| 757 × 145 | **\*Project** ; Console ; Animation ; Animator |
| 223 × 459 | **\*Inspector** ; Skinned Prefab Combiner ; Animator Baker |

- Visible during Play: GameObject Hierarchy, Game, Project, Inspector.
- Docked but behind another tab: Scene view, Console, Entities Hierarchy.
- Not in the layout: Profiler and Frame Debugger.
- What the Inspector had selected during the matches is not recorded in the layout.

## 2. Game view

- `m_TargetSize: {x: 1920, y: 1080}`, shown in a 586 × 314 panel at `m_Scale: 0.30949074`. The log line `[MainMenuV3PrefabBuilder] gameView=1920x1080` confirms the target size.
- `m_VSyncEnabled: 0`, `m_Stats: 0`, `m_Gizmos: 0`, `m_ShowGizmos: 0`, `m_LowResolutionForAspectRatios: 01…`.
- `[FrameRateDiag]` render settings: `quality=1:Mobile`, `gpu` 8.3 ms mean in the Release S004 match. Rendering does not account for the gap.

## 3. Logging volume during a match

Counted between `[SkirmishGateUnits] event=available` and `event=used` of each match:

| Match | log lines | MB | log entries (`(Filename:` markers) | stack-frame lines | `[AriaArmyOrderTrace]` / `[AriaS002Trace]` | wall s | lines/s | entries/s | MB/min |
|---|---|---|---|---|---|---|---|---|---|
| Batch 3 S004 104733 (Release) | 129,801 | 18.1 | 5,192 | 51,704 | 5,149 | 213.9 | 607 | 24 | 5.07 |
| Batch 2 S004 104733 (Debug) | 77,774 | 11.0 | 3,067 | 30,448 | 3,022 | 254.4 | 306 | 12 | 2.59 |
| Batch 2 S002 130365 en (Debug) | 74,875 | 10.6 | 2,988 | 29,049 | 2,963 | 265.0 | 283 | 11 | 2.40 |

- In the Release S004 match, `[AriaArmyOrderTrace]` makes up 5,147 of the 5,192 log entries. That is about 1.7 entries per rendered frame (≈2,950 frames at 13.8 fps). Other prefixes: `[FrameRateDiag]` 104, `[PerfDiag]` 98, `[FreezeDetect]` 61, `[AudioDiag]` 23, `[RenderSceneDiag]` 20.
- Each entry carries a managed stack trace of about 10 frames. `ProjectSettings/ProjectSettings.asset` line 59: `m_StackTraceTypes: 010000000100000001000000010000000100000001000000`, i.e. ScriptOnly for all log types. This is a project setting, so it is the same on the Mac.
- `[AriaArmyOrderTrace]` rate: Release ≈1.7 per frame / 24 per s; Debug ≈3.5 per frame (3,022 over ≈865 frames at 3.4 fps) / 12 per s. It is neither constant per frame nor constant per second.

## 4. Editor.log writes

- `-logFile` is `D:\Projects\WarlineCapture-program-logs\…\unity.log`. C: and D: are partitions of the same `NVMe Samsung SSD 970` (932 GB).
- Growth during a Release match is 5.07 MB/min, about 24 entries/s. As raw I/O this is negligible for an NVMe drive.
- Whether Unity flushes each line synchronously, and whether Bitdefender scans each write, cannot be determined from logs. It needs a sampled run.

## Candidates for the remaining ~90 ms/frame (not changed; for the lead/owner)

1. **Per-frame `[AriaArmyOrderTrace]` logging with stack traces.** About 1.7 `Debug.Log` calls per frame, each capturing a ~10-frame managed stack trace, and every entry goes through the Editor Console's entry store even while the Console tab is hidden. Compare `[AriaArmyOrderTrace]` entries per second and per frame in the Mac lane5-s002 log. If the Mac logs a similar volume at 30 fps, logging is less likely to be the main cost here.
2. **GameObject Hierarchy and Inspector visible during Play.** The Editor repaints them while the scene changes (5,024 units in `[FrameRateDiag]`). The Mac layout may differ; the Scene view and Entities Hierarchy were not in front here.
3. **Bitdefender on-access scanning** of `Library`, `Temp` and the 5 MB/min log, and **Balanced** power plan clocks (see `2026-11-17-1631-perf-diagnosis.md`). Both are the owner's decision.

A sampled run (Unity's Profiler, or per-process CPU sampling) would split these apart. It needs a Unity launch, which is on hold.
