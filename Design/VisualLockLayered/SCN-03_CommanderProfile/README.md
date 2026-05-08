# SCN-03 CommanderProfile Layer Pack

Status: `RouteShellLayerPackGenerated`

This pack is the layer-first source for the route-ready `Screen_CommanderProfile.prefab` shell. It uses the accepted flat target as reference and shared WarlineCapture route-shell chrome as the initial implementation layer set until backing services are ready.

## Contents

- `reference/SCN-03_CommanderProfile_Landscape_Target.png`
- `layers/*.png`
- `generated_one_go/layers_contact_sheet.png`
- `layer_manifest.json`

## Rules

- Do not use the flattened target as a Canvas background.
- Keep buttons, panels, icons, and TMP text as separate Unity layers.
- This is a designed-unavailable shell, not final service-backed UI.
