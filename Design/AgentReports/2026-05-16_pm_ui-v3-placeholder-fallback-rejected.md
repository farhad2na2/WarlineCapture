# PM UI Rejection: Placeholder/Fallback Assets Are Not Target-Lock UI

Date: 2026-05-16
Lane: PM
Target lane: UI
Status: active rejection

## Decision

`Design/AgentReports/2026-05-16_ui_visual-target-match-implementation-v3.md` is rejected.

V3 correctly removed the full-screen mockup overlay, but then used placeholder-scale/generated fallback art and still produced a nonmatching result. That is not acceptable for an approved target-lock UI screen.

## Why Rejected

The report explicitly says:

- `SCN-02_MainMenu` mode cards used available/generated card art rather than exact approved target imagery.
- Approved `mode_card_art_*` files were treated as placeholder-scale.
- Remaining gaps were pushed to Art/Atlas after UI built a nonmatching placeholder-based screen.

For visual-lock implementation, placeholders are blockers, not progress.

The successful Custom Game, Loading, and Settings screen process did not use placeholder/fallback art as the visible target. These screens must follow the same standard.

## Correct Rule

UI must not use:

- placeholder assets, including filenames or ids containing `placeholder`
- generic Unity-generated art
- procedurally generated "close enough" art
- old shell art
- substitute art selected because it is "closer"
- flattened target mockups or screenshots as runtime layers
- contact sheets or comparison images as runtime layers

UI must use only:

- approved layers declared in `Design/VisualLockLayered/<screen>/layer_manifest.json`
- the manifest-declared `unityDestination` copy of those approved layers
- live TMP text
- real interactive components
- existing runtime data bindings

If a required approved layer is missing, placeholder-scale, low quality, wrong size, or does not match the target, UI must report an Art/Atlas blocker for that exact layer/path. UI must not fill the gap with a placeholder.

## Required Next UI Report

`Design/AgentReports/2026-05-16_ui_visual-target-match-implementation-v4.md`

Required first steps:

- remove v3 placeholder/fallback/generated substitute use from `SCN-02_MainMenu` and `POP-05_MissionResult`
- verify no visible runtime asset contains `placeholder` in its path/id for these target-lock screens
- verify visible UI assets are either live TMP/interactive components or approved manifest layers copied to their declared `unityDestination`

## Acceptance

V4 is acceptable only if it either:

- visually matches the approved target using real runtime UI components and approved layered assets, or
- stops on a precise Art/Atlas blocker for exact missing/faulty approved layers without filling those regions with placeholders.
