# Gameplay Current Task

Date: 2026-05-08
Status: waiting
Priority: accepted rejected-art runtime fix handoff; waiting for QA/HCI rerun

## Assignment

Wait for QA/HCI to rerun focused Gate 4 validation against the rejected-art fixes.

Gameplay delivered:

- `Design/AgentReports/2026-05-08_gameplay_m01-ecs-scale-selection-motion-fix.md`

PM accepted the handoff for QA/HCI rerun in:

- `Design/AgentReports/2026-05-08_pm_rejected-art-fixes-ready-for-qa-review.md`

Do not start M02-M05, vehicles, base/build mechanics, broad combat rebalance, or unrelated polish.

## Waiting On

Waiting on lane:
QA/HCI

Waiting on exact report:

- `Design/AgentReports/2026-05-08_qa-hci_gate4-rejected-art-rerun.md`

Owner of next action:
QA/HCI

Can my lane still continue fallback work? no

## Accepted QA Checklist Inputs

QA/HCI should validate the Gameplay handoff claims:

- public M01 unit visuals use ECS atlas quad presentation
- no public player/enemy unit `SpriteRenderer` components
- no public `MissionRuntimeSpriteRendererRuntime` component on player/enemy units
- no `M01RuntimeSpriteRenderers` public unit root naming
- infantry scale near `0.20`
- building/decor readability direction near `0.80` where visible as door/road-context anchor
- small grounded per-soldier selection markers
- realistic infantry movement speed around the reported `0.42` run / `0.28` walk values
- visible move/run animation while moving
- public M01 golden path still reaches result popup
- M01 remains infantry-only

## Cross-Lane Notes

- Art/Atlas final art gaps remain but do not block the QA rerun.
- Designer metric contract is accepted as the visual/readability checklist source.
- UI and Support/FTUE have no current action unless QA/HCI finds a concrete issue.
- PM/user owns final visual approval after QA/HCI provides reviewable evidence.

## Completion Report

If QA/HCI or PM assigns a concrete Gameplay follow-up, write:

`Design/AgentReports/2026-05-08_gameplay_<specific-followup>.md`

Use the standard WarlineCapture handoff format.
