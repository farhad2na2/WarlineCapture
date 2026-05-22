# SCN-04 Settings / Accessibility Generated V01

Date: 2026-05-22

This folder contains the generated source sheets used to prepare the SCN-04 Settings / Accessibility implementation layer pack.

## Source Sheets

- `source/SCN-04_Background_21x9_NoUI.png`: wide no-UI command-base settings background.
- `source/SCN-04_Frames_Green.png`: clean header, panel, tab, row, slider, toggle, dropdown, segmented control, chip, and button frames on green.
- `source/SCN-04_Icons_Green.png`: separate logo, resource, category, control, status, and action icons on green.

## Extraction

Run:

```bash
python3 Tools/UI/prepare_scn04_settings_v01_layers.py
```

The extractor writes:

- `generated_v01/layers/`
- `generated_v01/layer_manifest.json`
- `generated_v01/validation/SCN-04_SettingsAccessibility_layers_contact_sheet.png`

It also promotes the active handoff copies to:

- `layers/`
- `layer_manifest.json`
- `validation/SCN-04_SettingsAccessibility_layers_contact_sheet.png`

The target-lock reference is not used as an extraction source.
