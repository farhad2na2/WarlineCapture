# Visible mode navigation — 2026-09-18

Both Skirmish setup and Operations hid Back navigation behind the Warline logo. Replaced that upper-left hit area with a bordered button, the existing Back arrow asset and the explicit label “BACK TO MAIN MENU” / “بازگشت به منوی اصلی”. Both retain the shell history route and Main Menu fallback. The label has a shared localization key in the localization configuration, including locale font/shaping support. Both prefab builders reproduce the controls.

Verified all four actual UI hit-tested returns (both modes, English and Farsi) against the live shell route, with an isolated QA profile. All four screenshots were visually inspected. Editor state and QA save override restored afterward.

- [Live return checks](mode-back-live.txt)
- [Initial focused checks](mode-back-tests.txt): five passed; the old logo-border assertion was then updated to require the new visible Back button.
- [Skirmish EN](skirmish-back-en.png) / [FA](skirmish-back-fa.png)
- [Operations EN](operations-back-en.png) / [FA](operations-back-fa.png)

The updated [final layout check](mode-back-layout-final.txt) passed. All six focused checks are now passing.
