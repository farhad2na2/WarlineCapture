# Approved match HUD implementation

Owner-approved six proposals. UI-only implementation on codex/match-hud-approved-polish. No mission bindings, map geometry, economy, ARIA behavior or outcome changes.

Implemented: readable feedback, square minimap zoom, coherent disabled graphics, selected-building placement artwork, expanded single-item build card, passenger drawer sizing/rows and navy surfaces and unload labels, exchange hierarchy/stepper, pause action emphasis, settings thumbs, centered close icons.

Squad navigation remains direct scrolling. No strip is added above the tray.

## Evidence gates

- Visual direction: owner approved.
- Native authoring: run04 passed the required builder marker. Runs01/02 compile failures and run03 Graphic component conflict retained under Logs.
- Automated native view fixtures: 174 checks passed for English/Persian availability toggles, placement artwork, feedback size, minimap art, passenger capacities and direct scrolling. Final fixtures passed including amount visibility, repeated authoring order, resource-icon identity, radial close ownership and disabled-state restoration.
- Native rendered visual comparison: M04 final matrix passed at standard, compact and wide sizes in English/Persian. Final M02 build/placement matrix is captured.
- Complete normal-input flow with ARIA guidance, result and return: M01 passed in English/Persian, 23 Input System touch gestures, real combat, outcomesInjected=0. Autonomous ARIA Play and full loaded M04 extraction are not claimed.
- Real player/device acceptance: pending.

Pause audit removed invented fallback 14:32 / Capture Enemy HQ values and authored 14:30 autosave timestamp. Unavailable measurements show a dash; live read models still supply values when present.

Existing font changes are excluded from this UI change. Pre-existing HUD edits are preserved in Preexisting/ for provenance.

Intermediate native capture run02 completed M02 but its attempted in-process transition to M04 failed (Editor scene open during Play-mode teardown); the run remains failed. M04 is captured in its own checked wrapper.

Live M04 review found wheel-heading coordinates tied to the full-screen overlay and feedback overlapping the long ARIA rail. Both were corrected before the final native pass. The ribbon now reserves space when ARIA extends into its row.

Final spacing pass reserves the Settings close-button column, gives Persian tab labels 24 logical px right padding, separates Build stat columns by 18 logical px and reserves Exchange selection-checkmark space. Native review includes English 1280/1920 and Persian 1920/2400.
