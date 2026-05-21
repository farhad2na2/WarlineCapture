# PM Art/Atlas Current Work Rejected - Imagegen Redo Required

Date: 2026-05-16
Owner: Art/Atlas
Status: rejected, redo required
Priority: P0

## Decision

Current Art/Atlas work is rejected.

Rejected handoff:

- `Design/AgentReports/2026-05-16_art-atlas_aaa-readiness-visual-lock-revisions.md`

## Reason

The delivered work appears to be deterministic/generated layered output rather than imagegen-sourced AAA target-lock visual art.

This violates the new Art/Atlas heartbeat rule:

- target-lock bitmap mockups, VisualLockLayered reference images, contact sheets, and flattened review PNGs must use imagegen
- deterministic local rendering, scripted compositing, manual overlays, pixel patching, HTML/CSS screenshots, programmatic HUD assembly, and placeholder renders are not acceptable as final target-lock visuals

## Required Correction

Art/Atlas must redo the routed packages:

- `Design/VisualLockLayered/POP-05_MissionResult/`
- `Design/VisualLockLayered/SCN-02_MainMenu/`

Requirements:

- remove or replace rejected deterministic visual outputs from the routed packages
- generate target-lock reference images with imagegen
- after imagegen result selection, rebuild layer manifests/source notes/contact sheets around the imagegen visual
- do not continue from deterministic reference PNGs, generated atlases, or local assembled layers as the visual source
- if imagegen is unavailable, stop and write a blocker report

## Required New Report

Art/Atlas must write:

- `Design/AgentReports/2026-05-16_art-atlas_aaa-readiness-visual-lock-revisions-imagegen-redo.md`

The report must explicitly confirm that the target-lock reference images and flattened review PNGs were generated with imagegen.

## Routing

Current owner remains Art/Atlas.

Held:
Gameplay, QA/HCI, UI validation, Support/FTUE, Designer review, and all non-routed Art packages.
