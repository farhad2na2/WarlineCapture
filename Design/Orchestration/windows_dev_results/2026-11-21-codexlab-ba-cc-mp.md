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

## BlockedSpawn fix

The follow-up evidence is under `/Volumes/T7/Projects/WarlineCapture-program-logs/codexlab/author-ba-fix-cc-mp`. The baseline references in this section use the original `author-ba-cc-mp` log above.

`SkirmishStartingBuildingsService.cs:89` computes the preferred lower-left cell from the anchor and the footprint. Its failed-request check reports `BlockedSpawn` at line 118. The shared placement diagnostic on `BuildingRuntimeProcessingCompositionSystemHelper.cs:721` records the first rejection at the preferred footprint and totals for the alternate origins. Every preferred footprint below hit an authored road. Source buildings and the road/water check together rejected every candidate admitted by the typed pad. The diagnostics recorded zero sidewalk or bounds rejections.

| Affected missions | Starting structure | Faction | Preferred grid origin | Baseline placement diagnostic, one line per mission in order |
| --- | --- | ---: | --- | --- |
| S027–S030 | Barracks | 1 | (968, 675) | `aria/unity.log:11014`, `:12583`, `:14177`, `:15722` |
| S027–S030 | Refinery | 1 | (987, 692) | `aria/unity.log:11093`, `:12662`, `:14256`, `:15801` |
| S027–S030 | Refinery | 2 | (1059, 352) | `aria/unity.log:11141`, `:12710`, `:14304`, `:15849` |
| S027–S030 | Fuel bladder | 2 | (1097, 340) | `aria/unity.log:11189`, `:12758`, `:14352`, `:15897` |
| S027–S030 | Ammunition depot | 1 | (996, 676) | `aria/unity.log:11237`, `:12806`, `:14400`, `:15945` |
| S051–S054 | Barracks | 2 | (347, 186) | `aria/unity.log:17479`, `:18841`, `:20272`, `:21659` |
| S051–S054 | Fuel bladder | 2 | (359, 168) | `aria/unity.log:17527`, `:18889`, `:20320`, `:21707` |

S027–S030 reuse the measured S026 structure pads. S051–S054 reuse the S050 base, structure, spawn and rally pads and their associated route waypoints. Each draft records its sibling source. Mac lane 50 recorded all 20 sibling grants at their preferred origins (`2026-11-21/lane50/a/unity.log:242395–242833` and `:329111–329548`, relative to the program log root). The re-authoring retained the Field and Established profiles and their 12 size/difficulty variants.

### Authoring and layout containment

The draft checker passed all eight drafts (`check-draft.log:1–8`). The authoring batch passed all eight with 12 size/difficulty variants each (`author.log:2117`). The batch layout entry invoked the existing containment checks for each draft.

| Mission | Authoring passed | Containment passed, failures=0 | Rows |
| --- | --- | --- | ---: |
| S027 | `author.log:808` | `layout.log:653` | 168 |
| S028 | `author.log:998` | `layout.log:997` | 256 |
| S029 | `author.log:1183` | `layout.log:1221` | 168 |
| S030 | `author.log:1365` | `layout.log:1565` | 256 |
| S051 | `author.log:1552` | `layout.log:1789` | 168 |
| S052 | `author.log:1736` | `layout.log:2133` | 256 |
| S053 | `author.log:1920` | `layout.log:2357` | 168 |
| S054 | `author.log:2106` | `layout.log:2701` | 256 |

The batch containment marker is at `layout.log:2715`. The revised S052/S054 air-pad results are below.

### Uncounted warm-ups

S031 and S055 ran at seed 130366 in English in their own queue. Both used the touch menu and returned from the result screen.

| Mission | Dispatch | Menu | Last building | Match | Result and return | Queue |
| --- | --- | --- | --- | --- | --- | --- |
| S031 | 130366/en (`warmup/unity.log:684`) | Passed (`warmup/unity.log:1361`) | 14 (`warmup/unity.log:1921`) | Victory / TicketsDepleted, 543.307 s (`warmup/unity.log:11488`) | Passed (frozenResultAnd:MAIN MENU) (`warmup/unity.log:11416`) | Victory (`warmup/unity.log:21656`) |
| S055 | 130366/en (`warmup/unity.log:11561`) | Passed (`warmup/unity.log:12254`) | 14 (`warmup/unity.log:12771`) | Victory / TicketsDepleted, 610.417 s (`warmup/unity.log:21538`) | Passed (frozenResultAnd:MAIN MENU) (`warmup/unity.log:21466`) | Victory (`warmup/unity.log:21671`) |

`[SkirmishAriaQueue] result=Completed jobs=2 recorded=2 wins=2 sourceUnchanged=1 exitCode=0` at `warmup/unity.log:21686`.

### Counted ARIA smoke

Each target ran once in English at seed 130365, Standard/Regular. The Editor validation library enabled the Planned cards for the touch-menu journey. All eight placed every starting building. Six entered play. S052 and S054 reached starting-force placement and aborted on the faction 1 transport helicopter.

| Mission | Dispatch | Menu | Last building | Match | Result and return | Queue |
| --- | --- | --- | --- | --- | --- | --- |
| S027 | 130365/en (`aria/unity.log:527`) | Passed (`aria/unity.log:1209`) | 14 (`aria/unity.log:1761`) | Abort / ariaStopped:Blocked, 591.831 s (`aria/unity.log:108414`) | No marker | Abort (`aria/unity.log:627001`) |
| S028 | 130365/en (`aria/unity.log:108490`) | Passed (`aria/unity.log:109115`) | 20 (`aria/unity.log:109786`) | Abort / ariaStopped:Blocked, 335.077 s (`aria/unity.log:209457`) | No marker | Abort (`aria/unity.log:627016`) |
| S029 | 130365/en (`aria/unity.log:209530`) | Passed (`aria/unity.log:210176`) | 14 (`aria/unity.log:210683`) | Defeat / MainBaseDestroyed, 481.084 s (`aria/unity.log:349829`) | Passed (frozenResultAnd:MAIN MENU) (`aria/unity.log:349757`) | Defeat (`aria/unity.log:627031`) |
| S030 | 130365/en (`aria/unity.log:349901`) | Passed (`aria/unity.log:350434`) | 20 (`aria/unity.log:351182`) | Victory / MainBaseDestroyed, 205.557 s (`aria/unity.log:435026`) | Passed (frozenResultAnd:MAIN MENU) (`aria/unity.log:434954`) | Victory (`aria/unity.log:627046`) |
| S051 | 130365/en (`aria/unity.log:435098`) | Passed (`aria/unity.log:435769`) | 14 (`aria/unity.log:436261`) | Abort / ariaStopped:Blocked, 573.255 s (`aria/unity.log:528370`) | No marker | Abort (`aria/unity.log:627061`) |
| S052 | 130365/en (`aria/unity.log:528449`) | Passed (`aria/unity.log:529111`) | 20 (`aria/unity.log:529742`) | Abort / expandedStartupFailure=BlockedSpawn, 0 s (`aria/unity.log:529862`) | No marker | Abort (`aria/unity.log:627076`) |
| S053 | 130365/en (`aria/unity.log:529934`) | Passed (`aria/unity.log:530572`) | 14 (`aria/unity.log:531064`) | Abort / ariaStopped:Blocked, 381.719 s (`aria/unity.log:625400`) | No marker | Abort (`aria/unity.log:627091`) |
| S054 | 130365/en (`aria/unity.log:625475`) | Passed (`aria/unity.log:626128`) | 20 (`aria/unity.log:626765`) | Abort / expandedStartupFailure=BlockedSpawn, 0 s (`aria/unity.log:626885`) | No marker | Abort (`aria/unity.log:627106`) |

`[SkirmishAriaQueue] result=Completed jobs=8 recorded=8 wins=1 sourceUnchanged=1 exitCode=0` at `aria/unity.log:627121`.

The first relevant failure markers are `WaitForAirPadSite` for S027 and S028 (`aria/unity.log:108321`, `:209365`), `Recruit` for S051 (`:528279`), `Inspect` for S053 (`:625307`), and `[SkirmishStartingForce] failed prefab=Unit_Veh_Helicopter_Transport faction=1 member=0` for S052 and S054 (`:529765`, `:626788`).

### Mountain Pass Established air-pad follow-up

S052 and S054 use the measured AirReturn and helipad pair from the Air Established S058 and Combined Established S060 drafts respectively. The pair is (947, 495.5) for the player and (1261, 467.5) for the enemy. Mac lane 49 recorded the two helipads and both starting helicopters in the initial world for S058 (`2026-11-20/lane49/a/unity.log:431067`, `:431090`, `:431136`) and S060 (`:508593`, `:508616`, `:508662`). The sources share the same Citywide Alert definition.

Both revised drafts passed the draft checker (`check-air-drafts.log:1–2`), re-authoring (`author-air.log:1000`), and containment with `failures=0` (`layout-air.log:773`, `:1117`, batch at `:1131`). The follow-up used the native startup-only harness at 130365/en through the touch menu.

| Mission | Touch menu | Faction 2 helipad actual origin | First startup failure | Startup verdict |
| --- | --- | --- | --- | --- |
| S052 | Passed (`startup-S052.log:1338`) | (392, 160) (`startup-S052.log:2017`) | Faction 2 transport helicopter, member 0 (`startup-S052.log:2040`) | Failed / BlockedSpawn (`startup-S052.log:2197`) |
| S054 | Passed (`startup-S054.log:1188`) | (392, 160) (`startup-S054.log:1868`) | Faction 2 transport helicopter, member 0 (`startup-S054.log:1891`) | Failed / BlockedSpawn (`startup-S054.log:2049`) |

The two startup probes passed the player aircraft spawn and failed at the enemy aircraft. The same draft center at world X 1261 placed the enemy helipad at (393, 160) in S058/S060. Recomputing `TryProjectToMap` with the serialized single-precision values gives world X 1260.99987793 in S052/S054 and 1261.00000000 in S058/S060. The S052/S054 frame is about 19.55 m narrower across its base-to-base axis. At this cell boundary the computed preferred X changes from 393 to 392, matching the native placement lines. W24 recorded the same one-cell shift and enemy transport failure on this map (`2026-11-18-w24-s070-heli-spawn.md`). The startup probes did not log the aircraft footprint's individual occupancy flags.

The second transport failures ended the revisions for S052/S054. The counted smoke queue records the prior candidate. Manual play, visual review and real player/device acceptance remain pending. The complete harness logs and generated evidence are in the follow-up log directory (`archived-evidence.log` and `archived-startup-evidence.log`).

Drafts and generated assets are in `057985010` (City Crossroads) and `718e043e2` (Mountain Pass).
