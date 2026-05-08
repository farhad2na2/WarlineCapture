# Support/FTUE Current Task

Date: 2026-05-08
Status: waiting
Priority: no current Support/FTUE action; selected-readability gate waiting on PM/user review decision

## Assignment

Stand by while the selected-readability rejection gate is resolved.

Read:

- `Design/AgentReports/2026-05-08_pm_selected-readability-rejected-process-failure.md`
- `Design/AgentTasks/user_feedback_review_gate.md`

Support/FTUE re-engages only if a later QA/HCI pass reports a concrete assistant guidance, API, ownership, `Stop`, `Show Me`, result-explanation, invalid-command recovery, tutorial prompt, or FTUE behavior issue.

## Current Blockers Owned By Other Lanes

Gameplay, Art/Atlas, UI, Designer, and QA/HCI have delivered their current inputs and are waiting on PM/user review.

QA/HCI delivered:

- `Design/AgentReports/2026-05-08_qa-hci_user-feedback-regression-gate.md`

## Waiting On

- PM/user review decision on selected-readability pass

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
