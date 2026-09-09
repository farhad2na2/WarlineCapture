# Campaign comic style alignment

User direction, 2026-09-09: prefer the newer M3 comic style and bring earlier comics into that style. M3 remains the reference; this supersedes the initial proposal to redraw M3 to match the older artwork.

## Observed drift

The earlier FirstLaunch/M1 and M2 scenes use coarse, matte polygon surfaces, small simplified eyes, strong planar facial features and muted khaki/sandstone lighting. M3 uses finer facial modelling, illustrated eyes/lips, more detailed materials and stronger warm lighting. Matching clothing colours alone did not preserve character identity.

Samira's M3-D03 face is the new reference: warm tan skin, dark almond eyes/strong brows, defined nose/lips/jaw, mustard scarf, charcoal jacket and muted teal clothing. Dalia uses M3-B02/D01 as her reference (dark tied hair, wraparound sunglasses, headset, tan tactical uniform). ARIA uses the current M3 hologram design. Returning characters must match those references across panels and portraits. Other characters retain their existing identities while receiving the same rendering treatment.

## Production constraints

- Preserve each earlier scene's story beat, cast roles, props, composition and chronology. Do not replace earlier scenes with M3 scenes.
- Use original artwork as the composition reference and actual M3 panels as style/identity references for every generation.
- Keep captions, localized words and UI separate from art. Both language paths share the same image assets.
- Keep all seven current M3 panel sources unchanged.
- Generate review candidates outside Assets first; preserve old source identities/hashes and Unity metadata when replacements are installed.
- Validate aspect framing and visible character consistency, not only successful import or caption bounds.

Generation uses the built-in imagegen tool. Exact prompts, input references and generated paths are retained in generation_manifest.json. A generated candidate is not automatically an installed or accepted replacement.
