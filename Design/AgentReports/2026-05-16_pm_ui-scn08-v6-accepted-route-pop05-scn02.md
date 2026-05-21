# PM UI SCN-08 v6 Accepted; POP-05 SCN-02 Routed

Date: 2026-05-16
Owner: PM
Status: accepted; next UI slice routed
Priority: P0

## Reviewed Handoff

- `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation-v6.md`
- `Design/AgentReports/Captures/M01-01_SCN08_NoSelection_v6_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v6_1920x1080.png`

## Decision

Accepted for the UI-owned SCN-08/M01 Match HUD slice.

## Accepted

- Green chroma-key artifacts are gone from integrated HUD captures.
- M01 command order is correct: `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`.
- `SPECIAL` is not used for M01.
- M01 no-selection state remains correct.
- Editor and runtime captures exist.
- Focused UI validation passed.

## Caveat

Runtime capture still includes Gameplay-owned battlefield/soldier/readability differences. Those are outside this UI-owned SCN-08 HUD acceptance.

## Next UI Routing

Current owner:
UI

UI must implement the previously approved VisualLockLayered targets:

- `Design/VisualLockLayered/POP-05_MissionResult/`
- `Design/VisualLockLayered/SCN-02_MainMenu/`

Required report:

- `Design/AgentReports/2026-05-16_ui_pop05-scn02-approved-target-implementation.md`

## Requirements

- Preserve live TMP/data ownership.
- Do not bake imagegen text into reusable runtime UI where manifest says live text/data.
- Use approved target imagery and layer manifests as visual/layout authority.
- POP-05 must use M01/current Chapter 1 result content.
- SCN-02 must use Credits, Materials, Command Authority, mode cards, designed-unavailable route states, and commander profile fallback.
- Provide captures/comparisons where available, validation commands/log paths, changed files, and remaining mismatches by owner.

## Held

QA/HCI, Gameplay continuation, Art/Atlas, Support/FTUE, Designer, and non-routed packages until PM/user accepts the POP-05/SCN-02 UI implementation handoff.
