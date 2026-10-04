# Match HUD UI/UX audit — 2026-10-04

Scope: existing native HUD, focused readability/localization fixes; no new visual direction or substantial mission redesign.

## Findings

- **Confirmed:** squad name strips occupy the same bottom space as HealthFrame. Live Farsi screenshots at 1920×1080 and 2400×1080 show the health bars crossing the labels. Native measurement: card height 193, NameStrip bottom offset 6 and height 34, HealthFrame top offset 174 and height 10. Separate the name strip from the health bar with a 6-unit gap.
- **Confirmed in source:** current-order presenter write directly to TMP despite localized text bindings on native HUD text. Repeated writes can undo shaping, font selection and locale alignment, and banner hide can retain an old runtime source. Use the shared UiLocalizedText presentation boundary, including clearing hidden banner sources. The legacy objective presenter was inspected but its objective references are unbound in the current prefab; it is left unchanged.
- Resource/Settings/Pause controls are visually contained at 16:9 and 20:9. The disabled ARIA/card/command state in the captured mission accompanies a gameplay startup failure; it is not accepted as a ready mission HUD.

## Evidence scope

Live before captures are `../InnerScreenUiAudit/After/input-20261004-082623/match-hud-2400-fa-IR.png`, `match-hud-1920-fa-IR.png`, and `match-hud-2400-en.png`. They were captured after normal touch navigation through Campaign/Briefing/Preparation/Deploy, with no injected outcome. The mission logs repeat missing packed render readiness/metrics/state-change ownership and virtualized-building/canonical-state count mismatch. ARIA remains Manual with zero actions.

Native prefab fixtures can establish presentation and localized update behavior, not mission completion. Full normal-input ARIA/result/return and real Android device acceptance remain separate gates. No device acceptance is claimed.

The complete normal-input before attempt timed out after ARIA remained Manual with zero actions (`warline-match-hud-before-01.log`). First fixture attempt failed because it assumed legacy objective references were bound; it also tried to restore the scene setup too early during Play-mode exit. Both fixture issues are corrected; failure evidence retained.

Native fixture `after-02` passed localization-source/font/atlas, repeated apply, hide/reopen, and name/health clearance assertions, but visual review rejected its screenshots: the automatic menu bootstrap covered the fixture. Capture isolation is corrected; these screenshots do not establish visual acceptance. Post-change normal-input `journey-after-01` failed with MissingMethodException because the new focused entry point was not yet imported; no journey result is claimed from that dispatch.

The isolated `after-03` fixture passed its original assertions, but visual review found the first name-strip adjustment incorrect: measuring HealthFrame during initial layout put labels outside the card. The same regression is visible in live `input-20261004-084858` captures; those images are rejected. The correction uses the authored 26-unit name-strip bottom spacing instead of measuring unsettled transforms. The fixture now checks containment inside each card in addition to a minimum health-bar gap. Post-change normal-input `journey-after-02` reached the native HUD and captured both locales/aspect ratios but failed ARIA/result/return at its explicit 90-second timeout with zero ARIA actions and the same startup errors.

## Final focused presentation verification

`after-04`: checked wrapper exit 0 and `[MatchHudUiUxReview] result=Passed`. Four native prefab fixture captures in `After/20261004-085516` cover English/Farsi at 1920×1080 and 2400×1080. Checks include repeated localized updates, source preservation, correct locale font/atlas, hide/reopen/clear, card containment and health-bar clearance. Visual review confirms each name is inside its card above the health bar. This fixture has a black background and authored/sample HUD data; it is not a complete mission screenshot or player/device acceptance. The current-order presenter is checked at the component level, not claimed as a completed player command interaction.

## Final live review and remaining gates

`journey-after-03` uses normal InputSystem touch navigation through Campaign, Briefing, Preparation, Deploy and narrative Skip/Confirm. Final native captures are `../InnerScreenUiAudit/After/input-20261004-085639`. Visual review of Farsi 1920×1080 and English 2400×1080 confirms the names are contained and clearly separated from the health bars. No injected mission outcome was used. Compilation and focused native fixture assertions pass.

The journey wrapper exits 1: `[InnerScreenUiMission] result=Failed` and `[ExistingEditorValidation] result=Failed`, with an explicit mission timeout. ARIA remains Manual with zero actions; render-readiness ownership/building-state startup errors persist. ARIA/result/normal return-to-menu acceptance therefore remains failed/pending; the 90-second focused deadline does not establish the eight-minute full journey as passing. Physical Android button/gesture, touch comfort and real-player acceptance remain pending. This follow-up includes the focused source fixes and reviewed final captures; rejected captures and raw failure logs remain local.
