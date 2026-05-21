# WarlineCapture Design Index

Date: 2026-05-21

This folder is the source of truth for WarlineCapture product design, gameplay planning, UI/UX implementation, visual targets, audio direction, monetization planning, marketing asset workflow, and the active 3D single-map production direction.

## Current Alignment

- Product structure: one shared 3D operation-map RTS simulation wrapped by Campaign, Operations, and Skirmish. Internal legacy names such as Saga Campaign, Persistent Operation, and Quick Custom Game may remain only as runtime compatibility terms until updated.
- Active 3D direction: `WarlineCapture_3D_SingleMap_Gameplay_Direction.md` is the current source of truth for returning to full 3D, single large operation maps, prefab-catalog roster usage, and the command-base menu style.
- Gameplay north star: `WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md` locks the core fantasy, mission archetypes, threat families, Chapter 1 teaching arc, Operation week rhythm, and balance target bands before level-by-level content authoring.
- Command premise: `WarlineCapture_Command_Offensive_Premise_Alignment.md` aligns the accepted proactive field-commander framing with the 3D single-map direction.
- Large-scale movement: `WarlineCapture_LargeScale_Grid_Movement_Design.md` defines how the original grid-movement promise becomes an AAA mobile RTS design through readable squad command, tactical metadata, staged validation, and production-scale gates.
- Level and mission planning: `WarlineCapture_Level_And_Mission_Content_Plan.md` owns the shared mission spec template, high-level Campaign chapter set, Operations hooks, Skirmish probe mapping, and acceptance gate. Dedicated chapter docs under `SagaChapters` own chapter-specific mission matrices and specs.
- Map contract: the active map contract is one large 3D operation map with planning, briefing, minimap, deployment, threat, and battle views as overlays/camera states on the same world.
- FTUE and assistant: `WarlineCapture_FTUE_And_Command_Assistant_Design.md` defines the reusable ARIA command assistant, Chapter 1 FTUE flow, contextual recommendations, and safe assistant control takeover model. `WarlineCapture_AssistantPanel_M01_Implementation_Contract.md` is the current support/UI/gameplay handoff for `PREFAB-05_AssistantPanel` and M01 ARIA recommendation states.
- Agent coordination: `WarlineCapture_Agent_Coordination_Workflow.md` defines PM handoff, validation, cross-lane sync, tracking workflow, lane ownership, and commit/push rules for agents.
- Gameplay architecture: `Architecture/gameplay_solid_ecs_contract.md` defines the SOLID/ECS runtime contract, bootstrap responsibility boundaries, service/logging rules, and no-new-drift guardrails.
- Performance regression: `Architecture/performance_regression_contract.md` defines the structured-metrics, budget, FreezeDetect, and hot-path rules for preventing new performance regressions.
- Designer workflow: `WarlineCapture_Designer_Role_And_Documentation_Workflow.md` defines the Designer lane for README/design-index clarity, source-of-truth ordering, terminology alignment, product/design coherence, and documentation pruning recommendations.
- Gameplay layer: `GameLaunchPayload`, scenario setup, objectives, results, rewards, progression, persistence, Campaign, Operations, Skirmish, AI profiles, and encounter templates are planned in the gameplay feature specs.
- Combat catalog: `WarlineCapture_Combat_Catalog_And_Upgrade_Design.md` plus `BalanceConfigs/WarlineCapture_Combat_Balance_Config_v0_1.json` and `VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json` define all unit, building, skill, ability, and upgrade-track ids, including availability, unlock moments, implementation owners, and balance data separated from art data.
- UI layer: mobile landscape app shell, command-base menus, and battle HUD should be built as real Unity Canvas hierarchy from separate panels, sprites, icons, TMP text, and controls over the 3D operation-map flow.
- Feedback layer: `WarlineCapture_Visual_Feedback_VFX_Recommendations.md` defines shared UI motion, gameplay VFX, warning feedback, reward flyouts, popup transitions, and paired audio event ids for responsive play.
- Visual direction: new gameplay-facing battlefield art should use 3D town/base operation scenes with runtime units, buildings, civilians, vehicles, aircraft, markers, VFX, and metadata-backed command overlays.
- Main menu visual direction: `WarlineCapture_UIUX_MainMenu_Visual_Contract.md` now points at the command-base style with Campaign, Operations, Skirmish, Store, Commander, Settings, Credits, Supplies, Command, and Deploy Operation.
- Economy and rewards are locked by `WarlineCapture_Economy_Reward_Design.md`. Monetization and marketing claims use the same canonical resources, reward types, and disabled purchase states as the UI/gameplay alignment docs.

## Primary Reading Order

1. `GAME_DESIGN_REFERENCE.md`
2. `WarlineCapture_3D_SingleMap_Gameplay_Direction.md`
3. `WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md`
4. `WarlineCapture_Command_Offensive_Premise_Alignment.md`
5. `WarlineCapture_Combat_Catalog_And_Upgrade_Design.md`
6. `BalanceConfigs/WarlineCapture_Combat_Balance_Config_v0_1.json`
7. `VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json`
8. `WarlineCapture_AAA_Mobile_Game_Design_Document_v0_1.md`
9. `WarlineCapture_LargeScale_Grid_Movement_Design.md`
10. `WarlineCapture_AAA_Mobile_Technical_Targets.md`
11. `WarlineCapture_Level_And_Mission_Content_Plan.md`
12. `WarlineCapture_M01_FirstContact_Production_Contract.md`
13. `WarlineCapture_FTUE_And_Command_Assistant_Design.md`
14. `WarlineCapture_AssistantPanel_M01_Implementation_Contract.md`
15. `WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md`
16. `WarlineCapture_Designer_Role_And_Documentation_Workflow.md`
17. `WarlineCapture_Agent_Coordination_Workflow.md`
18. `Architecture/gameplay_solid_ecs_contract.md`
19. `Architecture/performance_regression_contract.md`
20. `WarlineCapture_Gameplay_Features_High_Level_Spec.md`
21. `WarlineCapture_Gameplay_Features_Detailed_Spec.md`
22. `WarlineCapture_UIUX_Implementation_High_Level_Spec.md`
23. `WarlineCapture_UIUX_Implementation_Detailed_Spec.md`
24. `WarlineCapture_Economy_Reward_Design.md`
25. `WarlineCapture_Balancing_Automated_Test_Plan.md`
26. `WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
27. `WarlineCapture_Visual_Feedback_VFX_Recommendations.md`
28. `WarlineCapture_UIUX_MainMenu_Visual_Contract.md`
29. `WarlineCapture_UIUX_Mockup_To_Canvas_Conversion_Plan.md`
30. `WarlineCapture_UIUX_Target_To_Canvas_Workflow_Guide.md`

## Core Product And Gameplay

- `GAME_DESIGN_REFERENCE.md` - compact reference for the currently implemented RTS simulation.
- `WarlineCapture_Combat_Catalog_And_Upgrade_Design.md` - canonical unit, vehicle, air, sea, building, skill, ability, and upgrade-track design, with availability, unlock, implementation, and balance/visual data separation rules.
- `BalanceConfigs/README.md` - balance config folder rules.
- `BalanceConfigs/WarlineCapture_Combat_Balance_Config_v0_1.json` - gameplay/economy config for 57 units, 30 buildings, 27 skills/abilities, and 40 upgrade tracks.
- `VisualConfigs/README.md` - visual config folder rules.
- `VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json` - visual-only companion entries for combat entities, abilities, and upgrade tracks.
- `WarlineCapture_AAA_Mobile_Game_Design_Document_v0_1.md` - high-level AAA mobile game direction.
- `WarlineCapture_AAA_Mobile_Game_Design_Document_v0_1.docx` - authored document version of the AAA mobile GDD.
- `WarlineCapture_3D_SingleMap_Gameplay_Direction.md` - active 3D single-map direction, including mode alignment, prefab-catalog roster usage, world scale, and UI menus that need updating.
- `WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md` - gameplay north star, content grammar, mission archetypes, threat families, Chapter 1 teaching arc, Operation week rhythm, balance target bands, and mission acceptance checklist.
- `WarlineCapture_Command_Offensive_Premise_Alignment.md` - proactive command-operation framing aligned to the 3D single-map direction.
- `WarlineCapture_LargeScale_Grid_Movement_Design.md` - AAA mobile movement design for staged large-scale grid movement, squad-scale command, metadata-backed 3D operation maps, UI feedback, mission patterns, and validation gates.
- `WarlineCapture_AAA_Mobile_Technical_Targets.md` - concrete device-tier, frame, scale, marker, readability, and validation targets for the AAA mobile promise.
- `WarlineCapture_M01_Metric_Scale_Readability_Contract.md` - M01 tactical metric scale and readability contract for soldier/building anchors, selection treatment, movement animation, and ECS/atlas-backed public unit presentation.
- `WarlineCapture_Level_And_Mission_Content_Plan.md` - required mission spec template, high-level Campaign chapter set, Operations mission hooks, Skirmish probe mapping, balance targets, and mission acceptance gate.
- `WarlineCapture_M01_FirstContact_Production_Contract.md` - concrete first playable slice contract for M01 First Contact, including map metadata anchors, UI command feedback, FTUE targets, asset manifest, audio/VFX requirements, and validation gates.
- `WarlineCapture_FTUE_And_Command_Assistant_Design.md` - first-time user experience and reusable ARIA command assistant design, including Chapter 1 tutorial steps, contextual recommendations, safe control takeover, data model, UI surfaces, and validation plan.
- `WarlineCapture_AssistantPanel_M01_Implementation_Contract.md` - implementation contract for `PREFAB-05_AssistantPanel`, M01 ARIA recommendation states, runtime data fields, Show Me / Do It / Stop behavior, player-control cancellation, `BattleHudGameplayBridge` integration, asset-register implications, and acceptance checks.
- `WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md` - runtime wiring contract for M01 ARIA assistant services, context data flow, recommendation transitions, typed intents, save/session fields, button rules, invalid-command recovery, and validation tests.
- `WarlineCapture_Agent_Coordination_Workflow.md` - PM assistant operating workflow for agent handoffs, cross-lane contract changes, validation gates, and tracking updates.
- `WarlineCapture_Designer_Role_And_Documentation_Workflow.md` - Designer lane workflow for README/design-index optimization, source-of-truth hierarchy, terminology alignment, documentation pruning, and product/design coherence reviews.
- `Architecture/gameplay_solid_ecs_contract.md` - gameplay SOLID/ECS architecture contract, including bootstrap composition boundaries, ECS-first runtime rules, service/logging guidance, and no-new-drift migration rules.
- `Architecture/performance_regression_contract.md` - performance regression contract for structured frame/system/GC metrics, scenario budgets, FreezeDetect usage, hot-path rules, and ratcheted performance gates.
- `SagaChapters/README.md` - Saga chapter design folder index and update rules.
- `SagaChapters/WarlineCapture_Saga_Chapter01_First_Response.md` - Chapter 1 / First Response mission matrix and detailed specs for all five Chapter 1 missions.
- `SagaChapters/WarlineCapture_Saga_Chapter02_Broken_Grid.md` - Chapter 2 / Broken Grid high-level chapter arc.
- `SagaChapters/WarlineCapture_Saga_Chapter03_Hidden_Network.md` - Chapter 3 / Hidden Network high-level chapter arc.
- `SagaChapters/WarlineCapture_Saga_Chapter04_Air_And_Armor.md` - Chapter 4 / Air And Armor high-level chapter arc.
- `SagaChapters/WarlineCapture_Saga_Chapter05_Citywide_Command.md` - Chapter 5 / Citywide Command high-level chapter arc.
- `WarlineCapture_Gameplay_Features_High_Level_Spec.md` - mode, objective, reward, progression, persistence, Campaign, Operations, and Skirmish roadmap. Internal legacy naming may remain where runtime code has not yet been renamed.
- `WarlineCapture_Gameplay_Features_Detailed_Spec.md` - code-oriented implementation plan for gameplay systems.
- `WarlineCapture_Economy_Reward_Design.md` - canonical resources, reward types, resource strips, and popup/panel gameplay goals.
- `WarlineCapture_Balancing_Automated_Test_Plan.md` - implementation plan for balance harness tests, opt-in probes, metrics, reports, and data sanity checks.
- `AI_CONTROLLER_DESIGN.md` - AI controller architecture and tuning companion.

## UI/UX

- `WarlineCapture_UIUX_Gameplay_Element_Alignment.md` - gameplay contract matrix for every planned UI element, including route/effect, data source, enable rule, and locked/designed-unavailable/read-only state.
- `WarlineCapture_Visual_Feedback_VFX_Recommendations.md` - prioritized shared UI feedback, popup motion, gameplay VFX, reward flyout, critical warning, and paired audio recommendations for responsive gameplay and UI.
- `WarlineCapture_UIUX_Implementation_High_Level_Spec.md` - app shell, routing, screen strategy, and implementation phases.
- `WarlineCapture_UIUX_Implementation_Detailed_Spec.md` - detailed UI implementation and prefab/component plan.
- `WarlineCapture_UIUX_Mockup_To_Canvas_Conversion_Plan.md` - canonical visual-lock inventory and conversion rules.
- `WarlineCapture_UIUX_Target_To_Canvas_Workflow_Guide.md` - operational workflow for converting targets into layered Unity Canvas prefabs.
- `WarlineCapture_UIUX_MainMenu_Visual_Contract.md` - active Main Menu visual contract.
- `WarlineCapture_UIUX_Runtime_Optimization_Plan.md` - UI runtime optimization and validation direction.
- `VisualTargets/UIFlowNavigationTree.svg` - visual UI navigation tree showing Splash, Main Menu branches, game-mode paths, overlays, and safe returns.
Historical immediate UI phase plans have been archived under `Archive/LegacyUI_2026-05-21/ImmediateImplementationPlans/`. They are implementation history only; use the active UI/UX specs and visual-lock workflow above for new 3D-aligned work.

## Visual Direction And Production Art

- `WarlineCapture_3D_SingleMap_Gameplay_Direction.md` - active visual/gameplay production direction for 3D operation maps and command-base menu art.
- `WarlineCapture_M01_FirstContact_Production_Contract.md` - first implementation handoff for M01 First Contact and the target contract for the UI/gameplay/art agents.
- `WarlineCapture_Art_Asset_Requirements_Register.md` and `WarlineCapture_Art_Asset_Requirements_Register.csv` - consolidated approval checklist for production art, including combat, UI, Saga, store, Commander Identity, and ARIA assistant assets.
- `VisualReferences/README.md` - visual reference folder index.
- `VisualLockLayered/README.md` - active 3D-direction layered visual-lock inventory, pack shape, and acceptance gate for new implementation-ready screen targets.
- `VisualLock/README.md` - active scratch/reference area for temporary single-image drafts before they graduate to layered packs.
- `Archive/LegacyVisualLock_2026-05-22/ARCHIVE_MANIFEST.md` - archive manifest for the previous VisualLock and VisualLockLayered folders.

## Screen, Popup, And Prefab Visual Locks

The active target inventory now lives in `VisualLockLayered/README.md`. New screen, popup, and prefab targets should be created there with separated layers, `layer_manifest.json`, contact sheet, and README before any Unity Canvas implementation starts.

The previous visual-lock folders were moved to `Archive/LegacyVisualLock_2026-05-22/`. They are retained for history and comparison only.

## Audio, Monetization, Marketing, And Art Generation

- `WarlineCapture_Audio_Design_Guidelines.md` - audio identity, buses, event names, playback rules, shared visual-feedback audio cues, and generation guidance.
- `WarlineCapture_Visual_Feedback_VFX_Recommendations.md` - cross-discipline feedback matrix for UI motion, gameplay VFX, reward/account-state feedback, critical warnings, and paired audio event ids.
- `WarlineCapture_Art_Asset_Requirements_Register.md` - production art approval workflow and companion CSV index.
- `Monetization/WarlineCapture_Monetization_Strategy.md` - monetization principles and guardrails.
- `Monetization/WarlineCapture_Monetization_Store_Catalog.md` - design-facing starter pack, store, and offer catalog.
- `Monetization/WarlineCapture_Monetization_Visual_Targets.md` - store visual target index and prompt guidance.
- `Marketing/README.md` - marketing asset workflow, sample video outputs, source-image rules, and QA gates.
- `Marketing/SampleVideo/WarlineCapture_Sample_Marketing_Video_QA.md` - QA checklist for the current generated sample video.
- `Marketing/GenerativeVideoConcepts/README.md` - concept-cinematic AI video workflow for creative 3D-style marketing shots that do not use UI screenshots as footage.
- `Marketing/GenerativeVideoConcepts/WarlineCapture_Generative_Cinematic_Brief.md` - creative direction and approval criteria for AI-generated trailer clips.
- `Marketing/GenerativeVideoConcepts/WarlineCapture_Generative_Cinematic_Shots.json` - API-ready generative-video shot prompts and constraints.
- `Marketing/GenerativeVideoConcepts/WarlineCapture_Generative_Cinematic_QA.md` - automated and human QA checklist for generated concept clips.
- `WarlineCapture_Unit_Portrait_Art_Generation_Guide.md` - unit portrait generation guidance.

## Alignment Rules For Future Changes

- Update this index and the root `README.md` whenever a new design document is added.
- When older docs conflict with `WarlineCapture_3D_SingleMap_Gameplay_Direction.md`, the 2026-05-21 3D single-map direction wins.
- Do not create new active design work that assumes 2.5D isometric macro tiles or separate strategic/tactical maps unless PM explicitly reopens that decision.
- Treat archived source mockup JPG references under `Archive/LegacyUI_2026-05-21/WarlineCapture_UIUX_Codex_Package` as layout/content reference only; active implementation-ready targets live under `VisualLockLayered`.
- Keep canonical generated UI targets under `VisualLockLayered`; use `VisualLock` only for scratch/reference drafts.
- Keep production gameplay art references under `VisualReferences`.
- Keep combat gameplay numbers in `BalanceConfigs` and combat art/presentation references in `VisualConfigs`; do not duplicate balance values into visual files.
- When two docs disagree, prefer `WarlineCapture_3D_SingleMap_Gameplay_Direction.md` for gameplay/art direction, `WarlineCapture_UIUX_MainMenu_Visual_Contract.md` for Main Menu visuals, and the target-to-canvas workflow for Canvas implementation mechanics.
- Do not use generated visual references as direct implementation screenshots unless the relevant workflow document explicitly says that target is temporary runtime art.
