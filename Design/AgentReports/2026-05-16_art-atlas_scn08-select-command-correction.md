# Art/Atlas SCN-08 Select Command Correction

Date: 2026-05-16
Owner: Art/Atlas
Status: ready for PM/user review
Priority: P0

## Lane

Art/Atlas

## Task

Targeted command-slice correction for SCN-08/M01. This is not a full HUD regeneration.

Scope was limited to:

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/`

## Handoff Assessment

- `Design/AgentReports/2026-05-16_pm_art-atlas-scn08-select-command-correction.md`: accepted as P0 routing.
- `Design/AgentReports/2026-05-16_art-atlas_scn08-rtsbattlehud-complete-implementation-slices.md`: accepted as the previous Art/Atlas slice handoff requiring targeted correction.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`: accepted as command-order authority.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-01_TacticalStart_layers.json`: accepted as M01-01 command family authority.
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-02_SquadSelected_layers.json`: accepted as M01-02 command family authority.

## Correction

Added M01 `SELECT` command slice:

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/command_select_icon.png`

Updated command metadata so M01 order is:

- `SELECT`
- `MOVE`
- `ATTACK`
- `STOP`
- `HOLD`

`SPECIAL` remains in the package only for generic/non-M01 SCN-08 use. It is explicitly marked as not used for M01 in the manifest and README.

## Imagegen Provenance

The `SELECT` visual is imagegen-sourced from a focused command correction sheet.

Selected generated source root:

- `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618`

Selected imagegen file:

- `ig_066f017118725f96016a08262b138481918cc0e22f85a86082.png`

Project copies:

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/source/imagegen_scn08_m01_command_select_correction_chromakey.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/source/imagegen_scn08_m01_command_select_correction_alpha.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/command_select_correction_contact_sheet.png`

Deterministic tooling was used only after imagegen selection to remove chroma-key background, crop the selected `SELECT` icon, inspect dimensions, update metadata, and validate files.

## Files Changed

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/README.md`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/command_select_icon.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/source/imagegen_scn08_m01_command_select_correction_chromakey.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/source/imagegen_scn08_m01_command_select_correction_alpha.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/command_select_correction_contact_sheet.png`

## Manifest Updates

- Manifest status changed to `ReadyForReview_ImagegenM01SelectCommandCorrection_SCN08`.
- Added source entries for the M01 command select correction sheet.
- Added `command_select_icon` layer entry.
- Added binding note for `Screen_MatchOverlay/CommandBar/SelectButton/IconText`.
- Added `m01CommandRule` with command order `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`.
- Marked `SPECIAL` as generic/non-M01 only.
- Added `SelectButton` background binding note to the command button background metadata.

## Validation Run

- Read current Art/Atlas task and heartbeat instructions.
- Read PM correction routing report.
- Read M01 layer pack manifest and M01-01/M01-02 command family rules.
- Generated focused imagegen command correction sheet.
- Copied selected imagegen sheet into the SCN-08 package.
- Extracted `command_select_icon.png` from imagegen source.
- Updated SCN-08 manifest and README.
- Parsed `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json` with `python3 -m json.tool`: passed.
- Verified dimensions with `sips`:
  - `command_select_icon.png`: `375x410`
  - correction alpha sheet: `2172x724`
  - correction contact sheet: `2172x724`
- Ran dry-run copy helper and confirmed `command_select_icon.png` maps to:
  - `Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/command_select_icon.png`

## Validation Result

Ready for PM/user review.

- Full SCN-08 HUD regenerated: no
- Runtime code changed: no
- Unity prefabs changed: no
- `Assets/` imports changed: no
- Non-routed packages changed: no
- `SELECT` slice imagegen-sourced: yes
- `SPECIAL` kept only for generic/non-M01 SCN-08 use: yes
- M01 command order documented: yes

## Known Gaps

- UI SCN-08 v5 integration remains held until PM/user accepts this Art/Atlas correction.
- Runtime binding of `SelectButton` is UI-owned and was not modified by this Art/Atlas pass.
