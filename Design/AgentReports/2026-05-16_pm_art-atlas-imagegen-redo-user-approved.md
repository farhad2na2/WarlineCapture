# PM Art/Atlas Imagegen Redo User Approved

Date: 2026-05-16
Owner: PM
Status: user visually approved
Priority: P0

## Decision

The user visually approved the imagegen-redone Art/Atlas targets for the first AAA readiness VisualLockLayered subset:

- `Design/VisualLockLayered/POP-05_MissionResult/`
- `Design/VisualLockLayered/SCN-02_MainMenu/`

This closes the Art/Atlas visual approval gate for this routed subset.

## Approved Inputs

- `Design/AgentReports/2026-05-16_art-atlas_aaa-readiness-visual-lock-revisions-imagegen-redo.md`
- `Design/AgentReports/2026-05-16_pm_art-atlas-imagegen-redo-accepted-for-review.md`
- `Design/VisualLockLayered/POP-05_MissionResult/generated_one_go/source/imagegen_selected_reference.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_selected_reference_16x9.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_selected_reference_20x9.png`
- `Design/VisualLockLayered/POP-05_MissionResult/layer_manifest.json`
- `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json`

## Implementation Routing

Next implementation owner:
UI, after current Match HUD task is finished

Routing note:
The approved POP-05 and SCN-02 packages are queued as later UI implementation inputs. UI must first finish the current active SCN-08/M01 Match HUD correction and deliver `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation-v3.md`. After PM/user accepts that Match HUD handoff or explicitly releases UI to the next slice, UI should implement the approved Main Menu and Mission Result targets with Unity prefab/layout/TMP/live-data binding and visual evidence against the approved layered packages.

## Guardrails

- Do not route Art/Atlas for more work on these approved targets unless PM/user rejects implementation or requests a visual change.
- Do not bake imagegen text into runtime UI where live TMP/data binding is required.
- Do not start held targets: `POP-11`, `POP-10`, `SCN-11`, `SCN-12`, or `POP-06`.
- QA/HCI remains held until PM/user accepts a runtime/UI implementation handoff.
