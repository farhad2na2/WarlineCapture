# PM Review - Gameplay V31 Runtime Proof Rejected

Date: 2026-05-19
Lane: PM
Task: Review Gameplay V31 8-direction runtime proof
Status: rejected; routed to Art/Atlas

## Reviewed

Gameplay report:

- `Design/AgentReports/2026-05-19_gameplay_m01-v31-8dir-runtime-proof.md`

Key captures inspected:

- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v31_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV31_PlayerCrop.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV31_EnemyCrop.png`
- `Design/AgentReports/Captures/M01-01_V31_DirectionCellAudit.png`

## Decision

Reject as final visual approval.

Gameplay proved the V31 package is technically bound, but the target-facing visual read is still wrong.

## Accepted Technical Progress

- V31 manifest and body atlas are active in runtime.
- Runtime logs prove player soldiers use `idle_v31_8dir.up_right.*`.
- Runtime logs prove enemy soldiers use `idle_v31_8dir.down_left.*`.
- Body material remains `RGBA(1,1,1,1)`, so no whole-body tint is being used.
- V29 overscan background is still bound.
- Actual M01 flow reaches capture.
- `GameplayArchitectureContractTests` passed.

## Rejection Reasons

- The bottom/player squad is using the V31 `up_right` key, but the pose still does not visually face top-right toward the enemy group in the runtime crop.
- This is no longer a Gameplay closest-direction fallback. The active key is correct; the delivered pose/source labeling is visually wrong for the M01 camera.
- QA cannot validate this until the visual direction read is corrected and re-proven in runtime.

## Art/Atlas Dispatch

Art/Atlas must deliver a V32 soldier package with:

- all eight directions;
- corrected `up_right` pose that visually points top-right in the M01 runtime camera;
- opposite enemy-facing direction still visually correct;
- neutral body+shadow atlas;
- optional technical matching mask atlas only if needed;
- new manifest at `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_8dir_soldier_manifest_v32.json`.

Expected Art report:

- `Design/AgentReports/2026-05-19_art-atlas_m01-v32-corrected-direction-read-assets.md`

## Routing

Art/Atlas is active. Gameplay waits. QA remains held.
