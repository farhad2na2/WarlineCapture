# POP-03_BuildPlacement Layer Pack

This pack is the mandatory VisualLockLayered source for `Build Placement Popup`. It was created while reworking the popup track after the earlier popup implementation skipped the layer-pack gate.

## Gate Rules

- Reference target: `reference/POP-03_BuildPlacement_Landscape_Target.png`
- Layer manifest: `layer_manifest.json`
- Layer contact sheet: `generated_one_go/layers_contact_sheet.png`
- Unity prefab work must consume the separated files in `layers/` first.
- Do not bake text, icons, and frames into one image. Frames/buttons stay separate 9-sliced transparent sprites; icons stay separate transparent sprites.
- If a layer is visually wrong, fix the layer PNG and manifest first, then rebuild the prefab.

## Current Layer Source

This is a corrective baseline layer pack built from the existing separated Unity sprite assets so the workflow is enforceable. Future visual-lock refinements for this popup must update these layer PNGs directly instead of hiding fixes in prefab-only layout code.
