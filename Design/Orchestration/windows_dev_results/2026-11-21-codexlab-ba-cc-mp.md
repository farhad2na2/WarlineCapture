# Codex lab Base Assault authoring — 2026-11-21

City Crossroads S027–S030 were committed in `994a4f433`. Mountain Pass S051–S054 were committed in `2e74cadf2`, both on `missions/author-ba-cc-mp`. The assets use the existing City Crossroads and CH05M01 Citywide Alert Campaign sources. The expansion catalog resolves all eight definitions, the publication manifest records InProgress, and the shipped battle library lists all eight as Planned (`catalog-readback.log:1–8`).

Log paths below are relative to `/Volumes/T7/Projects/WarlineCapture-program-logs/codexlab/author-ba-cc-mp`. Unity 6000.5.8f1 ran through the macOS lab wrapper with GUI licensing.

## Draft and authoring checks

- The first authoring batch passed four of eight (`author.log:3592`). S028, S030, S052 and S054 failed the War/Recruit instance-pad check for the faction 1 refinery module (`author.log:2164`, `2643`, `3112`, `3569`). The drafts lacked `structure_footprints`, so the author could not derive the module's pad.
- Copied the measured footprint tables from the same-source S026 and S050 drafts into their respective mission drafts. This also supplied the table required by containment validation. All other draft fields were preserved.
- `check_draft.py` passed all eight (`check-draft.log:1–8`). `[SkirmishMissionAuthoringBatch] result=Completed drafts=8 passed=8 failed=0` appears at `author-r2.log:1959`.

## Authored layout containment

| Mission | Authoring passed, 12 variants | Containment passed, failures=0 | Rows |
| --- | --- | --- | ---: |
| S027 | `author-r2.log:638` | `layout-S027.log:617` | 168 |
| S028 | `author-r2.log:827` | `layout-S028.log:710` | 256 |
| S029 | `author-r2.log:1011` | `layout-S029.log:612` | 168 |
| S030 | `author-r2.log:1200` | `layout-S030.log:708` | 256 |
| S051 | `author-r2.log:1382` | `layout-S051.log:611` | 168 |
| S052 | `author-r2.log:1572` | `layout-S052.log:712` | 256 |
| S053 | `author-r2.log:1751` | `layout-S053.log:614` | 168 |
| S054 | `author-r2.log:1936` | `layout-S054.log:707` | 256 |

The containment marker covers pad envelopes. The method leaves the Editor open without `-quit`. After each verdict, the lab Editor closed normally via a verified PID after the three-minute minimum (`layout-launches.log`).

## ARIA smoke

The queue used the real menu with the validation library and `--no-table`. The warm-up used S031, seed 130366. Each target used one English Standard/Regular attempt at seed 130365.

| Mission | Dispatch | Touch menu | Queue result | Match reason and seconds | Result / menu return |
| --- | --- | --- | --- | --- | --- |
| S031 warm-up | `aria/unity.log:526` | Passed (`aria/unity.log:1250`) | Abort (`aria/unity.log:22290`) | `TicketsDepleted`, 585.065 s (`aria/unity.log:10070`) | Passed (`aria/unity.log:9998`) |
| S027 | `aria/unity.log:10142` | Passed (`aria/unity.log:10809`) | Abort (`aria/unity.log:22305`) | `expandedStartupFailure=BlockedSpawn`, 0 s (`aria/unity.log:11664`) | No marker |
| S028 | `aria/unity.log:11735` | Passed (`aria/unity.log:12381`) | Abort (`aria/unity.log:22320`) | `expandedStartupFailure=BlockedSpawn`, 0 s (`aria/unity.log:13276`) | No marker |
| S029 | `aria/unity.log:13349` | Passed (`aria/unity.log:13972`) | Abort (`aria/unity.log:22335`) | `expandedStartupFailure=BlockedSpawn`, 0 s (`aria/unity.log:14823`) | No marker |
| S030 | `aria/unity.log:14894` | Passed (`aria/unity.log:15523`) | Abort (`aria/unity.log:22350`) | `expandedStartupFailure=BlockedSpawn`, 0 s (`aria/unity.log:16415`) | No marker |
| S051 | `aria/unity.log:16487` | Passed (`aria/unity.log:17244`) | Abort (`aria/unity.log:22365`) | `expandedStartupFailure=BlockedSpawn`, 0 s (`aria/unity.log:17933`) | No marker |
| S052 | `aria/unity.log:18005` | Passed (`aria/unity.log:18618`) | Abort (`aria/unity.log:22380`) | `expandedStartupFailure=BlockedSpawn`, 0 s (`aria/unity.log:19355`) | No marker |
| S053 | `aria/unity.log:19426` | Passed (`aria/unity.log:20055`) | Abort (`aria/unity.log:22395`) | `expandedStartupFailure=BlockedSpawn`, 0 s (`aria/unity.log:20742`) | No marker |
| S054 | `aria/unity.log:20813` | Passed (`aria/unity.log:21443`) | Abort (`aria/unity.log:22410`) | `expandedStartupFailure=BlockedSpawn`, 0 s (`aria/unity.log:22174`) | No marker |

`[SkirmishAriaQueue] result=Failed reason=candidateSourceChangedDuringQueue jobs=9 recorded=9 wins=0 sourceUnchanged=0 exitCode=2` at `aria/unity.log:22425`.

All eight Base Assault jobs dispatched once and passed the touch-menu journey. Each aborted before the match clock started. City Crossroads first failed to place the faction 1 barracks at grid origin (968, 675) for S027, S028, S029 and S030 (`aria/unity.log:11282`, `12832`, `14442`, `15987`). Mountain Pass first failed to place the faction 2 barracks at (347, 186) for S051, S052, S053 and S054 (`aria/unity.log:17575`, `18936`, `20367`, `21755`).

The warm-up's per-attempt marker recorded Victory and the return-to-menu marker passed (`aria/unity.log:10070`, `9998`). Mountain Pass's first load created an ignored scene-cache `.meta` during the queue (`aria/unity.log:17158`, `aria/AssetImportWorkerHW0.log:305–308`). The candidate fingerprint changed between the opening and closing checks (`aria/unity.log:502`, `22276`), so the queue invalidated all nine aggregate rows, including the warm-up. The per-attempt lines retain the startup failures and warm-up outcome.

The lab saved the full logs and moved 75 generated evidence files to `aria/harness-evidence/` under the log directory. Native placement, full target-mission playthroughs and player/device acceptance remain open.
