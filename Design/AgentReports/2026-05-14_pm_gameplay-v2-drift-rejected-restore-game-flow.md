# PM Gameplay V2 Drift Rejected - Restore Game Flow

Date: 2026-05-14
Owner: Gameplay
Status: rejected, restore first
Priority: P0

## Decision

`Design/AgentReports/2026-05-14_gameplay_m01-01-game-scene-implementation-proof-v2.md` is rejected.

The v2 delivery drifted from the assignment. The task was to implement M01-01 through the existing game design/runtime path. It was not permission to replace the app entry flow, bypass navigation, or make `Game.unity` boot directly into a special M01 slice.

## What Went Wrong

Gameplay added scene-startup flow changes and reported that the actual `Game.unity` starts M01-01 through config-backed scene startup. That conflicts with the game design flow the user called out:

- loading screen must remain
- main menu must remain
- custom game mode must remain
- normal navigation/mission launch flow must remain
- M01 implementation must sit behind the existing designed mission/runtime path

This is also an architecture-contract risk. `Design/Architecture/gameplay_solid_ecs_contract.md` says bootstrap composes the application and must not own mission-specific behavior, unit spawning policy, camera/framing policy, UI route rules, or asset-resolution policy.

## Restore-First Requirement

Before any further visual-fit work, Gameplay must restore the existing app flow and remove the v2 scene-startup drift.

The next delivery must explicitly list restored/reverted files and confirm that no M01-specific scene startup replacement remains.

Known v2 drift areas to inspect and restore if they bypass the designed flow:

- `Assets/Game/Data/SceneStartup/`
- `Assets/Game/Scripts/Scenes/`
- `Assets/Game/Scenes/Game.unity`
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureM01GameSceneImplementationBuilder.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs`

Do not remove valid M01 data/content fixes that are needed behind the normal mission path, but do remove or rework any shell/startup replacement that bypasses loading, main menu, custom game mode, or route contracts.

## Correct Next Delivery

Create:

- `Design/AgentReports/2026-05-14_gameplay_m01-01-game-flow-restored-implementation-proof-v3.md`

Required proof:

- existing loading screen, main menu, custom game mode, and normal mission launch path still exist and are not bypassed
- M01-01 is reached through the existing designed game/navigation/mission launch contract
- runtime background matches the approved M01-01 visual lock source through the contracted M01 map path
- runtime visually matches the approved M01-01 target as closely as current assets allow
- all soldiers are visible through ECS/runtime presentation with animation proof
- HUD follows the M01 Designer spec without replacing unrelated app flow
- architecture notes confirm compliance with `Design/Architecture/gameplay_solid_ecs_contract.md`
- `GameplayArchitectureContractTests` result, or explicit reason if not run

## Routing

Current owner remains Gameplay.

QA/HCI, Designer, Art/Atlas, and additional M01 sequence work remain held until PM/user approves the restored-flow v3 implementation proof.
