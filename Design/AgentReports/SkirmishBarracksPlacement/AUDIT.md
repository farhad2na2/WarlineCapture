# S003 starting-building placement repair

The screenshot is Skirmish S003, Desert Base / Air Mobile / Field. The player barracks occupied grid origin (922,349), footprint (28,15). Native geometry inspection confirmed intersections with the authored hangar, control tower and plywood shed.

## Change

Skirmish shared building placement now reserves intact packed map buildings and the older authored ECS SM_Bld_ meshes. Reservations use world bounds plus one metre of clearance, cached per map database, grid and geometry count. The same reservation is checked during starting grants, preview and commit, including road gates. The starting-grant boundary waits for loaded geometry before searching; map metadata alone is insufficient readiness. Starting grants retain their identity and move through the existing nearest-legal-placement search. Campaign authored placements are outside this repair.

## Evidence

- Before: `Before/S003-player-base.png`.
- Initial probe launched S003 through the native Quick Custom setup and identified the intersecting geometry.
- First packed-only fix failed independent native geometry validation on `SM_Bld_Plywood_Shed_01`; this prompted coverage of the older ECS path.
- Two dispatches were rejected during Editor compilation, and one revision failed compilation on a direct rendering assembly dependency; corrected by using the existing rendering contract. Subsequent native checks exposed an additional startup load-order race: the map metadata was ready while render geometry count was still zero. Starting grants now wait for authored geometry bounds before resolving their placements. Failed evidence is preserved locally in `Logs/`.
- Final native geometry check passed: 14 buildings, 2 factions, zero streamed-building intersections. Player barracks relocated from (922,349) to (987,297); enemy barracks remains at its clear (1672,101) footprint.
- Native screen/control checks passed: 9 screens, en/fa-IR, 1920×1080 and 1280×720, touch inputs, camera actions preserve orders. Wrapper exit 0; `[ExistingEditorValidation] result=Passed` in `Logs/warline-placement-fixed-08.log`.
- Visual review of `After/en-1920-hud.png`: the player barracks stands on clear ground, separated from the hangar and control tower. User visual acceptance remains pending.

## Readiness

This targeted placement check does not establish a completed normal-input match or real-device acceptance. Existing virtualized building count / packed render owner errors remain visible in native logs; broader gameplay readiness is pending.
