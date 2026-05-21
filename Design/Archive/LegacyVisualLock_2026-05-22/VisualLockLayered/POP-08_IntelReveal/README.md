# POP-08 Intel Reveal Layered Pack

This pack converts the canonical POP-08 Intel Reveal target into reusable Unity Canvas layers.

- Reference target: `reference/POP-08_IntelReveal_Landscape_Target.png`
- Contact sheet: `generated_one_go/layers_contact_sheet.png`
- Manifest: `layer_manifest.json`

Layering rules:

- Use TMP for all UI labels and button text.
- Keep modal chrome, fills, evidence card frames, confidence chips, inspect buttons, CTA buttons, icons, and evidence thumbnails as separate sprites.
- Preserve transparent outside corners on frame sprites for 9-slice import.
- Treat evidence thumbnails as swappable content art, not as baked UI controls.
