# WarlineCapture Project State Dashboard

Generated from `Design/WarlineCapture_Project_State_Source.json` on `2026-05-21`.

> Do not manually edit this dashboard. Update the JSON source and run `python3 Tools/ProjectState/generate_project_state_dashboard.py`.

## Quick Read

- Overall estimated completion: **33%**
- Estimated 100% planning date: **2027-03-31**
- Forecast range: **2027-02-28 to 2027-05-31**
- Forecast confidence: **low**
- Forecast update cadence: **weekly, plus after major accepted milestone reports**
- Forecast basis: Updated after the accepted 3D single-map design redirection. Forecast remains range-based given remaining dependency risk across 3D operation-map validation, M01-M05 gameplay, UI visual lock, QA/HCI, balance, audio, store, and release hardening.
- Plans tracked: **11**
- Roadmap stages tracked: **5**
- Done: **1**
- In progress: **5**
- On hold: **1**
- Blocked: **0**
- Planned: **4**

## Roadmap

| Stage | Status | Completion | Depends On | Summary |
| --- | --- | ---: | --- | --- |
| Foundation | Done | 75% | - | Core RTS simulation, Unity project structure, design docs, and first UI/visual systems exist. |
| Visual Direction Lock | In Progress | 41% | - | 3D single-map direction, command-base menu style, operation-map metadata, validation scenes, and review captures are being formalized. |
| Asset Vertical Slice | In Progress | 8% | plan.3d_operation_map_validation | 3D world gameplay asset validation is being redirected around existing prefab configs, large operation maps, and command-base UI render needs. |
| Playable Vertical Slice | In Progress | 18% | plan.3d_operation_map_validation, plan.3d_world_gameplay_assets, plan.command_ui_render_assets | M01 integrated playable route is underway; the next playable-slice planning pass must align captures, UI, and QA/HCI expectations to the 3D single-map direction. |
| Production Scale | Planned | 8% | stage.playable_vertical_slice | Scale validated pipelines across large 3D operation maps, Campaign/Operations/Skirmish modes, UI surfaces, economy, balance, and content. |

## Plan Status

| Plan | Area | Status | Completion | Source |
| --- | --- | --- | ---: | --- |
| Core RTS Simulation | Gameplay | Done | 74% | `Design/GAME_DESIGN_REFERENCE.md` |
| 3D Operation Map Validation | Art/Runtime | In Progress | 29% | `Design/WarlineCapture_3D_SingleMap_Gameplay_Direction.md` |
| 3D World Gameplay Assets | Art/Runtime | In Progress | 14% | `Assets/Game/Configs/Prefabs` |
| Build, Test, Release | Engineering | In Progress | 45% | `README.md` |
| Marketing Assets | Marketing | In Progress | 35% | `Design/Marketing/README.md` |
| UI Visual Lock And Canvas Conversion | UI | In Progress | 45% | `Design/WarlineCapture_UIUX_Mockup_To_Canvas_Conversion_Plan.md` |
| Command UI Render Assets | UI/Art | On Hold | 5% | `Design/WarlineCapture_UIUX_MainMenu_Visual_Contract.md` |
| Audio | Audio | Planned | 18% | `Design/WarlineCapture_Audio_Design_Guidelines.md` |
| Game Modes, Missions, Progression | Product/Gameplay | Planned | 40% | `Design/WarlineCapture_Gameplay_Features_Detailed_Spec.md` |
| Monetization And Store | Product/UI | Planned | 18% | `Design/Monetization/WarlineCapture_Monetization_Strategy.md` |
| Balance And Automated Probes | QA/Gameplay | Planned | 20% | `Design/WarlineCapture_Balancing_Automated_Test_Plan.md` |

## Dependency Map

```mermaid
flowchart TD
  plan_3d_operation_map_validation["3D Operation Map Validation"]
  plan_3d_world_gameplay_assets["3D World Gameplay Assets"]
  plan_audio["Audio"]
  plan_balance_probe["Balance And Automated Probes"]
  plan_build_release["Build, Test, Release"]
  plan_command_ui_render_assets["Command UI Render Assets"]
  plan_core_simulation["Core RTS Simulation"]
  plan_game_modes_progression["Game Modes, Missions, Progression"]
  plan_marketing["Marketing Assets"]
  plan_monetization_store["Monetization And Store"]
  plan_ui_visual_lock["UI Visual Lock And Canvas Conversion"]
  stage_asset_vertical_slice["Asset Vertical Slice"]
  stage_foundation["Foundation"]
  stage_playable_vertical_slice["Playable Vertical Slice"]
  stage_production_scale["Production Scale"]
  stage_visual_direction["Visual Direction Lock"]
  plan_3d_operation_map_validation --> stage_asset_vertical_slice
  plan_3d_operation_map_validation --> stage_playable_vertical_slice
  plan_3d_world_gameplay_assets --> stage_playable_vertical_slice
  plan_command_ui_render_assets --> stage_playable_vertical_slice
  stage_playable_vertical_slice --> stage_production_scale
  plan_3d_operation_map_validation --> plan_3d_world_gameplay_assets
  plan_3d_operation_map_validation --> plan_command_ui_render_assets
  plan_command_ui_render_assets --> plan_ui_visual_lock
  stage_playable_vertical_slice --> plan_game_modes_progression
  plan_ui_visual_lock --> plan_game_modes_progression
  stage_playable_vertical_slice --> plan_balance_probe
  plan_ui_visual_lock --> plan_monetization_store
  plan_game_modes_progression --> plan_monetization_store
  stage_playable_vertical_slice --> plan_audio
  plan_ui_visual_lock --> plan_audio
  stage_playable_vertical_slice --> plan_marketing
  stage_playable_vertical_slice --> plan_build_release
```

## Completion By Plan

```mermaid
xychart-beta
  title "Estimated Completion By Plan"
  x-axis "Plan" ["core simulation", "3d operation map v", "3d world gameplay ", "command ui render ", "ui visual lock", "game modes progres", "balance probe", "monetization store", "audio", "marketing", "build release"]
  y-axis "Percent" 0 --> 100
  bar [74, 29, 14, 5, 45, 40, 20, 18, 18, 35, 45]
```

## Detailed State

### Core RTS Simulation

- Status: **Done**
- Completion: **74%**
- Area: `Gameplay`
- Source: `Design/GAME_DESIGN_REFERENCE.md`
- Depends on: -
- Summary: Grid RTS simulation has units, buildings, resources, roads, production, AI, combat, transport, radar warnings, minimap, and Android build support.

**Done**
- Core unit/building/resource/road systems exist.
- AI economy, build, production, squad, targeting, and combat systems exist.
- Transport, base breach, radar warnings, minimap, runtime stats, and Android support exist.
- M01 EditMode playable runtime slice is accepted with focused test coverage and stable runtime ids.
- Assistant typed-command hooks are accepted for selection, move, and attack via the command executor boundary.

**In Progress**
- Aligning existing runtime simulation with the new 3D single-map presentation and operation-map metadata direction.

**On Hold**
- -

**Next**
- Bind metadata-backed 3D operation-map data to movement/pathing probes.
- Validate gameplay layers over the selected 3D operation-map direction.

### 3D Operation Map Validation

- Status: **In Progress**
- Completion: **29%**
- Area: `Art/Runtime`
- Source: `Design/WarlineCapture_3D_SingleMap_Gameplay_Direction.md`
- Depends on: -
- Summary: The active direction is 3D single-map operation play; validation must prove large 3D maps, many units, camera states, metadata overlays, and mobile performance.

**Done**
- The 3D single-map direction is documented and accepted.
- Existing Unity runtime already supports grid movement, base building, production, AI, combat, transport, warnings, minimap, and Android build support.
- Prefab config catalog exists under Assets/Game/Configs/Prefabs for units, civilians, hostile variants, vehicles, aircraft, buildings, barriers, and support structures.

**In Progress**
- Audit and cleanup of stale superseded-direction references.

**On Hold**
- Production-scale 3D map expansion waits for a focused validation plan and PM acceptance.

**Next**
- Define a 3D operation-map validation plan for camera states, metadata overlays, many-unit performance, and civilian readability.
- Select or build the first large 3D town/base validation scene using existing prefab catalog assets.

### 3D World Gameplay Assets

- Status: **In Progress**
- Completion: **14%**
- Area: `Art/Runtime`
- Source: `Assets/Game/Configs/Prefabs`
- Depends on: 3D Operation Map Validation
- Summary: Existing prefab config assets provide the canonical unit/building roster for 3D gameplay; production work must validate these assets in large 3D operation maps.

**Done**
- Prefab config catalog includes soldiers, civilians, insurgents, contractors, pilots, ground vehicles, aircraft, missiles, military buildings, civilian buildings, utilities, and barriers.
- Display names and descriptions exist in config assets and are now design-canonical for UI/tooltips/briefings.

**In Progress**
- -

**On Hold**
- New asset generation should wait until the 3D operation-map validation target is approved.

**Next**
- Audit which prefab configs already have production-ready 3D presentation.
- Validate infantry, civilian, hostile, vehicle, aircraft, and building readability in the first large 3D operation scene.
- Define missing 3D asset states, LODs, selection markers, damage states, and VFX requirements.

### Command UI Render Assets

- Status: **On Hold**
- Completion: **5%**
- Area: `UI/Art`
- Source: `Design/WarlineCapture_UIUX_MainMenu_Visual_Contract.md`
- Depends on: 3D Operation Map Validation
- Summary: UI render assets must align to the command-base menu style and 3D operation-map content, starting with SCN-02B MainMenuAlt.

**Done**
- SCN-02B MainMenuAlt layered command-base package exists.
- Main Menu visual contract now points to the command-base target.

**In Progress**
- -

**On Hold**
- Broad UI render refresh waits on prioritized screen-by-screen target updates.

**Next**
- Promote command-base render language across Campaign, Operations, Skirmish, Store, Commander, and HUD-adjacent screens.
- Validate UI renders inside target Canvas captures, not just standalone PNGs.

### UI Visual Lock And Canvas Conversion

- Status: **In Progress**
- Completion: **45%**
- Area: `UI`
- Source: `Design/WarlineCapture_UIUX_Mockup_To_Canvas_Conversion_Plan.md`
- Depends on: Command UI Render Assets
- Summary: UI target docs, layered workflow, prefab builders, and several generated UI assets/prefabs exist, but many surfaces still need layer-pack validation and rendered capture comparison.

**Done**
- UI/UX specs and target-to-canvas workflow exist.
- Legacy VisualLock and VisualLockLayered folders were archived; the active `Design/VisualLockLayered` reset now defines the 3D-direction screen/popup/prefab inventory.
- Several UI prefabs and generated assets are present.
- UI audit readiness report now separates interaction audit from final visual-lock acceptance and records the current full EditMode gate.
- PREFAB-04 assistant button target lock and production prefab are accepted for the M01 HUD entry surface.
- Assistant runtime binding (live panel data, typed Do It, result-flow Stop, takeover/release visibility) is accepted with focused tests.

**In Progress**
- Layer-pack gate and target-vs-capture requirements are being tightened.
- Popup and tactical HUD continuation work is underway.
- Integrated M01 capture matrix (16:9/20:9) is in progress for QA/HCI Gate 4 readability sign-off.

**On Hold**
- Gameplay-facing UI content art should wait for command-base/3D operation render target updates.

**Next**
- Use layer-pack gate before more prefab implementation.
- Bind approved UI render assets into squad tray, mission briefing, reward unlock, mode cards, and build drawer.
- Run rendered capture comparisons for target surfaces.

### Game Modes, Missions, Progression

- Status: **Planned**
- Completion: **40%**
- Area: `Product/Gameplay`
- Source: `Design/WarlineCapture_Gameplay_Features_Detailed_Spec.md`
- Depends on: Playable Vertical Slice, UI Visual Lock And Canvas Conversion
- Summary: Campaign, Operations, Skirmish, objectives, rewards, progression, and mission grammar are specified but need cleanup around the 3D single-map direction.

**Done**
- Mode direction and gameplay feature specs exist.
- Mission/content grammar and campaign chapter docs exist.
- Economy/reward design exists.
- Chapter 1 mission configs, objective/result scoring, reward grants, campaign node binding/unlocks, Mission Briefing reward previews, Mission Result reward rows, and initial Operations persistence-backed state are implemented.
- Operation action simulation now records operation supplies, completed action count, and pending event rows for dashboard and future Inbox/End-of-Day surfaces.
- Operation actions now use a configurable cost/delta/event layer and block supply-gated actions when operation supplies are too low.
- Operation action tuning now loads from the authored Resources-backed OperationActionConfigSet asset while preserving code defaults as a fallback.
- Operation district-specific action modifiers now provide the first authored district consequence and event-copy layer.
- Inbox, Events, and Command Feed route shells now bind to the saved Operation event ledger at runtime while preserving visual-lock fallback text.
- Operation event ledger entries now carry typed category, severity, district, action, day, and unread metadata for future filters and report surfaces.
- Scan actions now create saved intel evidence archive rows, and POP-08 Intel Reveal reads the latest evidence for the selected district.
- OperationIntelArchive now provides shared latest/count/read helpers, POP-08 marks viewed evidence read, and Inbox, Events, and Command Feed route shells display evidence confidence/read metadata.
- Operations district state now includes trust, security, infrastructure, enemy influence, heat, and civilian risk metrics; authored actions and district modifiers can mutate those secondary consequences.
- OperationDistrictEventRule now provides authored heat, civilian-risk, and enemy-influence threshold alerts with source metric/value metadata in the saved event ledger.
- Raid confirmation and End Day reports now bind secondary Operation metrics directly instead of using stability/threat proxy labels.
- Operations authored action tables now cover Repair, Evacuate, and Build Outpost in addition to Patrol, Scan, Aid, and Raid, including district-specific modifiers and typed event categories.
- District Detail now exposes a six-action Operation ActionGrid that binds Patrol, Drone Scan, Raid, Repair, Evacuate, and Build Outpost to live OperationService state.
- Operation dashboard and district detail cards now share a secondary-metric text contract for trust, security, infrastructure, enemy influence, heat, civilian risk, stability, and intel.
- RewardService now grants Operation rewards into saved Operation state, including operation supplies and targeted district trust, security, intel, and infrastructure.
- Mission Briefing, generated fallback reward previews, and Mission Result reward rows now display Operation rewards with readable labels and district targets.
- Every Chapter 1 mission now carries an authored Operation outcome reward: operation supply plus targeted North Bridge, Old Market, or Port Breach metric gains.
- Operations-launched mission sessions now prioritize Operations reward rows in Mission Briefing and Mission Result, while Campaign-launched sessions keep default reward ordering.
- ProgressionService now provides the first commander XP table, derived commander level updates, and account combat-stat accumulation from mission results.
- RewardTrackService now provides deterministic commander-level reward milestones, persisted claimed-node ids, eligibility checks, and first claim grants.
- MissionHistoryService now archives recent local mission result summaries into saved profile data for the Commander Profile history tab.
- CommanderProfileScreenController now binds saved profile wallet, level, unlock, win/loss, combat-total, saved recent mission report data, reward-track eligibility, claimable reward-track row buttons with modal detail/claim feedback, local tab content, and a first-claim CTA into SCN-03 Commander Profile.

**In Progress**
- Aligning mission/map ids and UI surfaces with the 3D operation-map direction.
- Final visual-lock presentation for every secondary metric.

**On Hold**
- Production mission validation waits for the 3D playable vertical slice.

**Next**
- Build Chapter 1 vertical slice against the selected 3D operation-map validation target.
- Wire objectives/results/rewards into UI surfaces with real content art.
- Create final visual-lock layouts for every secondary metric.

### Balance And Automated Probes

- Status: **Planned**
- Completion: **20%**
- Area: `QA/Gameplay`
- Source: `Design/WarlineCapture_Balancing_Automated_Test_Plan.md`
- Depends on: Playable Vertical Slice
- Summary: Balance config and report writer/test direction exist, but large-scale gameplay probes need the playable vertical slice.

**Done**
- Balance config exists.
- Balance report/test concepts exist.
- First opt-in Skirmish compatibility probe, report writer, menu runner, and documented RunAll entry point exist.
- Shared BalanceProbeDefinition and BalanceMetricSample types now support both QuickCustom_Default_Medium and QuickCustom_Hard_Swarm.
- Chapter 1 mission reward configs now have a normal data-sanity gate for unique ids, positive amounts, required targets, and first-clear duplicate fallbacks.

**In Progress**
- -

**On Hold**
- Large 3D operation-map stress and economy probes wait on the playable validation target.

**Next**
- Run opt-in movement/combat/economy probes after 3D operation-map runtime binding.
- Classify reports and feed findings back into balance config.

### Monetization And Store

- Status: **Planned**
- Completion: **18%**
- Area: `Product/UI`
- Source: `Design/Monetization/WarlineCapture_Monetization_Strategy.md`
- Depends on: UI Visual Lock And Canvas Conversion, Game Modes, Missions, Progression
- Summary: Monetization principles, store catalog, and visual targets exist; production store UI and economy sanity checks are later work.

**Done**
- Strategy, store catalog, and visual targets exist.

**In Progress**
- -

**On Hold**
- Store production waits on core UI visual-lock and economy/reward pipeline maturity.

**Next**
- Bind store catalog to safe economy data.
- Create layered store UI and validate rendered captures.

### Audio

- Status: **Planned**
- Completion: **18%**
- Area: `Audio`
- Source: `Design/WarlineCapture_Audio_Design_Guidelines.md`
- Depends on: Playable Vertical Slice, UI Visual Lock And Canvas Conversion
- Summary: Audio direction and generation tooling exist, but production event integration is incomplete.

**Done**
- Audio guideline doc exists.
- Audio generation helper exists.

**In Progress**
- -

**On Hold**
- Final cues wait on locked gameplay/UI event lists.

**Next**
- Prioritize first vertical-slice cues for UI, movement, combat, construction, destruction, and alerts.

### Marketing Assets

- Status: **In Progress**
- Completion: **35%**
- Area: `Marketing`
- Source: `Design/Marketing/README.md`
- Depends on: Playable Vertical Slice
- Summary: Sample video and generative concept workflows exist; final marketing should wait for approved visual/gameplay slice.

**Done**
- Sample video workflow exists.
- Generative cinematic shot plan exists.

**In Progress**
- Marketing concepts track current design direction.

**On Hold**
- Final marketing captures wait on approved playable visual slice.

**Next**
- Refresh marketing visuals after the 3D operation-map slice looks and plays close to target.

### Build, Test, Release

- Status: **In Progress**
- Completion: **45%**
- Area: `Engineering`
- Source: `README.md`
- Depends on: Playable Vertical Slice
- Summary: Unity version, packages, Android support, and tests exist; release hardening depends on vertical-slice completion.

**Done**
- Unity project is set up.
- Android build support exists.
- EditMode/PlayMode tests exist.

**In Progress**
- Maintaining batch Unity validation for generated scenes and tests.

**On Hold**
- Release hardening waits on playable vertical slice scope.

**Next**
- Keep CI/build validation green as art/runtime/UI integrations land.
- Add targeted tests for new asset bindings and map adapters.
