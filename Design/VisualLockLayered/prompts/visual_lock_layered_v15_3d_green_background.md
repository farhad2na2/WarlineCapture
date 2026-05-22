# VisualLockLayered V15 3D Green-Background Prompt Template

Use this prompt for the UI agent when generating a new implementation-ready layer pack.

Replace bracketed fields before use.

```text
Create a WarlineCapture implementation-ready VisualLockLayered pack for:

Surface id: [SURFACE_ID]
Surface name: [SURFACE_NAME]
Target resolution: 1672x941 primary landscape, with layout safe for 1920x1080 and 20:9 mobile landscape.
Destination folder: Design/VisualLockLayered/[SURFACE_ID]/

Active game direction:
- Full 3D single-map mobile RTS.
- Middle Eastern-inspired town / forward command base / live operation-map presentation.
- Campaign, Operations, and Skirmish are the player-facing modes.
- Planning, briefing, minimap, deployment, threat, and battle views are UI/camera states over one 3D operation map.
- Use unit/building display names, descriptions, roles, and unlock states from Assets/Game/Configs/Prefabs where the surface shows roster, build, loadout, ability, or Armory content.

Visual style:
- AAA mobile command-base UI.
- Dark black/green military panels.
- Weathered metal frames.
- Olive selected states.
- Gold CTA/action accents.
- Muted blue command-resource accents.
- Restrained off-white live text.
- Oxanium-like typography, but all real text must remain live TMP in Unity unless explicitly listed as decorative.
- No 2D isometric tactical-map art.
- No old Saga / Quick Custom labels.
- No flattened full-screen UI background as the final Unity source.

Required delivery:
1. Full-screen target-lock preview:
   - reference/[SURFACE_ID]_Landscape_Target.png
2. Separate layer PNGs for every reusable Unity element:
   - layers/[layer_id].png
3. Contact sheet:
   - generated_one_go/layers_contact_sheet.png
4. Source images:
   - generated_one_go/source/[source_name].png
5. Manifest-ready layer list with ids, roles, alpha rules, intended Unity destination, and live text/runtime data notes.

Green-screen / transparency requirement:
- Prefer real transparent PNGs for all non-opaque layers.
- If transparent PNG output is not possible, place the layer on a perfectly flat #00ff00 background so it can be chroma-key extracted.
- The #00ff00 background must be clean, uniform, and not appear inside the actual art.
- Do not use green glows, green UI accents, green text, or green pixels near layer edges on chroma-key layers.
- Opaque PNGs are allowed only for full-screen background art and rectangular content images.

Layer separation rules:
- Frames contain no text, no icons, no counters, and no baked dynamic content.
- Button backgrounds contain no labels or icons.
- Icons are separate transparent sprites.
- Resource counters use separate frame, icon, label, and value.
- Cards use separate frame, content art, header icon, footer icon, state overlay, labels, and CTA.
- Meters use separate frame and fill/segment layers.
- Modal/popup panels use separate dim/scrim, panel frame, title icon, body icon, CTA frame, and close/back icon.
- Gameplay/HUD content uses separate 3D operation-map capture/background, markers, minimap frame, minimap content, command markers, selection rings, objective rows, and threat indicators.

Live text / data binding:
- These strings must be live TMP or runtime-bound, not baked:
  [LIVE_TEXT_LIST]
- These values must be runtime-bound:
  [RUNTIME_DATA_LIST]

Surface-specific content:
[SURFACE_SPECIFIC_CONTENT]

Required layers:
[REQUIRED_LAYER_LIST]

Acceptance criteria:
- The full-screen target reads as a premium AAA mobile RTS screen.
- The composition is safe at 16:9 and 20:9.
- All non-opaque layers are extractable with alpha or clean #00ff00 chroma key.
- No text overlaps or unreadable mobile-scale labels.
- No old 2D/isometric/split strategic-tactical visual language.
- No old Saga / Quick Custom player-facing terminology.
- The pack can be converted into Unity Canvas using separate images, TMP, buttons, toggles, sliders, tabs, and runtime-bound data.
```

