# PM Feedback: Enemy Infantry Scale Must Match Isometric Projection

Date: 2026-05-14
Lane: Art/Atlas
Topic: M01 mockup unit scale

## User Feedback

The current mockup makes enemies look smaller than player units. In a true isometric mockup, enemies on the same ground plane should not shrink just because they are farther away in the composition.

## Required Correction

Art/Atlas must normalize player and enemy infantry scale in the corrected M01-01/M01-02 sample:

- player and enemy infantry must share the same isometric projection scale on the same ground plane
- scale may differ only if there is a documented unit-class scale rule, not because of camera/zoom/distance/composition drift
- sprite sheet frame keys, pivots, feet anchors, formation offsets, and contact shadows must prove consistent scale
- LayerPack manifests must include player/enemy infantry scale comparison notes

## Routing

Add this feedback to the existing Designer and Gameplay feedback already routed in:

`Design/AgentReports/2026-05-14_pm_art-atlas-combined-design-gameplay-feedback.md`

Gameplay and QA/HCI remain blocked until the corrected sample is approved.
