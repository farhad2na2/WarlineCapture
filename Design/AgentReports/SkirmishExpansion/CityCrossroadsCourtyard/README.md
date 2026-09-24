# City Crossroads northern courtyard — 2026-09-24

Second Skirmish list entry: S025 City Crossroads. Replaces the inherited elevated market west/southwest of the northern base with a courtyard on the existing continuous sand foundation.

## Authored change

- Removed 962 overlapping cluster objects, including 19 building placements, within x938–1042 / z632–718.
- Preserved the map-wide sand foundation and major through-roads. Added no floating tiles or ground overlay.
- Added ten grounded palms, six rocks and seven low ruined walls around the perimeter; retained a 23-cell-wide north/south movement route.
- Rebuilt 8,944 surface cells; verified all serialized samples outside the footprint stayed unchanged.
- Rebuilt render database from actual authoring (74,876 source rows, 40,567 placements); synchronized scene readiness counts (36,411 generated identities, 8,568 accepted identities).
- This presentation and movement surface are shared; dependent map definitions received updated surface hashes.

## Evidence

Editor visual review: overview and both side joins inspected. All 23 dressing objects contact the ground. Ground-height and route validation passed through the required GUI-licensed macOS wrapper. `courtyard-overview.png`, `courtyard-west-join.png`, `courtyard-east-join.png` are Editor renders, not gameplay screenshots.

Initial runtime check caught stale render-database building counts after removing buildings. Rebuilt database and readiness contract; retained failure evidence in the runtime log. Earlier play startup was interrupted by script recompilation; it is not passing evidence.

Final normal-input launch: Menu → Skirmish → S025 City Crossroads → Start Match passed; match ran with zero Console errors after the render rebuild. Further clicks/drags failed in the desktop automation layer with `noWindowsAvailable`, including after reconnect/reset. ARIA, troop traversal, result and return were not verified. No injected outcomes were used. Real player/device acceptance remains pending. Final wrapper terrain check exited 0 with its required pass marker.
