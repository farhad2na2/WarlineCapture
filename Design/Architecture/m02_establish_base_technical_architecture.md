# M02 Establish The Base Technical Architecture

Date: 2026-08-24
Status: Active architecture authority for Chapter 1 Mission 2
Mission: `saga.ch01.m02.establish_base`
Scenario: `scenario.ch01.m02.establish_base`
Operation map: `opmap.ch01.forward_post_01`

## 1. Purpose

This document extends the accepted Mission 1 Campaign architecture to Mission 2 without creating a second mission framework. M02 teaches one deterministic loop inside the 3D operation map: place the canonical Barracks, spend Credits and Materials, produce one rifle squad, and defend the forward post against one delayed patrol.

The implementation remains ECS-first. Authored ScriptableObject data is projected once into unmanaged runtime state. Existing building placement, production, resource, combat, camera, UI-shell, mission progression, and dense-city loading owners remain authoritative.

## 2. Locked Decisions

1. `Building_Barrack` is the only M02 tutorial producer.
2. M02 grants mission-scoped access to Barracks; first-clear settlement grants its permanent unlock.
3. `Tent_Regular` and `Building_Road_Barrier` are not exposed by the M02 tutorial.
4. Barracks may produce the exact approved rifleline unit needed by the objective. The global Barracks config may expose only the reviewed bounded rifle set; M02 filters to one required unit.
5. The player starts with one rifle squad and enough Credits and Materials for the Barracks plus one required production order, with a small deterministic positive remainder.
6. Fuel and Oil remain hidden or zero. No optional upgrade, exchange, transport, air, or logistics flow is introduced.
7. The enemy patrol becomes active only after the warning boundary. It uses existing ECS combat and command systems.
8. The forward post is a mission role backed by existing building health/destruction truth. No parallel base-health state is allowed.
9. Civilian loss is a star condition, not a tutorial failure condition.
10. Final comic and voice assets are produced only after the playable vertical-slice timing is approved.
11. Android/Samsung certification is deferred. Editor, architecture, source-growth, lifecycle, and performance regression gates remain mandatory.

## 3. Assembly And Dependency Direction

The accepted dependency direction remains:

```text
Game.Missions.Contracts
        |
        v
Game.Configs / Game.Components
        |
        v
Game.Runtime / Game.Systems
        |
        v
Game.UI / Game.Composition / Editor validation
```

- Contract enums and immutable payloads remain in `Game.Missions.Contracts`.
- Authoring schemas remain in `Game.Configs`.
- Unmanaged mission facts and requests remain in `Game.Components`.
- Simulation owners remain Burst-capable `ISystem` implementations where their dependencies permit it.
- Canvas UI reads projections and emits typed requests only.
- No runtime assembly may depend on Editor code or narrative media assets.

## 4. Authored Data Contract

### 4.1 Mission Definition

M02 adds data-supported objective and star rules rather than mission-id conditionals:

- `BuildStructure` for `Building_Barrack`;
- `ProduceUnit` for one approved rifle squad;
- `DefendMissionRole` for the forward post;
- `NoCivilianLoss` for the second star;
- existing `CompleteMission` and `CompleteUnderMilliseconds` for stars one and three.

The mission definition owns sequence ids, objective display keys, allowed tactical commands, replay behavior, reward identities, and readiness feature ids. It does not own live building, production, resource, or combat state.

### 4.2 Scenario Setup

`ScenarioSetupConfig` receives additive, default-safe Campaign fields for:

- starting Credits, Materials, Fuel, and Oil;
- mission-scoped build catalog ids;
- required producer and produced-unit ids;
- forward-post mission role and build anchor;
- delayed defense-wave activation time and route;
- allowed feature restrictions.

Legacy Skirmish assets retain their current defaults. Campaign validation fails closed when required M02 economy/build/base data is absent, ambiguous, duplicated, unavailable, or unaffordable.

### 4.3 Barracks Production

The existing `BuildingDefinitionConfig` remains the sole production catalog source. `Building_Barrack` receives reviewed rifle production entries using the same prefab references already accepted for `Tent_Regular`; M02 then exposes exactly one required rifleline entry through its scenario catalog filter. No duplicate production registry is introduced.

### 4.4 Logical Operation Map

`opmap.ch01.forward_post_01` owns only the logical mission view and exact source binding. Required metadata includes:

- planning and battle camera poses;
- camera bounds and minimap projection;
- player squad spawn;
- command-post/base anchor;
- Barracks build footprint and valid placement surface;
- enemy wave spawn, route, warning, and defense boundary anchors;
- civilian edge/safe route anchors;
- narrative/comms focus anchors.

Accepted physical city, baked ECS presentation, virtualized renderer database, Addressables ownership, and rollback packages remain read-only. A logical view may reuse accepted physical content only through the existing exact identity/hash binding contract.

## 5. Runtime Ownership

### 5.1 Mission State

`CampaignMissionRuntimeSystem` remains the only semantic mission phase/outcome writer. Its logic becomes definition/fact driven for M01 and M02 rather than adding a second mission runtime system.

The existing attempt-facts component is extended additively with monotonic M02 facts:

- required Barracks placed/completed;
- required rifle production queued/completed;
- forward-post spawned/alive/damaged;
- defense wave activated/remaining;
- civilian total/lost;
- elapsed milliseconds.

Facts are sampled from authoritative ECS building, production, resource, unit health, and mission-role data. They are not written by UI, tutorial, narrative, or presentation systems.

### 5.2 Building And Production

Existing building systems retain ownership of:

- placement validation and confirmation;
- resource affordability and spend;
- construction/runtime building creation;
- production queue validation and spend;
- produced-unit spawning and read models;
- building health and destruction.

M02 may add a thin read-only projection into mission facts and mission-scoped catalog/restriction filtering. It may not fork these systems, create a tutorial-only producer, or bypass resource/placement validation.

### 5.3 Delayed Defense Wave

Scenario-authored enemy entities may be created during deterministic mission spawn, but movement, targeting, combat, and minimap hostility must remain suppressed until the authored activation boundary. Activation is a one-shot unmanaged command/state transition. No managed coroutine, GameObject enable loop, or per-frame hierarchy search is permitted.

### 5.4 Objectives And Settlement

`CampaignMissionObjectiveProjectionSystem` remains the only objective projection writer. It projects the definition plus authoritative facts for both M01 and M02.

Existing Campaign settlement remains idempotent and owns first-clear rewards, best stars, replay records, Barracks permanent unlock, and M03 unlock. Retry reconstructs the same scenario seed and does not persist failed-attempt resource spend.

## 6. Guidance And UI

M02 uses distinct typed guidance steps:

1. notice the forward-post objective;
2. open Build;
3. select Barracks;
4. place it on the valid footprint;
5. observe Credits/Materials spend;
6. queue the required rifle squad;
7. prepare for the warning;
8. defend until production and the wave resolve;
9. continue to result/debrief.

`SHOW ME` publishes a bounded focus/highlight request for the current typed target. `DO IT` may emit the same validated command a player can issue; it may not mutate mission facts directly or skip resource, placement, production, or combat rules.

UI restrictions derive from the active scenario. Only relevant resources, Barracks, required rifle production, permitted tactical commands, objectives, warning, result, and tutorial surfaces are visible. Existing no-Campaign/Skirmish defaults remain unchanged.

## 7. Narrative And Media

The sequence contract is:

- `seq.ch01.m02.brief`: abandoned forward post, Dalia's restore/defend direction, clinic-route civic purpose;
- `seq.ch01.m02.comms`: recovered municipal access list, confirmed stolen before the attack;
- `seq.ch01.m02.debrief`: post operational, Dalia accepts field-lead role, warning sector goes dark before M03.

Storyboard copy and shot ids are authored before gameplay. Final comic panels and English/Farsi voice assets are generated only after the playable timing gate. Displayed text and spoken text must match semantically and use the established character voices.

## 8. Performance And Lifecycle

- Match hot paths target `0 B/frame` managed allocation after warmup.
- No structural changes occur inside entity iteration; use the established command-buffer or bounded owner boundary.
- Mission-fact sampling uses cached queries and unmanaged data.
- Build/production/objective/tutorial projections update on version or state change, not by rebuilding managed collections every frame.
- Retry, replay, exit, menu return, World disposal, and repeated M01/M02 runs must not leak entities, presentation owners, handles, or persistent mission state.
- The M02 representative performance route includes placement, active production, delayed wave activation, combat, and result transition.

## 9. Validation

Every implementation boundary runs:

- focused M02 contract/data behavior tests;
- affected existing building/production/resource/combat tests;
- M01 regression tests for shared mission owners;
- architecture and source-growth suites;
- lifecycle/retry/replay tests;
- performance/allocation tests for changed hot paths;
- graphics-enabled multi-aspect Editor capture for map, placement, production, defense, and result;
- project-owner gameplay review before final comic/audio production.

## 10. Explicit Non-Goals

- no new runtime city generator;
- no managed runtime map visuals;
- no GameObject gameplay owner or permanent-map pooling;
- no second mission/objective/progression/build/production/resource system;
- no M03 implementation;
- no optional M02 upgrades, logistics, air, transport, Oil/Fuel loop, or Road Barrier;
- no mutation of accepted dense-city physical content or production Addressables ownership;
- no Android certification claim while the owner-deferred gate remains open.
