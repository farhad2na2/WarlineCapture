# Windows lane — read-only performance diagnosis (2026-11-17)

Requested by the program lead after batch 2: Windows ARIA matches average 3–4 fps, while the Mac lane5-s002 run averages ~30.6 fps. No Unity, Editor preference, antivirus, power or system setting was changed for this diagnosis, and no Unity process was launched. Sources: the batch 1/2 logs under `D:\Projects\WarlineCapture-program-logs\2026-11-17\`, `Library/Bee`, the HKCU EditorPrefs registry key, and WMI/Get-Process.

## 1. Code optimization, Burst, Jobs safety

- **Editor C# is compiled in Debug code optimization.**
  - The Bee DAG is `Library/Bee/1300b0aEDbg.dag` (`EDbg` = Editor Debug). It is the only `.dag` in `Library/Bee/artifacts`.
  - `Library/Bee/artifacts/1300b0aEDbg.dag/Game.Simulation.rsp` contains `/optimize-`, `/debug:portable`, `-define:DEBUG` and `-define:ENABLE_UNITY_COLLECTIONS_CHECKS`.
  - EditorPrefs (`HKCU\Software\Unity Technologies\Unity Editor 5.x`): `ScriptDebugInfoEnabled = 1`, `AllowAttachedDebuggingOfEditor = 1`, `ScriptCompilationDuringPlay = 0`.
- **Burst:** `com.unity.burst@1.8.30` is installed and `zzzUnity.Burst.CodeGen.BurstILPostProcessor` runs on the game assemblies. `Library/BurstCache` is 2.6 GB and `Library/BurstCache/JIT` was last written 2026-11-17 15:59 (during batch 2), so Burst JIT compilation is active in the Editor.
  - There are no Burst-named EditorPrefs keys, so Enable Compilation, Safety Checks and Synchronous Compilation are at package defaults.
  - The logs have no Burst enable/disable or error lines.
- **Jobs:** there are no EditorPrefs keys for Jobs Debugger or Leak Detection, so these are at Editor defaults.
  - Batch-2 log line 247981: `Leak Detected : Persistent allocates 41 individual allocations. To find out more please enable 'Preferences > Jobs > Leak Detection Level > Enabled With Stack Trace' and reproduce the leak again.` This shows leak detection is enabled (without stack traces).
  - `ENABLE_UNITY_COLLECTIONS_CHECKS` is defined (see above).

## 2. Throttling and focus

- EditorPrefs: `InteractionMode = 1`, `ApplicationIdleTime = 0`. Idle time 0 means no idle frame throttling.
- The `focused=` field in `[PerfDiag]` lines:
  - batch 1: 252 × `focused=1`, 0 × `focused=0`;
  - batch 2: 1793 × `focused=1`, 0 × `focused=0`.
  - The Editor reported focus for every sample, so it was not minimized or unfocused.
- Render settings in every `[FrameRateDiag]` sample: `vSync=0 targetFps=-1`, `quality=1:Mobile`, `screen=1920x1080`. There is no frame cap.
- No `Interaction Mode`, `Run In Background` or focus-loss lines appear in either log.

## 3. Machine

- Model: SchenkerTechnologiesGmbH `XMG_APEX15_XAP15E20` (laptop with a desktop CPU). On AC: battery present, `BatteryStatus=2`, 100%.
- CPU: AMD Ryzen 9 3900 12-Core Processor, 12 cores / 24 logical processors. WMI `MaxClockSpeed` = `CurrentClockSpeed` = 3094 MHz at idle; load 2% at sampling.
- RAM: 31.9 GB. During batch 1, RAM was 8.8 GB free with the Editor at 5.0 GB working set.
- GPU: NVIDIA GeForce RTX 2070, driver 32.0.15.6081.
- Power plan: `powercfg /getactivescheme` → `Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced)`.
- Antivirus: Bitdefender (`bdservicehost` ×5, `bdredline` ×2, `bdagent`). Its CPU time is not readable (protected processes report 0). Microsoft Defender `AMRunningMode = Not running`.
- Background activity during the runs:
  - Windows Update was staging cumulative update KB5132060 (3.9 GB in `C:\Windows\SoftwareDistribution\Download`; CBS logs written 15:22–15:43).
  - Bitdefender VPN/Agent installers were updating in `C:\Program Files\Bitdefender*`.
  - OneDrive was updating itself.
- Top CPU consumers, sampled with no Unity Editor running (cumulative CPU seconds since boot at 14:56):
  - ChatGPT 495 s, claude 333 s, Unity Hub 248 s, ChatGPT 197 s, claude 186 s, explorer 173 s, claude 118 s, claude 100 s, OneDrive.Sync.Service 87 s, claude 76 s, codex 68 s.
  - This is not a sample taken during a run. A per-process sample during a match needs a Unity launch, which the lead has paused.

## 4. `cpuMain` by phase

The compile-check run has no `[FrameRateDiag]` lines (no Play Mode), so it cannot be compared directly. Per job and phase (`[FrameRateDiag]` averages):

| Run / job | phase | n | fps | cpuMain ms | updateAvg ms | gpu ms |
|---|---|---|---|---|---|---|
| batch 1 S002 104731 | match | 54 | 3.2 | 357 | 102 | 25.8 |
| batch 2 S004 104733 (warm-up) | match | 117 | 3.4 | 251 | 78 | 10.6 |
| batch 2 S002 130365 en | match | 124 | 3.9 | 224 | 74 | 12.3 |
| batch 2 S002 104731 en | match | 129 | 3.7 | 220 | 77 | 6.3 |
| batch 2 S002 130365 fa-IR | match | 43 | 4.0 | 236 | 76 | 7.1 |
| batch 2 S002 jobs | menu/load | 3 each | 0.4 | 59–64 | ~2850 | 31–37 |

- During a match, `cpuMain` (≈220–360 ms) is about 3× the game's measured `updateAvg` (≈75–100 ms). About 150–250 ms per frame of main-thread time falls outside the steps that `[PerfDiag]` names.
- `cpuRender` is about 1–2 ms and `gpu` 6–26 ms, so the frame rate is not GPU-bound.
- Scene scale in `[FrameRateDiag]`: `units=5024` in most match samples, tris ≈ 6–7.7 M, `quality=1:Mobile`.

## What I would change (not done; for the owner to decide)

1. **Code Optimization → Release** (Editor status-bar bug icon, or Preferences > General > Code Optimization On Startup = Release; this sets `ScriptDebugInfoEnabled=0`). Debug mode compiles every game assembly with `/optimize-` and `DEBUG`, which can make managed code several times slower. If the Mac Editor is in Release (its `Library/Bee/*.dag` would end in `ERel`), this is the most likely main cause of the 10× gap. Check the Mac's mode first so both lanes match.
2. **Windows power plan → High performance / Best performance power mode** while lanes run. Balanced on this laptop can hold clocks near the 3.09 GHz base under sustained load.
3. **Bitdefender exclusions** for `D:\Projects\WarlineCapture` (at least `Library`, `Temp`, `Logs`), `D:\Projects\WarlineCapture-program-logs`, and `D:\Program Files\Unity\6000.5.8f1\Editor\Unity.exe`. The harness writes large trace files each frame, and the Editor reads `Library` heavily.
4. **Pause OneDrive sync and Windows Update** during runs, or schedule runs after the KB5132060 install has finished and the machine has rebooted.
5. Keep Burst, Jobs safety checks and leak detection the same as the Mac rather than tuning them here. If the Mac Editor runs with Burst safety checks off, mirror that.

To confirm the cause after any change: one S002 job (with the lead's approval) with per-process CPU sampling every 10 s, comparing `[FrameRateDiag] cpuMain` and `updateAvg` against the table above.
