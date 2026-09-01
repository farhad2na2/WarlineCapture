# V3 Shared Layered Art And Small-Atlas Strategy

Status: proposed implementation baseline for the V3 layered-art request pass.

This strategy converts the V3 target locks into one shared, sharp Unity UI art
system. It is intentionally asset-centric rather than screen-centric: a visual
primitive is requested once, stored once, packed once, and referenced by every
screen that needs it.

The target mockups remain composition references. They are never runtime
backgrounds and are never cropped into production sprites.

## Audit Baseline

- The repository's canonical inventory currently resolves to **46 final target
  PNGs**. This includes alternate states and related flow variants in addition
  to the target-lock screen count used in the request, so this strategy covers
  every canonical final rather than stopping at a filename count of 37.
- All 46 targets are `1672x941`, which gives one consistent 16:9 visual
  comparison baseline.
- The targets consistently use hard 90-degree rectangles, near-black and
  graphite surfaces, light text, and a small semantic accent palette.
- The repeated visual grammar is stronger than the screen-to-screen variation:
  header rails, footer rails, panels, chips, buttons, tabs, cards, progress
  controls, icon slots, portrait masks, and state overlays recur everywhere.
- `Assets/Game/Art/UI/Generated` currently contains 414 PNGs.
- A SHA-256 audit finds **19 exact-duplicate groups and 25 redundant PNGs** in
  that folder. Duplicates include button frames, resource chips, logo lockups,
  progress pieces, portraits, panel frames, and full background plates stored
  under different screen names.
- Existing Sprite Atlases cover brand and portrait groups. Generated V3 chrome
  has no equivalent shared atlas set yet.

The duplicate count is only the exact-byte floor. Visually equivalent assets
with different pixels or baked colors require a perceptual and semantic audit
before migration.

## Non-Negotiable Decisions

1. **One canonical asset ID owns one source PNG and one Unity GUID.** Screens
   reference it; they do not copy it into their own folder.
2. **Do not request 46 screen-local layer packs.** Request shared component
   families first, then request only proven exceptions.
3. **No baked live text, numbers, progress, selection, cooldown, prices, or
   state colors.** These remain live TMP/UI Toolkit/Canvas data.
4. **Neutral chrome is tintable.** Color variants of the same silhouette are
   runtime palette states, not new PNGs.
5. **A state is composition unless its silhouette changes.** Selected,
   disabled, warning, success, valid, invalid, hover, pressed, victory, and
   defeat normally reuse the same base plus tint and a shared overlay.
6. **Large scene plates and comic illustrations are standalone textures.** Do
   not force full-screen or large narrative art into a UI atlas.
7. **Atlas ownership follows reuse and load lifetime, not screen number.** No
   `SCN-xx.spriteatlas` or `POP-xx.spriteatlas` for shared chrome.
8. **All raster art sources still follow the accepted green-key layered-art
   workflow.** Runtime tinting, masking, slicing, and composition are allowed;
   cropping from flattened targets is not.
9. **Gradients are shared procedural layers, not screen-local bitmaps.** Use the
   canonical V3 vertical-gradient graphic with theme colors when the target
   silhouette is rectangular; request raster gradient art only when the lock
   proves a materially different texture or non-linear highlight.
10. **Render chrome exactly once.** A panel or control may have one 9-slice
    border layer and one inset fill layer. Never stack a panel frame, focus
    frame, and button frame to simulate weight; this creates inconsistent
    borders when the component is resized.

## Normalized V3 Visual Language

### Geometry

- 90-degree corners for panels, cards, buttons, tabs, chips, and rails.
- Straight, continuous one- or two-pixel highlight lines at final reference
  scale; avoid soft bevels, ornamental corners, and fuzzy outer glows.
- Hard offset shadows, separated from the face where practical so one shadow
  can support many components.
- Use an 8-pixel layout grid at `1672x941`. Critical strokes and sprite
  placement land on whole pixels at the validation resolution.
- Reuse a small set of 9-slice border thicknesses. Do not invent a border per
  screen.
- All components that share a chrome family use the same
  `pixelsPerUnitMultiplier`. V3 Settings uses `2.0`; its builder validator
  rejects a button with a different border scale.
- Header/footer gradients must be inset far enough to preserve the modal's
  outer chrome. Gradient fills never cover the owning component's border.

### Palette

Use four neutral roles and six semantic accents. The exact color values should
be stored once as Unity theme tokens after the first calibration pass.

| Role | Intent |
|---|---|
| `Canvas` | Near-black screen shell and dimmer |
| `Surface` | Primary graphite panel |
| `SurfaceRaised` | Raised/inset contrast surface |
| `LineText` | Dividers, labels, primary text, and muted text via alpha |
| `Cyan` | Information, ARIA, scan, navigation, and link state |
| `Blue` | Neutral primary action and menu route |
| `Green` | Confirmed, valid, ready, complete, and friendly |
| `Amber` | Attention, reward, build, economy, and pending |
| `OrangeRed` | Attack, danger, invalid, destructive, and failed |
| `Violet` | Rare identity/upgrade category accent only |

Do not ask image generation for separate blue, green, amber, and red versions
of the same frame. Request a neutral face and a white/neutral highlight overlay,
then apply these tokens in Unity.

## Canonical Shared Asset Set

The first production request pass should create this minimal component grammar.
Names are canonical IDs, not suggestions for screen-local aliases.

### Core chrome

| Canonical asset ID | Production form | Reuse rule |
|---|---|---|
| `ui_core_solid_face` | Tintable simple sprite | All flat panel/button interiors |
| `V3GradientGraphic` | Procedural tintable fill | Selected tabs, toggles, action buttons, and rails without atlas duplication |
| `ui_core_shadow_9s` | Neutral 9-slice | Shared hard offset shadow |
| `ui_core_panel_9s` | Neutral 9-slice | Default panel and drawer surface |
| `ui_core_panel_inset_9s` | Neutral 9-slice | Nested data and list areas |
| `ui_core_modal_9s` | Neutral 9-slice | Settings, pause, alerts, detail popups |
| `ui_core_header_9s` | Neutral 9-slice | Shared non-gameplay header rail |
| `ui_core_footer_9s` | Neutral 9-slice | Shared action/status footer rail |
| `ui_core_chip_9s` | Neutral 9-slice | Resources, status, tags, small counters |
| `ui_core_button_9s` | Neutral 9-slice | Rectangular and square controls |
| `ui_core_focus_overlay_9s` | Tintable line overlay | Hover, selected, pressed, and focus |
| `ui_core_selected_rail_3s` | Tintable 3-slice | Tab/nav/card selection edge |
| `ui_core_card_9s` | Neutral 9-slice | Menu, catalog, event, unit, reward cards |
| `ui_core_card_focus_9s` | Tintable line overlay | Selected/featured card state |
| `ui_core_progress_track_9s` | Neutral 9-slice | Progress, health, XP, loading, readiness |
| `ui_core_progress_fill_3s` | Tintable 3-slice | All horizontal fills |
| `ui_core_slider_track_3s` | Neutral 3-slice | Settings sliders |
| `ui_core_slider_thumb` | Tintable simple sprite | Settings sliders |
| `ui_core_toggle_track_9s` | Neutral 9-slice | On/off controls |
| `ui_core_toggle_thumb` | Tintable simple sprite | On/off controls |
| `ui_core_scroll_track_3s` | Neutral 3-slice | Lists, feeds, inbox, settings |
| `ui_core_scroll_thumb_9s` | Neutral 9-slice | Lists, feeds, inbox, settings |
| `ui_core_divider` | Tintable simple sprite | Horizontal or rotated vertical rule |

The base button must scale to square and wide rectangles. Do not request
separate close-button, footer-button, tab-button, or command-button backgrounds
unless a target-match capture proves the shared silhouette cannot serve them.

### Core icon grammar

Request monochrome, tintable icons with one stroke weight and one visual grid.
Icons never include a button frame, label, badge, or colored state.

- Shell: back, forward, close, menu, settings, mail, search, filter, sort,
  refresh, pause, play, skip, info, help, lock, unlock, check, cancel, timer.
- Resources: credits, command, materials, oil, fuel, parts, supplies, XP.
- Tactical actions: select, move, attack, hold, stop, scan, support, build,
  repair, patrol, raid, drone, evacuate, deploy, return, destroy, camera.
- Categories: units, soldiers, vehicles, aircraft, buildings, upgrades,
  operations, events, ranking, profile, rewards.
- Status: warning, target, route, objective, star, shield, health, confidence,
  signal, victory, defeat.

The same semantic icon is reused in every context. For example, `attack` is one
sprite used by the HUD bar, command wheel, district actions, and threat/raid
surfaces.

### Portrait and content masters

- One transparent ARIA master, reused through masks and crops across HUD,
  onboarding, operations, command feed, and takeover surfaces.
- One canonical transparent master per commander, soldier squad, vehicle,
  aircraft, building, reward object, and store object.
- Use Unity masks and layout crops for thumbnail, card, bust, and detail views.
- A second `hero` rendering is allowed only when the pose, lighting, or required
  display resolution is materially different; it must have a distinct semantic
  ID such as `aria_portrait` versus `aria_hero`, not a copied file with a screen
  prefix.
- Portrait frames, selection borders, rarity colors, ownership, locks, health,
  and progress are shared chrome/data, never baked into the portrait.

### Feature-specific exceptions

These are legitimate non-core requests because their silhouettes are unique:

- Command wheel base, center hub, and one tintable wedge highlight.
- Map marker rings, route endpoint, area highlight, and selection reticle.
- Tutorial focus mask edge and pointer treatment.
- ARIA waveform/data decoration, requested once and reused.
- Victory, defeat, warning, and reward emblems; their surrounding shell is core.
- Brand logo lockup.
- Unique story/comic illustrations and large background scene plates.

Feature exceptions must still reuse core panels, buttons, icons, and palette.

## Small Atlas Plan

Use explicit atlas groups and a maximum page size of `1024x1024`. If a group
overflows, create a numbered page with the same policy rather than raising the
page size. Do not combine assets with different compression, filtering, mipmap,
or load-lifetime requirements.

| Atlas | Max page | Contents | Load scope |
|---|---:|---|---|
| `UI_Brand_01` | 512 | Logo and brand marks | Boot/common |
| `UI_CoreChrome_01` | 1024 | Core 9-slice/3-slice chrome | Common |
| `UI_CoreIcons_01..N` | 1024 | Shell, resource, category, status icons | Common |
| `UI_HUDChrome_01` | 1024 | Command wheel, HUD-only frames/overlays | Match |
| `UI_HUDIcons_01` | 1024 | Tactical actions and map markers | Match |
| `UI_Onboarding_01` | 1024 | Tutorial/onboarding-only decoration | First launch |
| `UI_Operations_01` | 1024 | Operations-only emblems/overlays | Operations |
| `UI_Results_01` | 1024 | Result/reward emblems | Results/progression |
| `UI_Portraits_Characters_##` | 1024 | Canonical character masters or derivatives | Feature/route |
| `UI_Portraits_Units_##` | 1024 | Unit/vehicle/aircraft masters or derivatives | Feature/route |
| `UI_Portraits_Buildings_##` | 1024 | Building masters or derivatives | Feature/route |

Rules:

- Padding: 8 pixels for high-contrast UI, with edge extrusion enabled.
- Rotation: off. It complicates 9-slice validation and source comparison.
- Tight packing: off for sliced chrome; optional only for non-sliced icons after
  visual validation.
- Mipmaps: off for UI chrome, icons, and portraits used at UI scale.
- Filter: bilinear for antialiased high-resolution sources; avoid fractional
  layout scale that blurs one-pixel edges.
- Compression: begin with uncompressed RGBA32 for common chrome/icons. Accept
  ASTC 4x4 on mobile only after side-by-side device captures show no ringing,
  block contamination, or alpha-edge damage.
- Keep the always-loaded common atlas set below 8 MiB uncompressed where
  practical. A `1024x1024` RGBA32 page is 4 MiB; a `512x512` page is 1 MiB.
- Large backgrounds, maps, comics, and hero illustrations remain standalone
  textures with their own platform settings and load lifetime.

## Request Order

### Pass 0: registry and reuse gate

Before any generation call, create or update the asset registry with:

`asset_id`, `role`, `silhouette`, `state_strategy`, `tintable`, `source_master`,
`runtime_png`, `unity_guid`, `atlas`, `max_display_px`, `slice_lbrt`,
`used_by`, `sha256`, `perceptual_hash`, `status`.

For every proposed request:

1. Search by semantic role and silhouette.
2. Check exact hash and perceptual similarity against existing generated art.
3. Reuse a compatible approved V3 master.
4. Request new art only if there is a documented silhouette, material, or
   resolution gap.

### Pass 1: sharpness calibration

Request these four assets individually first:

1. `ui_core_panel_9s`
2. `ui_core_button_9s`
3. `ui_core_focus_overlay_9s`
4. one representative monochrome tactical icon

Clean, slice, import, atlas, and render them at `1672x941`, `1920x1080`,
`1280x720`, and `2400x1080`. Freeze stroke weight, border size, shadow offset,
palette tokens, PPU, and import settings only after these captures are sharp.
Do not request the rest of the pack before this gate passes.

### Pass 2: core chrome

Request the remaining core chrome in small homogeneous sheets of no more than
four components, or individually when a 9-slice corner is visually important.
Every item must be isolated and fully visible with generous green separation.

### Pass 3: icon grammar

Request icons in consistent sheets of 8-12 icons by semantic family. Use the
approved calibration icon as the style anchor. Slice to separate files, clamp
alpha, and reject any icon whose stroke weight or bounding box differs from the
grammar.

### Pass 4: portrait and content masters

Request one subject at a time or one clearly related subject family. Do not ask
for portrait frames, labels, selection colors, or card backgrounds in these
images.

### Pass 5: feature exceptions

Generate the command wheel, map overlays, tutorial focus treatment, ARIA data
decoration, and result emblems only after core composition proves which shapes
cannot be built from the shared pack.

### Pass 6: unique large art

Request unique no-UI scene plates, comics, and hero illustrations last. They do
not block validation of the shared component system and must not be atlas-packed
with UI chrome.

## Request Template: Shared Tintable Chrome

```text
Use case: background-extraction
Asset type: canonical shared Unity UI sprite source for Warline Capture V3.
Canonical asset ID: <asset_id>.
Reference images: <two or three representative accepted V3 target paths>.
Primary request: Generate one reusable <role> matching the shared V3 visual
language. This is a production component, not a screen crop.

Geometry and style:
- hard 90-degree rectangular silhouette
- crisp vector-like antialiased edges and continuous straight strokes
- neutral graphite/white tintable construction; no baked semantic hue
- no rounded corners, ornamental bevels, fuzzy glow, texture noise, or gradients
- no text, number, icon, badge, progress value, state color, or screen content
- shadow omitted unless this asset ID is the dedicated shared shadow

Extraction requirements:
- perfectly flat solid #00ff00 background
- complete, uncut, centered asset with generous green padding
- no green inside the artwork
- no extra components, mockup, watermark, or scene background
- designed for the declared 9-slice or 3-slice border plan
```

## Request Template: Monochrome Icon Sheet

```text
Use case: background-extraction
Asset type: canonical Warline Capture V3 Unity icon sheet.
Canonical asset IDs: <ordered IDs>.
Reference: <approved calibration icon and representative V3 targets>.
Primary request: Generate exactly <count> isolated monochrome tactical icons in
the listed order, using one stroke weight, one perspective, and one bounding-box
grid.

Requirements:
- white/light neutral tintable icon art only
- sharp simple silhouettes readable at 32-96 display pixels
- no button frames, circles behind icons, labels, numbers, colored states,
  shadows, glow, or repeated icons
- equal cell sizes and generous #00ff00 separation
- perfectly flat solid #00ff00 background
- every icon complete, centered, uncut, and visually distinct
```

## Alternate States That Must Not Create Duplicate Chrome

| Target states | Composition strategy |
|---|---|
| Threat alert / route preview | Same modal and HUD shell; add route overlay and live data |
| Build placement valid / invalid metadata | Same placement shell; tint grid, border, icon, and message |
| Build drawer enabled / disabled | Same drawer/card chrome; change interactivity, tint, lock, and copy |
| Command wheel / targeting | Same wheel base; rotate/show one shared wedge highlight and target reticle |
| Match HUD / passengers / tactical feedback | Same HUD; add passenger drawer or feedback banner module |
| Campaign chapter / mission select | Same shell/header/footer; swap body modules and live selection |
| Victory / defeat | Same result shell; swap emblem, palette state, backdrop, and live outcome data |
| Settings / pause | Same modal foundation, button grammar, dimmer, close control, and footer actions |
| Reward / upgrade / intel detail | Same detail-modal grammar; unique subject/emblem only |
| First-launch four steps | Same step header/footer/progress grammar; unique body content only |

## Coverage Across All Canonical Targets

| Family | Canonical target states | Shared pack usage | Unique requests |
|---|---:|---|---|
| First launch | 4 | Brand, core chrome/icons, progress, portraits | Comic/body illustrations only |
| Shell | 5 | Brand, header/footer, nav, cards, modal, controls | Loading/menu plates and subject masters |
| Campaign | 4 | Header/footer, tabs, cards, icons, progress | Campaign map/mission illustrations |
| Match | 4 | HUD chrome/icons, cards, ARIA master | Battlefield/live camera; passenger content masters |
| Match tools | 9 | Core modal/card/button plus HUD pack | Wheel, map overlays, tutorial focus |
| Operations | 9 | Header/footer, panels, chips, cards, core icons | Map/scene plates and a few emblems |
| Progression | 6 | Header/footer, cards, progress, result/detail shell | Catalog subjects and result/reward emblems |
| Connected routes | 4 | Header/nav, lists, cards, scroll, filters | Message/event thumbnails only |
| Other modes | 1 | Header/footer, tabs, cards, core icons | Skirmish map plate if not live-rendered |

Total: 46 target states. Shared components should cover nearly all chrome; the
unique request count should be driven by content subjects and large scene art,
not by the number of screens.

## Sharpness And Quality Gate

An asset is accepted only when all of the following pass:

- Source is at least 2x its maximum intended display size; icons intended for
  32-96 pixels should retain a clean 256-pixel production master.
- Edges are crisp after green removal and despill. No green pixels or green
  alpha fringe remain.
- Transparent bounds are clamped after every cleanup pass.
- Straight edges remain straight and continuous at final display size.
- No rounded/chamfered corner was introduced where the target uses a square
  corner.
- 9-slice corners and stroke thickness do not stretch at minimum, typical, and
  maximum component sizes.
- Sprite renders sharply at all four validation resolutions without relying on
  fractional RectTransform or USS scaling.
- Atlas-packed output is visually identical to the standalone source at normal
  and 200% crop inspection.
- Color comes from shared palette tokens and preserves text/icon contrast.
- Exact and perceptual duplicate checks pass before the file is added.
- Registry has one canonical path, GUID, atlas, slice border, and complete
  `used_by` coverage.

## Migration Plan For Existing Generated Art

1. Build the registry from current references and Unity GUID usage.
2. Mark exact duplicate groups and select one canonical owner for each group.
3. Do not delete or move existing files during the request phase. First update
   all prefab, UXML/USS, and serialized references to canonical GUIDs.
4. Add the new shared atlases and validate packed sprites in representative
   menu, HUD, modal, list, and card screens.
5. Migrate one family at a time: shell, HUD, modal, connected/data, progression.
6. Remove redundant files only after reference search, build validation, and
   visual comparison prove that no serialized reference remains.
7. Keep legacy V1/V2 art outside the active V3 registry and atlases.

## Definition Of Done

- Every canonical target state maps to shared asset IDs plus explicitly named
  unique content art.
- No screen-local copy exists for a shared asset.
- Every new request passed the reuse gate before generation.
- Shared chrome and icons live in small, explicit atlases no larger than 1024.
- Large art remains standalone and is not loaded with unrelated UI.
- The common V3 palette and state rules are applied in Unity, not baked into
  duplicate bitmaps.
- Representative screens match the target locks at 16:9 and 20:9 with sharp,
  stable edges and no atlas artifacts.
