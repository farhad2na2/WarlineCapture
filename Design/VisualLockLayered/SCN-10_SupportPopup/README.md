# SCN-10 Support Popup VisualLockLayered

Status: Imagegen V03 target-lock mockup active; V01 implementation layer pack present as extraction placeholder.
Date: 2026-06-08

## Active Target

- Reference target: `reference/SCN-10_SupportPopup_Landscape_Target.png`
- Existing-HUD target: `reference/SCN-10_SupportPopup_OnExistingMatchHUD_TargetLock_V03.png`
- Popup source: `generated_one_go/source/SCN-10_SupportPopup_Imagegen_PopupPanel_V03.png`
- Canonical layout context: `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png`
- Canonical size: `2400 x 1080`

This target is the Support popup opened from the SCN-08 match HUD. It follows the same in-match popup/drawer language as `SCN-09_BuildDrawer`, but it is for off-map and auxiliary support abilities rather than build/production.

The rejected V01 imagegen target changed the underlying HUD, command bar, side buttons, typography, and overall RTS visual language. It must not be used as an implementation reference. V03 keeps the SCN-08 HUD fixed and replaces only the Support popup surface.

## Layer Pack

- Manifest: `layer_manifest.json`
- Layers: `layers/`
- Contact sheet: `generated_one_go/layers_contact_sheet.png`
- Validation contact sheet: `validation/SCN-10_SupportPopup_layers_contact_sheet.png`

Current separated layers are a clean placeholder layer pack for implementation planning. Final production layers should be regenerated/extracted from the accepted imagegen V03 popup source so chrome, icons, thumbnails, highlights, pips, and buttons match the target exactly.

## Runtime Behavior

1. Player taps `SUPPORT` on the match HUD.
2. The Support popup opens and owns UI input. World taps behind it must not leak through.
3. Player selects a support ability such as Drone Scan, Airstrike, Smoke Drop, Supply Drop, Medevac, or Reinforcement.
4. If the ability needs a target, the popup closes or collapses and the HUD enters Support Targeting Mode.
5. Player taps a valid map target. Resources/charges/cooldown are spent only on accepted execution.
6. HUD returns to the previous selected-unit state unless the ability explicitly supports repeat targeting.

## Layer Rules Applied

- Do not cut the target-lock mockup into implementation sprites.
- Do not bake labels, cooldown values, charges, lock reasons, costs, or progress bars into reusable chrome.
- Keep popup frame, detail frame, card frames, selected/disabled overlays, icons, thumbnails, cooldown fills, charge pips, warning chips, and instruction strip as separate sprites.
- Keep support ability data live from mission/equipment/runtime support definitions.
- Keep style aligned with SCN-08 Battle HUD and SCN-09 Build Drawer.

## Design Source

- `Design/Match_HUD_And_Gameplay_Implementation_Spec.md`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/README.md`
- `Design/VisualLockLayered/SCN-09_BuildDrawer/README.md`
