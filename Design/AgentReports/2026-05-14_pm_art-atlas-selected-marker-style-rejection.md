# PM Rejection: Selected Marker Style Does Not Match VisualLock Quality

Date: 2026-05-14
Lane: PM
Status: rejected; routed back to Art/Atlas

## Decision

The latest Art/Atlas selected-marker pass is not approved.

The user reported that the new blue circles under the soldiers are ugly and do not match the previous clean blue VisualLock marker mockup. This is an Art quality issue, not a Gameplay audit issue.

## Rejected Output

Rejected current output:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-02_SquadSelected_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01_StepByStepGameplay_SampleContactSheet_1920x1080.png`

The rejected marker treatment must be replaced. It must not be polished, kept, referenced as acceptable, or presented again for approval.

## Required Art/Atlas Correction

Art/Atlas must replace the rejected marker pass with clean VisualLock-matched selected markers:

- Use the approved marker style reference:
  - `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_Markers_SelectionMoveAttackEnemyObjectiveHover.png`
- Use or match the clean runtime marker source:
  - `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Markers/selection_ring.png`
- Add one clean selected marker ring under each selected soldier in `M01-02_SquadSelected_1920x1080.png`.
- The marker must read as a thin/segmented sci-fi blue/cyan ring with transparent center, soft controlled glow, correct isometric ellipse perspective, and terrain-integrated lighting.
- Do not use crude filled blue circles, thick flat ellipses, high-saturation blobs, programmer debug rings, or quick paint-over marker shapes.
- Update the contact sheet after replacing the selected frame.

## LayerPack Requirements

Keep the four explicit per-soldier marker layers in:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-02_SquadSelected_layers.json`

Each marker layer must still include:

- source asset
- rect
- foot anchor
- pivot
- scale
- z-order
- alpha rule
- visible state

The source asset must point to the clean marker source or to a new approved clean marker asset, not to a flattened hand-painted circle.

Update:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/AssetPrep_M01_Sample.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/SourceNotes.md`

## Scope Control

Do not regenerate the rest of the sequence.

Do not change the approved sample quality direction, camera, world composition, HUD style, or normalized unit scale except where needed to replace the selected markers cleanly.

Do not write runtime/import assets into `Assets/`.

## Next Gate

After Art/Atlas replaces the selected marker style, PM/user review the corrected `M01-02_SquadSelected` frame and contact sheet again before Gameplay or QA receives any new task.
