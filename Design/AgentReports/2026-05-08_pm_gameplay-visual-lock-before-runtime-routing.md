# PM Gameplay VisualLock Before Runtime Routing

## Lane

PM

## Task

Route the user's approval into a pre-runtime Gameplay VisualLock gate covering strategic map, tactical map, markers, and atlases.

## Files changed

- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentTasks/designer_current.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/support-ftue_current.md`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/README.md`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/README.md`
- `Design/AgentReports/2026-05-08_pm_gameplay-visual-lock-before-runtime-routing.md`

## Contracts touched

- `Design/AgentReports/2026-05-08_art-atlas_m01-aaa-isometric-gameplay-visual-target-package.md`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/README.md`
- `Design/AgentTasks/pm_heartbeat.md`

## User-visible behavior

No runtime behavior changed. Runtime implementation is intentionally paused until the approved M01 style is expanded into a Gameplay VisualLock package.

## Validation run

- Read active Art/Atlas, Gameplay, QA/HCI, Designer, UI, and Support/FTUE task files.
- Confirmed the approved true-isometric target package exists.
- Created a gameplay VisualLock folder boundary separate from UI VisualLock folders.
- Routed Art/Atlas as active owner.

## Validation result

Routed.

The approved visual target is now the source style, but it must be expanded into locked references for:

- strategic map,
- tactical map/background,
- markers,
- player/enemy atlases,
- animation/destroyed atlas states,
- scale/grounding rules.

## Known gaps

- The Gameplay VisualLock package itself is not created yet.
- Gameplay remains blocked until Art/Atlas delivers it and PM/user accepts it.

## Cross-lane impacts

- Art/Atlas is active.
- Gameplay is waiting.
- QA/HCI is waiting on Art/Atlas, then Gameplay.
- Designer, UI, and Support/FTUE are waiting unless a concrete issue is routed.

## Next recommended task

Art/Atlas should create the Gameplay VisualLock package and write:

`Design/AgentReports/2026-05-08_art-atlas_m01-gameplay-visual-lock-package.md`
