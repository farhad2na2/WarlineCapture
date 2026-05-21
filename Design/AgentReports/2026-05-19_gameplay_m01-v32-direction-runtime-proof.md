# WarlineCapture Handoff - Gameplay M01 V32 Direction Runtime Proof

Date: 2026-05-19
Lane: Gameplay
Status: ready for PM/user visual review after Gameplay/Art pivot correction; QA still held
Priority: P0

## Lane

Gameplay

## Task

Bind the M01 V32 corrected 8-direction soldier package, keep the V29 overscan background, and produce actual Game scene runtime proof.

## Files changed

- `Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs`
- `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
- `Assets/Game/Scripts/Components/MissionRuntimeComponents.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v32_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV32_PlayerCrop.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV32_EnemyCrop.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v32_pivotfix_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV32_PivotFix_PlayerCrop.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV32_PivotFix_EnemyCrop.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v32_pivotfix2_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV32_PivotFix2_PlayerBootCrop.png`
- `Design/AgentReports/Captures/M01-01_V32_DirectionCellAudit.png`

## Contracts touched

- V32 is now the authoritative M01 soldier binding.
- M01 player/enemy soldiers do not fall through to V31/V30/V29/V28/V5 if V32 binding fails.
- V32 body atlas is bound from `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV32/SharedSoldier/soldier_8dir_animation_body_shadow_atlas_v32_pot_4096x4096.png`.
- V32 optional mask atlas is bound through the existing ECS mask overlay path.
- Body material remains `RGBA(1,1,1,1)`; no whole-body tint is applied to the neutral body atlas.
- Player/bottom idle facing resolves to `NE -> up_right`.
- Enemy/opposite idle facing resolves to `SW -> down_left`.
- Gameplay/Art corrected the V32 manifest frame pivots from `[128,212]` to a runtime visual boot-contact pivot.
- Initial bbox-bottom correction to `y=231/234` still read visually sunk in the M01 camera.
- Final correction uses `pivot.y = body_bbox.bottom + 15`, which puts non-death `up_right/down_left` pivots at `y=246` and removes the visible boot-under-ground read in the runtime crop.
- V29 overscan background remains active.

## User-visible behavior

- Real Game scene flow reaches splash/main menu/custom game/match and captures M01.
- Bottom/player squad renders from the V32 `up_right` key.
- Top/enemy squad renders from the V32 `down_left` key.
- V32 soldiers are larger/clearer than V31 and boot anchoring remains corrected.
- Background covers the 16:9 runtime frame without solid fill.

## Validation run

- Runtime proof before pivot correction:
  - Workspace: `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
  - Command: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV32 -logFile /private/tmp/warlinecapture-m01-game-flow-v32-runtime.log`
- Runtime proof after pivot correction:
  - Workspace: `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
  - Command: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV32 -logFile /private/tmp/warlinecapture-m01-game-flow-v32-pivotfix.log`
- Runtime proof after final visual pivot correction:
  - Workspace: `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
  - Command: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV32 -logFile /private/tmp/warlinecapture-m01-game-flow-v32-pivotfix2.log`
- Architecture contract:
  - Command: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -testResults /private/tmp/warlinecapture-gameplay-architecture-contract-v32.xml -logFile /private/tmp/warlinecapture-gameplay-architecture-contract-v32.log`
- Manifest/direction audit:
  - `m01_8dir_soldier_manifest_v32.json`: 224 frames, 8 directions, 28 frames per direction.

## Validation result

- Runtime capture succeeded before pivot correction:
  - `WARLINECAPTURE_M01_GAME_FLOW_CAPTURED path=Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v32_1920x1080.png`
- Runtime capture succeeded after pivot correction:
  - `WARLINECAPTURE_M01_GAME_FLOW_CAPTURED path=Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v32_1920x1080.png`
  - Copied local proof: `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v32_pivotfix_1920x1080.png`
- Direction/atlas proof:
  - Player logs `frame=idle_v32_8dir.up_right.*`
  - Enemy logs `frame=idle_v32_8dir.down_left.*`
  - Body texture logs `soldier_8dir_animation_body_shadow_atlas_v32_pot_4096x4096`
  - Mask texture logs `soldier_8dir_animation_faction_mask_atlas_v32_pot_4096x4096`
  - Body color logs `RGBA(1.000, 1.000, 1.000, 1.000)`
- ECS animation proof:
  - Runtime diagnostics sampled multiple frame indices for V32 idle animation.
  - Summary logs `runtimes=2 visibleSoldiers=8`.
- Pivot correction proof:
  - Before fix, V32 manifest pivot `[128,212]` left 19 px of visible boot below pivot for non-death frames and 22 px for death frames.
  - Bbox-bottom correction was insufficient visually in the M01 camera.
  - Final visual correction sets pivots 15 px below the visible bbox bottom: `minPivotBelowVisibleBottom=15`, `maxPivotBelowVisibleBottom=15` across all 224 frames.
  - New runtime proof `M01-01_RuntimeV32_PivotFix2_PlayerBootCrop.png` shows the player boot soles above the road surface instead of buried/cut by the map.
- Architecture result:
  - `GameplayArchitectureContractTests`: 6 passed, 0 failed.

## Known gaps

- PM/user still needs to judge final visual match. Gameplay is not routing QA directly.
- V32 mask coverage is low per Art handoff (`0.108`), so faction color is present in the mask overlay logs but remains visually weak in the runtime capture.
- Gameplay/Art corrected the pivot in the V32 manifest directly to avoid another PM/Art ping-pong cycle.
- The HUD/canvas target-lock remains UI-owned and was not modified.
- Unity editor startup still logs a QuickSearch `ArgumentOutOfRangeException`; it did not block runtime capture or tests.

## Cross-lane impacts

- PM/user can now review the V32 in-engine proof for soldier direction and visual match.
- Art/Atlas may need another pass if stronger faction color/readability is required, because Gameplay is not using whole-body tint.
- QA/HCI should remain held until PM/user accepts this V32 proof.

## Next recommended task

PM/user should review `M01-01_GameSceneRuntimeCapture_v32_1920x1080.png` plus the player/enemy crops. If direction is accepted but faction color is too weak, route Art/Atlas for a higher-coverage mask or approved faction-color treatment.
