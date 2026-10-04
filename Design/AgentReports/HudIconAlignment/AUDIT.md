# HUD icon vertical alignment — 2026-10-04

The icon rectangles were centred but artwork had uneven transparent padding in the v01 atlas. ARIA Play/Stop sat below the button centre; selection Commands sat above it. HudIconButtonView now applies measured vertical artwork offsets to all 16 atlas icons. Sizes, horizontal/RTL placement, labels, callbacks and hit targets are preserved.

## Checks

- Focused wrapper: `warline-icon-centres-02.log`, exit 0, `[HudButtonIcons] result=Passed`, 32 independently measured artwork-centre checks across all 16 icons in English/Farsi. Existing listener/disabled-state checks passed.
- Initial dispatch failed because the editor bridge did not respond; existing Editor preserved.
- Broader native check: `warline-icon-centres-native-01.log`, failed on existing Farsi label-size mismatch for YOUR MAIN BASE. Failure retained; not claimed as passed.
- Scoped native alignment check: `warline-icon-centres-native-02.log`, exit 0, `[HudIconAlignmentNative] result=Passed`, nine screens at 1920×1080 and 1280×720, English/Farsi. Visible alpha bounds measured against native button centres; real touch covered squad selection, player/enemy camera focus, ARIA consent/cancel and opening Commands. Camera focus preserved current orders. This scope does not assert unrelated font sizing.
- Native visual review: ARIA and Commands artwork centred in English/Farsi; screenshots in After/.

The native check uses Skirmish S003, Desert Base / Air Mobile / Field. Its starting barracks visibly overlaps nearby map structures; this separate placement issue is not fixed by the icon adjustment. Runtime barracks entity reports origin (922,349), footprint (28,15); resolving the static structure intersection remains pending.

Full match completion and physical-device acceptance are not established by these HUD checks. Raw failure and success logs remain local under Logs/. Changes are uncommitted.
