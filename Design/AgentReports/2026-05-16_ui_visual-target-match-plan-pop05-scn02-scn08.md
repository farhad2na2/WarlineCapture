# Lane
UI

# Task
P0 loop-break visual target-match plan for `SCN-02_MainMenu`, `POP-05_MissionResult`, and `SCN-08_RTSBattleHUD` / M01 Match HUD.

# Files changed
- `Design/AgentReports/2026-05-16_ui_visual-target-match-plan-pop05-scn02-scn08.md`

# Contracts touched
- No Unity prefab, script, generated asset, runtime binding, or capture contract was changed.
- This report follows the PM override in `Design/AgentReports/2026-05-16_pm_ui-loop-break-visual-target-match-procedure.md`.

# User-visible behavior
None. This heartbeat intentionally stopped implementation and produced the required planning gate before any further UI work.

# Validation run
- Read `Design/AgentTasks/ui_heartbeat.md`.
- Read `Design/AgentTasks/ui_current.md`.
- Read `Design/AgentReports/2026-05-16_pm_ui-loop-break-visual-target-match-procedure.md`.
- Inspected layer manifests and layer inventories for:
  - `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json`
  - `Design/VisualLockLayered/POP-05_MissionResult/layer_manifest.json`
  - `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json`
- Checked target/capture dimensions with PIL for SCN-02, POP-05, and SCN-08.

# Validation result
- Planning-only gate completed.
- No implementation commands, Unity builders, capture commands, or source edits were run after reading the PM override.

# SCN-02 Main Menu
Approved target image path: `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png`

Latest implementation capture path: `Design/AgentReports/Captures/SCN-02_MainMenu_TargetMatchFix_1672x941.png`

Reference resolution: 1672x941.

Can reach 100% visual target match with current assets: no.

Why no: the current layer pack has useful slices, but not enough standalone target-authored layers to reconstruct the exact target without either baking the flattened target image or accepting approximate composition. Missing are exact full-screen world/map background layers, exact masthead/wordmark/logo treatment, exact target card full-size image plates, exact Deploy Command CTA chrome, and exact screen-level overlay/depth passes as separate non-text runtime-safe assets. Code-only layout changes can move Unity objects, but cannot invent the missing target-quality bitmap detail or chrome depth.

| Region | Target mismatch to solve | Exact prefab/root objects to move, resize, rebuild, or delete | Exact layers/assets to use | Missing asset/data/blocker |
|---|---|---|---|---|
| Full background | Layout and density currently read as a dark runtime shell; target has a large illustrated world-map composition with integrated depth, color, and screen-wide hierarchy. | Rebuild/delete `Screen_MainMenu/WorldMapBackdrop`, `SagaMapBackdrop`, and `LowerVignette`; replace old synthetic background fills on `Screen_MainMenu`. | Existing: `mode_card_art_operation.png`, `mode_card_art_saga.png`, `screen_shell_frame.png`. | Art/Atlas: exact full-screen world-map/background plate split into safe runtime layers without baked live text. |
| Masthead/top strip | Scale, position, color, typography, and chrome differ; current masthead is live text over `profile_block_frame` and not the target brand lockup. | Rebuild `TopProfileBar`, `LogoImage`, `MastheadText`, `CommandDeckText`, `CommanderAvatar`, `CommanderNameText`, `LevelText`, `XpProgressTrack`, `XpProgressText`. Delete old top profile/profile-progress assumptions that do not exist in target. | `top_resource_strip_frame.png`, `resource_counter_frame.png`, `icon_credits.png`, `icon_materials.png`, `icon_command_authority.png`, `settings_gear_icon.png`, `profile_block_frame.png`. | Art/Atlas: exact Warline Capture masthead/brand plate or separable emblem/wordmark; exact top strip background/depth layer. |
| Resource counters | Current counters are semantically correct but not pixel-matched in size/spacing/value treatment; chrome depth and icon scale differ. | Move/resize `ResourceCounterList/Resource_Money`, `Resource_Trust`, `Resource_Intel`; rebuild child `Icon`, `LabelText`, `ValueText`; delete economy-plus button if target does not include it in that location. | `resource_counter_frame.png`, `icon_credits.png`, `icon_materials.png`, `icon_command_authority.png`. | Current assets can support structure, but exact target spacing/depth still depends on top strip layer fidelity. |
| Commander profile | Target left panel composition is denser and more integrated; current profile block uses generic placeholder crop and simple text. | Rebuild `LeftNav`, `CommanderProfilePanel`, `ProfileAvatar`, `ProfileNameText`, `ProfileStatusText`, and side route stack. Delete old nav-button placements inherited from previous shell. | `screen_shell_frame.png`, `profile_block_frame.png`, `commander_profile_placeholder.png`, `side_route_button_frame.png`, `designed_unavailable_badge.png`. | Art/Atlas: exact commander profile target plate if the target's profile card has nonreconstructable internal lighting/detail. |
| Side route buttons | Current unavailable badges are semantically present but scale, position, density, and button treatment do not match target. | Rebuild `ProfileButton`, `InboxButton`, `StoreButton`, `EventsButton`, `RankingButton`, and `DesignedUnavailableBadge` child placements. | `side_route_button_frame.png`, `designed_unavailable_badge.png`. | Current assets likely sufficient after exact coordinate pass if full left rail background is supplied. |
| Three mode cards | Current large cards are approximate: art quality, scale, color grading, frame depth, title typography, and card density differ from target. | Rebuild `ModeCardList`, `ModeCard_Saga`, `ModeCard_Operation`, `ModeCard_QuickCustom`; delete any old horizontal-row mode-card helper output; rebuild `ContentClip`, `ArtClip`, `TintWash`, `TextShade`, `TitleText`, `SubtitleText`, `BodyText`, `ProgressText`, `DistrictPressureRow`, `CityRiskRow`, `Button`. | `mode_card_frame.png`, `mode_card_art_saga.png`, `mode_card_art_operation.png`, `mode_card_art_quick_custom.png`, `resource_counter_frame.png`. | Art/Atlas: exact full-height mode-card illustrations/backgrounds at target dimensions; current 440x165 art is too small and generic for 100% match. |
| Persistent Operation risk content | Current risk rows are present but cramped and not target-quality; color, chrome depth, typography, and state emphasis differ. | Rebuild `ModeCard_Operation/DistrictPressureRow`, `CityRiskRow`, `ProgressText`, and body text area. | `resource_counter_frame.png`, `mode_card_frame.png`, `mode_card_art_operation.png`. | Current assets can support semantic rows, not exact target art treatment. |
| Footer and Deploy CTA | Current footer and CTA are approximate; target CTA button treatment and bottom rhythm differ. | Rebuild/delete `BottomUtilityBar`, `ChatButton`, `SocialButton`, `ChatMessageText`; rebuild `DeployCommandButton` instead of borrowing POP-05 continue button chrome. | `footer_status_frame.png`, `side_route_button_frame.png`, `designed_unavailable_badge.png`. | Art/Atlas: exact Deploy Command CTA background/chrome/chevrons if target requires pixel lock. |

SCN-02 old shell/pieces that must be replaced instead of preserved:
- The previous horizontal `ModeCardList` row model and `CreateMainMenuModeCard` assumptions.
- The old `TopProfileBar` as a commander/profile strip instead of target masthead/resource strip.
- The old `LeftNav` button-only rail.
- Synthetic background/vignette approximations.
- Borrowed POP-05 button art for `DeployCommandButton`.

SCN-02 implementation sequence after PM accepts plan and blockers are resolved:
1. Art/Atlas supplies exact missing SCN-02 background, masthead/brand, full-size mode card art, and Deploy CTA layers.
2. UI deletes old shell objects named above and rebuilds from target coordinates first, before reconnecting route/data bindings.
3. Bind live TMP only after each target region's frame/art/chrome is visually placed.
4. Capture 1672x941 without `-nographics`; compare against `SCN-02_MainMenu_Landscape_Target.png`.
5. Iterate region by region until the screenshot visually matches; only then run `WarlineCaptureUiMainMenuTests` as secondary proof.

# POP-05 Mission Result
Approved target image path: `Design/VisualLockLayered/POP-05_MissionResult/reference/POP-05_MissionResult_Landscape_Target.png`

Latest implementation capture path: `Design/AgentReports/Captures/POP-05_MissionResult_TargetMatchFix_1672x941.png`

Reference resolution: 1672x941.

Can reach 100% visual target match with current assets: no.

Why no: the POP-05 pack includes core frames/icons/buttons, but it does not include the target's exact separate mission thumbnail, full premium background/depth passes, exact hero panel/chrome layering, exact reward-card interior styling, or exact button treatment at target size. Code-only changes can improve layout but cannot create the missing art fidelity without flattening target text into runtime UI.

| Region | Target mismatch to solve | Exact prefab/root objects to move, resize, rebuild, or delete | Exact layers/assets to use | Missing asset/data/blocker |
|---|---|---|---|---|
| Full tactical background | Current background uses a single repeated/cropped tactical image and lacks target depth, color, and framing relationship. | Rebuild `MissionResultPopup/BackgroundTacticalArt`, `Scrim`, and any synthetic overlays. | `background_tactical_art.png`. | Art/Atlas: exact target full-screen background/depth plate split from live text. |
| Modal frame/chrome | Current modal is approximate; target has heavier AAA premium frame, inner fill, shadowing, and panel depth. | Rebuild `Frame`, `FrameFill`, `Header`, and body panel geometry; delete hidden old debug/shell remnants if present. | `modal_frame.png`, `modal_fill.png`, `section_panel_frame.png`. | Art/Atlas: if current frame slices are not visually identical to target, exact frame/depth slices are required. |
| Victory hero panel | Current `Header` has correct semantic content but target scale, star cluster, emblem placement, typography, and chrome depth differ. | Rebuild `Frame/Header`, `VictoryEmblem`, `TitleText`, `Star_1`, `Star_2`, `Star_3`, `MissionNameText`, `MissionMetaText`, `MapIdentityText`. | `victory_emblem.png`, `icon_star_filled.png`, `icon_star_empty.png`, `modal_fill.png`. | Current assets can provide emblem/star semantics; exact hero panel/depth likely needs improved source slice. |
| Mission image/identity block | Current `MissionIdentityBlock` reuses tactical background as thumbnail; target requires a distinct mission image/identity composition. | Rebuild `BodyRoot/MissionIdentityBlock`, `MissionImage`, `MissionImageScrim`, `MissionIdentityTitleText`, `MissionIdentityMetaText`, `MissionIdentityMapText`; delete old hidden `StatsPanel` from visible composition. | `section_panel_frame.png`, `background_tactical_art.png`. | Art/Atlas: exact mission thumbnail/image layer for M01 First Contact. |
| Objective row | Current row has correct objective but target row styling, density, icon treatment, and placement are not exact. | Rebuild `ObjectivesPanel`, `Objective_DestroyHostilePatrol`, hidden extra objective rows, `CompleteIcon`, `LabelText`, `StatusText`. | `objective_row_frame.png`, `icon_objective_complete.png`. | Current assets likely sufficient if target row sprite is exact; otherwise Art/Atlas owns row frame revision. |
| Reward grid | Current reward cards are visible but reward-card treatment, scale, icon position, and typography differ from target. | Rebuild `RewardsPanel`, `CommanderXpReward`, `CreditsReward`, `MaterialsReward`, `IntelReward`, their `IconImage`, `LabelText`, `ValueText`. Delete old stat-card reward layout assumptions. | `reward_card_frame.png`, `icon_commander_xp.png`, `icon_credits.png`, `icon_materials.png`, `icon_intel.png`. | Art/Atlas: exact reward-card interior/chrome if current 126x72 frame cannot reproduce target. |
| City consequence row | Current consequence row exists but target row styling and placement are not exact. | Rebuild `ConsequenceRow`, `ConsequenceText`; delete any fallback summary/stat row competing with it. | `consequence_row_frame.png`. | Current asset may be sufficient with exact coordinates if row frame matches target. |
| Replay/Continue buttons | Current buttons use available backgrounds but target size, glow, chrome, and typography differ. | Rebuild `ButtonRow`, `ReplayButton`, `ContinueButton`, child `LabelText`; delete generic button spacing. | `replay_button_background.png`, `continue_button_background.png`. | Art/Atlas: exact target button-state backgrounds if current layers are not visually identical. |

POP-05 old shell/pieces that must be replaced instead of preserved:
- The original `StatsPanel` first-column layout as visible UI.
- Any section-panel grid that treats Mission Result as generic stats/rewards/objectives instead of target hero + mission block + single objective row + reward row.
- Reusing background art as the mission thumbnail if pixel lock is required.
- Generic section titles and card labels where target places visual hierarchy differently.

POP-05 implementation sequence after PM accepts plan and blockers are resolved:
1. Art/Atlas supplies exact mission thumbnail/background/depth/button/card slices if PM requires 100% pixel lock.
2. UI rebuilds POP-05 from target coordinates using `modal_frame`, `modal_fill`, hero, mission block, objective row, reward row, consequence row, and buttons as first-class objects.
3. UI preserves live TMP/data only after visual hierarchy is locked.
4. Capture 1672x941 without `-nographics`; compare against `POP-05_MissionResult_Landscape_Target.png`.
5. Iterate region by region; run component/controller tests only after visual match.

# SCN-08 RTS Battle HUD / M01 Match HUD
Approved target image path: `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png`

Latest implementation capture paths:
- Editor/no-selection: `Design/AgentReports/Captures/M01-01_SCN08_NoSelection_v6_1920x1080.png`
- Runtime: `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v6_1920x1080.png`

Reference resolution: target is 1672x941. Latest captures are 1920x1080, so a fresh 1672x941 capture is required before any 100% target-lock claim.

Can reach 100% visual target match with current assets: no.

Why no: SCN-08 has many more layer slices than SCN-02/POP-05 and v6 fixed the accepted alpha and M01 command-order issues, but the latest captures still differ from the target in battlefield/map composition, target-resolution framing, panel proportions, bottom rail density, command state, squad-card composition, minimap scale/content, and gameplay-owned live unit/camera presentation. UI code can move HUD regions, but cannot force the live gameplay scene/camera/soldier/background to match the static target without Gameplay-owned setup/data. A new exact-match pass also needs the target state decided: generic SCN-08 target or M01 no-selection target, because the command state/content differ.

| Region | Target mismatch to solve | Exact prefab/root objects to move, resize, rebuild, or delete | Exact layers/assets to use | Missing asset/data/blocker |
|---|---|---|---|---|
| Capture resolution/state | Latest proof is 1920x1080 while target is 1672x941; M01 no-selection state is not necessarily the same as the generic SCN-08 mockup state. | Add/use a 1672x941 target capture path for `Screen_MatchOverlay`; clarify capture state before implementation. | `SCN-08_RTSBattleHUD_Landscape_Target.png` as comparison only, not flattened runtime asset. | PM: decide exact target state for SCN-08 completion. Gameplay: provide matching M01 runtime state if target includes live battlefield composition. |
| Battlefield/background | Current runtime/gameplay field differs in map, unit scale, selection/readability rings, camera crop, and density. | UI can only manage overlay objects; Gameplay must align scene/camera. Do not hide gameplay-owned differences inside UI report. | No UI layer in SCN-08 pack supplies battlefield background. | Gameplay owner: exact camera, terrain/background, unit positions, soldier silhouettes, health/readability rings matching target. |
| Objective panel | v6 has clean alpha and correct M01 objective but panel scale/position/density/typography may not match target. | Move/rebuild `Screen_MatchOverlay/ObjectivePanel`, `FillBackground`, `FrameChrome`, `SectionTitleText`, objective rows, star goals. Delete any old synthetic panel fill/frame layers still competing. | `objective_panel_frame.png`, `objective_panel_fill.png`, `objective_checked_square.png`, `objective_empty_square.png`, `objective_star_filled.png`. | Current assets likely sufficient for UI-only region after exact target coordinate pass. |
| Top resource bar and controls | Current top/resource chrome differs in spacing, clock/resource treatment, and button scale. | Rebuild resource/top controls inside `Screen_MatchOverlay`; move pause/settings buttons and resource counters. | `resource_bar_frame.png`, `resource_bar_fill.png`, `resource_money_icon.png`, `resource_crate_icon.png`, `resource_population_icon.png`, `time_clock_icon.png`, `top_icon_button_background.png`, `pause_icon.png`, `settings_gear_icon.png`. | Current assets likely sufficient if target state values are known. |
| Threat feed | v6 clean slices exist but row density, active row treatment, typography, and scale still need exact target pass. | Rebuild `ThreatFeedPanel`, threat rows, warning icon placements, title/divider. | `threat_feed_panel_frame.png`, `threat_feed_panel_fill.png`, `threat_row_active_background.png`, `threat_row_normal_background.png`, `threat_warning_icon.png`, `threat_enemy_spotted_icon.png`. | Current assets likely sufficient for UI-only region. |
| Squad cards/tray | v6 has portraits and badges, but bottom card composition, scale, status, health bars, and selected/nonselected states differ from target and M01 step mockups. | Rebuild `SquadTray`, `Squad_Rifle`, `Squad_APC`, `Squad_Tank`, `Squad_Helicopter`, child title/count/portrait/status/shield/rank/health. Delete old fixed card sizes if target requires different card geometry. | `squad_tray_frame.png`, `squad_tray_fill.png`, `squad_card_selected_background.png`, `squad_card_normal_background.png`, `squad_portrait_rifle.png`, `squad_portrait_apc.png`, `squad_portrait_tank.png`, `squad_portrait_helicopter.png`, `shield_badge_cyan.png`, `squad_rank_triple_chevron.png`. | PM/Gameplay: decide whether target state has selected squad, disabled squad cards, exact unit counts/status. |
| Command rail/buttons | M01 command order is accepted, but visual target lock requires exact rail position, scale, state, and button treatment. | Rebuild `CommandBar`, `CommandRailArt`, command buttons `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`; hide/delete `SPECIAL` for M01 target. | `command_rail_frame.png`, `command_rail_fill.png`, `command_button_normal_background.png`, `command_button_selected_background.png`, `command_select_icon.png`, `command_move_icon.png`, `command_attack_icon.png`, `command_stop_icon.png`, `command_hold_icon.png`. | Current UI assets sufficient for M01 command family; PM must confirm target selected/disabled states. |
| Minimap | v6 has minimap layers, but scale/content/viewport/zoom placement still differs from target. | Rebuild `MiniMapPanel`, `MapImage`, `ViewportRect`, `ZoomInButton`, `ZoomOutButton`, frame/fill. | `minimap_frame.png`, `minimap_fill.png`, `minimap_content.png`, `minimap_viewport_rect.png`, `minimap_zoom_plus_button.png`, `minimap_zoom_minus_button.png`. | Current assets likely sufficient for UI-only minimap if target state is fixed. |
| Global chrome/typography/density | HUD still reads as a runtime approximation; target has tighter mockup composition and global polish. | Rebuild panel coordinates from target first; apply TMP after art lock; delete old layout-preservation constraints that keep current shell proportions. | All SCN-08 frame/fill/icon layers above. | UI can handle after PM target state and Gameplay battlefield blockers are resolved. |

SCN-08 old shell/pieces that must be replaced instead of preserved:
- Current v6 panel coordinates and bottom HUD proportions if they conflict with the target.
- Any legacy `MatchHUD_*` generated frame assets still used where `LayeredOneGo` replacements exist.
- M01 generic/non-M01 `SPECIAL` command binding.
- Any capture proof at 1920x1080 used as evidence for a 1672x941 target.

SCN-08 implementation sequence after PM accepts plan and blockers are resolved:
1. PM decides whether target-lock means the generic SCN-08 target, the M01 no-selection state, or a selected-squad M01 step target.
2. Gameplay supplies exact battlefield/camera/unit setup if the live runtime capture must match the target background and unit composition.
3. UI rebuilds the HUD by target coordinates using only SCN-08 `LayeredOneGo` slices and live TMP after visual structure is placed.
4. Capture 1672x941 editor/prefab proof and runtime proof for the decided state.
5. Compare directly to `SCN-08_RTSBattleHUD_Landscape_Target.png`; run `WarlineCaptureUiMatchOverlayTests` only after visual match is established.

# Known gaps
- All three surfaces currently fail the new 100% visual target-lock standard.
- The current assets can support improved composition and semantic binding, but SCN-02 and POP-05 lack several target-authored standalone art layers required for pixel-level visual match without using flattened target images.
- SCN-08 additionally has a Gameplay-owned blocker for battlefield/camera/unit composition.

# Cross-lane impacts
- PM/user must accept this plan before UI resumes implementation.
- Art/Atlas is the likely blocker owner for missing SCN-02 and POP-05 target-quality standalone layers.
- Gameplay is the likely blocker owner for SCN-08 battlefield/camera/unit composition if runtime visual target lock is required.
- QA/HCI, Gameplay continuation, Support/FTUE, Designer, and non-routed packages should remain held until PM/user routes the next accepted step.

# Next recommended task
PM/user review this plan and choose one surface to target first. Recommended order: SCN-02 after Art/Atlas supplies exact missing background/masthead/card/CTA layers; then POP-05 after Art/Atlas supplies exact mission thumbnail/background/chrome layers; then SCN-08 after PM clarifies the target state and Gameplay supplies matching battlefield/camera setup.
