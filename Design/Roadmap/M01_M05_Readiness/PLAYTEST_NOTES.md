# Mobile gameplay assessment

Updated 2026-09-17. These are Editor observations, not a phone-performance or unfamiliar-player pass.

## M1

The normal route supports a useful first lesson: select the mission group, give a Move destination, then attack a marked hostile. English and Farsi routes reached victory and returned through Continue. The restored selection action and corrected cross-mission routing address actual progression blockers.

The final recovery route verifies a friendly-target rejection, localized feedback, pause/resume and successful return to the intended attack. Offscreen Show Me and cross-mission re-entry were exercised. These checks establish recovery mechanics; unfamiliar-player observation is still needed to judge how readily a new player understands targeting.

## M2

The current authored objective is to build a Barracks and train a rifle squad. English completion took about 49 seconds in the first replay. Keep this focused construction lesson; adding combat to match old result text would change the intended mission unnecessarily.

The visible build sequence, material-spend acknowledgment and recruitment all worked in English. Repeating the mission exposed a real building-baseline bug; after the fix, the Farsi route also completed. The result template and nine-step display were describing the old mission structure. Construction results now reflect actual building and recruitment facts in both languages; the tutorial displays its five current lessons.

Cancellation/reopening and pause/resume now complete in both languages. That recovery pass exposed an unexplained training interval; after the fix, both languages show what is happening, say no tap is needed, and continue automatically. Invalid placement, insufficient materials and repeated confirmation remain separate cases.

## M3

English and Farsi guided routes built one road barrier, selected defenders, moved them, used Hold and stopped both convoy elements. The English result showed 7/7 hostile units stopped, an undamaged post and three rifle losses. No health, positions, tutorial facts or time scale were edited to obtain victory.

The current waiting instruction explicitly says to stay on Hold, explains automatic fire and identifies the arrival estimate. This addresses the earlier ambiguity around a silent timer or mandatory Scan/Stop sequence. The wait marker is now a plain yellow ring, not a click crosshair over the defenders, and the instruction stays visible between waves. Final EN/FA replays passed.

Two experience concerns remain distinct:

- Observed cue defect: Move was offered again when one soldier arrived before the selected formation finished travelling. The read-model fix waits for the group; its focused regression and subsequent EN/FA routes passed with one Move then Hold.
- Pacing adjustment after player feedback: first arrival is now 25 seconds and second arrival 75 seconds, with a persistent wave/countdown/combat status. The final normal-speed capture shows 16 seconds between convoy elements where the earlier revision showed 31. Preserve optional preparation as a choice; the next pacing decision should use unfamiliar-player observations.

## M4

The Farsi introduction starts at the top of the rescue explanation, with a visible scrollbar when the larger text needs more room. Continue remains below the text viewport. Team navigation appears after the introduction, leaving room for the mission purpose. Focused checks also preserve deliberate scrolling when long copy actually needs it.

The critical gameplay gate is the complete transport sequence: select four specialists despite overlapping vehicles, board the APC, drive and unload, land the helicopter, select the specialists again, board, wait for clearance and depart. The EN first-clear and FA replay both completed this sequence and returned to campaign. The guide initially framed the whole wheel for Board; it now frames the actual sector. Both runs used the visible specialist group-selection action, so overlap with the helicopter did not require individual taps. The current route shows a specific wait after boarding and guides departure when clearance completes. The final arrow fallback was visibly confirmed on both transport stages in EN/FA. Recovery variations and phone touch still need their separate checks.

## M5

The planned gate is a full compound breach through current visible controls: attack the marked gate, enter, destroy the radar, handle remaining threats, secure the archive and return from results. Current opening checks passed in English and Farsi. Both normal routes completed, revealing repeated Attack guidance and an empty camera view during recovery. After the focused fixes, the English route uses one attack per marked structure and shows the actual archive with a green area ring and a readable countdown. The Farsi post-fix route also passed, with the same recovery framing and countdown. These are clarity fixes; enemy health, timings, mission rules and rewards are unchanged.

## Completion-pass assessment

The current English 1280×720 and Farsi 2400×1080 routes completed at normal speed with active comics played fully, pause/resume and deliberate recovery actions. ARIA body text no longer auto-shrinks, action buttons stay outside its scroll viewport, and narrow-screen placement arrows avoid the minimap and cancellation message. Runtime voice paths remain in the chosen language; gameplay narration is silenced at outcomes while authored debriefs remain audible.

These changes address observed clarity and reliability defects without adding commands, changing enemy health, bypassing objectives, or reviving hidden Do It controls. The M3 timing adjustment above is the documented pacing exception. The full coverage and finishing recovery/build status are in VERIFICATION.md.

Next external review: install the candidate on representative phones, check touch/safe-area/frame pacing/thermal behavior across repeated missions, and observe 3–5 unfamiliar players without coaching. Judge their hesitation and incorrect actions, especially M3's defense wait and M4's transport handoff. No phone or independent-player pass is inferred from Editor completion.
