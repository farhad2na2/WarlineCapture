# Gameplay Current Task

Date: 2026-05-08
Status: active
Priority: P0 rejected temporary Gate 4 runtime: ECS-only visuals, scale, selection, motion

## Assignment

The user rejected the temporary Gate 4 art/runtime review. Gameplay must fix and prove the public M01 runtime before QA/HCI or PM asks the user to review again.

Read first:

- `Design/AgentReports/2026-05-08_pm_temporary-art-rejected-ecs-scale-motion.md`
- `Design/AgentTasks/gameplay_pm_message.md`
- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/designer_current.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`

Do not start M02-M05, vehicles, base/build mechanics, broad combat rebalance, or unrelated polish.

## Required Behavior

M01 First Contact must open with a believable readable infantry teaching sequence:

- Visible public M01 units are ECS entity / atlas-backed presentation, not Unity `SpriteRenderer` public unit visuals.
- Remove or replace SpriteRenderer-era runtime naming and components for public M01 unit visuals, including the user-visible `M01RuntimeSpriteRenderers` smell.
- `MissionRuntimeSpriteRendererRuntime` must not be an accepted public M01 unit presentation path.
- Existing prefab/config identity may remain as authoring/data source, but visible child `Model` and separate child `Destroyed` runtime visual dependencies must not be used for public M01 units.
- Scale is automated/contract-driven from Art/Atlas/Designer scale roles, not tiny hardcoded/readability multipliers.
- Soldier scale must read near the user's expected `~0.2` target after metric calibration, unless a scale contract proves a better value.
- Building/decor scale must be calibrated from door/building/road context and must not stay around tiny `0.14` values if visible in the M01 review composition.
- Selection state must be small and grounded under each soldier or equivalent subtle readable treatment.
- No huge green marker covering the screen.
- No unclear blue marker unless Art/Atlas/QA accepts a defined purpose.
- Player rifle squad movement speed must read as realistic soldier movement, not teleporting.
- Movement must use intended tactical pathing metadata.
- The rifle squad must visibly animate while moving/running.
- Enemy projectile/impact visuals must remain tactical-scale and not oversized arcade bullets.
- M01 remains infantry-only: one player rifle squad type, one enemy patrol type, no player vehicles, no vehicle production, no transport, no base/build mechanics.

## Required Validation

Use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` unless PM routes otherwise.

Run focused validation that proves:

- Main Menu -> Saga Map -> M01 First Contact -> Mission Briefing/Loadout -> Deploy still works.
- The player can wait briefly, select the rifle squad, move to cover, attack the patrol, and reach result popup.
- The public M01 unit visuals have no active Unity `SpriteRenderer` components and no `MissionRuntimeSpriteRendererRuntime` component for player/enemy unit visuals.
- Public captures no longer expose `M01RuntimeSpriteRenderers` / SpriteRenderer-era naming for unit presentation.
- Selection markers stay under/near individual soldiers and do not cover the screen.
- Movement speed is bounded/calibrated from config and reads like infantry movement.
- Run/move atlas animation visibly advances while moving.
- M01 remains infantry-only.

If any of this cannot be proven, write a blocker report instead of a completion report.

## Waiting On

Waiting on lane:
Art/Atlas and Designer inputs are useful but do not block initial runtime cleanup.

Owner of next action:
Gameplay

Can my lane still continue fallback work? yes, only the required work above.

## Cross-Lane Notes

- Art/Atlas owns scale/readability art package and selected-state art treatment source.
- Designer owns concise metric scale/readability contract.
- QA/HCI reruns only after Gameplay, Art/Atlas, and Designer reports land.
- UI owns no current follow-up unless QA/HCI finds a HUD regression.
- Support/FTUE owns no current follow-up unless QA/HCI finds assistant/FTUE regression.

## Completion Report

Write:

`Design/AgentReports/2026-05-08_gameplay_m01-ecs-scale-selection-motion-fix.md`

Use the exact format from `Design/WarlineCapture_Agent_Coordination_Workflow.md`, and include:

- SpriteRenderer/proxy removal proof
- ECS atlas presentation proof
- automated scale consumption and target values
- selected-marker before/after
- movement speed before/after
- run animation proof
- files changed
- validation command and result
- generated capture paths
- confirmation that M01 remains infantry-only
- known gaps or blocked steps
