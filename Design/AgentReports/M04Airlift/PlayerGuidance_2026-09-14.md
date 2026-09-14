# M4 player guidance correction — 14 September 2026

## Reported failure

In Farsi lesson 4, selecting the Select command left the same yellow command cue on screen. Selecting three specialists did not explain the missing fourth person. Show Me remained clickable beside the existing indicator and did not reveal a useful next action.

## Root causes and changes

- The command-state read model read one-shot orders but ignored `RuntimeGameplayStateComponent.SelectionModeActive`. Entering Select clears the one-shot order, so the assistant interpreted the accepted selection mode as None. The shared reader now reports Select and resets its cached queries with the world.
- The assistant refreshed command mode inconsistently across missions. Managed tutorial cues now read accepted live input state before resolving the next action.
- Show Me checked whether a target existed, not whether its indicator was already visible. It now appears only for a recoverable target outside the playable view and hides once that target is revealed. Waiting and completed-selection cues are cleared.
- Once group selection is active, the Select control cue becomes a gold world marker that remains visible through occluding props. Partial selection targets an unselected specialist and reports the selected count in the configured English/Farsi copy. Completing selection lets the existing mission-fact projection advance the lesson.
- Do It activates Select when an action lesson lacks the required selection. It is hidden while the next action is a world gesture, so it cannot toggle Select off or issue a premature Board order.
- Selected rescue groups use the localized specialist identity instead of the generic soldier title.
- Boarded specialists count toward the accepted boarding action; losing their selection rings does not restart Select during transfer.
- The pickup destination is beside the specialist group, so the APC does not park on the people being selected.
- ARIA removes the empty primary-action row when both actions are hidden. The field-guide and mission-navigation controls retain their existing layout.
- The bottom feedback bar now updates its localization binding before becoming visible. Previously, enabling the panel could restore its prefab's example “Attack unavailable” message over the live Select instruction. A focused inactive-to-active binding check and a final Farsi visual pass cover that failure.

Progress text replaces the current selection instruction rather than concatenating multiple lessons. Narration retains its canonical recorded lesson text, avoiding a Farsi UTF-8 message overflow.

The recorded lesson audio is unchanged. The new contextual progress copy and specialist group label are localization-catalog entries.

## Validation scope

The new player-route probe opens M4 from the menu, uses the actual HUD button listeners and normal input-command handlers, deliberately submits an incomplete screen-space selection rectangle, pans away using mission camera navigation, recovers the missing person with Show Me, and continues through the rescue route. Screen rectangles use the normal deferred screen-rectangle command, which invokes the same rectangle selection handler as a drag. It does not inject selected tags, passenger facts, unit positions, health, mission outcomes, or completion flags.

This is Editor validation, not Android hardware validation or synthesized operating-system touch input. Screenshots remain in `/private/tmp`, outside Design.

## Results

| Check | Result | Evidence |
| --- | --- | --- |
| English M4 route, 1920×1080 | Passed through result, unlock persistence and campaign return; partial selection recovered; four useful Show Me actions | `/private/tmp/warline-m04-final-regression-en.log` |
| Farsi M4 route, 1920×1080 | Passed through result and campaign return | `/private/tmp/warline-m04-player-fa-final2.log` |
| Farsi M4 route, 2400×1080 | Passed through result and campaign return; partial selection recovered; four useful Show Me actions | `/private/tmp/warline-m04-player-fa-wide.log` |
| Shared mission next-action regression | Passed, including M1–M4 guidance suites | `/private/tmp/warline-m04-final-regression-en.log` |
| Show Me/Do It focused regression | Six cases passed, including asynchronous camera focus, stale selection mode, occluded-marker depth policy, missing selection and waiting | Same log |
| M4 mission integration and additional target/state/copy checks | Passed; boarded passengers do not restart Select; English/Farsi copy fits narration storage | Same log |
| ARIA presentation/layout | 192 cases passed; EN/FA, 16:9/20:9, normal/large text | Same log |
| Architecture, rerun after the feedback-binding fix | 139 passed, 0 failed across nine fixtures; no policy exceptions added | `/private/tmp/warline-m04-architecture-verified.log` |
| Final Farsi partial-selection visual and feedback binding | Passed at 2400×1080: missing-person marker visible, Select cue cleared, Show Me hidden, correct selection feedback | `/private/tmp/warline-m04-feedback-visual-final.log` |

The original stale Select behavior was reproduced by the first live player-route attempt, despite the isolated fake-gateway tests passing. The real-state regression now exercises both persistent selection state and one-shot order state. The Farsi pass also caught and resolved a too-long concatenated progress message. Those failed intermediate attempts were not counted as successful QA.

This report covers the reported guidance flow and the exercised M4 rescue route; it is not a claim that every possible gameplay path is bug-free.

## Follow-up: legacy boarding buttons

Removed the legacy Board All and Cancel footer actions from both boarding directions. The transport-to-passenger prompt now reuses the existing English/Farsi “Select units to board” localization entry. The command wheel and world-target boarding remain available.

All 15 feedback checks passed, including error-message expiry and command switching without restoring the retired buttons. The Farsi 2400×1080 M4 player route passed through APC boarding, unloading, helicopter boarding, extraction, results and campaign return, with live assertions that both legacy buttons remain hidden during boarding. APC and helicopter boarding screenshots were inspected outside Design. Evidence: `/private/tmp/warline-m04-legacy-boarding-removal.log`.

## Follow-up: landing safety countdown and overlapping selection

Lesson 10 now displays a live countdown and explicitly says no click is needed while the helicopter is safely inside the landing zone. Separate configured English/Farsi instructions explain an enemy contesting the zone or a helicopter outside it. A yellow world cue identifies the landing zone; an off-screen target uses the existing Show Me recovery. Returning an out-of-zone helicopter guides selection, Move, then the landing destination. The duplicate primary field-guide action is absent during the wait. The original recorded narration is retained.

During specialist-selection and boarding lessons (4, 5 and 9), a general selection rectangle containing specialists prioritizes them over vehicles and armed escorts. A vehicle-only rectangle, explicit vehicle filter, and other lessons retain their normal behavior. This policy belongs to the mission-aware selection lookup rather than the general rectangle handler.

New checks distinguish countdown, contested and out-of-zone status, validate both languages against the message-size limit, and verify the selection filter's scope. The live route now deliberately includes the helicopter and APC in the specialist-selection rectangle before boarding, asserts that neither vehicle was selected, and checks the visible countdown/zone cue after boarding. The Farsi 2400×1080 route passed through extraction and campaign return; the countdown screenshot was inspected. Evidence: `/private/tmp/warline-m04-landing-guidance-fa.log`.

The final English 1920×1080 route also passed through results and campaign return after the selection-policy refactor. Shared M1–M4 guidance checks, 192 bilingual layout cases, and all 139 architecture checks passed in `/private/tmp/warline-m04-landing-final-en.log`. Both countdown screenshots were inspected. The initial architecture run rejected growth in the general rectangle handler; that run was not counted as a pass, and no architecture exception was added.

## Follow-up: empty opening on replay

M4 still suppressed its guidance for replay/retry with Replay Tutorial disabled, and for minimal guidance. Contextual guidance also skipped introductory lessons. The previous player-route probes created a fresh campaign profile and therefore did not cover this completed-profile entry path.

M4 launch and retry payloads now require full tutorial guidance and enable replay tutorials. The runtime projection independently preserves full M4 guidance for older payloads already in memory, while retaining session/attempt validation and cinematic/gameplay readiness gates. Every new attempt starts at lesson one and advances through its real acknowledgement and mission facts.

The regression covers all nine first-clear/replay/retry × full/contextual/minimal combinations, including legacy payloads with the replay toggle off and general assistant guidance Off. The opening probe uses a separate completed M4 save, launches through the campaign page, waits for the cinematic, verifies visible title/body and the Continue cue, clicks Continue, and verifies the next lesson's action cue. It does not clear the user's progress or change their persisted settings.

English and Farsi completed-profile opening probes passed, and both opening screenshots were inspected. Evidence: `/private/tmp/warline-m04-replay-opening-en.log` and `/private/tmp/warline-m04-replay-opening-fa-verified.log`. The English validation also passed shared M1–M4 guidance checks, 192 bilingual layout cases, and all 139 architecture checks. An initial test-harness compile error was corrected before these successful runs; its timed-out run is not counted as validation.
