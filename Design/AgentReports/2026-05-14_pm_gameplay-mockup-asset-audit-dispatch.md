# PM Dispatch: Gameplay M01 Mockup Asset Implementation Audit

Date: 2026-05-14
Lane: Gameplay
Topic: Asset preparation audit for pixel-perfect implementation of M01 mockups

## Reason

The user wants Gameplay to audit the current Art mockups and LayerPack before Art revises them, so Art receives both Designer alignment feedback and Gameplay implementation feedback. The goal is to identify exactly how assets must be prepared for final implementation to match the approved mockups 100%.

## Assignment

Gameplay owns an audit only. No runtime implementation, asset import, code changes, visual-complete claims, or QA/HCI handoff.

Gameplay must review:

- `Design/AgentReports/2026-05-14_designer_m01-step-by-step-gameplay-spec.md`
- `Design/AgentReports/2026-05-14_designer_m01-art-sample-alignment-review.md`
- `Design/AgentReports/2026-05-14_art-atlas_m01-step-by-step-gameplay-mockups.md`
- `Design/AgentReports/2026-05-14_pm_designer-art-sample-review-routing.md`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-02_SquadSelected_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-01_TacticalStart_layers.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-02_SquadSelected_layers.json`
- existing relevant `Assets/Game/Art/`, `Assets/Game/Prefabs/`, and runtime/UI ownership files needed only for asset/path mapping.

## Expected Output

`Design/AgentReports/2026-05-14_gameplay_m01-mockup-asset-implementation-audit.md`

## Required Findings

The audit must list exact asset-preparation requirements for pixel-perfect implementation:

- character sprite sheets, poses, facing, pivots, feet anchors, formation spacing, and contact shadows
- battlefield/camera plate, orthographic zoom lock, minimap mapping, world anchors, and map tile/source split
- selection, enemy affiliation/health, move, attack, objective, and invalid-command markers
- UI chrome, panels, icons, text separation, command buttons, objective/log/resource/squad/minimap surfaces, disabled Build state
- slicing requirements, 9-slice rules, transparent corners, z-order, anchors, dynamic/static status, and import settings
- existing source assets that can be reused and missing Art assets that must be produced
- exact feedback Art/Atlas must address before Gameplay can later implement `M01-01_TacticalStart`

## Routing

Current owner:
Gameplay

Next owner after audit:
PM combines Gameplay audit feedback with Designer feedback and sends both back to Art/Atlas.

Held lanes:
Runtime Gameplay implementation and QA/HCI remain blocked until Designer/PM/user approve the corrected sample.
