# Commander Profile V3 — Iteration 2

Status: review candidate; not accepted until explicit user confirmation.

This iteration supersedes Iteration 1 for review. It preserves the corrected
layout, crop, gradients, and constant 3 px borders, and replaces the temporary
icon substitutions with the mockup silhouettes:

- reticle, bar chart, badge shield, clock, and upgrade chevrons in the left rail;
- commander rank shield and edit pencil in the identity panel;
- wreath, mission reticle, civilian group, vehicle, and chart statistics;
- claimed check, checked crate, crate, reward badge, and lock states;
- history reticle/shields and the footer back, crate, and rank chevrons.

The bitmap icons are packed once in
`UI_V3_CommanderIcons_01.spriteatlas`; existing canonical core/main-menu icons
remain in their original atlases, so the prefab introduces no copied icon files.

Evidence:

- `commander_profile_v3_16x9.png` — actual Play Mode, 1920×1080.
- `commander_profile_v3_20x9.png` — actual Play Mode, 4800×2160 with the Game View set to that exact aspect before capture.
