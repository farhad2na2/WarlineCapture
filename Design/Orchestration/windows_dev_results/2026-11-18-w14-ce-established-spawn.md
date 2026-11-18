# W14 — Convoy Escort established starts abort: player convoy truck BlockedSpawn (Windows, 2026-11-18)

- Branch `missions/win-w14-ce-established-spawn`, cut from `origin/missions/integration` `261993437`. Draft fix `64a71f5f2`; re-authored assets in the following commit. Data-only: no shared C# changed, no starting-force or stat changes, no map changes.
- Codex: `codex exec -m gpt-6.2-sol -c model_reasoning_effort=xhigh -s workspace-write`. Brief, transcript and report: `D:\Projects\WarlineCapture-program-logs\2026-11-18\codex\w14-ce-established-spawn.md`, `-transcript.log`, `-report.txt`. The worker made no commits; the Windows dev lead reviewed and committed.
- Logs: `D:\Projects\WarlineCapture-program-logs\2026-11-18\win-dev-unity\w14\{author,containment-S020,containment-S022,containment-S024,menu}\unity.log`.

## Root cause

- The three convoy bays `convoy.origin.1..3` are thirds of the 14×12 `spawn.vehicle.player` pad at [926,651.5].
- Established starts queue the opening combat and logistics vehicles on that pad before the objective trucks. Their reserved footprints left the first 3×3 convoy truck no legal cell in its bay: W6 `ce\unity.log:1491/7739/13898` `[SkirmishStartingForce] failed prefab=Unit_Veh_Truck_Tray faction=1 member=0`, then `BlockedSpawn`.
- Field starts have fewer vehicles and fit. FC/BT have no convoy bays.
- Worker trace: `SkirmishSetupCompiler.cs:299`, `SkirmishMapLayoutBuilder.cs:367`, `SkirmishScenarioSpawnSystem.cs:622`.
- Per the worker, S019 (CE-G-F template) has the same overlap and would fail the same way with an established roster on those pads. This was not run.

## Fix

- In S020/S022/S024 only, `spawn.vehicle.player` (anchor and pad) moves to [830,583.5] at 22×20 m. The convoy bays stay at [921.6..930.4, 649.9..653.1].
- [830,583.5] is the measured centre of the unrotated Barracks candidate at origin [816,576] in `Design/AgentReports/SkirmishExpansion/LayoutDumps/S002/dump.json` (candidates[2354], 28×15, level). The 22×20 pad extends about 2.5 m beyond that 15 m depth on each side. pad_preflight passes it against the same dump.
- New `Tools/Missions/test_desert_base_convoy_pads.py`: fails on the old drafts (`FAILED (failures=9)`), passes on the new ones (`OK`).

## Checks

- `check_draft.py`: exit 0, `PASS` for S020/S022/S024. `pad_preflight.py --dump <S002 dump>`: exit 0, `PREFLIGHT PASS` for all three. `test_drafts.py`: `PASS: 34 drafts ...`. `test_check_draft.py`: `OK`.
- AuthorFromDraftList (3 drafts): wrapper exit 0, `author\unity.log:2773` `[SkirmishMissionAuthoringBatch] result=Completed drafts=3 passed=3 failed=0`. Per row: `[SkirmishMissionAuthoring] result=Passed catalog=S020` (:2310), `S022` (:2523), `S024` (:2750).
- Containment: `[SkirmishInstancePadContainment] catalog=S020 result=Passed failures=0 rows=280` (`containment-S020\unity.log:686`); S022 (:684) and S024 (:683) the same. The check is envelope-only.
- Non-counted menu launch (`run_aria_queue.py --launcher windows --jobs S020:130365:en,S022:130365:en,S024:130365:en --menu-journey --validation-library --no-table`):
  - `[SkirmishMenuJourney] result=Passed` for S020 (`menu\unity.log:3104`), S022 (:9578), S024 (:15834).
  - No `expandedStartupFailure`, no `[SkirmishStartingForce] failed`, no `[StartingGrantPlacement] failed`.
  - :21293/:21308/:21323: S020 `result=Victory seconds=206.3`, S022 `Victory 196.6`, S024 `Victory 187.9`.
  - :21338: `[SkirmishAriaQueue] result=Completed jobs=3 recorded=3 wins=3`. These are not counted results; nothing was written to missions.csv.

## Lead's checks

- **Convoy trucks spawn in their bays, not at the new pad.** First actor snapshot (harness `actors.jsonl`, elapsed ≈9 s):
  - S020: `role.convoy_logic` at (921.5,653.5), (927.5,654.5), (930.5,649.5).
  - S022 and S024: (921.5,653.5), (925.5,650.5), (930.5,649.5).
  - All are inside the bay strip; none is on the new pad. The opening combat vehicles (car, APC, tank, tanker) are inside the new pad rect [819..841, 573.5..593.5]. The logistics trucks had already left it to haul.
- **No established structure on or next to the new pad.** The authored player instance pads in S020 (same in S022/S024) are 33–127 m from the pad rect (satellite dish 33.0 m, approach watchtower 33.5 m, fabrication depot 54.5 m, barracks 71 m, helipad 79 m, rest farther). That is beyond the 12 m relocation slack, so no starting structure can be relocated onto it. Placed building origins are not in the log; the menu launch found no placement failure and all vehicles placed. Not checked: the compiler-derived second refinery module pad (War/LargeWar only; this launch was Standard).

## Open questions

- S019 (CE-G-F, Mac-authored) keeps the old overlap. It starts today because its field roster fits, but any established use of those pads would hit the same failure.
- The 22×20 pad extends about 2.5 m past the measured Barracks rect on its short sides; pad_preflight finds clear vehicle cells inside it.
