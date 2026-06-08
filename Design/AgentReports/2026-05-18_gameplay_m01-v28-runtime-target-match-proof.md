# Lane
Gameplay

# Task
P0 M01 V28 soldier runtime binding and target-match proof.

# Files changed
- `Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs`
- `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v28_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v28_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/M01-01_Target_PlayerCrop_520x300.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV28_PlayerCrop_520x300.png`
- `Design/AgentReports/Captures/M01-01_V28_PlayerCrop_Target_Runtime.png`
- `Design/AgentReports/Captures/M01-01_Target_EnemyCrop_520x300.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV28_EnemyCrop_520x300.png`
- `Design/AgentReports/Captures/M01-01_V28_EnemyCrop_Target_Runtime.png`
- `Design/AgentReports/2026-05-18_gameplay_m01-v28-runtime-target-match-proof.md`

# Contracts touched
- `Design/Architecture/gameplay_solid_ecs_contract.md`: followed. V28 binding is in the existing ECS/runtime sprite asset resolver and presentation system. No bootstrap mission policy, scene-start replacement, or static gameplay facade was added.
- `Design/M01_FirstContact_Production_Contract.md`: M01 mission/map/unit ids preserved.
- PM dispatch: `Design/AgentReports/2026-05-18_pm_art-atlas-m01-v28-accepted-gameplay-binding.md`.
- Art handoff: `Design/AgentReports/2026-05-18_art-atlas_m01-v28-target-scale-hard-shadow-full-baked-atlases.md`.

# User-visible behavior
M01 still launches through loading/main menu/custom game/match flow. Runtime soldiers now bind V28 body+shadow atlases through the ECS presentation path:

- Player: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV28/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v28.png`
- Enemy: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV28/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v28.png`
- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v28.json`

User-confirmed direction issue is resolved for V28: bottom/player soldiers read toward the top of the screen, and top/enemy soldiers read down-screen. Gameplay then increased only the per-soldier quad scale from `1.8` to `2.45`, while restoring formation metric spacing to `0.21`, so soldiers are no longer tiny without spreading the formation apart.

# Validation run
Runtime capture:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV28 -logFile /private/tmp/warlinecapture-m01-game-flow-v28-runtime-scale-r4.log
```

Comparison and crops:

```bash
magick Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v28_1920x1080.png +append Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v28_vs_Target_Comparison.png
magick Design/AgentReports/Captures/M01-01_Target_PlayerCrop_520x300.png Design/AgentReports/Captures/M01-01_RuntimeV28_PlayerCrop_520x300.png +append Design/AgentReports/Captures/M01-01_V28_PlayerCrop_Target_Runtime.png
magick Design/AgentReports/Captures/M01-01_Target_EnemyCrop_520x300.png Design/AgentReports/Captures/M01-01_RuntimeV28_EnemyCrop_520x300.png +append Design/AgentReports/Captures/M01-01_V28_EnemyCrop_Target_Runtime.png
```

Architecture:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform editmode -testFilter GameplayArchitectureContractTests -testResults /private/tmp/warlinecapture-gameplay-architecture-contract-v28.xml -logFile /private/tmp/warlinecapture-gameplay-architecture-contract-v28.log
```

Diff hygiene:

```bash
git diff --check -- Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs
```

# Validation result
- Runtime capture command exited 0.
- Fresh runtime capture: `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v28_1920x1080.png`.
- Side-by-side: `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v28_vs_Target_Comparison.png`.
- Player crop proof: `Design/AgentReports/Captures/M01-01_V28_PlayerCrop_Target_Runtime.png`.
- Enemy crop proof: `Design/AgentReports/Captures/M01-01_V28_EnemyCrop_Target_Runtime.png`.
- Normal flow proof: `WARLINECAPTURE_M01_GAME_FLOW_LAUNCHED splash=1 main=1 quickCustom=1 match=1 activeMission=saga.ch01.m01.first_contact`.
- Capture proof: `WARLINECAPTURE_M01_GAME_FLOW_CAPTURED path=Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v28_1920x1080.png`.
- ECS visible soldier proof: `WARLINECAPTURE_M01_ECS_QUAD_DIAG_SUMMARY runtimes=2 visibleSoldiers=8`.
- Player atlas proof: `frame=idle_v28_direction_locked.screen_locked_A.1/2/3 tex=player_rifle_squad_animation_body_shadow_atlas_v28`.
- Enemy atlas proof: `frame=idle_v28_direction_locked.screen_locked_A.1/2/3 tex=enemy_patrol_animation_body_shadow_atlas_v28`.
- Runtime draw count: `WARLINECAPTURE_M01_ECS_RUNTIME_QUAD_CAPTURE_DRAW_COUNT count=16`.
- `GameplayArchitectureContractTests`: passed 6/6 at `/private/tmp/warlinecapture-gameplay-architecture-contract-v28.xml`.
- Focused `git diff --check`: passed.

# Known gaps
- V28 soldier direction is now acceptable, and the scale is much closer after the per-soldier scale correction.
- Final visual approval is still held because target-match composition is not exact: HUD layout, command panel/card proportions, minimap placement, and exact camera/framing still differ from the target mockup.
- Soldier positions are close but not target-perfect; further tuning should be ECS/data driven, not pasted pixels or scene-only sprites.
- V28 binding is editor/proof-path based from `Design/VisualLock`; production import/copy under generated runtime asset folders is still a later productionization step.

# Cross-lane impacts
- Art/Atlas: no new Art blocker for direction or body scale at this point. V28 is usable in runtime.
- UI/HCI: still held unless PM wants the HUD/layout mismatch moved to UI.
- QA: still held until PM/user accepts this V28 runtime proof or assigns the next visual polish slice.

# Next recommended task
PM/user review the V28 runtime screenshot and crop proof. If accepted for soldier direction and scale, continue with M01 target-match polish focused on exact soldier/world positions, camera framing, and HUD composition through existing ECS/runtime/UI contracts.
