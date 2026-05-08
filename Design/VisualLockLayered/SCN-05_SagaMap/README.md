# SCN-05 Saga Map Layered Regeneration Pack

Status: `RouteReadyLayerPackGenerated`

This package follows the `SCN-08_RTSBattleHUD` layered workflow. The current reference image is a high-quality style baseline only; it has been regenerated as a Chapter 1 route-ready layered Canvas pack.

## Alignment Requirements

- Player-facing chapter title: `First Response`.
- Chapter index: `Chapter 01`.
- Five mission nodes: `1-1 First Contact`, `1-2 Establish The Base`, `1-3 Radar Warning`, `1-4 Airlift`, `1-5 Breach Assault`.
- Star progress uses Chapter 1 totals: 15 possible mission stars.
- Chapter reward progress uses Chapter 1 reward thresholds.
- TMP text remains live text. Node frames, route lines, markers, reward button, dropdowns, icons, and map art must be separate layers.

## Generation Prompt

Use `prompts/high_end_target_and_layers.md`.

## Unity Staging

```bash
python3 Design/VisualLockLayered/SCN-05_SagaMap/copy_layers_to_unity.py
```
