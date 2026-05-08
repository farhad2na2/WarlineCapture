# Support/FTUE Current Task

Date: 2026-05-08
Status: waiting
Priority: no current Support/FTUE action; final visual approval paused for Gameplay Visual Target package

## Assignment

Stand by while the gameplay visual target package is created.

Read:

- `Design/AgentReports/2026-05-08_pm_gameplay-visual-target-lane-routing.md`
- `Design/AgentTasks/visual-target_current.md`

Support/FTUE re-engages only if a later QA/HCI pass reports a concrete assistant guidance, API, ownership, `Stop`, `Show Me`, result-explanation, invalid-command recovery, tutorial prompt, or FTUE behavior issue.

## Waiting On

- `Design/AgentReports/2026-05-08_visual-target_m01-selected-readability-package.md`

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
