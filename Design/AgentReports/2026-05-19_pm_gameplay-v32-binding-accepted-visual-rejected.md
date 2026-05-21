# PM Review - Gameplay V32 Binding Accepted, Runtime Visual Rejected

Date: 2026-05-19
Lane: PM
Status: rejected back to Gameplay
Priority: P0

## Reviewed Gameplay Delivery

- `Design/AgentReports/2026-05-19_gameplay_m01-v32-direction-runtime-proof.md`

Reviewed captures:

- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v32_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV32_PlayerCrop.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV32_EnemyCrop.png`
- `Design/AgentReports/Captures/M01-01_V32_DirectionCellAudit.png`

## Decision

V32 binding proof is accepted.

Runtime visual match is rejected.

Gameplay owns the next correction pass. Do not route this to QA yet.

## Accepted

- V32 manifest and body atlas are bound in runtime.
- Player/bottom soldiers use the V32 `up_right` key.
- Enemy/opposite soldiers use the V32 `down_left` key.
- Body material remains untinted.
- V29 overscan background remains active.
- ECS animation is running.
- `GameplayArchitectureContractTests` passed.

## Rejected Runtime Visual

The current capture does not match the target gameplay mockup closely enough:

- Soldier scale is too large against the battlefield and target reference.
- Player squad members are clustered too tightly and read as overlapping mass instead of four readable soldiers.
- Enemy squad members are also oversized and clustered.
- Player selected-state blue ground rings are missing in the player crop.
- Enemy red ground rings are offset/detached from the soldiers instead of sitting under the boots.
- Runtime positions do not match the target lock composition closely enough.
- The proof does not demonstrate target-matching soldier positions, angles, selected states, zoom level, and marker attachment.

HUD/canvas quality remains UI-owned, but in-world soldier scale, placement, facing, selected rings, enemy rings, ECS animation, and background composition are Gameplay-owned for this pass.

## Required Gameplay Fix

Keep the V32 soldier package. Do not ask Art for new soldier pixels for this pass.

Gameplay must tune the actual Game scene implementation to match the M01 target composition:

- scale V32 soldiers to the target mockup size;
- separate the four player soldiers into readable target-like positions;
- place the enemy group in the target-like upper-right cluster without oversized overlap;
- keep player soldiers facing toward the enemy with V32 `up_right`;
- keep enemy soldiers facing toward the player with V32 `down_left`;
- attach blue selected rings under every selected player soldier;
- attach red enemy rings under enemy boots with no detached marker offset;
- keep health bars above enemy heads where target requires them;
- keep V29 overscan background and target-like zoom/framing;
- keep normal Game flow, loading, menu, and custom game routing intact;
- keep ECS architecture and animation path intact;
- do not use placeholders, flattened mockup overlays, whole-body tint, or non-V32 soldier atlases.

## Expected Gameplay Report

- `Design/AgentReports/2026-05-19_gameplay_m01-v32-world-visual-match-proof.md`

Required proof:

- fresh 16:9 runtime capture from the real Game scene;
- player and enemy crops;
- marker attachment crops proving blue/red ground rings sit under the correct soldiers;
- direction audit proving V32 `up_right` and V32 `down_left` remain active;
- before/after note against the rejected V32 capture for scale, spacing, ring attachment, and selected states;
- ECS animation proof;
- architecture test result;
- normal flow proof.

## QA Routing

QA/HCI remains blocked until PM/user accepts the corrected Gameplay visual proof.
