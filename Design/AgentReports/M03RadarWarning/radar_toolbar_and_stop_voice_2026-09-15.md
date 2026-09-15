# Radar toolbar and Stop voice — 2026-09-15

## Report and design reference

The player clicked the highlighted Radar Pulse button during M3 lesson 8, but the mission did not advance. The Stop confirmation sounded Arabic in Farsi gameplay.

Radar Ping was already specified in `Design/M03_Radar_Warning_Production_Plan.md` (two mission-loaned uses, a 60-second simulation cooldown, and a living ground sensor) and the M3 tutorial catalog. It updates ground-vehicle intelligence; it is not an attack or a replacement for selected-unit Scan.

## Cause and repair

The command-input owner clears the toolbar's runtime listeners when binding and supplies its own callbacks. Its Support slot fallback still called the generic selected-unit Scan handler. The shell gateway's mission-radar routing therefore did not protect this path. Previous radar validation called that gateway directly and did not cover the displayed toolbar button; the full tutorial journey skipped the optional radar lesson.

The toolbar owner now sends the mission Radar Ping request when M3 defense context is active. It leaves generic Scan behavior in other contexts intact. The request enters the existing sensor/charge/cooldown transaction. Accepted requests receive localized feedback; unavailable requests use the mission's localized status. The new feedback is in the central English/Farsi catalog and its source copy catalog.

Stop confirmation now uses the existing full-sentence `TacticalFeedbackStoppedSelectedUnits` voice event, with separate English/Farsi recordings, instead of the short `TacticalBannerAcceptedStopTitle` recording. No new paid generation or external audio upload was performed. Local Whisper small auto-detection classified the old 1.52-second phrase as Arabic (0.836), and the replacement 2.88-second sentence as Farsi (0.884). This supports the player's report, but automated language detection is not a native-listener pronunciation assessment. The source texts of both recordings were Farsi; the old clip was not intentionally wired to an Arabic locale.

## Validation scope

- Command-audio checks passed all 10 cases, including the actual imported EN/FA clip references for Stop (`/private/tmp/warline-stop-voice-regression.log`).
- New Editor gameplay probes reach lesson 8 through the preceding tutorial, raycast the visible Radar Pulse button, dispatch its pointer click, and require scan acceptance, exactly one charge consumed, cooldown, no generic Scan targeting, and advancement to reinforcement.
- The probe's first setup run exposed an outdated Continue-button prerequisite; the current lesson requires opening Build. A subsequent run observed the previous frame's guide immediately after ECS advanced, so the probe now allows a bounded presentation-frame update before asserting alignment. Neither change alters runtime lesson completion or test outcome predicates.
- English gameplay passed in `/private/tmp/warline-m03-radar-toolbar-en.log`; Farsi passed in `/private/tmp/warline-m03-radar-toolbar-fa-v4.log`. Both wrappers exited 0. The Farsi probe's earlier pointer check ran from Editor update and did not hit the button; moving the probe to the existing end-of-game-frame driver made its raycast use the rendered Game View. No runtime click bypass or completion fact was added. The English before-click capture and Farsi gameplay captures were inspected. The English after-capture restores the original Farsi preference during probe cleanup, so it is not claimed as English visual evidence.
- The main Editor's live asset API saved the central EN/FA feedback entry. Its first response timed out while saving, so the on-disk entry and a subsequent live localized read were verified before proceeding. Live reflection confirmed the replacement Stop event is loaded. Evidence images stay under `/private/tmp`.
- The architecture check initially rejected growth in two frozen source files. Confirmation-voice methods and toolbar binding methods were extracted into cohesive partial files, reducing their original owners to 1,565 and 375 lines respectively. No architecture baselines, exceptions, or test thresholds were changed. The gameplay callbacks are unchanged by the extraction.
- One final-suite launch on the second QA project stalled before project initialization and its wrapper timed out (exit 124, `/private/tmp/warline-radar-voice-architecture.log`). It produced no test result and is not counted as a pass. Validation was rerun through the same checked wrapper on the closed gameplay-QA project; no licensing reset or unrelated Editor termination was performed.
- Final combined validation passed on the extracted source (`/private/tmp/warline-radar-voice-architecture-final.log`, wrapper exit 0): four radar cases, four AI regressions, one wreck regression, ten command-audio cases, and all 139 mission architecture checks across nine fixtures. The main Editor completed its final supported recompile with no errors. All task-owned validation wrappers have exited. Changes remain uncommitted; unrelated font changes were preserved.
