# PM Dispatch: M01 Critical Path Speed Control

Date: 2026-05-07

## Trigger

The user asked how to finish faster and agreed to focus the workflow around the recommended approach.

## Change

Added `Design/AgentTasks/M01_CRITICAL_PATH.md` as the shared gate file agents should read before continuing lane work.

Updated:

- `Design/AgentTasks/README.md`
- `Design/AgentTasks/AUTO_CONTINUE.md`
- `Design/WarlineCapture_Agent_Coordination_Workflow.md`

## Required Direction

All lanes should prioritize M01 First Contact until the critical path is ready to expand.

Do not start M02-M05 implementation, broad polish, or optional legacy systems while these M01 gates are open:

- Gameplay stability, fixed roads, day/night isolation, legacy render audit, sprite-atlas migration plan.
- UI PREFAB-04 WarlineCapture-aligned AAA target lock and assistant surface validation.
- Support/FTUE typed assistant `Do It` wiring.
- QA/HCI M01 smoke, readability, and performance pass.

## Why This Speeds Up The Project

This reduces rework by making M01 the repeatable production pipeline. Later missions should copy a proven path instead of creating new patterns while M01 is still unstable.
