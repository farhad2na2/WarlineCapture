# Designer Current Task

Date: 2026-05-08
Status: active
Priority: P0 root README and Design index dedupe pass

## Assignment

Continue the project-facing documentation optimization pass. The Designer lane setup is accepted; the next task is to reduce duplication between the root `README.md` and `Design/README.md`.

Focus on documentation structure and clarity, not implementation. Do not edit gameplay/UI source, Unity prefabs, captures, or runtime assets.

## Required Work

- Review the root `README.md` and `Design/README.md`.
- Keep the root README as a concise project entry point: setup, active direction, key source-of-truth links, and contributor/agent entry points.
- Keep `Design/README.md` as the complete design index.
- Remove or shorten duplicated long inventories from the root README when `Design/README.md` already owns the complete list.
- Preserve useful top-level context already added to the root README: Saga/Operation/Quick Custom structure, 2D isometric tactical/strategic split, and Designer workflow link.
- Preserve active contracts and current lane ownership. Do not change product scope, M01 Gate 4 criteria, agent ownership, validation requirements, or current lane priorities unless PM explicitly asks.
- The root `README.md` contains uncommitted documentation-only changes. Work with those changes; do not revert them.

## Current Accepted Inputs

Read first:

- `README.md`
- `Design/README.md`
- `Design/AgentTasks/README.md`
- `Design/AgentTasks/pm_heartbeat.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/WarlineCapture_Designer_Role_And_Documentation_Workflow.md`
- `Design/WarlineCapture_Agent_Coordination_Workflow.md`
- `Design/AgentReports/2026-05-08_designer_docs-readme-optimization.md`
- `Design/AgentReports/2026-05-08_pm_designer-docs-readme-optimization-review.md`

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

`Design/AgentReports/2026-05-08_designer_readme-design-index-dedupe.md`

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
