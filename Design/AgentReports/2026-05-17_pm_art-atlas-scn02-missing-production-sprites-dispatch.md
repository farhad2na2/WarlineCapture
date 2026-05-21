# PM Dispatch: SCN-02 Missing Production Sprites

Date: 2026-05-17
Owner: PM
Assigned lane: Art/Atlas
Status: active

## Decision

SCN-02 Main Menu implementation is blocked on missing Art/Atlas production sprites. UI must not continue filling missing regions with placeholders, old shell art, deterministic substitutes, target-reference panel crops, or flattened mockup overlays.

## Rejected Shortcut

- `Design/AgentReports/2026-05-17_pm_scn02-mainmenu-target-slice-implementation-rejected.md`

## Evidence

- `Design/AgentReports/2026-05-17_pm_scn02-mainmenu-layered-canvas-wip.md`
- `Design/AgentReports/Captures/SCN-02_MainMenu_LayeredCanvasWorkInProgress_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_LayeredCanvasWorkInProgress_vs_Target_Comparison.png`

## Required Art/Atlas Output

- `Design/AgentReports/2026-05-17_art-atlas_scn02-mainmenu-complete-production-sprites.md`

## Required Missing Or Insufficient SCN-02 Sprites

- Full-screen tactical map/background layer matching the approved target, not copied from the flattened reference.
- Top-left logo/brand panel frame and final brand/emblem treatment if needed.
- Full top resource bar frame/chrome with panel divisions and settings dock.
- Target-quality resource counter slot/frame variants if current generic frame is insufficient.
- Settings button frame matching the target.
- Commander profile panel frame sized for the target.
- Left nav row frame/chrome.
- Left nav icons: Inbox, Store, Events, Ranking, Command Feed.
- Large vertical mode card frame/chrome, or per-card frame variants, matching the three target cards.
- Mode card header emblems: Saga, Persistent Operation, Quick Custom.
- Persistent Operation warning icon, segmented meters, row/divider chrome.
- Mode card footer icons/circle badges if needed to match the target.
- Dedicated Deploy Command CTA frame, amber glow overlays, chevrons, and states.
- 20:9 Command Feed panel frame and command feed icon.
- Button state sprites/rules for nav rows, mode cards, settings, and deploy.
- Shadows/glow/trim overlays needed for target depth without UI inventing deterministic effects.

## Package Requirements

- Add/update PNG layers under `Design/VisualLockLayered/SCN-02_MainMenu/layers/`.
- Update `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json` with layer ids, files, roles, `unityDestination`, slicing, binding notes, target rect/usage guidance, and 16:9/20:9 variants.
- Update/create a contact sheet showing the full production sprite set.
- Update `Design/VisualLockLayered/SCN-02_MainMenu/README.md`.

## Production Rule

New or replacement visual assets must be imagegen-sourced. Deterministic tooling is allowed only after imagegen selection for cleanup, slicing metadata, manifest/contact-sheet packaging, inspection, and validation.

Do not use HTML/CSS screenshots, local programmatic composites, vector substitutes, pixel patching, deterministic mockups, or target-reference panel crops as final runtime art.

## Acceptance Gate

UI remains blocked until Art/Atlas delivers the required report and PM/user accepts it.
