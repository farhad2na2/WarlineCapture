# PM Finding: PREFAB-04 Assistant Button Target Lock Gap

Date: 2026-05-07

## Decision

Needs UI/art fix.

## Finding

`Design/VisualLock/PREFAB-04_AssistantButton/PREFAB-04_AssistantButton_Landscape_Target.png` is currently a clean state-board reference. It is not the requested AAA-quality target-lock mockup on a blurred WarlineCapture gameplay/HUD background.

## Why It Matters

The temporary in-HUD ARIA button is accepted as a functional mount, but `PREFAB-04_AssistantButton` still lacks a high-quality visual target. If agents treat the current state board as a visual lock, the production prefab can drift toward a flat deterministic placeholder rather than the intended premium assistant affordance.

## Task Update

`Design/AgentTasks/ui_current.md` now assigns UI to replace the `PREFAB-04` target image with a real target-lock mockup and to keep the art-register rows unapproved until PM/user review.

## Cross-Lane Notice

- UI owns the target-lock rework.
- Support/FTUE can continue runtime recommendation wiring without waiting for this visual target.
- QA/HCI should treat `PREFAB-04` visual quality as not approved until the new target is reviewed.
