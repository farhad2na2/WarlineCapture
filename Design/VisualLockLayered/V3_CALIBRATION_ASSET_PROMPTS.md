# V3 Calibration Asset Prompts

Generation mode: built-in `image_gen`.

The built-in generator returned opaque checkerboard backgrounds for the first
two transparency attempts. The accepted sources therefore use the repository's
flat `#00ff00` green-key workflow, followed by deterministic keying, neutral
despill, alpha clamping, and Lanczos downsampling.

Style references for core chrome:

- `SCN-02_MainMenuV3/reference/SCN-02_MainMenuV3_Final_Target.png`
- `POP-06_Settings/reference/POP-06_SettingsV3_Final_Target.png`
- `SCN-08_MatchHudV3/reference/SCN-08_MatchHudV3_Final_Target.png`

The attack icon also uses:

- `SCN-10_UnitCommandWheel/reference/SCN-10_UnitCommandWheelV3_Final_Target.png`

## `ui_core_panel_9s`

```text
Use case: background-extraction
Asset type: canonical shared Unity UI 9-slice panel sprite for Warline Capture V3
Primary request: Generate exactly one reusable neutral V3 panel frame and face,
canonical asset ui_core_panel_9s, on a perfectly flat uniform solid chroma-key
green #00ff00 background for local extraction.
Style: sharp high-end mobile military command UI sprite with vector-like raster
precision. Use hard 90-degree rectangular geometry, perfectly straight edges,
square corners, symmetric stretch-safe construction, a crisp continuous
highlight stroke, a restrained inner graphite face, and subtle contained depth.
Color: neutral charcoal, graphite, and near-white edge accents only; tintable in
Unity; no green or semantic state color in the art.
Constraints: one centered complete front-facing component; no text, icons,
labels, numbers, scene art, rounded corners, chamfers, ornamental bevels, fuzzy
glow, watermark, or additional components. Background is exactly flat #00ff00
with no checkerboard, shadow, gradient, texture, reflection, or lighting change.
```

## `ui_core_button_9s`

```text
Use case: background-extraction
Asset type: canonical shared Unity UI 9-slice button sprite for Warline Capture V3
Primary request: Generate exactly one reusable neutral V3 button frame and face,
canonical asset ui_core_button_9s, on a perfectly flat uniform solid chroma-key
green #00ff00 background for local extraction.
Style: sharp high-end mobile military command UI sprite with vector-like raster
precision. Use a wide horizontal hard rectangle, straight edges, square corners,
symmetric stretch-safe construction, a crisp border, restrained graphite face,
and contained hard depth. It must scale to square, tab, footer, and wide action
proportions through Unity 9-slicing.
Color: neutral charcoal, graphite, and near-white accents only; tintable in Unity.
Constraints: one centered complete front-facing component; no text, icons,
labels, numbers, semantic hue, selected glow, rounded corners, chamfers,
ornamental bevels, watermark, extra elements, or scene art. Background is
exactly flat #00ff00 with no checkerboard, shadow, gradient, texture, reflection,
noise, or lighting variation.
```

## `ui_core_focus_overlay_9s`

```text
Use case: background-extraction
Asset type: canonical shared Unity UI 9-slice focus/selection overlay sprite
Primary request: Generate exactly one reusable empty V3 focus outline overlay,
canonical asset ui_core_focus_overlay_9s, on a perfectly flat uniform solid
chroma-key green #00ff00 background for local extraction.
Subject: one wide neutral white-to-light-gray rectangular outline only, with the
empty center represented by the same green background; hard 90-degree inner and
outer corners, straight symmetric edges, consistent stroke thickness, no face,
shadow, protrusions, or perspective; tintable in Unity.
Constraints: the background and empty center are exactly flat #00ff00. No text,
icons, labels, numbers, glow, semantic color, rounded corners, chamfers,
ornamental bevels, watermark, extra components, checkerboard, gradient, texture,
noise, or lighting variation.
```

## `ui_icon_attack`

```text
Use case: background-extraction
Asset type: canonical monochrome Unity tactical action icon for Warline Capture V3
Primary request: Generate exactly one reusable ATTACK action icon, canonical
asset ui_icon_attack, on a perfectly flat uniform solid chroma-key green #00ff00
background for local extraction.
Subject: a strong simple military targeting crosshair with four compact angular
brackets and a small centered impact diamond/dot. Use monochrome white-to-light
gray art with a bold consistent stroke, balanced negative space, and a flat
front-facing silhouette readable at 32-96 pixels; tintable in Unity.
Constraints: one centered square icon only; no surrounding button, frame,
circle, badge, plate, label, text, letters, numbers, gun, sword, explosion,
skull, glow, state color, watermark, extra icon, or scene art. Background is
exactly flat #00ff00 with no checkerboard, gradient, texture, shadow, reflection,
noise, or lighting variation.
```
