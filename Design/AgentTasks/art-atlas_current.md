# Art/Atlas Current Task

Date: 2026-05-08
Status: active
Priority: P0 selected-readability rejection art package for ECS atlas visuals, markers, animation, scale, and artifacts

## Assignment

The user rejected the selected-readability pass. Provide the art-side package and guidance needed for Gameplay to fix the public M01 review blockers.

Read first:

- `Design/AgentReports/2026-05-08_pm_selected-readability-rejected-process-failure.md`
- `Design/AgentTasks/art-atlas_pm_message.md`
- `Design/AgentTasks/user_feedback_review_gate.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`

## Required Behavior

- Provide or identify the correct atlas frames for player rifle squad idle and run/move states.
- Confirm frame mapping avoids crouched/sitting movement frames and stray foot/top artifacts.
- Provide high-end selected-state marker guidance/assets. The visible marker should be small and under each soldier/footprint, not a large screen-covering shape.
- Provide high-end target/move/attack marker guidance/assets. Target marker should be about two soldier footsteps wide.
- Remove dependency on placeholder yellow squares for user-facing selected state, or clearly block on missing art with exact source needed.
- Provide scale/aspect guidance for current art. The user's current readability target is around `0.15` visual scale; do not force `0.2` if it makes soldiers too large or squashed.
- Identify likely source for the red flashing sitting enemy/object and provide corrected enemy/patrol visual guidance or asset frames.
- Keep M01 infantry-only: player rifle squad and enemy patrol only, no vehicle art requirement for this gate.

## Validation Required

- Provide atlas/frame IDs or asset paths for idle, run, selected, target, and enemy/patrol states.
- Include visual reference/capture paths if generated.
- State whether any source art still needs user approval, and give the exact short user validation instruction if approval is needed.

## Waiting On

Waiting on lane:
none

Owner of next action:
Art/Atlas

Can my lane still continue fallback work? yes, only the package above.

## Completion Report

Write:

`Design/AgentReports/2026-05-08_art-atlas_m01-marker-animation-scale-package.md`

Use the standard WarlineCapture handoff format and include a user-feedback matrix for art-owned items.
