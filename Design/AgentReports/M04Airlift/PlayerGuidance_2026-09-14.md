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
