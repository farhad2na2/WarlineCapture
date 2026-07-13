# WarlineCapture Field Fabrication And Materials Design

Date: 2026-07-12
Status: High-level design source of truth

## Purpose

This document gives the currently nonfunctional `Building_Ammunition_Depot` a tactical economy role. The building becomes the player-facing **Field Fabrication Depot**: it receives Oil and converts that Oil into Materials used for battlefield construction, repair, and infrastructure.

The resulting logistics choice is:

```text
Oil Pump -> tray truck -> Oil Refinery -> Fuel -> vehicle and aircraft mobility
                     \-> Field Fabrication Depot -> Materials -> construction and repair
```

Oil is therefore not only a fuel precursor. It is the shared industrial input that forces a meaningful decision between mobility and battlefield expansion.

The Resource Exchange remains available in enabled matches, but importing Materials must be a slow and expensive recovery option. Building and supplying local fabrication is the normal, efficient strategy.

## Related Source Documents

- `Economy_Reward_Design.md` owns canonical resource names, lifecycle rules, account/tactical boundaries, rewards, sinks, and conversion guardrails.
- `Field_Logistics_Oil_Fuel_Design.md` owns Oil extraction, physical Oil storage, tray-truck delivery, refinery conversion, Fuel storage, and vehicle Fuel use.
- `Automated_Fuel_Logistics_Design.md` owns autonomous hauler assignment, reservations, route stability, and usable Fuel rules.
- `Resource_Logistics_Exchange_Design.md` owns timed import/export jobs. Its Materials import route is emergency recovery and must remain less efficient than local fabrication.
- `GAME_DESIGN_REFERENCE.md` owns the current building catalog and compatibility identifiers.
- `Combat_Catalog_And_Upgrade_Design.md` owns building unlock and upgrade-track identities.
- `Match_HUD_And_Gameplay_Implementation_Spec.md` owns Match HUD resource presentation and Build Drawer behavior.
- `Architecture/gameplay_solid_ecs_contract.md` owns ECS/SOLID naming, assemblies, simulation ownership, and no-drift rules.
- `Architecture/performance_regression_contract.md` owns runtime budgets, structured metrics, hot-path rules, and GC regression gates.
- `Architecture/field_fabrication_materials_implementation_tracker.md` owns implementation order and evidence.

## Identity And Compatibility

Player-facing identity:

- Name: `Field Fabrication Depot`
- Short role: `Converts Oil into Materials.`
- Description: `Consumes Oil to operate fabrication lines that produce Materials for construction, repairs, and battlefield infrastructure.`

V1 compatibility identity:

- Keep the internal building lookup id `Building_Ammunition_Depot`.
- Keep the existing prefab, map placement, AI plan, custom-base, save, and catalog references valid.
- Do not rename the prefab or lookup id as part of the mechanics implementation.
- A later explicit content migration may rename the internal id only after all serialized references and save compatibility are audited.

The old ammunition-storage description is retired because ammunition and storage are not active resources or mechanics.

## Player Fantasy

The player operates a field industrial chain under pressure:

1. Secure or build Oil production.
2. Protect tray trucks and connected routes.
3. Decide whether delivered Oil should become Fuel or Materials.
4. Build and supply a Field Fabrication Depot to reduce dependence on expensive imports.
5. Spend Materials on structures, repairs, and infrastructure that change the battle.
6. Use the Exchange only when local production is unavailable, destroyed, or too slow for an emergency.

The depot should feel like strategic infrastructure, not passive income. Its value comes from the Oil supply route, conversion time, location, protection, and the opportunity cost of not refining the same Oil into Fuel.

## Design Pillars

### One Industrial Input, Two Strategic Outputs

Oil feeds both Fuel and Materials production. The player cannot maximize mobility and construction from the same limited Oil supply without investing in extraction and logistics.

### Local Production Is Best

Steady local fabrication must have the best effective Materials cost. Exchange import adds a markup, queue delay, and availability constraints. It prevents deadlocks but does not replace infrastructure.

### Physical Input, Faction-Level Output

Oil remains a physical building resource moved by tray trucks. Completed Materials enter one authoritative faction tactical Materials inventory. V1 does not add a third Materials truck or physical Materials crates.

### Readable Failure

The player must always know whether the depot is running, waiting for Oil, blocked by Materials capacity, disabled, damaged, or disconnected from logistics.

### Existing Architecture Wins

The feature extends the current ECS resource, building, logistics, UI-shell, and Exchange paths. It must not create a parallel wallet, a scene singleton, a new updating `MonoBehaviour`, or a second movement loop.

## Resource Ownership

### Canonical Tactical Materials

There must be exactly one authoritative tactical Materials value per faction.

- Building placement, repair, fabrication output, rewards granted into the match, HUD display, AI affordability, and Resource Exchange import/export all read or mutate the same ECS-owned value.
- The current Exchange-specific wallet must not remain a second authoritative Materials store.
- Migration must not use steady-state dual writes or periodic reconciliation.
- Persistent profile Materials and active-match tactical Materials remain distinct lifetimes. Scenario startup explicitly seeds the tactical value from authored match rules or an approved profile projection.

V1 lifetime policy is locked as follows:

- `PlayerProfileSaveData.materials` is persistent account/progression state. Active-match simulation does not read or mutate it directly.
- `FactionTacticalMaterialsComponent` is match-scoped. Scenario startup seeds it from authored match rules; V1 does not automatically withdraw profile Materials.
- Unspent tactical Materials are discarded when the match ends. Mission rewards grant persistent profile Materials through the existing typed reward/save path, not by copying the remaining tactical balance.
- A future mode may opt into profile-funded deployment only through an explicit launch reservation/result and match-settlement transaction. It must define withdrawal, cancellation refund, victory/defeat settlement, overflow, and duplicate-result behavior before implementation.
- `PlayerProfileSaveData.rushTickets` is the persistent Rush Ticket owner. A Rush-enabled match receives an explicit scenario-approved tactical allowance projected once into the Exchange boundary. Rush spending never writes the profile during simulation; an approved future persistent-spend flow must reserve tickets before launch and settle exactly once after the match.
- Tactical Materials and Rush Ticket telemetry may update match results, but telemetry counters are not currency settlement and cannot mutate profile balances.

Oil and Fuel continue to use their existing physical building storage and faction summary contracts. The fabrication feature must not mirror Oil or Fuel into a new wallet.

Tactical Credits must likewise converge on the ECS faction economy. The current managed player-dollar field, AI `FactionEconomy.Money`, and Exchange Credits field are implementation debt, not three valid economies. The implementation tracker requires a single authoritative Credits mutation path before Credits + Materials construction ships.

### Materials Capacity

Tactical Materials have an authored faction capacity.

- Fabrication stops before overflow.
- Exchange import validates capacity before accepting or completing a job.
- Mission rewards follow their authored overflow policy.
- Capacity can later be increased by upgrades or logistics structures, but V1 may use a scenario-configured fixed capacity.

## Core Runtime Loop

### Inputs

- The depot has an Oil input buffer and authored Oil capacity.
- Existing tray trucks may deliver Oil from an Oil Pump to either an Oil Refinery or a Field Fabrication Depot.
- Delivery uses the existing reservation, movement, pickup, and unloading path.
- A destroyed or invalid source, destination, truck, or route releases reservations exactly once.

### Conversion

- Conversion consumes an authored Oil amount over an authored interval.
- Conversion grants an authored integer Materials amount to the owning faction.
- Conversion never runs without enough Oil input or available Materials capacity.
- Fractional progress, if needed, remains deterministic ECS data and is not reconstructed from frame time in UI.
- Map-placed and player-built depots use the same projected components and conversion system.

### Output

- Materials are granted directly to the faction tactical Materials inventory in V1.
- No Materials output pile, Materials hauler, or unload animation is required for the first implementation.
- The selected-building panel and HUD update from versioned ECS read models only when data changes.

### Production Control

V1 supports a simple enabled/disabled production toggle if the existing building command pattern can expose it without architecture drift. The depot automatically resumes when Oil and Materials capacity become available.

Detailed player-controlled Oil allocation ratios are outside V1. Automatic routing must nevertheless be deterministic and stable.

## Oil Destination Selection

Tray trucks can serve two Oil consumers: refineries and fabrication depots.

The assignment rule must:

1. Consider only same-faction, enabled, reachable destinations with unreserved Oil input capacity.
2. Preserve an active valid assignment instead of recalculating every frame.
3. Score demand from normalized free input capacity and production starvation.
4. Use route cost or distance only after eligibility and demand.
5. Use stable ids as the final tie-breaker.
6. Reserve source cargo and destination capacity before movement starts.
7. Apply a reassignment cooldown or hysteresis so trucks do not oscillate between refinery and depot targets.

The system must never alternate destinations every update because two scores are nearly equal.

## Materials Sinks

Materials need visible tactical uses when local production launches.

### V1 Required Sink

Player-built battlefield structures require both Credits and Materials. Each build definition receives an authored Materials cost in addition to its current Credits price.

- Map-authored structures do not retroactively spend resources.
- A placement request is accepted only when both costs are affordable.
- Both resources are committed exactly once at the same authoritative placement step.
- Failed or cancelled placement follows one documented refund policy.
- The Build Drawer shows both costs and a typed `InsufficientMaterials` state.

### V1 Or Immediate Follow-Up Sinks

- Building repair.
- Defensive barriers and battlefield infrastructure.
- Authored mission actions that restore damaged facilities.

### Not A General-Purpose Unit Tax

Materials should not automatically become a cost on every infantry or vehicle unit. Production costs may use Materials only when explicitly authored and justified by balance.

## Exchange Relationship

The `Credits -> Materials` Exchange route is an emergency recovery route, not the core source.

It is appropriate when:

- the faction has not yet secured Oil;
- the only depot or its supply route was destroyed;
- an urgent defensive build cannot wait for local conversion;
- a scenario intentionally begins with disrupted logistics.

It must be worse than local fabrication through all of the following:

- higher effective economic cost;
- queue duration;
- scenario availability and queue capacity;
- storage-capacity validation;
- optional transport risk or presentation delay where authored.

Balance guardrail:

- The effective Credit cost of imported Materials should start at least 1.5x to 2.0x the modeled local opportunity cost.
- Local opportunity cost includes the value of consumed Oil, conversion time, depot investment, and logistics risk.
- Exact values are config-driven and must be validated by the balance harness.
- `Materials -> Credits -> Materials` and `Oil -> Materials -> Credits` loops must never produce profit. A complete round trip should retain no more than 85% of input value before time costs.

Exchange import remains valuable because it restores agency. It should feel expensive, not punitive or useless.

## Pacing And Balance Targets

Initial tuning targets, subject to simulation and playtest:

- One continuously supplied standard depot should produce enough Materials for one medium defensive structure every 2 to 3 minutes.
- A player who invests in Oil extraction and protects logistics should sustain construction more cheaply than a player who repeatedly imports.
- A player should not be able to run unrestricted aircraft, vehicle movement, and rapid construction from one low-output Oil source.
- The starting Materials reserve should teach at least one construction action before scarcity becomes relevant.
- Material-enabled scenarios should seed a depot or guarantee an attainable recovery route.
- Destroying a depot or its Oil route should cause a meaningful temporary slowdown, not an unrecoverable loss.

The final Oil input, Materials output, cycle duration, capacity, build costs, and import markup are authored data. Runtime systems must not hard-code balance values.

## Building States And Player Feedback

Required typed states:

| State | Player meaning | UI copy |
|---|---|---|
| `Producing` | Oil is being converted. | `Producing Materials` |
| `NoOilInput` | Input buffer cannot fund the next conversion step. | `Waiting for Oil` |
| `MaterialsCapacityFull` | Faction Materials cannot accept output. | `Materials storage full` |
| `NoOilRoute` | No valid tray route currently serves the depot. | `No Oil route` |
| `ProductionDisabled` | Player or scenario disabled conversion. | `Production paused` |
| `BuildingDisabled` | Damage/power/scenario rules prevent operation. | `Depot offline` |

These are data enums or status codes. Runtime simulation does not store localized strings.

### Match Header

- Replace the placeholder Supply value with live tactical Materials current/capacity.
- Use the canonical Materials icon and terminology.
- Update only when the source version changes.
- Header interaction continues to open Resource Exchange only where that feature is enabled.

### Selected Building Panel

Show:

- current Oil / Oil capacity;
- Oil consumed per cycle or per minute;
- Materials output per cycle or per minute;
- conversion progress;
- current faction Materials / capacity;
- typed status reason;
- production enabled/disabled control when available.

### Build Drawer

Show Credits and Materials costs together. A disabled build command exposes the exact missing resource without replacing the current HUD language.

## AI Design

AI support follows player-loop validation.

AI should:

- understand the depot as an Oil consumer and Materials producer;
- reserve enough Materials for planned construction;
- choose between refinery and fabrication capacity based on Fuel pressure and build plans;
- protect or target Oil routes and fabrication infrastructure;
- use Exchange import only when the scenario enables it and local recovery is too slow;
- avoid repeatedly switching Oil destinations because of small score changes.

AI must consume the same canonical resource components and authored costs as the player. It must not use hidden Materials or bypass conversion unless a scenario explicitly defines a handicap.

## Scenario And Failure-Safety Rules

A Materials-enabled scenario must provide at least one valid recovery path:

- starting tactical Materials;
- a seeded Field Fabrication Depot and reachable Oil source;
- the ability to build/rebuild the chain from available resources; or
- an enabled expensive Exchange import route.

Validation rejects a scenario where all required construction can deadlock permanently after an expected loss.

The initial faction-base layout may continue to place `Building_Ammunition_Depot`; it is interpreted as the Field Fabrication Depot after config projection.

## Audio And Visual Feedback

V1 may reuse the existing industrial building visual. Feedback should communicate operation without pretending ammunition storage exists:

- low mechanical fabrication loop while selected or nearby;
- restrained cycle-complete accent;
- Oil unload feedback through the existing tray-truck path;
- Materials header delta flyout on completed batches;
- no repeated warning audio every frame while blocked.

Audio and VFX are presentation consumers of typed events. They never control conversion or resource mutation.

## Architecture And Performance Contract

Implementation must follow these non-negotiable rules:

- ECS data is authoritative for Oil input, conversion progress, Materials totals, reservations, requests, results, and status.
- Frequent simulation uses unmanaged `ISystem` and Burst-compatible code where practical.
- Runtime source belongs in existing explicit assemblies: `Game.Components`, `Game.Configs`, `Game.Runtime`, `Game.Composition`, `Game.UI.Contracts`, `Game.UI.Shell.Contracts.Ecs`, `Game.UI.Shell.Ecs`, and `Game.UI.Runtime` according to responsibility.
- No feature runtime file may fall into `Assembly-CSharp`.
- Do not add `Manager`, `Controller`, `Facade`, `Bridge`, `Port`, broad `Service`, global registry, static singleton, or mutable static gameplay state.
- Bare `*System` names are reserved for actual ECS systems. Managed edge helpers require an approved reason suffix.
- No LINQ, closure capture, boxing, managed collection creation, per-frame string formatting, broad scene search, or recurring entity/component snapshot allocation in hot paths.
- No structural changes every conversion tick. Required components are projected when the building/faction entity is created.
- UI reads versioned ECS read models; it does not calculate conversion, affordability, route policy, or localized reason selection every frame.
- Steady-state fabrication, logistics, Materials summary, and unchanged UI reads target `0 B/frame` managed allocation after warmup.
- Performance acceptance requires before/after profiler evidence and no regression against the active performance contract.

## Telemetry And Balance Evidence

Track per faction and scenario:

- Oil extracted;
- Oil delivered to refineries;
- Oil delivered to fabrication depots;
- Fuel produced and spent;
- Materials fabricated, imported, rewarded, exported, and spent;
- depot active time and blocked time by reason;
- build requests rejected for insufficient Materials;
- tray route assignments, reassignments, and failures;
- depot and logistics losses;
- Exchange import frequency and effective markup.

The data should answer whether local production is desirable, whether Oil creates a real tradeoff, and whether the Exchange is recovery rather than the optimal economy.

## V1 Scope

Included:

- Field Fabrication Depot identity and description;
- physical Oil input delivered by existing tray trucks;
- Oil-to-Materials conversion;
- one canonical tactical Materials inventory;
- Credits + Materials building costs;
- live HUD, Build Drawer, and selected-depot state;
- expensive timed Materials import fallback;
- deterministic routing and reservations;
- tests, balance telemetry, architecture gates, and performance evidence.

Deferred:

- physical Materials crates and Materials delivery trucks;
- detailed player allocation sliders;
- multiple fabrication recipes;
- ammunition as a separate resource;
- account-wide passive Materials production;
- depot interior/art replacement;
- broad upgrade-tree expansion.

## High-Level Acceptance Criteria

- The player can identify why the Field Fabrication Depot exists and what it consumes and produces.
- A tray truck can supply either a refinery or a depot without oscillation or broken reservations.
- A supplied depot converts Oil into the one canonical tactical Materials value.
- Building placement spends Credits and Materials exactly once and shows typed affordability feedback.
- The Match header shows live Materials rather than a placeholder Supply number.
- Local fabrication is measurably cheaper than Exchange import.
- No conversion or exchange loop creates profitable arbitrage.
- A valid scenario cannot permanently deadlock the player without an authored recovery path.
- AI and player use the same resource ownership and cost data.
- Architecture, assembly, naming, Burst, GC, and performance guardrails pass with evidence.
