# SCN-11 OperationDashboard Layer Pack

Status: `RouteShellLayerPackGenerated`

This pack is the layer-first source for the route-ready `Screen_OperationDashboard.prefab` shell. It uses the accepted flat target as reference and shared WarlineCapture route-shell chrome as the initial implementation layer set until backing services are ready.

## Contents

- `reference/SCN-11_OperationDashboard_Landscape_Target.png`
- `layers/*.png`
- `generated_one_go/layers_contact_sheet.png`
- `layer_manifest.json`

## Rules

- Do not use the flattened target as a Canvas background.
- Keep buttons, panels, icons, and TMP text as separate Unity layers.
- This is a designed-unavailable shell, not final service-backed UI.
