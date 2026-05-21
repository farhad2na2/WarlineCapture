# WarlineCapture Handoff - Gameplay M01 V31 8-Direction Runtime Proof

Date: 2026-05-19
Lane: Gameplay
Status: needs Art/PM fix before QA
Priority: P0

## Lane

Gameplay

## Task

Bind the V31 8-direction neutral soldier package for M01, keep the V29 overscan background, produce actual runtime proof, and validate architecture contracts.

## Files changed

- `Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs`
- `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
- `Assets/Game/Scripts/Components/MissionRuntimeComponents.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v31_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV31_PlayerCrop.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV31_EnemyCrop.png`
- `Design/AgentReports/Captures/M01-01_V31_DirectionCellAudit.png`

## Contracts touched

- V31 manifest is now checked before V29/V28/V17 legacy soldier fallbacks.
- V31 body atlas is bound from `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV31/SharedSoldier/soldier_8dir_animation_body_shadow_atlas_v31_pot_4096x4096.png`.
- V31 optional faction mask atlas is bound through the existing mask overlay path, not whole-body body tint.
- Body material color remains `RGBA(1,1,1,1)` for final atlas art.
- Boot pivot `[128,212]` is now used by the ECS quad placement so the soldier feet are anchored instead of drawing from cell center.
- Player idle facing resolves to `NE -> up_right`; enemy idle facing resolves to `SW -> down_left`.
- V29 overscan background remains the tactical map source.
- Capture timeout and post-runtime wait were adjusted only for the proof tool so batchmode can write the image before memory pressure grows.

## User-visible behavior

- M01 actual flow reaches splash/main menu/custom game/match and captures a fresh 16:9 runtime frame.
- Soldiers now render from V31 with boot pivot placement and faction mask colors.
- Red enemy health bars/rings remain visible.
- Background covers the 16:9 frame with no solid-color fill at the edges.

## Validation run

- Unity runtime proof:
  - Workspace: `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
  - Command: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV31 -logFile /private/tmp/warlinecapture-m01-game-flow-v31-runtime-r4.log`
- Direction manifest audit:
  - Parsed `m01_8dir_soldier_manifest_v31.json`: 224 frames, 8 directions, 28 frames per direction.
- Architecture contract:
  - Command: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -testResults /private/tmp/warlinecapture-gameplay-architecture-contract-v31-r2.xml -logFile /private/tmp/warlinecapture-gameplay-architecture-contract-v31-r2.log`

## Validation result

- Runtime capture succeeded:
  - `WARLINECAPTURE_M01_GAME_FLOW_CAPTURED path=Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v31_1920x1080.png`
- ECS V31 proof:
  - Player soldiers log `frame=idle_v31_8dir.up_right.*`
  - Enemy soldiers log `frame=idle_v31_8dir.down_left.*`
  - Body texture logs `soldier_8dir_animation_body_shadow_atlas_v31_pot_4096x4096`
  - Body color logs `RGBA(1.000, 1.000, 1.000, 1.000)`
  - Mask texture logs `soldier_8dir_animation_faction_mask_atlas_v31_pot_4096x4096`
  - Mask colors log blue player `RGBA(0.015, 0.035, 0.150, 0.700)` and red enemy `RGBA(0.170, 0.020, 0.015, 0.700)`
- Architecture contract passed:
  - `GameplayArchitectureContractTests`: 6 passed, 0 failed.

## Known gaps

- PM/user visual approval should not proceed to QA yet.
- Exact blocker: the runtime uses V31 `up_right` for the bottom player squad, but the delivered V31 `up_right` cell visually reads as the wrong diagonal for the target mockup. User confirmed the bottom soldiers still do not look top-right toward the enemy.
- The blocker appears to be the delivered V31 art direction/source labeling, not a Gameplay fallback: runtime log proves the V31 `up_right` key and V31 body atlas are active.
- The optional mask restores faction readability, but the final color balance still needs PM/user approval.
- Unity log still contains a Unity QuickSearch `ArgumentOutOfRangeException` during editor startup. It did not block capture or tests.

## Cross-lane impacts

- Art/Atlas owns the visual-direction blocker. Need a corrected soldier package where bottom/player top-right pose visually matches the M01 mockup and top/enemy down-left remains correct.
- PM should keep QA held until the corrected direction package is bound and visually accepted.
- Gameplay can continue once Art/Atlas supplies corrected V31 replacement cells or a V32 package.

## Next recommended task

Art/Atlas should produce a corrected all-direction soldier package, or at minimum corrected `up_right` player-facing frames that visually point top-right in the M01 runtime camera. Gameplay should then rebind the corrected package and rerun the same runtime proof.
