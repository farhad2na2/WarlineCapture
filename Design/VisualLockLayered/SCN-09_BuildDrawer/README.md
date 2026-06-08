# SCN-09 Build Drawer Visual Lock

Status: Target-lock mockup and V01 implementation layer pack generated.
Date: 2026-05-22

## Active Target

- Reference target: `reference/SCN-09_BuildDrawer_OnExistingMatchHUD_TargetLock_V01.png`
- Canonical layout context: `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png`
- Canonical size: `2400 x 1080`

This target is the Build drawer opened on top of the active match HUD. It keeps the same SCN-08 match layout and overlays a left-side production drawer for adding Buildings, Vehicles, and Soldiers during a 3D single-map match.

## Layer Pack

Active implementation pack:

- Manifest: `layer_manifest.json`
- Layers: `layers/`
- Green-source sheets: `generated_one_go/source/`
- Contact sheet: `validation/SCN-09_BuildDrawer_layers_contact_sheet.png`

The V01 pack contains separate chrome, building thumbnails, and icons. Parent panels are clean and do not bake live labels, costs, timers, lock states, progress bars, selected states, or warning text.

## Layer Rules Applied

- Do not crop or cut the target-lock mockup into implementation assets.
- Generate clean independent source assets for the layer pack.
- Keep all item names, costs, resource values, cooldowns, locked reasons, placement warnings, and tab labels live in Unity.
- Keep thumbnails, card frames, selected highlights, buttons, status chips, resource icons, and placement icons separate.
- Use `#00ff00` green-source sheets only for extraction assets, not for the target-lock mockup.
- Build drawer must sit on the existing SCN-08 match HUD, not a different battlefield layout.

## Design Source

- `Design/Match_HUD_And_Gameplay_Implementation_Spec.md`
- `Design/Field_Logistics_Oil_Fuel_Design.md`
- `Design/3D_SingleMap_Gameplay_Direction.md`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/README.md`
- Building and unit config source: `Assets/Game/Configs/Prefabs`

## Target Prompt Summary

The target asks for a AAA mobile RTS Build drawer with:

- three tabs: Buildings, Vehicles, and Soldiers
- config-backed production cards
- separate thumbnails for building visuals
- separate Credits, Supplies, Oil, Fuel, Time, Lock, Warning, Add, Queue, Confirm, and placement icons
- building placement flow that requires a valid 3D footprint
- vehicle and soldier production flow that spawns at valid game-decided production/rally points
- clear disabled states and placement warnings without baking text into backgrounds

No 2.5D/isometric strategy-map layout, alternate match HUD, baked labels, baked progress bars, or old visual-lock UI language should appear in this active target.
