# S003 acceptance scaffold (Regular Standard)

Catalog identity **S003** (Desert Base · Base Assault · Air Mobile · Field Base).
First visit remains Regular / Standard. Seed sample: `104732` (also `130366`, `155924`).
This file is a fill-in scaffold, not a certified matrix. It does not mark the
mission complete and it does not mark the AriaWon matrix complete. `runs.csv`
stays header-only until a live match appends a terminal row.

The watch reuses the S002 driver (`SkirmishExpandedAriaWatchDriver`). There is
no second Watch path. Live AriaWon for S003 waits until after the S002 Windows
proof and until the Skirmish Editor lock is free.

Pure Editor reducer tests do **not** replace normal-speed matches. Do not force
Victory, inject an army, skip gameplay, increase money, or call the victory
reducer to fill a counted run.

## How to fill `runs.csv`

Checked-in `runs.csv` holds only the mandated header. Append one row per
attempt after the match has actually finished, or after the harness aborts.
Do not pre-write Victory.

| Field | How to fill |
|---|---|
| `run_id` | Stable slot id (`S003-aria-rs-104732-en-1`, …). Failed seeds keep their row; do not reuse the id for a retry. |
| `catalog_id` | `S003` |
| `definition_version` | Integer from the compiler census (`1` for `skirmish.s003`) |
| `code_hash` | Hex from the census captured for that definition, size, difficulty, and seed |
| `config_hash` | Hex from the same census capture |
| `size` | `Standard` for first visit |
| `difficulty` | `Regular` for first visit |
| `seed` | Integer seed actually launched |
| `locale` | `en` or `fa-IR` |
| `device` | Device class that ran the match. Leave empty until a real device run. |
| `executor` | `aria` for these watch samples |
| `normal_speed` | `1` only when the match ran at normal speed |
| `started_at` | ISO-8601 UTC when the live match started |
| `result` | `Victory`, `Defeat`, `Draw`, or `Abort`. Launch success is not a result. |
| `end_reason` | Terminal reason, or `simulationNotAdvancing` when the clock stays stuck |
| `duration_seconds` | Elapsed match seconds at the terminal tick |
| `input_violations` | Counted ARIA wins require `0` |
| `human_interventions` | Counted ARIA wins require `0` |
| `trace_path` | Relative path under `Design/AgentReports/SkirmishExpansion/S003/_Evidence/` |
| `log_path` | Relative path to the sibling log |

A counted win requires a finished match whose outcome is Victory, normal speed,
no input violations, and no human interventions. Forced Victory is refused.
An unfinished match is refused. An abort is stored as `Abort` and is not a win.

### ARIA harness (Windows Skirmish shadow)

Record slots from a live match. The cloud agent VM has no Unity. Work on
`D:\Projects\WarlineCapture-Skirmish`, not the shared checkout. Keep Unity Hub
open and signed in. Keep the Game View focused for the whole match: ARIA stops
when the Editor is unfocused or `timeScale` is not 1.

Do not start these launches while Programmer 1 still holds the Skirmish Editor
for the S002 proof.

One cell at a time, from the open Editor. English is the first locale. fa-IR
entries use the same driver.

| Seed | Locale | Menu | `-executeMethod` |
|---|---|---|---|
| 104732 | en | `Tools/Warline/Skirmish/Launch S003 ARIA Watch 104732 en` | `Game.Editor.SkirmishS003AriaRunHarness.Launch104732En` |
| 104732 | fa-IR | `Tools/Warline/Skirmish/Launch S003 ARIA Watch 104732 fa-IR` | `Game.Editor.SkirmishS003AriaRunHarness.Launch104732Fa` |
| 130366 | en | `Tools/Warline/Skirmish/Launch S003 ARIA Watch 130366 en` | `Game.Editor.SkirmishS003AriaRunHarness.Launch130366En` |
| 130366 | fa-IR | `Tools/Warline/Skirmish/Launch S003 ARIA Watch 130366 fa-IR` | `Game.Editor.SkirmishS003AriaRunHarness.Launch130366Fa` |
| 155924 | en | `Tools/Warline/Skirmish/Launch S003 ARIA Watch 155924 en` | `Game.Editor.SkirmishS003AriaRunHarness.Launch155924En` |
| 155924 | fa-IR | `Tools/Warline/Skirmish/Launch S003 ARIA Watch 155924 fa-IR` | `Game.Editor.SkirmishS003AriaRunHarness.Launch155924Fa` |

`LaunchFromEnvironment` reads `WARLINE_S003_SEED` and `WARLINE_S003_LOCALE`.
Each launch queues expanded S003 Regular Standard at `normal_speed=1`, starts
ARIA through the shipping touch driver, and writes a live trace under
`Design/AgentReports/SkirmishExpansion/S003/_Evidence/`. When the match
finishes, or the watch aborts, it copies that trace to
`s003-aria-rs-{seed}-{en|fa}-{n}.jsonl`, writes a sibling `-log.txt`, and
appends the next open slot. It never pre-writes Victory. Draw, Defeat, and
Abort stay in the file. Do not reuse a filled `run_id`. After three rows exist
for a seed and locale, the harness logs an error and does not invent a fourth id.

After Playing, the watch aborts with `end_reason=simulationNotAdvancing` if
simulation is still inactive or match elapsed is still 0 for about 45 seconds.
That abort is not a win. It replaces the 1500 second wall-clock wait for a
stuck-zero clock. The wall-clock budget remains for a match whose clock is
actually advancing.

`Tools/CI/InvokeUnityExecuteMethodValidation.ps1` always passes `-quit`, so it
returns when `Launch*` schedules Play Mode and will not wait for the terminal
row. Use the menu, or call the executeMethod from the open Editor without
`-quit`. The wrapper is for the focused Editor suite below.

Census hashes on the row come from a Regular Standard compile of the launched
seed (the same hash functions as the S003 census capture). The harness does
not flip publication status.

### Windows re-run when the Skirmish lock is free

Keep Hub signed in. Open `D:\Projects\WarlineCapture-Skirmish` on this branch
tip, focus the Game View, then either:

- Menu: `Tools/Warline/Skirmish/Launch S003 ARIA Watch 104732 en`
- Or `Game.Editor.SkirmishS003AriaRunHarness.Launch104732En` from the open Editor, with no `-quit`

Expect trace lines with rising `elapsed` / `clockElapsed` and
`simulationActive=1` within about 45 seconds of Playing. A stuck-zero clock
must Abort with `simulationNotAdvancing`. Do not stamp Victory by hand.

Focused proof that the log refuses a forced Victory and accepts only a
finished match outcome (temp csv; the checked-in `runs.csv` stays one line).
This suite does not play a match, so `-quit` on the wrapper is expected:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/ResolveUnityEditor.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishS003AriaHarnessTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s003-aria-harness.log" `
  -RequiredPassMarker "[SkirmishS003AriaHarnessTests] result=Passed" `
  -GuiLicensing
```

The same suite also runs at the end of
`Game.Tests.Editor.SkirmishExpandedAriaTests.RunFocusedValidation`.

## Required first-visit ARIA sample

From the S003 Regular Standard seeds `104732`, `130366`, `155924` in both EN
and FA at Standard. Three full normal-speed runs per locale. Empty until a
live match appends the row. A Draw is not a win. The manual Game View slot is
not filled by this harness.

| run_id | seed | locale | executor |
|---|---|---|---|
| `S003-aria-rs-104732-en-1` | 104732 | en | aria |
| `S003-aria-rs-104732-en-2` | 104732 | en | aria |
| `S003-aria-rs-104732-en-3` | 104732 | en | aria |
| `S003-aria-rs-104732-fa-1` | 104732 | fa-IR | aria |
| `S003-aria-rs-104732-fa-2` | 104732 | fa-IR | aria |
| `S003-aria-rs-104732-fa-3` | 104732 | fa-IR | aria |
| `S003-aria-rs-130366-en-1` | 130366 | en | aria |
| `S003-aria-rs-130366-en-2` | 130366 | en | aria |
| `S003-aria-rs-130366-en-3` | 130366 | en | aria |
| `S003-aria-rs-130366-fa-1` | 130366 | fa-IR | aria |
| `S003-aria-rs-130366-fa-2` | 130366 | fa-IR | aria |
| `S003-aria-rs-130366-fa-3` | 130366 | fa-IR | aria |
| `S003-aria-rs-155924-en-1` | 155924 | en | aria |
| `S003-aria-rs-155924-en-2` | 155924 | en | aria |
| `S003-aria-rs-155924-en-3` | 155924 | en | aria |
| `S003-aria-rs-155924-fa-1` | 155924 | fa-IR | aria |
| `S003-aria-rs-155924-fa-2` | 155924 | fa-IR | aria |
| `S003-aria-rs-155924-fa-3` | 155924 | fa-IR | aria |

`AriaPlayEditorValidation.AcceptExpandedPayload` and
`SkirmishExpandedAriaWatchValidation.AcceptExpandedPayload` accept the
immutable payload and log the selected configuration. They do not award Victory.

Edge, recovery, and device slots are not opened on this branch.
