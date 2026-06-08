# UI Screen Reference To Icons/Panels Green-Key Workflow

Use this workflow when the user asks to create layered UI assets from a screen or popup reference and place the resulting sprites under:

```text
Assets/Game/Art/UI/Icons
Assets/Game/Art/UI/Panels
```

The goal is reusable Unity UI sprites: icons, buttons, panels, frames, borders, badges, chips, dividers, and command controls. Do not place rectangular content art, building thumbnails, portraits, minimaps, or full-screen mockups in these two folders unless the user explicitly asks for those asset types.

## Required Inputs

- Screen or popup id, for example `SCN08`, `SCN09`, or `SCN13`.
- Canonical reference image under `Design/VisualLockLayered/<SurfaceId>/reference/`.
- Optional existing green source sheets under `Design/VisualLockLayered/<SurfaceId>/generated*/source/`.

If the user asks for a screen but does not provide a reference path, find it first with:

```bash
rg --files Design/VisualLockLayered | rg -i "<screen id>|<screen name>|reference"
```

Show the reference image before generation when the target is ambiguous.

## Output Rules

Save final sprites as:

```text
Assets/Game/Art/UI/Icons/<screen>_<asset_id>.png
Assets/Game/Art/UI/Panels/<screen>_<asset_id>.png
```

Use lowercase ids with the screen prefix, for example:

```text
scn09_icon_build_tools.png
scn09_panel_gold_action_button_bg.png
scn08_command_move_chevrons.png
scn08_command_button_selected_frame.png
```

Every PNG must have a matching Unity sprite `.meta` file using:

- `textureType: 8`
- `spriteMode: 1`
- `alphaUsage: 1`
- `alphaIsTransparency: 1`
- `enableMipMap: 0`
- `spritePixelsToUnits: 100`
- `filterMode: 1`

Do not overwrite unrelated assets. Same-screen regeneration may overwrite same-name assets only when the user requested cleanup/replacement for that screen.

## Imagegen Request

Use the `imagegen` skill. Ask for separate green-background source sheets, not a full baked UI screenshot.

Request at least two sheets:

1. **Panels/chrome sheet**
   - frames
   - panel backgrounds
   - button selected/default/disabled states
   - cards
   - chips
   - dividers
   - strips
   - popup frames

2. **Icons sheet**
   - command icons
   - resource icons
   - close/settings/menu icons
   - checkboxes
   - arrows/chevrons
   - warnings/markers/badges

Only request content art in a separate sheet when the user asks for it. Do not mix buildings/portraits/minimap art into `Icons` or `Panels`.

Prompt requirements:

```text
Use case: ui-mockup
Asset type: Unity UI sprite source sheet, chroma-key extraction source
Reference image: the loaded <SCREEN_ID> target lock. Use only the <screen/popup> UI layer assets, not the surrounding screen unless explicitly requested.

Primary request: Generate separate reusable UI layer assets on a perfectly flat solid #00ff00 chroma-key background for later transparency cutting.

Required assets: <explicit list of panels/icons needed>

Style: match the reference exactly: clean military tactical chrome, thin worn gold bevels, dark graphite interiors, narrow cut corners, subtle amber edge wear, crisp linework. Do not invent thick borders, double borders, neon glow, cartoon rounded panels, generic symbols, or extra decorations.

Layout: arrange each layer in a neat grid with generous #00ff00 gaps. Every asset must be complete, uncut, centered, and padded enough for cropping.

Background: one uniform #00ff00 chroma-key color only, with no shadows, gradients, texture, lighting variation, floor plane, reflections, or green spill. Do not use #00ff00 anywhere in the UI artwork.

Output constraints: no text labels on the sheet, no watermark, no full mockup screenshot, no gameplay background, no transparent background.
```

Save raw generated sheets under a traceable generated folder, for example:

```text
Assets/Game/Art/UI/Generated/<SurfaceName>/ImagegenLayerRequests/<Surface>_Panels_Green_v01.png
Assets/Game/Art/UI/Generated/<SurfaceName>/ImagegenLayerRequests/<Surface>_Icons_Green_v01.png
```

If imagegen fails but an existing approved green source sheet exists in `Design/VisualLockLayered/<SurfaceId>/generated*/source/`, it can be used only after confirming it is the correct current surface and scope.

## Cutting And Cropping Rules

Use deterministic local processing for the actual cut. Do not trust visual inspection only.

For each sheet:

1. Remove green to alpha.
2. Split assets by manifest source boxes when a `layer_manifest.json` exists.
3. If no manifest exists, use explicit crop regions or connected components, but group disconnected icons that belong together, such as list icons or corner brackets.
4. Crop every output to its actual alpha bounding box.
5. Preserve intentional olive/gold artwork inside the sprite. Only remove green-key background and green fringe.

## Mandatory Clean Green Pass

This pass is required. Do not stop after basic chroma-key removal.

Imagegen often leaves dark key-green pixels such as `(2, 104, 1)` around edges. These are visible in Unity even when bright `#00ff00` is gone.

Run all three checks and fixes:

### 1. Remove Bright And Dark Pure Key Green Globally

Remove pixels that are pure key-green remnants:

```python
def is_pure_key_green(r, g, b):
    return g >= 24 and r <= 24 and b <= 24 and g >= max(r, b) + 20

def is_key_green_fringe(r, g, b):
    return g >= 45 and r <= 42 and b <= 42 and g >= max(r, b) * 2.2
```

Any opaque pixel matching either rule must become `(0, 0, 0, 0)`.

### 2. Remove Or Despill Green Near Transparent Edges

Remove or despill greenish pixels touching transparency:

```python
def greenish(r, g, b):
    return g >= 35 and g > r + 8 and g > b + 8 and g >= max(r, b) * 1.08

def weak_greenish(r, g, b):
    return g >= 28 and g > r + 5 and g > b + 5 and g >= max(r, b) * 1.04
```

If a matching pixel is within 1-3 pixels of transparent alpha, either:

- set it to alpha `0` when it is clearly key background, or
- despill it by lowering green to `max(r, b)` when cutting would erode the sprite shape.

### 3. Scrub Crop Borders

After re-cropping, inspect the outer 3 px border. Remove suspicious green pixels on the crop border:

```python
def suspicious_border_green(r, g, b):
    return g >= 30 and r <= 38 and b <= 38 and g >= max(r, b) + 12
```

Any opaque border pixel matching this rule must become transparent. Then crop to alpha bounds again.

## Required Validation

Run validation on every output PNG before reporting done.

Pass criteria:

- `RGBA` mode.
- Alpha bounding box equals full image bounds after crop:

```text
alpha_bbox == (0, 0, width, height)
```

- No pure key-green pixels remain anywhere:

```python
pure_key_remaining == 0
```

- No suspicious green pixels remain on the crop border:

```python
suspicious_border_green_remaining == 0
```

- Named user-problem assets must be checked directly. If the user named `scn08_minimap_zoom_plus_icon.png`, validate that exact file and report its result.
- Generate checkerboard preview sheets for icons and panels so green halos are visible before final response.

Validation summary format:

```text
files=<count> icons=<count> panels=<count>
clamp_errors=0
pure_key_remaining=0
suspicious_border_green_remaining=0
```

If any value is nonzero, do another cleanup pass. Do not report completion with visible or measured green leftovers.

## Preview Requirement

Create two preview sheets:

```text
/private/tmp/<screen>_icons_greenkey_clean_preview.png
/private/tmp/<screen>_panels_greenkey_clean_preview.png
```

Preview on a checkerboard background. Include asset filenames under each thumbnail. Inspect the preview before final response.

## Final Response

Report:

- Reference path used.
- Whether imagegen generated fresh sheets or existing approved green source sheets were used.
- Final destination folders.
- Counts of icons and panels.
- Clean-green validation numbers.
- Preview image links.

Example:

```text
Saved:
- Assets/Game/Art/UI/Icons
- Assets/Game/Art/UI/Panels

Validation:
- files=63
- icons=31
- panels=32
- clamp_errors=0
- pure_key_remaining=0
- suspicious_border_green_remaining=0
```

## Common Failure Modes

- **Visible green line remains:** basic bright-key removal missed dark key-green edge pixels. Run the pure dark key-green and border scrub passes.
- **Icon is broken into pieces:** connected components split a logical icon. Use manifest boxes or explicit grouped crop regions.
- **Intentional olive UI fill is damaged:** the cleanup rule was too broad. Use border/edge-only cleanup for olive/gold interiors.
- **Transparent padding remains:** crop after every cleanup pass, not just after first cut.
- **Wrong scope:** generated the full screen or included surrounding HUD. Regenerate only panels/icons for the requested surface.
