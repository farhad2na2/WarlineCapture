# PM Review - Gameplay V29 Mask Runtime Proof Rejected

Date: 2026-05-19
Lane: PM
Task: Review Gameplay V29 all-faction mask runtime proof
Status: rejected; routed to Art/Atlas

## Reviewed

Gameplay report:

- `Design/AgentReports/2026-05-19_gameplay_m01-v29-all-faction-mask-runtime-proof.md`

Key captures inspected:

- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_player_closest_direction_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV29Mask_PlayerCrop.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV29Mask_EnemyCrop.png`
- `Design/AgentReports/Captures/M01_V29_DirectionKey_Proof.png`
- `Design/AgentReports/Captures/M01_V29_Background_RefreshFit_AspectProof.png`

## Decision

Reject as final visual approval.

Gameplay proved useful runtime progress, but the result is not target-lock ready and must not go to QA yet.

## Accepted Technical Progress

- V29 overscan background is bound and no longer exposes solid-fill side bands at 16:9, 20:9, or 21:9.
- Actual M01 flow remained intact.
- Body atlas and mask atlas runtime path exists.
- `GameplayArchitectureContractTests` passed.

## Rejection Reasons

- V29 soldier atlas lacks required isometric diagonal facings. The direction proof shows only up/away, down/toward, left, and right.
- Gameplay used closest-direction fallback for the player squad. This cannot match the target mockup.
- Mask-only faction color was effectively invisible in Gameplay's pixel audit.
- Gameplay used a visible whole-body tint fallback at one point. That violates the PM guardrail because final faction readability must not tint baked shadows, highlights, and the whole neutral body.
- The runtime capture still reads as a fallback proof, not a final target match.

## Art/Atlas Dispatch

Art/Atlas must deliver a V30 shared soldier package with:

- real diagonal facings: up-right, up-left, down-right, down-left;
- matching body and mask atlases with identical frame rects, pivots, UVs, sequence order, and frame count;
- mask coverage that visibly distinguishes player blue and enemy red at M01 runtime scale without whole-body tint;
- proof previews using mask-only blue/red overlays;
- a new manifest at `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v30.json`.

Expected Art report:

- `Design/AgentReports/2026-05-19_art-atlas_m01-v30-diagonal-faction-mask-assets.md`

## Routing

Art/Atlas is active. Gameplay waits. QA remains held.
