# Support/FTUE Current Task

Date: 2026-05-08
Status: waiting
Priority: no current Support/FTUE action; waiting on M01 rejection gate fixes from Gameplay, Art/Atlas, UI, Designer, and QA/HCI

## Assignment

Stand by while the selected-readability rejection gate is resolved.

Read:

- `Design/AgentReports/2026-05-08_pm_selected-readability-rejected-process-failure.md`
- `Design/AgentTasks/user_feedback_review_gate.md`

Support/FTUE re-engages only if a later QA/HCI pass reports a concrete assistant guidance, API, ownership, `Stop`, `Show Me`, result-explanation, invalid-command recovery, tutorial prompt, or FTUE behavior issue.

## Current Blockers Owned By Other Lanes

Gameplay owns:

- true ECS entity visual presentation for public M01 units/buildings, not `MeshRenderer`, `MeshFilter`, or `SpriteRenderer` GameObject wrappers,
- target marker sizing,
- animation integration,
- selection hit targeting,
- red artifact/enemy fix,
- scale/aspect application.

Art/Atlas owns:

- marker art,
- idle/run frame mapping,
- scale/aspect guidance,
- enemy/artifact visual guidance.

UI owns:

- marker/selection overlay ownership audit and fix if UI-owned.

Designer owns:

- rejection-informed visual scale/readability contract refresh.

QA/HCI owns:

- user-feedback regression gate and later focused validation.

## Waiting On

- `Design/AgentReports/2026-05-08_gameplay_m01-ecs-visual-marker-animation-reset.md`
- `Design/AgentReports/2026-05-08_art-atlas_m01-marker-animation-scale-package.md`
- `Design/AgentReports/2026-05-08_ui_m01-marker-selection-overlay-audit.md`
- `Design/AgentReports/2026-05-08_designer_m01-rejection-scale-marker-contract.md`
- `Design/AgentReports/2026-05-08_qa-hci_user-feedback-regression-gate.md`

## Guardrails While Waiting

- Keep `CommandIntentExecutor` as the command boundary.
- Keep `Show Me` as focus/highlight intent only.
- Keep `Stop` bounded to assistant/takeover state.
- Do not use UI child paths, screen coordinates, HUD text scraping, or runtime scene searches.
- Do not invent new Chapter 1 mechanics.

## Completion Report

If QA/HCI or PM assigns a concrete Support/FTUE issue, write the next report to:

`Design/AgentReports/2026-05-08_support-ftue_<specific-qa-rerun-issue>.md`

Use the standard WarlineCapture handoff format.
