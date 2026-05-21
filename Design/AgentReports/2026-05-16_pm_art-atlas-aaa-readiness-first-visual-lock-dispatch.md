# PM Art/Atlas AAA Readiness First Visual Lock Dispatch

Date: 2026-05-16
Owner: Art/Atlas
Status: dispatched
Priority: P0

## Decision

Designer/Game Design completed:

- `Design/AgentReports/2026-05-16_designer_aaa-readiness-recommendation-validation.md`

PM accepts the first recommended dispatch subset only:

- `Design/VisualLockLayered/POP-05_MissionResult/`
- `Design/VisualLockLayered/SCN-02_MainMenu/`

The following remain held:

- `POP-11_CommanderIdentity`
- `POP-10_AssistantTakeover`
- `SCN-11_OperationDashboard`
- `SCN-12_DistrictDetailActions`
- `POP-06_EndOfDayReport`
- M01 Gameplay continuation

## Art/Atlas Required Output

Art/Atlas must revise/create the routed VisualLockLayered packages and write:

- `Design/AgentReports/2026-05-16_art-atlas_aaa-readiness-visual-lock-revisions.md`

## POP-05 Mission Result

Revise:

- `Design/VisualLockLayered/POP-05_MissionResult/`

Use M01/current Chapter 1 canonical result content:

- `saga.ch01.m01.first_contact`
- `scenario.ch01.m01.first_contact`
- `level.ch01.district_edge_01`
- `iso.ch01.district_edge_01`
- objective: `Destroy hostile patrol`
- star rows
- canonical reward names only
- visible civilian/district consequence row
- replay/continue button states

Remove stale terms:

- `Downtown Breakthrough`
- `Supply Crate`
- `Unlock Fragments`

## SCN-02 Main Menu

Create or revise:

- `Design/VisualLockLayered/SCN-02_MainMenu/`

Required content:

- Credits
- Materials
- Command Authority
- Saga Campaign
- Persistent Operation
- Quick Custom Game
- Persistent Operation copy frames district/city operation pressure
- visible designed-unavailable states for Inbox, Store, Events, Ranking, and Command Feed if not live

## Guardrails

- Do not edit runtime code.
- Do not work outside the two routed packages.
- Do not start POP-11, POP-10, Operation screens, or M01 Gameplay art.
- Use imagegen for all target-lock bitmap mockups, VisualLockLayered reference images, contact sheets, and flattened review PNGs.
- Do not use deterministic local rendering, scripted compositing, manual shape overlays, pixel patching, HTML/CSS screenshots, programmatic HUD assembly, or placeholder renders as final target-lock visuals.
- Deterministic tooling is allowed only for metadata, slicing specs, layer manifests, file inspection, and validation after an imagegen visual is selected.
- If imagegen is unavailable, Art/Atlas must stop and write a blocker report instead of substituting deterministic output.
- Keep target-lock packages layered, not flattened-only.
- Preserve live-text/TMP and dynamic-data ownership in layer metadata.

## Routing

Current owner:
Art/Atlas

Held:
Gameplay, QA/HCI, UI, Support/FTUE, Designer, and all non-routed Art packages.
