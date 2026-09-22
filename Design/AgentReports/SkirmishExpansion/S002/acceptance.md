# S002 acceptance scaffold (Regular Standard)

Catalog identity **S002** (Desert Base · Base Assault · Ground Maneuver · Established Base).
First visit remains Regular / Standard. The authored publication row may already
be **Playable** from Game View evidence. That row does not certify this ARIA
matrix, and this harness must not demote it. This file is a fill-in scaffold,
not a certified matrix. `runs.csv` stays header-only until a live match appends
a terminal row.

Pure Editor reducer tests do **not** replace normal-speed matches. Do not force
Victory, inject an army, skip gameplay, increase money, or call the victory
reducer to fill a counted run.

## How to fill `runs.csv`

Checked-in `runs.csv` holds only the mandated header. Append one row per
attempt after the match has actually finished. Do not pre-write Victory.

| Field | How to fill |
|---|---|
| `run_id` | Stable slot id from the matrix below (`S002-manual-rs-104731-en`, `S002-aria-rs-104731-en-1`, …). Failed seeds keep their row; do not reuse the id for a retry. |
| `catalog_id` | `S002` |
| `definition_version` | Integer from the compiler census (`1` for `skirmish.s002.v1`) |
| `code_hash` | Hex from `Tools/Warline/Skirmish/Capture S002 Regular Standard Census` (`[SkirmishAcceptanceCensus]`) |
| `config_hash` | Hex from the same census capture for that definition/size/difficulty/seed |
| `size` | `Standard` for first visit. War / Large War stay later slots. |
| `difficulty` | `Regular` for first visit. Recruit / Veteran / Commander stay later slots. |
| `seed` | Integer seed actually launched (see matrix) |
| `locale` | `en` or `fa-IR` |
| `device` | Device class that ran the match (Editor, Windows, Android, …). Leave empty until a real device run. |
| `executor` | `human` for manual victory; `aria` for Watch/Play samples; `fixture` for edge/recovery placeholders once those are recorded from live play |
| `normal_speed` | `1` only when the match ran at normal speed. Faster clocks fail the win gate. |
| `started_at` | ISO-8601 UTC when the live match started |
| `result` | Terminal outcome only: `Victory`, `Defeat`, `Draw`, `Abort`. Launch success is not a result. Forced or injected outcomes are invalid. |
| `end_reason` | Terminal reason (`MainBaseDestroyed`, `BothBasesDestroyed`, `Deadline`, `Surrender`, …) |
| `duration_seconds` | Elapsed match seconds at the terminal tick |
| `input_violations` | Count of illegal or hidden-control inputs. Counted ARIA wins require `0`. |
| `human_interventions` | Count of human tactical interventions. Counted ARIA wins require `0`. Manual rows may be greater than 0. |
| `trace_path` | Relative path to the retained trace under this folder |
| `log_path` | Relative path to the retained Editor or device log |

Use `SkirmishAcceptanceScaffold.FormatPendingRow` only to reserve a blank
result row. Never stamp `Victory` from a helper.

### ARIA harness (Windows Skirmish shadow)

Record the 18 ARIA slots from a live match. The cloud agent VM has no Unity.
Work on `D:\Projects\WarlineCapture-Skirmish`, not the shared checkout. Keep
Unity Hub open and signed in. Keep the Game View focused for the whole match:
ARIA stops when the Editor is unfocused or `timeScale` is not 1.

One cell at a time, from the open Editor:

| Seed | Locale | Menu | `-executeMethod` |
|---|---|---|---|
| 104731 | en | `Tools/Warline/Skirmish/Launch S002 ARIA Watch 104731 en` | `Game.Editor.SkirmishS002AriaRunHarness.Launch104731En` |
| 104731 | fa-IR | `Tools/Warline/Skirmish/Launch S002 ARIA Watch 104731 fa-IR` | `Game.Editor.SkirmishS002AriaRunHarness.Launch104731Fa` |
| 130365 | en | `Tools/Warline/Skirmish/Launch S002 ARIA Watch 130365 en` | `Game.Editor.SkirmishS002AriaRunHarness.Launch130365En` |
| 130365 | fa-IR | `Tools/Warline/Skirmish/Launch S002 ARIA Watch 130365 fa-IR` | `Game.Editor.SkirmishS002AriaRunHarness.Launch130365Fa` |
| 155923 | en | `Tools/Warline/Skirmish/Launch S002 ARIA Watch 155923 en` | `Game.Editor.SkirmishS002AriaRunHarness.Launch155923En` |
| 155923 | fa-IR | `Tools/Warline/Skirmish/Launch S002 ARIA Watch 155923 fa-IR` | `Game.Editor.SkirmishS002AriaRunHarness.Launch155923Fa` |

`LaunchFromEnvironment` reads `WARLINE_S002_SEED` and `WARLINE_S002_LOCALE`.
Each launch queues expanded S002 Regular Standard at `normal_speed=1`, starts
ARIA through the shipping touch driver, and writes a live trace under
`Design/AgentReports/SkirmishExpansion/S002/_Evidence/`. When the match
finishes, or the 1500s wall clock aborts, it copies that trace to
`s002-aria-rs-{seed}-{en|fa}-{n}.jsonl`, writes a sibling `-log.txt`, and
appends the next open slot. It never pre-writes Victory. Draw, Defeat, and
Abort stay in the file. Do not reuse a filled `run_id`. After three rows exist
for a seed and locale, the harness logs an error and does not invent a fourth id.

`Tools/CI/InvokeUnityExecuteMethodValidation.ps1` always passes `-quit`, so it
returns when `Launch*` schedules Play Mode and will not wait for the terminal
row. Use the menu, or call the executeMethod from the open Editor without
`-quit`. The wrapper is for the focused Editor suite below.

Census hashes on the row come from `SkirmishAcceptanceCensusCapture.TryCapture`
plus `FormatLog` for the launched seed (the same hash functions as
`Tools/Warline/Skirmish/Capture S002 Regular Standard Census`). The menu probe
still logs seed `104731` only and still refuses a Playable in-memory row. The
harness does not call `CaptureRegularStandard()` and does not change
publication status.

Focused proof that the log refuses a forced Victory and accepts only a
finished match outcome (temp csv; the checked-in `runs.csv` stays one line):

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/ResolveUnityEditor.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishS002AriaHarnessTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s002-aria-harness.log" `
  -RequiredPassMarker "[SkirmishS002AriaHarnessTests] result=Passed" `
  -GuiLicensing
```

The same suite also runs at the end of
`Game.Tests.Editor.SkirmishExpandedAriaTests.RunFocusedValidation`.

### Manual slot still empty

`S002-manual-rs-104731-en` is a human win. Launch
`Tools/Warline/Skirmish/Launch S002 Regular Standard Game View` (seed `104731`,
locale `en`, normal speed), destroy the original enemy Barracks while the
original player Barracks survives, and append that row by hand. The ARIA
harness must not fill it. A Draw is not a win.

## Required first-visit matrix

### Manual victory

One normal-speed human win on Regular Standard seed `104731` locale `en`.
Destroy the original enemy Barracks while the original player Barracks
survives. A Draw, launch success, or forced result fails the win gate.

| run_id | size | difficulty | seed | locale | executor | notes |
|---|---|---|---|---|---|---|
| `S002-manual-rs-104731-en` | Standard | Regular | 104731 | en | human | Required counted manual victory. Empty until a real win. |

### ARIA sample (Regular Standard)

From `Scenarios/DB_BA.md` § S002: Regular seeds `104731`, `130365`, `155923`
in both EN and FA at Standard. Three full normal-speed runs per locale, at
least two real wins in each, all traces and losses retained. ARIA uses visible
controls only. No hidden knowledge, state mutation, extra resources, human
tactical intervention, or restart in a counted win. A Draw is not a win.

| run_id | seed | locale | executor |
|---|---|---|---|
| `S002-aria-rs-104731-en-1` | 104731 | en | aria |
| `S002-aria-rs-104731-en-2` | 104731 | en | aria |
| `S002-aria-rs-104731-en-3` | 104731 | en | aria |
| `S002-aria-rs-104731-fa-1` | 104731 | fa-IR | aria |
| `S002-aria-rs-104731-fa-2` | 104731 | fa-IR | aria |
| `S002-aria-rs-104731-fa-3` | 104731 | fa-IR | aria |
| `S002-aria-rs-130365-en-1` | 130365 | en | aria |
| `S002-aria-rs-130365-en-2` | 130365 | en | aria |
| `S002-aria-rs-130365-en-3` | 130365 | en | aria |
| `S002-aria-rs-130365-fa-1` | 130365 | fa-IR | aria |
| `S002-aria-rs-130365-fa-2` | 130365 | fa-IR | aria |
| `S002-aria-rs-130365-fa-3` | 130365 | fa-IR | aria |
| `S002-aria-rs-155923-en-1` | 155923 | en | aria |
| `S002-aria-rs-155923-en-2` | 155923 | en | aria |
| `S002-aria-rs-155923-en-3` | 155923 | en | aria |
| `S002-aria-rs-155923-fa-1` | 155923 | fa-IR | aria |
| `S002-aria-rs-155923-fa-2` | 155923 | fa-IR | aria |
| `S002-aria-rs-155923-fa-3` | 155923 | fa-IR | aria |

`AriaPlayEditorValidation.AcceptExpandedPayload` and
`SkirmishExpandedAriaWatchValidation.AcceptExpandedPayload` accept the
immutable definition / size / difficulty / seed / locale payload and log the
selected configuration. They do not award Victory.

### Edge and recovery placeholders

These slots stay empty until a live match or a live recovery session produces
the evidence. Editor-only fixtures may inform the case, but they do not fill
`result`.

| run_id | kind | notes |
|---|---|---|
| `S002-edge-replacement-barracks` | edge | Replacement Barracks is not the designated victory base. |
| `S002-edge-same-tick-bases` | edge | Same-tick both designated bases dead is Draw. |
| `S002-edge-deadline-draw` | edge | Deadline with both original bases standing is Draw. |
| `S002-edge-field-army-wipe` | edge | Field-army wipe with a surviving producer is non-terminal. |
| `S002-edge-hidden-health` | edge | Hidden enemy health stays last-observed. |
| `S002-recovery-checkpoint` | recovery | Pause capture / restore on Regular Standard. |
| `S002-recovery-replay` | recovery | Replay is a new session with fresh Regular Standard stocks. |
| `S002-recovery-os-interrupt` | recovery | Device / OS interruption. Not claimed from Editor-only evidence. |
| `S002-device-review` | device | Camera / map / EN / FA / device HUD review. |

## Later samples (not this first-visit scaffold)

Do not treat these as certified because Regular Standard is still open.

- Additional exposed difficulties at Standard / EN: Recruit `196615`, Veteran `262149`, Commander `327675`.
- Additional exposed sizes: Regular EN and FA at War seed `393243` and Large War seed `458881`, plus their device and recovery gates.

## Compiler census and hashes

Run `Tools/Warline/Skirmish/Capture S002 Regular Standard Census` on the
Programmer 1 shadow. The probe compiles Regular Standard seed `104731` and
logs `code_hash`, `config_hash`, setup hash, and the starting-force census.
It does **not** mark Playable.

Copy the hashes into counted `runs.csv` rows. Recapture after any compiler,
layout, or roster change.

## Game View capture and guarded Playable flip

Programmer 1 captures live Game View on the Windows Skirmish shadow, then may
flip Playable when satisfied. Do not wait for another approval. Focused Editor
suites must not persist Playable.

1. `Tools/Warline/Skirmish/Launch S002 Regular Standard Game View` queues
   expanded Regular Standard seed `104731`, enters the match, dumps
   repo-root `_Evidence/s002-regular-standard-104731-playing.png` plus a
   sidecar (and a copy under this report folder), and stays in Play Mode.
   It does not inject an army or force Victory.
2. Play the match at normal speed if a counted manual win is still needed.
3. `Tools/Warline/Skirmish/Flip S002 Playable If Evidence Ready` writes
   Playable only when compiler hashes still match **and** the required
   evidence files exist. `EvaluateS002PlayableFlip` is a dry run (`playable=0`).

## Publication

Authored status remains **InProgress** until Programmer 1 runs the guarded
flip after Game View evidence. Library launch stays disabled until that write.
Asset existence, census capture, and this scaffold are not acceptance.
