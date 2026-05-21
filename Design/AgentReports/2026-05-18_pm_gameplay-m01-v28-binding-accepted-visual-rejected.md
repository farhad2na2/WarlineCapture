# PM Review - M01 V28 Runtime Binding Accepted, Visual Target Rejected

Date: 2026-05-18
Owner: PM
Status: Gameplay V28 binding accepted; final visual target rejected; Gameplay continues
Priority: P0

## Scope Clarification

2026-05-18 user clarification: Gameplay should fix all in-game tasks now. UI will fix the canvas and HUD later.

Follow-up clarification:

- `Design/AgentReports/2026-05-18_pm_gameplay-m01-v29-ingame-only-clarification.md`

## Reviewed

Gameplay report:

- `Design/AgentReports/2026-05-18_gameplay_m01-v28-runtime-target-match-proof.md`

Runtime proof:

- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v28_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v28_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/M01-01_V28_PlayerCrop_Target_Runtime.png`
- `Design/AgentReports/Captures/M01-01_V28_EnemyCrop_Target_Runtime.png`

## Decision

Accept as a technical Gameplay milestone:

- V28 player/enemy soldier body+shadow atlases are bound in the runtime proof path.
- V28 soldier direction is materially improved versus the rejected V17 direction matrix.
- Soldier scale is closer to the target after the V28 runtime scale adjustment.
- Normal loading/main-menu/custom-game/match flow is reported preserved.
- ECS/runtime presentation proof is present.
- `GameplayArchitectureContractTests` is reported passed 6/6.

Reject as final visual target approval:

- The full runtime frame still does not match `M01-01_TacticalStart_1920x1080.png`.
- HUD layout/proportions are far from the target: objective panel, command rail, minimap, squad cards, and threat/log placement do not match.
- Camera/framing and tactical composition are still not target-perfect.
- Player and enemy formation positions still differ from the target.
- Runtime proof still needs a cleaner region-by-region match assessment and exact owner split for any remaining HUD-owned mismatches.

## No Art Blocker

Do not route this back to Art/Atlas for soldier direction or body scale.

V28 is usable for the next runtime pass. Art/Atlas remains held unless a later runtime proof identifies an exact missing Art-owned sprite, pivot, atlas, shadow, or scale blocker.

## Gameplay Next Action

Gameplay remains the current owner.

Deliver:

- `Design/AgentReports/2026-05-18_gameplay_m01-v29-ingame-target-match-proof.md`

Gameplay must continue implementation rather than stopping at the V28 proof. The next pass must focus on the remaining Gameplay-owned target-match work:

- camera zoom/framing
- tactical map composition
- player formation positions and spacing
- enemy formation positions and spacing
- soldier facing/angles using V28
- no-selection state in the live M01 launch path
- runtime proof that soldiers still come from ECS and V28 atlases

HUD/canvas visual matching is deferred to UI later. Gameplay should preserve correct runtime state/data for the later UI pass, but should not spend V29 on HUD visual layout, canvas scaling, chrome, TMP sizing, sprite slicing, or panel placement.

## Current Routing

Current owner:
Gameplay

Expected report:

- `Design/AgentReports/2026-05-18_gameplay_m01-v29-ingame-target-match-proof.md`

Held:

- QA remains held.
- Art/Atlas remains held.
- UI/HCI remains on its currently routed UI work unless PM/user explicitly dispatches a new M01 HUD pass.
