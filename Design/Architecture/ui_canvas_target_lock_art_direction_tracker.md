# UI Canvas Target Lock Art Direction Tracker

Purpose:
Update the existing Unity Canvas screens and popups to use the approved Target Lock art direction currently proven in the UI Toolkit work, while keeping the runtime on Canvas for performance and stability.

This is a Canvas visual migration tracker. It is not a UI Toolkit rewrite, not an ECS task, and not a gameplay behavior migration.

Last updated:
2026-06-24

Approved visual source:

- `Design/Architecture/ui_toolkit_target_lock_mockup_conversion_playbook.md`
- `Design/Architecture/ui_canvas_target_lock_mockup_conversion_playbook.md`
- `Design/Architecture/ui_toolkit_target_lock_visual_match_tracker.md`
- Approved SCN-02 shared chrome baseline from the UI Toolkit main menu pass.
- Latest Target Lock reference mockups under `Design/VisualLockLayered/**/reference/`.

## Progress Snapshot

- Checklist progress: `99 / 158 complete (62.7%)`.
- In progress: `1`.
- Remaining open: `59`.
- Current target: `Phase 5 - popup/modal sprite-only art-direction pass`.
- Active Canvas shell/modal surfaces target-matched: `3 / 12`.
- Secondary/reference Canvas popup surfaces target-matched: `1 / 6`.
- Secondary/reference Canvas popup baseline captured or decisioned: `6 / 6`.
- Shared Canvas chrome baseline status: `asset map and contact sheet complete; left-nav state seed applied to active Main Menu and Armory Canvas nav instances; Main Menu mode-card state seed applied; PopupFrameView shared modal seed and UIShellAppCanvas placeholder modal fallback seed applied; shared chrome material audit confirms default UI materials on seeded chrome`.
- Button/selectable interaction standard status: `active visible Button audit across Assets/Game/Prefabs/UI passes with 0 missing SpriteSwap state issues; left-nav route button, Main Menu mode-card, Main Menu Inbox/Settings header actions, Main Menu deploy CTA, Armory catalog item card, Armory right action buttons, PopupFrame close-button, shell placeholder close-button, SCN-08 Match HUD command/squad/transport/zoom/close buttons, SCN-08 Build Placement Bar buttons, SCN-08 Full Map buttons, SCN-09 Build Drawer buttons/cards/tabs, shell/legacy Mission Result buttons, and active popup close/confirm/action buttons have full highlighted/pressed/selected/disabled sprite states. Documented exceptions are inactive prototype buttons and transparent route Hotspot buttons that must remain nonvisual unless their owning screen is structurally reopened`.
- Responsive CanvasScaler validation status: `SCN-02 Main Menu iteration 66 is user-approved after shadow captures passed at 1280x720, 1920x1080, 2400x1080, and 4800x2160; broader surface validation still pending`.
- Performance validation status: `Phase 0 shadow batchmode baselines captured for Main Menu and Match HUD with Canvas active vs disabled; render counter recorder returned zero draw/batch values in batchmode, so real Game View/device profiling remains separate`.
- Shadow-project validation status: `Canvas main menu/deploy UI fallback validation passed at 1280x720, 1920x1080, 2400x1080, and 4800x2160; 4800x2160, 1920x1080, and 2400x1080 captures passed for reachable Loading, Armory, Match HUD, Build Drawer, and Build Placement Bar surfaces; 4800x2160 captures passed for active Mission Result, Confirm Raid, End Of Day Report, and Intel Reveal modal prefabs; 4800x2160 captures passed for secondary/reference Ability Upgrade Detail, Build Placement Panel, Pause Menu, Popup Frame, Reward Unlock, and Threat Alert popup prefabs; Main Menu and Match HUD Canvas active/disabled performance baselines passed; shared left-nav state seed 4800x2160 Main Menu and Armory captures passed; Main Menu and Armory left-nav overlap validation passed at 1920x1080; Main Menu header/logo scale validation passed at 1280x720; Main Menu route smoke passed after mode-card state seed; PopupFrame target-lock seed 4800x2160 modal capture passed; Main Menu route smoke passed after UIShellAppCanvas placeholder modal fallback seed; SCN-02 Canvas Main Menu iteration 66 captured in the shadow project after rejected header-action/resource correction and approved by the user; SCN-19 Armory catalog-card iteration 11, right action-button iteration 12, right detail section iteration 13, footer-tab iteration 14, all-category iteration 16, and final all-aspect iteration 18 captures passed in the shadow project; SCN-03 Commander Profile static full-root iteration 13 captures passed at 1920x1080 and 2560x1080 in the shadow project; graphics-enabled shadow captures passed after the SCN-08/SCN-09 sprite-only pass for Match HUD at 1920x1080, Build Drawer at 1920x1080, and Build Placement Bar at 1280x720, 1920x1080, 2400x1080, and 4800x2160; graphics-enabled shadow capture passed for active POP-05 Mission Result after replacing missing legacy sprite references with Target Lock sprite references; graphics-enabled shadow capture passed for Confirm Raid after replacing missing legacy sprite references and correcting the accidental gold backing; graphics-enabled shadow capture passed for Intel Reveal after replacing missing legacy sprite references; graphics-enabled shadow capture passed for End Of Day Report after replacing missing legacy sprite references; graphics-enabled shadow capture passed for Pause Menu after replacing missing legacy sprite references. SCN-19 is counted target-matched for the current Canvas runtime behavior; its footer strip is a visual/data tab family with no active content-switching controller in the current Canvas prefab. SCN-03 is counted target-matched for the current prefab/static capture behavior; the runtime router still does not directly expose the content route`.
- Main-project validation status: `RuntimeUiConfig is now Canvas by default; no main-project capture validation yet`.

Recent slice notes:

- Replaced all known visible legacy `MainMenuV15C` and Synty sprite references in `SCN02_MainMenuContent.prefab` with the SCN-02C Target Lock sprite family or the UI Toolkit-approved logo. The latest prefab GUID audit still finds no known visible legacy sprite GUIDs; remaining visible sprite references are SCN-02C Target Lock sprites plus `scn01_v04_logo_lockup.png`.
- Iterated the SCN-02 Canvas mode cards into live lower label plates using existing attached card children: `ProgressMeter` now renders the bottom plate, `TitleIcon` renders the badge icon above the plate, stale progress text/segments are disabled, and the transparent card hotspot remains the raycast target.
- Converted the SCN-02 right commander area away from one tall baked-looking block into live Canvas subpanels: the existing header frame, portrait frame, identity row, readiness row, and lower status row now render separate Target Lock chrome Images. Iteration 32 narrows the stack so the right chrome edge remains visible and turns the lower block into two readable status rows.
- Updated the Canvas left-nav instances to use Target Lock labels/icons/chrome in the approved order: CAMPAIGN, ARMORY, SUPPLY, COMMAND, TECH TREE, PROFILE. The sixth PROFILE row uses the approved `scn02c_nav_profile_tag_icon` and routes to `UIRoute.CommandFeed`, matching the UI Toolkit main menu behavior.
- Fixed the commander panel render order after sprite replacement so the portrait renders above the panel backing again.
- Latest shadow-project artifact: `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_07/shadow_canvas_scn02_mainmenu_iter18_card_restore_commander_1920x1080.png`. The capture passed technically, but this is not a final visual match candidate.
- Continued the first focused Canvas SCN-02 Main Menu Target Lock loop: corrected the logo/header treatment to use the UI Toolkit-approved logo lockup without the old Canvas logo chip, switched the tactical table background/cards/commander portrait to Target Lock assets, cleaned mode-card layering so labels render above chrome, and rebuilt the deploy CTA with balanced chevrons, a center star tab, and Canvas Button state targeting on the visible frame.
- Correction: SCN-02 is not complete or approved yet. The latest capture is cleaner and the known visible legacy sprite GUID audit is clean, but the screen still needs strict panel-by-panel and button-by-button target comparison before it can be counted as target-matched.
- Strengthened the SCN-02 loop gates after user review: the main menu is not complete until every visible old-art-direction sprite is replaced or explicitly documented as invisible/runtime-only, every panel family has a focused crop, every selectable family has full state coverage, and the final candidate matches the approved UI Toolkit Target Lock baseline panel-by-panel.
- User correction for SCN-02 Canvas header actions: keep exactly two header action buttons, `Inbox` and `Settings`. Do not add a visual-only hamburger/menu button to Canvas just because the UI Toolkit reference has one.
- SCN-02 Canvas header action correction applied and validated in the shadow project: the old long action backing is disabled, the bound `InboxButton` and `SettingsButton` render as two separate square Target Lock buttons, and no hamburger/menu action exists in the prefab. Latest artifact: `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_12/shadow_canvas_scn02_mainmenu_iter23_two_header_actions_1920x1080.png`.
- User correction applied for single-panel multi-section art: the one-piece commander background candidate was removed and the right side must stay split into separate Canvas panels. Current commander work uses separate portrait and row frame sprites with PPU-tuned sliced imports; it remains visually open until scale, spacing, and row content are finished.
- User correction applied for SCN-02 mode-card art containment: all three thumbnail viewports now stay inside the card chrome instead of bleeding above the frame, using the existing `RectMask2D` viewports with corrected top inset and width.
- User correction applied for SCN-02 deploy CTA: the visible deploy frame and both route hit targets now use full sprite-state transitions, the deploy frame import is PPU-tuned for thinner chrome, and the CTA was resized with less crowded chevrons/text. Latest artifact: `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_16/shadow_canvas_scn02_mainmenu_iter27_deploy_tight_1920x1080.png`.
- SCN-02 commander margin pass captured in the shadow project after moving the separate right-panel stack inward. Focused review crops were saved at `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_17/crops/scn02_iter28_panel_crop_contact.png`. The crops show header actions, card containment, and deploy CTA are no longer the first blockers; commander row/content polish and final button-state proof remain open.
- SCN-02 focused card/commander iteration 32 captured in the shadow project at `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_18/shadow_canvas_scn02_mainmenu_iter32_commander_edge_1920x1080.png`; focused crops were saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_18/crops/`. This pass tightens the card art masks, preserves separate commander panels, exposes the commander right edge, and makes the readiness/status rows readable.
- SCN-02 commander profile hotspot state coverage added: the active route button targets Commander Profile through `UIRoute.CommandFeed` and now uses the visible portrait frame as its target graphic with matching default, hover, pressed, selected, and disabled sliced sprite states. The active-button audit reports no missing sprite-state coverage; contact sheet saved at `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_19/scn02_iter33_commander_button_state_contact.png`.
- SCN-02 final legacy sprite re-audit passed after the commander button-state slice: `0 / 28` old `Assets/Game/Art/UI/Generated/MainMenuV15C` GUIDs are referenced by `SCN02_MainMenuContent.prefab`. Remaining sprite Image references are the approved logo, SCN-02C/Target Lock sprite families, and documented shared UI chrome.
- SCN-02 focused visual proof set is complete for the current candidate: iteration 32 contains crops for header actions, left nav, mode cards, right commander panel, and deploy CTA; iteration 19 contains the commander button-state contact sheet after the final active-button state audit.
- SCN-02 all-aspect validation captured in the shadow project under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_20/`: 1280x720, 2400x1080, and 4800x2160 stay readable, aligned, and unclipped.
- SCN-02 target comparison artifact saved at `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_20/scn02_iter34_reference_vs_canvas_candidate_1920x1080.png` for the previous candidate. Iteration 41 supersedes it; remaining intentional deviations are documented rather than hidden: Canvas keeps the user-approved two header action buttons, and the right side stays split into separate live Canvas panels instead of a one-piece baked background.
- SCN-02 iteration 41 replaces the prior final candidate: the sixth PROFILE nav row is now present, all remaining SCN-02 sliced structural frame sprites have `spritePixelsToUnits: 300` for cleaner thin chrome, the background viewport/art is overscanned to avoid 16:9 camera-clear bands, the commander portrait area has a separate dark Canvas backing rather than raw map bleed, and the right commander stack has a restored outer margin.
- SCN-02 iteration 41 all-aspect shadow captures are saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_21/`: `shadow_canvas_scn02_mainmenu_iter41_aspect_1280x720.png`, `shadow_canvas_scn02_mainmenu_iter41_aspect_2400x1080.png`, and `shadow_canvas_scn02_mainmenu_iter41_aspect_4800x2160.png`. These are the current user-verification candidates.
- User rejected the SCN-02 iteration 41/42 candidate as not approval-ready. Visible defects called out: Settings/Inbox actions were too small, the right commander area was messy/clipped, the deploy CTA felt too small, the left navigation did not match, game mode cards did not match, and sizing/padding detail was not acceptable.
- SCN-02 correction loop restarted through iterations 43-48. Iteration 48 is the latest shadow capture after moving layout control to the actual child-level levers used by the runtime section installer: shared nav row width/inset, mode-card layout padding/force-expand, card vertical band, commander panel local offset, and deploy frame size. Latest artifact: `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_28/shadow_canvas_scn02_mainmenu_iter48_left_nav_inset_1920x1080.png`.
- Still wrong / next iteration: run focused crop comparison for iteration 48 before approval; mode-card lower star/divider detail and right commander row richness need one more focused comparison against the approved UI Toolkit baseline; final all-aspect captures must be rerun after any polish.
- Added the Canvas-specific conversion playbook at `Design/Architecture/ui_canvas_target_lock_mockup_conversion_playbook.md` after the rejected SCN-02 candidate. The playbook codifies the faster loop for this work: compare first, classify visible defects, fix PPU/9-slice before layout guesses, finish each panel family before moving on, and always report `Still wrong / next iteration`.
- SCN-02 iteration 56 supersedes iteration 48 for the current correction loop. Latest shadow capture: `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_36/shadow_canvas_scn02_mainmenu_iter56_clean_revert_divider_1920x1080.png`; latest contact sheet: `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_36/scn02_iter56_ui_toolkit_vs_canvas_contact.png`.
- Iteration 56 improvements: the left nav now uses thinner shared row geometry with matching root and instance sizes, the active Campaign label no longer reads as clipped, the right commander header badge/title no longer overlaps the title strip, the portrait viewport is masked with a darker backing, and the deploy CTA is no longer tiny.
- Rejected during iteration 55: a text-based `----- * -----` mode-card divider looked cheap/noisy in capture, so it was removed. The mode-card star/divider finish remains open and should be solved with real Canvas image/sprite pieces, not ASCII text.
- SCN-02 iteration 57 supersedes iteration 56 for the current correction loop. Latest shadow capture: `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_37/shadow_canvas_scn02_mainmenu_iter57_real_card_dividers_1920x1080.png`; latest contact sheet: `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_37/scn02_iter57_ui_toolkit_vs_canvas_contact.png`.
- Iteration 57 improvements: added real Canvas Image divider/star pieces to all three mode-card lower plates using the thin divider and deploy-star sprites; saved focused crop contacts for header actions, left nav, mode cards, right commander panel, and deploy CTA; reran shadow all-aspect captures at 1280x720, 2400x1080, and 4800x2160.
- User rejected SCN-02 iteration 57 as not approval-ready. Visible defects called out: Settings and Inbox buttons are not the same height/y-position as the resource panels; resource panels are too large against the mockup; resource text is too small and overlaps icons; commander panels overlap and leave excessive empty margin to the right edge; there is a large gap between game modes and deploy; commander text is too small; commander padding is too tight; the deploy star has a black background.
- Still wrong / next iteration: shrink and align the header resource/action family, increase resource text/icon separation, move and pad the separate commander panel stack, increase commander text readability, raise the deploy CTA toward the mode cards, remove the black deploy-star backing, then rerun focused crops and all-aspect shadow captures before any approval request.
- SCN-02 iteration 61 supersedes the rejected iteration 57 and the wide-aspect-broken iteration 60. Corrections: resource chips were resized and their values/icons separated; Inbox/Settings were aligned to the chip height/y-position; the commander stack was moved right, widened, and given larger text/padding; the deploy star black tab was disabled; the deploy/card vertical spacing was tuned and revalidated at 1920x1080, 2400x1080, and 4800x2160. Latest artifacts: `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_41/shadow_canvas_scn02_mainmenu_iter61_responsive_spacing_1920x1080.png`, `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_41/scn02_iter61_ui_toolkit_vs_canvas_contact.png`, and `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_41/scn02_iter61_all_aspect_contact.png`.
- User rejected the SCN-02 iteration 61 family as still not approval-ready. Visible defects called out: resources and Inbox/Settings were not vertically centered in the header; Settings/Inbox did not match the target height/chrome; action icons and resource plus icons were crowding/overlapping borders; commander panel sat too close to the header; commander subpanels overlapped the portrait/top panel and needed more padding.
- SCN-02 iteration 66 supersedes iteration 61 for the current correction loop. Corrections: Inbox and Settings are restored to the same `160x160` Canvas frame size and centerline as the resource chip family; their sliced square-frame Images use `m_PixelsPerUnitMultiplier: 5.5` to match the UI Toolkit thin-chrome slice scale; action icons are padded inside the frame; resource plus icons are smaller and moved inward; the commander stack is moved down and its row panels are expanded to reduce overlap and improve internal padding. Latest artifacts: `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_42/shadow_canvas_scn02_mainmenu_iter66_thin_action_chrome_1920x1080.png`, `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_42/focused/iter66_header_reference_vs_canvas.png`, and `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_42/scn02_iter66_all_aspect_contact.png`.
- User approved SCN-02 Canvas Main Menu iteration 66 as the main menu visual baseline. This approval locks the current Canvas main menu choices as the shared reference for later menu-adjacent Canvas screens: UI Toolkit-approved logo, shared header/resource rhythm, two-button Inbox/Settings action rule for Main Menu, left-nav chrome, separate right-side live panels, thin sliced chrome from correct PPU/Image multiplier, and full-frame selectable states.
- Still wrong / next iteration: no known SCN-02 Main Menu defects remain after user approval. Next action is to start the next tracker surface using the updated playbook and the approved SCN-02 Canvas baseline.
- SCN-19 Armory initial audit started from the approved SCN-02 Canvas baseline. The new-art reference bitmap now exists despite the stale reference README; baseline comparison artifacts were created under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_01/`: `scn19_reference_vs_canvas_baseline_1920x1080.png` and `scn19_focused_baseline_contact.png`.
- SCN-19 first-panel-family findings: current Canvas Armory does not yet match the approved product family. The Armory mockup header differs from the approved SCN-02 header, but user direction now locks every menu-adjacent Canvas screen to the SCN-02 main menu header unchanged; do not target-match a separate Armory header. The left-side category/nav area was effectively absent in the active baseline; the right inspection panel is clipped off-screen; bottom navigation is legacy and too small; central catalog cards are not visible in the active baseline. Commander Profile remains prefab-only/not active in the current Canvas route notes, so the next active runtime surface is Armory.
- Still wrong / next iteration: for SCN-19, keep the inherited SCN-02 main menu header unchanged and fix only the left-category panel family before catalog or right-panel work. Exact next fixes: establish visible left category buttons with full-frame Target Lock states and safe icon/text padding, keep runtime-bound names intact, then recapture focused left-nav crops before moving to center catalog cards.
- SCN-19 locked-header inheritance and left-nav slice validated in the shadow project after refreshing the stale Armory baseline. No Armory-specific header edits were made. Current evidence is saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_06/`: `shadow_canvas_scn19_armory_left_nav_scaled_fit_1920x1080.png` and `scn19_iter06_reference_vs_canvas_contact.png`.
- SCN-19 left-nav correction keeps the approved SCN-02 shared chrome/style while adding Armory-only child overrides for the five category rows. The row frames, icons, labels, and chevrons now fit the narrower Armory category column without clipping at the screen edge.
- Still wrong / next iteration: SCN-19 is not screen-complete. Catalog cards are still too dense/small compared with the Target Lock Armory reference, card state coverage needs a proper default/hover/selected/pressed/disabled audit, the right inspection panel still needs a separate-section padding/readability pass, and footer tabs remain legacy/tiny. Next fix only the catalog-card family before right-detail or footer work.
- SCN-19 catalog-card family updated and validated in the shadow project without changing the locked shared header. The active catalog now uses a four-column card grid, denser row rhythm, a deeper local list viewport, larger readable card text, full-frame `SpriteSwap` states on the visible item frame, and a green progress fill closer to the Armory reference. Latest evidence is saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_11/`: `shadow_canvas_scn19_armory_catalog_cards_1920x1080.png`, `focused/canvas_iter11_catalog_cards.png`, and `focused/catalog_cards_reference_vs_iter11.png`.
- Still wrong / next iteration: SCN-19 is not screen-complete. The right inspection panel still needs a separate live-section padding/readability pass, the right-side action buttons must be made large/readable with full states, footer tabs remain legacy/tiny, and selected-detail imagery needs a live visual audit. Next fix the right inspection panel family before footer or popup work.
- SCN-19 right action-button family updated and validated in the shadow project without changing the locked shared header. The existing `UpgradeButton`, `InspectButton`, and `EquipButton` now share uniform sizing/spacing, sliced visible CTA frames, centered labels, and Button transitions targeting the visible frames instead of transparent hotspot Images. Latest evidence is saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_12/`: `shadow_canvas_scn19_armory_actions_1920x1080.png`, `focused/canvas_iter12_right_detail_actions.png`, `focused/canvas_iter12_right_action_buttons.png`, and `focused/right_detail_reference_vs_iter12.png`.
- SCN-19 right detail section pass iteration 13 centered and widened the bound title/type/description fields, widened the bound source/unlock row, and kept every existing bound detail field visible. Latest evidence is saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_13/`: `shadow_canvas_scn19_armory_right_detail_1920x1080.png`, `focused/canvas_iter13_right_detail_sections.png`, `focused/canvas_iter13_right_hero_text.png`, `focused/canvas_iter13_right_stats_source_progress.png`, and `focused/right_detail_reference_vs_iter13.png`.
- SCN-19 footer-tab visual slice iteration 14 enlarged the existing `Owned`, `UpgradeTracks`, `Parts`, and `GearModules` tab family, tuned the repeated icon/label geometry, and switched the tab frames to thinner sliced chrome using the existing Armory Target Lock tab sprites. Latest evidence is saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_14/`: `shadow_canvas_scn19_armory_footer_tabs_1920x1080.png`, `focused/canvas_iter14_footer_tabs.png`, and `focused/canvas_iter14_right_and_footer_context.png`.
- Still wrong / next iteration: SCN-19 is not screen-complete. The right inspection panel still reads denser than the reference because it preserves runtime-bound source, ability, and progress sections; footer tab switching still needs live visual validation across all tab states; selected-detail imagery still needs a live visual audit. Next validate tab-state changes and selected-detail image updates, then decide whether the extra right-panel runtime-bound sections are an accepted Armory-specific deviation or need another density pass.
- Added editor-only Armory category route-capture support to `CanvasMenuFallbackValidation.RunRouteCapture` via `WARLINE_CANVAS_ARMORY_CATEGORY=Characters|Vehicles|Aircrafts|Buildings|Support`. This is validation plumbing only: it enqueues the existing Armory category request after installing the Armory Canvas route and does not edit runtime Armory view behavior.
- SCN-19 iteration 15 per-category validation is blocked by Unity licensing before scene load. Characters capture was attempted in sandboxed batchmode, retried per the validation policy, retried after the licensing helper processes had exited, rerun with the mandated escalated/out-of-sandbox batchmode workaround, and also attempted through a graphics-enabled Editor/open path. All attempts stalled before `CanvasMenuFallbackValidation.RunRouteCapture` configured the Armory route, so no category screenshot was produced. Logs are saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_15/`; blocker report saved at `Design/AgentReports/2026-06-23_ui_canvas_armory_shadow_validation_blocked.md`.
- Still wrong / next iteration: superseded by iteration 16 category captures; the remaining SCN-19 validation is footer-tab switching and the right inspection panel density decision.
- SCN-19 iteration 16 resolved the prior licensing blocker and captured all Armory categories in the shadow project at 1920x1080: Characters (`luma=0.239`), Vehicles (`luma=0.253`), Aircrafts (`luma=0.272`), Buildings (`luma=0.264`), and Support (`luma=0.301`). Evidence is saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_16/`: `scn19_iter16_armory_category_contact.png`, `focused/scn19_iter16_category_catalog_category_contact.png`, `focused/scn19_iter16_right_detail_category_contact.png`, and `focused/scn19_iter16_footer_tabs_category_contact.png`.
- SCN-19 live imagery validation passed: selected-detail imagery changes for every captured category, and catalog card portraits change for Characters, Vehicles, Aircrafts, and Buildings. Support has no visible catalog cards in the captured category state, but the right detail image still updates.
- Still wrong / next iteration: SCN-19 is not screen-complete. Footer tab switching (`Owned`, `Upgrade Tracks`, `Parts`, `Gear Modules`) still needs live visual validation without layout shifts, and the right inspection panel still needs a density decision because the runtime-bound source, ability, and progress sections make it denser than the reference.
- SCN-19 iteration 18 is the final Armory visual pass for the current Canvas runtime behavior. All-aspect shadow captures passed at 1280x720 (`luma=0.242`), 1920x1080 (`luma=0.239`), 2400x1080 (`luma=0.240`), and 4800x2160 (`luma=0.244`). Evidence is saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-19_Armory/iteration_18/`: `scn19_iter18_all_aspect_contact.png` plus focused crops for left nav, catalog, right panel, and footer.
- SCN-19 right inspection panel density decision: accepted for the current Canvas migration. The panel is denser than the flat reference because it preserves live runtime-bound source/unlock, capability, stat, progress, and action sections, but the latest focused crop shows separate panels, readable labels, safe border padding, and no sibling overlap.
- SCN-19 footer decision: the current Canvas footer strip is a visual/data tab family, not a live content-switching controller. Editor-only selected-button tooling proved only currently bound Buttons can be selected; footer roots are not backed by active Button/controller behavior in the prefab. The visual footer family remains stable across category captures and all-aspect captures, so it is counted complete for this visual migration without adding new runtime behavior.
- Still wrong / next iteration: no known SCN-19 Armory visual defects remain at the current Canvas runtime behavior level. Next action is the SCN-03 Commander Profile reachability/baseline audit; if SCN-03 remains prefab-only, record the route limitation before visual work.
- SCN-03 Commander Profile route audit confirmed the Canvas runtime router does not directly expose the content through a reliable `UIRouterView.screenPrefabs` entry, so editor-only static full-root capture support was added to `CanvasMenuFallbackValidation.RunRouteCapture` using `WARLINE_CANVAS_STATIC_CONTENT_PREFAB` and `WARLINE_CANVAS_STATIC_CONTENT_FULL_ROOT=1`. This preserves the locked SCN-02 shared header during capture and does not change runtime gameplay or UI route behavior.
- SCN-03 Commander Profile target-lock pass completed for the current Canvas prefab/static capture behavior. The existing full-screen prefab now inherits the approved SCN-02 header unchanged in capture, uses the SCN-02-style left navigation chrome, separates the identity, overview, account snapshot, reward track, recent history, and footer action panel families, and replaces stale legacy labels with explicit readable Target Lock text where the Canvas prefab lacked the matching labels.
- SCN-03 shadow validation evidence is saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-03_CommanderProfile/iteration_13/`: `shadow_canvas_scn03_fullroot_layout_1920x1080.png`, `shadow_canvas_scn03_fullroot_layout_2560x1080.png`, and `scn03_iter13_focused_crop_contact.png`. Both full-screen captures passed route visibility (`luma=0.162` and `luma=0.194`), focused crops cover left nav, identity/overview, account/footer, reward track, and recent history, and the editor helper compiles without project/compiler warnings after replacing obsolete `FindObjectsByType(..., FindObjectsSortMode)` and TMP wrapping APIs.
- SCN-03 route decision: counted target-matched for the current Canvas visual migration, but not counted as runtime-route-complete. The prefab remains a static/full-root content surface until a separate runtime navigation task wires `UIRoute.CommandFeed` to live Canvas screen installation.
- Still wrong / next iteration: no known SCN-03 visual defects remain at the current Canvas prefab/static capture behavior level. Next action is the focused crop rollup for menu panel families and then the next menu-adjacent surface audit; do not reopen the locked SCN-02 shared header unless the user explicitly asks.
- Latest validation: `git diff --check` passed after the SCN-03 prefab whitespace cleanup.
- Added editor-only route-capture cleanup for `MenuDiagnosticsPanel`/`Panel_FPS`/`Label_FPS` so visual screenshots do not include the scene FPS diagnostics overlay. This does not change runtime route behavior or gameplay.
- Latest shadow-project approval artifact: `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-02C_MainMenuBrightCommand/iteration_02/shadow_canvas_scn02_mainmenu_iter11_no_diagnostics_1920x1080.png`.
- Applied the shared card state seed to the existing SCN-02 Main Menu mode cards without renaming runtime-bound objects: the transparent hotspot remains the raycast target, while each Button now targets the visible full-frame card Image for default/hover/pressed/selected/disabled sprite swapping.
- Applied the shared popup foundation to `PopupFrameView.prefab`: sliced Target Lock panel frame, header bar, close button frame, and full-frame close button hover/pressed/selected states.
- Applied the same Target Lock modal fallback seed to the inline `UIShellAppCanvas.prefab` placeholder modal: sliced panel frame, rectangular close button frame, and full-frame close button hover/pressed/selected states.
- Confirmed seeded nav, card, and popup chrome Images still use the default UI material (`m_Material: {fileID: 0}`); real draw/batch profiling remains a Phase 8 validation gate.
- Saved shadow validation artifacts under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/shared/`: `shadow_scn02_mode_card_state_seed_4800x2160.png`, `shadow_popup_frame_target_lock_seed_4800x2160.png`, and `shadow_scn02_shell_placeholder_seed_4800x2160.png`.
- Verified the shared left-navigation reuse contract for the Canvas Phase 2 pass: SCN-02 Main Menu and SCN-19 Armory use the same seeded left-nav style, while SCN-08 Match HUD remains excluded from menu nav/header reuse.
- Captured focused Phase 2 shadow evidence under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/phase2/`: Main Menu and Armory left-nav overlap at `1920x1080`, Main Menu header/logo scale at `1280x720`, and header/nav crop artifacts.
- User locked the shared menu header after SCN-02 approval: every menu-adjacent Canvas screen must inherit the approved SCN-02 main menu header unchanged. Future menu-screen passes skip header target-matching unless the shared SCN-02 header itself is explicitly reopened by the user.
- SCN-08 Match HUD runtime-bound inventory completed before visual edits. The protected shell install regions, root names, serialized component fields, Button target-graphic constraints, runtime-driven health/map/feedback fields, squad card selected-state sprite contract, and passenger drawer pooling contract are recorded at `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-08_MatchHUD/iteration_00/scn08_match_hud_runtime_bound_inventory.md`.
- SCN-08 sprite-only Target Lock pass applied to the existing `SCN08_MatchHudContent.prefab` without hierarchy/layout restructuring: resource chips/icons, current-order rail/chevrons, objective/status/progress frames, selected-entity panel chrome, minimap/quick-rail panel chrome, command button frames/icons, squad card frames, squad portraits, and transport/action button families now use direct V02 sprite equivalents where available.
- SCN-08 button state audit passed after the sprite-only pass: all `24` existing Button components now use SpriteSwap with non-empty highlighted, pressed, selected, and disabled sprites. Command and square buttons use the V02 square selected/default frame pair; transport/action buttons use the V02 rectangular selected/default pair; squad cards use the V02 squad-card selected/default pair.
- SCN-08 remaining old-sprite audit is intentionally limited to gameplay/minimap marker sprites and icons with no direct V02 equivalent: board vehicle, jump arrow, shield/rank badge, minimap content, and tactical map marker/dot/pin/path/ring sprites. These were not replaced with unrelated art to avoid changing gameplay readability by guessing.
- Active visible Canvas Button state sweep completed across `Assets/Game/Prefabs/UI`: secondary/reference popups, shell popups, SCN-08 Build Placement Confirmation Bar, SCN-08 Full Map, and SCN-09 Build Drawer now have non-empty highlighted, pressed, selected, and disabled SpriteSwap states using existing Target Lock rect/square/scn09 card/button state sprite pairs. The audit reports `0` active visible Button issues; remaining Button issues are intentionally excluded transparent `Hotspot` route hitboxes or inactive prototype objects.
- SCN-08 Build Placement Confirmation Bar sprite-only pass completed without hierarchy or layout edits. The serialized panel frame, instruction strip, status chip, cancel/confirm rect buttons, and rotate square button now point to the SCN-08 V02 rail/chip/rect/square chrome family; active visible button state audit reports `0` issues for the bar.
- SCN-09 Build Drawer sprite-only pass completed without drawer structure or behavior edits. The remaining old `Assets/Game/Art/UI/Panels` card/detail/button sprites were normalized to the existing generated `BuildDrawer/LayeredOneGo` drawer chrome where direct equivalents exist, with shared SCN-08 V02 square chrome only for the close button state. Active visible button state audit reports `0` issues for the drawer.
- Shadow validation correction: `-nographics` route captures produced false-positive gray screenshots, so the accepted evidence for this slice is graphics-enabled shadow batchmode capture only.
- SCN-08/SCN-09 graphics-enabled shadow captures passed after the sprite-only pass. Evidence: Match HUD `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-08_MatchHUD/iteration_02/shadow_canvas_scn08_match_hud_sprite_pass_graphics_1920x1080.png`, Build Drawer `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-09_BuildDrawer/iteration_01/shadow_canvas_scn09_build_drawer_sprite_pass_graphics_1920x1080.png`, and Build Placement Bar all-aspect contact `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-08_BuildPlacementConfirmationBar/iteration_01/scn08_build_placement_bar_sprite_pass_all_aspect_contact.png`.
- Focused crop contacts were saved for the required command buttons, squad cards, drawer cards, drawer detail/queue sections, and build placement rail/button families. The squad-card crop shows no health/progress/value text overlap with the card chrome in the current seeded data.
- SCN-09 behavior-only checks were validated without runtime or structure edits and recorded at `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/SCN-09_BuildDrawer/iteration_01/scn09_build_drawer_behavior_validation_note.md`. The current capture shows the empty production state, while existing runtime code paths show category changes repopulate card thumbnails, selected items update detail preview imagery, and no active card button/control chrome is clipped in the visible scroll rows.
- POP-05 Mission Result active/runtime usage was reconciled: `UIShellAppCanvas.prefab` binds `Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab`, while `Assets/Game/Prefabs/UI/Shell/Popups/POP05_MissionResultPopup.prefab` remains reference/shell material. The active prefab had missing legacy sprite GUIDs and rendered as a white modal in the shadow capture; those missing structural/button/icon references were replaced with existing Target Lock sprite references without hierarchy, anchor, C#, or layout edits.
- POP-05 graphics-enabled shadow captures now pass for the active runtime modal after the sprite-only repair. Evidence: `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/POP-05_MissionResult/iteration_01/shadow_canvas_pop05_mission_result_small_check_1920x1080.png`.
- Confirm Raid active modal sprite-only pass completed without hierarchy, anchor, C#, or layout edits. Missing legacy sprite references were replaced with existing Target Lock panel, row, button, and warning/icon sprites; the first capture exposed an accidental full-modal gold backing from an overbroad progress-fill sprite choice, which was corrected to dark panel chrome. Evidence: `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/ConfirmRaid/iteration_01/shadow_canvas_confirm_raid_sprite_pass_dark2_1920x1080.png`.
- Intel Reveal active modal sprite-only pass completed without hierarchy, anchor, C#, or layout edits. Missing legacy sprite references were replaced with existing Target Lock panel, row, thumbnail, action button, close button, warning, scan, and resource/icon sprites; the active visible button state audit remains clean. Evidence: `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/IntelReveal/iteration_01/shadow_canvas_intel_reveal_sprite_pass_1920x1080.png`.
- End Of Day Report active modal sprite-only pass completed without hierarchy, anchor, C#, or layout edits. Missing legacy sprite references were replaced with existing Target Lock frame, status row, meter, fill, resource, action button, and icon sprites; the active visible button state audit remains clean. Evidence: `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/EndOfDayReport/iteration_01/shadow_canvas_end_of_day_sprite_pass_1920x1080.png`.
- Pause Menu secondary/reference modal sprite-only pass completed without hierarchy, anchor, C#, or layout edits. Missing legacy sprite references were replaced with existing Target Lock frame, resume/action button, secondary button, close/settings/pause/scan/time icons, and panel sprites; the active visible button state audit remains clean. Evidence: `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/PauseMenu/iteration_01/shadow_canvas_pause_menu_sprite_pass_1920x1080.png`.
- Still wrong / next iteration: SCN-08 full HUD panel-by-panel validation remains open because the right quick-rail family can still clip at the right edge in the 1920x1080 static capture, which is a layout/anchoring issue outside the user's latest sprite-only scope. POP-05 Mission Result is no longer white/broken, but the active legacy modal still has oversized/cropped top victory imagery and cramped content scale caused by existing RectTransforms; do not fix these by structure changes unless the user reopens layout work. Confirm Raid is sprite-repaired, but its thumbnail/detail composition remains inherited from the existing modal layout and is not target-matched panel-by-panel under this scope. Intel Reveal and End Of Day Report are sprite-repaired, but their current modal captures are top-cropped by inherited prefab layout; do not fix by moving/resizing objects unless structure work is reopened. Pause Menu is sprite-repaired, but the command icons are oversized by inherited RectTransforms and were not resized under sprite-only scope. Next tracker action is the remaining Phase 5 secondary/reference popup sprite-only pass, starting with Threat Alert or Reward Unlock.

## Decision

Canvas is the preferred runtime target for this migration because the recent UI Toolkit Target Lock implementation is visually useful but has shown heavy frame cost on the main menu. This tracker ports the look, not the UI Toolkit runtime architecture.

The implementation should therefore favor:

- existing Canvas prefabs and scene bindings;
- sliced sprites, sprite states, Canvas Selectable transitions, and prefab variants;
- stable CanvasScaler behavior across aspect ratios;
- low rebuild cost and low overdraw;
- no per-frame visual scripts unless already present and justified.

## Active Canvas Scope

These are the active Canvas shell and modal prefabs confirmed from scene and prefab bindings:

| Surface | Canvas prefab | Reference source |
| --- | --- | --- |
| Shell | `Assets/Game/Prefabs/UI/Shell/UIShellAppCanvas.prefab` | Approved shared shell/chrome contract |
| SCN-01 Loading | `Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab` | `Design/VisualLockLayered/SCN-01_SplashLoading/reference/SCN-01_SplashLoading_NewMainMenuArtDirection_TargetLock_V04.png` |
| SCN-02 Main Menu | `Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab` | `Design/VisualLockLayered/SCN-02C_MainMenuBrightCommand/reference/scn02c_target_lock_warline_capture_bright.png` |
| SCN-03 Commander Profile | `Assets/Game/Prefabs/UI/Shell/Content/SCN03_CommanderProfileContent.prefab` | `Design/VisualLockLayered/SCN-03_CommanderProfile/reference/SCN-03_CommanderProfile_NewMainMenuArtDirection_TargetLock_V01.png` |
| SCN-08 Match HUD | `Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab` | `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_NewMainMenuArtDirection_TargetLock_V02.png` |
| SCN-08 Build Placement Bar | `Assets/Game/Prefabs/UI/Shell/Content/SCN08_BuildPlacementConfirmationBar.prefab` | `Design/VisualLockLayered/SCN-08_BuildPlacementConfirmationBar/reference/SCN-08_BuildPlacementConfirmationBar_NewMainMenuArtDirection_TargetLock_V01.png` |
| SCN-09 Build Drawer Popup | `Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab` | `Design/VisualLockLayered/SCN-09_BuildDrawer/reference/SCN-09_BuildDrawer_NewMainMenuArtDirection_TargetLock_V03.png` |
| SCN-19 Armory | `Assets/Game/Prefabs/UI/Shell/Content/SCN19_ArmoryContent.prefab` | `Design/VisualLockLayered/SCN-19_Armory/reference/SCN-19_Armory_NewMainMenuArtDirection_TargetLock_V04.png` |
| POP-05 Mission Result | `Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab` | `Design/VisualLockLayered/POP-05_MissionResult/reference/POP-05_MissionResult_NewMainMenuArtDirection_TargetLock_V01.png` |
| Confirm Raid | `Assets/Game/Prefabs/UI/Popups/ConfirmRaidPopup.prefab` | Use shared Target Lock modal style until a specific reference exists |
| End Of Day Report | `Assets/Game/Prefabs/UI/Popups/EndOfDayReportPopup.prefab` | Use shared Target Lock modal style until a specific reference exists |
| Intel Reveal | `Assets/Game/Prefabs/UI/Popups/IntelRevealPopup.prefab` | Use shared Target Lock modal style until a specific reference exists |

Commander Profile reachability note:

- `Assets/Game/Prefabs/UI/Shell/Content/SCN03_CommanderProfileContent.prefab` exists as a Canvas prefab and remains in this migration scope for art-direction parity.
- It is not currently installed by `UIShellContentView`; the live Canvas content system exposes Loading, Main Menu, Armory, Match HUD, Build Drawer, and Build Placement Bar only.
- The current `UIRouterView.screenPrefabs` entries in `UIShellAppCanvas.prefab` and `Menu.unity` reference GUIDs that are not present as asset `.meta` files under `Assets`, so the legacy router path is not reliable for SCN-03 capture.
- UI Toolkit mounts Commander Profile through `UIRoute.CommandFeed`; Canvas has no equivalent active route install path at Phase 0.

Secondary or reference Canvas popup prefabs discovered during Phase 0 inventory:

| Surface | Canvas prefab | Status rule |
| --- | --- | --- |
| Ability Upgrade Detail | `Assets/Game/Prefabs/UI/Popups/AbilityUpgradeDetailPopup.prefab` | Audit active usage before styling |
| Build Placement Panel | `Assets/Game/Prefabs/UI/Popups/BuildPlacementPanel.prefab` | Audit overlap with shell build placement bar |
| Pause Menu | `Assets/Game/Prefabs/UI/Popups/PauseMenuPopup.prefab` | Style if still active in match flow |
| Popup Frame | `Assets/Game/Prefabs/UI/Popups/PopupFrameView.prefab` | Prefer as shared popup chrome foundation |
| Reward Unlock | `Assets/Game/Prefabs/UI/Popups/RewardUnlockPopup.prefab` | Audit active usage before styling |
| Threat Alert | `Assets/Game/Prefabs/UI/Popups/ThreatAlertPopup.prefab` | Style if still active in match flow |
| Reference POP-05 shell prefab | `Assets/Game/Prefabs/UI/Shell/Popups/POP05_MissionResultPopup.prefab` | Not bound by Canvas runtime at Phase 0; keep as visual/reference material unless later wired |

Settings and Inbox were checked during Phase 0. No active Canvas `Settings` or `Inbox` prefab was found under `Assets/Game/Prefabs/UI`; both are currently UI Toolkit-only popup assets in `UiToolkitShellView`.

## Phase 0 Inventory Notes

2026-06-22 slice 01:

- `Assets/Game/Data/UI/RuntimeUiConfig.asset` is set to `mode: 0`, so `MenuBootstrapView.ApplyRuntimeUiMode()` enables the Canvas path by default and disables the UI Toolkit shell root/document.
- `Assets/Game/Scenes/Menu.unity` binds `MenuBootstrapView` to `RuntimeUiConfig`, `uiCanvas`, `uiToolkitDocument`, `uiToolkitShellRoot`, and `uiToolkitShellView`.
- `UIShellContentView` scene fields confirm the active Canvas route prefabs: loading, main menu, armory, match HUD, build drawer popup, and build placement confirmation bar.
- `UIShellAppCanvas.prefab` confirms additional active Canvas modal bindings through `WarlineCaptureMatchResultFlow` and `WarlineCaptureOperationModalFlow`: mission result, confirm raid, end-of-day report, and intel reveal.
- `UIShellAppCanvas.prefab` currently uses CanvasScaler Scale With Screen Size, reference resolution `1672x941`, screen match mode `MatchWidthOrHeight`, match `0.5`, and reference pixels per unit `100`.
- Active Canvas runtime-bound component classes found on shell/content/modal prefabs: `ArmoryCatalogItemView`, `ArmoryInspectionPanelView`, `ArmoryRightContentView`, `BattleHudRuntimeFeedbackView`, `BuildDrawerCatalogRuntimeView`, `BuildDrawerItemView`, `BuildDrawerQueueItemView`, `BuildDrawerView`, `BuildPlacementConfirmationBarView`, `MainMenuNavigationView`, `MatchHudFooterContentView`, `MatchHudMinimapView`, `MatchHudObjectivesElapsedView`, `MatchHudRightQuickRailView`, `MatchHudSelectionPanelView`, `MatchHudSquadTrayView`, `MatchHudTransportPassengerDrawerView`, `MatchHudTransportPassengerItemView`, `MatchOverlayCommandControlsView`, `MatchOverlayCommandTabGroupView`, `UIAccessibilityApplier`, `UIModalView`, `UIPopupCloseButtonView`, `UIPopupCloseView`, `UIRouterView`, `UISafeAreaView`, `UIShellContentSectionsView`, `UIShellLoadingProgressView`, `UIShellRouteButtonView`, `WarlineCaptureMatchResultFlow`, `WarlineCaptureOperationModalFlow`, and `WarlineCaptureShellResultConfirmButtonView`.
- Runtime-bound section/component names must be preserved during visual work, especially shell region sections, `SCN09_BuildDrawerPopup`, build drawer tabs/cards/queue/detail controls, `SCN08_MatchHudContent`, command controls, squad tray, minimap, right quick rail, `SCN19_ArmoryContent`, armory catalog and inspection panel roots, and modal popup roots.

2026-06-22 slice 02:

- Baseline inventory artifact created at `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/phase0_canvas_inventory.md`.
- Protected serialized fields and GameObject names are recorded there before any Canvas prefab visual edits.
- CanvasScaler inventory is recorded there. The Menu scene runtime canvas uses `4800x2160`, while the `UIShellAppCanvas.prefab` source still uses `1672x941`; future size tuning must validate against the live scene canvas, not prefab preview alone.

2026-06-22 slice 03:

- `CanvasMenuFallbackValidation.Run` now accepts editor-only screenshot path and resolution environment variables while preserving the old defaults.
- The updated editor-only tool was synced to `/Users/farhad/Projects/WarlineCapture-CodexUnity1` for validation.
- Shadow Canvas main menu/deploy UI validation passed at `1280x720` (`luma=0.103`), `1920x1080` (`luma=0.092`), and `4800x2160` (`luma=0.111`).
- Captures and logs are saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- Scope note: these captures prove the Canvas main menu/deploy UI path only; the all-active-surface screenshot gates remain open.

2026-06-22 slice 04:

- `CanvasMenuFallbackValidation.RunRouteCapture` added as editor-only validation tooling.
- The route capture helper accepts `WARLINE_CANVAS_ROUTE`, `WARLINE_CANVAS_POPUP`, `WARLINE_CANVAS_SCREENSHOT_PATH`, `WARLINE_CANVAS_SCREENSHOT_WIDTH`, and `WARLINE_CANVAS_SCREENSHOT_HEIGHT`.
- The helper drives only existing Canvas presentation methods: menu route body swaps, Match HUD command presentation, and Build Drawer popup install. It does not change runtime gameplay/UI behavior.
- The updated editor-only tool was synced to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Shadow route capture passed for `Armory` at `4800x2160` (`luma=0.112`), `Match` at `4800x2160` (`luma=0.055`), and `Match + BuildDrawer` at `4800x2160` (`luma=0.106`).
- Captures and logs are saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- Scope note: Loading, Commander Profile, Build Placement Bar, secondary/reference popups, 1920x1080 route captures, wide-aspect captures, and FPS/rebuild baselines remain open.

2026-06-22 slice 05:

- `CanvasMenuFallbackValidation.RunRouteCapture` now supports the existing Canvas `ShowLoading` command through `WARLINE_CANVAS_ROUTE=Splash`.
- The updated editor-only tool was synced to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Shadow route capture passed for `Splash`/SCN-01 Loading at `4800x2160` (`luma=0.375`).
- Captures and logs are saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- Scope note: Commander Profile, Build Placement Bar, secondary/reference popups, 1920x1080 route captures beyond Main Menu, wide-aspect captures, and FPS/rebuild baselines remained open after this slice.

2026-06-22 slice 06:

- `CanvasMenuFallbackValidation.RunRouteCapture` now supports `WARLINE_CANVAS_OVERLAY=BuildPlacementBar`.
- The overlay capture binds a fake editor-only `IBuildingUiCommand` to the existing `BuildPlacementConfirmationBarView` after Match HUD install, so the placement bar can render for baseline evidence without entering gameplay placement or changing runtime behavior.
- The updated editor-only tool was synced to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Shadow overlay capture passed for `Match + BuildPlacementBar` at `4800x2160` (`luma=0.068`).
- Captures and logs are saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- Scope note: Commander Profile reachability, secondary/reference popups, 1920x1080 route captures beyond Main Menu, wide-aspect captures, and FPS/rebuild baselines remain open.

2026-06-22 slice 07:

- Commander Profile reachability was audited before capture work continued.
- `SCN03_CommanderProfileContent.prefab` remains a Canvas art-direction target, but it is not mounted by the current Canvas `UIShellContentView`; UI Toolkit owns the live `CommandFeed` commander profile route.
- Legacy `UIRouterView.screenPrefabs` GUID references in `UIShellAppCanvas.prefab` and `Menu.unity` do not resolve to asset `.meta` files under `Assets`, so the legacy Canvas router cannot be used as authoritative capture evidence.
- Shadow route capture passed at `1920x1080` for `Splash`/SCN-01 Loading (`luma=0.357`), `Armory` (`luma=0.092`), `Match` (`luma=0.044`, using the lower static-HUD threshold), `Match + BuildDrawer` (`luma=0.089`), and `Match + BuildPlacementBar` (`luma=0.055`).
- Captures and logs are saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- Scope note: secondary/reference popups, wide-aspect captures, and FPS/rebuild baselines remain open.

2026-06-22 slice 08:

- Shadow wide-aspect captures passed at `2400x1080` for main menu/deploy UI (`luma=0.103`), `Splash`/SCN-01 Loading (`luma=0.377`), `Armory` (`luma=0.160`), `Match` (`luma=0.059`), `Match + BuildDrawer` (`luma=0.109`), and `Match + BuildPlacementBar` (`luma=0.068`).
- Captures and logs are saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- Scope note: secondary/reference popups and FPS/rebuild baselines remain open.

2026-06-22 slice 09:

- `CanvasMenuFallbackValidation.RunRouteCapture` now supports `WARLINE_CANVAS_MODAL` for editor-only popup prefab screenshot baselines.
- Supported modal keys are `MissionResult`, `ConfirmRaid`, `EndOfDayReport`, `IntelReveal`, `AbilityUpgradeDetail`, `BuildPlacementPanel`, `PauseMenu`, `PopupFrame`, `RewardUnlock`, and `ThreatAlert`.
- The modal capture helper instantiates the configured popup prefab under the active Canvas in PlayMode only; it does not change runtime route, gameplay, ECS, or prefab behavior.
- Shadow modal captures passed at `4800x2160` for `MissionResult` (`luma=0.986`), `ConfirmRaid` (`luma=0.976`), `EndOfDayReport` (`luma=0.954`), and `IntelReveal` (`luma=0.966`).
- High luma is expected from the current baseline because these modal prefabs are still mostly light placeholder styling; Target Lock styling remains future work.
- Captures and logs are saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- Scope note: secondary/reference popup captures or usage decisions and FPS/rebuild baselines remain open.

2026-06-22 slice 10:

- Secondary/reference popup usage was audited before styling work.
- `AbilityUpgradeDetailPopup`, `BuildPlacementPanel`, and `PopupFrameView` are not directly installed by the active Canvas `UIShellContentView` route/popup path.
- `Pause`, `ThreatAlert`, and `RewardUnlock` exist in `UiShellPopupKind`/ECS popup requests, but the current Canvas content view only installs the `BuildDrawer` popup prefab directly; these popup prefabs therefore need a wiring/usage decision before final art polish is treated as runtime-active.
- All six secondary/reference popup prefab baselines were still captured for visual decision material in the shadow project at `4800x2160`: `AbilityUpgradeDetail` (`luma=0.995`), `BuildPlacementPanel` (`luma=0.679`), `PauseMenu` (`luma=0.514`), `PopupFrame` (`luma=0.049`), `RewardUnlock` (`luma=0.928`), and `ThreatAlert` (`luma=0.617`).
- `PopupFrame` uses a lower screenshot luma threshold because it is intentionally sparse shared chrome; this only affects editor-only baseline validation.
- Captures and logs are saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- Scope note: FPS measurements and Canvas rebuild/draw-call baselines remain open before visual prefab edits begin.

2026-06-22 slice 11:

- `CanvasMenuFallbackValidation.RunCanvasPerformanceBaseline` was added as editor-only baseline tooling.
- The helper opens the Menu scene, forces Canvas mode, installs either Main Menu or Match HUD, and measures a fixed warmup/sample window with the runtime Canvas active or disabled.
- Shadow performance baselines passed with `90` warmup frames and `240` sample frames: Main Menu Canvas active (`avgMs=0.434`, `fps=2303.0`, `p95Ms=0.513`), Main Menu Canvas disabled (`avgMs=0.678`, `fps=1475.1`, `p95Ms=1.303`), Match HUD Canvas active (`avgMs=0.611`, `fps=1637.4`, `p95Ms=0.916`), and Match HUD Canvas disabled (`avgMs=0.890`, `fps=1124.2`, `p95Ms=1.121`).
- These are relative editor batchmode smoke baselines, not real Game View/device FPS. The values are intentionally recorded only to catch large regressions during Canvas prefab styling.
- Unity render `ProfilerRecorder` counters returned `0.0` for draw calls, batches, SetPass, triangles, and vertices in this batchmode path; draw-call proof must come from Game View/Frame Debugger if needed.
- No Canvas rebuild warnings were emitted in the captured performance logs; Unity domain reload `RebuildCommonClasses` lines are editor startup noise and not Canvas rebuild warnings.
- Logs are saved under `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- Scope note: Phase 0 evidence gates are complete; next work can begin with shared Canvas chrome and sprite-state foundation.

2026-06-22 slice 12:

- Shared Canvas chrome mapping was created at `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/shared/canvas_shared_chrome_asset_map.md`.
- The map ties approved SCN-02 UI Toolkit sprites to Canvas usage for header frames, logo, resource chips, header icon buttons, left navigation, card frames, label plates, HUD panels, rectangular buttons, square buttons, Build Drawer tabs, and Build Drawer card highlights.
- Import audit notes confirm the SCN-02 shared menu chrome is already imported under `Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/`, HUD/shared utility chrome under `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/`, and Build Drawer chrome under `Assets/Game/Art/UI/Generated/BuildDrawer/LayeredOneGo/`.
- PPU audit note: SCN-02 shared chrome originally mixed `spritePixelsToUnits: 100` with several `300` HUD/button-style imports. The SCN-02 structural frame sprites used by the Canvas main menu are now normalized to `300` after screenshot evidence showed oversized chrome at `100`; do not normalize unrelated screens globally until their own Canvas screenshots prove the mismatch causes distortion.
- 9-slice audit note: SCN-02 frame assets already have usable sprite borders; some Build Drawer tab/card sprites have zero borders and must be used fixed-size or border-tuned before heavy stretching.
- Scope note: no runtime, scene, prefab, or UI Toolkit files were changed in this slice; only mapping documentation was added.

2026-06-22 slice 13:

- `Assets/Game/Prefabs/UI/Components/MainMenuLeftNavButton.prefab` was updated as the shared Canvas left-nav state seed.
- The state seed now uses `scn02c_nav_button_frame_default.png` for the normal/disabled frame and `scn02c_nav_button_frame_selected.png` for highlighted, pressed, and selected states.
- The Button target graphic now points at the full `Frame` Image instead of the transparent removed `Hotspot` Image, so hover/selected states replace the whole chrome frame.
- Active left-nav instances in `SCN02_MainMenuContent.prefab` and `SCN19_ArmoryContent.prefab` were updated as well because both screen prefabs remove the source `Hotspot` object and add their own Button components on the instance root.
- Existing static selected-route intent was preserved by replacing old `scn02_nav_button_selected_frame.png` overrides with the approved `scn02c_nav_button_frame_selected.png`; old inactive-frame overrides were replaced with `scn02c_nav_button_frame_default.png`.
- Import decision: no sprite import changes were needed in this slice. The SCN-02 nav and mode-card frames already have mipmaps enabled for scaled chrome, while thinner header/resource/HUD button frames keep mipmaps disabled and default uncompressed texture settings for sharp edges.
- Shared chrome contact sheet saved at `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/shared/canvas_shared_chrome_contact_sheet.png`.
- Shadow validation passed for the shared left-nav state seed at `4800x2160` on Main Menu (`shadow_scn02_left_nav_state_seed_4800x2160.png`, `luma=0.122`) and Armory (`shadow_scn19_left_nav_state_seed_4800x2160.png`, `luma=0.140`) in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- `git diff --check` passed after the prefab and tracker updates.
- Scope note: only Canvas prefabs, tracker markdown, and visual evidence artifacts changed; no runtime C#, scene, gameplay, ECS, route behavior, or UI Toolkit files were edited.

## Allowed Write Scope

Allowed by default:

- `Assets/Game/Prefabs/UI/**/*.prefab`
- `Assets/Game/Art/UI/**/*.png`
- `Assets/Game/Art/UI/**/*.png.meta`
- existing UI sprite/font/material assets under `Assets/Game/**/UI/**` when the asset is already used by Canvas UI;
- Canvas-only animation controllers or transition assets only when they already belong to the target UI prefab family;
- `Design/Architecture/ui_canvas_target_lock_art_direction_tracker.md`
- `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/**`
- narrowly scoped editor-only screenshot/validation tooling when needed for static Canvas preview evidence.

Forbidden unless separately approved:

- gameplay, ECS, composition, match logic, production logic, or route behavior changes;
- UI Toolkit UXML/USS changes as part of this Canvas migration;
- scene rewiring outside the target UI Canvas/prefab validation path;
- replacing live UI with a baked full-screen screenshot;
- adding new `Update`, `LateUpdate`, coroutine polling, or runtime visual controllers;
- changing data values to make a visual mockup look right;
- deleting UI Toolkit work or Canvas fallback assets.

## Shared Art Direction Rules

These rules override pixel-level mockup matching when they conflict:

- Keep the approved SCN-02 main menu header/chrome unchanged for main-menu-adjacent Canvas screens.
- Do not create, restyle, resize, or replace per-screen menu headers; ignore mockup header differences outside the approved shared header.
- Reuse the approved SCN-02 left navigation style for main-menu-adjacent Canvas screens; only icons, labels, and active route change.
- Match HUD owns its own gameplay header and may differ from menu chrome.
- If a reference uses one large baked multi-section background, rebuild it as separate Canvas panels like the approved UI Toolkit SCN-02 right commander area.
- Every button-like or selectable control family must have visible default, hover/focus, selected/current, disabled, and pressed/impact states.
- Selected and hover states should be chrome-level state sprites or full-frame state treatments, not small translucent overlays.
- Repeated cards/buttons must use one template; a highlighted mockup card is a reusable state example, not a one-off layout.
- Text must be readable at all target aspects, and button captions must remain fully visible.
- Padding must be symmetrical inside repeated components unless the mockup and data justify an explicit exception.

## Canvas Performance Rules

Canvas migration is only successful if the UI remains cheap enough at runtime.

- Keep static backgrounds out of high-rebuild Canvas groups where practical.
- Do not place huge full-screen transparent images over the entire screen unless they are necessary and batched.
- Prefer sliced sprites over multiple stacked decorative images.
- Avoid nested LayoutGroups on hot, frequently updated panels unless the panel is small and measured.
- Avoid ContentSizeFitter/LayoutElement combinations that rebuild every frame.
- Split dynamic panels from static chrome so data updates do not dirty the whole screen.
- Use atlased sprites and compatible materials where possible.
- Use mipmaps only for large sprites that are scaled down materially; do not blur small icons.
- Record FPS and profiler observations before and after each major surface pass.
- Compare active Canvas FPS against the same scene with the target UI object disabled when investigating regressions.

## Validation Loop

Use this loop for every screen or popup:

1. Inspect the active Canvas prefab, runtime bindings, and current screenshot before editing.
2. Identify the matching UI Toolkit approved surface and Target Lock reference.
3. Classify mismatches as `sprite`, `9-slice`, `PPU`, `layout`, `padding`, `font`, `state`, `responsive`, `content`, `performance`, or `artifact`.
4. Fix sprite import, Pixel Per Unit, and 9-slice issues before compensating with layout.
5. Apply one coherent visual-only prefab/art slice.
6. Sync allowed files to `/Users/farhad/Projects/WarlineCapture-CodexUnity1` when available.
7. Validate static Canvas/Game View captures in the shadow project first.
8. Capture at least `4800x2160`, `1920x1080`, and one wide aspect used by the project when the screen is responsive.
9. Create focused crops for every major panel family, repeated card family, and button family.
10. Compare against the mockup and the approved UI Toolkit screen.
11. Run `git diff --check`.
12. Update this tracker with progress, artifact paths, and validation status.
13. Continue only when the current surface passes a full panel-by-panel visual audit or has a recorded user-approved exception.

Strict completion gates for every screen:

- The screen is not complete after one good screenshot. Continue the loop until the capture is visually matched against the approved target or an explicit user exception is recorded.
- Every visible panel is audited separately: header, nav, middle/content, right side, footer, modal/popup, repeated cards, and every nested sub-panel.
- Every button/selectable family is audited separately: default, hover/highlight, selected/current, pressed/impact, disabled, icon, text alignment, padding, target graphic, and state sprite coverage.
- All visible legacy sprites from the previous art direction must be replaced. A screen may keep only invisible runtime placeholders, transparent raycast images, or documented runtime-only references that do not render.
- Before marking a screen complete, run a sprite GUID audit for that prefab and record any remaining non-Target-Lock sprite references with a reason.
- If the approved UI Toolkit screen uses different shared chrome than the raw mockup, the approved UI Toolkit chrome wins.
- If a screenshot passes the technical capture helper but still has old sprites, wrong iconography, poor alignment, unreadable text, or missing button states, it remains in progress.

SCN-02 Main Menu strict loop:

- Use the UI Toolkit-approved SCN-02 main menu as the primary visual baseline.
- Use the correct UI Toolkit-approved logo lockup from `Assets/Game/Art/UI/Generated/SplashLoading/TargetLockV04Imagegen/Sprites/scn01_v04_logo_lockup.png`.
- Replace all visible old MainMenuV15C/Synty sprites in the Canvas prefab with SCN-02C Target Lock sprites or documented shared Target Lock chrome.
- Nav labels/icons must match the approved SCN-02 family, not the legacy Canvas labels.
- Header resource chips, action buttons, left nav, center cards, right commander panel, footer/deploy CTA, and all button states must pass focused crop review.
- Do not ask for approval until the strict gates pass; approval is only for the final candidate capture, not for stopping the loop early.

## Phase 0 - Inventory, Baseline, And Safety

Goal:
Know exactly which Canvas surfaces are active, how they are bound, and what the current performance/visual baseline is before styling.

- [x] Confirm all active Canvas shell content prefabs and popup prefabs from scene and route bindings.
- [x] Confirm whether Settings and Inbox have active Canvas prefabs or are UI Toolkit-only.
- [x] Inventory runtime-bound component scripts on every active Canvas prefab.
- [x] Record which serialized field names and GameObject names must not be renamed.
- [x] Inventory current CanvasScaler settings on menu and match canvases.
- [x] Capture baseline 4800x2160 Canvas screenshots for all active shell surfaces.
- [x] Capture baseline 1920x1080 Canvas screenshots for all active shell surfaces.
- [x] Capture baseline wide-aspect Canvas screenshots for all active shell surfaces.
- [x] Capture baseline screenshots for all active secondary popups.
- [x] Capture current FPS for menu Canvas active vs Canvas disabled.
- [x] Capture current FPS for match HUD Canvas active vs Canvas disabled.
- [x] Record current draw calls, batches, and Canvas rebuild warnings where available.
- [x] Create `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/baseline/`.
- [x] Save baseline captures and notes under the Canvas visual match folder.
- [x] Run `git diff --check` before implementation edits.

Acceptance:

- Active Canvas targets are known.
- Baseline visuals and performance are captured.
- No prefab editing starts from guesswork.

## Phase 1 - Shared Canvas Chrome And Asset Foundation

Goal:
Create the reusable Canvas art foundation before per-screen tuning.

- [x] Map approved UI Toolkit SCN-02 header sprites to Canvas Image/Sliced Image usage.
- [x] Map approved UI Toolkit SCN-02 left nav sprites to Canvas button templates.
- [x] Map shared panel, card, chip, divider, tab, and square-button sprites.
- [x] Identify which Target Lock art is already imported for Canvas and which needs import/meta tuning.
- [x] Audit Pixel Per Unit for every shared Canvas chrome sprite.
- [x] Audit 9-slice borders for every shared Canvas frame/button/card sprite.
- [x] Enable mipmaps only for large scaled-down background/chrome sprites that need them.
- [x] Confirm texture compression keeps thin Target Lock chrome sharp.
- [x] Create or update a shared Canvas popup frame using `PopupFrameView` where active.
- [x] Create or update a shared Canvas button state set: default, hover, selected, disabled, pressed.
- [x] Create or update a shared Canvas card state set: default, hover, selected, disabled, pressed.
- [x] Verify shared state sprites cover the whole chrome frame, not only inner content.
- [x] Confirm static shared chrome can batch cleanly with existing Canvas materials.
- [x] Save a shared chrome contact sheet under `_CanvasTargetLockVisualMatch/shared/`.
- [x] Run `git diff --check`.

Acceptance:

- Shared visual primitives exist before screen-specific copies multiply.
- PPU and 9-slice decisions are recorded.

## Phase 2 - Shell, Header, Left Navigation, And Global Background

Goal:
Make the Canvas shell match the approved Target Lock visual language while preserving the shell structure.

- [ ] Update `UIShellAppCanvas.prefab` static background strategy without increasing menu overdraw unnecessarily.
- [x] Port the approved SCN-02 logo/header treatment into Canvas shell/header regions.
- [x] Port the approved SCN-02 left navigation background into Canvas.
- [x] Update `MainMenuLeftNavButton.prefab` to use the shared Target Lock button states.
- [x] Validate menu-adjacent screens keep the locked SCN-02 main menu header unchanged before each surface is counted target-matched.
- [x] Confirm menu-adjacent screens reuse the same left navigation prefab/style.
- [x] Keep Match HUD excluded from menu header/nav reuse.
- [x] Validate left nav does not overlap the middle region at 4800x2160.
- [x] Validate left nav does not overlap the middle region at 1920x1080.
- [x] Validate header text/logo scale does not become oversized at lower resolutions.
- [x] Capture shell/header/nav focused crops.
- [x] Run `git diff --check`.

Acceptance:

- Shared shell chrome is visually consistent and responsive.
- Header/nav can be reused by later screen passes.

## Phase 3 - Menu Screens

Goal:
Update Canvas menu screens using the shared shell, header, and left nav baseline.

- [x] SCN-02 Main Menu: update center mode cards to approved Target Lock card style.
- [x] SCN-02 Main Menu: replace all visible legacy MainMenuV15C/Synty sprites with Target Lock sprites or documented shared Target Lock chrome.
- [x] SCN-02 Main Menu: update header resource chips and header action buttons to the approved SCN-02C sprite family.
- [x] SCN-02 Main Menu: tune header resource chips, plus buttons, Inbox/Settings buttons, and spacing until they match the approved Target Lock header style while preserving the user-approved two-button Canvas header action rule.
- [x] SCN-02 Main Menu: update left navigation labels/icons/chrome to match the approved SCN-02 UI Toolkit main menu family.
- [x] SCN-02 Main Menu: resolve the approved sixth `PROFILE` nav-row parity either by safely adding it or recording an explicit user-approved exception.
- [x] SCN-02 Main Menu: tune mode-card badge, lower plate, title, star/divider, and image spacing from focused crops, not only full-screen review.
- [x] SCN-02 Main Menu: update right commander panel as separate live Canvas panels, not a baked multi-section image.
- [x] SCN-02 Main Menu: tune commander portrait, identity row, readiness row, faction-standing row, and row values to match the approved separate-panel structure.
- [x] SCN-02 Main Menu: update footer/deploy controls with full interaction states.
- [x] SCN-02 Main Menu: verify every button/selectable has default, hover/highlight, selected/current, pressed/impact, and disabled state coverage.
- [x] SCN-02 Main Menu: run and record the prefab sprite GUID audit; document any remaining non-Target-Lock sprite references.
- [x] SCN-02 Main Menu: rerun the sprite GUID audit after final visual edits and confirm every remaining non-Target-Lock sprite is invisible, transparent raycast-only, or documented runtime-only.
- [x] SCN-02 Main Menu: reject any candidate that still shows old sprites, mismatched chrome, partial hover overlays, unreadable text, asymmetric padding, or button states that do not cover the full frame.
- [x] SCN-02 Main Menu: capture focused crops for header, left nav, mode cards, right commander panel, footer/deploy CTA, and button states.
- [x] SCN-02 Main Menu: compare final candidate against the approved UI Toolkit SCN-02 capture and the Target Lock reference.
- [x] SCN-02 Main Menu: validate readable text and clean panel alignment at all target aspects.
- [x] SCN-02 Main Menu: user approves final candidate capture before the surface is counted as target-matched.
- [x] SCN-03 Commander Profile: reuse shared header and left nav.
- [x] SCN-03 Commander Profile: split profile/stat/loadout areas into clean panel sections.
- [x] SCN-03 Commander Profile: update portrait, rank, stats, and action buttons.
- [x] SCN-03 Commander Profile: validate repeated rows and action states.
- [x] SCN-19 Armory: reuse shared header and left nav.
- [x] SCN-19 Armory: update catalog cards with full default/hover/selected/disabled/pressed states.
- [x] SCN-19 Armory: update right inspection panel as separate live sections.
- [x] SCN-19 Armory: ensure right-side buttons are readable, large enough, and visible.
- [x] SCN-19 Armory: validate tabs update visually without layout shifts.
- [x] SCN-19 Armory: validate card portraits and selected detail imagery stay live.
- [x] Capture focused crops for every menu panel family.
- [x] Run `git diff --check`.

Acceptance:

- Menu screens look like one product family.
- No screen carries a one-off header or left navigation style.

## Phase 4 - Match HUD And Gameplay Canvas Surfaces

Goal:
Update gameplay Canvas surfaces without hurting runtime performance or gameplay bindings.

- [x] SCN-08 Match HUD: inventory every runtime-bound HUD element name before editing.
- [x] SCN-08 Match HUD: update unique gameplay header/resources/current-order area.
- [x] SCN-08 Match HUD: update selected-unit/selection details panel.
- [x] SCN-08 Match HUD: update objectives/status panels.
- [x] SCN-08 Match HUD: update minimap and right quick-rail panels.
- [x] SCN-08 Match HUD: update command buttons with visible hover/selected/focus/press impact states.
- [x] SCN-08 Match HUD: update all squad cards from one repeated template.
- [x] SCN-08 Match HUD: ensure selected squad state is a full chrome state, not a partial overlay.
- [x] SCN-08 Match HUD: ensure squad card health/progress/value text never overlaps chrome.
- [ ] SCN-08 Match HUD: validate all HUD panels panel-by-panel before moving on.
- [x] SCN-08 Build Placement Bar: update rail, preview, cost, time, rotate, cancel, and confirm controls.
- [x] SCN-08 Build Placement Bar: validate the bar stays readable and anchored at all target aspects.
- [x] SCN-09 Build Drawer Popup: update tabs, catalog cards, right detail, queue, and progress panels.
- [x] SCN-09 Build Drawer Popup: ensure build progress panel is hidden by default and only shown when active.
- [x] SCN-09 Build Drawer Popup: ensure tab changes update card portraits and selected detail imagery.
- [x] SCN-09 Build Drawer Popup: validate scrolling content has no clipped card buttons.
- [x] Capture focused crops for command buttons, squad cards, drawer cards, and build placement rail.
- [x] Run `git diff --check`.

Acceptance:

- Gameplay UI remains live, readable, and performant.
- No runtime-bound names are renamed or removed.

## Phase 5 - Popups And Modal Surfaces

Goal:
Bring Canvas popups into the same Target Lock modal language.

- [x] POP-05 Mission Result: reconcile shell popup vs legacy MissionResult popup usage.
- [ ] POP-05 Mission Result: update modal frame, result header, stat rail, objectives, rewards, casualties, score, and footer actions.
- [ ] POP-05 Mission Result: validate victory/defeat/neutral states.
- [x] Pause Menu: update frame, mission info, settings, resume, retry, quit, and footer controls if active.
- [ ] Threat Alert: update alert frame, icon, severity state, message, and action controls if active.
- [x] Confirm Raid: update confirmation frame, risk/reward rows, and confirm/cancel states if active.
- [ ] Reward Unlock: update reward card, icon/portrait, rarity state, and claim controls if active.
- [x] Intel Reveal: update reveal panel, image, text hierarchy, and close/continue controls if active.
- [x] End Of Day Report: update summary sections, stat rows, charts, rewards, and action controls if active.
- [ ] Ability Upgrade Detail: update detail panel, upgrade rows, requirements, and action controls if active.
- [ ] Build Placement Panel legacy popup: either retire as inactive or align with build placement shell style.
- [x] PopupFrameView: make it the shared Target Lock modal foundation where feasible.
- [x] Ensure every popup close button has hover/focus/pressed states.
- [ ] Ensure every destructive or confirm action has distinct but consistent state styling.
- [ ] Validate popup readability at 4800x2160.
- [ ] Validate popup readability at 1920x1080.
- [ ] Capture focused modal crops for every active popup.
- [ ] Run `git diff --check`.

Acceptance:

- Active popups share one premium modal language.
- Inactive legacy popups are documented before any styling work is skipped.

## Phase 6 - Interaction, Motion, And State Polish

Goal:
Make controls feel premium without adding runtime polling or layout instability.

- [x] Audit every Button, Toggle, selectable card, tab, and row in active Canvas prefabs.
- [x] Add default, highlighted/hover, pressed, selected/current, disabled, and focused visuals where supported.
- [x] Use sprite-swap or color-tint transitions consistently per control family.
- [ ] Add subtle scale/impact animation only through existing Canvas selectable/animator mechanisms.
- [x] Confirm hover/selected states cover the full chrome frame where the mockup shows frame replacement.
- [ ] Confirm state transitions do not move neighboring layout or cause overlap.
- [ ] Confirm selected/current state can move to any repeated item at runtime.
- [ ] Confirm disabled/locked state remains readable but clearly unavailable.
- [ ] Capture focused state contact sheets for button and card families.
- [ ] Run `git diff --check`.

Acceptance:

- Interactive states are visible, consistent, and reusable.
- No new MonoBehaviour update loop is introduced for visual polish.

## Phase 7 - Responsive Layout And CanvasScaler Pass

Goal:
Make Canvas visuals stay clean across the same aspect ranges the game uses.

- [ ] Record the existing CanvasScaler mode and reference resolution before any changes.
- [ ] Decide whether the Canvas reference should remain current settings or move to the Target Lock 4800x2160 authoring reference.
- [ ] Validate 4800x2160 layout for every active surface.
- [ ] Validate 1920x1080 layout for every active surface.
- [ ] Validate wide aspect layout for every active surface.
- [ ] Validate popup anchoring on menu and match scenes.
- [ ] Validate text does not become oversized at lower resolutions.
- [ ] Validate text does not become unreadably small at high resolutions.
- [ ] Validate left nav never overlaps middle content.
- [ ] Validate right panels and drawers stay inside the safe area.
- [ ] Validate HUD bottom tray/squad panels remain aligned and unclipped.
- [ ] Validate scroll views preserve usable viewport height.
- [ ] Save responsive comparison contact sheets.
- [ ] Run `git diff --check`.

Acceptance:

- Canvas behaves like a stable responsive UI, not a one-resolution mockup.

## Phase 8 - Performance And Regression Gates

Goal:
Prove the Canvas art migration does not recreate the UI Toolkit FPS problem.

- [ ] Measure menu FPS with Canvas active after shared shell pass.
- [ ] Measure menu FPS with Canvas disabled after shared shell pass.
- [ ] Measure menu FPS with Canvas active after all menu surfaces.
- [ ] Measure menu FPS with Canvas disabled after all menu surfaces.
- [ ] Measure match HUD FPS with Canvas active after HUD pass.
- [ ] Measure match HUD FPS with Canvas disabled after HUD pass.
- [ ] Inspect Canvas rebuild profiler markers on static menu screens.
- [ ] Inspect Canvas rebuild profiler markers on dynamic match HUD screens.
- [ ] Reduce overdraw from large transparent images where profiler/captures show cost.
- [ ] Split static and dynamic Canvas groups when dynamic updates dirty too much static chrome.
- [ ] Confirm large scaled art has appropriate mipmap/import settings.
- [ ] Confirm repeated cards/buttons are batched where practical.
- [ ] Confirm no runtime errors are introduced in editor logs.
- [ ] Run focused Unity validation in the shadow project when available.
- [ ] Run main-project validation only when explicitly needed or requested.
- [ ] Run `git diff --check`.

Acceptance:

- Canvas remains materially cheaper than the rejected heavy UI Toolkit menu path.
- No visual pass is accepted with unresolved runtime errors.

## Phase 9 - Final Audit And Handoff

Goal:
Finish with a traceable, reusable Canvas art system.

- [ ] Recount checklist progress and update this snapshot.
- [ ] Confirm every active Canvas surface has final screenshots.
- [ ] Confirm every active popup has final screenshots or documented inactive status.
- [ ] Confirm every button/selectable family has state evidence.
- [ ] Confirm every PPU/9-slice change is recorded.
- [ ] Confirm no forbidden files were edited.
- [ ] Confirm all `.meta` files are preserved.
- [ ] Run `git diff --check`.
- [ ] Record final validation status and remaining risks.
- [ ] Mark automation complete only after all active Canvas surfaces and validation gates are complete.

Acceptance:

- The Canvas UI carries the Target Lock art direction with stable performance.
- The tracker can be used later as a regression checklist.
