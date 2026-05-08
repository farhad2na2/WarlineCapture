# Support/FTUE Current Task

Date: 2026-05-08
Status: waiting
Priority: blocked until QA/HCI reports a concrete assistant/FTUE issue

## Assignment

Stand by while QA/HCI reruns focused Gate 4 after PM accepted Gameplay's manual M01 opening-control proof.

Do not repeat accepted recommendation service, `CommandIntentExecutor`, live `AssistantContextProvider`, UI runtime-binding, public-launch routing, or opening-control work.

Support/FTUE re-engages only if a later QA/HCI pass reports a concrete assistant guidance, API, ownership, `Stop`, `Show Me`, result-explanation, invalid-command recovery, or FTUE behavior issue.

## Current Blockers Owned By Other Lanes

UI owns:

- M01 HUD showing APC, Tank, air support, Build, vehicle production, transport, or base/build affordances in an infantry-only tutorial.

Gameplay has delivered:

- manual M01 opening-control proof so the player can wait, select, and move before hostile fire kills or critically damages the squad
- public camera-scale readability of four distinct soldiers under one squad identity
- selected-state clarity in the world
- projectile/impact visual scale assertions

Art/Atlas has delivered:

- temporary-art approval package
- player/enemy infantry atlas state coverage
- selected-state visual treatment source art
- destroyed/death atlas-state art

PM/user later owns:

- approve or reject the temporary M01 infantry art package only after Gameplay proves the review route is stable

QA/HCI owns:

- final Gate 4 rerun after UI and Gameplay/Art fixes land.

## Waiting On

- `Design/AgentReports/2026-05-08_ui_m01-infantry-only-hud-scope.md`
- `Design/AgentReports/2026-05-08_gameplay_m01-manual-opening-control-fix.md`
- `Design/AgentReports/2026-05-08_pm_gameplay-m01-manual-opening-control-review.md`
- `Design/AgentReports/2026-05-08_art-atlas_m01-infantry-atlas-readiness.md`
- `Design/AgentReports/2026-05-08_qa-hci_gate4-final-rerun.md`

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
