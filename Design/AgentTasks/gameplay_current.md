# Gameplay Current Task

Date: 2026-05-08
Status: active
Priority: P0 selected-readability rejection gate; replace MeshRenderer presentation with ECS entity visuals and fix markers, animation, scale, selection

## Assignment

The user approved the `Game_Legecy` scene-isolation fix. Resume the public M01 selected-readability/ECS visual rejection gate.

Read first:

- `Design/AgentReports/2026-05-08_pm_game-legecy-scene-accepted.md`
- `Design/AgentReports/2026-05-08_pm_selected-readability-rejected-process-failure.md`
- `Design/AgentReports/2026-05-08_art-atlas_m01-marker-animation-scale-package.md`
- `Design/AgentReports/2026-05-08_designer_m01-rejection-scale-marker-contract.md`
- `Design/AgentReports/2026-05-08_ui_m01-marker-selection-overlay-audit.md`
- `Design/AgentTasks/gameplay_pm_message.md`
- `Design/AgentTasks/user_feedback_review_gate.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`

## Required Behavior

- Public M01 units and buildings must be presented as ECS entity visuals, not scene/runtime `GameObject` presentation wrappers with `MeshRenderer`, `MeshFilter`, or `SpriteRenderer` components.
- If true ECS entity rendering is blocked by package/tooling limits, write a blocker report with the exact technical reason and unblock owner. Do not ship another "ECS" pass that is really a GameObject renderer wrapper.
- Remove or replace the accepted visible path that creates `M01RuntimeEcsAtlasQuads` / runtime quad GameObjects for units or buildings.
- The huge green target marker must be replaced with a small world marker about two soldier footsteps wide, positioned under/near the intended point without covering units or the screen.
- Soldier movement animation must use a running/moving loop when moving, idle animation when idle, and must not show crouched/sitting frames or stray feet at the top of the sprite.
- Soldier visual scale/aspect must not be vertically squashed. With the current art, use the user's observed readable target around `0.15` visual scale unless Art/Atlas provides a better contract and QA verifies it.
- Identify and fix the red flashing sitting object/enemy seen on the right side of the review.
- Selection must be easy on the full soldier/body or formation footprint, not only on the foot pixels.
- Placeholder yellow selection squares must not be the final visible selected state. Coordinate with Art/Atlas/UI for the high-end marker asset and do not request user review while placeholder markers remain.
- Keep M01 infantry-only: one player rifle squad type, one enemy patrol type, no player vehicles.

## Validation Required

- Add or update validation so it fails if public M01 runtime visible unit/building presentation uses scene/runtime `MeshRenderer`, `MeshFilter`, or `SpriteRenderer` GameObjects.
- Validate the public route: Main Menu -> Saga Map -> M01 First Contact -> Mission Briefing/Loadout -> Deploy.
- Capture evidence that shows:
  - idle animation,
  - run/move animation,
  - selection marker size and placement,
  - target marker size and placement,
  - enemy patrol/artifact state,
  - scale/aspect against road/building context.
- Use video, frame sequence, or automated measurement for animation/movement. A single screenshot is not enough.
- Include exact Unity command(s), project path, result, and log/result paths.

## Waiting On

Waiting on lane:
none. Art/Atlas, Designer, and UI have delivered their current inputs.

Owner of next action:
Gameplay

Can my lane still continue fallback work? no. This rejection is the active P0.

## Completion Report

Write:

`Design/AgentReports/2026-05-08_gameplay_m01-ecs-visual-marker-animation-reset.md`

Use the standard WarlineCapture handoff format and include a user-feedback matrix for each rejected bullet.
