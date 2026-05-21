# PM Acceptance - SCN-02 Art/Atlas V2 Accepted, UI Placement Pass Dispatched

Date: 2026-05-17
Owner: PM
Status: accepted for UI placement pass
Priority: P0

## Decision

Art/Atlas v2 handoff is accepted for UI import and focused placement/capture:

- `Design/AgentReports/2026-05-17_art-atlas_scn02-target-lock-asset-revisions-v2.md`

This is not final SCN-02 target-lock acceptance. It means the v2 source-layer package is clean and close enough to test in runtime UI.

## PM Checks

PM reviewed the v2 Art report and package evidence:

- `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json` parses.
- Manifest status is `ReadyForReview_TargetLockAssetRevisionsV2_SCN02`.
- Manifest layer count remains `49`.
- Required revised layer ids are present.
- Every manifest-declared layer file exists.
- Every manifest-declared source file exists.
- No `target_slice_*` file is referenced by the manifest.
- Revised routed layers are marked with v2 imagegen provenance.
- `copy_layers_to_unity.py` dry-run maps the revised layers to Unity destinations.
- V2 contact sheet exists: `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/scn02_target_lock_asset_revisions_v2_contact_sheet.png`.

Visual review:

- V2 card art, resource icons, badge/lock, left-nav icons, commander silhouette, and deploy chevrons/glow are acceptable for the next UI pass.
- Some target-lock risk remains, but another Art-only loop should not happen before UI tests these v2 layers in the actual screen.

## Current Routing

- Art/Atlas is held.
- UI owns the next SCN-02 focused placement/import/capture pass.
- QA/HCI must not review SCN-02 target-lock until UI produces fresh v2 runtime proof and PM/user accepts it.

## Required UI Output

UI must deliver:

- `Design/AgentReports/2026-05-17_ui_scn02-mainmenu-v2-placement-pass.md`

Scope:

- `SCN-02_MainMenu` only.
- Do not work on `POP-05_MissionResult`, `SCN-08_RTSBattleHUD`, or new screens during this pass.

Required first step:

- Run `python3 Design/VisualLockLayered/SCN-02_MainMenu/copy_layers_to_unity.py --apply --force`.

Reason:

- The v2 revised layer filenames reuse existing Unity destination filenames.
- Plain `--apply` skips existing files and will not import the v2 Art.

Required UI placement work:

- Rebuild `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab`.
- Move the 20:9 command feed to the lower-left target position.
- Tighten top bar and settings rects toward the target.
- Tighten commander profile frame, portrait, and lower label.
- Tighten left-nav badge/lock placement and TMP sizing.
- Tighten mode card title/icon/body/footer placement.
- Tighten Persistent Operation warning rows and meters.
- Reduce deploy CTA scale/tone/chevron placement toward the target.
- Preserve real route buttons and live TMP/data bindings.
- Use accepted manifest-declared layers only.
- Do not use placeholders, target slices, target composites, screenshots, comparison images, contact sheets, or full mockup overlays as runtime UI.

Required proof:

- Fresh `1672x941` runtime/editor capture after forced v2 import.
- Fresh 20:9 runtime/editor capture after forced v2 import.
- Direct comparison images against the 16:9 and 20:9 SCN-02 target references.
- Updated MSE scores compared against the previous final pass values:
  - previous 16:9 MSE: `1077.03`
  - previous 20:9 MSE: `1043.91`
- Region-by-region mismatch table covering background, masthead, top bar, settings, commander profile, left nav, three mode cards, operation detail rows, deploy CTA, and 20:9 command feed.
- Files changed and validation commands/tests.
- Explicit statement that Unity asset destinations were overwritten with v2 Art/Atlas source layers.

## Acceptance Rule

UI must not claim SCN-02 target-lock complete unless fresh captures visibly match the approved references region by region. If the result is still off, classify each remaining mismatch as UI-owned placement/TMP/layout or Art/Atlas-owned content.
