# Gameplay Handoff - M01 V29 In-Game Target-Match Pass

Date: 2026-05-18
Lane: Gameplay
Status: blocked on fresh Unity recapture after final anchor tweak
Priority: P0

## Lane

Gameplay

## Task

P0 M01 V29 in-game target-match pass from `Design/AgentTasks/gameplay_current.md`.

V29 scope was limited to in-game/runtime composition: V28 soldier atlas binding, camera/framing, tactical map composition, player/enemy position and spacing, facing, idle animation, no-selection state, and live M01 launch flow. HUD/canvas visual target-lock remains deferred to UI by PM clarification.

## Files changed

- `Assets/Game/Data/TacticalMaps/Chapter01/iso.ch01.district_edge_01.asset`
  - Adjusted the M01 player spawn anchor from the current V28/V29 position to `{x: 0.22, y: 0.46}` so the player squad moves up-left toward the target street patch.
  - Kept the V29 enemy patrol/objective/threat anchors on the upper road cluster:
    - `enemy_spawn.patrol_start`: `{x: 0.70, y: 0.76}`
    - `route.enemy_patrol_01.a`: `{x: 0.70, y: 0.76}`
    - `route.enemy_patrol_01.b`: `{x: 0.61, y: 0.74}`
    - `route.enemy_patrol_01.c`: `{x: 0.52, y: 0.72}`
    - `objective.destroy_patrol_group`: `{x: 0.57, y: 0.74}`
    - `threat.patrol_warning_01`: `{x: 0.63, y: 0.74}`
- `Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs`
  - V28 direction-locked soldier manifest and atlas path remain the runtime source path before older fallback atlases.
- `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
  - V28 ECS atlas quad presentation path remains active.
  - Runtime V28 infantry scale remains `M01PlayerSoldierScale = 2.45f`.
- `Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs`
  - V29 capture method remains available as `CaptureGameSceneViaExistingFlowV29`.

## Contracts touched

- `Design/Architecture/gameplay_solid_ecs_contract.md`
- Runtime remains ECS/data-driven for the M01 soldier presentation path:
  - `MissionRuntimeSpritePresenter`
  - `MissionRuntimeAtlasQuadRuntime`
  - `MissionRuntimeAtlasQuadPresentationSystem`
  - `LocalTransform`
  - runtime render quad entities tagged with `MissionRuntimeEcsVisualTag`
- M01 contracted ids remain:
  - `saga.ch01.m01.first_contact`
  - `unit.player.rifle_squad_01`
  - `unit.enemy.patrol_01`
  - `player_spawn.command_squad`
  - `enemy_spawn.patrol_start`
  - `objective.destroy_patrol_group`
  - `route.enemy_patrol_01`

## User-visible behavior

- Player squad and enemy patrol still launch through the normal loading screen, main menu, quick custom mode, and match flow.
- Player and enemy soldiers continue to render from the V28 direction-locked body+shadow atlases through ECS runtime presentation.
- Bottom player soldiers face upward and top enemy soldiers face downward according to the accepted V28 direction-locked binding.
- Player squad anchor was adjusted up-left after visual review because the last valid V29 proof still placed the player squad too far down/right relative to the target street patch.
- HUD/canvas visual mismatch is intentionally not solved in this pass; UI owns that target-lock later.

## Validation run

- `git diff --check -- Assets/Game/Data/TacticalMaps/Chapter01/iso.ch01.district_edge_01.asset Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs`
- Previous V29 architecture validation:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform editmode -testFilter GameplayArchitectureContractTests -testResults /private/tmp/warlinecapture-gameplay-architecture-contract-v29.xml -logFile /private/tmp/warlinecapture-gameplay-architecture-contract-v29.log`
- Last successful V29 runtime proof before final player-anchor tweak:
  - log: `/private/tmp/warlinecapture-m01-game-flow-v29-ingame-r3.log`
  - runtime capture: `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_1920x1080.png`
  - target comparison: `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_vs_Target_Comparison.png`
  - player crop: `Design/AgentReports/Captures/M01-01_V29_PlayerCrop_Target_Runtime.png`
  - enemy crop: `Design/AgentReports/Captures/M01-01_V29_EnemyCrop_Target_Runtime.png`
- Fresh recapture attempt after final player-anchor tweak:
  - sandbox attempt failed because Unity Package Manager IPC socket was blocked:
    - command: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV29 -logFile /private/tmp/warlinecapture-m01-game-flow-v29-ingame-r4.log -quit`
    - log: `/private/tmp/warlinecapture-m01-game-flow-v29-ingame-r4.log`
    - error: `Error: listen EPERM: operation not permitted /tmp/Unity-Upm-827.sock`
  - escalated rerun started but did not reach the capture method because Unity licensing repeatedly failed handshake:
    - log: `/private/tmp/warlinecapture-m01-game-flow-v29-ingame-r4.log`
    - blocker lines:
      - `HandshakeResponse reported an error: ResponseCode: 505 ResponseStatus: Unsupported protocol version '1.18.1'.`
      - `Timed-out after 60.00s, waiting for channel: "LicenseClient-farhad-6000.4.0"`
      - `Error: 'com.unity.editor.headless' was not found.`
    - process was stopped after the licensing loop did not progress to `WARLINECAPTURE_M01_GAME_FLOW_LAUNCHED`.

## Validation result

- `git diff --check`: passed.
- `GameplayArchitectureContractTests`: passed 6/6 in the V29 run at `/private/tmp/warlinecapture-gameplay-architecture-contract-v29.xml`.
- Last valid V29 runtime proof from r3:
  - flow preserved: `WARLINECAPTURE_M01_GAME_FLOW_LAUNCHED splash=1 main=1 quickCustom=1 match=1 activeMission=saga.ch01.m01.first_contact`
  - ECS runtime ready: `WARLINECAPTURE_M01_GAME_FLOW_RUNTIME_READY player=Entity(2684:1) enemy=Entity(2685:1)`
  - ECS draw path active: `WARLINECAPTURE_M01_ECS_RUNTIME_QUAD_CAPTURE_DRAW_COUNT count=16`
  - V28 player atlas bound: `player_rifle_squad_animation_body_shadow_atlas_v28`
  - V28 enemy atlas bound: `enemy_patrol_animation_body_shadow_atlas_v28`
  - idle animation proof included distinct V28 frame keys: `idle_v28_direction_locked.screen_locked_A.1`, `.2`, `.3`
  - capture completed: `WARLINECAPTURE_M01_GAME_FLOW_CAPTURED path=Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_1920x1080.png`
- Fresh capture after the final player-anchor adjustment is blocked by Unity licensing, so the new `{x: 0.22, y: 0.46}` player anchor is not yet visually verified.

## Known gaps

- Fresh 1920x1080 runtime capture after the final player-anchor adjustment is blocked by Unity licensing, not by missing Gameplay code.
- Last valid r3 proof still showed target-vs-runtime mismatch:
  - player squad was too far down/right before the final anchor adjustment
  - enemy cluster direction was correct but not target-perfect in spacing and overlay placement
  - tactical framing is improved but not final target-perfect
- Soldier shadow overlay diagnostics in r3 show `soldierShadow total=4 visible=0`; this is expected for V28 because body and shadow are integrated in the bound atlas texture rather than rendered as separate shadow quads.
- HUD/canvas mismatch remains intentionally deferred to UI.

## Cross-lane impacts

- UI: HUD/canvas target-lock is still owned by UI. Gameplay preserved runtime state/data and did not tune panels, anchors, chrome, TMP, minimap layout, command rail, squad cards, or threat feed visuals.
- Art/Atlas: no new Art blocker. V28 remains the accepted runtime source for this pass. No new soldier direction/body-scale request is being routed to Art.
- QA: QA should wait for a successful Unity recapture after licensing is fixed, then review the updated V29 image and crops.
- PM: PM attention is needed for the Unity licensing blocker if this machine is expected to keep producing fresh batchmode visual proof.

## Next recommended task

Unblock Unity batchmode licensing on this machine, then rerun `WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV29` and regenerate:

- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/M01-01_V29_PlayerCrop_Target_Runtime.png`
- `Design/AgentReports/Captures/M01-01_V29_EnemyCrop_Target_Runtime.png`

If the recapture confirms the player anchor is now closer, continue only Gameplay-owned world tuning. If the remaining mismatch is HUD/canvas only, route to UI.
