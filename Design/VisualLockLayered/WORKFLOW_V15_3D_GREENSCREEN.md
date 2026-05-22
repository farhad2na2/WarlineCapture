# VisualLockLayered V15 3D Green-Screen Workflow

Date: 2026-05-22

## Purpose

This is the active continuation of the UI-agent VisualLockLayered workflow that was proven on the old `SCN-02_MainMenu` work. Use it for new 3D-direction screen, popup, and prefab packs.

The previous working example lives in:

- `Design/Archive/LegacyVisualLock_2026-05-22/VisualLockLayered/SCN-02_MainMenu/LATEST_WORKFLOW.md`
- `Design/Archive/LegacyVisualLock_2026-05-22/VisualLockLayered/SCN-02_MainMenu/prompts/scn02_main_menu_layers_3840_request.md`
- `Tools/UI/prepare_scn02_standalone_assets.py`
- `Tools/UI/build_scn02_component_plates.py`
- `Tools/UI/validate_scn02_component_layout.py`
- `Assets/Game/Scripts/Editor/WarlineCaptureScn02LayerCanvasBuilder.cs`
- `Assets/Tests/Editor/WarlineCaptureScn02LayerCanvasBuilderTests.cs`

Use those files for process knowledge only. Do not copy old Saga / Quick Custom / 2D-isometric content or old SCN-02 art direction into new targets.

## Active Direction Changes

All new packs must use:

- Full 3D single-map mobile RTS direction.
- Command-base menu visual language.
- Campaign, Operations, and Skirmish as player-facing modes.
- Credits, Supplies, and Command as top-level menu resources unless a specific screen contract says otherwise.
- Prefab-catalog unit/building display names and descriptions from `Assets/Game/Configs/Prefabs`.
- 3D operation-map captures or generated 3D operation art for gameplay previews, minimaps, unit cards, building cards, mission key art, and HUD backdrops.

## Pack Destination

Each surface must be generated into:

```text
Design/VisualLockLayered/<SurfaceId>/
  README.md
  reference/
    <SurfaceId>_Landscape_Target.png
  layers/
  layer_manifest.json
  generated_one_go/
    source/
    layers_contact_sheet.png
  validation/
```

## V15 Workflow

1. Read the surface contract in `Design/VisualLockLayered/README.md` and the relevant design doc.
2. Create a layer request using `prompts/visual_lock_layered_v15_3d_green_background.md`.
3. Ask image generation for:
   - one full-screen target-lock preview,
   - individual layer PNGs,
   - a contact sheet for review,
   - transparent PNGs when possible,
   - solid `#00ff00` background for any layer that cannot be delivered with transparency.
4. Save raw generated images under `generated_one_go/source/`.
5. Convert green-screen layers to transparent PNGs using the chroma-key helper pattern from `Tools/UI/prepare_scn02_standalone_assets.py`.
6. Save final implementation layers under `layers/`.
7. Write `layer_manifest.json` with:
   - source image paths,
   - layer ids,
   - file paths,
   - role,
   - Unity destination,
   - sprite import type,
   - slicing border where needed,
   - alpha rule,
   - live text / runtime binding rule.
8. Build `generated_one_go/layers_contact_sheet.png` with checkerboard previews for transparent layers.
9. Run a layer-pack gate before any Unity work:
   - target reference exists,
   - separated layers exist,
   - manifest exists,
   - contact sheet exists,
   - README exists,
   - important non-opaque layers have real transparent pixels after chroma-key conversion.
10. Copy layers to Unity with a surface-specific `copy_layers_to_unity.py` or a generic manifest copier.
11. Build a real Canvas using frames, icons, art, meters, and live TMP text as separate children.
12. Validate safe rects and child collisions before capture.
13. Capture 16:9 and 20:9 Unity renders.
14. Produce target-vs-capture comparison images.
15. Accept only after the capture is nonblank, aligned, readable, and uses live UI elements instead of one flattened background.

## Chroma-Key Rules

- Use `#00ff00` only as an extraction background, never as visible product art.
- Use chroma-key for frames, icons, buttons, badges, markers, overlays, meter segments, chevrons, and other non-opaque layers.
- Opaque layers are allowed only for full backgrounds and content art that should be rectangular, such as mission key art or card thumbnails.
- After extraction, inspect alpha on every functional layer. The layer must have transparent outside corners/holes when the visual design requires them.
- Do not accept green fringe, checkerboard residue, or baked labels inside reusable frames.

## Frame-First Canvas Rule

The old SCN-02 workflow learned that baked composite plates are not enough. Use frames as parent backgrounds, then place art, icons, badges, meters, and live TMP labels inside safe rects.

Every functional panel should define:

- panel rect,
- safe rect,
- child slots,
- z order,
- 16:9 behavior,
- 20:9 behavior.

Do not fix overlap by shrinking text below mobile readability. Adjust panel safe zones, child lanes, or target composition first.

## Surface Order Recommendation

Start with this order:

1. `SCN-02_MainMenu`
2. `SCN-08_RTSBattleHUD`
3. `SCN-19_Armory`
4. `SCN-07_LoadoutSquadPrep`
5. `SCN-09_BuildDrawerProduction`
6. `SCN-10_UnitCommandWheel`
7. `SCN-05_CampaignMap`
8. `SCN-06_MissionBriefing`
9. `SCN-13_SkirmishSetup`
10. `SCN-14_CommandExchange`

This order rebuilds the main route, battle HUD, unit/building inspection, loadout, production, command controls, and mode entry screens before lower-risk shells.

