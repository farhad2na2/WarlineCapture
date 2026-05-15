# Gameplay M01-01 Game Scene Implementation Proof V2

Date: 2026-05-14

## Lane

Gameplay

## Task

Implement and prove the actual `Game.unity` M01-01 tactical-start/no-selection state using the contracted M01 map id, ECS runtime soldiers, and the M01 HUD baseline.

## Files changed

- `Assets/Game/Scenes/Game.unity`
- `Assets/Game/Data/SceneStartup/M01_01_GameSceneStartupConfig.asset`
- `Assets/Game/Scripts/Scenes/GameSceneStartupConfig.cs`
- `Assets/Game/Scripts/Scenes/GameSceneStartupInstaller.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureM01GameSceneImplementationBuilder.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs`
- `Assets/Game/Data/TacticalMaps/Chapter01/iso.ch01.district_edge_01.asset`
- `Assets/Game/Data/TacticalMaps/Chapter01/chapter01_tactical_asset_manifest.asset`
- `Assets/Game/Scripts/TacticalMaps/Chapter01TacticalAssetManifest.cs`
- `Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs`
- `Assets/Game/Scripts/Campaign/Chapter01M01PlayableRuntime.cs`
- `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
- `Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v2_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v2_vs_Target_Comparison.png`

## Contracts touched

- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/WarlineCapture_M01_Metric_Scale_Readability_Contract.md`
- Runtime map id preserved: `iso.ch01.district_edge_01`
- Runtime background source now resolves through the contracted map path to `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/TacticalMaps/m01_tactical_plate_a_source.png`

## User-visible behavior

- The actual `Assets/Game/Scenes/Game.unity` starts M01-01 through config-backed scene startup.
- The runtime no longer uses the older bright battlefield source for the contracted M01 map path.
- The M01 no-selection HUD shows `Destroy hostile patrol`, Star Goals, one mission-start threat row, assistant closed, no selected unit panel, no selected rings, no world command markers, and no Build command.
- The command strip now shows the allowed no-selection command set: `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`.
- The Game scene creates the M01 command squad and hostile patrol as ECS mission entities when generic initial spawning has not produced them yet.
- The visible soldiers are drawn by the ECS mission atlas presentation system, not by pasted mockup pixels or GameObject sprite mirrors.

## Validation run

- Builder:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureM01GameSceneImplementationBuilder.Build -logFile /private/tmp/warlinecapture-m01-game-scene-builder.log`
- Capture:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameScenePlayMode -logFile /private/tmp/warlinecapture-m01-game-scene-v2-capture.log`
- Architecture tests:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-gameplay-architecture-contract-tests.log -testResults /private/tmp/warlinecapture-gameplay-architecture-contract-tests.xml`
- Comparison:
  `magick Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v2_1920x1080.png +append Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v2_vs_Target_Comparison.png`

## Validation result

- Builder result: passed; marker `WARLINECAPTURE_M01_GAME_SCENE_STARTUP_BUILT`.
- Capture result: screenshot produced; marker `WARLINECAPTURE_M01_GAME_SCENE_CAPTURED`.
- Capture artifact: `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v2_1920x1080.png`.
- Comparison artifact: `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v2_vs_Target_Comparison.png`.
- Runtime diagnostics during capture showed `units=2`, matching one ECS command-squad entity and one ECS hostile-patrol entity; each is presented by the ECS atlas squad formation path.
- `GameplayArchitectureContractTests`: passed, 6/6, XML result `/private/tmp/warlinecapture-gameplay-architecture-contract-tests.xml`.

## Known gaps

- The capture still has visual differences from the approved target: camera/framing and UI proportions are closer but not pixel-matched.
- The current proof is a still capture; it validates the ECS atlas presentation path is active, but does not provide a multi-frame animation delta/video.
- The soldier formation is visible, but the target may still require finer soldier count/readability tuning after PM/designer review.
- The Unity capture command logs a post-capture Entities/SubScene shutdown `NullReferenceException` and an editor `ArgumentOutOfRangeException` after the screenshot marker. The screenshot and architecture tests complete, but the shutdown exception should be isolated separately.

## Cross-lane impacts

- Art/Atlas: contracted map source now points at the M01 tactical plate candidate under the existing `iso.ch01.district_edge_01` path; no new map id was introduced.
- HCI/QA: command labels are now visible in no-selection state and Build is absent for M01.
- Architecture: scene startup mission/route policy moved into config data; a new contract test prevents scene startup installers from hardcoding mission/catalog or route values.

## Next recommended task

Gameplay should continue with a focused visual-fit pass on camera crop, soldier formation/readability, and command/minimap spacing, then add a two-frame or short capture proof for ECS soldier idle animation.
