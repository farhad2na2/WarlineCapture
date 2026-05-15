# PM Routing: Art/Atlas Selected Marker Correction After Gameplay Audit

Date: 2026-05-14
Lane: PM
Status: routed to Art/Atlas

## Context

Gameplay completed the corrected-sample implementation-readiness audit:

- `Design/AgentReports/2026-05-14_gameplay_m01-corrected-sample-asset-audit.md`

Gameplay did not stop with a blocker-only report. It audited camera/zoom, player/enemy scale, no-selection state, selected-but-no-command-mode state, Build disabled/hidden state, enemy affiliation/health, LayerPack metadata, and implementation readiness.

## Decision

Next active owner is Art/Atlas.

The user approved the current Art quality direction, but `M01-02_SquadSelected` is missing the blue/cyan selected marker circle under each selected soldier. Gameplay confirmed this is the remaining approval-blocking Art issue for the current two-frame sample.

Runtime implementation remains blocked. QA/HCI remains blocked.

## Required Art/Atlas Fix

Art/Atlas must revise only the selected-marker treatment:

- Add visible blue/cyan selected marker circles under each selected soldier in `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-02_SquadSelected_1920x1080.png`.
- Match the approved AAA imagegen VisualLock marker style, not a flat deterministic overlay.
- Replace group-only selected-marker metadata with four explicit per-soldier selected marker child layers or entries in `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-02_SquadSelected_layers.json`.
- Each marker layer must include source asset, rect, foot anchor, pivot, scale, z-order, alpha rule, and visible state.
- Update `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`.
- Update `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/AssetPrep_M01_Sample.json`.
- Update `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/SourceNotes.md`.
- Update `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01_StepByStepGameplay_SampleContactSheet_1920x1080.png` if the selected frame changes.

## Scope Control

Do not regenerate the full sequence.

Do not change the approved quality direction, shared camera, world composition, HUD style, or normalized unit scale except where required to add the selected markers correctly.

Do not write runtime/import assets into `Assets/`.

## Updated Task Files

- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/art-atlas_pm_message.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/gameplay_pm_message.md`

## Next Gate

After Art/Atlas delivers the selected-marker correction, Designer/PM/user review the corrected two-frame sample for approval.

Only after approval can Gameplay start the next implementation slice, and that slice is limited to `M01-01_TacticalStart` exactly from the approved layered sample and LayerPack.
