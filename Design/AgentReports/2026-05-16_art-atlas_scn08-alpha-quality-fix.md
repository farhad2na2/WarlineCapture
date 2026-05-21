# Art/Atlas SCN-08 Alpha Quality Fix

Date: 2026-05-16
Owner: Art/Atlas
Status: ready for PM/user review
Priority: P0

## Lane

Art/Atlas

## Task

Alpha-quality correction for SCN-08/M01 Match HUD after UI v5 rejection.

Scope was limited to:

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/`
- this handoff report under `Design/AgentReports/`

No runtime code, Unity prefabs, `Assets/` imports, source docs, or other lane task files were modified.

## Handoff Assessment

- `Design/AgentReports/2026-05-16_pm_ui-scn08-v5-rejected-alpha-quality.md`: accepted as current P0 Art/Atlas routing.
- `Design/AgentReports/2026-05-16_ui_scn08-v6-blocked-art-alpha-quality.md`: accepted; UI is blocked until this Art/Atlas handoff exists and is accepted.
- `Design/AgentReports/2026-05-16_pm_art-atlas-scn08-select-accepted-ui-v5.md`: accepted; M01 command order and Select correction remain approved.

## Correction

Removed green chroma-key contamination and edge spill from SCN-08 imagegen-derived layer slices while preserving the accepted M01 command family:

- `SELECT`
- `MOVE`
- `ATTACK`
- `STOP`
- `HOLD`

`SPECIAL` remains excluded from M01 and is still marked generic/non-M01 only.

## Corrected Slice Groups

- objective panel frame/chrome and objective icon slices
- threat feed frame/chrome, warning icon, row backgrounds, and enemy spotted icon
- squad tray/card backgrounds, badge, rank, and squad portrait slices
- command rail frame/fill, command button states, and command icons including `command_select_icon.png`
- minimap frame/fill/content, viewport rectangle, and plus/minus zoom buttons
- supporting top/resource chrome and clock/resource icons that shared the same alpha/chroma extraction risk

## Imagegen And Alpha-Cleanup Provenance

The visual source remains the accepted imagegen SCN-08 package:

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/source/imagegen_scn08_squad_badge_objective_threat_atlas_alpha.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/source/imagegen_scn08_minimap_command_chrome_atlas_alpha.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/source/imagegen_scn08_m01_command_select_correction_alpha.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/source/imagegen_scn08_complete_slices_contact_sheet.png`

No deterministic replacement art was created. Deterministic tooling was used only after imagegen selection for alpha cleanup/despill, extraction cleanup, metadata updates, inspection, and validation. The focused alpha QA sheet is assembled from cleaned imagegen-derived slices and is evidence for the cleanup, not a new target art source.

## Files Changed

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/README.md`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/alpha_quality_fix_contact_sheet.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/layers_contact_sheet.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/source/generated_layer_atlas_alpha.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/source/generated_layer_atlas_chromakey.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/source/imagegen_scn08_squad_badge_objective_threat_atlas_alpha.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/source/imagegen_scn08_minimap_command_chrome_atlas_alpha.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/source/imagegen_scn08_m01_command_select_correction_alpha.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/*.png` corrected where green chroma-key edge spill was present or at risk.

Previously delivered imagegen source sheets and new SCN-08 layer files from the accepted package remain part of this same package handoff.

## Manifest Updates

- Manifest status changed to `ReadyForReview_AlphaQualityFix_SCN08`.
- Added `source.alphaQualityFixContactSheet`.
- Added alpha-quality fix provenance under `sourceGeneration`.
- Replaced stale missing-slice open items with current UI reimport notes.
- Preserved `m01CommandRule` as `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`.
- Preserved `SPECIAL` as generic/non-M01 only.

## Review Evidence

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/alpha_quality_fix_contact_sheet.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/layers_contact_sheet.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/command_select_correction_contact_sheet.png`

I confirm no green chroma-key artifacts remain in the review/contact evidence listed above.

## Validation Run

- Read `Design/AgentTasks/art-atlas_heartbeat.md`.
- Read `Design/AgentTasks/art-atlas_current.md`.
- Read PM v5 rejection and UI v6 blocker reports.
- Inspected the v5 runtime/review capture evidence and identified green edge spill around Art/Atlas-provided SCN-08 slices.
- Applied deterministic alpha cleanup/despill only to accepted imagegen-derived SCN-08 source/package assets.
- Parsed `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json` with `python3 -m json.tool`: passed.
- Scanned `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layers/*.png` for opaque green chroma-key pixels: `PURE_GREEN_REMAINING 0`.
- Scanned `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/*.png` review sheets for opaque green chroma-key pixels: `PURE_GREEN_REMAINING 0`.
- Ran dry-run copy helper and confirmed corrected key slices still map to the expected Unity destinations.

## Validation Result

Ready for PM/user review.

- Green chroma-key edge spill removed from package slices: yes
- Green chroma-key artifacts absent from review/contact evidence: yes
- M01 command order preserved: yes
- `command_select_icon.png` preserved: yes
- `SPECIAL` excluded from M01: yes
- Replacement visual slices generated deterministically: no
- Runtime code changed: no
- Unity prefabs changed: no
- `Assets/` imports changed: no
- Other packages changed: no

## Known Gaps

UI must reimport the corrected SCN-08 package assets and produce `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation-v6.md` after PM/user accepts this Art/Atlas handoff.
