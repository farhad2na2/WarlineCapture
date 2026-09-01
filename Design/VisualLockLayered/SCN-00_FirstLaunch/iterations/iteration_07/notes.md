# Iteration 07 — V3 Skip Confirmation

## Target locks

- First Launch composition: `../../reference/SCN-00_FirstLaunchV3_ComicPlayback_Final_Target.png`
- V3 confirmation chrome: `../../../POP-02_ConfirmRaid/reference/POP-02_ConfirmRaidV3_Final_Target.png`

## Accepted result

- Replaced the legacy textured skip prompt with sharp V3 procedural chrome.
- Uses one constant 3 px frame on the modal and both action buttons.
- Uses real cyan/blue and red/orange button gradients rather than solid fills.
- Added authored vector-style warning, pause, and double-chevron icons; no new raster asset or duplicate UI texture was introduced.
- Restored all four localized bindings for title, body, keep-watching, and skip-intro text.
- English is left-to-right. Farsi uses Noto Sans Arabic, shaped RTL text, and right alignment.

The first Farsi capture was rejected because a global RTL flag also reversed English-only HUD labels. The final implementation derives RTL behavior from the localized string and keeps unrelated English labels unchanged.

## Validation

- Editor validation: passed (`skip=v3-bilingual`).
- Focused First Launch integration suite: passed, 10 tests.
- Runtime English: opened modal, Keep Watching closed it, Skip Intro advanced to Match.
- Runtime Farsi: language selection localized the comic and modal; Keep Watching closed it.
- Captured and visually reviewed at 1920x1080 and 4800x2160 in both languages.

## Captures

- `skip_confirmation_v3_en_16x9.png`
- `skip_confirmation_v3_fa_16x9.png`
- `skip_confirmation_v3_en_20x9.png`
- `skip_confirmation_v3_fa_20x9.png`
