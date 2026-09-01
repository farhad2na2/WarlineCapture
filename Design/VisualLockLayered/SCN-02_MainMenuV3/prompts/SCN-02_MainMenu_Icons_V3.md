# SCN-02 Main Menu V3 Icon Set

Built-in tool mode: `imagegen` image editing. Five exact V3 target crops were
used as edit targets. Each output preserves only the target icon and has genuine
transparent alpha. The originals under `.codex/generated_images` remain
untouched; runtime copies are deterministically trimmed, resized, padded, and
have alpha below 8% removed to eliminate subpixel debris.

All five runtime sprites are packed exactly once in
`UI_V3_MainMenuIcons_01.spriteatlas`.

## Campaign target

Generated source:

`/Users/farhad/.codex/generated_images/01a05313-9ab3-7c90-9fff-9ea1e1a0134c/exec-5de3f24d-5f56-4c16-a945-97bd3039a077.png`

Final center-ring correction source:

`/Users/farhad/.codex/generated_images/01a05313-9ab3-7c90-9fff-9ea1e1a0134c/exec-9b1e8d1e-ab1d-42f6-9167-8aa3a2be03f2.png`

Runtime asset:

`Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_CampaignTarget_V3.png`

```text
Use case: background-extraction
Asset type: sharp reusable Unity UI icon sprite for the SCN-02 Main Menu Campaign card.
Input image: edit target and exact visual reference.
Primary request: extract and faithfully reconstruct only the central gold target-reticle icon from the supplied crop. Preserve its exact circular silhouette, four directional gaps/ticks, central ring, warm amber-gold color, subtle metallic bevel, hard crisp edges, proportions, and front-facing orientation.
Background: genuine transparent alpha across every non-icon pixel.
Constraints: no square cell, no brown panel, no border, no glow box, no text, no added symbols, no shadow outside the icon, no watermark. Center the icon with generous transparent padding and do not crop any tip.
```

Targeted correction:

```text
Use case: precise-object-edit
Asset type: transparent Unity UI Campaign target icon.
Input image 1: edit target. Input image 2: exact approved V3 reference.
Primary request: change only the center of Image 1 so it matches Image 2: replace the filled gold center disc with a crisp hollow circular targeting ring with a genuinely transparent dark/open center. Preserve the existing outer gold reticle, four directional ticks, metallic bevel, color, sharp edges, scale, centering, and transparent canvas exactly.
Constraints: change only the center-disc geometry; no panel, no background, no border, no text, no glow box, no red debris, no added marks, no watermark. Keep genuine transparent alpha outside the gold icon and inside the hollow center.
```

## Operations compass

Generated source:

`/Users/farhad/.codex/generated_images/01a05313-9ab3-7c90-9fff-9ea1e1a0134c/exec-fd78b5e1-362f-447e-a02a-5260ecd987b0.png`

Runtime asset:

`Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_OperationsCompass_V3.png`

```text
Use case: background-extraction
Asset type: sharp reusable Unity UI icon sprite for the SCN-02 Main Menu Operations card.
Input image: edit target and exact visual reference.
Primary request: extract and faithfully reconstruct only the central military compass-rose icon from the supplied crop. Preserve its exact circular compass ring, long north/south/east/west points, smaller diagonal points, inner star geometry, pale desaturated green-white metal, subtle low-poly bevel, hard crisp edges, proportions, and front-facing orientation.
Background: genuine transparent alpha across every non-icon pixel.
Constraints: no green cell, no panel, no border, no text, no chevron, no added symbols, no shadow outside the icon, no watermark. Center the complete icon with generous transparent padding.
```

## Skirmish blades

Generated source:

`/Users/farhad/.codex/generated_images/01a05313-9ab3-7c90-9fff-9ea1e1a0134c/exec-27752bf5-03d4-459a-b831-fad12fe8e27a.png`

Runtime asset:

`Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_SkirmishBlades_V3.png`

```text
Use case: background-extraction
Asset type: sharp reusable Unity UI icon sprite for the SCN-02 Main Menu Skirmish card.
Input image: edit target and exact visual reference.
Primary request: extract and faithfully reconstruct only the central pair of crossed military blades from the supplied crop. Preserve the exact crossed silhouette, pointed faceted blades, short guards and handles, orange-red and amber-gold material, dark bevel/shadow detail, crisp low-poly hard edges, proportions, and front-facing orientation.
Background: genuine transparent alpha across every non-icon pixel.
Constraints: no red cell, no panel, no border, no text, no added symbols, no shadow outside the icon, no watermark. Center the complete crossed blades with generous transparent padding.
```

## Store cart

Generated source:

`/Users/farhad/.codex/generated_images/01a05313-9ab3-7c90-9fff-9ea1e1a0134c/exec-4fe081a0-237d-45a1-9ac7-ad0cc9a65e7f.png`

Runtime asset:

`Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_StoreCart_V3.png`

```text
Use case: background-extraction
Asset type: sharp reusable Unity UI icon sprite for the SCN-02 Main Menu Store footer button.
Input image: edit target and exact visual reference.
Primary request: extract and faithfully reconstruct only the central white shopping-cart icon from the supplied crop. Preserve its exact angled handle, tapered solid basket, lower axle, two circular wheels, off-white metal, subtle gray bevel/shadow along the form, crisp hard edges, proportions, and front-facing orientation.
Background: genuine transparent alpha across every non-icon pixel.
Constraints: no blue button, no panel, no border, no text, no added marks, no external drop shadow, no watermark. Center the complete cart with generous transparent padding and do not crop the handle or wheels.
```

## Armory crate

Generated source:

`/Users/farhad/.codex/generated_images/01a05313-9ab3-7c90-9fff-9ea1e1a0134c/exec-9d04ab04-3785-4370-9dd5-35ad12a32106.png`

Runtime asset:

`Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_ArmoryCrate_V3.png`

```text
Use case: background-extraction
Asset type: sharp reusable Unity UI icon sprite for the SCN-02 Main Menu Armory footer button.
Input image: edit target and exact visual reference.
Primary request: extract and faithfully reconstruct only the central compact military equipment crate from the supplied crop. Preserve its exact three-quarter perspective, square reinforced gray metal body, segmented lid panels, dark seams and bevels, small amber top detail, bright amber square side latch, crisp low-poly hard-surface edges, proportions, and high-quality game UI rendering.
Background: genuine transparent alpha across every non-crate pixel.
Constraints: no navy button, no panel, no border, no text, no floor, no external drop shadow, no added objects, no watermark. Center the complete crate with generous transparent padding.
```
