# POP-12 Resource Logistics Exchange Visual Lock

Status: target-lock reference saved, layer pack pending.

This pack defines the accepted target reference for the in-match Resource Exchange popup. The reference PNG is saved under `reference/`; implementation remains blocked until the separated layer pack exists.

Current target-lock request:

`prompts/POP-12_ResourceLogisticsExchange_NewMainMenuArtDirection_TargetLock_V01.md`

Accepted saved reference:

`reference/POP-12_ResourceLogisticsExchange_NewMainMenuArtDirection_TargetLock_V01.png`

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
- The locked `IMPORT OIL` card is a disabled/gated visual state only. Runtime implementation must keep Credits -> Oil disabled by default unless a mission explicitly enables it.
- Do not build a Unity Canvas from the target PNG alone; create the separated layer pack first.

## Next Steps

1. Generate separated green-key layer requests for popup chrome, tabs, cards, icons, progress bars, buttons, amount stepper, and queue rows.
2. Create `layer_manifest.json`, a contact sheet, and implementation notes before Canvas prefab work starts.
