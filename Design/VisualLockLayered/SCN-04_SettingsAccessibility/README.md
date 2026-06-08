# SCN-04 Settings / Accessibility Visual Lock

Status: Target-lock mockup and V01 implementation layer pack generated.
Date: 2026-05-22

## Active Target

- Reference target: `reference/SCN-04_SettingsAccessibility_Landscape_Target.png`
- Candidate source: `reference/SCN-04_SettingsAccessibility_TargetLock_V02.png`
- Canonical size: `2400 x 1080`

This target is the active Settings / Accessibility screen for the 3D command-base direction. The top header uses the same shared V22 shell as Main Menu and Skirmish: Warline logo panel, Credits/Supplies/Command counters, and right inbox/settings dock. The Settings title/back strip sits below that shared shell.

## Layer Pack

Active implementation pack:

- Manifest: `layer_manifest.json`
- Layers: `layers/`
- Source sheets: `generated_v01/source/`
- Contact sheet: `validation/SCN-04_SettingsAccessibility_layers_contact_sheet.png`
- Generated V01 manifest: `generated_v01/layer_manifest.json`

The generated pack contains separate source groups for no-UI command-base background art, shared header frames, title/back frames, category tabs, content panels, sliders, toggles, dropdowns, segmented controls, buttons, icons, and status chips. Text and numeric values should be live in Unity.

## Layer Rules Applied

- Do not crop or cut the target-lock mockup into implementation assets.
- Generate clean independent source assets for the layer pack.
- Keep the shared Main Menu / Skirmish top header structure; do not use a custom Settings-only full-width header.
- Parent frames/backgrounds must not bake child icons, values, labels, slider fills, toggle states, or button text.
- Keep Skirmish AI/economy/gameplay-speed controls out of SCN-04. Those belong to `SCN-13_SkirmishSetup`.
- Use `#00ff00` green-source sheets only for extraction assets, not for the target-lock mockup.

## Design Source

- `Design/UIUX_Gameplay_Element_Alignment.md`
- `Design/UIUX_MainMenu_Visual_Contract.md`
- `Design/UIUX_Runtime_Optimization_Plan.md`
- `Design/Audio_Design_Guidelines.md`
- `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-13_SkirmishSetup/reference/SCN-13_SkirmishSetup_Landscape_Target.png`

## Target Prompt Summary

The target asks for a AAA mobile RTS Settings / Accessibility screen with:

- shared Warline Capture header matching Main Menu and Skirmish
- separate Settings title/back panel below the shared header
- category tabs for Audio, Graphics, Controls, Notifications, Accessibility, and Language
- Accessibility selected state
- sliders for Master Volume, Music, SFX/UI, and Voice
- Graphics Quality dropdown and Target FPS segmented control
- accessibility controls for Large Text, High Contrast, Colorblind Mode, and Reduced Motion
- Language dropdown set to English
- Readability Preview panel
- Reset Defaults, Back, and Apply Changes footer actions

No Skirmish AI settings, 2.5D/isometric language, old teal/cyan style, or one-off Settings-only top header should appear in this active target.
