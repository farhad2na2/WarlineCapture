# PM Gameplay M01 V17 Facing Blocker Art V18 Dispatch

Date: 2026-05-18
Lane: PM
Status: Gameplay diagnostic accepted; final visual rejected; Art/Atlas v18 dispatched

## Decision

Accept Gameplay's v17 delivery as technical binding and diagnostic proof only.

Do not route QA/HCI and do not call this final visual approval.

Gameplay reports:

- `Design/AgentReports/2026-05-18_gameplay_m01-v17-clean-animation-baked-shadow-runtime-proof.md`
- `Design/AgentReports/2026-05-18_gameplay_m01-v17-facing-visual-blocker-art-request.md`

Runtime evidence:

- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v17_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/M01_V17_Runtime_PlayerFacingMatrix_NE_SE_SW_NW.png`

## Accepted From Gameplay

Gameplay proved:

- v17 baked body+shadow atlases bind through ECS/runtime presentation
- separate TargetMatchV5 soldier shadow atlas is no longer bound
- normal loading/main-menu/custom-game/match flow remains intact
- all four v17 player facings were tested in the actual M01 camera
- `GameplayArchitectureContractTests` passed

## Rejected For Final Visual Approval

Final visual approval is rejected.

Blocking issue:

- none of the available v17 player facings reads as the required bottom-soldiers-look-up-screen direction in the actual M01 runtime camera
- compass labels do not solve the problem; the Art package needs screen-space direction-locked cells
- target runtime still needs bottom/player soldiers facing up-screen and top/enemy soldiers facing down-screen

Secondary remaining issues:

- HUD/composition still diverges from the M01-01 target mockup
- final QA/HCI remains held

## Art/Atlas Dispatch

Art/Atlas owns the next action.

Expected report:

- `Design/AgentReports/2026-05-18_art-atlas_m01-v18-direction-locked-baked-soldiers.md`

Art must provide M01 direction-locked baked body+shadow soldier assets:

- player/bottom squad idle direction that clearly faces up-screen toward the tactical field in `M01-01_TacticalStart`
- enemy/top squad idle direction that clearly faces down-screen toward the player squad
- baked/contact shadows matching the v6 plate lighting
- clean single-pose cells with no merged/two-half-frame contamination
- pivots/foot anchors compatible with existing ECS placement

Required proof:

- placement proof on the v6 plate
- side-by-side comparison against `M01-01_TacticalStart_1920x1080.png`
- labels `player_bottom_faces_up_screen` and `enemy_top_faces_down_screen`
- direction-key mapping table for Gameplay
- clean-cell validation
- Gameplay binding checklist

## Routing

Current owner:

- Art/Atlas

Held:

- Gameplay implementation and runtime proof
- UI/HCI/QA
- selected-state implementation
- broader M01 sequence expansion

After Art v18 is accepted, Gameplay should bind the corrected direction-locked package and regenerate M01 runtime target-match proof.
