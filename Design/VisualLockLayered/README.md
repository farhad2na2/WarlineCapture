# WarlineCapture Layered Visual Lock Index

Date: 2026-05-05

This folder contains high-end UI visual-lock layer packs for Unity Canvas implementation. A layered pack is different from a flat target PNG: it must include a flattened design reference plus separated implementation sprites, layer manifest, contact sheet, and dry-run import helper.

## Layered Pack Status

| Surface | Status | Notes |
|---|---|---|
| `SCN-08_RTSBattleHUD` | `LayerPackExists` | Existing one-go layer export and manifest. Resource icon naming must still map to canonical resource labels during implementation. |
| `SCN-10_UnitCommandWheel` | `OverlayImplemented` | Command Wheel layer pack created with separate hint, selected entity, radial segment, center hub, icon, and targeting layers. Implemented as hidden `CommandWheelCanvas` inside `Screen_MatchOverlay`, opened from `SpecialButton`, with focused tests and 16:9 / 20:9 captures. |
| `SCN-03_CommanderProfile` | `RouteShellImplemented` | Designed-unavailable route shell pack created with reference target, shared one-go chrome layers, manifest, contact sheet, generated Unity art, prefab, route wiring, tests, and 16:9 / 20:9 captures. |
| `SCN-11_OperationDashboard` | `RouteShellImplemented` | Designed-unavailable route shell pack created with reference target, shared one-go chrome layers, manifest, contact sheet, generated Unity art, prefab, route wiring, tests, and 16:9 / 20:9 captures. |
| `SCN-12_DistrictDetailActions` | `RouteShellImplemented` | Designed-unavailable route shell pack created with reference target, shared one-go chrome layers, manifest, contact sheet, generated Unity art, prefab, route wiring, tests, and 16:9 / 20:9 captures. |
| `SCN-14_StoreCommandExchange` | `HighEndTargetAndLayerAtlasGenerated` | Regenerated high-end flat target plus alpha atlas and 28 extracted candidate layers. Needs Unity import/crop QA before implementation. |
| `SCN-15_Inbox` | `RouteShellImplemented` | Designed-unavailable route shell pack created with reference target, shared one-go chrome layers, manifest, contact sheet, generated Unity art, prefab, route wiring, tests, and 16:9 / 20:9 captures. |
| `SCN-16_Events` | `RouteShellImplemented` | Designed-unavailable route shell pack created with reference target, shared one-go chrome layers, manifest, contact sheet, generated Unity art, prefab, route wiring, tests, and 16:9 / 20:9 captures. |
| `SCN-17_Ranking` | `RouteShellImplemented` | Designed-unavailable route shell pack created with reference target, shared one-go chrome layers, manifest, contact sheet, generated Unity art, prefab, route wiring, tests, and 16:9 / 20:9 captures. |
| `SCN-18_CommandFeed` | `RouteShellImplemented` | Designed-unavailable route shell pack created with reference target, shared one-go chrome layers, manifest, contact sheet, generated Unity art, prefab, route wiring, tests, and 16:9 / 20:9 captures. |
| `SCN-19_Armory` | `FinalHighEndTargetAndLayerPackGenerated` | Final Armory target, alpha atlas, contact sheet, manifest, copy helper, and separated layer PNGs generated for ability/upgrade inspection. |
| `SCN-05_SagaMap` | `RouteScreenImplemented` | Chapter 1 / First Response layer pack generated with five mission nodes, generated Unity art, Saga Map prefab, Main Menu routing, shell coverage, focused tests, and 16:9 / 20:9 captures. |
| `SCN-06_MissionBriefing` | `RouteScreenImplemented` | Chapter 1 Breach Assault layer pack generated with Mission / Scenario / Level / Map content, generated Unity art, Mission Briefing prefab, Start Mission route, shell coverage, focused tests, and 16:9 / 20:9 captures. |
| `SCN-07_LoadoutSquadPrep` | `RouteScreenImplemented` | Loadout / Squad Prep layer pack generated with selected unit cards, support slots, recommended gear, mission summary, generated Unity art, Mission Briefing route, deploy-to-Match route, shell coverage, focused tests, and 16:9 / 20:9 captures. |
| `PREFAB-04_AssistantButton` | `FlatPanelPopupTarget` | High-quality assistant button state board over blurred Match Overlay context. Flat visual target only; no new sliced layer pack required for this pass. |
| `PREFAB-05_AssistantPanel` | `FlatPanelPopupTarget` | High-quality ARIA recommendation panel over blurred Match Overlay context. Flat visual target only; no new sliced layer pack required for this pass. |
| `PREFAB-06_TutorialCard` | `FlatPanelPopupTarget` | High-quality FTUE tutorial card over blurred Match Overlay context with readable copy and expected controls. Flat visual target only; no new sliced layer pack required for this pass. |
| `PREFAB-07_TutorialHighlight` | `FlatPanelPopupTarget` | High-quality tutorial highlight component showcase over blurred Match Overlay context. Flat visual target only; no new sliced layer pack required for this pass. |
| `POP-01_ThreatAlert` | `ReadyForCanvasImplementation` | Layer pack exists with separate modal frame, header plate, warning icon, info rows, convoy art, and jump button layers. |
| `POP-03_BuildPlacement` | `ReadyForCanvasImplementation` | Layer pack exists and `BuildPlacementPanel_ConsumesVisualLockLayerPackSprites` enforces the prefab consumes the mapped resource bar and build chrome sprites. |
| `POP-04_RewardUnlock` | `ReadyForCanvasImplementation` | Layer pack created for RewardUnlockPopup with separated modal chrome, unlock display art, reward cards, reward icons, and continue button. |
| `POP-05_MissionResult` | `ReadyForCanvasImplementation` | Layer pack created with canonical rewards and civilian/district consequence row. |
| `POP-07_PauseOptions` | `ReadyForCanvasImplementation` | Layer pack exists with separate modal frame, selected/normal/destructive button states, and pause action icons. |
| `POP-09_AbilityUpgradeDetail` | `FinalHighEndTargetAndLayerPackGenerated` | Final reusable ability/upgrade detail popup target, alpha atlas, contact sheet, manifest, copy helper, and separated layer PNGs generated. |
| `POP-10_AssistantTakeover` | `FlatPanelPopupTarget` | High-quality ARIA takeover popup over blurred Match Overlay context with current intent and stop/resume controls. Flat visual target only; no new sliced layer pack required for this pass. |
| `POP-11_CommanderIdentity` | `FlatPanelPopupTarget` | High-quality commander identity popup over blurred Commander Profile context for commander icon, callsign, frame choice, and confirmation controls. Flat visual target only; no new sliced layer pack required for this pass. |

## Required Pack Contents

Each layered pack should contain:

- `reference/<SurfaceId>_Landscape_Target.png`
- `generated_one_go/source/generated_layer_atlas_chromakey.png`
- `generated_one_go/source/generated_layer_atlas_alpha.png`
- `generated_one_go/layers_contact_sheet.png`
- `layers/*.png`
- `layer_manifest.json`
- `README.md`
- `copy_layers_to_unity.py`
- `prompts/high_end_target_and_layers.md`

Flat panel/popup target references may instead contain `prompts/flat_panel_popup_target.md` and do not require generated alpha atlas, copy helper, or separated sprite contents until the design is selected for Unity prefab implementation.

## Implementation Rules

- Do not implement a UI screen from a flattened target image.
- TMP text must remain live text in Unity.
- Frames, fills, buttons, icons, content art, and state backgrounds must be separate PNGs.
- 9-slice hints and alpha rules must be recorded in `layer_manifest.json`.
- Use `copy_layers_to_unity.py` in dry-run mode before staging any generated sprites into `Assets/Game/Art/UI/Generated/...`.
