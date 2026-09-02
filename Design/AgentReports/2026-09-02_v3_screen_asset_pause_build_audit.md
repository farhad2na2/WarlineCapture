# V3 screen asset, Pause, and Build Drawer audit — 2026-09-02

## Scope

This pass audited every UI prefab for legacy/placeholder sprite references and V3 atlas ownership, then performed focused visual and interaction acceptance on POP-07 Pause Options and SCN-09 Build Drawer at 16:9 and 20:9.

## Project-wide V3 asset result

| Check | Result |
| --- | ---: |
| UI prefabs scanned | 37 |
| Sprite references scanned | 765 |
| Legacy sprite references | 0 |
| Placeholder sprite references | 0 |
| V3 atlas packable duplicates | 0 |
| V3 atlas orphans | 0 |
| Packed shared V3 sprites | 65 |

The migration replaced 10 stale command-icon references in seven prefabs with the sharp `MatchCommandsAligned` versions. Eight obsolete duplicate command PNGs were removed after their remaining references were migrated. The validator now checks the complete small-sprite roots, not only sprites that happened to be referenced by the current prefabs.

Affected prefab references:

- `ConfirmRaidPopup`
- `IntelRevealPopup`
- `SCN12_DistrictDetailActionsContent`
- `SCN13_SkirmishSetupContent`
- `SCN19_ArmoryContent`
- `POP13_ARIACommandAssistantPopup`
- `SCN08_FullMapPopup`

## Build Drawer findings and fixes

The visible category tabs could become inactive because their click listeners were only connected in `OnEnable`. A late catalog/metadata binding or domain reload could refresh the visible drawer without restoring listeners. `Refresh` now performs idempotent tab wiring, so every runtime refresh preserves interaction.

The builder now fails validation unless:

- all four unique categories are bound;
- the unit and building catalogs are serialized;
- every visible nonzero border is exactly three pixels;
- every user-facing button has a raycastable target;
- non-catalog UI art is shared V3 atlas art;
- Materials, Oil, and Fuel use the shared Match V3 resource set.

Final interaction result: **28/28 passed**. Coverage includes all categories, item/detail binding, placement, unit production, instruction states, queue cancel/clear, close behavior, HUD input blocking/restoration, and pointer hit-testing.

Visual proof:

- `Design/VisualLockLayered/SCN-09_BuildDrawer/iterations/iteration_02/build_drawer_ready_prefab_16x9.png`
- `Design/VisualLockLayered/SCN-09_BuildDrawer/iterations/iteration_02/build_drawer_ready_prefab_20x9.png`
- `Design/VisualLockLayered/SCN-09_BuildDrawer/iterations/iteration_02/build_drawer_disabled_prefab_16x9.png`
- `Design/VisualLockLayered/SCN-09_BuildDrawer/iterations/iteration_02/build_drawer_disabled_prefab_20x9.png`

## Pause Options findings and fixes

Pause validation now rejects Synty, legacy, placeholder, or unowned standalone sprites. It accepts only V3-root art or a sprite packed exactly once by a V3 atlas. The rebuilt prefab uses constant three-pixel frames and procedural V3 gradients.

Final result: **4/4 contract groups passed**, covering hierarchy/borders, real shell actions and runtime overlays, shared V3 art, and pointer targets on every visible button. Live route captures passed at both aspect ratios.

Visual proof:

- `Design/VisualLockLayered/POP-07_PauseOptions/iterations/iteration_02/pause_options_live_16x9.png`
- `Design/VisualLockLayered/POP-07_PauseOptions/iterations/iteration_02/pause_options_live_20x9.png`

## Remaining visual issue found

The 20:9 Pause route exposed a separate Match HUD problem: a red status/current-order strip remains visible behind the modal near the ARIA rail. This is not part of the Pause prefab. It needs a Match HUD modal-occlusion/alignment correction and a new SCN-08 wide target-lock iteration.

The zero-placeholder/zero-legacy result is an asset-identity guarantee. It does not replace screen-by-screen visual comparison; other screens still require their own target-lock acceptance passes for spacing, scale, and composition.

## Validation evidence

- `/private/tmp/warline-v3-build-drawer-audit-final.log` — `[BuildDrawerCatalogQueryValidation] result=Passed tests=28`
- `/private/tmp/warline-v3-build-drawer-capture-r2.log` — Build/validation and four captures passed
- `/private/tmp/warline-v3-pause-options-audit-final.log` — `[PauseOptionsV3PrefabTests] result=Passed tests=4`
- `/private/tmp/warline-v3-pause-capture-16x9-r2.log` — live 1920x1080 route passed
- `/private/tmp/warline-v3-pause-capture-20x9.log` — live 4800x2160 route passed
- `/private/tmp/warline-v3-shared-atlas-audit-final.log` — 37-prefab shared-atlas audit passed
