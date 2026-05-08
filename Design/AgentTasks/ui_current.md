# UI Current Task

Date: 2026-05-08
Status: active
Priority: P0 M01 infantry-only HUD scope and first-control selected-squad readability

## Assignment

Fix the UI-owned Gate 4 blocker from `Design/AgentReports/2026-05-08_qa-hci_gate4-focused-rerun.md`: public M01 is an infantry-only teaching slice, but the player-facing HUD still shows APC, Tank, air support, and Build affordances/cards.

Do not start new mockups, M02 work, broad HUD redesign, or unrelated polish. Keep this scoped to the public M01 First Contact route and the Gate 4 HCI findings.

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

Use `/Users/farhad/Projects/WarlineCapture-CodexUnity2`.

Run focused validation proving:

- Public M01 route still reaches the production slice.
- M01 HUD no longer presents APC, Tank, air support, Build, vehicle production, transport, or base/build affordances as usable first-mission options.
- Selected rifle squad HUD state remains readable.
- Objective/result popup route still works.
- No new runtime scene-search usage is introduced in touched files.

If the existing focused PlayMode suite is available in this workspace, run the relevant `Chapter01M01PlayModeValidationTests` filter after the UI change. If Unity cannot run because the workspace is locked or stale, report that as a blocker with the exact command and first failure.

## Cross-Lane Notes

- Gameplay/Art owns world-scale unit readability, four-soldier presentation, selected marker clarity, projectile scale, and atlas art readiness.
- QA/HCI owns final Gate 4 rerun after UI and Gameplay/Art fixes land.
- Support/FTUE owns no action unless QA/HCI finds assistant, Stop, Show Me, result explanation, or invalid-command recovery issues.

## Completion Report

Write the report to:

`Design/AgentReports/2026-05-08_ui_m01-infantry-only-hud-scope.md`

Use the exact format from `Design/WarlineCapture_Agent_Coordination_Workflow.md`, and include:

- HUD affordances before/after
- files changed
- validation command and result
- screenshots/captures if generated
- confirmation that M01 remains infantry-only in player-facing HUD
- confirmation that selected-squad HUD state remains readable
- known gaps or blocked steps
