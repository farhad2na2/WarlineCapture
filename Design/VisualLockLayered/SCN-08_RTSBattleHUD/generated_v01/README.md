# SCN-08 RTS Battle HUD Generated V01

Date: 2026-05-22

This folder contains the generated source sheets used to prepare the SCN-08 match HUD implementation layer pack.

## Source Sheets

- `source/SCN-08_Battlefield_21x9_NoUI.png`: wide no-UI 3D battlefield art.
- `source/SCN-08_Frames_Green.png`: clean HUD frames, panels, command buttons, tray frames, and minimap frame on green.
- `source/SCN-08_Icons_Green.png`: separate command, resource, objective, system, status, and minimap icons on green.
- `source/SCN-08_WorldMarkers_Green.png`: selection rings, path line, objective/hostile/civilian markers, scan ping, minimap dots, and invalid markers on green.
- `source/SCN-08_SquadPortraits_Green.png`: separate roster portrait art for Rifle Squad, Fast APC, Recon Drone, and Bomb Suit.
- `source/SCN-08_MinimapContent.png`: square minimap terrain/content layer.

## Extraction

Run:

```bash
python3 Tools/UI/prepare_scn08_match_v01_layers.py
```

The extractor writes:

- `generated_v01/layers/`
- `generated_v01/layer_manifest.json`
- `generated_v01/validation/SCN-08_RTSBattleHUD_layers_contact_sheet.png`

It also promotes the active handoff copies to:

- `layers/`
- `layer_manifest.json`
- `validation/SCN-08_RTSBattleHUD_layers_contact_sheet.png`

The target-lock reference is not used as an extraction source.
