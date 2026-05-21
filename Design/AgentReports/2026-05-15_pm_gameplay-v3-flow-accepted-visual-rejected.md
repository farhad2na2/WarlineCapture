# PM Gameplay V3 Flow Accepted - Visual Rejected

Date: 2026-05-15
Owner: Gameplay
Status: active, continue implementation
Priority: P0

## Decision

`Design/AgentReports/2026-05-14_gameplay_m01-01-game-flow-restored-implementation-proof-v3.md` is accepted only for restoring the app/game flow.

The visual implementation is not accepted. We committed the current iteration as a checkpoint, but M01-01 still does not match the approved target mockup.

## Accepted From V3

- Existing Splash/loading, Main Menu, Quick Custom, and Match routes remain present.
- M01 launches through the existing designed mission launch path.
- No M01-specific scene startup replacement remains.
- `GameplayArchitectureContractTests` passed.

## Rejected From V3

- Battlefield soldiers are not visible in `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v3_1920x1080.png`.
- ECS player/enemy entities exist in logs, but the screenshot does not show the lower-left player rifle squad or upper-right enemy patrol.
- Soldier count, placement, readability, and animation cannot be accepted until visible runtime soldiers appear in the capture.
- After visibility is fixed, Gameplay must also match the approved M01-01 target mockup composition: zoom level, camera angle/framing, player/enemy soldier positions, soldier facing/angles, and no-selection state.

## Required Next Gameplay Delivery

Continue from the restored v3 flow. Do not rewrite shell/navigation again.

Create:

- `Design/AgentReports/2026-05-15_gameplay_m01-01-ecs-soldier-visibility-proof-v4.md`

Create fresh captures:

- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v4_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v4_vs_Target_Comparison.png`

Required implementation focus:

- fix the existing ECS presentation/camera/layer/material/render-texture visibility path
- show the player rifle squad in the lower-left tactical area
- show the enemy patrol in the upper-right tactical area
- match the approved target mockup zoom level and camera angle/framing
- match player and enemy soldier positions, formation offsets, and facing/angles to the M01-01 target
- preserve the M01-01 no-selection state: no selected rings, selected status, selected markers, command target markers, or selected command mode
- keep soldiers rendered through ECS/runtime presentation, not scene-only GameObject hacks or pasted mockup pixels
- preserve the existing loading/main-menu/custom-game-mode launch flow
- keep `Design/Architecture/gameplay_solid_ecs_contract.md` compliance

Required proof:

- visible soldiers in the runtime screenshot
- entity/component/system/render-path evidence proving the visible soldiers come from ECS/runtime presentation
- at least a two-frame or short capture/diagnostic proving visible idle animation changes, or a precise implementation blocker after soldier visibility is fixed
- visual comparison against `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`
- explicit target-vs-runtime comparison for zoom level, camera angle/framing, soldier positions, soldier facing/angles, and selected/no-selected state
- `GameplayArchitectureContractTests` result

## Routing

Current owner remains Gameplay.

QA/HCI, Designer, Art/Atlas full sequence production, and selected-state implementation remain held until PM/user approves the v4 soldier-visible M01-01 proof.
