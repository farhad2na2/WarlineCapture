# Gameplay Current Task

Date: 2026-05-08
Status: waiting
Priority: blocked on PM/user art decision and UI HUD scope fix before QA/HCI rerun

## Assignment

Wait for PM/user art decision and the UI HUD scope handoff before doing more Gameplay work.

Gameplay has delivered the current M01 public first-control readability and selected-marker handoff:

- `Design/AgentReports/2026-05-08_gameplay_m01-unit-readability-selection-art.md`

PM accepted it as valid temporary-art/runtime readability integration evidence in:

- `Design/AgentReports/2026-05-08_pm_art-atlas-gameplay-readability-review.md`

Do not repeat the same readability work, start M02-M05, broaden combat rebalance, add vehicle work, add base/build mechanics, or do unrelated visual polish.

## Waiting On

Waiting on lane:
PM/user and UI

Waiting on exact file/report/decision:

- PM/user approval or rejection of the temporary M01 infantry art package from `Design/AgentReports/2026-05-08_art-atlas_m01-infantry-atlas-readiness.md`
- `Design/AgentReports/2026-05-08_ui_m01-infantry-only-hud-scope.md`

Owner of next action:
PM/user owns the art decision. UI owns the HUD scope report.

Can my lane still continue fallback work? no

## Required Behavior

M01 First Contact must open with a readable infantry teaching sequence:

- Player can immediately understand they control `unit.player.rifle_squad_01`.
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

Gameplay has already integrated the current temporary package enough to produce public selected first-control captures. Continue only if PM/user requests changes after the art decision, Art/Atlas provides replacement assets, or QA/HCI reports a concrete Gameplay regression.

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

No new Gameplay validation is required while waiting. If PM/user, Art/Atlas, or QA/HCI assigns a concrete Gameplay follow-up, use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` and rerun focused validation for the affected public M01 route, golden path, ECS atlas presentation, selected state, infantry-only scope, scene-search guardrail, and public captures.

## Cross-Lane Notes

- UI owns the separate HUD blocker: APC, Tank, air support, Build, vehicle/build affordances must not appear as usable M01 options.
- QA/HCI owns final Gate 4 rerun after this Gameplay/Art fix and the UI HUD fix land.
- Support/FTUE owns no action unless the next QA/HCI pass finds assistant, Stop, Show Me, result explanation, or invalid-command recovery issues.

## Completion Report

If new Gameplay follow-up work is assigned, write the report to:

`Design/AgentReports/2026-05-08_gameplay_<specific-followup>.md`

Use the exact format from `Design/WarlineCapture_Agent_Coordination_Workflow.md`, and include:

- squad readability before/after
- selected-marker implementation before/after
- Art/Atlas package consumed, pending, or blocked
- files changed
- validation command and result
- generated capture paths, if any
- golden playthrough impact
- confirmation that M01 remains infantry-only
- confirmation that ECS atlas presentation remains the public visible path
- known gaps or blocked steps
