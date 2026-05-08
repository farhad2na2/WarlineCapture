# POP-05 Mission Result Layered Regeneration Pack

Status: `ReadyForCanvasImplementation`

This package follows the `SCN-08_RTSBattleHUD` layered workflow. The layer pack now contains separated, Unity-consumable sprites derived from the accepted `POP-05_MissionResult` target plus canonical live-text content rules.

## Alignment Requirements

- Current visual-lock target example: `Downtown Breakthrough`.
- Include victory/defeat state support, stars, mission metadata, stats, objectives, canonical rewards, and civilian/district consequence row.
- Match the accepted target content until this target is regenerated: `Difficulty Hard`, `Supply Crate`, and `Unlock Fragments`.
- Keep the `ConsequenceRow` prefab hook for runtime Saga/Operation data, but leave it visually inactive for the current target because the accepted target does not show that row.
- TMP text remains live. Modal frame, inner fill, section panel frames, stat cards, reward cards, icons, stars, consequence row, objective rows, and buttons must be separate layers.

## Layer Pack

- Reference target: `reference/POP-05_MissionResult_Landscape_Target.png`
- Separated layers: `layers/*.png`
- Layer contact sheet: `generated_one_go/layers_contact_sheet.png`
- Manifest: `layer_manifest.json`

## Generation Prompt

Use `prompts/high_end_target_and_layers.md`.

## Unity Staging

```bash
python3 Design/VisualLockLayered/POP-05_MissionResult/copy_layers_to_unity.py
```
