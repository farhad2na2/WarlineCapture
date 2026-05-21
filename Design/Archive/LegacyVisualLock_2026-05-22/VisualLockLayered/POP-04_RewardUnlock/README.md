# POP-04 Reward Unlock Layered Pack

Status: `ReadyForCanvasImplementation`

This is the mandatory VisualLockLayered source for `RewardUnlockPopup`. The pack is built before Unity prefab work, per Phase 7 rules.

## Layer-Pack Gate

- Reference target: `reference/POP-04_RewardUnlock_Landscape_Target.png`
- Separated layers: `layers/*.png`
- Contact sheet: `generated_one_go/layers_contact_sheet.png`
- Manifest: `layer_manifest.json`

## Implementation Rules

- Use live TMP text for header, unlock title/subtitle, reward labels, amounts, and Continue.
- Use separate sprites for modal frame, modal fill, close button, display art, reward cards, reward icons, and Continue button.
- Do not place the full target image in the Canvas.
- 9-sliced chrome must keep transparent corners and separate icons/text.
