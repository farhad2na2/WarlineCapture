# PM UI SCN-08 Battle HUD Target Implementation Dispatch

Date: 2026-05-16
Owner: UI
Status: dispatched
Priority: P0

## Decision

The current M01 battle HUD does not match the existing layered target. UI is now assigned to implement/alignment-correct the runtime Battle HUD against:

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json`

## Required Output

UI must write:

- `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation.md`

## Scope

Match the existing target for:

- objective panel and Star Goals
- top resource bar
- pause/settings controls
- log/threat feed
- squad cards
- command bar and M01 command states
- minimap frame and viewport
- dark glass/metal depth, cyan trim, bevels, typography, transparent-corner behavior

M01 state rules:

- objective text is `Destroy hostile patrol`
- Build unavailable or disabled with `MissionDoesNotAllowBuild`
- no selected squad panel/status in M01-01
- no selected rings or command markers in M01-01
- assistant/ARIA closed unless separately routed

## Guardrails

- This is UI implementation/alignment, not Art generation.
- Use the existing layered target as authority.
- Do not create a new art direction.
- Do not own gameplay policy in UI code.
- Do not bypass the normal M01 launch flow.

## Routing

Current owner:
UI

Held:
QA/HCI validation, Gameplay continuation, Support/FTUE, and non-routed Art packages.
