# QA/HCI Current Task

Date: 2026-05-08
Status: waiting
Priority: waiting for rejected temporary-art fixes before next Gate 4 rerun

## Assignment

Stand by. The user rejected the temporary Gate 4 art/runtime review, so QA/HCI must not rerun or ask for approval until the required Art/Atlas, Designer, and Gameplay follow-ups land.

Read first:

- `Design/AgentReports/2026-05-08_pm_temporary-art-rejected-ecs-scale-motion.md`
- `Design/AgentTasks/qa-hci_pm_message.md`
- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/designer_current.md`
- `Design/AgentTasks/gameplay_current.md`

## Waiting On

Art/Atlas report:

- `Design/AgentReports/2026-05-08_art-atlas_m01-rejected-temp-art-scale-readability.md`

Designer report:

- `Design/AgentReports/2026-05-08_designer_m01-metric-scale-readability-contract.md`

Gameplay report:

- `Design/AgentReports/2026-05-08_gameplay_m01-ecs-scale-selection-motion-fix.md`

Owner of next action:
Art/Atlas, Designer, Gameplay

Can QA/HCI continue fallback work? no

## Required Rerun Criteria After Fixes Land

QA/HCI must validate:

- Public M01 golden path remains intact: Main Menu -> Saga Map -> M01 First Contact -> Mission Briefing/Loadout -> Deploy -> select rifle squad -> move to tutorial cover -> attack hostile patrol -> enemy destroyed/neutralized -> objective/result popup.
- Player has a relaxed first-control window and can inspect before hostile damage.
- Public M01 unit visuals have no active Unity `SpriteRenderer` components and no `MissionRuntimeSpriteRendererRuntime` component for player/enemy unit visuals.
- `M01RuntimeSpriteRenderers` / SpriteRenderer-era naming no longer appears as the public unit presentation.
- M01 player squad reads as four distinct soldiers.
- Soldier/building/decor scale reads from the metric scale contract and is no longer tiny.
- Selected state is small/grounded under soldiers or equivalent subtle treatment; no huge screen-covering marker; no unclear blue marker.
- Movement speed reads as realistic infantry movement, not teleporting.
- Run/move animation visibly advances while moving.
- Projectile/impact/death feedback remains tactical-scale.
- M01 remains infantry-only with no player vehicles, transport, or base/build mechanics.

## Completion Report

After all required upstream reports land and QA/HCI reruns, write:

`Design/AgentReports/2026-05-08_qa-hci_gate4-rejected-art-rerun.md`

Use the exact format from `Design/WarlineCapture_Agent_Coordination_Workflow.md`.
