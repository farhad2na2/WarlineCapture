# PM Rejection: Art/Atlas Deterministic Mockup Pass

Date: 2026-05-14
Lane: Art/Atlas
Topic: M01 step-by-step gameplay target-lock mockups

## Decision

Rejected. The first Art output under `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/` is not approved for user review or downstream Gameplay implementation. It must be removed or replaced before the next Art pass.

## Reason

The output reads as deterministic/assembled placeholder imagery rather than AAA target-lock mockups. The current goal is not programmer layout diagrams or mechanically composed proof frames. The goal is polished AAA visual target mockups, consistent with the approved M01 VisualLock targets, that the user can approve before Gameplay implements against them.

## Required Correction

Art/Atlas must regenerate only a tiny 1-2 sequence AAA sample set first, not the full mockup sequence.

Preferred first approval sample:

- `M01-01_TacticalStart_1920x1080.png`
- `M01-02_SquadSelected_1920x1080.png`
- one 1920x1080 contact sheet for those sample frames

Optional second approval sample only if PM/user needs combat/marker proof before approval:

- `M01-05_AttackPreview_1920x1080.png`
- one 1920x1080 contact sheet or sample board including the selected/tactical-start sample plus this attack-preview frame

The corrected sample must use generated or painted bitmap mockups with cohesive lighting, material detail, unit integration, tactical readability, polished UI, and direct alignment to `Design/VisualLock/GamePlay/M01_ApprovedIsometricGameplay/`.

The corrected sample must also be layered like the existing VisualLock lockups. Use `Design/VisualLock/SCN-08_RTSBattleHUD/LayerPack/manifest.json` as the implementation pattern. The output must include `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json` and per-frame layer breakdowns for the approved sample frames.

Required layer coverage:

- battlefield/camera plate
- HUD chrome and panel fills
- command buttons and stateful button chrome
- unit sprites/poses
- selection rings
- move, attack, objective, and invalid-command markers
- minimap content and viewport
- ARIA panel and assistant affordances
- toasts/log rows
- combat/objective FX
- result popup

Each layer must specify intended Unity object path or prefab ownership, rect/anchor/resolution, z-order, alpha/transparent-corner rule, and whether it is reusable, stateful, dynamic, or visual-reference-only.

## Hard Rejects

- deterministic renderer output presented as final target-lock art
- schematic, wireframe, flat placeholder, or programmer-facing layout art
- contact-sheet-only substitutes
- flattened-only mockups without layer manifests/source breakdowns
- visible debug/frame labels inside the gameplay image unless they are actual approved UI
- any runtime implementation, import, or Gameplay routing before user approval
- leaving rejected deterministic frames in the output folder beside corrected samples

## Routing

Current owner:
Art/Atlas

Held lanes:
Gameplay and QA/HCI

Next approval:
User approval that the corrected 1-2 sequence AAA layered sample is good and aligned before full-frame production or Gameplay implementation.
