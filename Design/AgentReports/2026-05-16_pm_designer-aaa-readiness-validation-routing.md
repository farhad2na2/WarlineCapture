# PM Designer AAA Readiness Validation Routing

Date: 2026-05-16
Owner: Designer / Game Design
Status: dispatched
Priority: P0

## Decision

Pause the current M01 Gameplay implementation iteration here. The latest Gameplay delivery exists, but PM/user wants to stop before continuing because remaining work includes Art/background target-lock work.

New active priority is Designer/Game Design validation of:

- `Design/AgentReports/2026-05-10_pm_aaa-readiness-recommendation-approval.md`

That report is recommendation-only. It must be validated before it changes lane tasks or sends Art/Atlas into revised target-lock layered mockups.

## Designer Assignment

Designer/Game Design must write:

- `Design/AgentReports/2026-05-16_designer_aaa-readiness-recommendation-validation.md`

The report must decide whether each recommendation is valid, partially valid, invalid/stale, or deferred.

For every valid or partially valid recommendation that affects visuals, Designer must create Art-ready specs for revised `Design/VisualLockLayered/` target-lock layered mockups.

## Required Scope

Designer must review the AAA readiness mock change matrix, including:

- `SCN-02` Main Menu
- `SCN-03` Commander Profile
- `SCN-05` Saga Map
- `SCN-06` Mission Briefing
- `SCN-07` Loadout
- `SCN-08` Battle HUD
- `SCN-09` Build Drawer
- `SCN-10` Command Wheel
- `SCN-11` Operation Dashboard
- `SCN-12` District Detail
- `POP-01` Threat Alert
- `POP-03` Build Placement
- `POP-05` Mission Result
- `POP-06` End Of Day
- `POP-10` Assistant Takeover
- `POP-11` Commander Identity

Designer may mark any item invalid/stale or deferred if current design contracts do not support the recommendation.

## Guardrails

- Designer must not create images.
- Designer must not edit runtime code.
- Designer must not dispatch Art/Atlas directly.
- Art/Atlas waits until PM/user approves the Designer validation/spec report.
- Gameplay remains paused until PM/user resumes it.

## Routing

Current owner:
Designer / Game Design

Held:
Gameplay, QA/HCI, Art/Atlas revised mockup production, UI, Support/FTUE.
