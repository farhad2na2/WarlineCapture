# WarlineCapture Agent Report

## Lane
Gameplay

## Task
M01-01 v4 proof after PM accepted v3 flow restoration but rejected visual implementation.

Goal from `Design/AgentTasks/gameplay_current.md`: keep the restored loading/main-menu/Quick Custom/Match flow, fix ECS soldier visibility, align M01-01 player/enemy composition closer to the approved target mockup, and provide fresh v4 proof.

## Files changed
- `Assets/Game/Scripts/Campaign/Chapter01M01PlayableRuntime.cs`
  - Re-applies tactical-map placement for existing `MissionRuntimeEntityId` entities every M01 initialization.
  - Keeps opening patrol route data passive for the M01-01 no-selection state by clearing `UnitTarget`, `UnitPathRequest`, `UnitPathFollow`, and `UnitPathRange` on the opening enemy patrol.
  - Suppresses the oversized command-point decor presentation for this no-selection proof because it was not part of the M01-01 visual lock and occluded the objective panel.
- `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
  - Uses ECS visual entities' `LocalToWorld` matrices for proof drawing.
  - Adds camera command-buffer drawing for ECS runtime quads during capture.
  - Adds ECS quad diagnostics: visible count, world position, viewport position, atlas frame key, texture, color, render queue.
  - Fixes formation offsets so local offsets map onto the ground-plane screen axes instead of vertical height.
  - Tightens four-soldier formation footprint toward the visual-lock rects.
- `Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs`
  - Adds `CaptureGameSceneViaExistingFlowV4`.
  - Captures to `M01-01_GameSceneRuntimeCapture_v4_1920x1080.png`.
  - Logs v4 ECS quad diagnostics at two runtime samples plus capture time.
- `Assets/Game/Data/TacticalMaps/Chapter01/iso.ch01.district_edge_01.asset`
  - Moves player/enemy tactical anchors toward the target lower-left and upper-right composition while preserving the contracted map id.

## Contracts touched
- `Design/Architecture/gameplay_solid_ecs_contract.md`
  - Runtime soldiers remain ECS entities/components/systems.
  - No direct scene startup replacement was reintroduced.
  - Capture command buffer reads ECS runtime quad data; it does not paste mockup pixels or instantiate scene-only soldiers.
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
  - Preserved `IsoMapId: iso.ch01.district_edge_01`.
- `Design/WarlineCapture_M01_Metric_Scale_Readability_Contract.md`
  - Improved visible soldier count and formation readability.
  - Still needs final scale/background/UI polish before full visual acceptance.

## User-visible behavior
- Existing flow remains intact: Splash/Main Menu/Quick Custom/Match route proof is still logged.
- V4 runtime capture now shows all eight battlefield soldiers:
  - Four player rifle squad soldiers in the lower-left tactical area.
  - Four enemy patrol soldiers in the upper-right tactical area.
- M01-01 no-selection state remains preserved:
  - No selected rings.
  - No selected squad status panel.
  - No move/attack/objective/invalid world markers.
  - Command panel remains neutral with Select/Move/Attack/Stop/Hold labels.
  - Assistant/ARIA remains closed.
  - Build is not visible in the M01 opening state.
- Oversized command-point decor no longer covers the objective panel.

Proof artifacts:
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v4_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v4_vs_Target_Comparison.png`

## Validation run
Workspace:
- `/Users/farhad/Projects/WarlineCapture-CodexUnity1`

V4 existing-flow capture:
```text
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV4 -logFile /private/tmp/warlinecapture-m01-game-flow-v4-final.log
```

Key route/runtime markers:
```text
WARLINECAPTURE_M01_GAME_FLOW_LAUNCHED splash=1 main=1 quickCustom=1 match=1 activeMission=saga.ch01.m01.first_contact
WARLINECAPTURE_M01_GAME_FLOW_RUNTIME_READY player=Entity(108:1) enemy=Entity(109:1)
WARLINECAPTURE_M01_ECS_RUNTIME_QUAD_CAPTURE_DRAW_COUNT count=8
WARLINECAPTURE_M01_GAME_FLOW_CAPTURED path=Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v4_1920x1080.png player=Entity(108:1) enemy=Entity(109:1)
```

ECS visibility evidence from `/private/tmp/warlinecapture-m01-game-flow-v4-final.log`:
```text
WARLINECAPTURE_M01_ECS_QUAD_DIAG_SUMMARY runtimes=2 visibleSoldiers=8
```

Sample ECS runtime quad evidence:
```text
player viewport samples: 0.221,0.477 through 0.335,0.368
enemy viewport samples: 0.739,0.777 through 0.853,0.668
textures: player_rifle_squad_animation_atlas_v2, enemy_patrol_animation_atlas_v2
render queue: 3100
frame key: idle.NE.0
```

Comparison generation:
```text
magick Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v4_1920x1080.png +append Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v4_vs_Target_Comparison.png
```

Architecture contract tests:
```text
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -logFile /private/tmp/warlinecapture-gameplay-architecture-contract-tests-v4-final.log -testResults /private/tmp/warlinecapture-gameplay-architecture-contract-tests-v4-final.xml
```

Architecture result:
```text
result="Passed" total="6" passed="6" failed="0" inconclusive="0" skipped="0"
```

## Validation result
Needs PM review, not full visual approval yet.

Passed:
- Existing app/game flow remains restored and used.
- V4 capture is nonblank.
- Player rifle squad and enemy patrol are visible in the actual Game scene capture.
- Visible soldiers are sourced from ECS runtime presentation:
  - source entities: `MissionRuntimeEntityId` player/enemy entities
  - components: `MissionRuntimeSpritePresenter`, `MissionRuntimeAtlasQuadRuntime`, `LocalToWorld`
  - system/path: `MissionRuntimeAtlasQuadPresentationSystem` plus capture command buffer reading ECS runtime matrices/materials
  - textures: v2 player/enemy atlas textures
- M01-01 no-selection state remains preserved.
- `GameplayArchitectureContractTests` passed 6/6.

Still mismatched against target:
- Runtime battlefield/background still does not visually match the approved target background. It uses the contracted `iso.ch01.district_edge_01` map path but the current runtime plate lacks the target's war-torn building/fire composition.
- Runtime zoom/framing is closer but not target-perfect.
- Player squad is visible and lower-left, but sits farther left/lower than the target.
- Enemy patrol is visible and upper-right, but sits farther right and higher than the target.
- Enemy affiliation/readability overlays and red health bars from the target are not implemented in this v4 slice.
- Squad card strip still differs from the target: only Rifle Squad card content is present/readable; APC/Tank/Helicopter cards are not matched.
- Objective panel/Star Goals exist but layout does not fully match the visual lock.

Animation diagnostic:
- The v4 log proves visible ECS soldiers use atlas frame key `idle.NE.0` at multiple runtime samples.
- Frame key did not advance between the logged samples, so visible idle animation changes are not yet proven.
- Precise remaining animation blocker: `MissionRuntimeAtlasQuadPresentationSystem` resolves and draws the idle atlas frame, but the current opening-state frame key remains `idle.NE.0` through the v4 proof window. Gameplay must verify the atlas frame timing data/resolver for idle-state frame cycling next.

## Known gaps
- Full background visual-lock match remains open.
- Full target composition polish remains open: camera/framing, exact soldier rects, target-facing angles, enemy readability overlays, and bottom HUD/card layout.
- Idle animation frame cycling is not yet proven visible.
- Unity logs still include a recurring Editor `ArgumentOutOfRangeException` from Unity internals before capture; the capture and tests complete successfully despite it.

## Cross-lane impacts
- QA/HCI remains held for final acceptance because v4 still has visual mismatches.
- Designer/Art may need to confirm whether the current runtime plate can be accepted as an interim source or whether a clean no-HUD target-matching battlefield plate must be produced under the existing `iso.ch01.district_edge_01` path.
- Gameplay can continue without changing shell/navigation flow.

## Next recommended task
Continue Gameplay on target-match polish:
- Fix idle atlas frame cycling for visible ECS soldiers.
- Add enemy readability/health overlays through ECS/UI runtime presentation, not command markers.
- Align camera/background source to the approved visual lock.
- Tune player/enemy anchors and facing until the v4/v5 comparison matches the target rects more closely.
