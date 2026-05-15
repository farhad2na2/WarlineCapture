# PM Rejection: Selected Rings, World Health Bars, And HUD Do Not Match Original Reference

Date: 2026-05-14
Lane: PM
Status: rejected; routed back to Art/Atlas

## Visual Check

Compared the current selected frame against the original reference image provided by PM/user.

Current checked frame:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-02_SquadSelected_1920x1080.png`

Original reference treatment:

- selected player squad has four clean blue/cyan segmented rings under the soldiers' feet
- selected squad has a blue shield icon plus segmented blue horizontal status/health bar above the selected soldiers
- enemy soldiers have readable red segmented health bars above heads
- enemy soldiers also have restrained red foot rings as affiliation/readability treatment
- HUD has premium beveled sci-fi panels, layered dark glass/metal depth, crisp cyan edge lighting, dense readable typography, integrated icons, polished command buttons, high-quality squad cards, and a detailed minimap frame

## Decision

The current Art output does not match the original reference.

The under-foot selected rings are too weak/plain and do not read like the original clean neon segmented blue rings. The selected-squad world status/health treatment above the soldiers is missing. Enemy above-head red health bars and restrained red foot rings must also remain readable and match the original style.

The HUD quality is also not acceptable. Compared with the original reference, the current HUD reads flatter and less finished: weaker bevels/inner shadows/edge glow, simplified objective/log panels, lower-detail squad cards, crude command/Build button treatment, and minimap framing/readability below the original AAA VisualLock finish.

## Required Art/Atlas Correction

Art/Atlas must revise `M01-02_SquadSelected_1920x1080.png` and the contact sheet so the world-view combat readability elements match the original reference:

- Replace the selected under-foot rings with clean, high-quality, segmented blue/cyan rings matching the original reference and VisualLock marker sheet.
- Add the selected-squad blue shield icon plus segmented blue horizontal status/health bar above the selected soldiers.
- Ensure the selected-squad status/health treatment is anchored in world space above the squad/leader area, not only shown on the bottom squad card.
- Ensure enemy soldiers use readable red segmented above-head health bars and restrained red foot rings.
- Rebuild HUD quality in both `M01-01_TacticalStart_1920x1080.png` and `M01-02_SquadSelected_1920x1080.png` so objective/log panels, top resource bar, squad cards, command buttons, Build disabled state, and minimap frame match the original reference quality, style, and layout density.
- Keep the sample camera, world composition, and normalized unit scale unchanged unless directly required by the above visual fixes.

HUD must match:

- beveled dark glass/metal panels
- crisp cyan trim and controlled glow
- readable objective/log/resource/top-bar typography
- polished squad cards with rich portraits and health treatments
- premium command buttons with consistent geometry and icon language
- M01 Build disabled treatment that feels intentional, not crude
- detailed readable minimap frame and viewport treatment

## LayerPack Requirements

Update the implementation metadata so Gameplay can reproduce the visual exactly:

- Keep four explicit per-soldier selected marker ring entries in `LayerPack/Frames/M01-02_SquadSelected_layers.json`.
- Add explicit separate entries for the selected-squad world shield icon and segmented blue status/health bar.
- Add or correct explicit enemy affiliation/readability entries for red above-head health bars and red foot rings.
- Add or correct entries for HUD chrome groups that changed: objective panel, log/threat panel, top resource bar, squad cards, command buttons, Build disabled state, and minimap frame.
- Each added or corrected layer must include source asset, rect, world anchor, pivot, scale, z-order, alpha rule, visible state, and whether it is reusable/stateful/dynamic.
- Mark enemy red bars/rings as permanent affiliation/readability layers for M01-01/M01-02, not attack-target command markers.

Update:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/AssetPrep_M01_Sample.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/SourceNotes.md`

## Scope Control

Do not generate the rest of the sequence.

Do not route to Gameplay or QA/HCI yet.

Do not write runtime/import assets into `Assets/`.

## Next Gate

PM/user must visually approve the corrected selected rings and above-soldier status/health bars before any Gameplay implementation audit or Designer approval routing.
