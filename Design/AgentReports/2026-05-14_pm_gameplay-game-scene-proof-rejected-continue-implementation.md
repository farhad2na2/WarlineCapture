# PM Gameplay Game Scene Proof Rejected - Continue Implementation

Date: 2026-05-14
Owner: Gameplay
Status: rejected, continue implementation
Priority: P0

## Decision

`Design/AgentReports/2026-05-14_gameplay_m01-01-game-scene-implementation-proof.md` is not accepted as delivery.

The report and comparison image are useful because they prove the runtime no longer captures a blank screen, but the result still does not visually match the approved M01-01 target mockup and does not satisfy the M01 Designer spec.

## Rejection Reasons

- Camera/framing does not match the approved M01-01 composition.
- Runtime is using the wrong/old battlefield background. The approved M01-01 mockup background is the visual lock source. `Design/M01_FirstContact_Production_Contract.md` still owns the production ids, including `IsoMapId: iso.ch01.district_edge_01`; do not invent a new mission/map id without updating the contract. Fix stale source art/content behind the contracted M01 map path so the runtime background matches the approved target.
- The correction must follow `Design/Architecture/gameplay_solid_ecs_contract.md`: runtime behavior in ECS data/systems, scene/bootstrap only for composition, no mission-specific behavior in bootstrap/root scene glue, no static logging spread, and no scene-only hacks.
- Soldier placement/count/readability do not match the target lower-left player squad and upper-right enemy patrol layout.
- ECS animation proof is not enough unless the visible runtime soldiers match the target composition.
- Objective copy is wrong: M01-01 target requires `Destroy hostile patrol`.
- Objective/Star Goals panel has extra/incorrect rows and count/state compared with the target.
- Command bar is wrong: M01-01 no-selection state must be neutral/disabled and show the allowed set Select, Move, Attack, Stop, Hold.
- Build is still present as a primary command; M01 must treat Build as unavailable and communicate `MissionDoesNotAllowBuild` if visible.
- ARIA/assistant is visible; M01-01 requires assistant closed.
- Threat feed is noisy and does not match the mission-start/no-selection target state.
- Bottom HUD and minimap layout do not match the target mockup closely enough for implementation approval.

## Required Next Gameplay Delivery

Continue implementation in the actual Game scene path:

- `Assets/Game/Scenes/Game.unity`
- `Assets/Game/Scenes/Game/GameSubScene.unity`

Create a corrected proof report:

- `Design/AgentReports/2026-05-14_gameplay_m01-01-game-scene-implementation-proof-v2.md`

Create fresh proof captures under:

- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v2_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v2_vs_Target_Comparison.png`

The v2 proof must show the actual Game scene, not a pasted flattened mockup, and must explicitly pass or identify remaining code-level fixes for:

- runtime map/background source matches the approved M01-01 mockup background while preserving the M01 production contract ids unless the contract is deliberately updated
- implementation compliant with `Design/Architecture/gameplay_solid_ecs_contract.md`
- all visible soldiers from the approved target/spec
- ECS animated soldier presentation visible in the capture
- camera/framing
- objective and Star Goals panel
- command panel no-selection state
- allowed command set Select, Move, Attack, Stop, Hold
- Build unavailable reason `MissionDoesNotAllowBuild`
- squad cards
- threat feed
- minimap start viewport
- assistant closed
- no selected rings/status/world markers
- architecture notes listing touched systems/files and confirming compliance with `Design/Architecture/gameplay_solid_ecs_contract.md`
- `GameplayArchitectureContractTests` result, or an explicit explanation if the test could not be run

## Routing

Current owner remains Gameplay.

QA/HCI, Designer, Art/Atlas, and additional M01 sequence work remain held until PM/user approves the corrected v2 Game scene implementation proof.
