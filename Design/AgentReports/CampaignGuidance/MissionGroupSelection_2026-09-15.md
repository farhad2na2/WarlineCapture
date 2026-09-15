# Mission group selection — 2026-09-15

## Player-facing change

M1–M5 selection lessons now highlight a full-width ARIA selection button instead of a tap marker over soldiers or the Select-mode button. The button names the required group, selects it through the normal selection-command queue, and focuses the camera on it. After an accepted click, the button and its cue disappear; the guide follows the next command or destination. Manual rectangle selection remains available outside this guided shortcut.

- M3 selects both four-person defender squads together. Radar vehicles, civilians and actors from another mission attempt are excluded.
- M4 selects all available rescue specialists without selecting the overlapping helicopter or armed guards. APC and helicopter selection lessons use their own explicit labels. Embarked passengers are excluded from ground selection.
- Labels and contextual instructions are in the central English/Farsi localization catalog. M4's old instruction to drag a rectangle was replaced with the named group button.

## Implementation

The HUD publishes a mission-role selection request. The existing RTS selection consumer owns the actual selection and feedback. Ordinary squad requests retain their squad scope. No new ECS system or direct UI mutation of selection tags was introduced.

The selection button reuses the existing ARIA button style, full-width layout and animated double-border/arrow guide. The shared next-action logic covers the five campaign mission tutorial shapes.

## Verification

- Shared M1–M5 tests cover selection, accepted click, cue removal, next command, destination and waiting states.
- Entity tests cover rescue specialists beside transports, dead/embarked exclusions, both defender squads, unrelated roles and previous attempts.
- English M4 completed through actual EventSystem pointer clicks for group selection, boarding, road transfer, unloading, helicopter boarding, landing security, extraction, debrief and campaign return. Farsi M4 completed the same mission route.
- Inspected English M4 and Farsi M3 rendered selection buttons: readable text, full-width touch target, aligned double border and arrow, no soldier tap marker.
- The final Farsi M3 check confirmed all eight defenders selected and holding. A subsequent optional-production probe race was corrected by waiting for the visible lesson before asserting its next-click guide.
- Final Farsi M3 rerun passed: actual pointer selection of eight defenders, Move/Hold, optional reinforcement production, three-star victory with zero civilian losses, frozen-clock finale, three debrief panels and localized results. Log: `/private/tmp/warline-group-selection-m3-fa-v2.log`.
- Architecture rerun passed all 139 tests across nine fixtures, plus the group-selection, shared guidance and layout checks. Log: `/private/tmp/warline-group-selection-final-validation-v2.log`. Both final wrapper runs exited zero with explicit pass markers. The first run found a helper byte-ceiling violation; the consumer now passes the complete request to its existing selection utility and stays below the original ceiling. A managed-array camera query discovered during recompilation was replaced with a Burst-compatible query builder.
- Main Editor confirmed compilation finished, no script compilation failures and Play mode stopped. Central catalog values were verified after import; `git diff --check` passed.

Validation is Editor-only. No screenshots were added to Design. This is a focused selection-flow change, not a claim that every mission has no remaining bugs.
