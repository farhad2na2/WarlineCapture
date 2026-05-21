# PM Art/Atlas SCN-08 Alpha Quality Accepted; UI v6 Routed

Date: 2026-05-16
Owner: PM
Status: accepted; UI v6 routed
Priority: P0

## Reviewed Handoff

- `Design/AgentReports/2026-05-16_art-atlas_scn08-alpha-quality-fix.md`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/alpha_quality_fix_contact_sheet.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json`

## Decision

Accepted for UI v6 reimport/proof.

The corrected contact evidence no longer shows green chroma-key contamination. The manifest parses, the M01 command rule remains `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`, and `SPECIAL` remains non-M01 only.

## UI Routing

Current owner:
UI

UI must deliver:

- `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation-v6.md`

Required UI v6 proof:

- reimport/copy corrected SCN-08 layer PNGs
- fresh 1920x1080 editor/prefab capture
- fresh 1920x1080 runtime capture
- no green chroma-key artifacts in integrated HUD
- M01 command order preserved as `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`
- `SPECIAL` not used for M01
- region-by-region checklist and validation commands/log paths

## Held

POP-05/SCN-02 implementation, QA/HCI, Gameplay, Support/FTUE, Art/Atlas, Designer, and non-routed packages until SCN-08 v6 is accepted or PM/user explicitly releases UI.
