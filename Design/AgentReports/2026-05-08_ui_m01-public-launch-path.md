Lane:
UI

Task:
Fix and prove the public M01 launch path so Main Menu -> Saga Map -> First Contact -> Mission Briefing/Loadout -> Launch and Quick Custom -> Launch reach the current M01 production slice instead of the old legacy 3D prototype.

Files changed:
- `Assets/Game/Scenes/Game.unity`
- `Assets/Game/Data/TacticalMaps/Chapter01/iso.ch01.district_edge_01.asset`
- `Assets/Game/Art/Generated/IsometricMaps/TacticalGroundQualityTest_A/tactical_ground_quality_test_close_pot_a_cropped_runtime.png`
- `Assets/Game/Art/Generated/IsometricMaps/TacticalGroundQualityTest_A/tactical_ground_quality_test_close_pot_a_cropped_runtime.png.meta`
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/Environment/RuntimeDecorationSpawnerSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeGridBlockerSystem.cs`
- `Assets/Game/Scripts/Systems/M01LegacyEcsRenderingSuppressionSystem.cs`
- `Assets/Game/Scripts/Systems/M01LegacyEcsRenderingSuppressionSystem.cs.meta`
- `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
- `Assets/Game/Scripts/Systems/UnitModelSpawnSystem.cs`
- `Assets/Game/Scripts/TacticalMaps/TacticalMapRuntimeLoader.cs`
- `Assets/Game/Scripts/UI/RTSSelectionSystem.cs`
- `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01-20x9.png`
- `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`

Contracts touched:
- `GameBootstrap.BeginGameplay()` applies M01 production scene visibility after the tactical mission is loaded.
- `GameBootstrap` keeps the M01 production orthographic camera active after selection/camera systems update, frames the production anchors, clamps to the tactical map, and exposes `ApplyM01ProductionCameraPoseForCurrentAspect()` for validation captures.
- `Game.unity` wires `legacyVisualRootsDisabledForM01` so legacy `Decorations`, `SM_Skydome_01`, and root `Ground` are hidden during M01.
- `RuntimeGridBlockerSystem` and `RuntimeDecorationSpawnerSystem` skip legacy auto-spawn paths while `Chapter01M01PlayableRuntime.IsActiveMission()` is true.
- `UnitModelSpawnSystem` does not spawn detail/mid/low legacy model instances for entities tagged with `MissionRuntimeSpritePresenterSuppressesLegacyModelTag`.
- `M01LegacyEcsRenderingSuppressionSystem` suppresses unsuppressed ECS mesh renderers while M01 is active.
- `TacticalMapRuntimeLoader` now orients the GameplayXZ ground sprite toward the production top-down tactical camera.
- `iso.ch01.district_edge_01` now uses the cropped runtime tactical ground sprite so the public player capture is authored terrain instead of the source art matte border.
- `Chapter01M01PlayModeValidationTests` covers the public campaign route through Saga Map, Mission Briefing, Loadout, and Deploy, plus Quick Custom launch, and captures full player-facing HUD plus world evidence at 16:9 and 20:9.

User-visible behavior:
Launching First Contact through the public campaign flow or Quick Custom now reaches `WarlineCaptureRoute.Match` with the WarlineCapture HUD active over the current M01 tactical/isometric production slice. The visible first gameplay state shows authored road/terrain art, readable player and enemy sprite-presenter units, objective/threat/command/minimap context, inactive legacy `UI_Canvas`, and no old skydome/legacy flat ground view.

Validation run:
- Unity PlayMode graphics-enabled in `/Users/farhad/Projects/WarlineCapture-CodexUnity2`: `Chapter01M01PlayModeValidationTests`
- Capture dimension check with `sips` for campaign and Quick Custom 16:9/20:9 evidence.
- Runtime banned-lookup scan on touched runtime files.
- `git diff --check`

Validation result:
- `Chapter01M01PlayModeValidationTests`: 5/5 passed. Results: `/private/tmp/warlinecapture-m01-public-launch-results.xml`.
- Public campaign smoke entry path: `Game scene -> WarlineCaptureRouter -> SagaMap -> First Contact node -> MissionBriefing -> StartMissionButton -> LoadoutSquadPrep -> DeployButton`.
- Quick Custom smoke entry path: `Game scene -> WarlineCaptureRouter -> QuickCustomSetup -> Launch`.
- Expected mission id and visual direction: `saga.ch01.m01.first_contact`, `iso.ch01.district_edge_01`, current M01 2D/isometric sprite-presenter/sprite-renderer production slice.
- Actual first visible gameplay state asserted by tests: `WarlineCaptureRoute.Match`, WarlineCapture app canvas active, Match overlay visible, legacy `UI_Canvas` inactive, legacy root `Decorations` inactive, legacy root `SM_Skydome_01` inactive, legacy root `Ground` inactive, M01 command/enemy entities present with `MissionRuntimeSpritePresenter`, enabled `M01Sprite_` renderers visible, no unsuppressed legacy ECS mesh renderers, tactical ground renderer active, and camera/map overlap above the acceptance threshold.
- Capture evidence:
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png` 1280x720.
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png` 1600x720.
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png` 1280x720.
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01-20x9.png` 1600x720.
- Capture review result: campaign and Quick Custom captures show authored tactical terrain/roads under the live HUD with readable unit groups; they are not route-only, flat brown/blank, or old 3D prototype evidence.
- `sips` confirmed the two 16:9 captures are 1280x720 and the two 20:9 captures are 1600x720.
- Runtime banned-lookup scan found only pre-existing `Transform.Find` usage in `TacticalMapRuntimeLoader` for generated child reuse; this pass did not add new banned runtime scene searches.
- `git diff --check`: passed.

Known gaps:
- The capture is a deterministic camera render with the WarlineCapture canvas temporarily rendered through the gameplay camera for batchmode evidence, then restored. It is not a `ScreenCapture.CaptureScreenshot` artifact.
- `TacticalMapRuntimeLoader` still has pre-existing `Transform.Find` generated-child reuse. This was not introduced by this pass and should be handled separately if PM wants a zero-hit runtime lookup scan.
- The public launch path is now proven in focused PlayMode on desktop batchmode. Android/manual HCI smoke is still the next QA/HCI gate.

Cross-lane impacts:
- Gameplay should review and own the M01-specific world guardrails: skipping legacy blockers/decorations, suppressing legacy ECS mesh renderers, hiding legacy scene roots, and using the cropped runtime tactical ground sprite.
- QA/HCI can rerun manual public launch smoke on campaign and Quick Custom paths using the listed captures as expected behavior.
- Support/FTUE assistant binding remains preserved because the WarlineCapture router/HUD stays active on `WarlineCaptureRoute.Match`.

Next recommended task:
PM/QA should review the four public launch captures. If accepted, QA/HCI should run Android/device smoke for Main Menu -> Saga Map -> First Contact -> Mission Briefing/Loadout -> Launch and Quick Custom -> Launch, confirming the player sees the same M01 production slice and no legacy 3D prototype visuals.
