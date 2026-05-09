# M01 AI Production Asset Manifest
Status: needs PM/user review
Source lock: `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_TacticalMap_Target.png`
Runtime manifest JSON: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_ai_production_asset_manifest.json`

## Runtime Asset Families
- Strategic maps: 1 regenerated larger city-like strategic/background map after PM rejected the closed-compound direction
- Tactical maps: 3
- Markers: 7
- Player rifle squad static state frames: 24
- Enemy patrol static state frames: 24
- Player rifle squad animation v2 frames: 112
- Enemy patrol animation v2 frames: 112
- Building/prop states: 12

## Strategic Review Overlay
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Strategic/m01_isometric_strategic_background_placement_overlay.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_strategic_placement_overlay_contact.png`

## Review Contact Sheets
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_buildings_contact.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_maps_contact.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_markers_contact.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_unit_atlases_contact.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_strategic_placement_overlay_contact.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_soldier_animation_contact_v2.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_soldier_animation_sources_v2_contact.png`

## Runtime Rules
- Tactical plates are clean ground: no baked units, vehicles, markers, UI, labels, or annotations.
- Strategic runtime background is clean: no text, units, markers, or finished gameplay buildings baked into reserved placement zones.
- Strategic runtime background must preserve open city-block continuity and must not be treated as a closed compound/base.
- Tactical POT textures are padded to 2048x1024 without stretching.
- Player and enemy soldier atlases are separate 4-facing x 6-state sheets.
- Marker and entity sprites are transparent PNGs generated from chroma-key AI source sheets.
- All assets remain `needs_pm_user_review` until approved.

## Soldier Animation Fix
- V1 was rejected because repeated or near-identical poses were visible, especially in run sequences.
- Current review target: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest_v2.json`
- Review mirror: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_soldier_animation_manifest_v2.json`
- Review contact sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_soldier_animation_contact_v2.png`
- Player/enemy animation atlases are separate v2 sheets and contain 112 frames per faction: four facings x idle/run/aim/fire/damaged/death frame requirements.
- Adjacent-frame validation found zero duplicate adjacent frame images.
- Strategic map is approved and unchanged.
