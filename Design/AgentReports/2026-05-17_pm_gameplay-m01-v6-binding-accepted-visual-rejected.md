# PM Gameplay M01 V6 Binding Accepted Visual Rejected

Date: 2026-05-17
Lane: PM
Status: Gameplay technical binding accepted; final visual rejected; Art/Atlas shadow fix dispatched

## Decision

Accept Gameplay's M01 v6 delivery only as a technical binding milestone.

Do not route QA/HCI and do not call this final visual approval.

Gameplay report:

- `Design/AgentReports/2026-05-17_gameplay_m01-v6-art-binding-runtime-proof.md`

Runtime comparison:

- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v6_vs_Target_Comparison.png`

## Accepted From Gameplay

Gameplay proved:

- normal loading/main-menu/custom-game/match flow remains intact
- M01 still uses the contracted route/id path
- v6 tactical plate is bound in runtime proof
- eight ECS/runtime soldiers render
- TargetMatchV5 top/bottom facings are used instead of the previous side-biased fallback
- enemy readability/health overlays render
- `GameplayArchitectureContractTests` passed 6/6
- focused Gameplay diff check passed

## Rejected For Final Visual Approval

Final visual approval is rejected for now.

Blocking issues:

- the current separate TargetMatchV5 shadow atlas casts in a direction inconsistent with the v6 plate's baked map lighting
- keeping this shadow package would force Gameplay into transform/offset tuning against wrong source art
- runtime HUD composition still diverges from the M01-01 target mockup, but the immediate blocker is Art-owned soldier shadow direction/contact
- full runtime target lock and QA/HCI remain held

## Art/Atlas Dispatch

Art/Atlas owns the next action.

Expected report:

- `Design/AgentReports/2026-05-17_art-atlas_m01-v7-map-light-matched-soldier-shadows.md`

Art must provide M01-light-matched soldier shadows for the accepted TargetMatchV5 facings used by Gameplay:

- player top/bottom/side facings
- enemy top/bottom/side facings
- full animation shadows if feasible in the same pass

Requirements:

- match the v6 plate's baked light direction
- correct foot anchors and contact points
- correct asphalt/sidewalk softness and opacity
- no compact oval/debug shadows
- no shadow direction that fights the baked map shadows
- no runtime transform hack requirement
- prefer integrated body+contact-shadow atlases or per-facing/per-frame shadow atlases with exact pivots/rects/z-order metadata

## Routing

Current owner:

- Art/Atlas

Held:

- Gameplay implementation and further runtime proof
- UI/HCI/QA
- selected-state implementation
- broader M01 sequence expansion

After Art v7 is accepted, Gameplay should bind the corrected shadow/body package and regenerate M01 runtime target-match proof.
