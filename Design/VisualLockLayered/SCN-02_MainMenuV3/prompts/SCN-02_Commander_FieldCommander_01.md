# SCN-02 Main Menu V3 Field Commander 01

> Superseded provenance only. The transparent cutout exposed a pasted-on edge
> against the independently generated environment. The runtime now uses the
> cohesive baked scene documented in
> `SCN-02_MainMenuEnvironment_V3_BackgroundPlate.md`.

Built-in tool mode: `imagegen` image editing, using the V3 target/composite as
the pose and style input. The generated chroma source is converted mechanically
to a transparent PNG so the same commander can be selected independently of the
environment and live UI.

Generated chroma source:

`/Users/farhad/.codex/generated_images/01a05313-9ab3-7c90-9fff-9ea1e1a0134c/exec-ee912fe7-f17c-4cdb-b5f7-3ac922b2e710.png`

Archived provenance asset:

`Design/VisualLockLayered/SCN-02_MainMenuV3/provenance/split_experiment/Commander_FieldCommander_01.png`

Commander ID: `field_commander_01`

SHA-256: `c8caa82c6577f63ca2c8a4fc049d4d1142cb86bb08719a530e5d34132080c284`

## Final prompt

```text
Use case: stylized-concept
Asset type: Unity character cutout source on chroma key for a swappable Main Menu commander.
Recreate only the central male field commander from the supplied reference: full head and gray hair, face/expression, neck, dark green shirt, tan jacket, torso, both arms, and both complete hands in the exact leaning-over-table pose. Preserve crisp low-poly/polygonal military RTS style, perspective, lighting direction, proportions, and sharp edge detail.

Place the isolated character on one perfectly flat, uniform, solid RGB #00FF00 chroma-green background covering every non-character pixel. No checkerboard, no gradient, no texture, no shadow, no glow, no floor, no environment. Keep generous green padding around the character and do not crop hair, elbows, fingertips, or lower torso.

Remove all flag, vehicles, towers, helicopter, sky, smoke, soldiers, tactical table, cyan hologram, map, UI, text, logos, borders, and background objects. Ensure no green appears inside the character art.
```

## Deterministic alpha cleanup

```bash
magick exec-ee912fe7-f17c-4cdb-b5f7-3ac922b2e710.png -alpha on -fuzz 28% -transparent '#00ff00' -trim +repage commander_alpha_28.png
magick commander_alpha_28.png \( +clone -alpha extract -morphology Erode Disk:1 \) -alpha off -compose CopyOpacity -composite Commander_FieldCommander_01.png
```
