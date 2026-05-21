# WarlineCapture Mockup-to-Canvas Conversion Plan

## Goal

Convert the generated/mockup UI into a real Unity Canvas UI made from separate panels, sprites, icons, text, and interactive controls. The mockups are visual targets, not full-screen backgrounds.

This plan replaces the temporary full-background visual-lock approach for production UI.

## Core Principle

Never ship a screen as one flat background image with invisible buttons.

Each screen must be decomposed into:

- responsive layout containers
- sliced panel/frame sprites
- separate background art where appropriate
- separate icon sprites
- real `Button`, `Toggle`, `Slider`, `Dropdown`, and `ScrollRect` controls
- real TMP text using Oxanium
- normal/hover/pressed/disabled visual states
- mobile-safe hit targets

This conversion method is now the default for every remaining UI screen. Work should proceed screen by screen: target, real Canvas, route/runtime behavior, capture comparison, optimization, and tests.

For the operational step-by-step workflow, use `Design/WarlineCapture_UIUX_Target_To_Canvas_Workflow_Guide.md`. That guide records the accepted layered target-to-canvas process from the Main Menu, Settings, Quick Custom, Splash, and corrected MatchOverlay passes.

The active battlefield/menu direction is now full 3D single-map mobile RTS with command-base menu presentation. UI targets remain under `Design/VisualLock` or `Design/VisualLockLayered`; battlefield/art-production targets should reference the current 3D operation-map direction.

## 3D Single-Map Compatibility Rule

Older UI source specs and some historical VisualLock notes mention Synty, low-poly, 2D isometric, or macro-tile battlefield assets. Treat those as historical source references only. For any new or regenerated UI target that shows gameplay, map previews, mission key art, unit portraits, squad thumbnails, minimap content, tactical overlays, or battlefield content, use the 3D single-map direction in `Design/WarlineCapture_3D_SingleMap_Gameplay_Direction.md` and config-backed roster data from `Assets/Game/Configs/Prefabs`.

UI chrome should follow the command-base material language: dark black/green military panels, weathered metal frames, olive selected states, gold CTA/action accents, muted blue command-resource accents, restrained off-white text, Oxanium typography, and separated reusable Canvas layers.

## Visual-Lock Target Creation Rule

For WarlineCapture production UI work, a new `Design/VisualLock/.../*_Landscape_Target.png` must be a high-quality generated landscape target in the accepted WarlineCapture AAA mobile RTS HUD style unless the task explicitly says to make an exact source extraction.

Do not create new VisualLock targets by simply cropping, stretching, padding, or upscaling archived source spec JPGs. The original source JPGs now live under `Design/Archive/LegacyUI_2026-05-21/WarlineCapture_UIUX_Codex_Package/warlinecapture_uiux_spec_assets/` and are content/layout references only. The correct target-creation workflow is:

1. Read the source spec and identify required labels, panels, controls, icons, hierarchy, and gameplay meaning.
2. Write a generation prompt that creates a new landscape `1672 x 941` target in the accepted WarlineCapture style: command-base military panels, olive/black/gold chrome, soft AAA compositing shadows, readable Oxanium-like typography, and 3D operation-map/key art where gameplay context is relevant.
3. Use the source spec only as a reference for intent and composition, not as pixels to promote into the target.
4. Save the generated target under `Design/VisualLock/<ID_Name>/<ID_Name>_Landscape_Target.png`.
5. Write `<ID_Name>_CleanLandscape_Notes.md` with the source reference, canonical target path, implementation notes, and the exact generation prompt.
6. Only use the source-promotion/upscale approach for an explicitly requested archival/exact-reference target, and label that notes file as an exact source promotion rather than a generated target.

## Canonical Visual-Lock Target Inventory

Use these paths as the current canonical visual targets for production Canvas conversion:

| Surface | Canonical target |
| --- | --- |
| SCN-01 Splash / Loading | `Design/VisualLock/SCN-01_SplashLoading/SCN-01_SplashLoading_Landscape_Target.png` |
| SCN-02 Main Menu / Mode Select | `Design/VisualLockLayered/SCN-02B_MainMenuAlt/reference/MainMenuAlt_CommandTarget_Source_1672x941.png` |
| SCN-03 Commander Profile | `Design/VisualLock/SCN-03_CommanderProfile/SCN-03_CommanderProfile_Landscape_Target.png` |
| SCN-04 Settings / Accessibility | `Design/VisualLock/SCN-04_SettingsAccessibility/SCN-04_SettingsAccessibility_Landscape_Target.png` |
| SCN-05 Campaign Map | `Design/VisualLock/SCN-05_SagaMap/SCN-05_SagaMap_Landscape_Target.png` |
| SCN-06 Mission Briefing | `Design/VisualLock/SCN-06_MissionBriefing/SCN-06_MissionBriefing_Landscape_Target.png` |
| SCN-07 Loadout / Squad Prep | `Design/VisualLock/SCN-07_LoadoutSquadPrep/SCN-07_LoadoutSquadPrep_Landscape_Target.png` |
| SCN-08 RTS Battle HUD | `Design/VisualLock/SCN-08_RTSBattleHUD/SCN-08_RTSBattleHUD_Landscape_Target.png` |
| SCN-09 Build Drawer / Production | `Design/VisualLock/SCN-09_BuildDrawerProduction/SCN-09_BuildDrawerProduction_Landscape_Target.png` |
| SCN-10 Unit Command / Command Wheel | `Design/VisualLock/SCN-10_UnitCommandWheel/SCN-10_UnitCommandWheel_Landscape_Target.png` |
| SCN-11 Operations Dashboard | `Design/VisualLock/SCN-11_OperationDashboard/SCN-11_OperationDashboard_Landscape_Target.png` |
| SCN-12 District Detail / Actions | `Design/VisualLock/SCN-12_DistrictDetailActions/SCN-12_DistrictDetailActions_Landscape_Target.png` |
| SCN-13 Skirmish Setup | `Design/VisualLock/SCN-13_QuickCustomGameSetup/SCN-13_QuickCustomGameSetup_Landscape_Target.png` |
| SCN-14 Store / Command Exchange | `Design/Monetization/Images/SCN-14_Store_CommandExchange_Target.png` |
| SCN-15 Inbox | `Design/VisualLock/SCN-15_Inbox/SCN-15_Inbox_Landscape_Target.png` |
| SCN-16 Events | `Design/VisualLock/SCN-16_Events/SCN-16_Events_Landscape_Target.png` |
| SCN-17 Ranking | `Design/VisualLock/SCN-17_Ranking/SCN-17_Ranking_Landscape_Target.png` |
| SCN-18 Command Feed | `Design/VisualLock/SCN-18_CommandFeed/SCN-18_CommandFeed_Landscape_Target.png` |
| SCN-19 Armory | `Design/VisualLock/SCN-19_Armory/SCN-19_Armory_Landscape_Target.png` |
| POP-01 Threat Alert | `Design/VisualLock/POP-01_ThreatAlert/POP-01_ThreatAlert_Landscape_Target.png` |
| POP-02 Confirm Raid | `Design/VisualLock/POP-02_ConfirmRaid/POP-02_ConfirmRaid_Landscape_Target.png` |
| POP-03 Build Placement | `Design/VisualLock/POP-03_BuildPlacement/POP-03_BuildPlacement_Landscape_Target.png` |
| POP-04 Reward / Unlock | `Design/VisualLock/POP-04_RewardUnlock/POP-04_RewardUnlock_Landscape_Target.png` |
| POP-05 Mission Result | `Design/VisualLock/POP-05_MissionResult/POP-05_MissionResult_Landscape_Target.png` |
| POP-06 End of Day Report | `Design/VisualLock/POP-06_EndOfDayReport/POP-06_EndOfDayReport_Landscape_Target.png` |
| POP-07 Pause / Options | `Design/VisualLock/POP-07_PauseOptions/POP-07_PauseOptions_Landscape_Target.png` |
| POP-08 Intel Reveal | `Design/VisualLock/POP-08_IntelReveal/POP-08_IntelReveal_Landscape_Target.png` |
| POP-09 Ability / Upgrade Detail | `Design/VisualLock/POP-09_AbilityUpgradeDetail/POP-09_AbilityUpgradeDetail_Landscape_Target.png` |
| PREFAB-01 Objective Tracker | `Design/VisualLock/PREFAB-01_ObjectiveTracker/PREFAB-01_ObjectiveTracker_Landscape_Target.png` |
| PREFAB-02 Squad Tray | `Design/VisualLock/PREFAB-02_SquadTray/PREFAB-02_SquadTray_Landscape_Target.png` |
| PREFAB-03 Build Drawer | `Design/VisualLock/PREFAB-03_BuildDrawer/PREFAB-03_BuildDrawer_Landscape_Target.png` |

SCN-14 is indexed here for conversion planning, but ownership remains with the monetization docs because its catalog, purchase states, and reward grants are governed by `Design/Monetization/WarlineCapture_Monetization_Strategy.md`, `Design/Monetization/WarlineCapture_Monetization_Store_Catalog.md`, and `Design/Monetization/WarlineCapture_Monetization_Visual_Targets.md`.

SCN-15 through SCN-18 are newly added route surfaces from `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`. Their target paths are reserved here, but their VisualLock and VisualLockLayered packs may not exist yet. Do not implement these screens from placeholders or toasts. Generate the landscape target, separated layer assets, manifest, and designed-unavailable empty state first. SCN-19 Armory and POP-09 Ability / Upgrade Detail already have final high-end layered packs and should use those packs as the gate before Unity prefab work.

## 3D Operation-Map Target Refresh - 2026-05-21

The updated gameplay design removes the former split between separate strategic and tactical maps. Use `Design/WarlineCapture_3D_SingleMap_Gameplay_Direction.md`, `Design/WarlineCapture_UIUX_MainMenu_Visual_Contract.md`, and `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md` before continuing Phase 6 or M01 UI work.

New or refreshed state targets created for this change:

| Surface | Target |
| --- | --- |
| SCN-08 M01 Tactical Feedback | `Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png` |
| SCN-09 M01 Disabled Build Drawer | `Design/VisualLock/SCN-09_BuildDrawer_M01DisabledState/SCN-09_BuildDrawer_M01DisabledState_Landscape_Target.png` |
| SCN-10 Command Wheel Targeting | `Design/VisualLock/SCN-10_UnitCommandWheel_TargetingState/SCN-10_UnitCommandWheel_TargetingState_Landscape_Target.png` |
| POP-01 Threat Route Preview | `Design/VisualLock/POP-01_ThreatAlert_RoutePreviewState/POP-01_ThreatAlert_RoutePreviewState_Landscape_Target.png` |
| POP-03 Build Placement Metadata Validity | `Design/VisualLock/POP-03_BuildPlacement_MetadataValidityState/POP-03_BuildPlacement_MetadataValidityState_Landscape_Target.png` |
| POP-05 Mission Result M01 Contract | `Design/VisualLock/POP-05_MissionResult_M01ContractState/POP-05_MissionResult_M01ContractState_Landscape_Target.png` |
| PREFAB-04 Assistant Button | `Design/VisualLock/PREFAB-04_AssistantButton/PREFAB-04_AssistantButton_Landscape_Target.png` |
| PREFAB-05 Assistant Panel | `Design/VisualLock/PREFAB-05_AssistantPanel/PREFAB-05_AssistantPanel_Landscape_Target.png` |
| PREFAB-06 Tutorial Card | `Design/VisualLock/PREFAB-06_TutorialCard/PREFAB-06_TutorialCard_Landscape_Target.png` |
| PREFAB-07 Tutorial Highlight | `Design/VisualLock/PREFAB-07_TutorialHighlight/PREFAB-07_TutorialHighlight_Landscape_Target.png` |
| POP-10 Assistant Takeover | `Design/VisualLock/POP-10_AssistantTakeover/POP-10_AssistantTakeover_Landscape_Target.png` |
| POP-11 Commander Identity | `Design/VisualLock/POP-11_CommanderIdentity/POP-11_CommanderIdentity_Landscape_Target.png` |

These targets are not implementation layer packs yet. The layer-pack gate still applies. Before Canvas conversion, create matching `Design/VisualLockLayered/<SurfaceId>/` folders with separated layers, `layer_manifest.json`, contact sheet, and README.

Strategic map rule:

- `SCN-05`, `SCN-06`, threat previews, and minimap context can use strategic / preview images.
- `SCN-08`, command markers, build placement, ARIA tactical highlights, and M01 validation must use tactical / close-up map context and runtime overlays.
- Never bake units, markers, minimap viewport rectangles, build footprints, objective markers, or ARIA highlights into strategic or tactical map art.

## Reusable Prompt - Visual-Lock Canvas Conversion

Use this prompt when asking Codex to convert any target mockup into a production Unity Canvas screen. The intent is to prevent rough placeholder implementations, baked screenshots, stretched artwork, opaque rectangular crops, mismatched borders, and stopping before the rendered prefab actually matches the target.

```text
Convert `[SCREEN_PREFAB_PATH]` to a production Unity Canvas that visually matches the target mockup at:

`[TARGET_MOCKUP_PATH]`

Do this like the accepted production screens, not like a rough placeholder.

Non-negotiable requirements:

0. Hard stop before implementation: verify `Design/VisualLockLayered/<SurfaceId>/reference`, `layers`, `layer_manifest.json`, `generated_one_go/layers_contact_sheet.png`, and `README.md`. If any are missing, do not edit Unity prefabs, builder code, or generated Unity assets. Create/repair the layer pack first. If that cannot be done, report `blocked on layer-pack gate`.
0a. Hard stop before implementation: create a target-to-canvas mapping table for every visible target element. Required columns: target element, target bounds/crop, Unity object path, layer type, sprite/TMP source, 9-slice or alpha rule, z-order/children, 16:9 behavior, 20:9 behavior, and QA status. Do not implement until every visible target element is mapped.
0b. The target mockup is the content contract as well as the visual contract. Text, reward amounts, objective names, mission names, difficulty labels, icons, selected states, image subjects, and CTA labels must match the target unless the target/layer pack is deliberately updated first. Do not silently substitute canonical/runtime placeholders and call the result target-matched.
1. Build real layered Canvas UI, not a baked screenshot.
2. Do not use the full mockup or large baked panel screenshots as fake UI.
3. Every visible element must be split into reusable sprites and objects: panels, frames, tabs, buttons, cards, bars, counters, controls, icon plates, decorative chrome, icons, text, and content art.
4. Use 9-sliced sprites for frames, buttons, bars, cards, and chrome wherever scaling is needed.
5. Do not stretch artwork. Preserve corners, borders, icon proportions, and line thickness across 16:9, 20:9, and similar landscape phone ratios.
6. Border, frame, bar, and chrome sprites must preserve transparency outside the visible shape. Corners and cutouts must have real alpha, not opaque black or opaque target-background pixels, so the game render or screen background shows through cleanly.
7. Never use opaque rectangular crops for UI chrome that sits over gameplay or changing backgrounds. If a target crop includes background pixels outside the frame, remove them with alpha masking before import.
8. Every HUD panel, bar, card, tray, button, portrait plate, and minimap must separate its fill/content layer from its border/frame/chrome layer unless the target visibly uses a single flat unframed image. Use a transparent 9-sliced overlay frame only when the target frame is actually an overlay; if the target has a solid cut-corner backplate, generate a solid backplate with transparent outside edges and place dynamic content/icons/text above it.
9. Frame/chrome/backplate transparent corners must be proven, not assumed. Inspect the generated sprite and the rendered Unity capture over a non-black or checkerboard/gameplay-colored background. Pixels outside the visible target silhouette must be alpha 0. Pixels in frame center cutouts must be alpha 0 only for true overlay frames; solid target backplates must keep the interior alpha/opacity visible.
10. Do not approximate corner geometry. The generated corner cut, bevel, border line, and chrome thickness must match the target silhouette and line thickness within 1-2 rendered pixels at target resolution. If the target has small thin corners, do not generate larger chunky corners.
11. If the target already has a visible chrome silhouette, do not replace it with a generic rectangle, octagon, or reusable synthetic bevel. Trace/crop/mask the target silhouette, then prove the rendered corner crop matches it. Generic generated geometry is only allowed when no target silhouette exists.
12. If a target-derived chrome crop still contains terrain/background noise after masking, do not keep it. Reconstruct the same silhouette as a clean transparent raster frame/fill using the measured target polygon, then compare the focused crop again.
13. All icons must be separate Image objects, not baked into panel backgrounds.
14. Icon sprites must be extracted as clean transparent reusable sprites. Do not leave icon pixels inside card, row, portrait, panel, or button background crops; this causes duplicated or inaccurate child icons.
15. If a target-derived background crop contains an icon, badge, checkbox, chevron, marker, or state indicator, remove/mask it from the background and recreate it as a separate transparent sprite child.
16. Button backgrounds must contain only the button frame/fill/state chrome. Never bake the button icon or label into the button background; render icons and labels as separate children for every button state.
17. Content images and frames must be separate layers. For maps, portraits, cards, previews, resource bars, thumbnails, objective panels, threat panels, trays, and minimaps, create a content/fill image layer plus the correct target backplate/frame/chrome layer; do not merge dynamic content into borders/backplates or bake borders into the content image.
18. Recognize grouped/tab-like buttons from the target. If buttons behave or look like a tab group, segmented command rail, navigation rail, mode selector, or stateful command set, use the shared animated button state setup with Normal, Highlighted, Pressed, Selected, and Disabled states.
19. The visually selected tab/button in the mockup must be configured as Selected using the shared Animator/Animation transition system, not only by manual tinting or a one-off selected sprite.
20. Every Button component must be reviewed as a stateful control, not only obvious tab bars. Squad cards, command buttons, top icon buttons, navigation buttons, mode cards, and segmented items must use the shared animated state system when they have normal/selected/pressed visual states in the target.
21. A visually selected card/button in the target is selected even if it is not named "tab". For example, a selected squad card must be authored as Selected with the same button-state workflow as selected command/nav buttons.
22. Decorative rails, backplates, and chrome strips are not exempt from frame/fill splitting. Objects such as command rails, resource rails, side rails, card rails, top buttons, and footer rails must use transparent frame/chrome overlays plus separate fill/content where the target has a border or cut-corner shape.
23. For stateful cards/buttons, target-derived 9-sliced state chrome may combine that control's fill plus frame only when the crop contains no dynamic text, icons, portraits, health bars, thumbnails, or markers. Mask those dynamic regions out of the crop and recreate them as children. Do not use frame-only synthetic outlines when the target card/button relies on integrated chrome body shading.
24. Icon placement is a visual-lock requirement. Every icon child must match the target icon shape, scale, and center point within 1-2 rendered pixels in a focused crop. Passing an asset-path or alpha test is not enough.
25. All text must be separate TMP text, aligned like the mockup, centered where the mockup centers it, and sized to avoid clipping.
26. Use the same typography rules as the accepted screens: Oxanium Light for normal labels, Oxanium Bold only for titles and emphasized CTA text.
27. Mask or remove any baked text/icons/content from target-derived panel crops before using them as reusable backgrounds.
28. Do not create chunky synthetic borders, oversized bevels, wrong corner cuts, generic placeholder icons, or rounded slider-style controls unless the mockup uses them.
29. Keep interactive elements as Buttons/Toggles/Images with proper hierarchy and raycast settings.
30. Optimize generated assets for sprite atlases and reuse them across repeated UI elements.
31. Passing Unity tests is not visual acceptance. Tests only prove structure and behavior; the rendered capture must still match the target mockup object by object before the work is complete.
31a. Add or update source-mapping tests for high-risk prefab images. Important frames, fills, button backgrounds, content images, and icons must reference the expected layer-pack destination sprites, not older generated assets or target crops.
32. Do not mark the task complete because the screen is improved, broadly similar, or panel-level close. Completion requires checking each visible child object against the target: frame, fill, icon, label, selected state, spacing, alpha, and layering.
33. Before finalizing, review the actual Unity hierarchy against the target decomposition. If a target element exists visually, it must have a matching Unity object or explicitly documented reason why it is intentionally merged.
34. Before implementation, classify every visible target object into exactly one layer type: transparent overlay frame, solid cut-corner backplate, content image, dynamic icon/symbol, TMP text, stateful button/card, divider/accent, control fill/track/handle, or decorative rail. This classification controls alpha, z-order, sprite generation, and tests.
35. Use accepted screens as implementation templates. Prefer the proven MainMenu, Settings, Custom Game, and corrected MatchOverlay generator patterns before inventing new chrome/button/slider methods.
36. Do not apply a generic "transparent center frame" rule to all chrome. Transparent overlay frames have alpha 0 in the center and render above content. Solid backplates have visible interior alpha/color and render below content. Content images render above backplates and below overlay-only frames.
37. Final completion is forbidden without a fresh rendered Unity capture of the prefab. If capture fails, renders blank, hangs, or cannot be compared, the task is blocked and must be reported as blocked instead of complete.
38. Structural tests, prefab hierarchy checks, alpha checks, and direct PNG inspections are required support checks, but they are never substitutes for rendered visual acceptance.
39. For every named problem area or object path, create a focused target-vs-capture comparison crop and inspect that crop before saying it matches.
39a. Always create a full-screen target-vs-capture comparison with target, rendered capture, and amplified difference overlay after the latest changes. Inspect it before finalizing.
40. If any focused comparison crop still shows visible differences in chrome thickness, corner shape, fill color, separator placement, icon shape, icon scale, icon center, text alignment, text scale, selected state, spacing, opacity, alpha, or merged fill/frame layers, continue fixing or explicitly report the remaining difference as not complete.
41. If one named object path fails the focused crop check, the screen is not complete. Do not mark unrelated improvements as completion for that screen.
42. Do not use phrases like "done", "complete", "matches", or "visual locked" unless the latest rendered capture and focused comparison crops prove it.
43. Prefer a layer-pack workflow over reverse-engineering reusable sprites from a flattened target. When creating a new target, request both the flattened target and separate transparent layer assets for frames, fills, icons, buttons, cards, content images, and state variants.
44. Every converted screen must keep a layer-pack manifest next to the target mockup. The manifest must classify each visible object, record its reusable asset path, alpha rule, z-order rule, separate child layers, and QA status.
45. If only a flattened target exists, generate clean replacement layer sprites from measured target geometry and document any temporary target-derived content layers in the manifest. Do not silently rely on target crops for frames, buttons, rails, icons, or text.

Process:

0. Run the layer-pack gate. Confirm the matching `Design/VisualLockLayered/<SurfaceId>/` folder has the required reference image, separated layers, manifest, contact sheet, and README. If missing, stop Canvas implementation and create those files first.
1. Run the target-contract gate. Compare the target text/content against the planned runtime content. If there are mismatches, update the target/layer pack first or mark the surface `not target-matched`; do not silently substitute different content.
2. Inspect the target mockup and list every distinct visual element that needs its own Unity object or reusable sprite.
3. Create a decomposition and target-to-canvas mapping table before editing. Required columns: target element, target bounds/crop, Unity object path, target layer type, sprite/content source, alpha rule, z-order rule, reusable asset path, separate child layers, 16:9 behavior, 20:9 behavior, target crop path, rendered crop path, and QA status.
4. Create or update the screen layer-pack manifest before implementation. The flattened target is QA reference; the manifest and layer assets are the implementation contract.
5. For every generated or cropped frame/chrome/backplate sprite, inspect alpha on a checkerboard or gameplay-colored background and confirm transparent outside corners/cutouts before using it.
6. For each object, apply the alpha rule from the decomposition table: overlay-frame centers must be alpha 0; solid backplate interiors must remain visible/opaque enough to match the target; outside silhouette pixels must be alpha 0.
7. Inspect every target-derived background crop for baked icons, indicators, badges, text, and content remnants. Remove them from the background and create separate transparent sprite children for each one.
8. Inspect every button background/state sprite and confirm it contains no icon or label pixels. Extract those icons/labels into separate child Image/TMP layers.
9. Inspect every content-with-frame area, such as maps, portraits, cards, previews, resource bars, thumbnails, objective panels, threat panels, trays, and minimaps. Use the correct target layer type: content above solid backplate, content below true overlay frame, or content inside a masked viewport when required by the target.
10. Measure or crop the target frame/backplate silhouette before generating replacement chrome. Validate the replacement against the target silhouette: corner cuts, bevel angle, border line thickness, and chrome thickness must not drift by more than 1-2 rendered pixels.
11. Identify every Button component implied by the target, including cards and icon-only buttons. Configure every button that has target state styling with the shared animated button controller and set the selected mockup item to Selected.
12. Identify decorative chrome/rail/backplate elements separately from panels. Classify each one as overlay frame or solid backplate before generating it.
13. Create a per-object validation checklist from the mockup before implementation. Include every panel, background, frame, rail, icon, icon center/scale, label, button, selected state, divider, content image, and decorative marker.
14. Implement the prefab using layered Canvas objects and generated/cropped reusable assets.
15. Add or update source-mapping tests for the current surface's important frame/fill/button/icon/content sprites.
16. Rebuild the prefab through Unity batch mode.
17. Capture the rendered prefab at the target resolution.
18. Confirm the capture is valid before using it: not blank, not all one color, correct resolution, correct screen visible, and generated after the current changes.
19. If capture fails, returns a blank image, hangs, or cannot be generated, stop the visual-lock claim and report the capture failure as a blocker. Do not replace this with tests or asset inspection.
20. Generate a full-screen target-vs-capture comparison image with target, rendered capture, and amplified difference overlay.
21. Compare the capture against the target mockup panel by panel and object by object. Do not rely only on full-screen or panel-level similarity.
22. For every named problem object path, generate and inspect a focused side-by-side crop: target on one side, rendered Unity capture on the other.
23. Also capture/check a 20:9 landscape aspect.
24. If the screen overlays gameplay or dynamic content, capture/check it over a non-black test background so opaque crop corners and rectangular artifacts are visible. This check must reveal whether outside corners are actually transparent and whether solid backplates/overlay frames use the correct interior alpha.
25. If the capture shows duplicated icons, ghosted icons, or mismatched icon shapes caused by pixels left in a background crop, fix the source crop and regenerate the separate icon sprite.
26. If a content area or decorative chrome/rail shows baked borders, non-reusable borders, merged fill/frame pixels, opaque rectangular corners, wrong interior alpha, or corner geometry larger than the target, correct the layer classification, regenerate the alpha mask, and recapture.
27. If grouped, tab-like, card-like, or selected-looking buttons lack animated Normal/Highlighted/Pressed/Selected/Disabled states, add the shared button Animator setup and recapture.
28. If button icons are smaller, larger, or off-center compared with the target, adjust the icon RectTransform/source sprite and recapture.
29. If target text/content differs from the rendered capture, update the prefab content or revise the target; do not call the current capture matched.
30. If Unity tests pass but the visual capture still has mismatched child objects, keep fixing. Tests are a gate, not the finish line.
31. If any visible object is obviously not matching, do not stop. Iterate until the rendered capture visually matches the target, or explicitly report the remaining difference as not accepted.
32. Only stop when the result is professionally layered, atlas-ready, responsive, alpha-correct, state-correct, hierarchy-verified, content-matched, and visually locked to the mockup.

Acceptance criteria:

- The output must match the target chrome, thin borders, corner shapes, icon placement, and centered text.
- The output must match target visible content: text strings, rewards, objective names, mission names, difficulty labels, counters, image subjects, selected states, and CTA labels. If content intentionally differs, the screen is not target-matched until the target is revised.
- Frame, chrome, overlay, and backplate sprites must have transparent outside corners/cutouts and must not show opaque rectangular backgrounds over gameplay or dynamic screen content.
- Frame/chrome/backplate sprites must follow their classified target layer type. True overlay frames must be transparent overlays separate from fill/content. Solid cut-corner backplates must keep their visible interior and sit below dynamic content. A panel, bar, tray, or rail with the wrong interior alpha is rejected even if its outside corners are transparent.
- Corner shape and border thickness must match the target within 1-2 rendered pixels. Bigger, sharper, chunkier, or synthetic corners are rejected even if tests pass.
- Alpha correctness must be verified in the rendered Unity capture over a non-black/checkerboard/gameplay-colored background, not only by reading the PNG file.
- Icons, badges, chevrons, checkboxes, markers, and state indicators must be clean transparent child sprites, with no duplicated remnants baked into background or portrait crops.
- Button backgrounds must be icon-free and label-free; icon and text children must carry all button symbol and label rendering.
- Maps, portraits, previews, thumbnails, and content art must be separate from their target frame/backplate layer, with z-order matching the decomposition table.
- Grouped/tab-like buttons must use the shared animated button state system, and the target-selected item must be set as Selected.
- Card-like buttons, icon-only buttons, and selected-looking buttons are covered by the same rule; do not limit animated states to objects literally named tab, command, or nav.
- Decorative rails/backplates/chrome strips must be layered and alpha-correct like panels; no rail or backplate may remain a merged opaque crop when the target has transparent corners or separate chrome.
- Button/icon crops must prove icon shape, scale, and centering; asset-path, hierarchy, and alpha tests alone do not prove icon visual match.
- Repeated components must use reusable backgrounds and separate child layers, not merged one-off images.
- All controls must preserve the target state style: normal, highlighted, pressed, selected, and disabled where applicable.
- The final result must pass existing UI tests and include any new tests needed to prevent regressions, but passing tests alone is not enough to accept the work.
- Important prefab images must have source-mapping tests to prove they reference the expected VisualLockLayered destination sprites.
- The final result must have a fresh valid Unity capture at the target resolution plus a 20:9 capture. If either capture is missing, blank, stale, or failed, the screen is not complete.
- The final result must include a full-screen target-vs-capture comparison image with target, rendered capture, and difference overlay from the latest changes.
- Every named object path called out by the user must have a focused side-by-side target-vs-capture crop inspected before claiming it matches.
- A completed screen must include a per-object visual QA pass. Each target child element must be matched or explicitly called out as a remaining difference.
- Before final response, provide the capture paths, focused comparison crop paths, and summarize remaining visual differences. If there are obvious differences, keep fixing instead of asking me to point them out.
- Before final response, state whether the layer-pack gate passed and name the exact `Design/VisualLockLayered/<SurfaceId>/layer_manifest.json` used for implementation.
- If capture or comparison cannot be completed, final response must say "blocked on visual acceptance" and explain why. Do not say the screen matches the target.
- If the layer pack is missing or invalid, final response must say "blocked on layer-pack gate" and explain what is missing. Do not proceed with Canvas implementation.
```

For `Screen_MatchOverlay.prefab`, use:

- `[SCREEN_PREFAB_PATH]`: `Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab`
- `[TARGET_MOCKUP_PATH]`: `Design/VisualLock/SCN-08_RTSBattleHUD/SCN-08_RTSBattleHUD_Landscape_Target.png`

## Main Menu Target

Reference style:

- `Design/VisualLock/MainMenu/MainMenu_Landscape_Visual_Target.png`

Production target:

- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab`

The generated landscape image should be used only as a reference board and optional source for cropping concept art, not as the screen background.

## Step 1 - Revert Temporary Full-Background Screen

Remove the full-screen mockup sprite from `Screen_MainMenu.prefab`.

Keep:

- current routing behavior
- transparent-safe old UI deactivation
- `WarlineCaptureUiMainMenuTests`
- generated mockup file as reference under `Design/VisualLock/MainMenu`

Do not keep:

- `MainMenu_Landscape_Visual_Target.png` as a full-screen runtime UI background
- invisible-only interaction over a flat image

## Step 2 - Decompose the Main Menu Mockup

Create a component inventory from the mockup:

### Shell

- outer metal HUD frame
- top profile/resource bar frame
- left navigation rail frame
- bottom chat/utility strip frame

### Main Menu Cards

- Campaign card frame
- Operations card frame
- Skirmish card frame
- card background art crop
- card accent trim color
- card icon/emblem
- title text
- subtitle text
- play/arrow marker

### Top Bar

- commander portrait frame
- commander portrait art
- level/XP bar
- credits resource counter frame
- supplies resource counter frame
- command resource counter frame
- plus button
- settings button

### Left Nav

- square nav button frame
- profile icon
- inbox icon
- store icon
- events icon
- ranking icon
- notification badge

### Bottom Bar

- chat button frame
- social button frame
- chat text area

## Step 3 - Asset Source Strategy

Use this order:

1. Accepted WarlineCapture UI kit sprites and generated HUD chrome from already visual-locked screens.
2. Existing project logo, generated character portraits, and approved UI art.
3. 3D single-map art references, existing prefab configs, and command-base menu assets for gameplay, map, minimap, unit, and battlefield content.
4. New generated raster assets only for card art, portraits, and scene illustrations.
5. Coded Unity UI shapes only for simple fills, masks, and layout helpers.

Generated assets should be saved under:

- `Assets/Game/Art/UI/Generated/MainMenu/Cards/`
- `Assets/Game/Art/UI/Generated/MainMenu/Portraits/`
- `Assets/Game/Art/UI/Generated/MainMenu/Backgrounds/`

Prompt records should be saved under:

- `Design/WarlineCapture_UIUX_MainMenu_Art_Generation_Guide.md`

## Step 4 - Create Sliced Runtime Sprites

For reusable UI frames, create individual sliced sprites:

- `Panel_Frame_Dark`
- `Panel_Frame_Cyan`
- `Panel_Frame_Amber`
- `Panel_Frame_Green`
- `Button_Square_Normal`
- `Button_Square_Pressed`
- `Button_Wide_Normal`
- `Button_Wide_Pressed`
- `ResourceCounter_Frame`
- `NotificationBadge_Red`

Import settings:

- Texture Type: Sprite
- Sprite Mode: Single or Multiple as needed
- Mesh Type: Full Rect
- Pixels Per Unit consistent across UI kit
- Border configured for 9-slice scaling

## Step 5 - Build Real Main Menu Prefabs

Create/extend reusable prefabs:

- `HudFrameView.prefab`
- `SideNavButtonView.prefab`
- `ModeCardView.prefab`
- `ResourceCounterView.prefab`
- `FooterUtilityButtonView.prefab`
- `IconButtonView.prefab`

Each component must expose real references:

- background image
- accent frame image
- icon image
- title TMP text
- subtitle TMP text
- real button
- optional badge

## Step 6 - Responsive Layout Rules

Use Unity anchors and layout groups, not absolute full-screen image stretching.

Reference resolution:

- `1920x1080`

Supported validation aspect ratios:

- 16:9
- 19.5:9
- 20:9
- tablet-ish landscape if needed

Rules:

- Header keeps fixed relative height.
- Left rail keeps fixed relative width with minimum touch target sizes.
- Mode cards scale horizontally, not vertically beyond readability.
- Card art uses `Image.preserveAspect` or masked crop containers.
- Text containers use stable bounds and must not overlap.
- Buttons remain real UI elements with normal/pressed/disabled states.

## Step 7 - Interaction States

Every interactive element must have:

- normal state
- highlighted/hover state for editor/device pointer
- pressed state
- disabled state
- clear hit target

For mobile, hover is optional visually but still useful in editor.

Use either:

- `Button.transition = SpriteSwap` with sliced state sprites
- or `ColorTint` over real sliced panel sprites

Do not use invisible buttons except as temporary debug overlays.

## Step 8 - Main Menu Implementation Order

1. Restore Main Menu to real Canvas elements, no flat full-screen background.
2. Build top bar with real profile/resource UI.
3. Build left nav with real buttons and icons.
4. Build mode cards with real frames, text, separate art images, and buttons.
5. Build bottom utility strip.
6. Add generated card art assets.
7. Add button state sprites.
8. Validate routing.
9. Validate responsive screenshots.

## Step 9 - Screenshot Validation

Screenshot comparison should validate layout and visual fidelity, not rely on full-image equality.

Generate screenshots at:

- `1920x1080`
- `2340x1080`
- `2400x1080`
- `2560x1440`

Compare:

- panel bounds
- card positions
- text alignment
- color palette
- button/icon visibility
- no stretching artifacts
- no overlaps
- no clipped text

Pixel diff can still be tracked, but it is not the only pass/fail gate because the UI is now real, responsive, and stateful.

## Step 10 - Acceptance Criteria

Main Menu is visually accepted when:

- it clearly matches the mockup style and layout
- no full-screen mockup background is used as the UI
- all major panels are separate Canvas objects
- mode card artwork is separate from card frames/text
- all buttons are real and stateful
- layout works across common Android landscape aspect ratios
- existing tests pass
- screenshot artifacts are generated for review
- `git diff --check` is clean

## Apply to Other Screens

After Main Menu is accepted, repeat the same conversion method for:

1. Settings and Accessibility
2. Skirmish Setup
3. Splash / Loading
4. Tactical HUD
5. Build Drawer
6. Command Wheel
7. Popups
8. Campaign Map / Briefing / Loadout
9. Operations / District Detail
10. Commander Profile

## Per-Screen Acceptance Gate

For every screen after Main Menu:

- Use the original screen or popup mockup as the style source.
- If a landscape target is needed, preserve the original element hierarchy and visual language. Do not redesign the screen while adapting it to landscape.
- Split replaceable pieces into separate runtime objects: background art, frame, icons, buttons, labels, counters, portraits, and controls.
- Reuse shared assets when the style matches an existing accepted screen, especially the Splash/Settings outer frame and the accepted animated button states.
- Validate at `1920x1080` plus at least one wide Android aspect before calling the screen accepted.
- Optimize the screen immediately after visual acceptance by assigning atlas labels, setting UI texture import options, disabling decorative raycasts, and removing transparent placeholder graphics.
- Add tests for visual structure and interaction wiring before continuing to the next screen.

Accepted examples:

- Main Menu: establishes real Canvas conversion for complex app shell screens.
- Splash/Loading: establishes shared brand frame and loading treatment.
- Settings/Accessibility: establishes reusable controls for tabs, sliders, toggles, dropdowns, segmented rows, footer buttons, and shared full-screen frame reuse.
