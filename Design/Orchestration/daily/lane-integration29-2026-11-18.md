# Lane integration 29 — 2026-11-18 (lane worker 29)

Brief: `WarlineCapture-program-logs/briefs/lane-integration29-2026-11-18.md`. Logs: `WarlineCapture-program-logs/2026-11-18/lane29/`.
Startup: no Editor owned WarlineCapture, Temp/UnityLockfile held by no process, no Library/LibraryInitializing, disk 234 GB.
Every launch passed the strict startup check (`Library Redirect Path` within seconds). Each Editor I stopped was stopped by its own verified PID.

## 1. Merges (both `--no-ff`, no conflicts)
- `missions/s002-hud-retest` -> 2d860dd67; compile clean (compile1.log:1776, 0 `error CS`).
- `missions/s002-camera-retest` -> 4e287142e; compile clean (compile2.log:1764, 0 `error CS`). The two branches share no files.

## 2. Tests
| Check | Result |
|---|---|
| Python (`Tools/Missions`, PYTHONPATH=repo) | 149 OK (python.txt). Two earlier invocations used the wrong working directory (import errors) and are kept as python-attempt0/1. |
| SkirmishS002HudRetestTests | `[S002HudRetest] result=Passed controls=9` (hudretest.log:484) |
| SkirmishS002CameraRetestTests | **FAILED x2**, same line: `AssertionException: yaw 60 Expected: False But was: True` in `Yaw65IsTheSmallestRotationFromNorthThatFitsOpeningAndAlert` (cs:163; camretest-attempt1.log:506, camretest2.log:510). No marker. |
| SkirmishCameraFramingTests | `result=Passed tests=10` (framing.log:483) |
| SkirmishHudChipPlacementTests | `[SkirmishHudChipPlacement] result=Passed` (chip.log:483) |
| ApplyConfiguredUiTables (-quit) | `result=Passed configEntries=1068` (apply-tables.log:551). Only the serializer form changed: 10 English keys lost their quotes and 6 fa-IR values were line-wrapped. No value changed. Committed fb09788a6. |
| EditMode (edit.xml) | 64/79: AriaBaseAlertDefense 6/6, MatchHudSquadTrayPaging 2/2, SkirmishObjectiveCopy 6/6, SkirmishResultHonesty 6/6, SkirmishS005OpeningViewPolicy 15/15, StartThroughM02Localization 9/9; **SkirmishExpandedArmy 2/16** (14 fail at setup: `InsufficientAnchor:definition/layout/source: Explicit setup, logical layout and Campaign source required.`); **GameLocalizationCatalog 18/19** (EveryV3UiPrefab_HasSharedEnglishAndPersianCoverage: 36 issues, 35 missing bindings in SCN13_SkirmishSetupContent.prefab, plus the shared Persian font lacks glyph U+FE82). |

The program logs have no earlier run of either failing EditMode class, so I could not tell whether these failures predate the merges. Neither merge touched these files.

## 3. Queue (q/unity.log, q/job-extract.txt, q.out.txt): Completed jobs=9 recorded=9 wins=7 sourceUnchanged=1, MenuJourney Passed x9
| Job | Result | Clock (% deadline) | gate-3 unused | Notes |
|---|---|---|---|---|
| S004 104733 en (warm-up) | Victory | 224.0 s (20.74%) | apc_armored, attack_heli, drone, radar | aa ordered t=78.4; **transport_heli ordered t=160.3**; base_alert t=100.7, home_defense ordered t=123.9; probe Passed |
| S002 130365 en | **Abort** ariaStopped:Blocked | 230.6 s (21.36%) | unproven | alert t=222.0 (attacker in sight). home_defense triggered with selectedMask=0 defenderMask=0, then the Attack control was tapped 3 times at screen (1199,106) with no squad selected. Result: `[AriaBlocked] reason=115 kind=Control intent=DefendBase` (:462474) and `[AriaBlockedControl] control=Attack homeDefenseStage=4 selectedMask=0` (:462507). The alert-row tap at (722,912) was taken on the first try. No base damage; the enemy base was at 690. |
| S002 104731 en | Victory | 232.6 s (21.53%) | empty | alert t=226.5, home_defense triggered (selectedMask=2); probe Passed |
| S002 155923 en | **Abort** ariaStopped:Blocked | 383.3 s (35.49%) | unproven | alerts at t=61.9 and 67.7 (damage, base 772). home_defense ordered (group 4) and held the base at 520. A second alert at t=241.3 was ordered (group 3) and held at 520. Then `[AriaBlocked] reason=101 kind=Waiting intent=FindBase` (:1143095) after about 80 s of FindBase/Inspect taps at (200,413), (1660,968) and (1199,106). |
| S002 130365 fa-IR | Victory | 226.3 s (20.95%) | empty | result localized Passed; alert t=220.0, home_defense ordered (group 4); probe Passed |
| S013 130365 en | Victory BreakthroughEvacuated 8/8 lost 0 | 376.9 s (34.89%) | 11 types | 8 frame_missing frameReason=4 |
| S019 130365 en | **Victory ConvoyDelivered 2/2** lost 0 | 115.2 s (10.67%) | 14 types | first CE win; escort_offer lines; 3 frame_missing frameReason=4 |
| S004 130367 en | Victory | 189.8 s (17.57%) | attack_heli, drone, radar, transport_heli | aa ordered t=75.2; transport_heli released x2, gave_up t=123.2 |
| S004 155925 en | Victory | 192.6 s (17.84%) | attack_heli, drone, radar, transport_heli | aa ordered t=75.2; transport_heli gave_up t=124.0 |

On BASE HELD: no S002 job logged a tap that failed near the top centre, and no BASE HELD or defense_held line appeared. Both S002 aborts are ARIA plan stalls. In 130365 en, ARIA pressed Attack with no squad selected. A possible cause, not verified: the defender control now only pages and flashes (SHOW HOME GUARD). In 155923, FindBase looped after two held defenses. There are 2 [AriaBlockedControl] lines in total, one from each S002 abort. No MissingReferenceException.

## 4. Captures (2400x1080, label=after; committed 3ebdd82c6)
- **S002 opening (en)**: the Barracks are centred and clear of the HUD. No infantry squad is visible in the world (cards only), the tank and its approach are not visible, and the minimap is blank. The run composed yaw=65 groundRequest=(996.9,308.4) (s002-en.log:2028), but the captured jsonl reads cameraEuler.y=107.8 and ground centre (960.7,335.5), so the saved frame is not the composed opening.
- **S002 alert focus (en)**: attempt 1 lost Editor focus at 54.6 s (`[AriaInputLifecycleStop] ended=False route=Match`); kept. Attempt 2 Captured homeInPlayArea=1 attackerInPlayArea=1 unitOrders=0 (s002-alert.log:33006). The attacker tank with its red marker is clearly in the clear area. Only a corner of the home compound shows and the Barracks are not recognisable. No impact ring or shot line is visible (base 800/800 at the shot). The alert rows fit their text. The ARIA hand cursor overlaps the SHOW HOME GUARD row.
- **S026 en / fa-IR**: the RENDEZVOUS / 'محل تجمع' chip is drawn with an up arrow under the objective banner, clear of the gear and pause buttons; playArea=384,241,1924,783.

## Open
- The camera retest's yaw-minimality assertion fails because yaw 60 also fits.
- The S002 en 130365 and 155923 ARIA stalls.
- The S002 opening frame differs from the composed one.
- SkirmishExpandedArmy setup failures and SCN13 localization bindings.
- A foreign edit to SkirmishMatchView.Observation.cs appeared at 12:23 in the main checkout, after my last Editor exited. It is not mine and is uncommitted.
