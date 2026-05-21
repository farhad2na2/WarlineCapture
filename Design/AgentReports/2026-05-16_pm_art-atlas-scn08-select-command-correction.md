# PM Art/Atlas SCN-08 Select Command Correction

Date: 2026-05-16
Owner: PM
Status: correction routed
Priority: P0

## Reviewed Handoff

- `Design/AgentReports/2026-05-16_art-atlas_scn08-rtsbattlehud-complete-implementation-slices.md`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/layers_contact_sheet.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-02_SquadSelected_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-01_TacticalStart_layers.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-02_SquadSelected_layers.json`

## Decision

Do not regenerate the whole SCN-08 HUD.

This is a targeted command-slice correction. The M01 command rail must use:

- `SELECT`
- `MOVE`
- `ATTACK`
- `STOP`
- `HOLD`

The current SCN-08 Art handoff includes `SPECIAL`, but M01 step-by-step mockups explicitly say no `SPECIAL` command in this sample.

## Required Art Correction

Art/Atlas must add:

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/command_select_icon.png`

Art/Atlas must update:

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/README.md`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/layers_contact_sheet.png` or a focused command correction contact sheet

`SPECIAL` may remain only for generic/non-M01 SCN-08 use if needed, but it must be marked not used for M01.

## Imagegen Rule

The `SELECT` visual must be imagegen-sourced. A full HUD regeneration is not required.

Acceptable approaches:

- generate a small imagegen command-icon/button correction sheet, then crop/extract the selected `SELECT` slice
- crop/extract from an already approved imagegen M01 command target if quality and style match the current SCN-08 command button set

Do not draw the select icon deterministically with vector/HTML/CSS/scripted shapes or pixel patching.

## Required Report

Art/Atlas must write:

- `Design/AgentReports/2026-05-16_art-atlas_scn08-select-command-correction.md`

## Routing

Current owner:
Art/Atlas

Held:
UI SCN-08 v5 integration, POP-05/SCN-02 implementation, Gameplay, QA/HCI, Support/FTUE, Designer, and non-routed Art packages.
