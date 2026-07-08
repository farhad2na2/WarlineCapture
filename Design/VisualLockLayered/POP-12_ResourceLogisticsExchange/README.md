# POP-12 Resource Logistics Exchange Visual Lock

Status: target-lock reference saved, separated layer pack V01 ready, Canvas implementation pending.

This pack defines the accepted target reference and V01 separated layers for the in-match Resource Exchange popup. The reference PNG is saved under `reference/`; Unity Canvas implementation should use the generated layer sprites and live TMP/UI binding, not the target PNG.

Current target-lock request:

`prompts/POP-12_ResourceLogisticsExchange_NewMainMenuArtDirection_TargetLock_V01.md`

Accepted saved reference:

`reference/POP-12_ResourceLogisticsExchange_NewMainMenuArtDirection_TargetLock_V01.png`

Separated layer pack:

- `layer_manifest.json`
- `generated_one_go/source/POP-12_ResourceExchange_Panels_Green_v01.png`
- `generated_one_go/source/POP-12_ResourceExchange_Icons_Green_v01.png`
- `generated_one_go/source/POP-12_ResourceExchange_Content_Green_v01.png`
- `layers/`
- `generated_one_go/layers_contact_sheet.png`
- `validation/pop12_layer_validation.json`

## Design Source

- `../../Resource_Logistics_Exchange_Design.md`
- `../../Architecture/resource_logistics_exchange_implementation_tracker.md`
- `../SCN-09_BuildDrawer/reference/SCN-09_BuildDrawer_NewMainMenuArtDirection_TargetLock_V03.png`
- `../SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_NewMainMenuArtDirection_TargetLock_V02.png`
- `../../UI_Screen_Reference_To_Icons_Panels_GreenKey_Workflow.md`
- `../../UIUX_Target_To_Canvas_Workflow_Guide.md`

## Acceptance Gate

- The popup must read as a Build Popup sibling: dark brushed-metal panels, gold selected tabs, clear queue panel, separate icon/progress layers, and dense tactical readability.
- The reference must show the popup over a dimmed match HUD/world background. The modal is the focus; background detail must not fight the popup.
- The target must include Export and Import tabs, recipe cards, selected details, amount stepper, queue panel, Rush All, Clear Completed, and Close.
- The reference must show at least one disabled/locked or warning state without baking lock/warning/progress art into card backgrounds.
- The generated PNG exists in `reference/` with the exact filename above.
- The V01 layer pack contains separate chrome/panel frames, card frames, tabs, action buttons, progress elements, icons, badges, and route thumbnails.
- Route thumbnails are content images only; card borders, lock/check/warning badges, labels, values, and progress UI must be overlaid as separate Unity elements.
- Layer validation reports 55 sprites with 0 pure key-green pixels, 0 border key-green pixels, and 0 frame green-spill pixels.
- The locked `IMPORT OIL` card is a disabled/gated visual state only. Runtime implementation must keep Credits -> Oil disabled by default unless a mission explicitly enables it.
- Do not build a Unity Canvas from the target PNG alone.

## Next Steps

1. Build the Canvas popup from the V01 layer pack, reusable TMP text, buttons, and read-model bindings.
2. Validate 16:9 and 20:9 captures against the target reference and contact sheet before marking visual lock complete.
