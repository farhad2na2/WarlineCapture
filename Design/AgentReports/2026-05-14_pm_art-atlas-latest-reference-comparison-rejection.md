# PM Rejection: Latest Art Pass Still Does Not Match Original Reference

Date: 2026-05-14
Lane: PM
Status: rejected; routed back to Art/Atlas

## Compared Files

Latest Art output reviewed:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-02_SquadSelected_1920x1080.png`

Reference:

- original selected gameplay reference provided by PM/user in thread

## Decision

Not approved.

The new pass is closer than the prior rejected marker output, especially on selected rings and enemy red bars, but it still does not match the original reference quality or HUD structure. It should not be routed to Gameplay, Designer approval, or QA/HCI yet.

Additional PM instruction: markers are still not 100% exactly matched to the original mockup. Art/Atlas must stop deterministic/image-editing fixes for this visual pass and use the imagegen skill for a fresh cohesive bitmap mockup.

## What Improved

- Selected player soldiers now have visible blue/cyan under-foot rings.
- Enemy soldiers now have visible red above-head bars and restrained red foot rings.
- A selected-squad blue shield/status treatment is present above the player squad.

## Remaining Mismatches

1. HUD quality is still below the original reference.
   - Current HUD uses bright cyan construction-line corner strokes and oversized outlines that read like layout/debug guides.
   - Original reference uses polished beveled dark glass/metal panels with restrained cyan edge trim, inner depth, and integrated glow.

2. Command bar contents and order are wrong.
   - Original reference shows `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`.
   - Current output shows `STOP`, `HOLD`, `MOVE`, `ATTACK`, `SPECIAL`, plus an oversized disabled Build block.

3. Objective panel is simplified.
   - Original reference includes `OBJECTIVES` and `STAR GOALS`.
   - Current output only shows the objective area and misses the original panel density.

4. Selected-squad world status treatment is too flat.
   - Original reference shows a blue shield icon plus clean segmented blue horizontal bar above the selected squad.
   - Current output has a flatter rectangular bar and an extra visible squad label that does not match the reference.

5. Bottom HUD still lacks original AAA finish.
   - Squad cards, log panel, command buttons, and Build treatment are not yet as polished as the reference.
   - The Build disabled state reads heavy/crude instead of secondary and intentional.

6. Minimap still does not match the reference finish.
   - Original minimap has a polished beveled frame, clearer tactical read, and integrated viewport treatment.
   - Current minimap frame/readability remains below that bar.

7. Marker generation approach is wrong.
   - The markers still read like edited/placed overlays rather than imagegen-native elements in the same art pass.
   - The final marker look must match the original reference exactly: ring thickness, segmented gaps, glow strength, perspective ellipse, size, opacity, terrain integration, and placement under each soldier.

## Required Art/Atlas Correction

Art/Atlas must revise both sample frames and the contact sheet:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-02_SquadSelected_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01_StepByStepGameplay_SampleContactSheet_1920x1080.png`

Required fixes:

- Remove debug/construction-line-looking cyan corner strokes from HUD panels.
- Use the original reference's restrained beveled cyan edge trim, layered dark glass/metal depth, and controlled glow.
- Restore the selected-state command bar order: `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`.
- Do not replace `SELECT` with `SPECIAL` in this selected/no-command sample.
- Restore original objective panel density, including Star Goals, unless Designer explicitly rejected that content.
- Rework Build disabled treatment so it is secondary and intentional, not a dominant crude block.
- Rework the selected-squad world status treatment to match the original shield plus segmented blue bar. Remove the extra visible squad-name label unless the reference requires it.
- Bring squad cards, log panel, command buttons, minimap frame, and top bar to the same AAA VisualLock quality as the original reference.
- Use the imagegen skill for the flattened visual mockup pass.
- Do not use deterministic image editing, scripted overlays, local compositing, manual marker drawing, or pixel-patch edits to fix the current PNG.
- Generate a fresh cohesive AAA bitmap mockup from the original reference and locked VisualLock references, then update LayerPack metadata to describe the chosen result.

## LayerPack Requirements

Update LayerPack metadata for all changed UI/HUD and world-status layers:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-01_TacticalStart_layers.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-02_SquadSelected_layers.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/AssetPrep_M01_Sample.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/SourceNotes.md`

LayerPack entries must identify source asset/slice notes, rect, anchors/world anchors, z-order, alpha/transparent-corner rule, state, and Unity owner path for all changed HUD groups.

## Scope Control

Do not generate the rest of the sequence.

Do not route to Gameplay, Designer approval, or QA/HCI.

Do not write runtime/import assets into `Assets/`.
