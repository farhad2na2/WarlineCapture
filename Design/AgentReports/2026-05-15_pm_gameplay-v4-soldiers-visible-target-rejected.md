# PM Gameplay V4 Soldiers Visible - Target Rejected

Date: 2026-05-15
Owner: Gameplay
Status: active, continue implementation
Priority: P0

## Decision

`Design/AgentReports/2026-05-15_gameplay_m01-01-ecs-soldier-visibility-proof-v4.md` is accepted only for the soldier-visibility milestone.

The runtime visual implementation is not accepted yet.

## Accepted From V4

- Existing Splash/Main Menu/Quick Custom/Match flow remains intact.
- M01 still launches through the normal designed runtime path.
- Runtime capture shows eight visible soldiers.
- Visible soldiers are reported as ECS/runtime presentation, not pasted pixels or scene-only soldier hacks.
- M01-01 no-selection state is preserved at a high level.
- `GameplayArchitectureContractTests` passed.

## Rejected From V4

- Runtime battlefield/background does not match the approved target mockup background/content.
- Zoom and framing are closer but still not target-perfect.
- Player squad is visible but positioned farther left/lower than the target.
- Enemy patrol is visible but positioned farther right/higher than the target.
- Enemy red health/readability overlays from the target are missing.
- Bottom HUD and squad card layout do not match the target.
- Idle animation frame cycling is not proven; the v4 report only shows `idle.NE.0` across samples.

## Required Next Gameplay Delivery

Continue from the restored flow and v4 soldier visibility. Do not rewrite shell/navigation again. Do not lose visible ECS soldiers.

Create:

- `Design/AgentReports/2026-05-15_gameplay_m01-01-target-match-proof-v5.md`

Create fresh captures:

- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_vs_Target_Comparison.png`

Required implementation focus:

- align runtime background/source plate to the approved M01-01 target mockup
- match target zoom level and camera framing
- tune player squad positions, formation offsets, and facing/angles to target
- tune enemy patrol positions, formation offsets, and facing/angles to target
- add enemy health/readability overlays through the runtime/ECS/UI presentation path
- improve HUD/card layout match where Gameplay owns runtime binding/state
- prove visible idle animation frame changes
- preserve normal app/game launch flow
- preserve `Design/Architecture/gameplay_solid_ecs_contract.md` compliance

If the approved no-HUD background/source plate required for target match is missing from runtime assets, Gameplay must prove that exact asset gap and ask PM to route Art/Atlas for the source plate. Do not substitute a different battlefield background and call it matched.

## Required Proof

- v5 runtime screenshot and target comparison
- explicit target-vs-runtime comparison for background, zoom, framing, player positions, enemy positions, soldier facing/angles, selected/no-selection state, enemy overlays, HUD/card layout, and idle animation
- ECS/runtime evidence for visible soldiers and overlays
- animation proof showing frame advancement, or a precise implementation blocker after soldier visibility remains working
- `GameplayArchitectureContractTests` result

## Routing

Current owner remains Gameplay.

QA/HCI, Designer, Art/Atlas full sequence production, and selected-state implementation remain held until PM/user approves the v5 target-match proof, except Art/Atlas may be routed only if Gameplay proves the target background/source plate is missing.
