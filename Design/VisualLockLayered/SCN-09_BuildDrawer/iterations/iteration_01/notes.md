# SCN-09 Build Drawer V3 — Iteration 1

Status: current review candidate; explicit user acceptance pending.

Targets:

- `../../reference/SCN-09_BuildDrawer_MatchBuildPopupV3_Final_Target.png`
- `../../reference/SCN-09_BuildDrawerV3_DisabledState_Final_Target.png`

Evidence:

- `build_drawer_ready_prefab_16x9.png` and `build_drawer_ready_prefab_20x9.png` — deterministic ready-state prefab renders at 1920x1080 and 4800x2160.
- `build_drawer_disabled_prefab_16x9.png` and `build_drawer_disabled_prefab_20x9.png` — deterministic disabled-state prefab renders at both required ratios.
- `build_drawer_live_16x9.png` and `build_drawer_live_20x9.png` — the real Menu scene in Play Mode, routed to Match with the live Build Drawer popup and runtime catalog binding.
- `prefab_build_capture.log`, `focused_behavior_validation.log`, `live_capture_16x9.log`, and `live_capture_20x9.log` — immutable Unity evidence for this iteration.

Target comparison:

- The V3 header, four-tab rail, 2x2 catalog, selected-item detail, production queue, instruction strip, and standalone `PLACE` action now follow the target hierarchy.
- Major surfaces use visible directional procedural gradients and the same 3 px border width. Neighboring frames are independently inset, so no header, tab, catalog, detail, queue, or footer border cuts through another panel.
- The ready state uses amber selection only for the selected tab/card. The disabled state removes card selection emphasis, dims the catalog consistently, presents the lock reason, and disables `PLACE`.
- The centered 1672x941 composition stays stable at 1920x1080 and 4800x2160. Ultrawide adds context at the sides instead of stretching the popup or its art.
- Runtime Play Mode populated Airport, Barracks, Contractor Tent, and Dirt Wall from the current catalog. The deterministic evidence uses four other existing building definitions only to exercise the same reusable card layout.
- No unit or building illustration was generated for this prefab. Cards bind the existing config `portraitCardSprite`; detail binds `portraitActionSprite`; runtime queue thumbnails resolve through the same catalog. All small symbols come from existing shared V3 atlases or procedural geometry.

Known difference, not accepted as final:

- The Menu-scene Match route does not load a battlefield scene, so its live evidence has dark empty context behind the modal with only the Match HUD edge visible. This validates real runtime mounting, catalog data, state, and responsive geometry, but a later in-match capture must verify the target's dimmed battlefield backdrop before acceptance.

Validation markers:

- `[BuildDrawerV3PrefabBuilder] validation=Passed tabs=4 gradients=19 borders=3 images=runtime-catalog aspect=preserved`
- `[BuildDrawerV3PrefabBuilder] result=Passed layout=1672x941 gradients=procedural borders=3 art=catalog-bound atlases=shared`
- `[BuildDrawerCatalogQueryValidation] result=Passed tests=25`
- `[CanvasRouteCaptureValidation] result=Passed` at 1920x1080 and 4800x2160.
