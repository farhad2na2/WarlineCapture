# SCN-13 Skirmish Setup Visual Lock

Status: Target-lock mockup approved for layer generation; V01 implementation layer pack generated.
Date: 2026-05-22

## Active Target Candidate

- Reference target: `reference/SCN-13_SkirmishSetup_Landscape_Target.png`
- Candidate source: `reference/SCN-13_SkirmishSetup_TargetLock_V02.png`
- Canonical size: `2400 x 1080`

This target follows the current Main Menu V22 command-base style: dark graphite/black military panels, olive selected state, gold bevel trims, realistic forward-command tent backdrop, 3D operation-map preview, and a gold `LAUNCH MISSION` CTA.

## Layer Pack

Active implementation pack:

- Manifest: `layer_manifest.json`
- Layers: `layers/`
- Source sheets: `generated_v01/source/`
- Contact sheet: `validation/SCN-13_SkirmishSetup_layers_contact_sheet.png`
- Generated V01 manifest: `generated_v01/layer_manifest.json`

The generated pack contains separate source groups for parent frames, child icons, map markers, operation preview art, and the 21:9 no-UI background.

## Layer Rules Applied

- Do not crop or cut this reference target into implementation assets.
- Generate clean independent source assets for the layer pack.
- Parent frames/backgrounds must not bake in child icons, progress bars, locks, chevrons, text, or stateful controls.
- Operation preview art should be generated as its own wide image.
- Header, logo, resource icons, panels, buttons, locks, preset icons, control icons, and CTA chevrons must be separate assets.
- Use a clean green-background source workflow only for extraction assets, not for the target-lock mockup.

## Design Source

- `Design/WarlineCapture_Skirmish_Mode_Implementation_Spec.md`
- `Design/WarlineCapture_3D_SingleMap_Gameplay_Direction.md`
- `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png`
- `Design/AgentReports/Captures/MainMenuV15C/SCN02_MainMenu_V22OneGo_2400x1080.png`

## Target Prompt Summary

The target asks for a AAA mobile RTS Skirmish Setup screen with:

- top V22-style resource header
- `SKIRMISH / Configure Operation` title
- operation preset rail with selected and locked presets
- large 3D operation preview for `Desert Outpost`
- operation rule controls for enemy, economy, pacing, aggression, win condition, and fog lock reason
- bottom reset/randomize/launch action bar

No `Quick Custom`, `Custom Game Setup`, `Saga`, 2.5D, or teal/cyan legacy styling should appear in this active target.
