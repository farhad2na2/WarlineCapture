# UI Current Task

Date: 2026-05-08
Status: waiting
Priority: no current UI action; waiting for rejected temporary-art fixes by Art/Atlas, Designer, and Gameplay

## Assignment

Stand by while Art/Atlas, Designer, and Gameplay handle the rejected temporary-art/runtime review.

UI has delivered and PM accepted the infantry-only HUD scope fix:

- `Design/AgentReports/2026-05-08_ui_m01-infantry-only-hud-scope.md`
- `Design/AgentReports/2026-05-08_pm_ui-m01-infantry-only-hud-scope-review.md`

Do not repeat the same HUD scope work, start new mockups, M02 work, broad HUD redesign, or unrelated polish. Re-engage only if QA/HCI reports a concrete HUD/UI regression after the rejected-art fixes land.

## Waiting On

Waiting on lane:
Art/Atlas, Designer, Gameplay, then QA/HCI

Waiting on exact file/report/decision:

- rejected temporary Gate 4 art/runtime fixes and QA/HCI rerun

Owner of next action:
Art/Atlas, Designer, Gameplay, then QA/HCI.

Can my lane still continue fallback work? no

## Required Behavior

For M01 First Contact only:

- The public HUD must match infantry-only scope.
- APC, Tank, air support, Build, vehicle production, transport, and base/build affordances must be removed, hidden, locked with explicit unavailable treatment, or otherwise clearly suppressed so the player is not invited to use them in M01.
- The selected rifle squad HUD state must remain clear after selecting `unit.player.rifle_squad_01`.
- The command UI must still support the golden playthrough: select rifle squad -> move to `tutorial.move_target.cover_01` -> attack `unit.enemy.patrol_01` -> objective/result popup.
- Do not break existing public route shell flow: Main Menu -> Saga Map -> M01 First Contact -> Mission Briefing/Loadout -> Deploy.
- Do not use runtime scene-search patterns in touched files.

## Current Accepted Inputs

Read first:

- `Design/AgentReports/2026-05-08_qa-hci_gate4-focused-rerun.md`
- `Design/AgentReports/2026-05-08_pm_qa-hci-gate4-focused-rerun-review.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`

Previously accepted UI evidence remains useful but is no longer sufficient:

- `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`
- `Design/AgentReports/2026-05-08_pm_public-launch-handoff-workspace-review.md`

## Validation Required

No new UI validation is required while waiting. If QA/HCI reports a concrete UI regression, use `/Users/farhad/Projects/WarlineCapture-CodexUnity2` and rerun focused validation for the affected public M01 route, HUD scope, selected-squad state, result flow, and scene-search guardrail.

## Cross-Lane Notes

- Gameplay/Art owns world-scale unit readability, four-soldier presentation, selected marker clarity, projectile scale, and atlas art readiness.
- QA/HCI owns final Gate 4 rerun after UI and Gameplay/Art fixes land.
- Support/FTUE owns no action unless QA/HCI finds assistant, Stop, Show Me, result explanation, or invalid-command recovery issues.

## Completion Report

If new UI follow-up work is assigned, write the report to:

`Design/AgentReports/2026-05-08_ui_<specific-followup>.md`

Use the exact format from `Design/WarlineCapture_Agent_Coordination_Workflow.md`, and include:

- HUD affordances before/after
- files changed
- validation command and result
- screenshots/captures if generated
- confirmation that M01 remains infantry-only in player-facing HUD
- confirmation that selected-squad HUD state remains readable
- known gaps or blocked steps
