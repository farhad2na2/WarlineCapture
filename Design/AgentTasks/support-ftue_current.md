# Support/FTUE Current Task

Date: 2026-05-08
Status: waiting
Priority: no current Support/FTUE action; waiting on rejected Art/Atlas visual target replacement

## Assignment

Stand by while Art/Atlas replaces the rejected gameplay visual target package.

Read:

- `Design/AgentReports/2026-05-08_pm_art-atlas-gameplay-visual-target-rejected.md`
- `Design/AgentReports/2026-05-08_art-atlas_m01-aaa-gameplay-visual-target-package.md`

Support/FTUE re-engages only if a later QA/HCI pass reports a concrete assistant guidance, API, ownership, `Stop`, `Show Me`, result-explanation, invalid-command recovery, tutorial prompt, or FTUE behavior issue.

## Waiting On

- PM/user approval of replacement gameplay visual target package

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
