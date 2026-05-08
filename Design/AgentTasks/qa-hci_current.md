# QA/HCI Current Task

Date: 2026-05-08
Status: active
Priority: rerun focused Gate 4 after rejected-art fixes landed

## Assignment

Rerun focused Gate 4 validation after the rejected temporary-art fixes.

Read first:

- `Design/AgentReports/2026-05-08_pm_temporary-art-rejected-ecs-scale-motion.md`
- `Design/AgentReports/2026-05-08_pm_rejected-art-fixes-ready-for-qa-review.md`
- `Design/AgentReports/2026-05-08_designer_m01-metric-scale-readability-contract.md`
- `Design/AgentReports/2026-05-08_art-atlas_m01-rejected-temp-art-scale-readability.md`
- `Design/AgentReports/2026-05-08_gameplay_m01-ecs-scale-selection-motion-fix.md`
- `Design/WarlineCapture_M01_Metric_Scale_Readability_Contract.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`

## Required Validation

Use `/Users/farhad/Projects/WarlineCapture-CodexUnity3` unless PM routes otherwise.

Validate:

- Public M01 golden path: Main Menu -> Saga Map -> M01 First Contact -> Mission Briefing/Loadout -> Deploy -> select rifle squad -> move to tutorial cover -> attack hostile patrol -> enemy destroyed/neutralized -> objective/result popup.
- Player has a relaxed first-control window and can inspect before hostile damage.
- Public M01 unit visuals have no active Unity `SpriteRenderer` components for player/enemy unit visuals.
- Public M01 player/enemy units have no `MissionRuntimeSpriteRendererRuntime` component.
- `M01RuntimeSpriteRenderers` / SpriteRenderer-era naming no longer appears as public unit presentation.
- M01 player squad reads as four distinct soldiers.
- Infantry scale reads near `0.20`.
- Visible building/decor scale direction reads near `0.80` where used as door/road-context anchor.
- Selected state is small/grounded under soldiers or equivalent subtle treatment; no huge screen-covering marker; no unclear blue marker.
- Movement speed reads as realistic infantry movement, not teleporting.
- Run/move animation visibly advances while moving.
- Projectile/impact/death feedback remains tactical-scale.
- M01 remains infantry-only with no player vehicles, transport, or base/build mechanics.

Generate fresh public review captures if graphics device is available:

- 16:9 selected first-control
- 20:9 selected first-control
- any before/after or route captures needed to make the review unambiguous

## Waiting On

Waiting on lane:
none

Owner of next action:
QA/HCI

Can QA/HCI continue fallback work? yes, only the validation above.

## Completion Report

Write:

`Design/AgentReports/2026-05-08_qa-hci_gate4-rejected-art-rerun.md`

Use the exact format from `Design/WarlineCapture_Agent_Coordination_Workflow.md`, and include:

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
- capture paths
- whether PM/user should review, and exact review instructions if ready
