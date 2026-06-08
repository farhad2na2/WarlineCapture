# WarlineCapture Target-to-Canvas Workflow Guide

## Purpose

Use this guide whenever a high-quality UI target mockup needs to become a real Unity Canvas prefab.

The target mockup is only the visual contract. The shipped UI must be made from separate Canvas objects, reusable sliced sprites, transparent icons, TMP text, and real controls. Do not ship a flattened target image or large baked panel crops as UI.

## Required Inputs

For each screen, popup, or reusable panel, start with:

- A canonical target image under `Design/VisualLockLayered/<SurfaceId>/reference/`.
- A layer pack under `Design/VisualLockLayered/<ScreenId>/layers`.
- A layer manifest next to the target, for example `Design/VisualLockLayered/<ScreenId>/layer_manifest.json`.
- The target Unity prefab path, for example `Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab`.
- The builder/test files that own that screen.

If a new target is being generated, request the flattened target and the separate layer assets in the same generation workflow. Do not generate a target first and then try to reverse-engineer clean layers from the flattened image unless there is no alternative.

## Hard Layer-Pack Gate

This is a blocking gate. Before editing Unity prefab files, builder code, generated Unity sprites, or tests for any visual-lock screen, popup, or reusable panel, verify that the matching layer-pack folder exists:

- `Design/VisualLockLayered/<SurfaceId>/reference/<SurfaceId>_Landscape_Target.png`
- `Design/VisualLockLayered/<SurfaceId>/layers/`
- `Design/VisualLockLayered/<SurfaceId>/layer_manifest.json`
- `Design/VisualLockLayered/<SurfaceId>/generated_one_go/layers_contact_sheet.png`
- `Design/VisualLockLayered/<SurfaceId>/README.md`

If any item is missing, do not implement or revise the Unity Canvas prefab. Create or repair the layer pack first, then run a layer-pack validation check. If the layer pack cannot be created in the current turn, report `blocked on layer-pack gate` and stop before touching the prefab.

For popup work, this gate applies to every prefab under `Assets/Game/Prefabs/UI/Popups`, including existing popups. Existing non-layer-pack popup prefabs are not visually locked and must not be used as acceptance baselines.

Add a validation step to every implementation plan:

```text
Layer-pack gate: verify `Design/VisualLockLayered/<SurfaceId>/reference`, `layers`, `layer_manifest.json`, `layers_contact_sheet.png`, and `README.md` before any prefab or builder edit. If missing, create the layer pack first. Do not proceed to Canvas implementation until this passes.
```

When a Unity/EditMode validation test exists for the current surface, run it before visual implementation. If it does not exist, add or update the plan with the required validation before continuing.

## Target Contract Gate

The target mockup is the contract for both visual design and visible content. Text strings, reward amounts, objective names, difficulty labels, icons, selected states, image subjects, relative scale, and layout hierarchy must match the target unless the target is deliberately revised first.

Do not silently substitute runtime/canonical content and still call the screen target-matched. If content or gameplay rules have changed since the target was generated, either regenerate/update the target and layer pack before Canvas work, or list the difference as `not target-matched` in the final status.

Before builder or prefab implementation, create a target-to-canvas mapping table and keep it with the surface notes, manifest, or implementation plan. Required columns:

| Target element | Target bounds/crop | Unity object path | Layer type | Sprite/TMP source | 9-slice/alpha rule | Z-order/children | 16:9 behavior | 20:9 behavior | QA status |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |

Every visible target element must appear in this table before implementation starts. Missing rows are a blocker because they usually become missing icons, wrong labels, merged layers, incorrect button states, or spacing drift later.

## 3D Single-Map Content Rule

The active gameplay art direction is full 3D single-map mobile RTS. For screens that show gameplay, map previews, mission art, unit portraits, squad thumbnails, minimap content, tactical overlays, or battlefield content, use `Design/3D_SingleMap_Gameplay_Direction.md`, `Design/UIUX_MainMenu_Visual_Contract.md`, accepted command-base visual locks, and the prefab catalog under `Assets/Game/Configs/Prefabs` as the content source. Do not regenerate old 2D isometric, macro-tile, or generic low-poly/desert battlefield imagery for new visual-lock targets.

The UI track owns Canvas chrome, controls, states, layout, and screen routing. The 3D gameplay/art track owns operation-map assets, runtime unit/building presentation, camera validation, and production captures behind the HUD. `Screen_MatchOverlay` and later gameplay overlays should be checked over a non-black 3D operation-map capture when available.

## Layer Pack Contract

The layer pack must include separate PNGs for:

- Solid cut-corner backplates and fills.
- Transparent overlay frames/chrome.
- Button/card state backgrounds.
- Icons, badges, checkboxes, chevrons, markers, and resource symbols.
- Content art such as portraits, minimaps, previews, and thumbnails.
- Slider, toggle, dropdown, tab, and segmented-control pieces when used.

Each reusable sprite must have one job. A frame sprite must not contain dynamic content. A button background must not contain an icon or label. A content image must not contain its frame. If a layer contains baked text, icons, markers, or frame noise, it is not ready for Canvas implementation.

## Decomposition First

Before editing prefabs or builder code, classify every visible target element.

Use this table shape:

| Object path | Layer type | Sprite/content source | Alpha rule | Z-order rule | Child layers | QA status |
| --- | --- | --- | --- | --- | --- | --- |

Allowed layer types:

- `transparent overlay frame`
- `solid cut-corner backplate`
- `content image`
- `dynamic icon`
- `TMP text`
- `stateful button/card`
- `divider/accent`
- `control track/fill/handle`
- `decorative rail`

This classification decides import mode, alpha behavior, z-order, and tests. Do not apply one generic frame rule to every object.

## Sprite Preparation Rules

1. Inspect every generated PNG before using it.
2. Remove transparent gutters that cause Unity sizing drift.
3. Confirm outside corners are alpha 0 for cut-corner shapes.
4. Confirm overlay-frame centers are alpha 0 when the frame sits above content.
5. Confirm solid backplate centers remain visible and opaque enough when the backplate sits below content.
6. Confirm icons are transparent standalone sprites with no baked background.
7. Confirm state backgrounds contain no icons, labels, portraits, health bars, badges, or markers.
8. Use 9-sliced sprites for scalable chrome, buttons, bars, cards, and panels.
9. Add atlas labels and sprite atlas membership for generated UI assets.

Alpha checks on source PNGs are useful, but not enough. Opaque rectangular artifacts often only show up in a rendered capture over a non-black background.

## Canvas Construction Rules

Build the Canvas as real hierarchy:

- Root screen/popup object.
- Fixed-edge or proportional layout anchors appropriate for landscape phones.
- Separate `FillBackground` and `FrameChrome` children where the target needs both.
- Separate `Image` children for icons, markers, portraits, minimaps, and thumbnails.
- Separate TMP text using Oxanium.
- Real `Button`, `Toggle`, `Slider`, `Dropdown`, `ScrollRect`, and input components.

Typography:

- Page titles and emphasized CTA labels use `Oxanium-Bold SDF`.
- Normal labels, values, descriptions, and control text use `Oxanium-Light SDF`.
- Text should stay single-line unless the target explicitly has paragraph copy.
- Autosize only to prevent clipping, not to hide poor layout.

Buttons and cards:

- Use the shared animated button state workflow for Normal, Highlighted, Pressed, Selected, and Disabled.
- Treat selected cards, selected command buttons, selected nav tabs, and selected segmented items as real selected button states.
- Do not use one-off color tinting for target-selected controls.

Responsive behavior:

- Validate 16:9 and 20:9.
- Edge HUD elements should remain attached to their intended edge.
- Do not stretch icons or content art.
- Sliced chrome may scale, but its corner shape and line thickness must visually match the target.

## Visual QA Loop

The screen is not accepted until the rendered Unity capture matches the target object by object.

Required loop:

1. Rebuild the prefab through Unity batch mode.
2. Capture the prefab at the target resolution.
3. Capture the prefab at 20:9.
4. Confirm captures are current, nonblank, correct resolution, and show the correct screen.
5. Create a full-screen comparison image: target, rendered capture, and amplified difference overlay.
6. Create focused target-vs-capture comparison crops for every important panel and every user-reported problem path.
7. Compare each crop for:
   - frame thickness
   - corner silhouette
   - fill opacity and color
   - separator placement
   - icon shape, scale, and center
   - text size, weight, alignment, and clipping
   - text/content values versus target
   - reward/objective/difficulty values versus target
   - selected state
   - spacing and padding
   - alpha/corner artifacts
   - merged layers or baked child content
8. Keep iterating while visible differences remain.

Tests are required gates, not visual acceptance. Passing hierarchy, sprite-path, alpha, and navigation tests only proves structure. A capture can still be visually wrong.

## First-Go Self-QA Checklist

Before reporting any screen as done, complete this checklist yourself:

- Target text/content exactly matches or target was intentionally revised first.
- Rewards, objectives, mission names, difficulty labels, counters, and CTA labels match the target.
- Header/logo/title scale and alignment match the target.
- Every major panel has the correct frame, fill, border thickness, and corner silhouette.
- 9-sliced chrome has transparent outside corners and no opaque rectangular artifacts.
- Icons are separate child sprites, centered and scaled like the target.
- Buttons/cards/tabs use the correct selected/normal/pressed state workflow.
- No content art, labels, badges, icons, health bars, or counters are baked into reusable backgrounds.
- No text is clipped, wrapped unexpectedly, or autosized so far away from target scale that it looks mismatched.
- 16:9 and 20:9 captures both preserve intended edge anchoring and avoid stretching content art.
- Full-screen and focused comparison images have been inspected after the latest changes.
- Any visible remaining difference is listed as `not target-matched`; do not ask the user to find obvious differences.

## Regression Tests

Every new or materially revised visual-lock surface should add tests that enforce source mapping for the high-risk sprites:

- modal/frame chrome
- fill/backplate sprites
- button state backgrounds
- major icons
- content images
- repeated cards/rows/panels

These tests should assert that the prefab references the expected layer-pack destination sprites. They do not prove visual match, but they prevent a common failure mode where the builder falls back to older generated sprites or baked target crops.

## Acceptance Criteria

A screen can be called visually locked only when:

- The Unity capture matches the target in full-screen view and focused crops.
- The visible text/content values match the target, or the target has been intentionally revised first.
- Every target element has a matching Unity object or a documented intentional merge.
- No visible UI is a flattened screenshot pretending to be Canvas UI.
- Frames, fills, icons, text, and content images are separated correctly.
- Outside cut-corner pixels are actually transparent.
- Icons and labels are not baked into button/card/panel backgrounds.
- Stateful controls use the shared animated button states.
- Text is readable and unclipped at 16:9 and 20:9.
- Focused tests pass.
- Any remaining differences are explicitly listed as not yet accepted.

Do not use words like `done`, `complete`, `matches`, or `visual locked` unless these checks have passed.

## Common Failure Modes To Avoid

- Using the target mockup as a full-screen background.
- Cropping a whole panel with icons/text/content baked in.
- Generating chunky synthetic bevels when the target has thin chrome.
- Treating every chrome sprite as a transparent-center overlay frame.
- Treating every chrome sprite as a solid backplate.
- Leaving opaque black or target-background pixels outside cut corners.
- Passing Unity tests and stopping without visual comparison.
- Looking only at full-screen screenshots instead of focused object crops.
- Forgetting that selected cards and selected command buttons are button-state controls.
- Letting icons overlap labels or labels overlap count/rank badges.
- Fixing one panel and accidentally changing unrelated panels.

## MatchOverlay Lesson

The `Screen_MatchOverlay` pass proved that having high-quality separated assets is not enough. The Canvas still failed until the builder constants, child rects, z-order, alpha rules, icon centering, title autosizing, and focused comparison crops were adjusted.

For future screens, do not stop after importing the layer pack. The real work is the full loop:

`layer pack -> decomposition -> sprite QA -> Canvas hierarchy -> rebuild -> capture -> focused crop comparison -> fix -> tests -> 16:9/20:9 capture`.

## Short Reference Command

When starting a new screen, use:

```text
Use `Design/UIUX_Target_To_Canvas_Workflow_Guide.md` and the Reusable Prompt - Visual-Lock Canvas Conversion from `Design/UIUX_Mockup_To_Canvas_Conversion_Plan.md` to convert [SCREEN_PREFAB_PATH] against [TARGET_MOCKUP_PATH]. Do not stop until the rendered capture and focused crops match the target, or explicitly list remaining differences as not accepted.
```
