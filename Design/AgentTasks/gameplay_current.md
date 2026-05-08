# Gameplay Current Task

Date: 2026-05-08
Status: active
Priority: fix/prove manual M01 opening-control window before any PM/user temporary-art approval request

## Assignment

Fix or prove the manual public M01 opening route so the hostile patrol cannot kill `unit.player.rifle_squad_01` before the player has a relaxed first-control window.

PM/user manual review found the enemy can kill the player before they can inspect art, select the squad, or issue the first move. That invalidates the temporary-art approval request until this route is fixed and proven.

Gameplay has delivered the current M01 public first-control readability and selected-marker handoff:

- `Design/AgentReports/2026-05-08_gameplay_m01-unit-readability-selection-art.md`

PM previously accepted it as temporary-art/runtime readability integration evidence in:

- `Design/AgentReports/2026-05-08_pm_art-atlas-gameplay-readability-review.md`

That acceptance is not enough for user art review because the manual Unity path is not currently reviewable.

Do not start M02-M05, broaden combat rebalance, add vehicle work, add base/build mechanics, or do unrelated visual polish.

## Waiting On

Waiting on lane:
Gameplay

Waiting on exact file/report/decision:

- New focused Gameplay report: `Design/AgentReports/2026-05-08_gameplay_m01-manual-opening-control-fix.md`

Owner of next action:
Gameplay owns the fix/proof.

Can my lane still continue fallback work? yes, only on this blocker.

## Required Behavior

M01 First Contact must open with a readable infantry teaching sequence:

- Player can immediately understand they control `unit.player.rifle_squad_01`.
- The player must be able to wait briefly after Deploy without the hostile patrol killing or critically damaging the squad.
- The rifle squad reads as four distinct soldiers under one command/squad identity at actual public gameplay camera scale.
- Selection produces a clear world selected state after selecting the squad: ring, outline, marker, grounding treatment, or equivalent that is readable in public captures.
- The selected state must remain consistent with the existing HUD selected state.
- Player can issue a move order toward `tutorial.move_target.cover_01`.
- Movement uses intended tactical walkable metadata/pathing.
- Attack and result flow remain reachable.
- Enemy projectile/impact visuals remain tactical-scale and do not regress into oversized bullets.
- Visible M01 infantry remains ECS runtime atlas-backed presentation through the accepted `MissionRuntimeAtlasQuadRuntime` path, not `MissionRuntimeSpriteRendererRuntime`, legacy visible `Model`, or separate child `Destroyed` runtime dependency.
- M01 remains infantry-only: one player rifle squad type, one enemy patrol type, no player vehicles, no player vehicle production, no transport, no base/build mechanics.

## Art/Atlas Dependency

Art/Atlas owns the asset/readiness package:

- `Design/AgentTasks/art-atlas_current.md`
- Expected report: `Design/AgentReports/2026-05-08_art-atlas_m01-infantry-atlas-readiness.md`

Gameplay has already integrated the current temporary package enough to produce public selected first-control captures, but PM/user manual review found the route is not safe enough for review. Do not ask for temporary art approval again until this manual opening-control blocker is fixed/proven.

Do not claim final visual/art readiness unless the Art/Atlas report says final-ready or PM/user explicitly accepts the temporary-art package.

## Current Accepted Inputs

Read first:

- `Design/AgentReports/2026-05-08_qa-hci_gate4-focused-rerun.md`
- `Design/AgentReports/2026-05-08_pm_qa-hci-gate4-focused-rerun-review.md`
- `Design/AgentReports/2026-05-08_gameplay_m01-opening-control-window.md`
- `Design/AgentReports/2026-05-08_qa-hci_gameplay-m01-opening-control-window-validation.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`

Accepted but not sufficient for final Gate 4:

- automated public golden path to result popup
- opening-control protection test
- ECS atlas architecture assertions
- four-soldier renderer count assertions
- tactical projectile trace assertions

The current task is about player-facing readability and art readiness in the public first-control composition.

## Validation Required

Use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` and rerun focused validation for the public M01 route, golden path, ECS atlas presentation, selected state, infantry-only scope, scene-search guardrail, and public captures.

Required proof:

- Open/deploy M01 through the same public route the user reviews.
- Let the mission run briefly without input after Deploy.
- Confirm the hostile patrol cannot kill or critically damage the rifle squad before selection/first move.
- Select the rifle squad.
- Issue the first move toward `tutorial.move_target.cover_01`.
- Confirm the squad remains alive and controllable through the first move.
- Then confirm attack/objective/result flow remains reachable.

## Cross-Lane Notes

- UI HUD scope handoff is accepted by PM, but QA/HCI may still report concrete UI findings after the next rerun.
- QA/HCI owns final Gate 4 rerun after this Gameplay blocker is fixed/proven and PM decides whether temporary art is reviewable.
- Support/FTUE owns no action unless the next QA/HCI pass finds assistant, Stop, Show Me, result explanation, or invalid-command recovery issues.

## Completion Report

If new Gameplay follow-up work is assigned, write the report to:

`Design/AgentReports/2026-05-08_gameplay_m01-manual-opening-control-fix.md`

Use the exact format from `Design/WarlineCapture_Agent_Coordination_Workflow.md`, and include:

- squad readability before/after
- manual opening-control behavior before/after
- selected-marker implementation before/after
- Art/Atlas package consumed, pending, or blocked
- files changed
- validation command and result
- generated capture paths, if any
- golden playthrough impact
- confirmation that M01 remains infantry-only
- confirmation that ECS atlas presentation remains the public visible path
- known gaps or blocked steps
