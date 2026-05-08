# Designer Current Task

Date: 2026-05-08
Status: active
Priority: P0 design-doc and README clarity pass

## Assignment

Optimize the project-facing design documentation so agents and humans can quickly understand the current product goal, source-of-truth order, and active M01/Gate 4 state.

Focus on documentation structure and clarity, not implementation. Do not edit gameplay/UI source, Unity prefabs, captures, or runtime assets.

## Required Work

- Review the root `README.md`, `Design/README.md`, `Design/AgentTasks/README.md`, and `Design/WarlineCapture_Agent_Coordination_Workflow.md`.
- Identify duplicated, stale, confusing, or overly long sections that make agents misread the current project direction.
- Propose and, where safe, make concise documentation improvements:
  - clearer reading order
  - shorter project summary
  - current source-of-truth map
  - designer-facing guidance for what should be optimized versus left alone
  - less duplication between root README and `Design/README.md`
- Preserve active contracts and current lane ownership. Do not change product scope, M01 Gate 4 criteria, agent ownership, validation requirements, or current lane priorities unless PM explicitly asks.
- If the root `README.md` contains uncommitted changes, review them first and avoid overwriting unrelated work. Report whether the changes are safe to keep, need cleanup, or should be split into a PM commit later.

## Current Accepted Inputs

Read first:

- `README.md`
- `Design/README.md`
- `Design/AgentTasks/README.md`
- `Design/AgentTasks/pm_heartbeat.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/WarlineCapture_Designer_Role_And_Documentation_Workflow.md`
- `Design/WarlineCapture_Agent_Coordination_Workflow.md`
- latest PM reports under `Design/AgentReports/`

## Validation Required

- Run a focused markdown/link sanity check where practical.
- Confirm edited docs still mention the correct lane files and heartbeat pattern.
- Confirm no source/runtime files were modified.
- Confirm no agent commit/push instruction contradicts PM-only commit gate.

## Cross-Lane Notes

- PM owns final acceptance and commit/push.
- Designer may propose docs cleanup that affects all lanes, but PM decides whether to route it immediately or defer it.
- Designer must not modify `Design/AgentTasks/*_current.md` except `designer_current.md` unless PM explicitly assigns a routing update.

## Completion Report

Write the report to:

`Design/AgentReports/2026-05-08_designer_docs-readme-optimization.md`

Use the standard WarlineCapture handoff format and include:

- Lane
- Task
- Files changed
- Contracts touched
- User-visible behavior
- Validation run
- Validation result
- Known gaps
- Cross-lane impacts
- Next recommended task
