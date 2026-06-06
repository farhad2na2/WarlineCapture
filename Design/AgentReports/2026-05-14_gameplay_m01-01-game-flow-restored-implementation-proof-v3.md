# WarlineCapture Agent Report

## Lane
Gameplay

## Task
M01-01 Game scene implementation proof v3 after PM rejection of v2 scene-startup drift.

Scope from `Design/AgentTasks/gameplay_current.md`:
- Restore existing loading screen, main menu, quick custom/custom-game-mode, and normal mission launch flow.
- Remove v2 M01-specific scene startup replacement.
- Reach M01-01 through the existing designed runtime path.
- Produce fresh v3 runtime capture and target comparison.
- Validate `GameplayArchitectureContractTests`.

## Files changed
- `Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs`
  - Added `CaptureGameSceneViaExistingFlow`.
  - Opens `Assets/Game/Scenes/Game.unity`, enters play mode, verifies Splash/MainMenu/QuickCustomSetup/Match routes, navigates Splash -> MainMenu -> QuickCustomSetup, launches through `QuickCustomScreenController.LaunchMission`, waits for M01 ECS runtime readiness, and captures `M01-01_GameSceneRuntimeCapture_v3_1920x1080.png`.
- `Assets/Game/Scripts/Components/MissionRuntimeComponents.cs`
  - Added runtime quad mesh reference storage for ECS visual presentation/capture attempts.
- `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
  - Added reusable camera draw helper for runtime atlas quads.
  - Updated the proof draw helper to use ECS visual entities' `LocalToWorld` matrices.
  - Moved atlas quad materials later in the transparent queue so unit quads are not ordered behind the tactical ground plate.
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
  - Updated scene-startup installer guard so the contract passes when the scene-startup folder has been removed as part of restoring the normal app flow.
- Existing M01/map/HUD files from earlier implementation remain in-flight:
  - `Assets/Game/Data/TacticalMaps/Chapter01/chapter01_tactical_asset_manifest.asset`
  - `Assets/Game/Data/TacticalMaps/Chapter01/iso.ch01.district_edge_01.asset`
  - `Assets/Game/Scripts/Campaign/Chapter01M01PlayableRuntime.cs`
  - `Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs`
  - `Assets/Game/Scripts/TacticalMaps/Chapter01TacticalAssetManifest.cs`
  - `Assets/Game/Scripts/UI/Screens/M01InfantryOnlyHudScopeController.cs`
  - `Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs`

Restored/reverted v2 drift:
- Removed direct startup scene objects from `Assets/Game/Scenes/Game.unity`.
- Restored the scene startup route value so the Game scene no longer boots directly into a special M01 match route.
- Removed v2 M01-specific scene startup replacement files:
  - `Assets/Game/Scripts/Scenes/GameSceneStartupConfig.cs`
  - `Assets/Game/Scripts/Scenes/GameSceneStartupInstaller.cs`
  - `Assets/Game/Data/SceneStartup/M01_01_GameSceneStartupConfig.asset`
  - `Assets/Game/Scripts/Editor/WarlineCaptureM01GameSceneImplementationBuilder.cs`
- Confirmation command found no remaining direct-startup references under Game scene/scripts/data:
  - `rg -n "GameSceneStartup|M01_GameSceneStartup|M01GameSceneStartupController|GameSceneStartupInstaller|M01_01_GameSceneStartupConfig|WarlineCaptureM01GameSceneImplementationBuilder" Assets/Game/Scenes/Game.unity Assets/Game/Scripts Assets/Game/Data`
  - Result: no matches.

## Contracts touched
- `Design/Architecture/gameplay_solid_ecs_contract.md`
  - Preserved bootstrap as composition/runtime entry instead of adding mission-specific scene-startup policy.
  - Added validation coverage for removed scene-startup installer folder.
  - Runtime soldier state remains ECS data/system-driven; no flattened mockup PNG is used as runtime soldier source.
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
  - Preserved `IsoMapId: iso.ch01.district_edge_01`.
  - Runtime map path continues to use the contracted M01 tactical map id.
- `Design/WarlineCapture_M01_Metric_Scale_Readability_Contract.md`
  - Still partially unmet in v3 proof because world soldier presentation is not visible in the captured frame.

## User-visible behavior
- Restored normal app flow: Splash/loading, Main Menu, Quick Custom, and Match routes remain present and are used for the proof path.
- M01 launch now goes through the existing Quick Custom mission launch route:
  - `QuickCustomScreenController.LaunchMission`
  - `new ActiveMissionSession().BeginMission(saga.ch01.m01.first_contact, WarlineCaptureRoute.QuickCustomSetup)`
  - `WarlineCaptureGameLaunchUtility.StartExistingGameplayAndHideRouter`
  - `GameBootstrap.BeginGameplay`
- No M01-specific scene startup replacement remains.
- Fresh v3 capture exists:
  - `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v3_1920x1080.png`
- Fresh v3 side-by-side comparison exists:
  - `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v3_vs_Target_Comparison.png`

Current visual result:
- Background/HUD/mission UI capture is nonblank.
- Objective/Star Goals/threat row/command labels/minimap/squad card UI are visible.
- Assistant is closed.
- Build is absent/unavailable in the visible M01 state.
- No selected rings, no selected status panel, and no world markers are visible.
- Blocker: battlefield world soldiers are still not visible in the v3 screenshot, even though the M01 ECS runtime reports active player/enemy entities.

## Validation run
Workspace:
- `/Users/farhad/Projects/WarlineCapture-CodexUnity1`

Game-flow capture:
```text
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlow -logFile /private/tmp/warlinecapture-m01-game-flow-v3-capture.log
```

Key log markers:
```text
WARLINECAPTURE_M01_GAME_FLOW_LAUNCHED splash=1 main=1 quickCustom=1 match=1 activeMission=saga.ch01.m01.first_contact
WARLINECAPTURE_M01_GAME_FLOW_RUNTIME_READY player=Entity(108:1) enemy=Entity(109:1)
WARLINECAPTURE_M01_GAME_FLOW_CAPTURED path=Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v3_1920x1080.png player=Entity(108:1) enemy=Entity(109:1)
```

Comparison generation:
```text
magick Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v3_1920x1080.png +append Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v3_vs_Target_Comparison.png
```

Architecture contract tests:
```text
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-gameplay-architecture-contract-tests.log -testResults /private/tmp/warlinecapture-gameplay-architecture-contract-tests.xml
```

Architecture test result XML:
```text
result="Passed" total="6" passed="6" failed="0" inconclusive="0" skipped="0"
```

Follow-up reruns after ECS presentation draw-path fixes:
```text
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlow -logFile /private/tmp/warlinecapture-m01-game-flow-v3-capture-rerun2.log
```

```text
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-gameplay-architecture-contract-tests-rerun.log -testResults /private/tmp/warlinecapture-gameplay-architecture-contract-tests-rerun.xml
```

Rerun architecture test result:
```text
result="Passed" total="6" passed="6" failed="0" inconclusive="0" skipped="0"
```

## Validation result
Needs fixes / blocked for visual approval.

Passed:
- Existing app flow restored and proven by log marker: Splash, Main Menu, Quick Custom, and Match routes exist and were used.
- M01 launch uses the existing designed mission launch path rather than direct scene startup.
- No M01-specific scene startup replacement references remain.
- ECS runtime initializes active M01 player/enemy entities.
- Fresh v3 capture and v3 target comparison were generated.
- `GameplayArchitectureContractTests` passed 6/6.
- Post-report ECS draw-path fixes compiled and the focused architecture contract rerun still passed 6/6.

Failed:
- The v3 capture does not satisfy the target mockup contract because battlefield soldiers are not visible.
- ECS animation/readability proof cannot be accepted until the world soldiers are rendered visibly in the runtime capture.
- Soldier placement/count/readability cannot be fairly assessed from the current v3 image.
- The latest rerun after using ECS `LocalToWorld` matrices and later transparent queue still produced the same visible failure: no battlefield soldiers in the screenshot.

## Known gaps
Exact blocker:
- Battlefield world soldiers are absent from `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v3_1920x1080.png`.

Failed/missing proof:
- Missing visible player rifle squad in the lower-left tactical area.
- Missing visible enemy patrol in the upper-right tactical area.
- Missing visible ECS soldier idle animation proof.
- Missing soldier count/readability match assessment because the units are not visible.

Evidence that this is presentation/capture visibility rather than mission creation:
- `/private/tmp/warlinecapture-m01-game-flow-v3-capture.log` reports:
  - `WARLINECAPTURE_M01_GAME_FLOW_RUNTIME_READY player=Entity(108:1) enemy=Entity(109:1)`
  - `WARLINECAPTURE_M01_GAME_FLOW_CAPTURED ... player=Entity(108:1) enemy=Entity(109:1)`

Owner lane:
- Gameplay.

Can another lane continue?
- QA/HCI should remain held for this slice because the visual proof is not acceptable without visible battlefield soldiers.
- Designer/Art can review the background/HUD comparison if desired, but Gameplay must fix ECS presentation/capture visibility before PM/QA approval.

## Cross-lane impacts
- PM rejection of v2 is addressed for flow drift: direct M01 scene startup was removed and normal route contracts were restored.
- QA/HCI remains blocked from final M01-01 acceptance.
- Art/Atlas is not blocked by this report, but the current Gameplay proof does not yet validate soldier atlas runtime visibility.
- Architecture contract remains enforced by tests; no source docs or other lane task files were modified for this handoff.

## Next recommended task
Gameplay should continue immediately on the ECS soldier presentation visibility path:
- Debug why `MissionRuntimeSpriteRendererSystem` / runtime atlas quads do not appear in the render-texture capture even though ECS entities initialize.
- Prefer fixing the existing ECS presentation path or camera/layer/material visibility rather than adding scene-only GameObject soldier hacks.
- Re-run the same existing-flow capture command.
- Produce a follow-up proof only after the player squad and enemy patrol are visible in `M01-01_GameSceneRuntimeCapture_v3_1920x1080.png` or a new v4 capture.
