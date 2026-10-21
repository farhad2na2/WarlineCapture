# Skirmish saga menu — visual review

**Please review before merging.** This branch remains unmerged.

The Unity Game view scaling is corrected: a dense logical canvas now preserves the authored screen size of controls. Native Play mode measured a 100 px footer and 80 px mission stops at 2400×1080. Rectangular yellow panel outlines and the old safe-area guide perimeter are removed.

The theatre now covers **66.1%** of the open map, retaining the **10% aspect** and **12 px halo-gap** limits. Blue City Crossroads, gold Industrial Basin and teal Airfield Plains follow the approved palette. At 4:3 the battlefield road pans to retain mobile-sized stops; objective tabs jump to their chapters. The selected theatre cue uses the portrait preview without covering its title or progress.

## Compare the approved direction

![Battlefield comparison](saga-mockup-comparison.jpg)

![Theatre comparison](theater-mockup-comparison.jpg)

## All native screens

The PNGs are unmodified Unity captures. Progress in the static matrix is an explicitly labelled capture fixture. Actual earned medals and profile reloads are evidenced separately by normal-input native journeys.

![Battlefield matrix](saga-matrix.jpg)

![Theatre matrix](theater-matrix.jpg)

[Full-resolution PNGs and SHA-256 manifest](../captures/final-15/manifest.json)

Automated checks and normal-input evidence are listed in [STATUS.md](../STATUS.md). Owner visual review and real player/device acceptance remain pending.

[Persian 16:9 Desert Base selection regression](../captures/final-15/theater-16x9-fa-IR-selected-DB.png) · [English 20:9 Airfield Plains selection regression](../captures/final-15/theater-20x9-en-selected-AP.png)

## Acceptance scope

95 focused tests and all 113 pure geometry cases pass. The 18 native captures include the two additional selected-map regressions. Current normal-input journeys use the actual zoom cue; cold-process reloads use the footer. Both EN and FA native journeys won S002 through touch navigation and ARIA, then showed the result medal and returned ribbon. Fresh-process reloads retained those medals without seeding progress. [Final proof manifest](evidence/final-validation.json) · [English result](evidence/native-en/journey.terminal-result.png) · [Farsi result](evidence/native-fa/journey.terminal-result.png). No merge has been performed.
