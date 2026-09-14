# Campaign tutorial clarity and attention

## Behavior

- M3–M5 command camera starts 40 units above the focus ground, replacing 55. Cinematic shot framing and user zoom controls retain their authored behavior.
- M1–M5 launches, replays and retries request full mission guidance regardless of saved replay-tutorial/minimal settings. Direct launch requests use the same policy. Older in-memory M1/M2/M5 sessions also retain required mission instructions.
- All five missions use live next-action cues. Selecting an interaction mode replaces its button cue with the actual unit/destination. Actual selection advances the tutorial; a raw click cannot complete the M1 mission objective.
- ARIA stays visible while M1 switches from a command instruction to its destination instruction. Narration still changes to the localized substep and retries a rejected narration request. Moving units show the existing arrival/wait instruction without repeating Do It or Show Me.
- M5 reads the persistent Select state used by the rest of the HUD. Its completed selection no longer points back to Select; moving and wait states do not offer misleading repeat commands.
- Show Me is visible only for a resolvable target that is not already visibly indicated. It hides immediately while an accepted camera reveal is in progress.
- Yellow UI frames get a gentle 2.4-second border pulse; world target rings vary line width gently. Show Me uses the same border treatment after four seconds without progress. Changing the action or hiding the button resets the delay, so wrong/repeated clicks do not complete guidance. Reduced Motion uses a steady border.
- Attention is presentation-only, uses unscaled time, consumes no raycasts, and does not move or resize any button hit area. Pulse borders draw behind captions. Caption placement is clamped to the screen, including the leftmost squad card.
- Long ARIA instructions show a cyan scroll indicator when constrained by the minimap. Players swipe the full text area; the indicator is not a small touch target. Timer/status refreshes preserve scroll position, and a new instruction resets it.
- All displayed instruction/caption text remains in existing localization catalogs; there are no new unconfigured UI strings or voice replacements.

## Validation scope

Editor only. Evidence captures remain under `/private/tmp/warline-campaign-tutorial-entry`, outside Design.

Focused validation includes the 45-case campaign entry/retry/settings matrix, 18 old M1/M2 session projections, a legacy M5 replay progressing from the plan through real selection, camera height, attention timing/reset/reduced-motion/touch behavior, caption bounds, and live-mode/selection/destination/arrival tests across the five missions. Existing M1/M2/ARIA/M3/M4 suites, M5 rule/content integration, and architecture checks are included.

The live entry probe uses an isolated completed profile with Assistant guidance Off. It launches M1–M5 from the real campaign menu in English and Farsi, waits through camera transitions, checks visible ARIA instruction text and actionable guidance, and exercises the first action in M1/M2/M4/M5. M3 opening and later construction/command progression are covered separately by the existing guidance regression; this probe does not claim a new full mission completion playthrough.

## Results

- Final focused wrapper run: `/private/tmp/warline-campaign-clarity-verified.log`, exit 0. `[CampaignTutorialClarityFinal] result=Passed`.
- Entry policy: 45 combinations; 18 legacy early-mission sessions; legacy M5 replay advances from briefing through actual selection. Camera height, attention delay/reset, Reduced Motion, fixed touch areas and edge-caption bounds passed.
- Shared Show Me: 10 tests. Construction transactions: 6; M1 guidance: 14; M2 guidance: 42; shared ARIA: 24; M3 rules: 12; M4 integration: 10. Tutorial next-action suite passed.
- M5 rules and integration passed (4 integration cases); voice catalog passed with 30 clips, 16 tutorial routes and two locales.
- Architecture: 9 fixtures, 139 passed, 0 failed. No C# or Burst compilation errors in the final run.

- Bilingual entry wrapper `/private/tmp/warline-campaign-tutorial-entry-verified.log` exited 0: all ten real menu entries passed (English Replay and Farsi Retry). Screenshots were inspected for readable ARIA, camera framing and unclipped captions.
- Visual inspection found an additional M5 Farsi status overflow: the text area correctly held the content but exposed no scrolling cue and reset to the top each second. The fix adds the passive scrollbar and preserves the reader’s position. `/private/tmp/warline-aria-breach-status-fixed.log` exited 0 with eight English/Farsi viewport cases across 1920/2400 widths; HUD dock and source-growth checks also passed.

- Final live wrapper `/private/tmp/warline-campaign-tutorial-entry-attention.log` exited 0 with `[CampaignTutorialEntry] result=Passed`: all ten entries passed again on the final UI code. In both languages, M5 also checked an active pulse after five idle seconds, immediate Show Me hiding on click, and a camera reveal of the unit target. The Farsi post-reveal capture shows the complete status including the clock. These are entry/early-interaction checks, not full campaign completion playthroughs.
