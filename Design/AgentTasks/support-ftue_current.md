# Support/FTUE Current Task

Date: 2026-05-08
Status: waiting
Priority: P1 Gate 4 route-capture assistant watch

## Assignment

Stand by for the Gate 4 route-driven capture/safe-area sequence. Do not repeat the accepted recommendation service, `CommandIntentExecutor`, live `AssistantContextProvider`, or UI runtime-binding work. Support/FTUE re-engages only if the next QA/HCI rerun reports a concrete assistant guidance, API, ownership, `Stop`, `Show Me`, or result-explanation behavior issue. Do not modify production code unless PM assigns a concrete Support/FTUE blocker.

## Context

Read first:

- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/WarlineCapture_AssistantPanel_M01_Implementation_Contract.md`
- `Design/WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md`
- `Design/AgentReports/2026-05-07_support-ftue_assistant-service-slice.md`
- `Design/AgentReports/2026-05-07_pm_support-assistant-service-slice-review.md`
- `Design/AgentReports/2026-05-07_gameplay_assistant-typed-command-hooks.md`
- `Design/AgentReports/2026-05-07_pm_gameplay-typed-command-hooks-review.md`
- `Design/AgentReports/2026-05-07_gameplay_m01-log-performance-fixed-roads.md`
- `Design/AgentReports/2026-05-07_pm_gameplay-m01-log-performance-fixed-roads-review.md`
- `Design/AgentReports/2026-05-07_support-ftue_command-intent-executor.md`
- `Design/AgentReports/2026-05-07_pm_support-ftue_live-assistant-context-provider-review.md`
- `Design/AgentReports/2026-05-07_ui_prefab04-assistant-button-production-fix.md`
- `Design/AgentReports/2026-05-07_pm_ui-prefab04-assistant-button-production-fix-review.md`
- `Design/AgentReports/2026-05-07_ui_assistant-runtime-binding-fix.md`
- `Design/AgentReports/2026-05-07_pm_ui-assistant-runtime-binding-fix-review.md`
- `Design/AgentReports/2026-05-07_ui_m01-integrated-capture-matrix.md`
- `Design/AgentReports/2026-05-07_pm_ui-m01-integrated-capture-matrix-review.md`
- `Design/AgentReports/2026-05-07_qa-hci_m01-gate4-integrated-readiness.md`
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-pass.md`
- `Design/AgentReports/2026-05-08_pm_qa-hci-m01-player-route-safe-area-pass-review.md`
- `Design/AgentReports/2026-05-08_pm_support-ftue-route-capture-watch-review.md`
- `Design/AgentReports/2026-05-08_pm_design-audit-stale-support-current-task.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`

The recommendation service slice, `CommandIntentExecutor` boundary, live `AssistantContextProvider`, and UI assistant runtime binding are accepted. Gameplay typed hooks are accepted for `TrySelectRuntimeEntity`, `TryIssueMoveToAnchor`, and `TryIssueAttackTarget`. M01 fixed-road/log/performance guardrails are accepted. The accepted UI capture matrix and QA/HCI player-route automation do not identify a Support/FTUE API gap. The current Gate 4 blocker is UI route-driven capture/safe-area tooling followed by a QA/HCI rerun.

## Required Work

- Wait for UI to deliver `Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md`.
- Wait for QA/HCI to rerun `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md` after the UI handoff.
- If the QA/HCI rerun reports misleading ARIA recommendation, assistant ownership, `Stop`, `Show Me`, result explanation behavior, or a missing Support/FTUE API, implement only that concrete Support/FTUE contract with focused tests.
- If UI reports a missing Support/FTUE API or ambiguous assistant contract while building route-capture tooling, implement only that contract with focused tests.
- Keep `CommandIntentExecutor` as the command boundary; do not move gameplay execution into UI or Support/FTUE presentation code.
- Keep `Show Me` as focus/highlight intent only; do not execute gameplay commands through `Show Me`.
- Keep `Stop` bounded to assistant/takeover state. Do not implement full autopilot.
- Do not use UI child paths, screen coordinates, or HUD text scraping.
- Do not add runtime scene searches (`FindObjectOfType`, `FindObjectsOfType`, `FindFirstObjectByType`, `FindAnyObjectByType`, `GameObject.Find`, `Transform.Find` path traversal, `GetComponentInChildren` discovery, or name/tag lookup). Use typed assistant context, command executor, service, or mission/session APIs.
- Do not invent new Chapter 1 mechanics.
- Do not start AAA FTUE screen mockup revisions in this pass. Those should wait until Gate 4 route-driven capture/safe-area evidence is accepted or PM assigns a new design task.

## Validation Required

- If no Support/FTUE code changes are needed, no validation run is required; report blocked/waiting status only if asked.
- If a missing API or contract ambiguity is fixed, add or update focused assistant runtime tests.
- If QA/HCI reports a Support/FTUE route behavior failure, rerun the relevant assistant runtime tests and only the focused Unity validation needed for the touched Support/FTUE contract.
- If PM assigns Unity validation for a Support/FTUE issue and Unity batchmode hits `LicenseClient-farhad` reconnect/time-out loops before tests start, rerun the same required command with Codex escalation/out-of-sandbox execution in the PM-assigned workspace. QA/HCI confirmed this resolves the sandbox licensing issue. Do not borrow another lane workspace to work around licensing.
- Confirm Support/FTUE still does not directly depend on UI button hierarchy for gameplay command execution.
- Validate no new scene-search warnings or banned runtime lookup calls were introduced in touched Support/FTUE runtime files.
- Waiting on lane: Gameplay and QA/HCI
- Waiting on exact file/report/asset/command: revised Gameplay proof/fix for ECS world-source ownership of visible non-Canvas M01 world objects, then `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md` after QA/HCI reruns affected Gate 4 checks.
- Owner of next action: Gameplay owns ECS world-source proof/fix. QA/HCI owns the affected rerun after the Gameplay/PM blocker closes. Support/FTUE re-engages only for concrete assistant guidance/API/takeover/Stop/Show Me/result-explanation findings.
- Can my lane still continue fallback work? no.

## Completion Report

Write a report to:

`Design/AgentReports/2026-05-08_support-ftue_<specific-qa-rerun-issue>.md`

Use a new specific report filename for any future Support/FTUE review or fix from the QA/HCI rerun. Do not reuse the stale-task cleanup report. Use the exact format from `Design/WarlineCapture_Agent_Coordination_Workflow.md`.
