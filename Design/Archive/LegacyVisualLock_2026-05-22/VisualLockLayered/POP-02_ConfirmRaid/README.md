# POP-02 Confirm Raid Layered Regeneration Pack

Status: `ReadyForCanvasImplementation`

This is the mandatory VisualLockLayered source for `ConfirmRaidPopup`. It is built from the accepted landscape target before Unity prefab work.

## Alignment Requirements

- Match `Design/VisualLock/POP-02_ConfirmRaid/POP-02_ConfirmRaid_Landscape_Target.png`.
- Live TMP content: `CONFIRM RAID`, `North Bridge Cell`, Intel Confidence `78%`, Collateral Risk `Medium`, Civilian Density `Elevated`, warning body, `CANCEL`, `CONFIRM RAID`.
- Reusable layers must not contain text. Background, modal chrome, panel frames, icons, target thumbnail, meter rows, and button backgrounds stay separate.
- Button and frame sprites require transparent corners.

## Layer Pack

- Reference target: `reference/POP-02_ConfirmRaid_Landscape_Target.png`
- Separated layers: `layers/*.png`
- Layer contact sheet: `generated_one_go/layers_contact_sheet.png`
- Manifest: `layer_manifest.json`

## Unity Staging

```bash
python3 Design/VisualLockLayered/POP-02_ConfirmRaid/copy_layers_to_unity.py --apply --force
```
