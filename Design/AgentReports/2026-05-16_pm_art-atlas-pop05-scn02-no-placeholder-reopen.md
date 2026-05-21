# PM Art/Atlas Reopen: POP-05/SCN-02 No-Placeholder Implementation Layers

Date: 2026-05-16
Lane: PM
Target lane: Art/Atlas
Status: active routing

## Reason

UI v3 is rejected because it used placeholder-scale/generated fallback art and still did not match the approved targets.

Rejected UI report:

- `Design/AgentReports/2026-05-16_ui_visual-target-match-implementation-v3.md`

The PM/UI instruction is now clear: UI may not use placeholders, fallback art, generic generated art, old shell assets, or "closer" substitutes. If the approved layered package contains placeholder/faulty implementation slices, Art/Atlas must fix the package before UI retries.

## Required Art/Atlas Report

`Design/AgentReports/2026-05-16_art-atlas_pop05-scn02-implementation-ready-no-placeholders.md`

## Scope

- `Design/VisualLockLayered/SCN-02_MainMenu/`
- `Design/VisualLockLayered/POP-05_MissionResult/`

## Required Work

- Audit every manifest layer and every file under `layers/`.
- Replace any placeholder, placeholder-scale, generic, old-shell-derived, deterministic-looking, low-quality, wrong-size, or fallback-looking production layer.
- Replace any layer whose id/path/name contains `placeholder` with finished imagegen-sourced production art.
- If a fallback commander/profile visual is needed, it must still be polished production art and must not be named or treated as a placeholder.
- Verify SCN-02 mode-card art slices match the approved target quality and composition.
- Verify POP-05 background, mission image/identity art, modal chrome, reward cards, buttons, stars, consequence row, and icons match the approved target quality and composition.
- Update manifests with correct `unityDestination`, role, slicing settings, and binding notes.
- Provide contact sheet evidence and changed-file list.

## Visual Production Rule

Any visual replacement must be imagegen-sourced. Deterministic tooling is allowed only for slicing metadata, alpha cleanup, file inspection, manifest updates, and validation after imagegen source selection.

Do not use HTML/CSS screenshots, local compositing, vector drawing, scripted UI assembly, pixel patching, or placeholder renders as final visuals.

## Routing

Art/Atlas owns the next action. UI should not retry POP-05/SCN-02 target-lock implementation until this Art/Atlas report exists and PM/user accepts it.
