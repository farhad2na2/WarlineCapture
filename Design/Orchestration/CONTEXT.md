# Program context — stable facts (read this instead of re-deriving)

Updated 2026-11-16. Keep under 60 lines. Detail: `ORCHESTRATOR_PROMPT.md`, the mission plan, `AGENTS.md`.

## Where things are

- Ledger: `Design/Orchestration/missions.csv` (180 rows). Daily reviews: `Design/Orchestration/daily/`.
- Program logs (outside the repo, durable): `/Volumes/T7/Projects/WarlineCapture-program-logs/<date>/`.
- Branches: `missions/snapshot-2026-11-16` (pre-program state), `missions/integration` (work lands here).
- Harness: `Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.cs` and its partials.
- Catalog and budgets: `Design/Roadmap/Skirmish_Expansion/SCENARIO_CATALOG.csv`,
  `INITIAL_SETUP_MATRIX.csv` (must stay byte-identical to
  `Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv`).

## Unity lane (one Editor only)

- Always `Tools/CI/invoke_unity_macos.sh`; never `-batchmode`; Unity Hub must be open (exit 65 if not).
- **Never rebuild the Library (owner rule, 2026-11-16).** Do not delete or clear `Library`, do not open
  the project from a worktree or second checkout, and do not switch the main checkout between
  branches that swap many assets. Worktrees are for authoring only; Unity runs on the main checkout.
- Compile check (about 1–2 min):
  `invoke_unity_macos.sh --timeout 900 --log <log> -- -quit -executeMethod Game.Editor.SkirmishS002AriaRunHarness.RunNativeRenderingDiagnosticSerializationValidation`
  Pass = exit 0, marker `[SkirmishNativeRenderingDiagnosticSerialization] result=Passed`, no `error CS`.
- Single ARIA run (no `-quit`):
  `WARLINE_SKIRMISH_CATALOG=S002 WARLINE_S002_SEED=130365 WARLINE_S002_LOCALE=en invoke_unity_macos.sh --timeout 1800 --log <log> -- -executeMethod Game.Editor.SkirmishS002AriaRunHarness.RunFocusedAriaAndExit`
  Pass = exit 0 and `[SkirmishS002AriaRun] appended=1 countedWin=1`. Exit 2 = loss or abort.
  Seeds used historically: 104731, 130365, 155923. Locales: `en`, `fa-IR`. Size and difficulty are
  fixed to Standard / Regular in this entry point.
- The run appends a row to `Design/AgentReports/SkirmishExpansion/<ID>/_Evidence/candidates/<source hash>/runs.csv`
  (the top-level `<ID>/runs.csv` is the September record) and writes a trace under `_Evidence/`.
- **Startup check (2026-11-17):** within 3 minutes of every launch the log must contain
  `Library Redirect Path`. If it does not, the Editor is sitting on a startup dialog: report it at
  once, do not wait for the timeout, and do not kill it. Never kill an Editor during its first
  3 minutes: that leaves the marker `Library/LibraryInitializing` behind and the next launch stops
  on "Corrupted Library Detected" (the answer is always "Don't rebuild Library"; only the owner can
  click it or remove the marker). Before each launch check the marker does not exist.
- Source-hash guard: the harness hashes sources at start and end (about 30 s each). Any change under
  `Assets`, `Packages` or `ProjectSettings` during a run aborts it (`candidateSourceChangedDuringRun`).
  **No worker may write to the main checkout while the Unity lane is busy.** Workers use worktrees.
- Batch runner: `RunProductionBatchCanariesFromEnvironment` (`…ProductionBatch.cs:222`), descriptor path
  in `WARLINE_SKIRMISH_PRODUCTION_BATCH`. Its only launcher is Windows
  (`Tools/CI/invoke_skirmish_production_batch.py`) and the descriptor needs hash-pinned job, claim and
  manifest files. A macOS path does not exist yet.
- `--reuse` (persistent Editor) cannot pass environment variables to the open Editor, so catalog, seed
  and locale cannot be chosen per run through it today.

- After any change to authored scenario assets, the first match of the next Editor may re-import the
  SubScene for ~300 s and abort with `startupFailure=Timeout` (Codex 027, 2026-11-17). Put a throwaway
  warm-up job (e.g. `S002:130365:en`) first in a counted queue after authoring; it does not count.

## Disk (agents manage it; never ask the owner)

- `Tools/Missions/disk_guard.sh` before every Unity launch, Codex batch and after commits. Exit 2
  (below 30 GB free) = start nothing; exit 1 (below 80 GB) = find and fix what is growing.
- Git auto maintenance stays off (`maintenance.auto=false`, `gc.auto=0`); no `git gc`/`repack` by agents.

## Measured costs

- Editor launch to quit: median 64 s. Focused ARIA run, whole Editor lifetime: 128–390 s.
- Historical ARIA win times (Standard/Regular): S002 99 s, S004 149 s, S005 232 s, S003 297 s.

## Mission model

- 120 Skirmish = 5 maps × 4 objectives × 3 army profiles × 2 starts; 20 map-and-objective layouts.
- ARIA drives the game with synthetic touches on the real UI and camera; it has one Base Assault
  skill. It is paced on unscaled time, so do not rely on time acceleration.
- Only Base Assault has ever been played in Unity. Assets exist for S001–S005, S025, S073 (S049 staged).

## Known defects and open risks

- Every historical win predates three weeks of gameplay edits; nothing is verified on the current tree.
- S073 ends in a draw; S005 fails on current source; S002 had an "empty viewport" report in October.
- `rtk rg` fails to spawn in this environment; use `rtk proxy grep`.

## Token budget

- Daily review: 15 minutes, deltas only. Worker briefs: one deliverable, report in 10 lines or fewer.
- Provisional daily ceiling: 6 worker dispatches plus one review per 4-hour cycle. Revise after week 1.
