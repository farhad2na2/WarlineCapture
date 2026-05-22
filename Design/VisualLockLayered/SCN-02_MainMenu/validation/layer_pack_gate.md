# SCN-02 Main Menu Layer Pack Gate

Date: 2026-05-22
Status: Blocked / V15C regeneration active.

## Checked

- Target reference exists: `reference/SCN-02_MainMenu_Landscape_Target.png`
- Layer source sheet exists: `generated_one_go/source/SCN-02_MainMenu_LayerSourceSheet_Green.png`
- 21:9 background source exists: `generated_one_go/source/SCN-02_MainMenu_BackgroundArt_21x9_NoUI.png`
- Wide mode thumbnail source exists: `generated_one_go/source/SCN-02_MainMenu_ModeThumbnails_Wide_Source.png`
- Layer manifest exists: `layer_manifest.json`
- Contact sheet exists: `generated_one_go/layers_contact_sheet.png`
- Extracted layers exist in `layers/`, but current pack is not approved for implementation.
- Non-opaque UI layers have transparent pixels after chroma-key extraction.
- Rejected regeneration request exists: `layer_requests/SCN-02_MainMenu_Layer_Regeneration_Request_V15B.md`
- Active regeneration request exists: `layer_requests/SCN-02_MainMenu_Layer_Regeneration_Request_V15C.md`

## Blocker

The current source sheet does not provide a clean target-matching header, logo lockup, or right action panel. V15B also failed because it used a crowded one-sheet approach where mode images and UI assets were cut or touching. Do not solve this by cropping the target reference. V15C must regenerate proper clean layers as separated source groups.

The first V15C frame sheet `generated_v15c/source/SCN-02_Frames_CTA_Commander_Green.png` is rejected because parent backgrounds contain baked child elements: star badge, lock icons, progress/readiness bars, and CTA chevrons. Parent frames must be blank; child icons, bars, badges, chevrons, and state overlays must be separate layers.

## Aspect Notes

- Background art is 1915x821 and should be used with cover/crop behavior for 16:9, 20:9, and 21:9.
- Mode thumbnail art layers are 1632px wide and should sit behind card masks so wider layouts reveal more scene horizontally.
- Header art must be split into logo, resource, command, and right-action backgrounds so the top bar can adapt to wider mobile aspects, but these pieces must come from regenerated layer assets, not reference crops.

## Remaining Gate

Unity Canvas assembly should wait for V15C layer regeneration. Final VisualLock acceptance still requires live TMP labels, runtime-bound values, masks, and 16:9 / 20:9 / 21:9 captures.
