# SCN-09 Build Drawer — iteration 02

Target lock:

- `../../reference/SCN-09_BuildDrawer_MatchBuildPopupV3_Final_Target.png`

Proof captures:

- `build_drawer_ready_prefab_16x9.png`
- `build_drawer_ready_prefab_20x9.png`
- `build_drawer_disabled_prefab_16x9.png`
- `build_drawer_disabled_prefab_20x9.png`

Changes accepted in this iteration:

- Preserved the target's four-column hierarchy, sharp three-pixel frame language, selected-card emphasis, and green/red/blue/amber gradient states.
- Replaced the old global resource art with the shared V3 Materials, Oil, and Fuel sprites; colors now remain distinct in the header, catalog cards, and detail rows.
- Kept building and unit thumbnails bound to the game's canonical catalogs, so the screen does not duplicate the existing gameplay art as mockup-only UI images.
- Made tab listener wiring idempotent during refresh. This fixes category buttons that were visible but inactive after late runtime metadata binding or a domain reload.
- Verified ready and construction-disabled states at 1920x1080 and 4800x2160 without stretching the 1672x941 composition.

Functional acceptance:

- 28 focused checks passed.
- All four categories switch to their own catalog.
- Buildings route to placement and close the drawer.
- Vehicles, Aircrafts, and Soldiers route to production and keep the drawer open.
- Card selection, queue cancel, queue clear, close, and the primary action all expose valid pointer targets.

Intentional target variation:

- Catalog names and thumbnails use the project's real registries rather than the illustrative buildings rendered in the target lock.
