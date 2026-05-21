# PM Acceptance - SCN-02 Target-Lock Asset Revisions Accepted, UI Final Pass Dispatched

Date: 2026-05-17
Owner: PM
Status: accepted for UI final implementation pass
Priority: P0

## Decision

Art/Atlas handoff is accepted for UI import and final SCN-02 target-lock pass:

- `Design/AgentReports/2026-05-17_art-atlas_scn02-target-lock-asset-revisions.md`

This is not final SCN-02 visual acceptance. It means the revised Art package is good enough to hand back to UI for import, placement, rebuild, capture, and direct comparison.

## PM Checks

PM reviewed the Art report and focused package evidence:

- `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json` parses.
- Manifest status is `ReadyForReview_TargetLockAssetRevisions_SCN02`.
- Manifest layer count remains `49`.
- Required revised layer ids are present.
- Every manifest-declared layer file exists.
- No `target_slice_*` file is referenced by the manifest.
- Revised routed layers are marked with imagegen provenance.
- Contact sheet exists: `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/scn02_target_lock_asset_revisions_contact_sheet.png`.
- `copy_layers_to_unity.py` dry-run maps the revised assets to Unity destinations.

Visual review of the contact sheet is acceptable for the next UI pass:

- Card art now matches the intended Saga, Persistent Operation, and Quick Custom scenes much more closely.
- Commander portrait is now a target-style silhouette/profile scan.
- Resource icons, brand emblem, nav icons, badge, and deploy chevrons/glow are no longer placeholder-looking.

Important implementation note:

- The revised layer filenames are the same as the previous Unity asset filenames.
- `Design/VisualLockLayered/SCN-02_MainMenu/copy_layers_to_unity.py --apply` will skip existing Unity files unless `--force` is supplied.
- PM confirmed at least `mode_card_art_saga` and `commander_profile_portrait` currently differ between the design layer source and Unity destination.
- UI must import with `--apply --force`, not plain `--apply`, so the revised Art actually reaches `Assets/`.

## Current Routing

- Art/Atlas is held.
- UI owns the final SCN-02 import/placement/capture pass.
- QA/HCI must not review SCN-02 target-lock until UI produces fresh runtime/editor proof after importing the revised assets.

## Required UI Output

UI must deliver:

- `Design/AgentReports/2026-05-17_ui_scn02-mainmenu-final-target-lock-pass.md`

Scope:

- `SCN-02_MainMenu` only.
- Do not work on `POP-05_MissionResult`, `SCN-08_RTSBattleHUD`, or new screens during this pass.

Required first step:

- Run `python3 Design/VisualLockLayered/SCN-02_MainMenu/copy_layers_to_unity.py --apply --force`.

Then:

- Rebuild `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab`.
- Rebuild/update relevant Main Menu sprite atlases/import settings as needed.
- Preserve real route buttons and live TMP/data bindings.
- Keep using accepted manifest-declared layers only.
- Continue to reject target slices, target composites, screenshots, comparison images, contact sheets, and placeholders as runtime UI.

Required proof:

- Fresh `1672x941` runtime/editor capture after forced revised-asset import.
- Fresh 20:9 runtime/editor capture after forced revised-asset import.
- Direct comparison images against:
  - `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png`
  - `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_20x9_Target.png`
- Updated MSE scores.
- Region-by-region mismatch table covering background, masthead, top bar, settings, commander profile, left nav, three mode cards, operation detail rows, deploy CTA, and 20:9 command feed.
- Files changed and validation commands/tests.
- Explicit statement that Unity asset destinations were overwritten with revised Art/Atlas source layers.

Acceptance rule:

UI may not claim target-lock complete unless the fresh captures visibly match the approved references region by region. If the result is still off after revised art import, UI must identify whether each remaining mismatch is placement/TMP/layout owned by UI or art-content owned by Art/Atlas.
