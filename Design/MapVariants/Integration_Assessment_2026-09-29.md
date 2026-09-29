# Map variant integration assessment

Date: 2026-09-29. Status: recommendation, not an approved migration or runtime certification.

Use the three medium maps as new physical map sources. Start with Refinery District / CH02-M02 Supply Line as one complete pilot. Recommend migrating seven existing missions in total, while retaining the urban setting for eleven. Defer Frontier until performance and a mission requiring its scale justify it.

## Evidence and scope

Reviewed HANDOFF_Integration.md, all four layout JSONs and stored placement audits, top-down playable captures for all four maps, and battle captures for refinery supply/fork, port bridge, airfield helipads and Frontier refinery. Compared these with the campaign high-level design catalog, current mission/map/scenario assets, variant builder code, mission config builders, Supply Line runtime anchor consumption, and Addressables builder.

There are **18 current MissionDefinition assets**, not the handoff's 17: five each in chapters 1–3, and three in chapter 4. Their 18 chapter map definitions all reference `opmap.skirmish.desert_base_01` and the same authored subscene GUID. The high-level campaign plan contains 25 missions; the remaining seven are future authoring work, not seven more existing maps to migrate. Asset existence does not establish mission readiness.

The four stored audits report zero placement issues, with 11,066 placements for Refinery District, 9,492 for Ash Line Port, 11,084 for City-Edge Airfield and 25,678 for Frontier. These are placement records, not measured draw calls or total runtime entity counts. No Unity build, new placement audit, normal-input playthrough or device measurement was run during this assessment. No gameplay or map assets were changed.

## Recommended mission allocation

| Existing mission | Decision | Map / sector | Required mission work |
|---|---|---|---|
| CH01-M04 Airlift | Migrate | City-Edge Airfield: hospital, approach, helipads | Preserve rescue → APC boarding/movement → secure LZ → helicopter extraction; replace decorative aircraft at active pads, reserve boarding space, retune deadlines and camera guidance. |
| CH02-M02 Supply Line | Migrate first | Refinery District: pumps, refinery gate, storage | Place actual Oil/refinery/Fuel entities, two working hauling legs, a usable alternate lane and civilian reserve; rebalance travel and attack timings. |
| CH02-M04 Power Relay | Migrate | Refinery District: substation and service yard | Make the civic power objective distinct from the oil mission; place repair/defense roles and safe access routes around the substation. |
| CH02-M05 Route Reopened | Migrate | Ash Line Port: hub, canal, both crossings | Preserve simultaneous relief logistics and hub assault; give both lanes useful gameplay, protect routing records, verify truck crossings and breach access. |
| CH04-M01 Air Corridor | Migrate | City-Edge Airfield: tower, runway approaches | Keep radar and automatic G2A coverage central; author air routes, build footprints, warning lead time and ground threats. A tower-hold objective alone would not preserve this mission. |
| CH04-M02 Steel Push | Migrate | Refinery District: depot and outer approach | Add a tank-friendly defensive sector, readable armor approaches, reserve and Relay endpoint; preserve finite military Fuel and protected civilian reserve. Use a different direction and footprint from Supply Line. |
| CH04-M03 Split Front | Migrate last | Refinery District: depot, fork, ridge | Re-author both fronts, launcher preparation/range, target confirmation, civilian exclusion, cancellation and diversion timing. This is substantial encounter rework. |

This is **six strongest direct fits plus Steel Push**, for seven recommended migrations. All seven require layout changes and fresh acceptance; none is a scene-only substitution. Supply Line, Route Reopened and Split Front carry particularly substantial spatial/mechanical rework. The others still need balancing and complete replay.

Retain these eleven on their current urban source for this migration: CH01-M01, M02, M03 and M05; CH02-M01 and M03; all five CH03 missions. Urban continuity serves their forward-post, clinic, market, residential intelligence and evidence objectives. This does not mean those missions have no unrelated remediation or art needs.

An optional second expansion could move CH01-M02 and CH01-M03 together to the airfield's city-edge forward post and its approach, taking the migration total to nine and leaving nine on the urban source. Moving Radar Warning alone to the port would weaken its continuity with the post established in M02 and risk changing base defense into bridge defense. The handoff's “Convoy Approach,” “Forward Post,” and “Supply Yard” are map/zone labels, not replacements for canonical mission names or contracts.

For future missions, City-Edge Airfield is a strong candidate for CH04-M04 Grounded Signal, after making the runway operational. Frontier is a possible later foundation for CH04-M05 Armor Break or a bounded section of CH05-M04 Last Corridor. CH05-M01 would need distinct civic fronts; Frontier's scale alone does not supply them. CH05-M05 still needs a recognizable Civic Relay complex. Do not force all future missions onto these industrial themes.

## Visual and layout changes

The captures establish distinct refinery, canal-port and airfield silhouettes. That is useful campaign variety. However, the repeated tank rows, container grids, identical parked vehicles and evenly scattered props still look procedurally arranged, while large aprons leave little authored tactical structure.

- Reduce repeated props inside the active camera frame. Keep background density around the mission rather than across every playable space. Reserve stronger color accents for useful landmarks and player-readable objectives.
- Give each encounter a clear arrival, objective, threat direction and alternate route. Size clear lanes and turning areas against actual squads, haulers, APCs and armor; placement non-overlap does not establish navigability.
- Keep tall refinery columns away from the foreground of critical selection/targeting locations. Inspect occlusion with live units, markers and the real HUD at mobile size.
- Replace relevant decorative trucks, helicopters, launchers, pumps and stores with gameplay entities; do not leave visually identical inert objects beside interactable ones. Preserve staging space for production exits, boarding and extraction.
- For the port, strengthen bridge approaches, quay edges and route distinction. Water/shoreline polish comes after traversable crossings.
- The airfield top-down shows separated runway strips and wreckage nearby. Create a continuous clear movement/landing strip if it will support operational planes, especially Grounded Signal; retain wreckage outside operational clearance.
- Use different mission crops and attack directions on reused maps. Four refinery missions should not all replay the same east–west road.
- Choose one runtime lighting/volume authority and verify contrast against existing campaign UI. Do not assume the Demo lighting used by captures survives mission loading.

## Corrections needed in the handoff's technical recipe

1. **Reserved is an art-placement reservation, not a gameplay blocker.** `PlaceOptions.Vegetation` sets `Reserve = true`, and DesertDressing uses it for grass, pebbles and sand edges. Export explicit gameplay semantics such as movement blocking, build exclusion, visual-only, walkable surface and destructible ownership. Classify trees and large rocks intentionally too. Converting all reserved footprints would obstruct decorative ground.
2. **Ground height is insufficient at bridges.** The port layout records both bridge anchors at y = -4, while AddBridge supplies a deck level of 0. Export `BridgeCells` deck heights and compose the movement surface from ground plus walkable overrides. Sample anchors from the final playable surface, including the deck's authored offset, instead of the canal bed. Preserve water blocking outside the crossing.
3. **Medium-map world bounds do not entirely fit the runtime grid.** Their playable rectangles (400,300)–(1000,700) fit; their 1400×1200 world extends beyond the grid's z = 1024. Separate render-only backdrop from simulation and fail closed outside the active playable mask. Frontier needs the (-176,0,-176) conversion applied consistently to presentation, heights, blockers, roads, zones, minimap and camera data; moving only the root leaves exported data misaligned.
4. **Suggested zones are not validated mission bounds.** A static rectangle check finds seven zones extending beyond their medium map's playable rect: Port's RouteReopened_Hub, SupplyYard_East and Anchor_EastCheckpoint; Refinery's SplitFront_Ridge; Airfield's Airlift_Helipad, AirCorridor_Tower and Anchor_CityGate. Crop or expand intentionally, validate full footprints and paths, and provide deployment/camera margins. Frontier's listed zones pass that rectangle check.
5. **Registration requires tooling work.** OperationMapAddressablesLayoutBuilder contains desert-base-specific paths, addresses and group names. Mission config builders also write fixed coordinates and clone existing maps. Add a variant-aware source descriptor/build path and update mission generators; editing generated YAML alone risks being overwritten on the next rebuild.

The existing 600×400 medium playable region is much larger than some current mission windows. Supply Line is currently 165×80, and Split Front / Steel Push are 255×80. Preserve encounter pacing with bounded sectors rather than spreading existing units across the whole new map. The existing Supply Line spawn runtime already resolves semantic anchor IDs, which offers a useful reuse seam.

## Integration sequence if adopted

1. Agree on the pilot's mission footprint and visual direction using the real campaign framing. If implementation includes a substantial mission HUD/screen redesign, follow AGENTS.md: ImageGen mockups using actual Campaign references and user visual review before that implementation. Preserve visible objectives, next action and ARIA Play/Stop.
2. Extend deterministic variant output with versioned placement semantics, ground/bridge/pad surfaces, road topology, bounds, coordinate conversion and stable anchor IDs. Keep scene rendering and gameplay data generated from the same source.
3. Generate a new physical Refinery source: authoring scene, compatible entity-presentation subscene, grid/surface assets, static render data, deliberate gameplay building/vehicle ownership, runtime binding scene, minimap and content hashes. Reuse the dense-city pipeline architecture, but audit its map-specific assumptions. Use the project's surface/grid navigation pipeline; missing NavMesh in a visual prototype does not automatically require adding Unity NavMesh.
4. Add distinct physical source/catalog/Addressables entries with independent hashes. Keep the old dense-city source intact. Use a new logical map ID for the candidate mission while retaining the canonical mission and scenario identities, progression and reward contracts. Keep the previous binding available for rollback and handle stored map IDs where relevant.
5. Update the pilot's config/environment generators, semantic anchors, spawn footprints, objective entities, hauling routes, guidance/cameras, bounds and minimap. Recompute costs/timing only where changed movement requires it. Audit probes/tests for old coordinates and source assumptions. Ensure there is exactly one active map presentation and one gameplay owner for each functional structure.
6. Validate the pilot completely, then reuse the exporter/source tooling for Airfield and Port. Suggested mission order: Supply Line → Power Relay → Airlift → Air Corridor → Route Reopened → Steel Push → Split Front. Finish one representative mission before scaling the migration.
7. Retain old compatibility tests and add new-source identity, surface, path connectivity, spawn clearance and packed-load/unload parity checks. Update intentionally migrated mission expectations explicitly; do not simply loosen frozen hashes. Exercise restart, mission switching and return to prevent leaked entities or lighting.
8. Run Unity validation/captures through `rtk proxy Tools/CI/invoke_unity_macos.sh`, with explicit timeout/log and required pass markers, Hub open and signed in, and no batchmode. Keep failed evidence. Then complete normal-input manual and ARIA paths, result and return for every migrated mission, followed by real device/player acceptance.

Performance acceptance must measure load time, peak memory, frame time, rendering and movement with actual mission units and HUD on target devices. Start with existing project/device budgets. Profile the medium maps too; object counts alone cannot certify them. Frontier should wait for an instancing/entity-rendering and culling plan backed by those measurements.

## Readiness gates

| Gate | Status in this review |
|---|---|
| Visual assessment | Existing captures inspected; proposed production changes above; user acceptance of the migration remains pending. |
| Automated validation | Stored placement audits inspected; static asset count/source-binding and zone-rectangle checks performed. No new Unity or packed-runtime validation. |
| Normal-input mission completion | Not established on any variant. Required for each migrated mission, including ARIA, result and return. |
| Real player/device acceptance | Not measured or established for any variant. |

## Source locations

- `Design/MapVariants/HANDOFF_Integration.md`
- `Design/Campaign_Mission_High_Level_Design_Catalog.md`
- `Assets/Game/Art/MapPrototypes/Variants/*/MapVariant_*.layout.json`
- `Assets/Game/Scripts/Editor/MapPrototypes/Variants/MapVariantBuilder.cs` (reservation and placement semantics)
- `Assets/Game/Scripts/Editor/MapPrototypes/Variants/MapVariantBuilder.Compose.cs` (DesertDressing)
- `Assets/Game/Scripts/Editor/MapPrototypes/Variants/MapVariantBuilder.Roads.cs` (BridgeCells and deck creation)
- `Assets/Game/Scripts/Editor/CH02M02SupplyLineConfigBuilder.cs`
- `Assets/Game/Scripts/Editor/CH04M02SteelPushConfigBuilder.cs`
- `Assets/Game/Scripts/Editor/CH04M03SplitFrontConfigBuilder.cs`
- `Assets/Game/Scripts/Editor/OperationMapAddressablesLayoutBuilder.cs`
- `Assets/Game/Scripts/Runtime/Missions/CampaignMissionSpawnSystem.SupplyLine.cs`
- `Assets/Game/Configs/OperationMaps/Chapter01` through `Chapter04`
