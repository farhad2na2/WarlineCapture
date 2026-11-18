# Lane worker 26, 2026-11-18: S004 air coverage, Farsi localisation fixes

Logs: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-18/lane26/`. Lane taken 08:06, returned idle at the end.

## Step 1: merges and tests

- Merged `--no-ff`: `missions/fa-l10n-fixes` a2d9221cd (97dbb6559), `missions/aria-coverage-9` 8bb2ba21e (785890766),
  `missions/s004-no-airport` ad7d64f1f + a1784651e (fa83a2ffb). Compile clean after each:
  compile1.log:627, compile2.log:1942, compile3.log:1744, 0 `error CS`.
- Python tests: 149 OK (python.txt).
- `ApplyConfiguredUiTables` with `-quit`: `[ConfiguredUiTables] result=Passed configEntries=1058` (apply-tables.log:549).
  The catalog asset did not change, so no keys changed and there was nothing to commit.
- `[AriaAirCoverage] result=Passed controls=9 normalMissionAcceptance=Pending` (aircov.log:483). My helper stopped an earlier
  attempt before the marker because its stop pattern was too broad (it matched an unrelated `System.Exception` stack line). That
  log is kept as aircov-attempt0-helper-stopped-early.log. It was not a test failure, and `Library/LibraryInitializing` was not left behind.
- `[SkirmishS004NoAirportTests] result=Passed tests=5 s004Sizes=3 fullArsenalOffered=0` (s004noair.log:483).
- EditMode `-runTests` (edit.xml): 104/105 passed.

| Class | Passed |
|---|---|
| StartThroughM02LocalizationTests | 9/9 |
| AriaBaseAlertDefenseTests | 6/6 |
| AriaAirCoverageTests | 9/9 |
| AriaCoverageRecruitTests | 7/7 |
| AriaBreacherCoverageTests | 3/3 |
| AriaInterceptPolicyTests | 5/5 |
| SkirmishS004NoAirportTests | 5/5 |
| SkirmishEnemyAirCombined13Tests (S004 pre-policy pins) | 5/5 |
| SkirmishExpandedEconomyTests | 55/55 |
| SkirmishLegacyLayoutBaselineRegressionTests | 0/1 |

- The baseline regression fails at the captured Unity version assert: it expects `6000.5.2f1` but the Editor is `6000.5.8f1`.
  This failure has existed since the 6000.5.8 upgrade (f9f436c53), and the test stops before it checks any hash. To check the pins,
  I ran a temporary, uncommitted probe with the same hash loop minus the version assert (baseprobe.log:628-760). All 12 rows match,
  including S004 Standard 0x4197E539, War 0xC367BF7D and LargeWar 0xDFE40279 (`result=Passed ok=12`). I deleted the probe, and no
  re-pin was needed. Open item: the test's `CapturedUnityVersion` assert needs an owner/lead decision. Re-pinning to 6000.5.8f1 or
  dropping the assert is outside this lane.

## Step 2: S004 x3 + S002 queue (`--menu-journey --no-table`, s004/unity.log, s004/job-extract.txt)

| Job | Result | Match clock | % of 1080 | gate-3 unused | MenuJourney |
|---|---|---|---|---|---|
| S004 104733 en | Victory | 180.4 s | 16.71% | aa, apc_fast, attack_heli, drone, marksman, radar, transport_heli | Passed (:2426) |
| S004 130367 en | Victory | 290.5 s | 26.90% | aa, apc_fast, attack_heli, drone, radar, transport_heli | Passed (:423422) |
| S004 155925 en | Abort ariaStopped:Blocked | 153.7 s | 14.23% | unproven | Passed (:988877) |
| S002 130365 en | Victory | 226.9 s | 21.01% | (empty) | Passed (:1469658) |

- S004 `event=available` was the same in all three jobs: aa, apc_armored, apc_fast, attack_heli, attack_heli_light, breacher, car,
  drone, gunner, logistics_truck, marksman, radar, rifle, rocketeer, tanker, transport_heli. It has no airport, fighter, strike or
  transport_plane, so the exclusion holds.
- Coverage: on S004 the only coverage lines are `state=fielded` for the starting transport_heli, gunner and aa at t=7. Marksman was
  bought only in job 2 (t=151-167). No apc_fast, radar, drone or attack_heli buy appears on S004. Lane 24 had 12 unused types; 6 or 7
  remain. The 4 fixed-wing/Airport types are gone from the list, and breacher is now used.
- AA support order: every S004 job ran selecting, arming, tapping, disarming and released (tried=8) at t=90-103. In job 2 a second AA
  order ended `gave_up` at t=230.7. Even so, aa is still counted as unused. No `support_order kind=transport_heli` line appears in any job.
- Job 3 abort: `[AriaBlocked] reason=115 target=-240380 attempts=3 actions=73 kind=Control intent=DefendBase frame=5252` (:1467496),
  about 50 s after the second home_defense `ordered` line.
- Base alerts: one `event=base_alert state=onset` per S004 job (t=100.3, 101.5, 102.2), each followed by home_defense `triggered`
  and then `ordered`. S002 had one onset at t=217.75. Every alert and home_defense line shows baseHealth=800, and the terminal frame
  shows playerBaseHp=800 (jobs 1, 2 and 4), so the player base took no damage in any job.
- S002 regression: still a win with `unused=` empty, result_return_ok=yes.
