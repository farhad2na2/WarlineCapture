# Campaign map reuse policy for Skirmish and Operations

Owner direction: **2026-10-03. Adopted planning requirement.** This update changes documentation and authoring scope only; it does not rebind runtime content or certify a mission.

## Mandatory scope

Build the 120 Skirmish scenarios and 60 Operations missions on **current Campaign physical maps**. These are mission counts, not unique-map counts. Retain five Skirmish logical map groups, six Operations district identities and their stable catalog IDs. Multiple missions and logical groups may share one physical source.

This policy supersedes conflicting map-authoring instructions in the September 21/29 plans, handoffs, work packages, generated packets and Demo 2 sourcing assignments for these two modes. In particular, agents must not create mountain/highland terrain, larger industrial/airfield derivatives, additional runway compounds or new district environments to satisfy an old brief. Campaign mission rework and separately requested map preparation retain their own scope; a Skirmish/Operations mission task does not authorize that work.

## Allowed mission authoring

- Select an existing Campaign source and pin its actual definition path, source identity/content hash, runtime scene reference, surface/grid references and coordinate frame. Verify the current working tree before using this inventory; a filename or logical map ID alone is insufficient.
- Create logical map/layout configuration assets referencing that unchanged physical source. Configure bounded sectors, cameras/minimap, typed anchors, route waypoints, capture/exit zones, spawns, finite enemy packages and mission objectives.
- Place scenario-owned gameplay actors and required functional facilities on existing legal pads, using existing certified assets and one authoritative owner per entity. This is gameplay setup, not permission to build a new scenery district or procedural town. Do not duplicate existing scenery as a second functional building.
- Bind routes to existing connected walkable/drivable geometry. Adapt proposed compass orientation, landmark aliases, environment prose and planning envelope to the selected map; show actual geography in player copy. Internal `MP`/`highland_approach` IDs do not require constructing mountains.
- Rebuild mission metadata, overlays or packaging when required by an existing pipeline, preserving physical geometry and source ownership. A generated binding/config artifact is not a newly designed map.

## Outside a mission assignment

Do not create or extend terrain, road networks, bridges, runways, decorative environment compounds, environment scenes or physical map derivatives. Do not regenerate a shared Campaign world, clear scenery to manufacture a route, or rescale terrain/units/buildings to satisfy an old envelope. A terrain defect remains recorded; choose a valid sector/source or refer its repair to separately authorized map work.

New physical map design requires an **explicit owner request**. An older plan's map-builder ticket, asset-kit assignment, missing source or failed clearance check is not that request.

## Current Campaign source inventory

Source audit on 2026-10-03: 25 Campaign mission definitions reference 25 logical mission maps and 11 physical source scene bindings. This is an inventory of existing assets, **not a claim that all maps or missions are accepted on a target device**. Seven entries below are already implemented Campaign derivatives; reusing those existing derivatives is allowed, creating another derivative is not.

Paths below are relative to the repository root. Resolve through the current Campaign mission definition and actual scene/hash references; never select a source merely by matching `operationMapId`, which has compatibility/candidate variants.

| Existing physical source | Campaign consumers | Physical definition to inspect |
|---|---|---|
| Shared desert/dense-city world (`opmap.skirmish.desert_base_01`) | CH01-M01/M02/M03/M05; CH02-M01/M03; CH03-M01–M05 | `Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset`; existing compatibility packaging uses the same authored subscene |
| CityEdgeAirfield | CH01-M04; CH04-M01 | `Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate/Definition.asset` |
| RefineryDistrict | CH02-M02/M04; CH04-M02/M03 | `Assets/Game/GeneratedOperationMaps/Variants/RefineryDistrict/Candidate/Definition.asset` |
| AshLinePort | CH02-M05 | `Assets/Game/GeneratedOperationMaps/Variants/AshLinePort/Candidate/Definition.asset` |
| Grounded Signal | CH04-M04 | `Assets/Game/GeneratedOperationMaps/CH04M04GroundedSignal/Definition.asset` |
| Armor Break | CH04-M05 | `Assets/Game/GeneratedOperationMaps/CH04M05ArmorBreak/Definition.asset` |
| Citywide Alert | CH05-M01 | `Assets/Game/GeneratedOperationMaps/CH05M01CitywideAlert/Definition.asset` |
| Trust Under Fire | CH05-M02 | `Assets/Game/GeneratedOperationMaps/CH05M02TrustUnderFire/Definition.asset` |
| Network Collapse | CH05-M03 | `Assets/Game/GeneratedOperationMaps/CH05M03NetworkCollapse/Definition.asset` |
| Last Corridor | CH05-M04 | `Assets/Game/GeneratedOperationMaps/CH05M04LastCorridor/Definition.asset` |
| Command Node | CH05-M05 | `Assets/Game/GeneratedOperationMaps/CH05M05CommandNode/Definition.asset` |

Frontier's separate prototype/preparation candidate is not an extra approved Campaign source merely because it exists. The implemented Armor Break source can be evaluated directly without creating another Frontier derivative.

## Mode source selection

These are first sources to assess, not automatic capability certification. Another existing Campaign source in the inventory may be selected when it better satisfies the mission. Pin the final choice per logical layout; avoid per-S-ID source/ARIA switches.

| Logical group | First existing sources to assess |
|---|---|
| Skirmish DB | Shared desert/dense-city world; preserve S004's current CityEdgeAirfield binding |
| Skirmish CC | Shared city sectors; Citywide Alert / Network Collapse where their current geometry fits |
| Skirmish MP | Existing Armor Break approach sector; keep the internal MP ID and adapt mountain/valley prose to actual geography |
| Skirmish IB | RefineryDistrict; existing Armor Break sector if more usable space is required; preserve S073 |
| Skirmish AP | CityEdgeAirfield / Grounded Signal; use existing air infrastructure and assess actual capacity |
| Operations D01 | Existing Old Quarter/shared city binding; preserve coded intro bindings |
| Operations D02 | Citywide Alert / Command Node / shared civic sectors |
| Operations D03 | RefineryDistrict / existing Armor Break logistics sector |
| Operations D04 | AshLinePort / Trust Under Fire, using existing crossings only |
| Operations D05 | Existing Armor Break approach sector; adapt highland/switchback prose to measured existing routes |
| Operations D06 | CityEdgeAirfield / Grounded Signal |

## Source fit and acceptance

Preserve objective predicates, protected entities, distinct route choices, army profiles, resource/force budgets, deadlines, Support/product policies and saved identities. Older numeric map envelopes, mountain silhouettes, extra roads and paired-runway designs are superseded authoring suggestions; they do not justify geometry work. Record actual usable bounds and measured travel/capacity instead.

If a source cannot support the complete mission or an exposed size/profile, try another existing Campaign source. If none fits, record the failed clearance/capacity evidence and the affected source-fit gate; do not silently remove required units, counters, routes or objectives, expose an unsupported configuration, or add a new map. A gameplay-contract change needs an explicit content revision; new map design needs an explicit owner request. Continue unaffected mission work.

Preserve existing coded S001/S025/S073/S002/S003/S004 and O001 bindings/evidence; inspect current code before treating other entries as uncoded. Do not force historical O002/O003 model fixtures to become shipping map changes. Source reuse never imports Campaign garrisons, resources, production state, rewards or session entities.

Each released mission still requires source/metadata validation, native visual review, full normal-input manual and ARIA journeys, result/return, checkpoint/recovery and separate real player/device acceptance. Shared-source readiness, catalog status, compilation and injected outcomes do not establish mission readiness.
