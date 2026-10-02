# Citywide Alert map preparation

Updated 2026-10-02. Native saved-map, route, rule, objective and bilingual voice-install gates passed in `validation-wrapper-05.log`. Complete normal-input mission and device acceptance remain pending.

## Approved bounded Urban source

Mission `saga.ch05.m01.citywide_alert` uses independent `opmap.ch05.citywide_alert`, world rectangle `(860,300)`–`(1300,520)`, grid 440×220 at origin `(860,0,300)`. Source is the frozen ExistingDenseCity candidate, semantic content hash `2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c`. Build records fresh SHA256 for six source files before and after generation and fails if any changes. Source assets remain untouched.

Recipe revision is `urban-civic-v6-qualified-reserves-independent-prefabs`. The saved derivative has 325 gameplay building owners, 2,979 unique Dense presentation identities (325 gameplay identities and 2,654 render-only identities), three presentation ownership roots, one native GridAuthoring and one MapSurfaceAuthoring. Readiness counts are a global ECS contract, matching the native authoring totals. `build-wrapper-06.log` and `validation-wrapper-05.log` record the final v6 native counts; the source scene is unchanged. Legacy accepted render-only visuals are migrated to unique own Dense identities before the physical crop and retained-prop blocker pass; accepted building descriptors are retired after actual owners receive Dense identities. Unrelated authored source vehicles are removed with their UnitGrid owners. The derivative retains no accepted whole-city descriptors, preventing duplicate identity counts.

The crop uses resident rendering and removes the whole-city virtualized presentation owner from its own copied scene. It clears physical source owners/props only within the derivative: both service compounds, both Barracks/reserves, independent infantry egress and response boulevard. Native owner footprints are rebased; surface Cell and SurfaceId are rebased. Independent grid, surface, raster, physical scene, binding and definition are generated through Editor APIs. Binding uses one disabled unbound SubScene placeholder and explicit entity delivery.

## Civic services and real production

| Owner | Origin / footprint | Delivery |
|---|---|---|
| Clinic | 899,444 /14×12 | Own health1000 clone of Evidence Chain clinic; existing medical sign/crate |
| Water/power service |1196,442 /20×16 | Own health700 WaterTank clone with generator; one compound health owner |
| West Barracks |1006,330 /28×15 | Existing real production prefab; reserved40×20 plot |
| East Barracks |1166,330 /28×15 | Separate real production prefab; reserved40×20 plot |
| West reserve |1076,378 /26×25 | Own finite Fuel storage capacity240, supplied100, protected20 |
| East reserve |1234,356 /26×25 | Separate finite Fuel storage capacity240, supplied100, protected20 |

The final reserve lots were qualified against the live native startup road mask, authored road/sidewalk/water masks, blockers and flatness using the full26×25 footprint: west `(1076,378)` and east `(1234,356)`. Their clearance pads remove retained props/owners while preserving physical roads and every road mask. The earlier reserve positions `(1060,352)` and `(1220,352)` intersected authored road footprints despite empty primary surface road flags. `debug-captioned-en-wrapper-04.log` retains the failed placement/survey evidence.

All three own building prefabs now completely unpack their source before configuring and saving. Reloaded prefab verification requires independent prefab ownership, no source config and exact persisted values: clinic14×12/health1000, utility20×16/health700/no production, reserve26×25/Fuel capacity240. Earlier own prefabs were variants; independent saved-value checks now pass. The native utility health900 report was subsequently traced to an entity-binding collision: a prepared map owner and the managed request shared RuntimeBuildingId2. It was not evidence that the saved utility health700 was wrong.

Runtime creates all six through actual building placement requests. They are not decorative objectives. Starting Materials180 with runtime max240. Existing Barracks recipe is `Unit_Chr_Soldier_Male_02_Alt_04`, quantity4 per ordinary transaction; production readiness must prove an actual completed transaction and new units. No new gameplay buttons.

Response boulevard uses z426, x875–1275, 7-cell armor footprint. Independent production infantry exits use x1020/x1180, z352–426, 3-cell footprint. Canonical source movement masks, heights, normals and surface flags are preserved exactly. The crop applies retained static-prop masks and restores placement permission only on the six flat, off-road cleared lots; it does not recompute accepted road permissions from noisy curb normals or flatten terrain. Native validation passed the combined owner/static/surface checks for the full 401×7 response boulevard, two independent 75×3 infantry egress lanes and six building lots. Actual gameplay movement remains a normal-input gate.

Engineer rear starts are now authored at `(970,426)` and `(1130,426)`, about65m/77m from their external recovery anchors `(906,438)` and `(1206,438)` with radius6. The narrow anchor revision `citywide-anchor-v2-rear-engineers-970-1130` preserves the v6 physical scene/grid/surface/raster and migration hash; its native refresh is awaiting execution. These starts give95m/133m initial separation from the nearest incoming clinic infantry/utility tank, outside actual85m/120m ranges. They remain vulnerable when enemies advance; no health, threat count or timing was changed. G2A starts `(1100,426)`, about42m from coverage `(1058,428)` with radius24. Supplied units start outside these gates so ordinary Move commands are necessary; no hold radius was weakened.

## Supplied roster and warnings

12 friendly originals: two fronts each2 tanks+2 riflemen, two original engineers, G2A and radar. Four protected original staff (two per front). Eleven hostile originals: clinic4 infantry+APC, utility2 tanks+2 infantry, air2 drones. No paid command gate. Clinic/utility warning0, planned release130000ms/contact145000ms; air warning20000ms/release150000ms/contact165000ms. Runtime gates releases on actual production and readiness, owns bounded ARIA consent, recovery/perimeter rules and Fuel spending.

## Native presentation and catalogs

The final v6 build passed the native URP StandardRequest capture for the actual derivative with exact `(860,300,440,220)` projection and a black/uniform-image rejection gate. The approved-source raster crop remains an explicit fallback only; the accepted raster mode is `NativeURP`. Temporary renderer/light/ambient state is restored and source importers/materials remain unchanged. The failed black Build04 raster is retained at `Evidence/minimap-build04-before-repair.png`.

Chapter05 folders now participate in canonical mission and scenario discovery, and the generated Citywide physical map is explicitly registered with a duplicate guard. The native refresh receipt reports21 missions and28 maps. Citywide is campaign index20; its launch state uses real access/progression and later Chapter05 nodes remain story previews.

## Packaging and gates

Own content builder registers eight Addressables, builds own entity scene and a fixture with all scenario unit prefabs plus clinic, utility, reserve and Barracks (16 unique expected), and generates packed-content receipt. Normal Entities production build additions include both Citywide scene GUID and fixture GUID, preserving isolated candidate overrides.

Passed: native builder and independent map creation; saved metadata/GUID/local-ID/ownership/source six-SHA checks; combined footprint routes and building lots; native URP minimap; 21-entry mission/scenario catalog; rules and objective validation; 28 comic clips installed for14 lines in English and Persian. Focused validation receipts do not establish player readiness.

`packed-content-wrapper-04.log` passed with zero Burst errors. `normal-input-en-wrapper-05.log` confirms all six actual starting placements succeeded, then failed because binding selected the prepared map owner ID2 instead of the requested utility. Source now excludes OperationMapBuildingComponent and qualifies the entity by exact successful request origin, footprint and prefab key; its binding regression passed in `rules-wrapper-06.log`. Pending: actual resource/production/damage proof; complete English and Persian normal-input/ARIA/comics/results/settlement/return; normal production player build; real device acceptance. Failed build/validation/pack logs and visual evidence remain retained. In particular, `normal-input-en-wrapper-02.log` retains the failed v4 native load: inherited accepted descriptors added111 buildings,22 vehicles and8160 render-only identities to the Dense totals (434/323,22/0,9283/1123); v5/v6 fix ownership and cropping without weakening the SceneTag-scoped readiness checks. No readiness claim follows from source review or packing alone.

## Latest native progress

- `rules-wrapper-06.log`: exact building request binding regression passed. `normal-input-en-wrapper-06.log`: all six owners bound correctly and ten opening/briefing panels played naturally; recruitment failed because actual producer readiness was not projected to the existing Soldiers drawer. Failed native screen `live-production-06.png` remains retained.
- `rules-wrapper-07.log`: producer-readiness regression passed. EN07 reached the existing Soldiers card, but confirmation required10000 Credits/20 Materials despite supplied0 Credits/180 Materials. Citywide now uses the existing DefenseMaterialsV1 canonical30 Materials order and resource initialization dispatch.
- `rules-wrapper-08.log`: Materials and surviving-response fallback regressions passed. `presentation-wrapper-02.log`: existing HUD passed in two locales with zero new buttons.
- `recruitment-wrapper-01.log`: four soldiers were actually produced by supplied Barracks3/faction1, but the observer expected uppercase while the production pipeline emitted canonical `unit_chr_soldier_male_02_alt_04`. Exact normalized-key comparison is corrected; `rules-wrapper-09.log` passed its real-normalizer regression. Recruitment02 is running; no complete journey is established by this partial production result.

Full English manual and Persian ARIA journeys, final result/settlement/return and production player/device gates remain pending. Failed receipts remain retained.

EN08 passed actual recruitment and coverage but lost the utility engineer at hostile release: old forward start `(1206,428)` was within the actual120m tank range and received180 damage. Rear staging and mission-local noncombat engineer behavior are source corrections pending native validation; failed EN08 evidence is retained.
