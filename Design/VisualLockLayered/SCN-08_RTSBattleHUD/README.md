# SCN-08 RTS Battle HUD Visual Lock

Status: Target-lock mockup and V01 implementation layer pack generated.
Date: 2026-05-22

## Active Target

- Reference target: `reference/SCN-08_RTSBattleHUD_Landscape_Target.png`
- Candidate source: `reference/SCN-08_RTSBattleHUD_TargetLock_V01.png`
- 16:9 reference target: `reference/SCN-08_RTSBattleHUD_16x9_Landscape_Target.png`
- 16:9 candidate source: `reference/SCN-08_RTSBattleHUD_16x9_TargetLock_V01.png`
- Canonical size: `2400 x 1080`

This target is the active 3D match screen for WarlineCapture. It shows the player commanding squads on one large 3D Middle East town map, with objective tracking, squad selection, command actions, minimap awareness, threat feed, and civilian-risk feedback.

The 16:9 reference is a layout target only. It keeps the same gameplay/state content as the 20:9 target but compresses horizontal spacing so the HUD remains readable without clipping the left panels, bottom commands, right controls, or minimap.

## Layer Pack

Active implementation pack:

- Manifest: `layer_manifest.json`
- Layers: `layers/`
- Source sheets: `generated_v01/source/`
- Contact sheet: `validation/SCN-08_RTSBattleHUD_layers_contact_sheet.png`
- Generated V01 manifest: `generated_v01/layer_manifest.json`

The generated pack contains separate source groups for battlefield art, HUD frames, icons, world markers, minimap content, and squad portraits. Parent frames are intentionally clean and do not bake text, icons, progress bars, lock states, or child controls.

## Layer Rules Applied

- Do not crop or cut the target-lock mockup into implementation assets.
- Generate clean independent source assets for the layer pack.
- Keep text live in Unity unless explicitly approved as decorative.
- Keep selected states, command states, minimap markers, health/status bars, portraits, and world markers as separate assets.
- Use `#00ff00` green-source sheets only for extraction assets, not for the target-lock mockup.
- Preserve a clear central 3D battlefield area; HUD chrome must frame gameplay instead of covering it.

## Design Source

- `Design/3D_SingleMap_Gameplay_Direction.md`
- `Design/Match_HUD_And_Gameplay_Implementation_Spec.md`
- `Design/Match_Selection_Implementation_Spec.md`
- `Design/UIUX_Gameplay_Element_Alignment.md`
- `Design/Skirmish_Mode_Implementation_Spec.md`
- `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png`
- Gameplay scene references: `Assets/Game/Scenes/Demo.unity` and `Assets/Game/Scenes/Demo2.unity`
- Unit/building naming source: `Assets/Game/Configs/Prefabs`

## Target Prompt Summary

The target asks for a AAA mobile RTS match HUD with:

- active 3D battlefield background with no isometric or two-map split
- top resource/status strip with Credits, Fuel, Supply, Civilian Risk, pause, and settings
- objective panel for capture/clear/protect goals
- selected squad panel with role, health, ability chips, and rule feedback
- bottom squad tray using four quick-select roster cards. Current implementation ids are `Squad_Rifle`, `Squad_APC`, `Squad_Tank`, and `Squad_Helicopter`; these are active controllable group slots, not command buttons. M01 enables only Rifle Squad and keeps the other visible cards disabled/neutral if the layout shows them.
- bottom command bar for Select, Move, Attack, Hold, Stop, Build, Scan, and Support
- minimap, threat feed, jump action, world selection rings, path line, objective marker, hostile marker, and invalid-command feedback

No 2.5D isometric map, strategy/tactical dual-map split, decorative baked text, or old teal/cyan visual language should appear in this active target.
