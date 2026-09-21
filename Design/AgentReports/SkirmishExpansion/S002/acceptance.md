# S002 acceptance scaffold (Regular Standard)

Catalog identity **S002** (Desert Base · Base Assault · Ground Maneuver · Established Base).
First visit remains Regular / Standard. Publication stays **InProgress**. This
file is a fill-in scaffold, not a certified matrix.

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
