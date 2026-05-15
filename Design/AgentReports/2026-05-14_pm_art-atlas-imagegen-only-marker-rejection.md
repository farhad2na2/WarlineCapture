# PM Rejection: Markers Still Do Not Match; Use Imagegen Only

Date: 2026-05-14
Lane: PM
Status: rejected; routed back to Art/Atlas

## Decision

Not approved.

The latest markers are still not 100% exactly matched to the original mockup. The current correction approach is also wrong: it reads like deterministic overlay/editing work rather than a cohesive imagegen target-lock frame.

## Required Process Change

Art/Atlas must stop deterministic/image-editing fixes for the flattened visual mockups.

Do not use:

- scripted compositing
- manual shape overlays
- deterministic marker placement
- local image-editing workflows
- pixel-patch edits to the current PNG
- patched-on UI/marker shapes

Use the imagegen skill for the flattened visual mockup pass.

Generate a fresh cohesive AAA bitmap mockup from the original reference and locked VisualLock references. The final output must look like one imagegen-created target-lock frame, not a base image with edited UI or marker patches.

## Marker Quality Requirement

Markers must match the original mockup exactly in visual language:

- ring thickness
- segmented gaps
- glow strength
- perspective ellipse
- size
- opacity
- terrain integration
- placement under each soldier

The selected-squad world shield/status bar and enemy red readability bars/rings must also be generated cohesively with the scene, not patched on afterward.

## Deliverables

Regenerate the approval sample visual PNGs with imagegen:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-02_SquadSelected_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01_StepByStepGameplay_SampleContactSheet_1920x1080.png`

After the imagegen output is chosen, update metadata only to describe the final approved-candidate image:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-01_TacticalStart_layers.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-02_SquadSelected_layers.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/AssetPrep_M01_Sample.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/SourceNotes.md`

## Scope Control

Do not generate the rest of the sequence.

Do not route to Gameplay, Designer approval, or QA/HCI yet.

Do not write runtime/import assets into `Assets/`.
