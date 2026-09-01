# SCN-02 Main Menu V3 ARIA Portrait

Built-in tool mode: `imagegen` image editing. The supplied references were the
cropped V3 ARIA target panel and the existing ARIA identity portrait. The
generated chroma source was converted mechanically to a transparent PNG.

Generated source:

`/Users/farhad/.codex/generated_images/01a05313-9ab3-7c90-9fff-9ea1e1a0134c/exec-5f3bdb40-161e-47b0-b966-2f2521aff0f3.png`

Unity runtime asset:

`Assets/Game/Art/UI/V3Shared/Portraits/ARIA_MainMenu_V3.png`

SHA-256: `30cb45e4291f57d65222e37ee84ce6b303c16b94fcfc8aea179c606e0089c115`

## Final prompt

```text
Use case: stylized-concept
Asset type: isolated Unity UI character portrait source for the SCN-02 Main Menu ARIA panel.

Recreate only ARIA's holographic female bust from the supplied V3 target crop. Match the target exactly: adult woman, asymmetric side-swept bob with long lock on viewer-left, calm focused expression, three-quarter face orientation, visible neck, shoulders and upper chest, crisp low-poly triangular faceting, luminous cyan and deep-blue hologram shading, sharp high-quality edges. Preserve the same face identity and polygonal treatment shown in the references.

Place the isolated bust on one perfectly flat, uniform, solid RGB #FF00FF chroma-magenta background covering every non-character pixel. Keep generous magenta padding around all hair and shoulders. Do not crop hair, shoulders, or chest.

Remove all ARIA text, panel border, black panel background, HUD ticks, charts, crosshair, lines, labels, glow haze, logos, watermark, and all other interface elements. No checkerboard, no gradient background, no shadow, no environment. Ensure no magenta appears inside the character art.
```

## Deterministic alpha cleanup

```bash
magick exec-5f3bdb40-161e-47b0-b966-2f2521aff0f3.png -alpha on -fuzz 24% -transparent '#ff00ff' -trim +repage aria_alpha_24.png
magick aria_alpha_24.png \( +clone -alpha extract -morphology Erode Disk:1 \) -alpha off -compose CopyOpacity -composite ARIA_MainMenu_V3.png
```
