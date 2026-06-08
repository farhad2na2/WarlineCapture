# WarlineCapture UI/UX Phase 7 Immediate Implementation Plan

Date: 2026-05-05

## Goal

Continue from the tactical HUD slice into blocking and non-blocking popup UI while preserving the accepted layered Canvas workflow, reusable chrome, Oxanium typography, Android landscape validation, and the updated 2D isometric direction.

## Blocking Layer-Pack Gate

Before any popup prefab or builder change, verify the matching `Design/VisualLockLayered/<PopupId>/` folder exists and contains:

- `reference/<PopupId>_Landscape_Target.png`
- `layers/`
- `layer_manifest.json`
- `generated_one_go/layers_contact_sheet.png`
- `README.md`

If any of these are missing, stop popup implementation and create the layer pack first. Do not use `Assets/Game/Prefabs/UI/Popups` prefabs as visual baselines unless their matching `VisualLockLayered` pack exists and has been validated.

## Blocking Target-Match Gate

The target mockup is the visual and content contract. Do not silently replace target content, text, rewards, objective names, difficulty labels, icons, image subjects, selected states, or hierarchy with canonical/runtime placeholders while claiming target match.

Before prefab or builder implementation, create a target-to-canvas mapping for the current popup. The mapping must list each visible target element and record:

- target bounds or crop reference
- Unity object path
- layer type
- source layer PNG or TMP text value
- 9-slice border or alpha rule
- z-order and child-layer relationship
- expected 16:9 and 20:9 anchoring behavior
- QA status

If gameplay/content design intentionally differs from the current target, update the target/layer pack first or document the deviation as `not target-matched`. Do not call the prefab done, complete, visual-locked, or matching while such differences remain.

Current popup prefabs that need this gate applied retroactively before being called visual-locked:

- `PauseMenuPopup` -> `POP-07_PauseOptions`
- `ThreatAlertPopup` -> `POP-01_ThreatAlert`
- `BuildPlacementPanel` -> `POP-03_BuildPlacement`

## Current Status

- Operation-map UI additions for `SCN-09`, `SCN-10`, `POP-01`, `POP-03`, and `POP-05` are tracked in `Design/UIUX_Gameplay_Element_Alignment.md`. The first concrete target is `Design/M01_FirstContact_Production_Contract.md`. The relevant missing pieces are command target hints, explicit move/attack mode feedback, disabled command reasons, build availability reasons, metadata-backed footprint overlays, threat jump-to-camera behavior, and result binding to the active Mission / ScenarioSetup / OperationMap ids.
- Before revisiting tactical popups, use `Design/3D_SingleMap_Gameplay_Direction.md` and these refreshed state targets:
  - `Design/VisualLock/POP-01_ThreatAlert_RoutePreviewState/POP-01_ThreatAlert_RoutePreviewState_Landscape_Target.png`
  - `Design/VisualLock/POP-03_BuildPlacement_MetadataValidityState/POP-03_BuildPlacement_MetadataValidityState_Landscape_Target.png`
  - `Design/VisualLock/POP-05_MissionResult_M01ContractState/POP-05_MissionResult_M01ContractState_Landscape_Target.png`
  - `Design/VisualLock/SCN-09_BuildDrawer_M01DisabledState/SCN-09_BuildDrawer_M01DisabledState_Landscape_Target.png`
  - `Design/VisualLock/SCN-10_UnitCommandWheel_TargetingState/SCN-10_UnitCommandWheel_TargetingState_Landscape_Target.png`
  These are flattened target references until matching `VisualLockLayered` packs are created.
- `POP-07_PauseOptions`, `POP-01_ThreatAlert`, and `POP-03_BuildPlacement` have VisualLockLayered packs and generated popup prefabs.
- `POP-03_BuildPlacement` now has an explicit prefab sprite-source test so the resource bar, panel frame, card frame, and build action buttons cannot silently fall back to older non-pack source sprites.
- `POP-04_RewardUnlock` has a VisualLockLayered pack, generated popup prefab, focused prefab tests, and 16:9 / 20:9 capture validation.
- `POP-05_MissionResult` has a VisualLockLayered pack, generated popup prefab, focused prefab tests, and its target content repair pass.
- `POP-02_ConfirmRaid` has a VisualLockLayered pack, generated popup prefab, focused prefab tests, and 16:9 / 20:9 capture validation. It is the current popup baseline for the newer layer-pack-first flow.
- `POP-06_EndOfDayReport` has a VisualLockLayered pack, generated popup prefab, focused prefab tests, and 16:9 / 20:9 capture validation.
- `POP-08_IntelReveal` has a VisualLockLayered pack, generated popup prefab, focused prefab tests, and 16:9 / 20:9 capture validation.
- `POP-09_AbilityUpgradeDetail` has a VisualLockLayered pack, generated popup prefab, focused prefab tests, and 16:9 / 20:9 capture validation.
- Updated gameplay alignment adds side-nav and support route surfaces: `SCN-15 Inbox`, `SCN-16 Events`, `SCN-17 Ranking`, `SCN-18 Command Feed`, and `SCN-19 Armory`.
- `SCN-19 Armory` has a final layered target, generated Armory screen prefab, focused prefab tests, shell-route coverage, and 16:9 / 20:9 capture validation. It is implemented as the disabled-upgrade inspection shell until inventory/upgrade services are ready.
- `SCN-14 Command Exchange` has a layered target pack, generated Command Exchange screen prefab, generated one-go UI art staged under `Assets/Game/Art/UI/Generated/CommandExchange/LayeredOneGo`, Store-button routing from Main Menu, shell-route coverage, focused prefab tests, and 16:9 / 20:9 capture validation. Purchases remain disabled until wallet/catalog/receipt/reward services are ready.
- `SCN-15 Inbox`, `SCN-16 Events`, `SCN-17 Ranking`, and `SCN-18 Command Feed` have layered visual-lock packs, generated route shell prefabs, generated one-go UI art staged under `Assets/Game/Art/UI/Generated/<Route>/LayeredOneGo`, Main Menu route wiring, shell-route coverage, focused prefab tests, and 16:9 / 20:9 capture validation. These remain designed-unavailable shells until backing message, event, leaderboard, and social/feed services are ready.
- `SCN-05 Saga Map` and `SCN-06 Mission Briefing` now have Chapter 1 route-ready layered packs, generated screen prefabs, generated Unity art, Saga card route wiring, Start Mission route wiring, shell-route coverage, focused prefab tests, and 16:9 / 20:9 capture validation. Saga nodes carry mission metadata, and `SagaMapScreenController` binds the selected info panel from Chapter 1 mission config plus local completion/star progress. Runtime Saga progress now drives node locked/available/selected sprites, lock/star icon visibility, and next-mission unlocks after the required previous mission is completed.
- `SCN-07 Loadout / Squad Prep` now has a route-ready layered pack, generated screen prefab, generated Unity art, Mission Briefing route wiring, Deploy-to-Match route wiring, shell-route coverage, focused prefab tests, and 16:9 / 20:9 capture validation.
- Chapter 1 mission/runtime foundations now exist in code: `ChapterOneMissionCatalog`, `MissionConfig`, `ObjectiveConfig`, `ObjectiveManager`, `MissionResultBuilder`, `MissionResultData`, `ActiveMissionSession`, `SagaProgressStore`, `WarlineCaptureSaveData`, and `SaveService`. Focused EditMode coverage validates mission lookup, objective evaluation, result star scoring, active mission session state, local Saga progress, split JSON save files, and `MissionResultPopup` runtime binding. `StartMissionButton` and `DeployButton` now seed the active mission session; Deploy also launches the existing gameplay path.
- `Screen_MatchOverlay` now has `MatchObjectivePanelController`, which keeps the visual-lock fallback labels for prefab preview but binds `ObjectivePanel` rows to the active `ActiveMissionSession` at runtime. It displays live primary objective progress from `ObjectiveManager` and the first star-goal progress from `GameRuntimeStats.Snapshot`, with focused MatchOverlay tests covering active-session binding and no-session fallback.
- Initial Phase 8 reward foundations now exist in code: `RewardType`, `RewardItemConfig`, `RewardConfig`, `RewardGrantResult`, and `RewardService`. Chapter 1 missions now carry reward configs for Commander XP, Credits, and first-clear unlocks with duplicate fallback support. `Screen_MissionBriefing` previews those configured rewards, and `WarlineCaptureMatchResultFlow` applies the same rewards through `SaveService`, passes granted rows into `MissionResultPopupController`, and still updates `SagaProgressStore` for the current runtime slice when an active mission completes.
- `SCN-09 Build Drawer / Production` is implemented as hidden `BuildDrawerCanvas` inside `Screen_MatchOverlay`, opened from the HUD Build button, with layered chrome, animated tabs/buttons, input-blocking scrim, focused prefab tests, and capture validation.
- `SCN-10 Unit Command / Command Wheel` is implemented as hidden `CommandWheelCanvas` inside `Screen_MatchOverlay`, opened from the HUD Special button, with a new `Design/VisualLockLayered/SCN-10_UnitCommandWheel` pack, separated hint/card/radial/icon/targeting layers, animated command segments, focused prefab tests, and 16:9 / 20:9 captures.
- `SCN-03 Commander Profile`, `SCN-11 Operation Dashboard`, and `SCN-12 District Detail / Actions` now have designed-unavailable route shell prefabs, generated Unity art, Main Menu route wiring where applicable, shell-route coverage, focused tests, and 16:9 / 20:9 capture validation. `OperationService` now provides default districts, Resources-backed configurable Patrol/Scan/Aid/Raid meter updates, district-specific modifiers, raid mission-routing intent, operation supply deltas, secondary trust/security/infrastructure/enemy-influence/heat/civilian-risk consequences, typed pending event rows, scan evidence archive rows, authored heat/civilian-risk/enemy-influence alert rules, and end-of-day pressure. `WarlineCaptureOperationRuntime` persists this state through `SaveService`. `OperationDashboardScreenController`, `DistrictDetailScreenController`, and `WarlineCaptureOperationModalFlow` bind the first live Operation slice: dashboard district cards select a district, End Day applies passive pressure and opens `POP-06`, Scan mutates intel and opens `POP-08`, Raid opens `POP-02`, and confirmation seeds Breach Assault with Operation Dashboard as the return route. `OperationIntelArchive` now centralizes saved evidence latest/count/read helpers, `POP-08` reads latest saved evidence for the selected district and marks it read from View Intel, and `SCN-15 Inbox`, `SCN-16 Events`, and `SCN-18 Command Feed` surface the saved Operation event ledger plus intel evidence archive at runtime through lightweight controllers with category/severity/source-metric and evidence confidence/read labels.
- Phase 7 popup and support-route shell work is complete at the current scope. The next UI work should move to the next approved visual-lock screen or return to gameplay-backed service integration for these disabled shells.

## Completion Gate

Phase 7 popup and support-route scope is complete at the current implementation level.

- All Phase 7 popup prefabs exist under `Assets/Game/Prefabs/UI/Popups`.
- Each popup has a matching `Design/VisualLockLayered` pack and generated Unity sprite destinations.
- Focused prefab/component tests cover prefab existence, layer-pack source contracts, button hit sizes, typography, runtime result binding, and modal-shell structure.
- Operation-specific popups are now used by the first live Operation slice through `WarlineCaptureOperationModalFlow`.
- Tactical command/build/threat/result UI additions from `UIUX_Gameplay_Element_Alignment.md` have prefab/controller/test owners before M01 production gameplay is marked ready.
- `POP-05 Mission Result` is verified against `saga.ch01.m01.first_contact` ids, stars, stats, and reward rows from `M01_FirstContact_Production_Contract.md`.

Next active phase: `Phase 8 - Objectives and Results`.

## Validation

Run after each popup slice:

- Layer-pack gate validation before prefab work.
- Target-to-canvas mapping validation before prefab work.
- `WarlineCaptureUiComponentPrefabTests.VisualLockLayeredPopupPacks_ArePresentBeforePopupPrefabWork` must include the popup before the prefab is marked implementation-ready.
- Add or update a prefab sprite-source mapping test for important frame, fill, button, icon, and content sprites before marking the popup implementation-ready.
- Focused popup/component EditMode tests.
- Any available layer-pack/prefab mapping validation test. If no test exists yet, document that gap before proceeding.
- Any affected screen tests, especially `WarlineCaptureUiMatchOverlayTests` when shared HUD/popup assets are regenerated.
- 16:9 and 20:9 captures for each popup.
- Fresh target-vs-capture comparison image with target, rendered capture, and amplified difference overlay.
- Focused target-vs-capture crops for all major panels and every named problem path.
- Manual capture inspection for text clipping, button overflow, opaque-corner artifacts, and accidental baked target crops.
- Self-QA checklist must pass before reporting completion: target text/content matches, rewards/objectives match, header scale matches, frame and 9-slice corners match, no opaque corners, no baked child UI, no clipped text, both aspect ratios hold layout, icons/buttons are centered, and all intentional differences are explicitly listed.

## Rules

- Do not use one full popup mockup image as the prefab.
- Do not use a helper with hard-coded reference dimensions unless the popup row uses those exact dimensions.
- Buttons, icons, alert banners, panel fills, and frames must remain separate replaceable layers.
- Unimplemented popup actions may be present visually, but must be clearly testable and ready to wire through `WarlineCaptureModalController`.
- Tests are mandatory gates, but visual acceptance requires rendered target comparison. Passing tests is not enough.
