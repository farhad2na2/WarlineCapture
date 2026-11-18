# Lane integration 33 — 2026-11-18 (lane worker 33)

Brief: `WarlineCapture-program-logs/briefs/lane-integration33-2026-11-18.md`. Logs: `WarlineCapture-program-logs/2026-11-18/lane33/`.
This was a Mac native check of the 15 Windows-authored Desert Base rows (W6). No asset or code changes were made. No counted S002 matches were run.

Startup: no Editor owned WarlineCapture. Only the Fig-SquadPlay Editors were running, and they were left alone. There was no `Library/LibraryInitializing`, and disk_guard returned 0 before every launch.
Every launch passed the startup check (`Library Redirect Path` at line 92-93).

## 1. Compile and Python
- Compile was clean on 652e88722: `[SkirmishNativeRenderingDiagnosticSerialization] result=Passed` (compile1.log:753), 0 `error CS`, exit 0, Editor lifetime 47 s.
- Python: 149 OK (python.txt:33). A first attempt was run from `Tools/Missions` by mistake and gave 2 import errors. That was a bad invocation, not a test failure.

## 2. Containment: 15/15 Passed on the Mac
- One launch per draft, because `RunAuthoredLayoutValidation` reads a single `WARLINE_MISSION_DRAFT`. No `-quit` was used.
- Each Editor was stopped by its own verified PID after its marker, at 194-198 s.
- Row counts match Windows: 256 (S008/S010/S012), 168 (S009/S011), 280 (S014/S016/S018/S020/S022/S024), 192 (S015/S017/S021/S023).
- Marker lines: contain-S0xx.log:674-803.
- The check covers the envelope only: `note=envelope-only-does-not-prove-scenery-clearance`.

## 3. FC queue S008-S012 (130365 en), queue rc=0, 2/5 wins
Run with `--validation-library` added to the brief's command. Planned rows have menu cards only with that flag, which is how Windows ran W6. `--no-table` was used.

| Job | Result | Clock (% of 1200 s) | Zones own/opp | Tickets | Hold choices | Log |
|---|---|---|---|---|---|---|
| S008 FC-G-E | Defeat TicketsDepleted | 614.8 s (51.24%) | 1/2 | 0/486 | 0 | q/unity.log:11332 |
| S009 FC-A-F | Victory TicketsDepleted | 591.2 s (49.27%) | 2/1 | 500/0 | 9 | :20623 |
| S010 FC-A-E | Defeat TicketsDepleted | 863.2 s (71.93%) | 0/3 | 0/237 | 5 | :36088 |
| S011 FC-C-F | Victory TicketsDepleted | 591.0 s (49.25%) | 2/1 | 500/0 | 16 | :44975 |
| S012 FC-C-E | Defeat TicketsDepleted | 617.8 s (51.48%) | 1/2 | 0/482 | 0 | :54188 |

- MenuJourney and the terminal probe (MAIN MENU) passed on all 5 jobs.
- The whole log has 0 `[AriaBlocked]`/`[AriaBlockedControl]`, 0 `[StartingGrantPlacement] failed` and 0 startupFailure. `sourceUnchanged=1` (:54728).
- The outcomes match the Windows non-counted runs: field starts win and established starts lose. So the Windows defeats were not a low-fps effect.
- On the losing established rows (S008, S012), ARIA never chose Hold. It only moved to zones (Move ControlZone) and observed (None ObserveObjective). It held 1 zone against 2 and lost on tickets at about 51%.
- Gate-3 `unused=` is in STATUS. The air and combined rows leave airport, helipad, fighter, strike and attack helicopters unused.
- The harness appended `runs.csv` rows: countedWin=1 for S009/S011 and 0 for the others. Whether those count is the lead's decision. The rows stay planned.
- About 200 MB of harness evidence under `Design/AgentReports/SkirmishExpansion/S008..S012/_Evidence/` is left untracked. It was not committed, as in lanes 31/32.

## Open
- FC established starts (S008/S010/S012) lose on the Mac at full speed. ARIA's FC skill does not hold zones there. This needs a look at objective tactics or balance (owner direction 2026-11-06 allows starting-force tuning) before counted FC runs.
- BT/CE rows (S014-S018, S020-S024) have Mac containment only. No Mac match has been run for them.
