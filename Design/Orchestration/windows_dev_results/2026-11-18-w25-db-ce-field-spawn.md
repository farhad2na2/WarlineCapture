# Windows dev results — W25 S021/S023 (Desert Base CE field) truck BlockedSpawn on seed 155923 (2026-11-19)

Branch `missions/win-w25-db-ce-field-spawn` (from integration `cf2d006cd`); merged with integration `491c7b76d` (`90f0f167a`, no conflicts).
The Windows dev lead did this task. Unity: Windows Editor 6000.5.8f1, `-GuiLicensing`, main checkout detached at the commits named below. Logs under `win-dev-unity\w25-repro\`, `w25-db-ce-field\` and `w25-db-ce-field-seeds\`.

**Outcome: fixed with draft data.** S021 and S023 now start and win on seeds 155923, 130365 and 104731 (6/6 Victory). `expandedStartupFailure` 0, and every row returns to the result screen.

## 1. Reproduced on Windows
- The Mac lane-41 log is not on this machine, so I reproduced on integration `cf2d006cd` with one non-counted menu launch each (S021 and S023, seed 155923, en; `win-dev-unity\w25-repro\menu\unity.log`).
- Both rows logged `[SkirmishStartingForce] failed prefab=Unit_Veh_Truck_Tray faction=1 member=0` (:1970, :3402), then `aborted=True reason=expandedStartupFailure=BlockedSpawn`. That matches lane 41.

## 2. Cause
- **Shared space.** In the field rows, the player vehicle pad `spawn.vehicle.player` (926,651.5, 14×12) contains all three convoy bays: `convoy.origin.1..3` at x 921.6–930.4, z 649.9–653.1, each 4.6×12.
- **How it fails.** The opening combat and logistics vehicles are placed on that pad before the objective trucks, each at a seeded spot. On seed 155923 a vehicle's footprint takes cells in bay 1, and the first objective truck has no legal cell left in its bay.
- **Why other seeds pass.** The field roster has fewer vehicles than the established one, so most seeds fit.
- **Same as W14.** This is the failure W14 fixed for the established rows S020/S022/S024 (`2026-11-18-w14-ce-established-spawn.md`) by moving their vehicle pad. The field rows were left as they were.

## 3. Fix `ae7034004` → assets `441657fb4`
- S021 and S023: the `spawn.vehicle.player` anchor and pad move from (926,651.5) 14×12 to **(830,583.5) 22×20**. This is the W14 position: the centre of the level, unrotated Barracks candidate at origin (816,576) in the S002 layout dump.
- The convoy bays are unchanged.
- A note was appended to each draft.
- `pad_preflight.py` (S002 dump) 2/2 PASS, `check_draft.py` 2/2 PASS. `AuthorFromDraftList` → `result=Completed drafts=2 passed=2 failed=0`. Containment `result=Passed failures=0` 2/2 (rows 192).

## 4. Menu launches (NON-counted; `run_aria_queue.py --launcher windows --menu-journey --validation-library --no-table`, Standard Regular, en)
The runner labels each row `class=counted`; these are not counted runs.

| Row | Seed | Result | match s | deadline % | delivered/lost | return | Before |
|---|---|---|---|---|---|---|---|
| S021 air field | 155923 | **Victory** ConvoyDelivered | 114.8 | 10.63 | 2/2, 0 | yes | Abort BlockedSpawn |
| S023 combined field | 155923 | **Victory** ConvoyDelivered | 146.5 | 13.57 | 2/2, 0 | yes | Abort BlockedSpawn |
| S021 | 130365 | Victory | 145.6 | 13.48 | 2/2, 0 | yes | — |
| S023 | 130365 | Victory | 146.1 | 13.52 | 2/2, 0 | yes | — |
| S021 | 104731 | Victory | 128.3 | 11.88 | 2/2, 0 | yes | Victory (lane 41) |
| S023 | 104731 | Victory | 147.6 | 13.67 | 2/2, 0 | yes | Victory (lane 41) |

- `expandedStartupFailure` lines: 0 in both logs. Menu journey Passed.
- Harness evidence (34 KB + 5.3 MB + 9.8 MB) moved to `...\harness-evidence` in each log folder, not committed.
- fa-IR was not rerun here; lane 41 had 130365 fa-IR winning before the fix.

## 5. Notes
- **Pacing.** These wins land at 10.6–13.7 % of the 1080 s deadline, under the 20 % floor, like the other Desert Base CE wins. Per the lead, that decision is parked; no deadline was changed.
- **S019 (CE ground field) is still exposed.** It keeps the same pad/bay overlap (W14 noted this). It has not failed on the seeds run so far, but any seed whose opening vehicle lands in a bay would fail the same way. The same data move would close it; not done here because S019 was not in scope.

## Open
- S019 latent overlap (above).
- Mac quickcheck and counted runs pending.
