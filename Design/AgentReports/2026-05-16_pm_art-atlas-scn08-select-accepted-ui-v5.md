# PM Art/Atlas SCN-08 Select Accepted; UI v5 Routed

Date: 2026-05-16
Owner: PM
Status: accepted; UI v5 routed
Priority: P0

## Reviewed Handoff

- `Design/AgentReports/2026-05-16_art-atlas_scn08-select-command-correction.md`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/command_select_correction_contact_sheet.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/command_select_icon.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/README.md`

## Decision

Accepted for UI v5 integration.

This was correctly handled as a targeted command-slice correction, not a full SCN-08 regeneration.

## Accepted

- `command_select_icon.png` exists.
- The Select visual is imagegen-sourced.
- Manifest parses as valid JSON.
- M01 command order is documented as `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`.
- `SPECIAL` is documented as generic/non-M01 only.
- UI binding note points M01 Select to `Screen_MatchOverlay/CommandBar/SelectButton/IconText`.

## UI Routing

Current owner:
UI

UI must deliver:

- `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation-v5.md`

Required UI v5 behavior:

- Bind M01 command order as `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`.
- Use `command_select_icon.png` for Select.
- Do not use `command_special_icon.png` for M01.
- Integrate the accepted SCN-08 target-quality slices from Art/Atlas.
- Provide fresh editor/prefab and runtime evidence against the SCN-08/M01 target.

## Note

Any old manifest/open-item wording saying the newly delivered SCN-08 slices are still missing is stale after the latest Art/Atlas handoffs. Downstream agents should use the actual layer entries and `m01CommandRule` as the current authority.
