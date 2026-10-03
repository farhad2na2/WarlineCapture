# Approved Main Menu column alignment

2026-10-03 (Europe/Berlin). The user approved the five-screen visual direction, with Store and Armory matching ARIA width and Credits matching Commander width so all five panels align.

Implemented in the Main Menu editor builder: one authored x-position and width for Credits, Commander, ARIA, Store and Armory. Settings moves to the left of Credits; Commander appears above ARIA. Rebuilt the native prefab through the repository macOS GUI-licensing wrapper. The source project was not edited.

Visual review: inspected native English phone, Persian phone and English tablet images; column edges align and settings does not overlap Credits. Revised ImageGen reference saved at `../../Mockups/fix-main-menu-v02-aligned-column.png`; generated bitmap positioning is approximate, native geometry is measured.

Automated checks: `[MainMenuColumnAlignment] result=Passed nativeCaptures=6 alignedPanels=5 locales=en,fa-IR ratios=16:9,20:9,4:3` logged. The fixture measures actual world-space left/right panel edges with a 0.5-pixel tolerance and confirms Settings is outside the Credits panel. Native prefab validation and edited-code whitespace check also passed. Wrapper exit status is recorded in `validation-status.txt`.

Normal-input walkthrough: not performed for this change. Fixture captures do not establish player navigation.

Real player/device acceptance: pending.

Remaining handoff work: shared layout across the other screens, safe-area margins, typography, complete Persian localization/mirroring, tablet composition, developer wording removal, real Commander stats/history/portrait, all-screen route captures and normal-input review. Existing Persian English labels and tablet unused space remain visible in these captures. Approval is recorded; it does not need to be requested again.
