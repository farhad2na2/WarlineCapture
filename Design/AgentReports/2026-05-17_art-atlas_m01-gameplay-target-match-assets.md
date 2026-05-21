# Art/Atlas M01 Gameplay Target-Match Assets

Date: 2026-05-17
Owner: Art/Atlas
Status: ready for PM/user review
Priority: P0

## Lane

Art/Atlas

## Task

Deliver the M01 gameplay target-match source asset package requested by PM after Gameplay v5 proved runtime flow/soldier visibility but remained blocked on Art-owned background and unit source fidelity.

Scope was limited to:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/`
- this handoff report under `Design/AgentReports/`

No Unity runtime code, prefabs, scenes, UI implementation, `Assets/` imports, source docs, or other lane task files were modified.

## Handoff Assessment

- `Design/AgentReports/2026-05-17_pm_m01-gameplay-art-asset-resume-dispatch.md`: accepted as the active P0 PM routing.
- `Design/AgentReports/2026-05-15_gameplay_m01-01-target-match-proof-v5.md`: accepted as useful Gameplay proof plus valid Art/Atlas blocker evidence.
- `Design/AgentReports/2026-05-15_pm_gameplay-v4-soldiers-visible-target-rejected.md`: accepted as historical rejection context.
- `Design/AgentReports/2026-05-14_gameplay_m01-corrected-sample-asset-audit.md`: accepted as prior asset audit context.
- `Design/AgentReports/2026-05-14_art-atlas_m01-step-by-step-gameplay-mockups.md`: accepted as the current step-by-step visual-lock reference package.

## Result

Added a focused M01 target-match v3 package for PM/user review and downstream Gameplay binding:

- clean no-HUD/no-unit M01 tactical start plate
- runtime-ready POT padded tactical plate candidate
- player rifle squad idle facings atlas with baked per-frame contact shadows
- enemy patrol idle facings atlas at the same projection scale with restrained red accents
- marker/readability assets for M01-02 selection and M01-01/M01-02 enemy readability
- manifest metadata, source provenance, pivots, anchors, and binding checklist
- focused contact sheet

Primary manifest:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_target_match_asset_manifest_v3.json`

Existing package manifest pointer updated:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_ai_production_asset_manifest.json`

Contact sheet:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_target_match_assets_v3_contact.png`

## Imagegen Provenance

Built-in imagegen source root:

- `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618`

Selected generated files:

- Clean tactical plate: `ig_061caec3064fc95a016a09c2e27db88198ba5d404182569065.png`
- Player rifle squad idle facings: `ig_061caec3064fc95a016a09c42d93808198b1e49c943c5c33df.png`
- Enemy patrol idle facings: `ig_061caec3064fc95a016a09c468adf48198b6e2b817a57126cb.png`
- Marker/readability sheet: `ig_061caec3064fc95a016a09c3c454888198b4c54098f2ff9288.png`

Project source copies:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/TargetMatchV3/m01_tactical_start_clean_plate_v3_imagegen_source.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/TargetMatchV3/player_rifle_squad_idle_facings_v3_chromakey.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/TargetMatchV3/player_rifle_squad_idle_facings_v3_alpha.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/TargetMatchV3/enemy_patrol_idle_facings_v3_chromakey.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/TargetMatchV3/enemy_patrol_idle_facings_v3_alpha.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/TargetMatchV3/m01_marker_readability_v3_chromakey.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/TargetMatchV3/m01_marker_readability_v3_alpha.png`

Deterministic tooling was used only after imagegen source selection for source copy, chroma-key alpha removal, crop extraction, resizing into existing sprite-cell contracts, POT padding, residual chroma cleanup, contact-sheet packaging, metadata, inspection, and validation.

No final runtime art was created from target mockup crops, pasted flattened screenshots, comparison images, contact sheets, deterministic vector art, HTML/CSS/scripted rendering, manual shape overlays, or programmer placeholders.

## Final Asset Dimensions

| Asset | Dimensions |
|---|---:|
| `TacticalMaps/m01_tactical_start_clean_plate_v3_source_1920x1080.png` | 1920x1080 |
| `TacticalMaps/m01_tactical_start_clean_plate_v3_pot_2048x2048.png` | 2048x2048 |
| `Units/PlayerRifleSquad/TargetMatchV3/player_rifle_squad_idle_facings_atlas_v3.png` | 1024x256 |
| `Units/EnemyPatrol/TargetMatchV3/enemy_patrol_idle_facings_atlas_v3.png` | 1024x256 |
| player facing frames `*_idle_v3.png` | 256x256 each |
| enemy facing frames `*_idle_v3.png` | 256x256 each |
| `Markers/TargetMatchV3/selection_ring_v3.png` | 256x256 |
| `Markers/TargetMatchV3/selected_squad_status_v3.png` | 256x128 |
| `Markers/TargetMatchV3/enemy_readability_ring_v3.png` | 256x256 |
| `Markers/TargetMatchV3/enemy_health_bar_v3.png` | 256x96 |
| `ContactSheets/m01_target_match_assets_v3_contact.png` | 720x872 |

## Gameplay Binding Checklist

- Import clean plate source and POT candidate only after PM/user approval.
- Bind the plate through `IsoMapId: iso.ch01.district_edge_01` and `camera.default_start`; do not introduce a new mission/map id.
- Use `player_rifle_squad_idle_facings_atlas_v3.png` for M01-01/M01-02 idle proof frames `NE/SE/SW/NW`, 256x256 cells, pivot `[0.5, 0.16]`.
- Use `enemy_patrol_idle_facings_atlas_v3.png` at the same cell size, pivot, and projection scale as the player; no enemy shrink rule.
- M01-01: hide player selection rings; show enemy foot-readability rings and segmented enemy health bars only.
- M01-02: show four cyan selected rings at existing `selectedMarkerLayersM0102` anchors, plus selected squad shield/status and enemy readability/health overlays.
- Preserve the existing ECS/runtime presentation path. Do not use transform hacks, fake shadow quads, camera distortion, target crops, or pasted mockup pixels to claim final match.

## Files Changed

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/README.md`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_ai_production_asset_manifest.json`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_target_match_asset_manifest_v3.json`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_target_match_assets_v3_contact.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v3_source_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v3_pot_2048x2048.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Sources/TargetMatchV3/*`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/TargetMatchV3/*`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/TargetMatchV3/*`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Markers/TargetMatchV3/*`
- `Design/AgentReports/2026-05-17_art-atlas_m01-gameplay-target-match-assets.md`

## Validation Run

- Read `Design/AgentTasks/art-atlas_heartbeat.md`.
- Read `Design/AgentTasks/art-atlas_current.md`.
- Checked latest `Design/AgentReports/` handoffs and accepted the M01 PM resume dispatch.
- Read required Gameplay v5 and VisualLock references.
- Inspected the current M01 AIProductionAssets package and target references.
- Generated the v3 visual assets with built-in imagegen.
- Copied selected imagegen sources into the project review mirror.
- Removed chroma-key backgrounds for unit and marker sheets.
- Packaged unit facings into 256x256 frame cells and 1024x256 facing atlases.
- Packaged marker/readability sprites with transparent alpha.
- Packaged clean tactical plate source and centered POT candidate.
- Updated README and manifests.
- Built focused v3 contact sheet.
- Parsed `m01_target_match_asset_manifest_v3.json` with `python3 -m json.tool`: passed.
- Parsed `m01_ai_production_asset_manifest.json` with `python3 -m json.tool`: passed.
- Verified every manifest-declared `Design/...` review file exists: `missing 0`.
- Scanned v3 transparent unit/marker PNGs for opaque chroma-green pixels: `M01_V3_GREEN_REMAINING 0`.

## Validation Result

Ready for PM/user review.

- Required M01 v3 Art/Atlas package delivered: yes
- Final visual assets are imagegen-sourced: yes
- JSON manifests parse: yes
- Every manifest-declared review file exists: yes
- PNG dimensions recorded: yes
- Contact sheet provided: yes
- Target mockup crops/composites/screenshots/contact sheets used as final runtime art: no
- Deterministic/vector/programmatic final art used: no
- Unity runtime code changed: no
- Unity prefabs/scenes changed: no
- `Assets/` imports changed: no

## Remaining Blockers

- PM/user must approve or reject the v3 Art/Atlas package.
- After approval, Gameplay owns importing/binding these assets through the existing ECS/runtime presentation path and regenerating M01-01/M01-02 target-match proof.
- UI/HCI HUD chrome polish remains separate from this battlefield/source-asset package.
