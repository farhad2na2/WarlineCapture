# POP-05 Mission Result Layered Regeneration Pack

Status: `ReadyForReview_ImplementationReadyProduction_POP05`

This package revises POP-05 around M01 First Contact canonical result content. The target-lock reference bitmap and layer contact sheet in this pass are imagegen-sourced and replace the rejected deterministic pass from `Design/AgentReports/2026-05-16_art-atlas_aaa-readiness-visual-lock-revisions.md`.

Implementation-readiness audit: background art, mission identity/modal chrome, reward cards, buttons, stars, consequence row, and icons are already imagegen-derived final production slices.

## Required Canonical Content

- MissionId: `saga.ch01.m01.first_contact`
- ScenarioSetupId: `scenario.ch01.m01.first_contact`
- LevelId: `level.ch01.district_edge_01`
- IsoMapId: `iso.ch01.district_edge_01`
- Objective checklist: `Destroy hostile patrol` complete
- Reward grants: `CommanderXP`, `Credits`, `Materials`, `Intel`
- Civilian/district consequence row: visible neutral tutorial outcome, zero deltas
- Replay and Continue button states visible

Stale mission and reward terms from the prior target are not part of this revised target. TMP text remains live in implementation.

## Layer Pack

- Reference target: `reference/POP-05_MissionResult_Landscape_Target.png`
- Imagegen selected reference copy: `generated_one_go/source/imagegen_selected_reference.png`
- Imagegen selected contact sheet copy: `generated_one_go/source/imagegen_layers_contact_sheet_source.png`
- Separated layers: `layers/*.png`
- Layer contact sheet: `generated_one_go/layers_contact_sheet.png`
- Manifest: `layer_manifest.json`

## Unity Staging

```bash
python3 Design/VisualLockLayered/POP-05_MissionResult/copy_layers_to_unity.py
```

Default helper mode is dry-run. Do not import into `Assets/` until PM/user approval.
