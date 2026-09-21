# Demo 2 asset reuse across Campaign, Operations and Skirmish

Date: 2026-09-21. Owner: environment art / mode content owners.
Status: **Selected for planned reuse; production integration and acceptance pending.**
Inspection baseline: `950297fcc` plus the current working tree. This review changes documentation only.

## Decision

Implementation companion: follow the [asset integration guide](Demo2_Asset_Integration_Guide.md) and its [23-prefab planning manifest](VisualConfigs/Demo2_Environment_Asset_Manifest.json). They define project-owned output paths, authoring steps, ownership/bake rules, D2-A01–A04 delivery packages and D2-V1–V6 evidence gates. The manifest records source availability separately from art/runtime acceptance.

Use selected assets from `Assets/Game/Scenes/Demo2.unity` as a complementary infrastructure and military environment kit. Keep the desert/Middle Eastern identity of Sahrin, its established architecture, terrain and gameplay catalog. Modern warehouses, electrical equipment and reinforced checkpoints make its newer districts more credible and visually distinct.

Prioritize logistics/utility props, industrial buildings and perimeter dressing. Adapt residential architecture selectively. Defer playable vehicles, characters and weapons to separate catalog integration. Do not import the demo scene wholesale or treat a model as a functioning gameplay feature.

This plan refines art sourcing only. The narrative bible, mission contracts, operation-map identity rules, [Skirmish expansion](Roadmap/Skirmish_Expansion/PLAN.md), and [Operations plan](Roadmap/Operations/PLAN.md) retain authority over gameplay, scope and readiness. No additional maps, missions, scenario IDs or unlocks are introduced here.

## What was inspected

- Demo 2's serialized scene contains **1,533 direct prefab instances across 165 distinct source prefabs**: 164 Polygon Battle Royale prefabs and one Polygon Generic skydome. See the [resolved inventory](AgentReports/Demo2AssetReview/prefab_inventory.json). These are source-instance counts, not draw calls or unique mesh counts.
- Inspected the central scene in a temporary isolated Unity preview: paved junctions, low residential blocks, warehouses, container yards, a military compound, bridges, a plane wreck and green island terrain. The warehouse was also inspected in the asset Inspector. Preview atmosphere/large backdrop meshes were temporarily hidden for inspection; this was not a production lighting comparison.
- The running Match remained in Play mode and paused. The preview was closed without saving; no source scene, prefab, material or gameplay configuration was changed.
- Read representative imported prefabs through Unity Pipeline. The project already has the pack and its imported mesh/material assets; no new pack import is needed for this shortlist.

| Sample | Mesh vertices summed across MeshFilters | Renderers | Colliders | LODGroups | Material shader |
|---|---:|---:|---:|---:|---|
| Battle Royale warehouse | 7,157 | 1 | 1 | 0 | `Synty/Generic_Basic` |
| Battle Royale transformer | 3,830 | 1 | 1 | 0 | `Synty/Generic_Basic` |
| Battle Royale bridge 01 | 1,144 | 1 | 3 | 0 | `Synty/Generic_Basic` |
| Battle Royale USA tank 01 | 13,427 | 23 | 3 | 0 | `Synty/Generic_Basic` |
| Existing Polygon Military refugee tent | 5,461 | 1 | 1 | 0 | `Universal Render Pipeline/Lit` |

These five samples had no MonoBehaviour scripts. Numbers describe imported prefab structure, not visible triangle count, instancing efficiency, gameplay readiness or measured device performance. In particular, low-poly appearance does not make the 23-renderer tank an inexpensive mass-unit replacement. Full dependency, missing-reference, scale, shader-variant, navigation and Android acceptance remain unmeasured.

## Asset selection

Paths below are relative to `Assets/Synty/PolygonBattleRoyale/Prefabs/`. Every named prefab is present in Demo 2's resolved inventory. Reuse project-owned variants/wrappers, preserving vendor source assets.

| Priority / family | Verified examples | Planned use and limits |
|---|---|---|
| First: logistics | `Buildings/SM_Bld_Warehouse_01.prefab`, `Props/SM_Prop_Container_01.prefab`, `Props/SM_Prop_Pallet_Loaded_01.prefab`, `Props/SM_Prop_Crate_Medical_01.prefab` | Freight yards, relief depots and supply objectives. Warehouse is a visual shell; production/storage/health belongs to existing game authoring and configs. |
| First: utilities/comms | `Props/SM_Prop_Generator_01.prefab`, `Props/SM_Prop_Transformer_01.prefab`, `Props/SM_Prop_RadioTower_01.prefab`, `Props/SM_Prop_Wirespool_01.prefab` | Repair sites, substations and verified relay targets. No automatic electrical-grid or radar mechanic. |
| First: perimeter | `Props/SM_Prop_Barrier_01.prefab`, `Props/SM_Prop_BaseWall_01.prefab`, `Props/SM_Prop_WireFence_01.prefab`, `Props/SM_Prop_GuardTower_01.prefab` | Checkpoints and military yards; preserve readable openings and vehicle clearance. A guard-tower mesh does not receive targeting/combat implicitly. |
| Conditional: crossings/quays | `Environments/SM_Env_Bridge_01.prefab`, `SM_Env_Bridge_02.prefab`, `SM_Env_Bridge_Broken_01.prefab`, `SM_Env_Port_Concrete_Slab_01.prefab`, `SM_Env_Port_Wall_01.prefab` in the same folder | River routes and quay dressing. Usable bridges require measured deck width, ramp joins, collision/navigation and convoy passing. Broken bridge is authored scenery unless a supported objective explicitly owns its state. |
| Conditional: roads/damage | `Environments/SM_Env_Road_Straight_Damaged_01.prefab`, `Vehicles/SM_Veh_Car_Destroyed_01.prefab`, `Vehicles/SM_Veh_Truck_Destroyed_01.prefab` | Sabotage aftermath and route obstructions. Resolve visual blockage, logical occupancy, targetability and clearance together. |
| Conditional: buildings/camps | `Buildings/SM_Bld_SmallBuilding_01.prefab`, `Buildings/SM_Bld_House_01.prefab`, `Buildings/SM_Bld_Tent_01.prefab` | Utility outbuildings and selected newer neighborhoods. Review roofs, facades, proportions and signage against Sahrin; retain established desert homes/markets as the default. |
| Deferred: roster | `Vehicles/SM_Veh_Armored_Car_01.prefab`, `Vehicles/SM_Veh_Tank_USA_01.prefab`, `Vehicles/SM_Veh_Boat_01.prefab`, weapon props | Wreck/static display use can be assessed separately. New playable variants require catalog identity, rig/turret/wheel setup, ECS conversion, team readability, role, balance and performance acceptance. No naval expansion is implied. |

Do not carry over the green grass coverage, temperate trees, cloud/skydome setup, water treatment, demo cameras, lighting, volumes or loot styling by default. Keep useful rocks/ground pieces only after matching the desert palette and surface contract. Paved roads and modern infrastructure are compatible with the setting; a green island theme is a separate biome decision.

## Campaign placement

Preserve all mission IDs, clues, objectives and feature-readiness fallbacks. These are art-source assignments for future authoring, not claims of completed mission content.

| Campaign content | Asset role |
|---|---|
| Chapter 1 / current M01–M05 | Retain the established desert art baseline; no retrofit required by this review. |
| CH02-M02 Supply Line and CH02-M05 Route Reopened | Warehouse/container/pallet kit for freight and supply yards; generators as local service dressing. Refinery gameplay and specialist refinery assets still come from the existing catalog. |
| CH02-M03 Market Lifeline | Freight/medical crates at the exchange-yard edge; preserve local market architecture. |
| CH02-M04 Power Relay | Transformer, generator, wire spool and service enclosure form a recognizable substation cluster around existing objective anchors. |
| CH02-M01 Gridlock | Optional later damaged-road/wreck dressing pass; preserve the concurrent implementation and its obstruction/crew/route contracts. |
| CH03-M01 Signal Trace and CH03-M05 Network Break | Radio/service equipment and perimeter pieces distinguish relay infrastructure. Do not use a radio-tower silhouette alone to mark hostility. |
| CH04-M01 Air Corridor and CH04-M04 Grounded Signal | Perimeter barriers, radio equipment, supply warehouses and containers support newer Vanguard/air-support compounds. Existing validated runway and aircraft infrastructure remains authoritative. |
| Chapter 5 | Reuse the accepted logistics, utility and perimeter modules to connect the final districts visually to earlier chapters; no new art-driven mechanic. |

## Operations placement

Keep the six-district, 60-mission scope and existing O-IDs. Art availability does not certify any Operations mission.

| Existing district | Planned sourcing |
|---|---|
| D01 Old Quarter / O001–O010 | Small aid/checkpoint props only; established desert streets and homes dominate. |
| D02 Civic Center / O011–O020 | Electrical/service equipment and restrained road barriers; demo houses do not replace civic landmarks. |
| D03 Industrial Belt / O021–O030 | **Primary adoption:** warehouse, container, pallet, generator and transformer modules. |
| D04 River Crossing / O031–O040 | **Primary geometry candidate:** bridges, quay slabs/walls and freight props. Provide the two independent land crossings and quay road required by the briefs. Boats remain scenery; neither bridge collapse nor naval mechanics is added. |
| D05 Highland Approach / O041–O050 | Small radio/service compounds, fences and barriers; preserve arid terrain and independently traversable routes. |
| D06 Airport Perimeter / O051–O060 | Supply yard/perimeter/comms kit. Retain existing hangar/runway assets; Demo 2's crashed plane is optional authored scenery, not operational aircraft infrastructure. |

## Skirmish placement

Keep five maps, S001–S120, prototype/save identities and the existing gameplay delivery order. Replacing art does not create another battle or certify an expanded scenario.

| Existing map | Planned sourcing |
|---|---|
| DB Desert Base | Sparse crates/barriers/generator dressing; retain the recognizable desert baseline. |
| CC City Crossroads | Newer service-yard accents and restrained damaged-road/wreck pieces; retain city identity. Existing floating-ground defect still requires correction. |
| MP Mountain Pass | Compact checkpoint/radio compounds, optionally a tested crossing where the authored route needs one. No new bridge chokepoint requirement. |
| IB Industrial Basin | **First reuse pilot:** one warehouse/logistics/utility module on a candidate yard. Protect main freight route, service ring, infantry lane, supply pads and producer exits. |
| AP Airfield Plains | Perimeter, supply and communications dressing around the already-required runway/landing infrastructure. |

## Integration and acceptance

1. Environment art owner creates a small candidate module: warehouse, containers/pallets, generator/transformer and barriers beside existing desert buildings and representative infantry/APC/tank. Record exact source GUIDs and dependencies; confirm existing project asset entitlement/provenance before shipping. Imported presence alone is not license evidence.
2. Match scale, warm terrain/palette, surface roughness, weathering, signage and faction markings through project-owned variants. Inspect minimum/maximum gameplay zoom with the real HUD, selection outlines and objective markers. Avoid excessive small props, tall silhouettes hiding units, and baked faction/country markings inconsistent with the fictional setting.
3. Map owner defines each asset as decorative, blocking or gameplay-owned. Check collider suitability, footprints, cover/line-of-fire behavior, road/ramp seams, heavy-vehicle two-way travel, convoy alternatives, building/delivery exits and objective access. Bind behavior through existing config/ECS owners, never vendor prop scripts or names.
4. Integrate through the current operation-map presentation/baking/Addressables path. Share dependencies deliberately; do not bundle Demo 2 or its full texture/scene closure. Audit texture residency, material variants, missing references and shader support on Android. Add LOD/culling/instancing or simpler presentation where measured costs require it.
5. Compare the candidate with the desert baseline under identical camera, unit load and lighting. Record added renderer/material counts, CPU/GPU time, memory, loading and package size against existing project performance budgets. Recheck map hashes, minimap/preview, navigation, manual play and the affected mode's ARIA acceptance after geometry changes. Existing successes do not transfer automatically.
6. Accept the small module before spreading it to Campaign/Operations layouts. If visuals or cost fail, retain the existing desert asset in that role and revise the candidate. The adoption plan is useful now; runtime-ready status requires the evidence above.

Verification for this documentation change: source GUID resolution, representative live prefab inspection, temporary scene visual review, local path/link checks and diff whitespace checks. No gameplay tests, Android performance certification or production lighting acceptance were run.
