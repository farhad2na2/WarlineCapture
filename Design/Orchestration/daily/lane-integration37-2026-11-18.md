# Lane worker 37, 2026-11-18: first counted Base Assault runs S001, S003, S025, S049

HEAD 4efa772f8, branch missions/integration. Logs: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-18/lane37/`.
No asset or code changes. All queues ran with `--menu-journey --no-table`; disk_guard 0 before every launch; one Editor at a time.

| Step | Result | Evidence |
|---|---|---|
| 1 compile | Passed, 0 error CS, exit 0 | compile1.log:508; python 153 OK python.txt:33-35 |
| 2 smoke | S049 Victory 276.8 s (25.63%); S001/S003/S025 Abort at dispatch | smoke/unity.log:280211 |
| 2 S001/S025 retry `--validation-library` | same Abort (no expansion definition) | smoke-vl/unity.log:618, :900, :1144 |
| 2 S003 at coded seed 130366 | Defeat MainBaseDestroyed 185.4 s (17.16%) | smoke-s003/unity.log:477593 |
| 3 S049 counted | 155923 en Abort (101), 104731 en Abort (101), 130365 fa-IR Victory 263.9 s (24.43%) | counted/unity.log:382941 |

## Findings
- S001, S025: `Catalog S00x does not resolve to a definition in the expansion catalog` (smoke :601, :1064). No
  `SkirmishScenario_S001/S025` asset exists; they are coded legacy presets (index <= 7) the generic queue path cannot launch,
  and the validation library does not list them (smoke-vl :604). Failed twice; needs a dispatch path or an authored definition.
- S003: the brief's seed 130365 is rejected (`First-visit ARIA seed must be 104732, 130366 or 155924.`, smoke :883). Re-ran
  once at 130366 (deviation from the brief, flagged). Defeat: player base 800 hp at first base_alert t=162.8 (:411875),
  778 at t=174.5 (:442646), homeDefenseStage=0 throughout, destroyed at 185.4 s with enemy base 800 untouched (:477150).
  Not re-run. S003 counted seeds would be 155924 / 104732 / 130366 fa-IR.
- S049: MenuJourney Passed via TheaterPin_MP>ObjectiveTab_BA>BattleCard_S049 without `--validation-library`
  (harnessLibrary=1 published=0, smoke :1329). Two wins (130365 en, 130365 fa-IR), both terminal probe Passed MAIN MENU
  interventions=0 (smoke :279963, counted :382708). Two aborts `ariaStopped:Blocked` reason=101 kind=Waiting offered=0 at
  breacherStage=2 (counted :88940 intent=Inspect, :269540 intent=Recruit). Gate-3 unused on wins: helipad, satellite_dish,
  apc_heavy, drone, radar, siege, tank, transport_heli (+gunner on en, +apc_fast on fa-IR).
- 0 [StartingGrantPlacement] failed, 0 startupFailure, 0 BlockedSpawn, 0 error CS in every log.

## Open items
- S049 reason-101 ARIA stall (2 of 4 seeds) blocks counting S049.
- S001/S025 need a queue dispatch for legacy presets before any Mac run.
- S003 Defeat: ARIA does not defend its base on 130366; needs diagnosis before counted seeds.
- Whether the runs.csv rows count is the lead's call; rows stay planned.
