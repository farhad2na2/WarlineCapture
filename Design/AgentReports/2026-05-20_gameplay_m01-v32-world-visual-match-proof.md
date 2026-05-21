# WarlineCapture Handoff - Gameplay M01 V32 World Visual Match Proof

Date: 2026-05-20
Lane: Gameplay
Status: ready for PM/user visual review; QA still held until accepted
Priority: P0

## Lane

Gameplay

## Task

Fix the rejected M01 V32 runtime world visual match while keeping V32 soldier binding, V29 overscan background, ECS animation, normal Game flow, and architecture contract rules.

## Files changed

- `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v32_worldmatch_markerfix_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV32_WorldMatchMarkerFix_PlayerCrop.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV32_WorldMatchMarkerFix_EnemyCrop.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV32_WorldMatchMarkerFix_PlayerMarkerBootCrop_4x.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV32_WorldMatchMarkerFix_EnemyMarkerBootCrop_4x.png`
- `Design/AgentReports/2026-05-20_gameplay_m01-v32-world-visual-match-proof.md`

## Contracts touched

- V32 soldier manifest remains authoritative.
- V32 `up_right` remains active for player/bottom soldiers.
- V32 `down_left` remains active for enemy/opposite soldiers.
- V29 overscan background remains active.
- Runtime soldier rendering remains ECS-driven through `MissionRuntimeAtlasQuadPresentationSystem`.
- Selection and enemy readability markers are now drawn through the ECS runtime quad draw path instead of being created but omitted from capture/runtime rendering.

## User-visible behavior

- Player squad renders as four separated soldiers with visible blue selected rings.
- Enemy patrol renders as four separated soldiers with red boot-ground readability rings and health bars above the soldiers.
- Rings are smaller and closer to the boot contact point than the rejected V32/worldmatch captures.
- Normal splash/main menu/quick custom/match routing remains intact.

## Validation run

- Runtime proof:
  - Workspace: `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
  - Command: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV32 -logFile /private/tmp/warlinecapture-m01-v32-worldmatch-markerfix4.log`
- Architecture contract:
  - Workspace: `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
  - Command: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -testResults /private/tmp/warlinecapture-gameplay-architecture-contract-v32-worldmatch-markerfix.xml -logFile /private/tmp/warlinecapture-gameplay-architecture-contract-v32-worldmatch-markerfix.log`

## Validation result

- Runtime capture passed: `WARLINECAPTURE_M01_GAME_FLOW_CAPTURED`.
- Normal flow proof passed: log reports `splash=1 main=1 quickCustom=1 match=1 activeMission=saga.ch01.m01.first_contact`.
- Direction audit passed:
  - player frames log `idle_v32_8dir.up_right.*`;
  - enemy frames log `idle_v32_8dir.down_left.*`.
- Marker proof passed:
  - selection overlay summary logs `kind=selection total=4 visible=4` for the player runtime;
  - enemy readability overlay summary logs `kind=enemyReadability total=4 visible=4`;
  - enemy health bar overlay summary logs `kind=enemyHealthBar total=4 visible=4`.
- ECS animation proof passed: diagnostics sampled changing V32 idle frame keys and logged `runtimes=2 visibleSoldiers=8`.
- Architecture result passed: `GameplayArchitectureContractTests` 6 passed, 0 failed.

## Known gaps

- PM/user still needs to judge final visual match against the target mockup before QA routing.
- V32 manifest currently points to the V32 bootfix POT atlas, which was already present in the active V32 manifest to repair cut boot pixels.
- HUD/canvas target lock remains UI-owned and was not modified.
- Unity logs still include non-blocking editor shutdown/preview-scene leak warnings after successful capture/tests.

## Cross-lane impacts

- PM can review the fresh full capture plus player/enemy crops for visual acceptance.
- QA/HCI remains blocked until PM/user accepts this corrected Gameplay proof.
- UI lane is untouched.

## Next recommended task

PM/user should review `M01-01_GameSceneRuntimeCapture_v32_worldmatch_markerfix_1920x1080.png` and the boot marker crops. If accepted, route to QA/HCI; if rejected, Gameplay should tune only marker scale/position and squad spacing, not swap art packages.
