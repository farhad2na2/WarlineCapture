# Trust Under Fire map preparation

Updated 2026-10-02. Source authoring is complete; native generation, physical qualification and player readiness are pending.

## Approved source and independent ownership

Mission `saga.ch05.m02.trust_under_fire` owns `opmap.ch05.trust_under_fire`, a 600×400 derivative of prepared AshLinePort, world `(400,300)`–`(1000,700)`. Canonical source semantic hash is `3f8bceea706372ba2a6b0584d6e7820c73ce3e40c30c53acdd937d9998cae8d5`. Source native movement/load/damage evidence exists; its device acceptance remains pending and does not certify this mission.

The builder copies the entire native scene so internal owner/visual references remain local, retires legacy accepted descriptors before the Dense identity crop and rejects unrelated authored vehicles. Each surviving gameplay owner requires one Dense gameplay identity. The derivative requires exactly one own Grid/Surface, three roots with exact native readiness totals, no whole-city virtualization, a disabled unbound binding SubScene and explicit own entity-scene delivery. Source maps, materials and importers are preserved.

Six source SHA values were captured in `source-audit-before-authoring.json`; native generation captures fresh before/after receipts and fails on change. Source entity GUID is `1561a8788c3144347bccd1bbe76eb4a7`, binding `fa3ff7d2247cd411a929f4af52c58a6c`, definition `37ac2d9ae9e794d8297f9e6979caa4a8`. All generated assets have independent paths/GUIDs.

## Protected land routes

| Route | Start | Actual crossing | Shelter arrival | Shelter origin / footprint |
|---|---|---|---|---|
| North |470,505|680,505|930,505 /radius6|922,519 /14×12|
| South |470,385|680,385|930,385 /radius6|922,399 /14×12|

Both convoy routes require the full 5×5 footprint along all461 cells, source static/owner exclusion and wheeled/tracked movement. Both crossings retain source BridgeDeck type, elevation and Bridge flag; canal water remains blocked. Relay approach follows `(605,505)`→`(605,610)`→`(535,610)`, certified as a3×3 infantry corridor. Bridge decks are land infrastructure; no naval mechanic is claimed.

The broadcast compound is centered `(535,630)`, actual14×12 owner origin `(528,624)`, with external six-second verification gate `(535,610)`. Original noncombat engineer starts `(605,505)`, safely behind the initial relay threat. Reserve origin `(780,650)` has a26×25 footprint. GameplayEN02 passively qualified this entire exact footprint through unchanged native `IsPlacementValid`; final authored placement remains pending.

The prepared AshLine surface already zeros movement beneath static props. The derivative tracks removed visual coverage plus exact source classified static-footprint provenance, restoring only proven removed-static cells on qualified flat non-water land. Heights, normals, surface types and road/bridge/water flags remain unchanged. Unknown zero movement remains blocked. Retained props still produce static masks; native gates do not erase roads or waive footprint checks.

## Actual owners and protected convoy fallback

Own shelter prefab uses the refugee tent visual, health1000/14×12, with two independently placed health owners. The preserved broadcast uses the satellite-dish visual, health1000/14×12. Actual finite reserve capacity240 has supplied100 Fuel/protected20. Completely unpacked prefabs are reloaded after saving to verify independent ownership, no inherited config, exact health/footprint/Fuel and zero production. Runtime placement requests must prove each actual owner; decorative structures do not satisfy objectives.

The approved readiness fallback uses two named friendly noncombat TruckCanopy convoys moved by normal player commands across separate routes, followed by living six-second shelter holds. Four neutral original shelter staff remain visible/protected. This does not claim civilian selection, passenger boarding, automatic evacuation or a port-capture objective.

Original roster:13 friendlies (two tank+four-rifle escorts, two convoy trucks, one engineer),9 military attackers (APC+two infantry per route, three relay guards) and4 protected staff. Military elements have explicit stationary routes and genuine combat registration; civilian convoys have no automatic patrol. Eight stages retain route protection, convoy arrival, actual combat, verification and final stabilization.

## Presentation, packing and gates

Existing HUD/ARIA controls and approved marker renderer are reused, with bilingual field-guide/status/result copy and no new command buttons. The minimap requires readable native URP capture; black/uniform imagery fails, with approved-source raster crop an explicit fallback. Chapter05 M02 is campaign index21 with paired mission/scenario discovery and explicit own map registration.

Own Addressables registration includes eight assets. The content fixture includes scenario units and all three actual building prefabs. Normal production Entities build additions include own scene and fixture GUIDs; isolated packed content is a separate gate from production player/device acceptance.

`CH05M02TrustUnderFireIntegration.BuildCore()` can generate captions/sequences without voice files; it explicitly reports voices pending. `Finish()` and `ValidateInstalledVoices()` require all14 clips for seven EN/FA dialogue lines. Core Map/Rules/Objectives checks, packed content, complete EN manual/FA ARIA journeys, result/settlement/return and real device acceptance remain pending. Retain failed native receipts.

### Native lot correction (2026-10-02)

BuildCore02 failed at940,519. Exact compact payload inspection found preserved Road/type1, movement15, Road flag1 in both source and derivative: the original shelters crossed the vertical road beginning x940. Both14×12 origins are now922,519 and922,399 (end exclusive936), centers929,525/405; convoy arrival remains930,505/385. Staff staging is930,540/420, protected Support regions begin919. All four lots were inspected together: replacement shelters, broadcast528,624 and reserve815,630 contain only Terrain/type6 and flags0; source static-covered zero-mask cells remain subject to proven removal plus original static provenance before restoration. Removed gameplay owner footprints now contribute to removed-static coverage, matching the physical removal. Native full footprint/combined owner checks and staff/external engineer gate checks remain required; corrected build is pending. No road, water or footprint permission bypass.

### Native reserve relocation (2026-10-02)

GameplayEN02 rejected original815,630:182 live blocker cells, first832,630; cached/startup/Grid/authored road, sidewalk, water and outside counters were all0, and no existing-building overlap was found. The failed receipt is retained. The unchanged native placement survey qualified replacement780,650 for the full26×25 effective footprint. Own lot, fuel/legacy infrastructure anchor and padded physical clear zone now use780,650. Source compact payload inspection confirms all650 replacement cells are Terrain/type6, flags0 (static-covered zero-mask cells must still prove removal before restoration). Unknown original blockers were not erased and no placement gate was weakened. Native rebuilding, map gate, packed content and actual placement remain required. Provisional M02 REVIEW sidebar copy must become established M01–M02 READY after complete native gates; review status belongs in evidence, not final player guidance.
