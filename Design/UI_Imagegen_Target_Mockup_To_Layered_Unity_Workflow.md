# UI Imagegen Target Mockup To Layered Unity Workflow

This is the mandatory workflow for creating or revising visual-lock UI screens, popups, and reusable UI panels from imagegen output.

The key rule: the target mockup is a visual reference only. Runtime UI sprites must come from separately requested imagegen layers on a flat green background, then be cut to transparent sprites. Do not crop, extract, patch, or reuse artwork from the flattened target mockup as production UI layers.

## Required Sequence

1. **Request the full target mockup from imagegen**
   - Generate the complete screen/popup composition.
   - Use this image only to approve visual direction, hierarchy, layout, logo style, panel proportions, color, spacing, and typography intent.
   - Save it under `Design/VisualLockLayered/<SurfaceId>/reference/`.

2. **Get target approval before layer work**
   - Do not request layers until the target mockup is accepted or explicitly chosen as the source of truth.
   - If the target changes, discard stale layer requests and regenerate against the new target.

3. **Create a layer request list**
   - List every reusable production layer needed for Unity:
     - background/no-UI plate
     - logo lockup
     - panel frames
     - panel fills/backplates
     - button states
     - command chips
     - progress/slider tracks and fills
     - icons, badges, corner accents, dividers, markers
   - Separate dynamic text from sprites. Do not bake live labels or values into reusable panels.

4. **Request each layer or layer sheet from imagegen on green background**
   - Each requested asset must be generated separately, or as a clean layer sheet, on a perfectly flat solid `#00ff00` background.
   - Use the accepted target mockup as style reference only.
   - Ask imagegen to produce complete, uncut, padded assets with no text unless the asset is explicitly a decorative logo lockup.
   - Do not ask for a transparent background through the built-in imagegen path; use green-key and local removal.

5. **Cut green to 100% transparent**
   - Normalize green-key background first when needed.
   - Remove bright and dark green remnants.
   - Despill green fringe around antialiased edges.
   - Validate that no visible or measured green remains.

6. **Clamp empty transparent space**
   - Crop every output to the alpha bounding box.
   - Repeat clamping after every cleanup pass.
   - Transparent padding that changes Unity layout is a failed asset.

7. **Create Unity sprite metadata**
   - Import as Sprite.
   - `textureType: 8`
   - `spriteMode: 1`
   - `alphaUsage: 1`
   - `alphaIsTransparency: 1`
   - `enableMipMap: 0`
   - `filterMode: 1`
   - `spritePixelsToUnits: 100`
   - Add 9-slice borders for scalable frames, panels, buttons, chips, tracks, and fills.

8. **Build the Unity UI from the separated layers**
   - Canvas/UI Toolkit objects must use separated sprites, live text, and real controls.
   - Background art, frames, icons, fills, labels, and state visuals must remain separate.
   - Never use the target mockup as a full-screen background to fake the UI.

9. **Compare implementation against target**
   - Capture rendered output at the target aspect and 20:9.
   - Compare target vs rendered screen and focused crops.
   - Iterate by changing layout, sizing, layer prompts, or layer generation. Do not solve mismatches by cropping the flattened target.

## Non-Negotiable Rules

- Do not crop a logo, panel, icon, frame, background detail, or button from the flattened target mockup for runtime use.
- Do not use deterministic drawing, vector reconstruction, or patched screenshots as production art when the workflow calls for imagegen layers.
- Do not reuse old screen assets just because they are close; request the needed asset in the current target style.
- Do not keep stale mockups in active reference paths. Archive or delete rejected targets so future searches find only the approved current reference.
- Do not claim target match until the implementation is compared against the accepted target and obvious differences are fixed or listed as not accepted.

## Allowed Deterministic Steps

Deterministic local tooling is allowed only after imagegen creates the asset source:

- file copy/rename
- green-key normalization
- chroma-key alpha removal
- green fringe cleanup/despill
- crop/clamp to alpha bounds
- 9-slice metadata creation
- atlas/import metadata
- validation previews/contact sheets
- Unity layout and live text implementation

## Prompt Template: Target Mockup

```text
Use case: ui-mockup
Asset type: full target mockup for <SurfaceId>, landscape game UI.
Primary request: Generate the complete <screen/popup> target mockup in the approved visual direction.
Scene/backdrop: <screen-specific background>
UI composition: <major panels and hierarchy>
Style: premium stylized military command UI, clean chrome, readable text, cohesive layout.
Text: <reference text only; final Unity text will be live unless decorative logo>
Constraints: coherent full-screen mockup, no watermark, no placeholder UI, no random extra panels.
```

## Prompt Template: Individual Green-Key Layer

```text
Use case: background-extraction
Asset type: Unity UI sprite source, chroma-key background for later transparency cutout.
Reference image: the accepted <SurfaceId> target mockup is style reference only. Do not crop or copy from it.
Primary request: Generate <specific layer name> matching the target style on a perfectly flat solid #00ff00 background.
Requirements:
- complete, uncut, centered asset
- generous green padding for cropping
- crisp antialiased edges
- no text unless this is the approved decorative logo lockup
- no extra icons, labels, background scene, watermark, or full-screen UI
- background exactly uniform #00ff00 with no shadows, gradients, texture, reflections, or lighting variation
- do not use #00ff00 anywhere in the artwork
```

## Validation Checklist

Before implementation:

- Current target mockup path is known and shown/confirmed.
- Layer list exists.
- Every production sprite source came from imagegen green-key output, not target crops.
- Green removed to alpha.
- Empty transparent space clamped.
- 9-slice metadata added where needed.
- Checker/contact sheet inspected.

Before handoff:

- SCN/POP target and rendered output compared.
- Logo, panels, icons, text scale, progress bars, and spacing checked against target.
- Any remaining mismatch is listed as `not target-matched`.
