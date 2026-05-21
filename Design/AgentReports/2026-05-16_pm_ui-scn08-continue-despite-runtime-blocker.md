# PM UI SCN-08 Continue Despite Runtime Blocker

Date: 2026-05-16
Owner: UI
Status: active
Priority: P0

## Decision

UI must continue. The previous UI handoff is accepted only as partial scoped evidence, not as completion of the Match HUD target alignment.

The runtime capture/Gameplay blocker prevents final runtime visual acceptance, but it does not block UI from finishing UI-owned Match HUD work against:

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json`

## Required UI Action

Continue implementation/alignment for UI-owned regions:

- objective panel and Star Goals
- top resource bar
- pause/settings controls
- log/threat feed
- squad cards
- command bar and M01 command states
- minimap frame and viewport
- dark glass/metal depth, cyan trim, bevels, typography, transparent-corner behavior

If a mismatch is not UI-owned, document the exact owner and blocker. Do not idle.

## Required Output

UI must write:

- `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation-v2.md`

The report must include:

- files changed
- target used
- target-vs-runtime or target-vs-prefab/editor checklist
- validation commands/tests
- remaining mismatches classified as UI-owned, Gameplay-owned, Art-owned, or blocked by runtime capture
- explicit blocker details if runtime capture is still unavailable

## Routing

Current owner:
UI

Held:
QA/HCI validation until PM/user accepts UI evidence or runtime proof becomes available.
