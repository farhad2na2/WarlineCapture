# PM Feedback: Restore Per-Soldier Selected Markers

Date: 2026-05-14
Lane: Art/Atlas
Topic: M01 selected marker fix before Gameplay audit

## User Decision

The user approves the new sample quality direction.

## Required Fix

The selected marker blue circle under each soldier is missing in the new `M01-02_SquadSelected` mockup. Art/Atlas must restore it before Gameplay audits or implementation approval continues.

Required correction:

- `M01-02_SquadSelected_1920x1080.png` must show a blue/cyan selected marker circle under each selected soldier.
- Each circle must align to the soldier feet on the isometric ground plane.
- Do not replace the per-soldier circles with only a group highlight, selected card, or HUD state.
- `LayerPack/Frames/M01-02_SquadSelected_layers.json` must include per-soldier selected marker layers with source asset, rects/anchors, pivots, z-order, alpha rule, and visible state.
- `LayerPack/manifest.json`, `AssetPrep_M01_Sample.json`, and `SourceNotes.md` must stay consistent with the corrected marker treatment.

## Routing

Current owner:
Art/Atlas

Held lanes:
Gameplay and QA/HCI

Gameplay second audit resumes only after Art/Atlas fixes the selected markers.
