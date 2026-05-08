# Gameplay Current Task

Date: 2026-05-08
Status: active
Priority: fix M01 selected first-control soldier readability before user review

## Assignment

QA/HCI passed automated rejected-art validation, but PM visual review found the fresh selected first-control captures are not ready for user approval.

Read first:

- `Design/AgentReports/2026-05-08_qa-hci_gate4-rejected-art-rerun.md`
- `Design/AgentReports/2026-05-08_pm_qa-hci-rejected-art-rerun-review.md`
- `Design/AgentReports/2026-05-08_pm_temporary-art-rejected-ecs-scale-motion.md`
- `Design/WarlineCapture_M01_Metric_Scale_Readability_Contract.md`

Do not start M02-M05, vehicles, base/build mechanics, broad combat rebalance, or unrelated polish.

## Required Fix

Fix the selected first-control public composition:

- The player squad must read as four distinct individual soldiers, not a crowded duplicated blob/cluster.
- Soldier spacing/layout must be readable at public 16:9 and 20:9 gameplay scale.
- Selected-state treatment must be visible as small grounded markers under/near each soldier.
- The selected markers must not become huge overlays or unclear blue/green UI-like effects.
- Keep ECS atlas quad presentation.
- Keep no public player/enemy unit `SpriteRenderer` components.
- Keep no public `MissionRuntimeSpriteRendererRuntime` component.
- Keep realistic movement speed and move/run animation proof.
- Keep the public M01 golden path intact.
- Keep M01 infantry-only.

## Waiting On

Waiting on lane:
Art/Atlas may need to confirm individual-soldier frame/source readiness, but Gameplay can begin layout/marker visibility fixes now.

Owner of next action:
Gameplay

Can my lane still continue fallback work? yes, only the required fix above.

## Validation Required

Use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` unless PM routes otherwise.

Rerun focused PlayMode validation and generate fresh selected first-control captures.

Required proof:

- selected first-control 16:9 capture
- selected first-control 20:9 capture
- four distinct individual soldiers visible in world
- selected marker visible under/near each soldier
- no public SpriteRenderer unit presentation
- golden path still reaches result popup

## Completion Report

Write:

`Design/AgentReports/2026-05-08_gameplay_m01-soldier-readability-selection-fix.md`

Use the standard WarlineCapture handoff format and include capture paths.
