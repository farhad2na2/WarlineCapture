# PM Routing: Gameplay Approval Gate And M01-01 First Implementation

Date: 2026-05-14
Lane: PM
Status: routed to Gameplay

## Context

Designer approved the latest imagegen-only M01 two-frame sample for PM/user visual approval:

- `Design/AgentReports/2026-05-14_designer_m01-imagegen-sample-alignment-review.md`

Art/Atlas delivered:

- `Design/AgentReports/2026-05-14_art-atlas_m01-step-by-step-gameplay-mockups.md`

User request:

- Check whether Gameplay approves it.
- If Gameplay approves it, continue with implementation.

## Decision

Gameplay is now the active next lane.

Gameplay must first approve implementation readiness for the exact imagegen sample and LayerPack. If and only if Gameplay approves, it may implement the first runtime slice.

## Gameplay Approval Gate

Gameplay must read:

- `Design/AgentReports/2026-05-14_designer_m01-imagegen-sample-alignment-review.md`
- `Design/AgentReports/2026-05-14_art-atlas_m01-step-by-step-gameplay-mockups.md`
- `Design/AgentReports/2026-05-14_gameplay_m01-corrected-sample-asset-audit.md`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-02_SquadSelected_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/AssetPrep_M01_Sample.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-01_TacticalStart_layers.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-02_SquadSelected_layers.json`

Gameplay must decide:

- approved for `M01-01_TacticalStart` implementation
- blocked and why

## Implementation Scope If Approved

Implement only:

- `M01-01_TacticalStart`

Runtime target:

- tactical start/no selection
- shared camera/framing direction from approved imagegen sample
- objective panel and Star Goals baseline using native runtime text
- bottom HUD baseline state
- command buttons neutral/inactive
- Build disabled/secondary with canonical reason `MissionDoesNotAllowBuild`
- minimap baseline viewport if supported by existing systems
- enemy affiliation/readability baseline only if existing systems support it cleanly

Do not implement:

- `M01-02_SquadSelected`
- selected squad rings/status behavior
- M01-03 through M01-11
- QA/HCI validation
- runtime import of flattened PNG mockups as source
- unrelated mission/HUD refactors

## Required Gameplay Output

If approved and implemented:

- `Design/AgentReports/2026-05-14_gameplay_m01-imagegen-sample-implementation.md`

If blocked:

- `Design/AgentReports/2026-05-14_gameplay_m01-imagegen-sample-implementation-blocked.md`

## Updated Task Files

- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/gameplay_pm_message.md`
- `Design/AgentTasks/designer_current.md`
- `Design/AgentTasks/designer_pm_message.md`

## Held Lanes

QA/HCI remains held until a runtime implementation exists and PM routes validation.

Art/Atlas remains held unless Gameplay blocks on Art assets or PM/user rejects the sample.
