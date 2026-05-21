# SCN-07_LoadoutSquadPrep Layer Pack

Status: `RouteReadyLayerPackGenerated`

This is the mandatory layered source for `Screen_LoadoutSquadPrep.prefab`. It uses the approved flat target as the visual reference and provides separate implementation PNGs for Unity Canvas construction.

## Contents

- `reference/SCN-07_LoadoutSquadPrep_Landscape_Target.png`
- `layers/*.png`
- `generated_one_go/layers_contact_sheet.png`
- `layer_manifest.json`
- `prompts/high_end_target_and_layers.md`

## Canvas Rules

- Do not bake the full target into the screen.
- Unit thumbnails, card frames, support slot icons, gear art, summary panels, and deploy CTA are separate layers.
- TMP text remains live text using the project Oxanium fonts.
- 9-sliced chrome must preserve transparent chamfer corners.
