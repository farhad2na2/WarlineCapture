# PM Review - SCN-02 Final UI Pass Rejected For Target-Lock, Art/Atlas Rerouted

Date: 2026-05-17
Owner: PM
Status: Art/Atlas active; UI held
Priority: P0

## Decision

UI final pass is accepted as valid implementation evidence, but rejected as SCN-02 target-lock visual acceptance.

Reviewed UI reports:

- `Design/AgentReports/2026-05-17_ui_scn02-mainmenu-final-target-lock-pass.md`
- `Design/AgentReports/2026-05-17_ui_scn02-mainmenu-pm-art-asset-request.md`

Reviewed evidence:

- `Design/AgentReports/Captures/SCN-02_MainMenu_FinalTargetLockPass_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_FinalTargetLockPass_20x9.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_FinalTargetLockPass_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_FinalTargetLockPass_20x9_vs_Target_Comparison.png`

Target references:

- `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_20x9_Target.png`

UI correctly imported the revised Art with `--apply --force`, rebuilt the prefab, produced captures/comparisons, and did not use target slices, mockup overlays, screenshots, contact sheets, or placeholders as runtime UI. That part is accepted.

The result is still not target-lock:

- 16:9 MSE: `1077.03`
- 20:9 MSE: `1043.91`

PM agrees with UI that placement can improve, but the current source art still blocks a true target-lock pass. Routing Art/Atlas first prevents UI from repeatedly tuning around nonmatching source content.

## Current Routing

- Art/Atlas owns the next blocking task.
- UI is held until the next Art/Atlas revision is delivered and PM/user accepts it.
- QA/HCI must not review SCN-02 target-lock yet.

## Required Art/Atlas Output

Art/Atlas must deliver:

- `Design/AgentReports/2026-05-17_art-atlas_scn02-target-lock-asset-revisions-v2.md`

Scope:

- `Design/VisualLockLayered/SCN-02_MainMenu/`

Reference and runtime evidence:

- compare against the two SCN-02 target references listed above
- inspect the final UI capture and comparison images listed above

## Required Asset Revisions

Revise the current SCN-02 layers toward the approved target, not a thematically similar approximation:

| Asset group | Required correction |
| --- | --- |
| `mode_card_art_saga` | Replace with a closer 440x165 target-matching city/convoy/soldier/aircraft composition. The current art has the right theme but not the target camera angle, depth, smoke layout, or aircraft/city balance. |
| `mode_card_art_operation` | Replace with a closer 440x165 blue holographic district grid matching target perspective, density, bright center nodes, and orange threat points. |
| `mode_card_art_quick_custom` | Replace with a closer 440x165 mountain forward-base scene matching target base layout, aircraft silhouettes, sky contrast, and wider composition. |
| `brand_emblem` | Provide a closer angular Warline target mark. If needed, add a dedicated masthead/logo layer rather than expecting UI text/emblem composition to approximate it. |
| `icon_credits` | Match target stacked coin silhouette, bevel, scale, and warm lighting. |
| `icon_materials` | Match target stacked blue crate/materials silhouette, bevel, scale, and lighting. |
| `icon_command_authority` | Match target gold shield/star silhouette, bevel, scale, and lighting. |
| `commander_profile_portrait` | Preserve the silhouette but add closer target scan-grid/background detail and framing at the accepted dimensions. |
| `designed_unavailable_badge` | Provide a cleaner target-scale badge/lock treatment that remains readable in left-nav rows. Current badge is cramped and noisy at runtime scale. |
| `left_nav_icon_inbox` | Match target icon weight, contrast, and scale at runtime row size. |
| `left_nav_icon_store` | Match target icon weight, contrast, and scale at runtime row size. |
| `left_nav_icon_events` | Match target icon weight, contrast, and scale at runtime row size. |
| `left_nav_icon_ranking` | Match target icon weight, contrast, and scale at runtime row size. |
| `left_nav_icon_command_feed` | Match target icon weight, contrast, and scale at runtime row size. |
| `deploy_command_chevrons` | Provide subtler chevrons matching target scale and spacing. |
| `deploy_command_glow_overlay` | Provide subtler amber glow matching target intensity; current runtime CTA remains too bright. |

Art/Atlas should not modify UI layout, Unity prefabs, runtime code, or `Assets/` imports. This is a source layer package task only.

## Required Process

- Use imagegen for every replacement visual.
- Use deterministic tooling only after imagegen source selection for crop extraction, alpha cleanup, resizing, metadata, manifest updates, contact-sheet packaging, inspection, and validation.
- Do not use target-reference crops, target slices, full mockup overlays, screenshots, comparison images, contact sheets, vector substitutes, HTML/CSS/scripted art, or deterministic final art.
- Keep or update existing manifest ids unless a new layer id is truly required.
- Update `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json` with provenance and any changed usage/sizing notes.
- Update the SCN-02 contact sheet so PM/user can review the revised assets before UI imports them.

## Required Report Evidence

The Art/Atlas v2 report must include:

- changed file list
- imagegen source/provenance
- before/after notes for each revised layer
- confirmation manifest parses
- confirmation every manifest-declared file exists
- confirmation no `target_slice_*` file is referenced by the manifest
- confirmation no final runtime art was produced from deterministic/vector/programmatic methods
- confirmation no target crop/composite/screenshot/contact sheet was used as final layer art

## Next Step After Art

After PM/user accepts the Art/Atlas v2 revision, UI should run one more focused placement pass:

- force-copy revised layers into Unity
- rebuild `Screen_MainMenu.prefab`
- place 20:9 command feed in the lower-left target position
- tighten top bar/settings rects
- tighten commander profile frame/portrait/lower label
- tighten left-nav badge/lock and TMP sizing
- tighten mode card title/icon/body/footer placement
- tighten Persistent Operation rows/meters
- reduce deploy CTA scale/tone/chevron placement to target
- capture 16:9 and 20:9 and regenerate comparisons
