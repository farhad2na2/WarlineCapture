# UI Current Task

Date: 2026-05-08
Status: active
Priority: P0 audit M01 target/selection overlay ownership after user rejection

## Assignment

Audit whether UI owns any part of the huge green target marker, selected-state marker, unit selection affordance, or placeholder square seen in the rejected M01 review.

Read first:

- `Design/AgentReports/2026-05-08_pm_selected-readability-rejected-process-failure.md`
- `Design/AgentTasks/user_feedback_review_gate.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`

## Required Behavior

- If UI owns the huge green target marker, make or route the fix so it is about two soldier footsteps wide and does not cover units or the screen.
- If UI owns the selected-state marker or placeholder yellow square, make or route the fix so the marker is small, under each soldier/footprint, and not placeholder-looking.
- If UI owns selection affordance/hit feedback, ensure selecting a soldier does not require clicking only foot pixels.
- If UI does not own these issues, write a short report naming the owning lane and evidence.
- Do not broaden to M02, vehicles, build UI, or unrelated HUD redesign.

## Validation Required

- Inspect affected UI/world-overlay code and prefab ownership.
- If UI changes are made, validate the public M01 route and include capture paths.
- If no UI change is needed, report why and who owns the fix.

## Waiting On

Waiting on lane:
none

Owner of next action:
UI

Can UI continue fallback work? yes, only this ownership audit/fix.

## Completion Report

Write:

`Design/AgentReports/2026-05-08_ui_m01-marker-selection-overlay-audit.md`

Use the standard WarlineCapture handoff format.
