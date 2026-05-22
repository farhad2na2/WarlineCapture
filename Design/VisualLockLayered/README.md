# WarlineCapture VisualLockLayered

Date: 2026-05-22

This folder is the active destination for new implementation-ready UI visual targets. The previous layered packs were archived to `Design/Archive/LegacyVisualLock_2026-05-22/VisualLockLayered`.

## Direction

All new packs must align with:

- Full 3D single-map mobile RTS gameplay.
- Command-base menu presentation.
- Campaign / Operations / Skirmish player-facing modes.
- 3D operation-map planning, briefing, minimap, deployment, threat, and battle camera states.
- Gameplay scene references for menu/card/HUD imagery: `Assets/Game/Scenes/Demo.unity` and `Assets/Game/Scenes/Demo2.unity`.
- Prefab-catalog unit/building display names and descriptions from `Assets/Game/Configs/Prefabs`.
- Armory roster inspection through `SCN-19 Armory` and `POP-09 Ability / Upgrade Detail`.

## Required Pack Shape

Each active surface should use this folder shape:

```text
Design/VisualLockLayered/<SurfaceId>/
  README.md
  reference/
    <SurfaceId>_Landscape_Target.png
  layers/
  layer_manifest.json
  generated_one_go/
    layers_contact_sheet.png
  validation/
```

Do not start Unity Canvas implementation for a surface until the pack has the reference image, separated layers, manifest, contact sheet, and README.

Use `WORKFLOW_V15_3D_GREENSCREEN.md` and `prompts/visual_lock_layered_v15_3d_green_background.md` for the active UI-agent workflow: generate target and layers, use transparent PNGs or clean `#00ff00` chroma-key backgrounds, extract to alpha, write the manifest, validate the layer pack, then convert to Unity Canvas.

For `SCN-02_MainMenu`, start with reference-only mockup candidates in `SCN-02_MainMenu/REFERENCE_MOCKUP_REQUESTS.md`. Do not request separated layers until one main menu reference is approved.

## Active Screen Inventory

| Surface | Player-Facing Purpose | New 3D Direction Requirement |
|---|---|---|
| `SCN-01_SplashLoading` | App entry and loading. | Brand-first military command tone; no old map art. |
| `SCN-02_MainMenu` | Command-base hub. | Campaign, Operations, Skirmish, Store, Commander, Settings; Credits, Supplies, Command. |
| `SCN-03_CommanderProfile` | Profile, XP, history, reward track. | Commander identity and progression, with Armory/upgrades links. |
| `SCN-04_SettingsAccessibility` | Settings and accessibility. | Command-base shell; reachable from Main Menu and Pause. Target and V01 layer pack generated. |
| `SCN-05_CampaignMap` | Campaign mission selection. | Campaign nodes selected from command table/world overlay; launches 3D operation maps. |
| `SCN-06_MissionBriefing` | Objectives, intel, rewards, deployment preview. | Uses `OperationMapId`, `PlanningCameraId`, `MinimapProjectionId`. |
| `SCN-07_LoadoutSquadPrep` | Squad/support/gear setup. | Uses prefab-catalog roster, lock reasons, role descriptions, Armory detail links. |
| `SCN-08_RTSBattleHUD` | 3D operation battle HUD. | Objective tracker, squad tray, command bar, minimap, threat feed over one 3D map. Target and V01 layer pack generated. |
| `SCN-09_BuildDrawer` | Build/produce drawer. | Building, vehicle, and soldier production over the active 3D match HUD. Target and V01 layer pack generated. |
| `SCN-10_UnitCommandWheel` | Unit context command wheel. | Move, attack, hold, board, disembark, support, breach, scan, defend, cancel as available. |
| `SCN-11_OperationsDashboard` | District operations command. | District state, warnings, raids, resources, consequences. |
| `SCN-12_DistrictDetailActions` | District action detail. | Patrol, scan, aid, raid, repair, evacuate, build outpost with civilian/infrastructure consequences. |
| `SCN-13_SkirmishSetup` | Configurable replay setup. | Player-facing Skirmish naming; selectable operation-map presets and AI/rule controls. Target and V01 layer pack generated. |
| `SCN-14_CommandExchange` | Store / command exchange. | Deterministic products, disabled purchase states, no direct Campaign stars or Operation metric purchase. |
| `SCN-15_Inbox` | Inbox and reports. | Empty/designed-unavailable state until services exist. |
| `SCN-16_Events` | Events and challenges. | Empty/designed-unavailable state until event service exists. |
| `SCN-17_Ranking` | Local/account ranking categories. | No network dependency in first target. |
| `SCN-18_CommandFeed` | Command/system feed. | Local system feed and Operation report entries. |
| `SCN-19_Armory` | Unit/building/support roster inspection. | Shows name, description, role, unlock state, level/tier, stats, abilities, upgrades, parts, source. |
| `POP-01_ThreatAlert` | Threat alert and jump. | Jumps inside same 3D operation map. |
| `POP-02_ConfirmRaid` | Raid confirmation. | Intel confidence, civilian risk, district consequence. |
| `POP-03_BuildPlacement` | Build placement confirmation. | 3D socket/footprint validity and blocked/civilian-zone reasons. |
| `POP-04_RewardUnlock` | Major reward/unlock celebration. | Concrete deterministic reward; no hidden odds. |
| `POP-05_MissionResult` | Result, stats, stars, rewards. | Tactical success plus district consequence. Target and V01 layer pack generated. |
| `POP-06_EndOfDayReport` | Operations day resolution. | District deltas and saved Operation state. |
| `POP-07_PauseOptions` | Pause/options. | Resume, Settings, restart, exit to Main Menu with confirmation. |
| `POP-08_IntelReveal` | Intel/evidence reveal. | Evidence, confidence delta, archive route. |
| `POP-09_AbilityUpgradeDetail` | Ability/upgrade detail. | Rules, costs, cooldowns, unlock source, disabled CTA reason. |
| `POP-10_AssistantTakeover` | ARIA control takeover. | Uses typed operation-map targets and cancel/resume state. |
| `POP-11_CommanderIdentity` | Commander identity setup. | Portrait/frame/title selection and ownership state. |
| `PREFAB-01_ObjectiveTracker` | Objective rows. | Objective and star-goal runtime binding. |
| `PREFAB-02_SquadTray` | Squad cards. | Selected/locked/readiness states and 3D unit roles. |
| `PREFAB-03_BuildDrawer` | Reusable build drawer. | Config-backed building/production rows. |
| `PREFAB-04_AssistantButton` | ARIA entry. | Idle, recommendation, critical, takeover, muted states. |
| `PREFAB-05_AssistantPanel` | ARIA recommendations. | Show Me / Do It / Stop over operation-map anchors. |
| `PREFAB-06_TutorialCard` | Tutorial prompt. | Localized text, ARIA/commander voice, target highlight. |
| `PREFAB-07_TutorialHighlight` | Highlight/path preview. | UI/world highlight target resolves through typed gameplay references. |

## Acceptance Gate

A new pack is not accepted until:

- It is 16:9 and 20:9 safe.
- Text is live-layered or explicitly marked decorative.
- Icons, panels, backgrounds, and important art are separate layers.
- It uses the 3D direction and current player-facing terminology.
- It documents data sources, route links, disabled states, and locked-state reasons.
