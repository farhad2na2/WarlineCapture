# Approved match HUD — native review

Implementation follows the six owner-approved ImageGen proposals. The images below are native Game view captures, with real M04 state (empty transport, 0/10), not generated mockups. A fresh M01 journey passed through normal Input System touch controls, real combat, victory and Campaign return in English and Persian. Physical device acceptance remains pending.

| Surface | Native changes |
| --- | --- |
| HUD | 26 px feedback, padded ribbon which clears long ARIA panels, square blue minimap zoom |
| Commands | Header and Close follow the radial wheel; disabled Destroy greys; selected portrait preserved |
| Passengers | Navy drawer below status strip, readable capacity/rows, grey disabled Unload All; selection panel keeps full width |
| Build | Single-item card uses available space; detail preview remains non-selecting, actual cost/footprint retained |
| Placement | Selected building supplies its own portrait; invalid confirmation fully grey |
| Exchange | Input resource supplies recipe/queue icon; readable routes and amount; disabled submit greys |
| Pause / Settings | Action emphasis, centered close icons, compact slider thumbs, padded Persian tabs/title, no invented time/objective fallbacks |

Squad cards scroll directly. No pagination panel is added above them. No map geometry, mission bindings, budgets, outcomes or ARIA commands were changed. Existing typography, portraits and military UI materials are reused.

## Actual Game view captures

### Feedback and minimap

![Native HUD](FinalNative/hud-en-1920.png)

### Commands

![Native commands](FinalNative/commands-en-1920.png)

### Passengers

![Native empty passenger drawer](FinalNative/passengers-en-1920.png)

### Build and placement

![Native build menu](FinalNative/build-en-1920.png)

![Native placement](FinalNative/placement-en-1920.png)

### Exchange

![Native exchange](FinalNative/exchange-en-1920.png)

### Pause

![Native pause](FinalNative/pause-en-1920.png)

### Settings

![Native settings](FinalNative/settings-en-1920.png)

## QA gates

- Visual direction: owner approved.
- Native-view automated fixtures: 174 checks passed in English and Persian. Covers disabled/enabled recovery, portrait binding, 0/4/10 passenger capacities and scrolling, icon identity, amount visibility, repeated authoring, responsive wheel cache, Settings close-button clearance and RTL stat/rail gutters.
- Native rendered review: English at 1920×1080 and 1280×720; Persian at 1920×1080 and 2400×1080. Captures recorded in FINAL_CAPTURE_MANIFEST.json. Loaded passenger counts in fixtures are not claimed as live occupancy or readiness.
- Complete touch-input mission, victory and return: M01 passed in English/Persian, 23 gestures, outcomesInjected=0. This exercises ARIA guidance and Show Me; autonomous ARIA Play and full loaded M04 extraction are not claimed.
- Physical player/device acceptance: pending.

Native builder source is reproducible through ApprovedMatchHudPolishBuilder.Build. Existing font-atlas changes remain local and excluded from the PR.

Intermediate compile, authoring, capture-transition and Editor-startup failures remain in the validation evidence. Compilation and mockups are not mission-readiness evidence.
