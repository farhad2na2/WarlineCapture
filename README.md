# WarlineCapture

`WarlineCapture` is a Unity 6 DOTS/ECS mobile-first RTS project for large-scale grid-based movement, base building, tactical combat, procedural city gameplay, configurable AI, and campaign/operation game modes.

The current codebase already has the core tactical simulation: units, buildings, roads, resources, production, AI economy/building/production/squads/combat, transport, base breach, radar warnings, minimap, runtime stats, and Android build support. The next direction is to wrap that simulation in a polished mobile product structure with proper UI routing, game modes, objectives, results, rewards, progression, and persistence.

## Project Setup

- Unity editor version: `6000.4.0f1`
- Main scene: `Assets/Game/Scenes/Game.unity`
- Main subscene: `Assets/Game/Scenes/Game/GameSubScene.unity`
- Main gameplay code: `Assets/Game/Scripts`
- Main design docs: `Design`

## Packages

- Unity Entities: `6.4.0`
- Unity Entities Graphics: `6.4.0`
- Unity Input System: `1.19.0`
- Universal Render Pipeline: `17.4.0`

## Product Direction

WarlineCapture is being built around three major modes on one shared tactical simulation:

1. `Saga Campaign`
   Curated mission nodes, chapter progression, mission briefings, loadouts, objectives, star scoring, rewards, and unlocks.

2. `Persistent Operation`
   A saved multi-day city operation where district security, public trust, infrastructure, enemy influence, intel confidence, civilian density, and heat evolve over time.

3. `Quick Custom Game`
   Fast replayable skirmishes using existing AI and economy knobs: enemy count, difficulty, resources, build/production speed, aggression, target priority, map seed, and win condition.

The active production art direction is premium 2D isometric mobile RTS using large terrain macro tiles with separate gameplay metadata. The current 2D iso references live under `Design/VisualReferences/2DIsometricProduction`; the macro-tile plan lives at `Design/WarlineCapture_MacroTile_Terrain_Production_Plan.md`.

## Design Documents

Design folder index:

- `Design/README.md`
  Complete design map for the `Design` folder. Read this first when checking design alignment; it lists the current source-of-truth order, every active design document, the visual-lock note files, the 2D isometric production references, audio, monetization, and future update rules.
- `Design/WarlineCapture_Project_State_Source.json`
  Machine-readable source of truth for overall project plan/state, dependencies, on-hold work, in-progress work, roadmap stages, and completion estimates. Update this first when project state changes.
- `Design/WarlineCapture_Project_State_Dashboard.md`
  Generated quick-look dashboard with overall completion, roadmap table, plan status table, dependency Mermaid diagram, completion chart, and detailed state summaries. Regenerate it with `python3 Tools/ProjectState/generate_project_state_dashboard.py`; do not manually edit the generated dashboard.
- `Tools/ProjectState/generate_project_state_dashboard.py`
  Dashboard generator that keeps the human-readable project-state dashboard in sync with the JSON source document.
- `Design/WarlineCapture_Agent_Coordination_Workflow.md`
  PM assistant workflow for synchronizing gameplay, UI, and support/docs agents. Use it for completion reports, cross-lane contract changes, validation gates, and priority handoffs.

Foundational references:

- `Design/GAME_DESIGN_REFERENCE.md`
  Compact reference for the implemented RTS simulation: economy, units, buildings, AI, transport, combat, base breach, and threat warnings.
- `Design/WarlineCapture_Combat_Catalog_And_Upgrade_Design.md`
  Canonical combat catalog and upgrade design for all current and planned character, vehicle, air, sea, building, skill, ability, and upgrade-track ids.
- `Design/BalanceConfigs/WarlineCapture_Combat_Balance_Config_v0_1.json`
  Balance-only config for combat entities, abilities, and upgrade tracks. It owns costs, stats, cooldowns, unlock gates, producer relationships, and upgrade modifiers.
- `Design/VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json`
  Visual-only companion config for world assets, icons, portraits, damage states, animation/VFX/audio ids, and art briefs.
- `Design/WarlineCapture_AAA_Mobile_Game_Design_Document_v0_1.md`
  High-level AAA mobile GDD. The `.docx` beside it is the authored document version.
- `Design/WarlineCapture_AAA_Mobile_Game_Design_Document_v0_1.docx`
  Authored document version of the AAA mobile GDD.
- `Design/WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md`
  Gameplay north star and content grammar that must be read before level-by-level or mission-by-mission authoring; locks mission archetypes, threat families, Chapter 1 teaching arc, Operation week rhythm, balance bands, and mission acceptance rules.
- `Design/WarlineCapture_Level_And_Mission_Content_Plan.md`
  Working source for level-by-level and mission-by-mission authoring; defines the mission spec template, high-level Saga chapter set, Operation hooks, Quick Custom probe mapping, balance targets, and acceptance gate.
- `Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md`
  FTUE and reusable ARIA command assistant design for Chapter 1 tutorials, contextual recommendations, safe assistant control takeover, UI surfaces, data model, and validation plan.
- `Design/WarlineCapture_AssistantPanel_M01_Implementation_Contract.md`
  Implementation handoff for `PREFAB-05_AssistantPanel` and M01 ARIA recommendation states, including required UI ids, runtime data fields, Show Me / Do It / Stop behavior, player-control cancellation boundaries, `BattleHudGameplayBridge` dependencies, asset-register implications, and acceptance checks.
- `Design/WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md`
  Runtime wiring handoff for M01 ARIA assistant services, context data flow, recommendation transitions, typed Show Me / Do It / Stop intents, save/session fields, invalid-command recovery, and validation tests.
- `Design/SagaChapters/README.md`
  Saga chapter design folder index and update rules.
- `Design/SagaChapters/WarlineCapture_Saga_Chapter01_First_Response.md`
  Chapter 1 / First Response mission matrix and detailed specs for all five Chapter 1 missions.
- `Design/SagaChapters/WarlineCapture_Saga_Chapter02_Broken_Grid.md`
  Chapter 2 / Broken Grid high-level chapter arc.
- `Design/SagaChapters/WarlineCapture_Saga_Chapter03_Hidden_Network.md`
  Chapter 3 / Hidden Network high-level chapter arc.
- `Design/SagaChapters/WarlineCapture_Saga_Chapter04_Air_And_Armor.md`
  Chapter 4 / Air And Armor high-level chapter arc.
- `Design/SagaChapters/WarlineCapture_Saga_Chapter05_Citywide_Command.md`
  Chapter 5 / Citywide Command high-level chapter arc.
- `Design/AI_CONTROLLER_DESIGN.md`
  AI controller architecture, economy/build/production/squad/targeting/combat planning, tuning knobs, and validation logs.

Core implementation docs:

- `Design/WarlineCapture_UIUX_Implementation_High_Level_Spec.md`
  High-level UI shell, screen routing, visual-slice strategy, and implementation phases.
- `Design/WarlineCapture_UIUX_Implementation_Detailed_Spec.md`
  Code-oriented UI implementation plan, prefab/component names, screen hierarchy, and route details.
- `Design/WarlineCapture_Gameplay_Features_High_Level_Spec.md`
  Product gameplay roadmap for Saga, Persistent Operation, Quick Custom, objectives, rewards, progression, AI profiles, and opt-in balance/gameplay probes. Read after the north-star/content-grammar doc.
- `Design/WarlineCapture_Gameplay_Features_Detailed_Spec.md`
  Concrete gameplay system plan, folder layout, launch payloads, objective/result/reward systems, persistence, and opt-in balance probe implementation details. Mission configs should follow the content grammar.
- `Design/WarlineCapture_Economy_Reward_Design.md`
  Canonical resource names, reward types, resource-strip rules, and gameplay goals for popups and reusable panels.
- `Design/WarlineCapture_Balancing_Automated_Test_Plan.md`
  Concrete plan for balance harness tests, opt-in gameplay/economy probes, report outputs, and future store/reward/economy data sanity checks.
- `Design/WarlineCapture_UIUX_Mockup_To_Canvas_Conversion_Plan.md`
  Canonical visual-lock target inventory and the rules for converting generated targets into real layered Unity Canvas UI.
- `Design/WarlineCapture_UIUX_Target_To_Canvas_Workflow_Guide.md`
  Reusable operational workflow for converting target mockups and layer packs into real Unity Canvas prefabs.
- `Design/WarlineCapture_UIUX_Screen_Popup_Implementation_Spec.md`
  Root screen/popup spec with corrected links to the packaged source mockup images.
- `Design/WarlineCapture_UIUX_Screen_Popup_Implementation_Spec.docx`
  Authored document version of the UI/UX screen and popup spec.
- `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
  Gameplay contract matrix for every planned UI element, including purpose, route/effect, data source, enable rule, and locked/designed-unavailable/read-only state.
- `Design/WarlineCapture_Visual_Feedback_VFX_Recommendations.md`
  Shared visual feedback, UI motion, gameplay VFX, and paired audio recommendations for responsive buttons, locked states, rewards, popups, tactical commands, warnings, and critical combat feedback.
- `Design/WarlineCapture_UIUX_Mockup_Target_Alignment_Audit.md`
  Audit confirming UI mockup target coverage, Chapter 1 mission surface coverage, and nonblocking follow-up targets for later routes.
- `Design/WarlineCapture_UIUX_Codex_Package/WarlineCapture_UIUX_Screen_Popup_Implementation_Spec.md`
  Packaged copy of the screen/popup spec beside `warlinecapture_uiux_spec_assets`.
- `Design/WarlineCapture_UIUX_MainMenu_Visual_Contract.md`
  Active Main Menu visual contract.
- `Design/WarlineCapture_UIUX_MainMenu_Visual_Lock_Plan.md`
  Main Menu visual-lock implementation plan.
- `Design/WarlineCapture_UIUX_Runtime_Optimization_Plan.md`
  Runtime optimization and validation plan for generated UI.
- `Design/WarlineCapture_UIUX_Phase1_Immediate_Implementation_Plan.md`
  Phase 1 UI implementation plan.
- `Design/WarlineCapture_UIUX_Phase2_Immediate_Implementation_Plan.md`
  Phase 2 UI implementation plan.
- `Design/WarlineCapture_UIUX_Phase3_Immediate_Implementation_Plan.md`
  Phase 3 UI implementation plan.
- `Design/WarlineCapture_UIUX_Phase4_Immediate_Implementation_Plan.md`
  Phase 4 UI implementation plan.
- `Design/WarlineCapture_UIUX_Phase5_Immediate_Implementation_Plan.md`
  Phase 5 UI implementation plan.
- `Design/WarlineCapture_UIUX_Phase6_Immediate_Implementation_Plan.md`
  Tactical HUD continuation plan aligned with the 2D isometric gameplay direction.
- `Design/WarlineCapture_UIUX_Phase7_Immediate_Implementation_Plan.md`
  Popup implementation plan starting with Threat Alert after Pause Menu.
- `Design/WarlineCapture_2D_Isometric_Production_Direction.md`
  Active art-production direction for premium 2D isometric RTS visuals using terrain macro tiles and metadata.
- `Design/WarlineCapture_2D_Isometric_Art_Bible.md`
  Production rules for macro tiles, runtime entities, metadata, sprite import settings, sorting, faction colors, lighting, and readability.
- `Design/WarlineCapture_MacroTile_Terrain_Production_Plan.md`
  Selected macro-tile terrain direction, metadata model, building socket rules, destruction handling, memory strategy, and step-by-step implementation plan.
- `Design/WarlineCapture_2D_Isometric_Implementation_Validation_Plan.md`
  Step-by-step implementation and validation plan for the macro-tile 2D isometric art/runtime pipeline.

The canonical visual-lock target inventory for screens, popups, and reusable panels lives in `Design/WarlineCapture_UIUX_Mockup_To_Canvas_Conversion_Plan.md`. Current targets are stored under `Design/VisualLock`, including `SCN-*`, `POP-*`, `PREFAB-*`, and `MainMenu`.

Campaign/runtime UI foundations:

- Chapter 1 mission configs are centralized in `Assets/Game/Scripts/Campaign/ChapterOneMissionCatalog.cs`.
- Objective evaluation and mission result scoring are handled by `ObjectiveManager` and `MissionResultBuilder`, using `GameRuntimeStats.Snapshot`.
- `GameRuntimeStats.Snapshot` covers the initial Phase 8 objective inputs: kills, elapsed mission time, protected civilians, buildings built, captured/destroyed buildings, own losses, and resources earned.
- `Screen_MatchOverlay/ObjectivePanel` is no longer only static target text: `MatchObjectivePanelController` binds it to the active mission session and live objective/star-goal progress while preserving fallback labels when no mission is active.
- `WarlineCaptureMissionSession` tracks the active Saga mission from Mission Briefing / Loadout into gameplay launch.
- `Screen_SagaMap` carries mission metadata on every Chapter 1 node, and `SagaMapScreenController` binds the selected info panel from mission config plus local completion/star progress. It also refreshes locked/available/selected node visuals from Saga progress so completing the required previous mission unlocks the next node without rebuilding the prefab.
- `MissionResultPopupController` binds runtime `MissionResultData` into `MissionResultPopup`, including the actual granted reward rows returned by `RewardService`; `WarlineCaptureMatchResultFlow` completes active missions from gameplay victory snapshots and routes back to Saga or Operation.
- `RewardService` is the first reward grant layer. Chapter 1 mission configs include Commander XP, Credits, first-clear unlock rewards, and authored Operation outcome rewards for every Chapter 1 mission. Mission Briefing previews use the same config, mission completion applies them through `SaveService`, and Saga progress is updated from the result. Operation rewards now also grant saved operation supplies plus targeted district trust, security, intel, and infrastructure, and Mission Briefing / Mission Result reward rows display those Operation reward labels and district targets. `ProgressionService` advances commander level from XP and accumulates account combat totals from mission results, `RewardTrackService` adds deterministic commander-level reward milestones and persisted claim state, and `MissionHistoryService` archives recent local mission results for profile history.
- Operation-launched missions prioritize Operation reward cards in Mission Briefing and Mission Result while Saga-launched missions keep the standard XP/credits/unlock ordering.
- `SaveService` writes split JSON save files for profile, Saga, Operation, Settings, and Quick Custom data; `SagaProgressStore` keeps local best-stars progress for the current Saga slice.
- `OperationService` provides the initial Persistent Operation simulation layer for district meters, Resources-backed Patrol/Scan/Aid/Raid/Repair/Evacuate/Build Outpost action costs, district-specific modifiers, raid routing intent, operation supplies, pending event rows, and end-of-day pressure. District state now includes secondary Operation metrics for trust, security, infrastructure, enemy influence, heat, and civilian risk; `WarlineCaptureOperationRuntime` loads/saves that state through `SaveService`, and `OperationDashboardScreenController`, `DistrictDetailScreenController`, and `WarlineCaptureOperationModalFlow` bind the first live dashboard/detail/modal slice. `Screen_DistrictDetail` exposes the six-action Operation ActionGrid, and dashboard/detail UI text now uses a shared metric formatter so all secondary metrics are presented consistently across cards, feeds, raid risk, and end-of-day summaries.
- `OperationEventData` now stores typed ledger metadata: district id, action type, category, severity, operation day, unread state, source metric, and metric value. `OperationDistrictEventRule` adds authored threshold alerts for heat, civilian risk, and enemy influence; `Screen_Inbox`, `Screen_Events`, and `Screen_CommandFeed` still preserve their visual-lock shell fallback text, but at runtime their lightweight Operation feed controllers bind them to that saved Operation event ledger and local system notices.
- `OperationIntelEvidenceData` stores scan evidence separately from event notifications. Scan actions append evidence rows with source event id, district, confidence, day, and unread state; `OperationIntelArchive` exposes shared latest/count/read helpers; `POP-08 Intel Reveal` now displays the latest evidence entry for the selected district and marks it read when View Intel is pressed.
- `Screen_CommanderProfile` now has a first live binding pass through `CommanderProfileScreenController`: saved profile wallet counters, commander level/XP, unlock counts, account combat totals, saved result history, reward-track eligibility, reward-track row buttons with modal detail/claim feedback, local profile tabs, and a claim CTA replace the previous static shell copy at runtime.

2D isometric production references:

- `Design/VisualReferences/README.md`
  Index for active 2D isometric concepts and production spike outputs.
- `Design/VisualReferences/2DIsometricConcepts/README.md`
  Exploratory 2D isometric concept references.
- `Design/VisualReferences/2DIsometricProduction/README.md`
  Active ISO-01 production spike index.
- `Design/VisualReferences/2DIsometricProduction/ISO-01_CityCommand_Target/ISO-01_CityCommand_ProductionTarget.png`
  Active ISO-01 production visual target.
- `Design/VisualReferences/2DIsometricProduction/ISO-01_CityCommand_ProductionBreakdown.md`
  Production breakdown for the ISO-01 City Command target.
- `Design/VisualReferences/2DIsometricProduction/GoldenAssets/README.md`
  First golden asset batch for the 2D isometric production spike.
- `Design/VisualReferences/2DIsometricProduction/UnitySpike/ISO01_TilemapSpike_Report.md`
  Manual Unity spike result for Tilemap sorting, scale, readability, and performance smoke.
- `Design/VisualReferences/2DIsometricProduction/RuntimePrototype/README.md`
  Folder index for ISO-02 runtime prototype captures and report.
- `Design/VisualReferences/2DIsometricProduction/RuntimePrototype/ISO02_RuntimePrototype_Report.md`
  Runtime prototype report for 2D isometric movement, sorting, overlay followers, captures, and performance smoke.

Audio design:

- `Design/WarlineCapture_Audio_Design_Guidelines.md`
  Audio direction, mixer buses, playback rules, event naming, UI/gameplay sound coverage, shared visual-feedback audio event ids, and generation guidance for WarlineCapture audio assets. Use with `Design/WarlineCapture_Visual_Feedback_VFX_Recommendations.md` when implementing feedback.

Monetization design:

- `Design/Monetization/WarlineCapture_Monetization_Strategy.md`
  Store/economy principles, wallet currencies, safe monetized content types, and no-pay-to-win guardrails.
- `Design/Monetization/WarlineCapture_Monetization_Store_Catalog.md`
  Design-facing catalog for starter packs, featured offers, currency bundles, armory items, cosmetics, and operation supplies.
- `Design/Monetization/WarlineCapture_Monetization_Visual_Targets.md`
  Store visual target index and decomposition guidance. Generated store images live under `Design/Monetization/Images`.

Marketing asset workflow:

- `Design/Marketing/README.md`
  Workflow for creating and validating marketing video samples from design docs, economy rules, monetization strategy, and visual-lock targets.
- `Design/Marketing/SampleVideo/WarlineCapture_Sample_Marketing_Video_QA.md`
  QA checklist for the current 20 second sample marketing video.
- `Design/Marketing/GenerativeVideoConcepts/README.md`
  AI generative concept-cinematic workflow for creative 3D-style marketing clips based on WarlineCapture concepts rather than UI screenshots.
- `Design/Marketing/GenerativeVideoConcepts/WarlineCapture_Generative_Cinematic_Shots.json`
  Provider-ready shot prompts and constraints for five cinematic trailer clips.
- `Tools/Marketing/create_sample_marketing_video.py`
  Repeatable local generator for the sample video, manifest, preview contact sheet, and QA report.
- `Tools/Marketing/generate_concept_video_jobs.py`
  Dry-run and provider-ready runner for generative-video job plans, Sora submission/poll/download, storyboard output, and QA reporting.

Art generation:

- `Design/WarlineCapture_Art_Asset_Requirements_Register.md`
  Production art approval checklist and summary. The companion CSV now tracks Commander Identity and ARIA assistant assets alongside combat, UI, Saga, and store art.
- `Design/WarlineCapture_Art_Asset_Requirements_Register.csv`
  Editable production asset register with planned paths, status, approval, and completion fields.
- `Design/WarlineCapture_Unit_Portrait_Art_Generation_Guide.md`
  Unit portrait generation guidance.

Visual lock notes:

- `Design/VisualLock`
  Canonical notes beside generated screen, popup, and prefab targets: all `SCN-*`, `POP-*`, and `PREFAB-*` target notes are indexed in `Design/README.md` and summarized by `Design/WarlineCapture_UIUX_Mockup_To_Canvas_Conversion_Plan.md`.
- `Design/VisualLock/SCN-01_SplashLoading/SCN-01_SplashLoading_CleanLandscape_Notes.md`
- `Design/VisualLock/SCN-03_CommanderProfile/SCN-03_CommanderProfile_CleanLandscape_Notes.md`
- `Design/VisualLock/SCN-04_SettingsAccessibility/SCN-04_SettingsAccessibility_CleanLandscape_Notes.md`
- `Design/VisualLock/SCN-05_SagaMap/SCN-05_SagaMap_CleanLandscape_Notes.md`
- `Design/VisualLock/SCN-06_MissionBriefing/SCN-06_MissionBriefing_CleanLandscape_Notes.md`
- `Design/VisualLock/SCN-07_LoadoutSquadPrep/SCN-07_LoadoutSquadPrep_CleanLandscape_Notes.md`
- `Design/VisualLock/SCN-08_RTSBattleHUD/SCN-08_RTSBattleHUD_CleanLandscape_Notes.md`
- `Design/VisualLock/SCN-09_BuildDrawerProduction/SCN-09_BuildDrawerProduction_CleanLandscape_Notes.md`
- `Design/VisualLock/SCN-10_UnitCommandWheel/SCN-10_UnitCommandWheel_CleanLandscape_Notes.md`
- `Design/VisualLock/SCN-11_OperationDashboard/SCN-11_OperationDashboard_CleanLandscape_Notes.md`
- `Design/VisualLock/SCN-12_DistrictDetailActions/SCN-12_DistrictDetailActions_CleanLandscape_Notes.md`
- `Design/VisualLock/SCN-13_QuickCustomGameSetup/SCN-13_QuickCustomGameSetup_CleanLandscape_Notes.md`
- `Design/VisualLock/POP-01_ThreatAlert/POP-01_ThreatAlert_CleanLandscape_Notes.md`
- `Design/VisualLock/POP-02_ConfirmRaid/POP-02_ConfirmRaid_CleanLandscape_Notes.md`
- `Design/VisualLock/POP-03_BuildPlacement/POP-03_BuildPlacement_CleanLandscape_Notes.md`
- `Design/VisualLock/POP-04_RewardUnlock/POP-04_RewardUnlock_CleanLandscape_Notes.md`
- `Design/VisualLock/POP-05_MissionResult/POP-05_MissionResult_CleanLandscape_Notes.md`
- `Design/VisualLock/POP-06_EndOfDayReport/POP-06_EndOfDayReport_CleanLandscape_Notes.md`
- `Design/VisualLock/POP-07_PauseOptions/POP-07_PauseOptions_CleanLandscape_Notes.md`
- `Design/VisualLock/POP-08_IntelReveal/POP-08_IntelReveal_CleanLandscape_Notes.md`
- `Design/VisualLock/PREFAB-01_ObjectiveTracker/PREFAB-01_ObjectiveTracker_CleanLandscape_Notes.md`
- `Design/VisualLock/PREFAB-02_SquadTray/PREFAB-02_SquadTray_CleanLandscape_Notes.md`
- `Design/VisualLock/PREFAB-03_BuildDrawer/PREFAB-03_BuildDrawer_CleanLandscape_Notes.md`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/README.md`
  Layered export reference for the RTS Battle HUD.

Balance and gameplay probes:

- `Design/WarlineCapture_Balancing_Automated_Test_Plan.md`
  Concrete implementation plan for balance harness tests, opt-in probes, metrics, report files, and future data sanity checks.
- `Design/WarlineCapture_Gameplay_Features_High_Level_Spec.md`
  See `Balance and Gameplay Probes` for the high-level rule: these tests are opt-in, report-oriented, and excluded from normal build validation.
- `Design/WarlineCapture_Gameplay_Features_Detailed_Spec.md`
  See `Phase 13 - Opt-In Balance and Gameplay Probes` for suggested folders, NUnit categories, metrics, report outputs, runner entry points, and build-validation boundaries.

## UI/UX Roadmap Summary

The target UI is a mobile landscape app shell:

- `SafeAreaRoot`
- `HeaderBar`
- `ContentRoot`
- `FooterBar`
- `ModalOverlay`
- `TooltipLayer`

Tactical play should use a dedicated match HUD:

- `TopHUD`: objectives, threat feed, resources
- `BottomHUD`: squad tray, command bar, minimap, build toggle
- `ContextOverlay`: build drawer, command wheel, contextual actions
- `ModalOverlay`: pause, warning, confirmation, result, reward popups

Recommended UI implementation order:

1. Add app shell and route controller.
2. Replace first screen with Main Menu / Mode Select.
3. Add Quick Custom Game setup using existing AI settings.
4. Upgrade tactical HUD layout around existing systems.
5. Add objective tracker, mission result, reward, and pause popups.
6. Add Saga Map, Mission Briefing, and Loadout.
7. Add Persistent Operation dashboard and district screens.

Current UI execution rule:

- Build each screen as a vertical slice, not as a separate visual-only pass or a separate functionality-only pass.
- For each screen, popup, and reusable panel, first lock a high-quality generated landscape visual target from the original design references, then build a real Unity Canvas from separate panels, sprites, icons, text, and controls.
- Do not create new VisualLock targets by merely cropping, padding, stretching, or upscaling the source spec JPGs. Source JPGs are references for content and layout; the accepted target method is a new generated `1672 x 941` landscape target in the WarlineCapture AAA mobile RTS HUD style, with notes and the generation prompt saved beside it.
- Never ship a full-screen mockup image as the UI. Mockups are targets and references only.
- Validate each screen at common Android landscape aspects, including 16:9 and 20:9.
- Optimize each accepted screen before moving on: shared sprites, 9-sliced frames, atlas labels, correct import settings, disabled raycasts on decorative graphics, and no transparent placeholder `Image` components.
- Keep shared UI kit pieces reusable across screens: outer screen frame, thin button chrome, tab buttons, animated button states, sliders, toggles, dropdowns, Oxanium TMP text, and atlas/import validation.
- Do not reuse heavy section/panel borders for buttons. Buttons, tabs, segmented controls, dropdowns, and launch actions need their own thinner cleaner chrome.
- Page titles use `Oxanium-Bold SDF`; other screen/control text uses `Oxanium-Light SDF` and should stay single-line unless the target explicitly needs paragraph copy.
- Dropdowns must leave a clear gap from their left labels. Do not let the dropdown rect touch the label rect even if the visible text appears shorter.
- Quick Custom-style numeric controls use a minus/value/plus stepper, not a generic equal-width segmented control. Large CTA labels such as `LAUNCH MISSION` stay Bold.
- Do not use text placeholders for mockup icons. Add proper replaceable icon sprites and keep them separate from panel/background art.
- Phase work should proceed screen by screen: target match, real canvas, navigation, runtime data, capture comparison, tests, then optimization.
- The reusable operational workflow for converting target UI references into real layered Canvas prefabs is saved in `Design/WarlineCapture_UIUX_Target_To_Canvas_Workflow_Guide.md`.

## Gameplay Roadmap Summary

The missing gameplay layer is the mode/session layer above the current RTS simulation.

Highest-priority gameplay systems:

- `GameModeDefinition`
- `ScenarioSetup`
- `GameLaunchPayload`
- `QuickGameConfig`
- `ObjectiveManager`
- `MissionResultData`
- `StarGoalConfig`
- `RewardConfig`
- `PlayerProfileState`
- `SagaProgress`
- `OperationState`
- `DistrictState`
- `SaveService`
- `AIProfileDefinition`
- `EncounterTemplate`

Recommended gameplay implementation order:

1. Quick Custom gameplay config and launch payload.
2. `GameBootstrap.BeginGameplay(GameLaunchPayload payload)` while preserving the current no-argument path.
3. Objective Manager with the first objective types.
4. Mission result, star scoring, and rewards.
5. Player profile, unlocks, and save/load.
6. Saga Chapter 1 playable loop.
7. Persistent Operation state, district actions, and end-of-day simulation.
8. AI profiles, encounter templates, and balance configs.

Initial objective types should include:

- Destroy all enemies.
- Survive duration.
- Build required structure.
- Produce required unit.
- Protect civilians.
- Keep unit losses below threshold.

Initial reward types should include:

- Commander XP.
- Money/resources.
- Unit unlock.
- Building unlock.
- Saga stars.
- Operation trust/security/intel changes.

## Architecture

- `Assets/Game/Scripts/Components`
  ECS component data for grid state, movement, combat, visuals, spawning, roads, buildings, and occupancy.
- `Assets/Game/Scripts/Systems`
  ECS simulation systems for movement, pathfinding, engagement, combat, occupancy, health, visuals, and respawn.
- `Assets/Game/Scripts/Authorings`
  Thin ECS authoring/baker adapters used by the scene or subscene at bake time.
- `Assets/Game/Scripts/Bootstrap`
  Runtime bootstrap and bootstrap-owned services that replace scene controller MonoBehaviours.
- `Assets/Game/Scripts/UI`
  Runtime UI and controller logic owned by the bootstrap.
- `Assets/Game/Scripts/Environment`
  Runtime world-generation, blockers, decoration, and environment services.
- `Assets/Game/Scripts/Configs`
  ScriptableObject configs for scene systems, authorings, and runtime services.

## Runtime Pattern

The project no longer uses scene-placed controller MonoBehaviours for gameplay systems like selection, building placement, roads, day/night, city spawning, blockers, decorations, or UI orchestration.

Instead:

- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs` is the scene entry point.
- Bootstrap creates the runtime systems with `new`.
- Bootstrap passes required dependencies explicitly through `Init(...)` and `BindDependencies(...)`.
- Bootstrap drives per-frame behavior through its own `Update`, `LateUpdate`, and `OnGUI`.

When adding a new runtime system:

- do not add it as a new scene `MonoBehaviour` controller
- create it as a plain runtime class unless it must be an ECS authoring/baker
- initialize it from `GameBootstrap`
- pass dependencies explicitly instead of using global lookups

## Config Pattern

System settings should live in `ScriptableObject` configs, not in public serialized fields on runtime systems.

Current rule:

- each runtime system should have a config asset
- each authoring component should be a thin config-driven baker adapter
- scene and subscene objects should mostly hold config references, not duplicated inline values

When adding a new configurable system:

- add a config type under `Assets/Game/Scripts/Configs`
- create and assign the asset in the scene/subscene or bootstrap as appropriate
- avoid adding new public serialized fields directly on runtime controller classes
- Unity serialized fields must use lower camel case, not PascalCase
- for configs and authorings, prefer lowercase serialized backing fields with PascalCase properties only when code-facing access is needed

## Scene And Subscene Rules

- `Game.unity` should stay clean and use `GameBootstrap` as the runtime owner.
- `GameSubScene.unity` may still contain ECS authoring components such as grid or initial-spawn authorings, because bakers need scene/subscene authoring data at bake time.
- Authorings in the subscene should remain thin and config-driven.

## Performance Direction

Recent project patterns favor:

- bootstrap-owned runtime services instead of scene-wide object searches
- explicit dependency injection instead of singleton/bootstrap lookups
- config-driven setup instead of duplicated serialized scene data
- cached registries and direct references instead of `Find*` APIs

Avoid introducing:

- `FindObjectOfType`, `FindAnyObjectByType`, `FindObjectsByType`, `GameObject.Find`, `Camera.main`, or similar global lookup patterns in gameplay code
- new runtime controller MonoBehaviours placed directly in the scene

## Implementation Rules

Keep these rules for upcoming UI and gameplay work:

- Do not expand `MenuView.cs` for new product features. New screens should use small controllers under `Assets/Game/Scripts/UI/Screens`, `Shell`, `Popups`, or `Components`.
- Do not replace the working tactical scene in one large change. Add route/screen infrastructure around it and migrate surfaces step by step.
- Do not separate visual lock from implementation for new UI screens. Each screen must be completed as a testable vertical slice before the next screen is started.
- Do not bake replaceable UI elements into large background art. Portraits, resources, buttons, icons, text, and panel chrome must remain separate Canvas elements.
- Do not let future screen generators fall back to generic panel borders for buttons or tabs. Use the thin shared button chrome and keep controls clear of section-title divider lines.
- Match each control family to the mockup before reuse: dropdowns, segmented difficulty buttons, numeric steppers, map stat cards, and CTA buttons each have their own proportions and borders.
- Keep `GameBootstrap.BeginGameplay()` working while adding the payload-based launch path.
- Use data/config assets for scenarios, objectives, rewards, AI profiles, and balance. Avoid hard-coded mission IDs or reward values in UI scripts.
- Use visible objective and reward data. Win/loss and star goals should not be hidden rules.
- Persist abstract game state first: profile, Saga progress, Operation state, settings, and last Quick Custom setup. Do not persist raw ECS world state initially.
- Keep diagnostics opt-in or covered by `LogAssert`. Unexpected logs can fail Unity tests.
- Respect Android landscape and safe-area layout from the start.
- Use `Assets/Game/Textures/Logo.png` as the WarlineCapture logo source.
- Keep debug tools, test hooks, and direct-play shortcuts out of release-facing flows.

## Testing

- Edit mode tests: `Assets/Tests/Editor`
- Play mode tests: `Assets/Tests/PlayMode`
- Build check: `dotnet build WarlineCapture.sln`
- Unity Android build entry: `BuildScript.BuildAndroid`

Recommended gates after meaningful changes:

1. Targeted EditMode tests for touched systems.
2. Full EditMode suite.
3. Targeted PlayMode smoke test when scene/bootstrap behavior changes.
4. Android build when launch flow, build settings, or player assemblies change.

## Notes

- `Library/`, `Temp/`, and other generated Unity folders are local/generated state.
- If a change affects baking, reimport the subscene and verify the baked entities in Unity.
