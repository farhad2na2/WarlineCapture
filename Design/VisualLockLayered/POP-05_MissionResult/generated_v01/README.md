# POP-05 Mission Result Generated V01

Date: 2026-05-22

This folder contains the generated source sheets used to prepare the POP-05 Mission Result implementation layer pack.

## Source Sheets

- `source/POP-05_Background_21x9_NoUI.png`: wide no-UI debrief background art.
- `source/POP-05_MissionSnapshot.png`: mission summary snapshot art.
- `source/POP-05_Frames_Green.png`: clean result frames, panels, stat tiles, route chip, progress frame, and action buttons on green.
- `source/POP-05_Icons_Green.png`: separate result, objective, reward, consequence, stat, progress, and route icons on green.

## Extraction

Run:

```bash
python3 Tools/UI/prepare_pop05_mission_result_v01_layers.py
```

The extractor writes:

- `generated_v01/layers/`
- `generated_v01/layer_manifest.json`
- `generated_v01/validation/POP-05_MissionResult_layers_contact_sheet.png`

It also promotes the active handoff copies to:

- `layers/`
- `layer_manifest.json`
- `validation/POP-05_MissionResult_layers_contact_sheet.png`

The target-lock reference is not used as an extraction source.
