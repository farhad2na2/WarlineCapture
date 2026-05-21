# Gameplay Handoff - M01 V29 Recapture Blocked On Art Assets

Date: 2026-05-19
Lane: Gameplay
Status: blocked
Priority: P0

## Lane

Gameplay

## Task

P0 M01 V29 final recapture using the Unity licensing workaround from `Design/AgentTasks/gameplay_current.md`, with user-requested visual corrections for enemy soldier color/scale/facing and tactical background coverage.

## Files changed

- `Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs`
  - Reverted the mismatched TargetMatchV5 enemy idle substitution.
  - Enemy and player now both resolve through the matching V28 direction-locked soldier family for the opening proof.
- `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
  - Enemy runtime tint is disabled; no fake red tint is applied.
  - Enemy readability rings remain lower under the boot area.
  - Enemy rings draw before soldier quads; enemy health bars draw after soldier quads.
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_1920x1080.png`
  - Updated with the reverted r14 capture.
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_r14_1920x1080.png`
  - Fresh r14 capture proof.
- `Design/AgentReports/2026-05-18_gameplay_m01-v29-final-recapture-proof.md`
  - Updated as blocker report.

Validation workspace sync only:

- Mirrored the reverted Gameplay resolver into `/Users/farhad/Projects/WarlineCapture-CodexUnity1` for capture and contract validation.

## Contracts touched

- `Design/Architecture/gameplay_solid_ecs_contract.md`
- Runtime path remains ECS/data-driven through:
  - `MissionRuntimeSpritePresenter`
  - `MissionRuntimeAtlasQuadRuntime`
  - `MissionRuntimeAtlasQuadPresentationSystem`
  - `MissionRuntimeEcsVisualTag`
- M01 contracted ids remain unchanged:
  - `saga.ch01.m01.first_contact`
  - `unit.player.rifle_squad_01`
  - `unit.enemy.patrol_01`
  - `player_spawn.command_squad`
  - `enemy_spawn.patrol_start`
  - `objective.destroy_patrol_group`

## User-visible behavior

The fresh r14 capture launches through splash, main menu, quick custom, and match flow, then renders eight ECS soldiers. Player and enemy soldiers now use the same V28 direction-locked art family, so Gameplay is no longer substituting a larger mismatched red-accent enemy atlas.

The remaining visible issues are asset blockers:

- Enemy soldiers are still blue/steel because the available matching V28 enemy atlas is blue/steel.
- The tactical map exposes solid fill around the background because the currently bound clean tactical plate is only `1920x1080` and does not provide overscan/bleed for the current framing or 21:9 coverage.

## Validation run

Fresh V29 capture:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV29 -logFile /private/tmp/warlinecapture-m01-game-flow-v29-final-r14.log
```

Architecture contract:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -testResults /private/tmp/warlinecapture-gameplay-architecture-contract-v29-r14.xml -logFile /private/tmp/warlinecapture-gameplay-architecture-contract-v29-r14.log
```

Asset dimension check:

```bash
sips -g pixelWidth -g pixelHeight Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v6_source_1920x1080.png Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v6_pot_2048x2048.png
```

## Validation result

Blocked on missing/mismatched Art assets, not blocked on Unity or Gameplay flow.

Fresh capture proof:

- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_r14_1920x1080.png`
- Capture log: `/private/tmp/warlinecapture-m01-game-flow-v29-final-r14.log`

Key log proof:

- `WARLINECAPTURE_M01_GAME_FLOW_LAUNCHED splash=1 main=1 quickCustom=1 match=1 activeMission=saga.ch01.m01.first_contact`
- `WARLINECAPTURE_M01_ECS_QUAD_DIAG_SUMMARY runtimes=2 visibleSoldiers=8`
- `WARLINECAPTURE_M01_GAME_FLOW_CAPTURED path=Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_1920x1080.png player=Entity(108:1) enemy=Entity(109:1)`

Atlas binding proof:

- Player soldiers: `tex=player_rifle_squad_animation_body_shadow_atlas_v28 color=RGBA(1.000, 1.000, 1.000, 1.000)`.
- Enemy soldiers: `tex=enemy_patrol_animation_body_shadow_atlas_v28 color=RGBA(1.000, 1.000, 1.000, 1.000)`.
- This confirms no runtime tint and no mismatched TargetMatchV5 enemy substitution.

Background proof:

- Bound clean plate: `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v6_source_1920x1080.png`
  - Dimensions: `1920x1080`
- Optional POT plate: `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v6_pot_2048x2048.png`
  - Dimensions: `2048x2048`
- Neither is an approved oversized 21:9/overscan runtime tactical plate matching the target framing.

Architecture contract:

- `/private/tmp/warlinecapture-gameplay-architecture-contract-v29-r14.xml`
- Result: passed 6/6.

## Known gaps

- Missing exact enemy art: no matching V28-size/direction/stance enemy atlas with the same bottom-soldier art family but red enemy coloration is currently bound or proven available. Owner: Art/Atlas.
  - The blue/player atlas currently bound by Gameplay is `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV28/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v28.png`.
  - Its dimensions are `4096x1792`, arranged as `16 x 7` cells of `256x256`.
  - Width `4096` is POT; height `1792` is not POT. If Art wants better POT compression/mip behavior, deliver the red enemy atlas as `4096x2048` with the same `16 x 7` used frame area and a transparent unused row.
  - Required visual contract: enemy soldiers must be exactly like the blue/player soldiers in scale, projection, stance, facing family, pivot, baked shadow direction, frame layout, and silhouette readability, but with approved red enemy coloration. Do not deliver a different larger side-view/top-view soldier family.
- Missing exact background art: no approved oversized tactical background plate with enough bleed to cover 16:9 through 21:9 without solid fill around the playfield. Owner: Art/Atlas.
  - Current clean plate is `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v6_source_1920x1080.png` at `1920x1080`; it is not enough for the current framing/aspect coverage.
  - Required visual contract: provide a larger M01 tactical background plate with the same target composition and enough overscan/bleed for 16:9, 20:9, and 21:9 runtime capture without any solid color showing around the map.
- Enemy red health bars and red ground rings are present, but they do not replace the need for red enemy soldier uniforms if PM/user requires enemy bodies to read red.
- HUD/canvas target-lock remains UI-owned and is not part of this Gameplay blocker.

## Cross-lane impacts

- Art/Atlas must provide:
  - a V28-compatible enemy patrol atlas matching `player_rifle_squad_animation_body_shadow_atlas_v28` exactly: `4096x1792` current contract or `4096x2048` POT-padded with the same `16 x 7` used cells, `256x256` cell size, same scale, projection, stance family, direction keys, pivot, baked-shadow style, and frame layout, but with approved red enemy coloration;
  - an oversized/bleed tactical background plate for M01 that covers 16:9, 20:9, and 21:9 framing without solid edge fill.
- Gameplay can bind and recapture after those exact assets exist.
- QA should remain held for final visual signoff until those assets are supplied and rebound.

## Next recommended task

Route to Art/Atlas for the exact missing assets above. Gameplay should resume once the red V28-compatible enemy atlas and oversized M01 tactical background plate are delivered.
