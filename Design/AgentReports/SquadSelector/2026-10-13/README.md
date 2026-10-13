# Squad strip implementation — 2026-10-13

Owner approved design E: continuous horizontal scrolling within the existing squad strip, portrait-first fixed-size cards, unchanged command row and remaining HUD. Small rosters stay at the same width; overflowing rosters clip at the viewport and offer in-strip edge chevrons. No extra header, counter, selection summary, pager bar or Clear button.

## Selection behavior

Tap a squad to select it; tap its checked card to deselect that squad. A swipe never selects a card. Stable group identity is captured on pointer down, so casualties or roster reorder cannot select a replacement squad on release. ARIA and native probes read presented geometry and perform the same normal touch taps and swipes. Their internal five-slot observation window remains synchronized with scrolling; it is not a separate player pager.

The former Clear control served ARIA's accumulated multi-squad selection reset. It is unnecessary as a separate player control: ARIA now deselects checked cards through ordinary touches before assembling a different force.

## Evidence and acceptance

- Visual direction: owner approved E, including 1 and 2 squads. Native EN/FA captures cover 1, 2, 5 and 8 squads at 1920; overflow also at 2400. Command control bounds are asserted unchanged.
- Focused native Editor checks: run06 passes builder, layout, actual runtime Bind stable identity regression, group toggle/projection, original portrait/selection checks, 111 planner cases and 17 compatibility checks. See run06/unity.log.
- Normal native Input System tests: native-input02 passes both EN overlay and FA camera/nested-canvas cases with 18 cards, 11 swipes, 17 paired contacts, 5 card toggles per case, zero hidden paging/direct scroll writes/interventions/unexpected input. See its XML and full log.
- Complete normal-input mission: s005-en03 passed with wrapper exit 0, real Victory at 258.025 seconds, normal speed, zero violations/interventions, and a frozen result followed by normal touch ADJUST SETUP return. Source unchanged across 40,730 files. This qualifies the isolated T7 candidate; a fresh run is required for the combined dirty main source. Fixture captures and isolated touch tests alone do not certify mission Victory/result/return.
- Real player/device acceptance: pending.

Failed evidence remains preserved in this T7 checkout: run01 dark portrait layer, run02/run03 compile failures followed by wrapper timeouts, run04 planner fixture failure, native-input01 premature presentation assertion, s005-en01 production binding failure and source-change guard; s005-en02 real normal-input Victory/return with unchanged source but failed wrapper exit 128 from temporary process pressure. Required pass markers were retained; no readiness gate was weakened.

## Other agents

Do not recreate controls or text above the strip. Use MatchHudSquadTouchNavigation for reachable targets and normal touch input. Only a completed actual card tap can count as selection; preparatory swipes must not. Existing ARIA Play/Stop remains unchanged. Integrated S005 evidence must be collected again after this main update, since separate source snapshots do not qualify the final combined candidate.
