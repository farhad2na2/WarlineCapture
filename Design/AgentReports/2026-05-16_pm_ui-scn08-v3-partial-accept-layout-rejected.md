# PM UI SCN-08 v3 Partial Accept; Layout Rejected

Date: 2026-05-16
Owner: PM
Status: partial accept; full visual-quality rejection
Priority: P0

## Reviewed Handoff

- `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation-v3.md`
- `Design/AgentReports/Captures/M01-01_SCN08_NoSelection_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_1920x1080.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png`

## Decision

Accept v3 only for fixing the previous rejected M01 no-selection evidence state.

Reject v3 as final Match HUD completion because the runtime/editor HUD does not match the target mockup's visual quality, cleanliness, density, chrome, spacing, or bottom HUD composition.

## Accepted

- Objective text is now `Destroy hostile patrol`.
- ARIA/assistant is closed.
- Build is hidden/unavailable for M01.
- No selected squad/status panel is visible.
- No active command banner or command target UI markers are visible.
- Runtime capture succeeded and can be used as evidence for the corrected state.

## Rejected / Required UI Fix

The UI pass is not a 100% target-quality match. The following are still rejected:

- overall HUD visual quality, cleanliness, density, and polish do not match the SCN-08 target mockup
- chrome is too crude/blocky versus the target: bevels, frame thickness, corner detail, cyan trim, dark glass depth, shadows, and icon treatment need target-quality implementation
- objective panel, threat feed, squad cards, command rail, and minimap do not yet match the target's spacing, typography hierarchy, visual density, and AAA finish
- squad cards use mismatched/low-quality card art and incomplete card layout compared with the target
- minimap style/density does not match the target
- command rail/button treatment does not match the target's clean button set
- bottom command bar overlaps/clips into the minimap at 1920x1080
- Attack and later command buttons are partially hidden, failing command readability

UI must deliver:

- `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation-v4.md`

Required evidence:

- fresh 1920x1080 no-selection capture
- full HUD surface upgraded toward 100% SCN-08 target quality and cleanliness
- command rail fully readable and not overlapped by minimap
- squad cards, command rail, and minimap visually separated
- direct region-by-region target-vs-runtime/prefab checklist for objective panel, top resource bar, pause/settings, threat feed, squad cards, command rail/buttons, minimap, chrome, trim, shadows, transparency, typography, spacing, and visual density
- validation commands/log paths
- exact owner and missing asset/data for any remaining mismatch that UI cannot fix

## Routing

Current owner:
UI

Held:
`POP-05_MissionResult`, `SCN-02_MainMenu`, Gameplay, QA/HCI, Support/FTUE, Art/Atlas, and Designer until the v4 Match HUD layout fix is delivered and PM/user accepts or explicitly releases UI.
