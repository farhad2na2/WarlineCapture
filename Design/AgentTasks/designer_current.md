# Designer Current Task

Date: 2026-05-08
Status: active
Priority: P0 M01 metric scale/readability contract after rejected temporary art

## Assignment

The user rejected temporary Gate 4 art partly because scale and readability were not contract-driven. Designer must provide a concise design contract so Art/Atlas and Gameplay do not guess.

Read first:

- `Design/AgentReports/2026-05-08_pm_temporary-art-rejected-ecs-scale-motion.md`
- `Design/AgentTasks/designer_pm_message.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md`

Focus on documentation/contract clarity, not implementation. Do not edit gameplay/UI source, Unity prefabs, captures, or other lane task files.

## Required Work

- Define concise M01 tactical scale/readability rules.
- Use the user's anchors: soldier about `1.8m`, building door about `2.3m`, road/context as calibration.
- State how buildings should scale from doors/footprint/readability instead of tiny decor values.
- State selection treatment should be small, grounded, under each soldier or equivalent subtle readable treatment.
- State movement should look like realistic soldier movement and must animate while moving.
- State public M01 unit visuals must be ECS entity / atlas-backed and must not expose SpriteRenderer unit presentation as the accepted path.

## Waiting On

Waiting on lane:
none

Owner of next action:
Designer

Can my lane still continue fallback work? yes, only the required contract above.

## Cross-Lane Notes

- Art/Atlas consumes the scale/readability contract for asset recommendations.
- Gameplay consumes the scale/readability contract for runtime scale, marker, speed, and animation implementation.
- QA/HCI consumes the contract for the next Gate 4 rerun.
- PM owns final acceptance and commit/push.

## Completion Report

Write:

`Design/AgentReports/2026-05-08_designer_m01-metric-scale-readability-contract.md`

Use the standard WarlineCapture handoff format and include:

- Lane
- Task
- Files changed
- Contracts touched
- User-visible behavior
- Validation run
- Validation result
- Known gaps
- Cross-lane impacts
- Next recommended task
