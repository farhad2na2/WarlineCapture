# SCN-02 Main Menu VisualLockLayered V15

Date: 2026-05-22
Status: V15C regeneration active. V15B rejected.

## Direction

This pack follows the active 3D WarlineCapture direction: a field commander preparing attack operations from a forward command base overlooking a large Middle Eastern-inspired 3D operation area. It uses the Demo / Demo2 gameplay scene language: modular roads, concrete pads, barriers, base compounds, APCs, damaged roads, crashed aircraft debris, hangar staging, town blocks, and distant mountains.

## Files

- `reference/SCN-02_MainMenu_Landscape_Target.png` is the approved target-lock visual reference.
- `layer_requests/SCN-02_MainMenu_Layer_Regeneration_Request_V15C.md` is the active request for proper clean implementation layers.
- `layer_requests/SCN-02_MainMenu_Layer_Regeneration_Request_V15B.md` is rejected and must not be used for Unity implementation.
- `generated_one_go/source/SCN-02_MainMenu_LayerSourceSheet_Green.png` is the green-screen UI sprite source sheet.
- `generated_one_go/source/SCN-02_MainMenu_BackgroundArt_21x9_NoUI.png` is the 21:9-safe opaque background art source.
- `generated_one_go/source/SCN-02_MainMenu_ModeThumbnails_Wide_Source.png` is the wide mode-card art source.
- `layers/` contains extracted Unity-ready layer PNGs.
- `layer_manifest.json` lists every layer, source crop, role, alpha rule, Unity destination, and live-text rule.
- `generated_one_go/layers_contact_sheet.png` is the layer QA contact sheet.
- `validation/header_split_preview.png` shows the currently extracted header pieces, but this preview is not final until V15B is generated.

## Workflow Rule

Do not cut, crop, mask, or reuse pixels from `reference/SCN-02_MainMenu_Landscape_Target.png` as implementation layers. The reference is visual guidance only. Proper layers must come from a clean generated green-screen source sheet or from approved source art assets.

## Responsive Layout Rules

- Use `scn02_background_art.png` as a cover/crop background. Do not stretch it. It is 21:9-safe and should crop inward for 20:9 and 16:9.
- The header is split into separate pieces. The contact sheet may make the top row look like one continuous bar; use `validation/header_split_preview.png` when checking the split:
  - `scn02_header_logo_panel_bg.png`
  - `scn02_header_resource_panel_bg.png`
  - `scn02_header_command_panel_bg.png`
  - `scn02_header_right_actions_bg.png`
- Use `scn02_brand_logo_lockup.png` as a separate image over the logo panel.
- The V15B right header/action background must include the inbox/settings button wells and must anchor right so those actions remain stable on 20:9 and 21:9.
- Resource header panels may stretch or tile between fixed left and right header anchors.
- Mode card art layers are intentionally wide:
  - `scn02_campaign_thumbnail_art.png`
  - `scn02_operations_thumbnail_art.png`
  - `scn02_skirmish_thumbnail_art.png`
- Place each mode thumbnail behind a card mask/window. At wider aspect ratios, reveal more horizontal art instead of stretching.

## Live Text

All route labels, resource values, progress values, commander name/level, locked labels, and CTA labels must be TMP/runtime-bound. Frames and buttons in this pack intentionally contain no baked readable text.

## Current Notes

This is a first generated layer pass and is not final. V15C must regenerate the implementation assets as separated source groups: clean header sheet, isolated logo, separate frame/icon sheet, separate commander portrait, and separate wide mode thumbnails.
