# Campaign comic style alignment

User direction, 2026-09-09: prefer the newer M3 comic style and bring earlier comics into that style. M3 remains the reference; this supersedes the initial proposal to redraw M3 to match the older artwork.

## Observed drift

The earlier FirstLaunch/M1 and M2 scenes use coarse, matte polygon surfaces, small simplified eyes, strong planar facial features and muted khaki/sandstone lighting. M3 uses finer facial modelling, illustrated eyes/lips, more detailed materials and stronger warm lighting. Matching clothing colours alone did not preserve character identity.

Samira's M3-D03 face is the new reference: warm tan skin, dark almond eyes/strong brows, defined nose/lips/jaw, mustard scarf, charcoal jacket and muted teal clothing. Dalia uses M3-B02/D01 as her reference (dark tied hair, wraparound sunglasses, headset, tan tactical uniform). ARIA uses the M3-B01, M3-C01 and M3-D02 cyan hologram: asymmetric side-swept short hair, angular oval face and jaw, almond eyes, straight nose and restrained lips. Returning characters must match those references across panels and portraits. Other characters retain their existing identities while receiving the same rendering treatment.

## Production constraints

- Preserve each earlier scene's story beat, cast roles, props, composition and chronology. Do not replace earlier scenes with M3 scenes.
- Use original artwork as the composition reference and actual M3 panels as style/identity references for every generation.
- Keep captions, localized words and UI separate from art. Both language paths share the same image assets.
- Preserve the M3 character/rendering direction. Later user-requested Sahrin geography corrections to `M03-B02` and `M03-D03` supersede the earlier byte-identical-source constraint; keep their cast, scene beats and grade intact.
- Generate review candidates outside Assets first; preserve old source identities/hashes and Unity metadata when replacements are installed.
- Validate aspect framing and visible character consistency, not only successful import or caption bounds.
- Check every generated candidate against the specific source panel and the returning-character reference at face scale. Reject a candidate if ARIA, Samira or Dalia changes identity even when the composition and palette improve. In particular, a generic dark-haired hologram is not ARIA.

## Panel canvas

Campaign comic panels use one exact **1672 × 941 pixel** landscape canvas. Run `python3 Design/NarrativeVision/M3StyleAlignment/check_comic_dimensions.py` before accepting new panels. Portraits and dialogue frame pieces are separate UI assets and are excluded from this panel rule. Do not rely on a matching aspect ratio alone: a 1671-pixel width or a wider cinematic render still needs normalization. Preserve every important person, object and evidence mark when reframing a wide source; inspect the complete result at player display size.

## Color and environment continuity (2026-09-27)

Use a restrained warm desert grade across the campaign. The current M3 panels remain the approved reference for refined faces, faceted rendering and natural material detail. For daylight color, compare against `Assets/Game/Art/Narrative/M03RadarWarning/Final/M03-B02.png`; for warm late-day color, compare against `M03-D03.png`. Compare only the grade and rendering treatment, not their geography or story content. A daylight panel should show pale tan sandstone, natural skin, olive/charcoal equipment and blue daylight; warm sunlight may tint lit surfaces without turning the entire frame orange. Sunset panels may be warmer, and night panels may be darker, but faces and story evidence must remain readable. Reserve vivid cyan for ARIA and route displays, red for mission evidence, and mustard for Samira's scarf.

Review candidates side by side at equal display size with a same-lighting reference from another mission. Look for excessive global yellow/orange, crushed black equipment, chalky highlights, desaturated people or stone, and strong color jumps at comic cuts. A histogram is a review aid, not an acceptance rule: current M3 source panels have average HSV saturation of roughly 0.28–0.41; large departures should prompt visual review, especially when the time of day is similar. Preserve each scene's intended lighting rather than applying one fixed filter to all panels.

Sahrin is an arid desert city with bare rose/tan mountains, dry streets and sparse local palms/trees. Do not introduce a lake, permanent river, green mountains, lush valley or rain-wet road unless the mission story explicitly establishes it. Cyan route lines on tactical maps are not waterways. When editing an existing panel, use that panel as the composition and identity source, with an approved M3 panel only as a style/color reference. Preserve cast, facial identity, pose, important props, camera and story evidence while delivering the standard panel canvas. Check the 16:9 panel and the 20:9 crop in the actual comic UI before calling it player-ready.

Reusable generation instruction: “Refined faceted cinematic Sahrin comic in the approved M3 character style; balanced warm desert grade with natural tan skin and pale sandstone; readable charcoal shadows; selective mustard, muted teal and cyan accents; bare arid mountains and dry streets. Preserve the supplied panel's composition and story evidence. No global orange cast, muddy gray wash, lake, river, green mountains, baked text, UI or logos.”

Generation uses the built-in imagegen tool. The 2026-09-09 style transfer prompts, references and generated paths are retained in `generation_manifest.json`; the later continuity revisions and their acceptance evidence are recorded in `continuity_review_2026-09-27.md`. A generated candidate is not automatically an installed or accepted replacement.
