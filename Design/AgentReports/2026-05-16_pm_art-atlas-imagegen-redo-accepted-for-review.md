# PM Art/Atlas Imagegen Redo Accepted For Review

Date: 2026-05-16
Owner: PM
Status: accepted for PM/user visual review
Priority: P0

## Decision

Art/Atlas has delivered the required imagegen redo for the first AAA readiness VisualLockLayered subset:

- `Design/VisualLockLayered/POP-05_MissionResult/`
- `Design/VisualLockLayered/SCN-02_MainMenu/`

This is accepted as imagegen-compliant and ready for PM/user visual review. This is not runtime implementation approval.

## Accepted Handoff

- `Design/AgentReports/2026-05-16_art-atlas_aaa-readiness-visual-lock-revisions-imagegen-redo.md`

## Reviewed Outputs

- `Design/VisualLockLayered/POP-05_MissionResult/generated_one_go/source/imagegen_selected_reference.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_selected_reference_16x9.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_selected_reference_20x9.png`
- `Design/VisualLockLayered/POP-05_MissionResult/layer_manifest.json`
- `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json`

## Review Result

- Required imagegen-redo report is present.
- Report explicitly confirms imagegen was used for target-lock reference images, flattened review PNGs, and contact sheets.
- Report explicitly rejects deterministic local rendering, scripted compositing, manual overlays, HTML/CSS screenshots, pixel patching, and programmatic HUD assembly as final visual sources.
- POP-05 now uses M01/current Chapter 1 canonical result content and removes stale rejected labels.
- SCN-02 now uses Credits, Materials, and Command Authority, includes the requested mode cards, and marks non-live routes as designed unavailable.
- Both layer manifests are present and parse as valid JSON.

## Caveats

- Final runtime labels must remain live TMP/data-bound text, not baked imagegen text.
- Unity import, slicing, TMP binding, and implementation remain held until PM/user approves the visual targets.
- POP-11, POP-10, SCN-11, SCN-12, POP-06, Operation screens, and M01 Gameplay art remain held.

## Current Routing

Current owner:
PM/user visual review

Held:
Art/Atlas, Gameplay, QA/HCI, UI, Support/FTUE, Designer, and all non-routed Art packages unless PM/user routes follow-up work.
