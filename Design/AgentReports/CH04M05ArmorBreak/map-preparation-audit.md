# CH04-M05 Armor Break — map preparation audit

Recorded 2026-10-02. Source SHA snapshot: 2026-10-02T05:59:46Z. This report records authoring decisions and read-only checks; it does not claim a native mission pass or device acceptance.

## Approved bounded fallback

The campaign contract permits an independently versioned smaller industrial/airfield derivative when Frontier device qualification is unavailable. Root approved this fallback during this task. The source is prepared Frontier, content hash `e2d40d5eea9a0b81a1500cd14040329fc638b7909249ddf095ea3e5d9c6d54cf`.

The latest checked source evidence (`Design/MapVariants/Preparation/Frontier/Candidate/packed-runtime-evidence.json`) is a **Mac17,3 StandaloneOSX packed route fixture**, with Fuel disabled, moving and 120 idle frame diagnostics, and explicitly no player/Android device acceptance. Its recorded memory is 641,468,380 bytes. That does not qualify the full Frontier theatre for a device.

The independent derivative retains source-runtime world rectangle **(740,260)–(2020,780)**: 1280×520 cells, including the refinery, forward depot, military highway and city-edge runway/apron. Grid/surface origin is `(740,0,260)`; generated owner and static-blocker cell indices are rebased by `(740,260)`. World positions retain their original road alignment. The full source contains 25,678 classified placements; approximately 5,336 remain before final API ownership/count qualification. Native counts may differ because attached visual ownership and intersecting infrastructure are counted separately.

Builder: `Game.Editor.CH04M05ArmorBreakConfigBuilder.Build()`. API-authored outputs:

- `Assets/Game/GeneratedOperationMaps/CH04M05ArmorBreak/{Definition,Surface,Grid}.asset` and `Minimap.png`.
- `Assets/Game/Scenes/OperationMaps/CH04M05ArmorBreak/{PreparedEntities,RuntimeBinding}.unity`.
- `Assets/Game/Configs/Missions/Chapter04/MissionDefinition_Ch04_M05_ArmorBreak.asset`.
- `Assets/Game/Configs/Scenarios/Chapter04/ScenarioSetup_Ch04_M05_ArmorBreak.asset`.
- `Assets/Game/Prefabs/Buildings/CH04M05ArmorBreak/Building_ArmorBreak_ReserveDepot.prefab`.

These are separate assets. SourceBinding is blank because this derivative owns a different physical scene; provenance is represented by a versioned source identity hash. The binding uses the explicit entity-scene loader with disabled/unbound SubScene, preserving one runtime surface owner. The builder checks every DenseCity identity for uniqueness and verifies six source files remain byte-for-byte unchanged.

## Tactical geometry and real systems

All coordinates below are world X/Z. Anchors use `anchor.ch04.m05.`.

| Anchor | Position | Contract |
| --- | --- | --- |
| fuel_reserve | 1305,485 | Qualified 26×25 reserve footprint; finite capacity240, actual starting Fuel200, protected floor40 |
| launcher_ground | 1280,529 | Existing Ground Missile Launcher, min35/max600/radius8 |
| radar / coverage | 1440,529 | Real radar support and bounded Move stage |
| launcher_air | 1480,529 | Existing G2A, base detection220, radar support320 |
| armor / armor_b / armor_c | 1360/1348/1336,529 | Two tanks and APC on highway |
| command_squad | 1385,515 | Four original regular infantry |
| aircraft | 1540,379 | Real attack helicopter beside airfield taxiway |
| battery | 1560,529 | Stationary hostile military G2G target; distance280 from launcher |
| hostile_armor | 1585,529 | Ground threat, individually spaced companion anchors1597/1609 |
| command / command_staff | 1700/1715,529 | Command radar and four regular infantry |
| authority | 1740,529 | Surviving original infantry dismounted, together, six-second hold |
| relief | 1900,480 | Three neutral relief staff; protected radius40 |
| air_approach | 1900,294 | Actual east runway spawn, not a city rooftop |
| air_flank / air_contact | 1900,700 / 1500,700 | Northern air approach toward actual G2A coverage, clear of the helicopter apron | 

The scenario contains exactly 11 friendly units, 12 hostile military units and 3 protected neutral relief staff. All four hostile groups belong to finite Defense convoy Elements. Generic defense victory is replaced by the dedicated mission runtime. Radar ping charges remain zero to avoid introducing a gameplay button; the radar's real G2A support component is present.

Read-only weapon configuration checks confirmed G2G distance280 is within35–600 and the battery is more than40+8 from relief. The builder's fail-closed native route gate checks the471×7 military highway, deployment/authority/relief formation margins, and all650 cells of the exact26×25 off-road reserve placement. Only that cleared, non-road, low-slope reserve rectangle gains BuildingPlacement permission. It does not bypass runtime building validation. Wider logistics is pre-authored as this finite reserve; no mandatory hauling link is introduced.

## Source SHA-256 snapshot

| File | SHA-256 |
| --- | --- |
| Frontier/Candidate/Definition.asset | aaaa53263cfd17e032b01db9c5bc65492df0ab11f81bb7d9a03d5533e64df167 |
| Frontier/Candidate/Surface.asset | 3b03cc8035ffde5ff3ca52863aecfa024aec2e25b9a4ad409c73b3622c201388 |
| Frontier/Candidate/Grid.asset | e8ad7d074eff75cc1a4f645b42324cd3b268530f43bb77b2abcf2336785ccc28 |
| Frontier/Candidate/Minimap.png | 3659927ea9d4d1c07b9ef50ed6224e1c059ecdf4e30e82f85a013448a362994b |
| Frontier/PreparedEntities.unity | 720ccbb703d8389bc7d4e4b85825e29357a9c17ffb7cc6c52881c5a684e78bff |
| Frontier/RuntimeBinding.unity | e2d3acbd08c99e51536f11af061158f4d98231b8aeef1f2b1ff56828db9ac428 |

## Native authoring qualification

`Evidence/build-wrapper-08.log` and `Evidence/map-wrapper-01.log` passed with wrapper receipts0. The saved crop has **5,327 identities,667 gameplay building owners,4,660 render-only identities and3 ownership roots**. Own scene/binding GUIDs, disabled explicit-loader SubScene placeholder, grid/surface hashes, rebased footprints,471×7 highway and650reserve cells passed. All six prepared Frontier source files match the original SHA snapshot. Earlier failed generation logs remain under Evidence.

## Final native evidence and pending gates

- Passed English/Persian manual and English ARIA normal-input Editor journeys: actual aircraft order, G2A interception, real G2G shot, armor assault, original-infantry hold, relief/Fuel survival, 14 localized comic panels, result, settlement and return. Exact wrappers are listed in `review-readiness.md`. Voices remain pending.
- Final native Entities/Addressables build passed in `packed-wrapper-05`, receipt/wrapper exit 0, no Burst/compile errors. Final `map-wrapper-02` passed routes, footprints and all six unchanged source hashes. These gates do not establish player/device acceptance.
- Normal production player build/load and measured target-device performance; real player review/acceptance. Existing source diagnostics and reduced authoring counts are not substitutes.

## Source provenance label

The cloned DenseCityGeneratedRoot retains the Frontier prepared source generation label. The derivative has its own operation-map ID, physical scene GUID, source identity hash and presentation migration hash. The retained label records origin; it does not bind or load the original theatre.

## Native combat revisions

Normal-input runs exposed inherited base-defense timing and patrol routes. The air wave waits for actual G2A coverage and follows the northern flank. Original armored defenders remain at their military positions until the helicopter clears them; the ground armor then assaults the command group. The original helicopter uses its actual 155-metre weapon range with 10 metres of standoff padding and holds completed Move destinations. All original weapon damage and health values remain unchanged. The generic preparation banner is replaced by the current Armor Break objective. Failed evidence remains under Evidence.
