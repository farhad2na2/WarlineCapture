# Lane
UI

# Task
PM attention request for SCN-02 Main Menu target-lock asset revisions after UI final import/capture pass.

# Files changed
- `Design/AgentReports/2026-05-17_ui_scn02-mainmenu-pm-art-asset-request.md`

# Contracts touched
- No runtime contracts changed by this report.
- This request depends on the completed UI proof report:
  `Design/AgentReports/2026-05-17_ui_scn02-mainmenu-final-target-lock-pass.md`

# User-visible behavior
No user-visible runtime change in this report. It asks PM to route the remaining visual mismatch to the correct owner.

# Validation run
- Reviewed the final UI pass report and fresh captures:
  - `Design/AgentReports/Captures/SCN-02_MainMenu_FinalTargetLockPass_1672x941.png`
  - `Design/AgentReports/Captures/SCN-02_MainMenu_FinalTargetLockPass_20x9.png`
  - `Design/AgentReports/Captures/SCN-02_MainMenu_FinalTargetLockPass_vs_Target_Comparison.png`
  - `Design/AgentReports/Captures/SCN-02_MainMenu_FinalTargetLockPass_20x9_vs_Target_Comparison.png`
- Compared final captures against:
  - `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png`
  - `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_20x9_Target.png`

# Validation result
- UI can improve placement with another focused iteration, but the current source art will not reach target-lock by placement alone.
- Final comparison scores remain nonmatching:
  - 1672x941 MSE: `1077.03`
  - 20:9 MSE: `1043.91`
- The final UI pass is valid as an import/capture pass, not as visual acceptance.

# PM request
PM should route Art/Atlas for one more SCN-02 target-lock asset revision pass before asking UI to do another final placement pass, unless PM explicitly accepts the current art as non-target-lock.

Required Art/Atlas details:

| Asset group | Current issue | Requested Art/Atlas output |
| --- | --- | --- |
| Saga card art | Current image has convoy/soldiers but does not match the target city composition, camera angle, smoke layout, aircraft presence, or depth. | Replace `mode_card_art_saga` with a target-matching 440x165 source that preserves the target city/convoy/soldier/aircraft composition. |
| Persistent Operation card art | Current blue map is improved but has different perspective, density, node placement, and lighting from the target. | Replace `mode_card_art_operation` with a closer target-style holographic district grid matching the target viewpoint, bright center nodes, and orange threat points. |
| Quick Custom card art | Current base/mountain art is close in theme but differs in base layout, aircraft placement, sky contrast, and scale. | Replace `mode_card_art_quick_custom` with a target-matching mountain forward-base scene, including aircraft silhouettes and wider base composition. |
| Brand emblem/logo | Runtime emblem and logo treatment still differ from the target mark and masthead proportions. | Provide target-matching `brand_emblem` and, if needed, a dedicated logo/masthead art layer or exact emblem variant. |
| Resource icons | Credits/materials/command authority icons are improved but not target-identical in shape, scale, bevel, and lighting. | Provide closer `icon_credits`, `icon_materials`, and `icon_command_authority` layers matching target silhouettes, lighting, and size. |
| Commander scan background | Silhouette is now present, but the target has a more specific scan-panel/background treatment. | Provide revised `commander_profile_portrait` with target-like scan grid, silhouette framing, and background detail at accepted manifest dimensions. |
| Designed-unavailable badge and lock | Current badge/lock is cramped and visually noisy at runtime scale. | Provide a cleaner target-scale `designed_unavailable_badge`, ideally with lock plate readable when placed in left-nav rows. |
| Left-nav icons | Current icons are improved but do not match target weight/contrast at runtime scale. | Revise `left_nav_icon_inbox`, `left_nav_icon_store`, `left_nav_icon_events`, `left_nav_icon_ranking`, and `left_nav_icon_command_feed` for target-size readability. |
| Deploy chevrons/glow | Current CTA chevrons/glow still read too large/bright compared with target. | Provide subtler `deploy_command_chevrons` and `deploy_command_glow_overlay` matching target scale and amber intensity. |

# UI-owned follow-up after Art/Atlas
After PM accepts any new Art/Atlas package, UI should run a narrow placement pass for:

- 20:9 command-feed panel to lower-left target position.
- Top bar and settings rects.
- Left-nav badge/lock placement and TMP sizing.
- Commander profile panel/portrait/lower label placement.
- Mode card title/icon/footer/body TMP placement.
- Persistent Operation warning rows and meters.
- Deploy CTA scale, tone, chevron placement, and label position.

# Known gaps
- UI cannot create or substitute target-matching art assets under the current rules.
- UI should not use target crops, mockup overlays, screenshots, composites, contact sheets, or placeholders to close these gaps.
- If PM rejects another Art/Atlas pass and asks UI to continue only with current assets, UI can improve layout but should not claim target-lock completion.

# Cross-lane impacts
- PM: route Art/Atlas or explicitly accept current art as a non-target-lock compromise.
- Art/Atlas: owns source-layer fidelity for the table above.
- UI: owns placement/TMP/rect polish after PM accepts revised art.
- QA/HCI: should wait until PM decides whether the next step is Art/Atlas revision or UI-only polish.

# Next recommended task
PM should dispatch Art/Atlas for the listed SCN-02 asset revisions, then return the accepted package to UI for a final placement/capture pass.
