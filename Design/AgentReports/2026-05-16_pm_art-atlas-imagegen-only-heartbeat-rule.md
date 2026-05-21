# PM Art/Atlas Imagegen-Only Heartbeat Rule

Date: 2026-05-16
Owner: Art/Atlas
Status: rule added
Priority: P0

## Decision

Art/Atlas must never present deterministic local renders, scripted composites, patched overlays, HTML/CSS screenshots, programmatic HUD assembly, or placeholder renders as final target-lock visuals.

For target-lock bitmap mockups, VisualLockLayered reference images, contact sheets, and flattened review PNGs, Art/Atlas must use imagegen.

## Allowed Deterministic Work

Deterministic tools may be used only after an imagegen visual is selected, for:

- layer manifests
- slicing specs
- source notes
- file inspection
- dimensions/metadata checks
- validation/contact packaging

## Blocker Rule

If imagegen is unavailable for a routed visual target task, Art/Atlas must stop and write a blocker report. It must not substitute deterministic output.

## Files Updated

- `Design/AgentTasks/art-atlas_heartbeat.md`
- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/art-atlas_pm_message.md`
- `Design/AgentReports/2026-05-16_pm_art-atlas-aaa-readiness-first-visual-lock-dispatch.md`
