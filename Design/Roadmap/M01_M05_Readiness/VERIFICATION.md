# Current verification matrix

Updated: 2026-09-17. “Pending” means this readiness pass has not produced the evidence. Prior reports remain historical. The earlier changed-source snapshot is [recorded here](Evidence/final_source_hashes_20260916.json); the [completion-pass input snapshot](Evidence/Completion20260916/source_comparison_before_build.json) accompanies the candidate and its 19,336 source inputs were rechecked at handoff. Individual runs retain their own scope and source notes.

| Mission | Current player-control EN/FA journey | Recovery and persistence | Presentation/audio | Remaining external gate |
|---|---|---|---|---|
| M1 | Both complete at normal speed through selection, Move, screen Attack, victory and Continue | Wrong friendly Attack gives localized rejection; pause/resume; fresh-chain settlement and disk reload; later replay chain passed | EN 1280×720 / FA 2400×1080; active voice locale checked. The 18 newly recorded legacy comic clips passed standalone sequence playback; those unfinished comic assets remain outside active M1 | Physical phone and unfamiliar-player review |
| M2 | Both complete through placement, Materials Continue, closed-drawer recruitment and victory | Rotate → cancel → reopen → confirm; pause/resume; per-frame no repeated Build cue during delivery; first-clear save and replay isolation | Larger text and aligned cue reviewed; complete active comics and gameplay voice locale checked | Physical phone and unfamiliar-player review |
| M3 | Both complete road barrier → group Move → Hold → two convoys → victory | Rotated/canceled/reopened placement; pause after report modal; persistent wave/wait guidance; fresh-chain settlement/reload | EN narrow Build arrow and FA wide layout reviewed; wait countdown visible; current command voice/comics checked | Physical phone and unfamiliar-player pacing review |
| M4 | Both complete APC pickup → unload → helicopter boarding → clearance → departure → victory | Cleared selection before helicopter boarding and recovered via specialist group action; pause/resume; settlement/reload. Supplemental clearance reset/re-entry, actual combat defeat, clean Retry and no duplicate rewards passed | Larger scrollable introduction and both Board-sector cues reviewed; full EN/FA active comics and locale paths checked | Physical phone and unfamiliar-player review |
| M5 | Both complete gate → compound entry → transmitter → archive → victory | Wrong friendly Attack rejected; pause/resume; gate placement/blocking invariant; dead-target cue check; settlement/reload. Supplemental real deadline defeat and visible Retry passed with fresh targets, clock and lesson 1 | Gate between walls, translated Heavy APC description, archive area and progress reviewed; full EN/FA comics and voice locale paths checked | Physical phone and unfamiliar-player review |

Existing M3 evidence: `Design/AgentReports/M03PlacementCommit/` and `Design/AgentReports/M03PlacementHandoff/`. These cover the exact placement regressions, not full-mission readiness. Existing other mission reports do not automatically close this matrix.

## Evidence classes

- Source review: identifies likely causes and stale contracts; does not prove gameplay.
- Focused test: tests a bounded contract with controlled data.
- Gameplay simulation: real move/attack/production commands, potentially bypassing player UI; label clearly.
- Player UI replay: current visible UI handlers and pointer interactions; checks world and displayed results.
- Rendered review: inspect captures at the stated resolution/locale and state.
- Device / independent player: physical-device touch/performance and actual unfamiliar-player observations. Never inferred from Editor testing.

## Earlier runs (scope preserved)

- Opening regression: all five missions in both languages, isolated completed profile, assistant Off, comics skipped, first action and Pause → Exit. Passed with wrapper exit 0. See [evidence](Evidence/OpeningRegression/validation.txt). M3 opening coverage does not include its first build action.
- M1 full guided route: English first clear and Farsi replay, normal time scale, actual UI pointer handlers with hit-testing in the rendered Game view, screen-only move/attack requests, victory and result Continue back to campaign. Passed with wrapper exit 0. At that earlier stage, comics/audio and broad recovery had not yet been checked; completion-pass evidence follows below.
- M4 Farsi opening scroll/layout fix: complete introduction visible above Continue at 1920×1080. Focused scroll cases passed in EN/FA at widths 1920 and 2400, preserving deliberate player scroll. See Evidence/M04OpeningLayout/.
- M2 complete routes: English first clear and Farsi replay passed after QA-06 fix, including campaign return. See Evidence/M02Journey/. This is a construction/recruitment mission in the current config; it correctly finishes without a defensive encounter. Subsequent recovery journeys also completed with the revised construction result template.
- Those earlier player UI replays used isolated profiles and skipped comics. The completion runs below add full active comic playback and genuine sequential unlock chains.
- `adb devices -l` reported no connected device. Phone touch/performance and unfamiliar-player review remain pending.

- M3 EN/FA guided routes reached victory and campaign return, using a road barrier and Hold defense (no stat or outcome edits). English result: 7/7 hostiles stopped, post undamaged, three rifle losses. Evidence/M03Journey/. A subsequent refinement waits for the whole selected formation; focused tests and EN/FA shared-cue replays now confirm one Move followed by Hold.

- M4 full EN/FA player routes: `/private/tmp/readiness-m04-journey-03.log`, wrapper exit 0. The actual Board wedge was hit-tested through UI; all four specialists completed each transport stage. See Evidence/M04Journey/. The later arrow-without-caption fallback was reviewed on APC and helicopter boarding in both languages in Evidence/SharedCues/.
- M5 full post-fix EN/FA routes: `/private/tmp/readiness-m05-journey-02.log`, wrapper exit 0. Each structure received one guided attack; the accepted order no longer reopened Attack guidance. Archive recovery is framed once and marked with the authored radius; both-language 17-second wait screenshots reviewed. See Evidence/M05Journey/.
- M2 result presentation: four synthetic EN/FA victory/partial-failure fixtures, plus a focused test for real counts and label restoration when reusing the result view. See Evidence/M02Results/. Partial failure does not falsely claim a lost squad; completed objectives remain green.
- M2 cancel → reopen → confirm → pause/resume at Materials → recruit → victory routes passed in EN/FA. The blank-ARIA interval and stale Build cue were fixed and rechecked in run 07. Both languages show the localized training wait with the builder closed, display 5/5 and reach victory. See Evidence/M02Recovery/.

- Final shared-control replay (run 04): M3 EN/FA then M4 EN/FA reached victory and campaign return. M3 Build is visible/enabled, its corner arrow fits beside the minimap, and the defense instruction remains present between convoy elements. M4 Board frames its sector and retains a directional arrow in both languages. Focused ownership, travel, attack-state and geometry checks passed. See Evidence/SharedCues/validation.txt for exact run scope and terminal-status limitations.

- Final M3 wait presentation: focused wait-to-action style restoration and complete EN/FA defense routes passed, wrapper exit 0. Plain yellow ring and readable instructions reviewed in both languages; a continuous guard checks for guidance loss during battle. See [final M3 evidence](Evidence/M03FinalWait/validation.txt).


## Evening player-feedback regression — passed

Scope: QA-20, QA-21, QA-22 in FINDINGS.md. The earlier green routes did not cover these acceptance details.

- M5: `readiness-feedback-m05-03.log`, wrapper exit 0. EN and FA reached victory and returned to campaign. Each entry checked the **live** gate origin `(1020,444)`, footprint `8×2`, closed navigation blocker and inability to path around the sealed compound. Both rendered gate views were inspected; screenshots are in `Evidence/PlayerFeedback20260916/`.
- Focused M2 production test passes: closing/destroying the Build drawer retains the gameplay queue, cancellation releases the wait, delivery remains pending after dequeue, produced records remain observable until mission guidance consumes them, and unbind clears state.
- M2/M3 combined EN/FA replay passed: `readiness-feedback-m02-m03-01.log`, wrapper exit 0. All four routes reached victory and returned to campaign. M2 checks each rendered frame for repeated Build guidance after recruitment. M3 checks continuous defense status.
- M3 final pacing follow-up: the first replay still showed a 31-second remaining wait between convoys, so the second arrival estimate was reduced from 90 to 75 seconds (originally 165). First arrival is 25 seconds (originally 65). Final run `readiness-feedback-m03-final.log` passed both EN and FA, wrapper exit 0. The corresponding final capture shows 16 seconds remaining where the earlier capture showed 31. Both normal-speed routes reached victory and returned to campaign.
- Two failed M5 launches are retained as failures: run 01 caught a missing namespace import; run 02 caught the gate's visual footprint shrinking to 8×1. Both were corrected before run 03.

The retained evidence is in `Evidence/PlayerFeedback20260916/`: M2 training/end screens, M3 countdowns in both languages, M5 gates between the walls, filtered validation logs and the tested input hashes. Current code/localization/scenario/HUD inputs were compared byte-for-byte against the isolated validation project with no mismatches. Code/document/JSON whitespace checks pass; whole-tree checks still report Unity-generated empty YAML fields in existing prefab changes.

Scope remains Editor gameplay replays through visible controls and normal screen-position commands. No simulation speed, health, objective facts or mission outcomes were overridden. This validates these three reported regressions; it does not close the separate phone-device, broader recovery, narrative-audio or human usability gates in PLAN.md.

## Completion pass — current evidence

- Fresh EN and FA M1→M5 chains, followed by FA M4→M1→M3→M5→M2 replay: 15 victories with correct settlement and disk reload, wrapper exit 0. This run preceded final font/audio/storage changes and skipped comics. Exact scope: [campaign continuity](Evidence/Completion20260916/campaign_continuity.txt).
- Final English normal-speed journey with active comics: M1/M2 completed in recovery run 05. M3–M5 completed in run 06 using the **actual M1/M2 saved profile**. No unlock, position, health or outcome injection. Run 05 failed at the now-fixed narrow-screen arrow geometry; run 06 failed only when switching to an unavailable Farsi resolution preset. Preserve both failures: [run 05](Evidence/Completion20260916/recovery05.txt), [run 06](Evidence/Completion20260916/recovery06.txt).
- Fresh final Farsi M1→M5: all five completed at normal speed, full active comics, recovery actions, correct rewards, disk reloads and campaign returns; wrapper exit 0. Used the existing 2400×1080 preset. [Run 07](Evidence/Completion20260916/recovery07.txt).
- Voice checks read the **actually playing audio asset** against the selected locale throughout those journeys; [observed clips](Evidence/Completion20260916/observed_voice_locales.json). Local transcripts independently checked the reported Move/enemy-vehicle warning recordings and all 18 approved M1 clips. This detects incorrect-language assets and routing; it is not a native-speaker pronunciation review.
- Victory/defeat focused regressions verify existing gameplay narration stops and late gameplay requests are rejected. The separately owned debrief comic audio remains permitted. M5's first debrief line uses the authored `m05-comms-01` clip; hearing it during the debrief is intentional.
- The approved 18 M1 comic clips are generated/imported and each played once to its end in the real sequence player. M1's legacy comic assets lack artwork and are excluded by the active narrative policy; no blank panels were enabled. [Playback evidence](Evidence/Completion20260916/m01_comic_playback.txt).
- Native allocation traces from repeated routes show zero remaining Game-owned grid allocation stacks. Unity URP/Editor shutdown allocations remain separately recorded; no claim is made that all Editor allocations were removed. [Trace classification](Evidence/Completion20260916/native_allocation_review.json).

Focused regression: 269 distinct tests pass in their latest runs ([summary](Evidence/Completion20260916/focused_summary.json)). Supplemental M4 clearance interruption, combat defeat, Retry and reward isolation passed ([evidence](Evidence/Completion20260916/m04_recovery_pass.txt)); idle combat ran at 4x and is mechanics-only evidence. M5 real deadline defeat/Retry passed ([evidence](Evidence/Completion20260916/m05_retry_pass.txt)); simulation was accelerated to 12x for this mechanics check. The Android candidate built and passed signature/archive-integrity checks; see the artifact section below. Physical phone performance, touch/thermal behavior, and 3–5 unfamiliar-player observations remain external gates: `adb devices -l` returned no connected device.

## Android candidate build findings

- Attempt 01 failed the required pass-marker gate on player compilation: the operation-map baker called an Editor-only helper. Matching the call’s compilation guard fixed this without changing Editor baking.
- Attempt 02 compiled player scripts and built content, then correctly failed the duplicate-dependency gate.
- Attempt 03 used Unity’s built-in dependency isolation. It preserved all 122 existing addresses/labels/group owners and isolated 78 formerly implicit shared dependencies. Duplicate Analyze issues fell from 3,799 displayed issue rows to zero; the strict operation-map content report passed. Its one retained duplication row is the pre-existing permitted package Editor asset class, not a new runtime exemption. The repaired source configuration is in Assets/AddressableAssetsData. APK compilation/packaging passed, including the existing entity-scene package gate and Android build report.

## Verified Android artifact

Attempt 03 completed with wrapper exit 0 and `[AndroidBuildReport] result=Passed`. The existing entity-scene package validation also completed before that marker. The ARM64, API-26-minimum, non-debuggable APK is 634,020,483 bytes. Its signature, complete ZIP integrity and copied SHA-256 were verified; 19,336 source input hashes still match the handoff snapshot. See [artifact verification](Evidence/Completion20260916/apk_verification.json) and [candidate handoff](COMPLETION.md). No device startup/performance pass is claimed.
