# O001 shared-world integration evidence

This is button-event integration smoke, not a manual or ARIA win. The test starts from the real Menu scene and uses the Operations briefing and HUD buttons. It verifies launch, five seconds of live simulation, starting-squad visibility, withdrawal confirmation, durable settlement, actor cleanup, menu return and paid redeployment.

- `lifecycle-smoke.log`: wrapper exit 0; deploy/withdraw/save/return/redeploy passed.
- `focused-editmode.xml` and `.log`: 79 passed, zero failed; wrapper exit 0.
- `normal-launch.png`: capture during the first deployment of this journey.

Published logs redact authentication and machine metadata. Original full logs remain at `/private/tmp/o001-lifecycle-smoke-4.log` and `/private/tmp/o001-integration-editmode-5.log` on the validation host. Known HUD-prefab-save warnings during Editor teardown are retained. The actual prefab passes a pre-Play missing-script check; the teardown cause remains unresolved.

Mission content, English/Farsi layout, full outcomes, active-world checkpoints, normal-input ARIA, device and unfamiliar-player gates are not certified by these files. See the parent progress report and P4R plan.

## Additional Editor evidence

- `checkpoint-foundation-editmode.xml`: 29 passed, wrapper exit 0; checkpoint validation and transform-writer synchronization regression.
- `editor-readiness-regressions.xml`: 35 passed, wrapper exit 0; adds full original-force quick selection and verifies shared four-soldier behavior after leaving Operations, plus future checkpoint archive preservation.

These reports remove machine/environment properties. Full originals remain in `/private/tmp/o001-editor-recovery-tests-4.log` / `.xml` and `-5.log` / `.xml`. Normal mouse-input Withdrawal and Defeat journeys are documented in the parent progress report. No Victory, Partial Success, or full recovery certification is implied. Android/device acceptance is deferred under the user's Editor-only scope.

## Normal-input iteration: objective approach

- Fresh-profile mouse/keyboard attempt `20260922-205109-Victory` completed **Partial Success**, with two scans and nine original infantry extracted. The saved result returned to Operations with AP 2. The observer correctly failed the requested Victory gate (wrapper exit 1); this is partial-outcome evidence, not a victory pass. Full log: `/private/tmp/o001-manual-victory-1.log`; chronological JSON/PNG captures: `Build/EditorEvidence/O001Manual/20260922-205109-Victory/`.
- ATTACK rejected several apparent ground clicks near courtyard scenery. Ordinary ground movement toward Signal C stopped outside interaction range. Direct targeting worked, and extraction/conclusion saved correctly.
- Objective world markers now offer ADVANCE, queuing the shared attack-move request at the authored ground position. They preserve ordinary selected-unit movement, combat, range, LOS and interaction-time rules. This avoids projecting an objective approach onto elevated scenery.
- `objective-advance-editmode.xml`: 36 passed, zero failed, wrapper exit 0. Includes queue-only behavior, no teleport/scan completion, selection, locked evidence, invalid objective and paused-input checks. Full log: `/private/tmp/o001-objective-advance-tests-1.log`.
- Content rebuild: wrapper exit 0, configured UI tables 373, player 16, enemy 20, scans 3, reachable anchors 8. Full log: `/private/tmp/o001-objective-advance-content-1.log`.

Full victory replay of this revision is pending; recovery, FA/ARIA and remaining acceptance gates are still open.

### Runtime navigation blocker found and corrected

The second normal-input victory attempt retained 16 infantry through A/B and cleared the visible threat at C, but C remained outside scan reach. The live movement grid confirmed `(1870,815)` was dynamically blocked even though the map-surface bake marked it traversable. Selected units were redirected to destinations around `z=799`. This attempt was withdrawn through the visible confirmation and result screens; it does not pass Victory. Log: `/private/tmp/o001-manual-victory-2.log`.

C is moved to the open southern forecourt at `(1882.5,796.5)`. The live grid audit also confirmed that A, B, evidence, exit and both reinforcement origins were open. Before starting the mission clock, launch now checks all required objective cells and their connected ground route against real building blockers. Spawn placement also respects those blockers. `runtime-navigation-editmode.xml` records 37 passing tests, wrapper exit 0 (`/private/tmp/o001-navigation-tests-1.log`). The rebuild passed with zero missing HUD scripts (`/private/tmp/o001-navigation-content-1.log`). A full replay is still required.

### First complete normal-input Victory

`20260922-212846-Victory`: English, Regular, seed 1102, disposable profile; macOS Editor 6000.5.2f1. Input was normal mouse/keyboard via computer use, with no entity changes, direct order injection, time acceleration, restart or forced result. Read-only diagnostics inspected navigation and component coverage. Full-force grouped approach: scans at 147.710 / 270.736 / 371.910 simulation seconds, evidence at 532.337, Victory at **588.137 seconds (9:48)**. All 16 original infantry survived; evidence and three infantry reached the safe exit when automatic Victory latched. Continue returned to Operations with the result saved, AP 2 and no reserved deployment.

Observer and wrapper both passed (exit 0): `/private/tmp/o001-manual-victory-3.log`. Chronological state records and selected screenshots are in [manual-victory-1102](manual-victory-1102/). All original captures remain under `Build/EditorEvidence/O001Manual/20260922-212846-Victory/`. Source provenance: parent `8f9a6a81b` plus this change's objective ADVANCE, real navigation validation and corrected C anchor; the later explicit interruption-recovery changes were not present in that playthrough. Persistent allocation and preview-scene teardown warnings remain unresolved. This proves a complete playable route, not full R3–R6 acceptance.

### Explicit recovery and shared regressions

- `explicit-recovery-tests.xml`: 32 passed, wrapper exit 0. Disk restart count, preserved AP/session, duplicate request, changed content, saved-progress preservation and withdrawal covered.
- `shared-regressions.xml`: **93 passed**, zero failed/skipped, wrapper exit 0 (`/private/tmp/o001-shared-regressions-1.log` / `.xml`). Includes the final schema-2 manual-control checkpoint flags, Operations world/save tests, squad selection, Campaign return/lifecycle, Skirmish session/combat and scenario source binding. These are regression tests, not cross-mode human playthroughs.
- `/private/tmp/o001-interrupted-recovery-smoke-1.log`: wrapper exit 0 and `result=Passed journey=interrupted-restart-withdraw-save-return-redeploy input=button-event-smoke original=16 total=36 ap=1`. It seeds an interrupted reservation, then uses the visible restart/withdraw/continue/deploy buttons. It asserts unchanged session, one durable restart, no second AP on restart, complete actor cleanup and one AP for the next deployment.
- Content/catalog rebuild passed with 378 configured entries and `hudMissingScripts=0`: `/private/tmp/o001-explicit-recovery-content-1.log`, wrapper exit 0.

Full tactical resume is still unavailable; the briefing explicitly states this instead of silently resetting progress. The interrupted-attempt fallback does not close R4's checkpoint/process-restoration gate.

### Farsi normal-input Victory

`20260922-215524-Victory-fa`: Regular seed 1102, fresh disposable profile. Language was selected through the visible Settings → Accessibility language option before Operations deployment. The observer's initial isolated SettingsSaveData alone did not select the runtime locale; the actual UI selection did. No gameplay injection, restart or time acceleration was used. All three scans completed (118.814 / 194.665 / 275.547 s), evidence recovered at 388.819 s, **Victory at 439.264 s (7:19)** with all 16 infantry alive and three at exit. Result/Continue returned to Operations with AP 2, saved result and cleared reservation. Both manual observer and wrapper passed, exit 0: `/private/tmp/o001-manual-fa-1.log`. [Chronological states and screenshots](manual-victory-1102-fa/).

The experienced grouped route is faster than the tentative 8–10 minute target; the original 12-minute deadline is unchanged. This second grouped win is localization evidence, not the required different split-scout approach. Translated mission controls, warnings, evidence and result were readable; the shared settings tab initially showed stale DE instead of FA. Its bound label was being replaced when the inactive tab's localization component enabled. The segmented control now publishes the runtime label through the shared localization boundary, with a regression for inactive-tab activation. Some shared Settings captions also appeared blank in Farsi; this broader presentation issue is not certified by the mission pass.

Final broader localization run: `localization-regressions.xml`, **58 passed / 1 failed**, wrapper exit 2 (`/private/tmp/o001-localization-regressions-1.log`). The new inactive-language-segment regression, Settings popup checks and Operations/save checks passed. `EveryV3UiPrefab_HasSharedEnglishAndPersianCoverage` failed with 226 missing/broken bindings, including FirstLaunchNarrativeSequence and Operations dashboard popups such as ConfirmRaid/EndOfDayReport. These prefab files are unchanged by this patch; no clean-baseline execution was performed. The failure is retained and is not waived by the successful Farsi mission journey.
