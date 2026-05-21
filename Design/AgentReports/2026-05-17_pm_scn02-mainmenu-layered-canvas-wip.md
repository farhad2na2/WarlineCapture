# SCN-02 Main Menu Layered Canvas Work In Progress

Date: 2026-05-17
Owner: PM / direct implementation
Status: Work in progress, not accepted

## Current State

The runtime prefab has been corrected to stop using baked target-reference panel slices. The prefab now uses visible UI hierarchy built from SCN-02 layered assets and live TMP text.

Implemented in runtime:

- Top resource strip with live resource labels and counters.
- Left commander/nav panel with sliced frame assets and live labels.
- Three mode cards with sliced shell frame, icon, art layer, live title/subtitle/body, risk rows, and real route buttons.
- Deploy command button as a real button using sliced frame assets.
- Runtime prefab no longer contains `TargetRoot`, `TargetSlice`, or `target_slice` references.

## Proof

- Capture: `Design/AgentReports/Captures/SCN-02_MainMenu_LayeredCanvasWorkInProgress_1672x941.png`
- Comparison: `Design/AgentReports/Captures/SCN-02_MainMenu_LayeredCanvasWorkInProgress_vs_Target_Comparison.png`
- Current diff: `mse=624.59`

## Not Accepted Yet

This is not target-quality yet. Remaining issues:

- Visual depth, frame density, and background richness do not match the approved target.
- Top strip and deploy command still do not match the target chrome quality.
- Left nav row labels and unavailable badges need tighter layout.
- 20:9 authored layout still needs a proper pass after 16:9 is acceptable.

## Rule

Do not ship or claim visual acceptance until the runtime layered canvas, not baked reference slices, is visually accepted.
