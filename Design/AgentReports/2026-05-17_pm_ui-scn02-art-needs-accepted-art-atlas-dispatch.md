# PM Review - SCN-02 UI Handoff Accepted As Art-Blocked, Art/Atlas Dispatched

Date: 2026-05-17
Owner: PM
Status: Art/Atlas active; UI held
Priority: P0

## Decision

UI handoff is accepted as an honest implementation/art-blocker report, not as target-lock visual acceptance.

Reviewed UI reports:

- `Design/AgentReports/2026-05-17_ui_scn02-mainmenu-production-sprite-implementation.md`
- `Design/AgentReports/2026-05-17_ui_scn02-mainmenu-target-lock-art-needs.md`

Reviewed evidence:

- `Design/AgentReports/Captures/SCN-02_MainMenu_ProductionSpriteImplementation_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ProductionSpriteImplementation_20x9.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ProductionSpriteImplementation_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ProductionSpriteImplementation_20x9_vs_Target_Comparison.png`

UI did the right thing by not faking missing visual fidelity with placeholders, target slices, contact sheets, or flattened target overlays. The result is still not target-lock because several accepted manifest sprites do not match the approved target art content.

Current comparison scores remain too high for target-lock:

- 16:9 MSE: `1172.77`
- 20:9 MSE: `1156.73`

## Current Routing

- Art/Atlas owns the next blocking task.
- UI is held on SCN-02 until Art/Atlas delivers revised target-matching assets and PM/user accepts them.
- QA/HCI must not be routed for SCN-02 target-lock acceptance yet.

## Required Art/Atlas Output

Art/Atlas must deliver:

- `Design/AgentReports/2026-05-17_art-atlas_scn02-target-lock-asset-revisions.md`

Scope:

- `Design/VisualLockLayered/SCN-02_MainMenu/`

Target references:

- `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_20x9_Target.png`

Runtime implementation evidence to compare against:

- `Design/AgentReports/Captures/SCN-02_MainMenu_ProductionSpriteImplementation_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_ProductionSpriteImplementation_20x9.png`

## Required Asset Revisions

Revise the following manifest layers so they visually match the approved target, not merely "safe" production placeholders:

| Layer id | Needed correction |
| --- | --- |
| `mode_card_art_saga` | Replace with target-matching illustrated city/armored convoy scene for the Saga Campaign card. |
| `mode_card_art_operation` | Replace with target-matching blue tactical city-grid/hologram scene for the Persistent Operation card. |
| `mode_card_art_quick_custom` | Replace with target-matching mountain/base/helicopter scene for the Quick Custom Game card. |
| `commander_profile_portrait` | Replace with the target-style dark commander silhouette/profile scan treatment, or a PM-approved final portrait that matches that region. |
| `brand_emblem` | Replace with the angular Warline emblem treatment visible in the target masthead. |
| `icon_credits` | Replace with stacked coin icon matching the target top resource strip. |
| `icon_materials` | Replace with stacked crate/materials icon matching the target top resource strip. |
| `icon_command_authority` | Replace with shield/star icon matching the target top resource strip. |
| `designed_unavailable_badge` | Provide target badge treatment with readable two-line `Designed Unavailable` label area and lock plate region matching the target. |
| `left_nav_icon_inbox` | Match target left-nav mail icon silhouette, scale, and neutral styling. |
| `left_nav_icon_store` | Match target cart icon silhouette, scale, and neutral styling. |
| `left_nav_icon_events` | Match target calendar/star icon silhouette, scale, and neutral styling. |
| `left_nav_icon_ranking` | Match target ranking bars icon silhouette, scale, and neutral styling. |
| `left_nav_icon_command_feed` | Match target command-feed antenna icon silhouette, scale, and neutral styling. |

Optional polish if Art/Atlas sees the same mismatch in the target comparison:

- Tighten `deploy_command_chevrons` and `deploy_command_glow_overlay` so the CTA does not read brighter/larger than the target.
- Verify `mode_card_header_emblem_*` icons match target style and scale after the revised card art is installed.

## Package Requirements

Art/Atlas must update:

- revised PNGs under `Design/VisualLockLayered/SCN-02_MainMenu/layers/`
- `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json` if dimensions, slicing, bindings, source provenance, or usage notes change
- SCN-02 contact sheet showing the revised sprites
- SCN-02 README or source notes if implementation guidance changes

Art/Atlas must include in the report:

- changed file list
- imagegen source/provenance
- before/after notes for each revised layer
- confirmation every revised layer file exists and manifest parses
- confirmation no revised final runtime art was created with deterministic/vector/programmer-art methods
- confirmation no target-reference panel crop, target composite, screenshot, comparison image, or contact sheet is used as final runtime art

## Rules

Use imagegen for new/replacement visual assets. Deterministic tooling is allowed only after imagegen source selection for crop extraction, alpha cleanup, resizing, metadata, manifest updates, contact-sheet packaging, inspection, and validation.

Do not use:

- deterministic/programmatic final art
- HTML/CSS/vector/scripted substitutes
- placeholder-looking block art
- full target mockup overlays
- target-reference panel slices
- contact sheets or screenshots as final layer art

Existing `target_slice_*` files in the SCN-02 design folder remain rejected runtime assets and must not be referenced by the manifest. If Art/Atlas cleans the package, it may remove or archive rejected `target_slice_*` files, but it must not use them as final art.

## Next Step After Art

After PM/user accepts the Art/Atlas revision report, UI should run one final SCN-02 placement/import/capture pass:

- apply updated layers to Unity
- rebuild `Screen_MainMenu.prefab`
- capture 16:9 and 20:9
- regenerate target comparisons
- report remaining mismatches region by region
