# WarlineCapture UI HQ Green-Key To Final Sprite Workflow

Use this workflow when an existing UI panel/sprite is too low quality, clipped, or jagged, but the desired shape/layout is already known. The goal is to regenerate a high-quality source on green key, remove the green to full alpha, crop empty transparent padding, and save a Unity-ready sprite under `Assets/Game/Art/UI/Final`.

## When To Use

- A UI frame, panel, button, icon, or chrome layer is low resolution.
- The source image has clipped borders or missing edges.
- The asset needs a clean transparent PNG for Unity UI.
- The user asks for imagegen output with green background that can be cut transparent.

Do not use this workflow for code-native SVG/vector work or for already-clean transparent PNGs that only need import settings.

## Required Inputs

- Source/reference image path or attached image.
- Final asset name.
- Final destination under:
  `Assets/Game/Art/UI/Final`

Example:

- Source: `/Users/farhad/Desktop/scn19_inspection_panel_frame.png`
- Final: `Assets/Game/Art/UI/Final/scn19_inspection_panel_frame.png`

## Step 1: Generate HQ Green-Key Source With Imagegen

Use the imagegen skill and ask for a high-quality regenerated version on a flat chroma-key background.

Prompt requirements:

```text
Use case: background-extraction
Asset type: Unity UI sprite source, chroma-key background for later transparency cutout
Input image: the provided image is the edit/reference target. Preserve its exact overall composition, proportions, panel layout, material style, corner details, and visual language.

Primary request: Create a higher-quality regenerated version of this same UI asset on a perfectly flat solid #00ff00 chroma-key background. Repair any clipped borders, missing edges, jagged lines, or low-quality artifacts.

Critical requirements:
- Preserve the same layout and silhouette as the reference.
- Keep every border fully inside the canvas with generous padding.
- Use crisp anti-aliased edges and clean linework.
- No text, no watermark, no unwanted icons unless explicitly requested.
- Background must be exactly uniform #00ff00 with no shadows, gradients, texture, lighting variation, reflections, or green spill.
- Do not use #00ff00 anywhere in the actual UI artwork.
```

Save the generated source in a non-final generated-art folder first, for traceability:

```text
Assets/Game/Art/UI/Generated/<SCREEN_OR_FEATURE>/<AssetName>/<asset_name>_hq_green.png
```

## Step 2: Normalize Green Key

Imagegen can produce near-green pixels instead of exact `#00ff00`. Normalize all green-key background pixels to exact `#00ff00` before alpha removal.

Example command:

```bash
python3 - <<'PY'
from PIL import Image

src = 'Assets/Game/Art/UI/Generated/SCN19/InspectionPanel/scn19_inspection_panel_frame_hq_green.png'
out = 'Assets/Game/Art/UI/Generated/SCN19/InspectionPanel/scn19_inspection_panel_frame_hq_green_flatkey.png'

im = Image.open(src).convert('RGB')
px = im.load()
changed = 0
for y in range(im.height):
    for x in range(im.width):
        r, g, b = px[x, y]
        if g >= 140 and r <= 90 and b <= 90 and g > r * 2 and g > b * 2:
            px[x, y] = (0, 255, 0)
            changed += 1

im.save(out)
print(out, im.size, 'changed', changed)
PY
```

Validate corner pixels are exact `#00ff00`:

```bash
python3 - <<'PY'
from PIL import Image
p = 'Assets/Game/Art/UI/Generated/SCN19/InspectionPanel/scn19_inspection_panel_frame_hq_green_flatkey.png'
im = Image.open(p).convert('RGB')
for pt in [(0, 0), (im.width - 1, 0), (0, im.height - 1), (im.width - 1, im.height - 1)]:
    print(pt, im.getpixel(pt))
PY
```

Expected corner pixels:

```text
(0, 255, 0)
```

## Step 3: Remove Green To 100% Transparent Alpha

Use the bundled imagegen chroma-key remover.

```bash
python3 "$HOME/.codex/skills/.system/imagegen/scripts/remove_chroma_key.py" \
  --input Assets/Game/Art/UI/Generated/SCN19/InspectionPanel/scn19_inspection_panel_frame_hq_green_flatkey.png \
  --out Assets/Game/Art/UI/Final/scn19_inspection_panel_frame.png \
  --auto-key border \
  --soft-matte \
  --transparent-threshold 8 \
  --opaque-threshold 220 \
  --despill
```

The final PNG must be `RGBA`, with corners at alpha `0`.

## Step 4: Crop Empty Transparent Padding

Crop to the alpha bounding box so no empty transparent space remains around the sprite.

```bash
python3 - <<'PY'
from PIL import Image

p = 'Assets/Game/Art/UI/Final/scn19_inspection_panel_frame.png'
im = Image.open(p).convert('RGBA')
bbox = im.getchannel('A').getbbox()
if bbox is None:
    raise SystemExit('No non-transparent pixels found')

cropped = im.crop(bbox)
cropped.save(p)
print('before', im.size, 'bbox', bbox, 'after', cropped.size)
PY
```

## Step 5: Add Unity Sprite Import Metadata

Create a `.png.meta` beside the final PNG with:

- `textureType: 8`
- `spriteMode: 1`
- `alphaUsage: 1`
- `alphaIsTransparency: 1`
- `enableMipMap: 0`
- `filterMode: 1`
- `spritePixelsToUnits: 100`

Use a unique GUID. Label with `WarlineCaptureUI` and the screen/feature id when useful.

## Step 6: Validate

Run a pixel validation:

```bash
python3 - <<'PY'
from PIL import Image

p = 'Assets/Game/Art/UI/Final/scn19_inspection_panel_frame.png'
im = Image.open(p).convert('RGBA')
a = im.getchannel('A')
print('mode', im.mode, 'size', im.size)
print('alpha_bbox', a.getbbox())
print('corner_alpha', [a.getpixel(pt) for pt in [(0, 0), (im.width - 1, 0), (0, im.height - 1), (im.width - 1, im.height - 1)]])
print('transparent', sum(1 for v in a.getdata() if v == 0))
print('partial', sum(1 for v in a.getdata() if 0 < v < 255))
print('opaque', sum(1 for v in a.getdata() if v == 255))
PY
```

Pass criteria:

- `mode` is `RGBA`.
- `alpha_bbox` starts at `(0, 0, width, height)` after crop.
- Corner alpha values are `0` if the corners are outside the sprite silhouette.
- No visible green remains.
- No border is clipped.
- Sprite meta exists beside the final PNG.

## Output Convention

Keep both source and final assets:

```text
Assets/Game/Art/UI/Generated/<SCREEN_OR_FEATURE>/<AssetName>/<asset_name>_hq_green.png
Assets/Game/Art/UI/Generated/<SCREEN_OR_FEATURE>/<AssetName>/<asset_name>_hq_green_flatkey.png
Assets/Game/Art/UI/Final/<asset_name>.png
Assets/Game/Art/UI/Final/<asset_name>.png.meta
```

Do not overwrite unrelated assets. If replacing an existing final sprite, confirm the requested replacement or save a versioned filename.

