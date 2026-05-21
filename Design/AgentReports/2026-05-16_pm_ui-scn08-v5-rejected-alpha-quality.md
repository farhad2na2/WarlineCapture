# PM UI SCN-08 v5 Rejected For Alpha Quality

Date: 2026-05-16
Owner: PM
Status: rejected; Art/Atlas fix routed
Priority: P0

## Reviewed Handoff

- `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation-v5.md`
- `Design/AgentReports/Captures/M01-01_SCN08_NoSelection_v5_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v5_1920x1080.png`

## Decision

Reject UI v5 as final Match HUD completion.

Accepted:

- M01 command order is now correct: `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`.
- `SPECIAL` is not shown for M01.
- M01 no-selection state remains correct.
- Editor and runtime captures exist.

Rejected:

- The HUD has visible green chroma-key contamination/edge spill around multiple imported SCN-08 slices.
- Objective panel, threat feed, squad cards, command rail, and minimap frames show green outlines that are not present in the target mockup.
- Squad card/card art quality and text/card composition still do not match the clean target quality.
- This is not acceptable as a 100% visual-quality match.

## Routing

Current owner:
Art/Atlas

Art/Atlas must deliver:

- `Design/AgentReports/2026-05-16_art-atlas_scn08-alpha-quality-fix.md`

Required correction:

- remove green chroma-key contamination/edge spill from SCN-08 layer slices
- replace any slice whose alpha extraction or chroma source is unsuitable for runtime UI
- keep accepted M01 command order and Select icon
- do not reintroduce `SPECIAL` for M01
- preserve clean SCN-08/M01 target quality

After Art/Atlas correction is accepted, UI must deliver:

- `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation-v6.md`

## Held

UI v6 continuation, POP-05/SCN-02 implementation, Gameplay, QA/HCI, Support/FTUE, Designer, and non-routed Art packages.
