Lane:
Gameplay

Task:
Fix/prove the public M01 launch path so Quick Custom and Saga Map -> First Contact -> Mission Briefing/Loadout -> Deploy reach the current M01 2D/isometric production slice instead of the legacy 3D prototype, with authored tactical terrain visible, correctly oriented, and ECS-backed.

Files changed:
- Assets/Game/Scenes/Game.unity
- Assets/Game/Data/TacticalMaps/Chapter01/iso.ch01.district_edge_01.asset
- Assets/Game/Art/Generated/IsometricMaps/TacticalGroundQualityTest_A/tactical_ground_quality_test_close_pot_a_cropped_runtime.png
- Assets/Game/Art/Generated/IsometricMaps/TacticalGroundQualityTest_A/tactical_ground_quality_test_close_pot_a_cropped_runtime.png.meta
- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs
- Assets/Game/Scripts/Components/MissionRuntimeComponents.cs
- Assets/Game/Scripts/Environment/RuntimeDecorationSpawnerSystem.cs
- Assets/Game/Scripts/Environment/RuntimeGridBlockerSystem.cs
- Assets/Game/Scripts/Systems/M01LegacyEcsRenderingSuppressionSystem.cs
- Assets/Game/Scripts/Systems/M01LegacyEcsRenderingSuppressionSystem.cs.meta
- Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs
- Assets/Game/Scripts/Systems/MissionRuntimeTerrainSurfaceRendererSystem.cs
- Assets/Game/Scripts/Systems/MissionRuntimeTerrainSurfaceRendererSystem.cs.meta
- Assets/Game/Scripts/Systems/UnitModelSpawnSystem.cs
- Assets/Game/Scripts/TacticalMaps/TacticalMapRuntimeLoader.cs
- Assets/Game/Scripts/UI/RTSSelectionSystem.cs
- Assets/Game/Scripts/UI/Shell/WarlineCaptureRouter.cs
- Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs
- Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png
- Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png
- Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png
- Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01-20x9.png

Contracts touched:
- Mission id preserved: saga.ch01.m01.first_contact.
- Tactical map id preserved: iso.ch01.district_edge_01.
- Public routes validated: QuickCustomSetup -> Match and SagaMap -> MissionBriefing -> LoadoutSquadPrep -> Match.
- M01 runtime visual contract suppresses legacy scene roots, legacy ECS mesh renderers, and covered entity model trees while preserving MissionRuntimeSpritePresenter / MissionRuntimeSpriteRendererRuntime for unit.player.rifle_squad_01 and unit.enemy.patrol_01.
- Tactical terrain now has an ECS source contract through MissionRuntimeTerrainSurface plus MissionRuntimeTerrainSurfaceRendererRuntime. The Ground SpriteRenderer is only the ECS-driven presentation object for that terrain entity.
- M01 tactical ground orientation contract requires the SpriteRenderer up vector to align with positive world Z, matching tactical metadata anchors instead of flipping the authored map.
- M01 camera launch contract uses an orthographic tactical pose over the authored tactical map instead of the legacy perspective gameplay camera.
- Touched M01 validation no longer uses broad child-component discovery; the test path now uses router/provider references and loaded scene root references. WarlineCaptureRouter still contains pre-existing screen registration discovery, but this task did not add a new broad runtime lookup there.

User-visible behavior:
Public Quick Custom and campaign launch now enter the M01 match route with authored tactical terrain visible in the correct orientation, readable M01 sprite units, legacy UI_Canvas inactive, legacy scene Ground/Decorations/Skydome hidden, and legacy ECS mesh renderers suppressed. The first visible gameplay state is no longer the old 3D prototype, a flat brown/blank field, an upside-down tactical map, or a standalone screenshot-only terrain object.

Validation run:
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-m01-public-launch-results.xml -logFile /private/tmp/warlinecapture-m01-public-launch-playmode.log
- rg -n "GetComponentInChildren|GetComponentsInChildren|Resources.FindObjectsOfTypeAll|FindAnyObject|FindFirstObject|GameObject.Find|Transform.Find|FindButton|FindMissionNode" Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs Assets/Game/Scripts/TacticalMaps/TacticalMapRuntimeLoader.cs Assets/Game/Scripts/Systems/MissionRuntimeTerrainSurfaceRendererSystem.cs
- Visual inspection of Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png
- Visual inspection of Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png

Validation result:
Passed in the assigned Gameplay workspace WarlineCapture-CodexUnity: Chapter01M01PlayModeValidationTests 5/5, exit code 0. The focused suite validates direct M01 runtime anchoring, Quick Custom public launch, Saga/Briefing/Loadout campaign launch, selection/attack/result guard, M01 build rejection, authored terrain framing, tactical map non-upside-down orientation, terrain ECS backing, sprite-presenter visibility, legacy scene-root suppression, and M01 legacy ECS mesh suppression. The no-broad-lookup grep returned no matches for the touched M01 test, TacticalMapRuntimeLoader, or MissionRuntimeTerrainSurfaceRendererSystem. Public launch captures were regenerated for 16:9 and 20:9 evidence.

Known gaps:
- Unity log still contains early licensing handshake/access-token errors before the successful test run continues, an Entities Graphics URP Forward+ warning, an Animator warning, and a usbmuxd shutdown error. None failed the focused WarlineCapture-CodexUnity validation.
- WarlineCaptureRouter still has pre-existing GetComponentsInChildren screen registration inside InitializeIfNeededWithoutRouting; this task added TryGetRegisteredScreen so touched tests do not need that broad lookup.
- WarlineCaptureGameLaunchUtility still contains pre-existing Resources.FindObjectsOfTypeAll lookup usage. This task did not add new runtime scene searches.
- Current M01 map/unit/building art remains review art, not final approved atlas content.
- M01 production camera is fixed to the validated tactical launch framing; follow-up gameplay work should decide whether M01 needs bounded camera pan/zoom after the first-task path is accepted.

Cross-lane impacts:
- UI lane owns HUD/canvas/capture composition over this now-validated gameplay world; gameplay captures are world-under-HUD evidence, not final UI composition approval.
- QA/HCI can validate Quick Custom and Saga Map public launch against the new correctly oriented ECS-backed gameplay evidence instead of the old route-only, flat-field, or upside-down evidence.
- PM can review Gate 4 with explicit proof that the visible tactical ground is backed by ECS entity state and that touched M01 validation no longer depends on child-component discovery.

Next recommended task:
PM/QA should review the new M01 public-launch handoff and captures. If accepted, refresh Design/AgentTasks/gameplay_current.md to the next Chapter 1 implementation task; gameplay should not move to unrelated work from this stale P1 task.
