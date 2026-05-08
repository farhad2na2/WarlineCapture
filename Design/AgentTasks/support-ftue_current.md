# Support/FTUE Current Task

Date: 2026-05-08
Status: waiting
Priority: no current Support/FTUE action; waiting for PM/user selected-readability decision

## Assignment

Stand by while PM/user reviews the selected-readability pass.

Do not repeat accepted recommendation service, `CommandIntentExecutor`, live `AssistantContextProvider`, UI runtime-binding, public-launch routing, or opening-control work.

Support/FTUE re-engages only if a later QA/HCI pass reports a concrete assistant guidance, API, ownership, `Stop`, `Show Me`, result-explanation, invalid-command recovery, or FTUE behavior issue.

## Current Blockers Owned By Other Lanes

UI owns:

- M01 HUD showing APC, Tank, air support, Build, vehicle production, transport, or base/build affordances in an infantry-only tutorial.

Gameplay now owns rejected-runtime fixes:

- remove SpriteRenderer-era unit presentation/naming from the public M01 unit path
- consume automated scale rules
- replace huge/unclear selection markers
- calibrate realistic infantry movement speed
- prove run animation while moving

Art/Atlas now owns rejected-art fixes:

- metric scale/readability package for infantry and visible M01 buildings/decor
- selected-state art treatment source
- confirmation that run frames and destroyed/death atlas states are covered or blocked

PM/user already rejected:

- temporary Gate 4 art/runtime review

QA/HCI owns:

- no current Support/FTUE issue; QA/HCI rerun found no concrete assistant regression.

## Waiting On

- `Design/AgentReports/2026-05-08_pm_selected-readability-rerun-user-review.md`
- PM/user decision: approve or reject selected-readability pass

## Guardrails While Waiting

- Keep `CommandIntentExecutor` as the command boundary.
- Keep `Show Me` as focus/highlight intent only.
- Keep `Stop` bounded to assistant/takeover state.
- Do not use UI child paths, screen coordinates, HUD text scraping, or runtime scene searches.
- Do not invent new Chapter 1 mechanics.

## Completion Report

If QA/HCI or PM assigns a concrete Support/FTUE issue, write the next report to:

`Design/AgentReports/2026-05-08_support-ftue_<specific-qa-rerun-issue>.md`

Use the exact format from `Design/WarlineCapture_Agent_Coordination_Workflow.md`.
