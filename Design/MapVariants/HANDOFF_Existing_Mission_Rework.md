# Agent handoff: rework existing Campaign missions for prepared maps

Date: 2026-09-29

Status: **implementation assignment prepared; no mission migration or acceptance performed by this document.**

## Objective and scope

Rework the spatial layout, map bindings and affected gameplay of seven existing coded Campaign missions to use the prepared map variants. Preserve their identities, purpose, objective/failure contracts, current command flows, story, resource ownership, Support policies and progression. Revalidate the other eleven coded missions where shared changes can affect them.

This is the mission-side companion to [HANDOFF_Map_Preparation.md](HANDOFF_Map_Preparation.md). Map entity conversion, intact/destroyed cleanup, physical surfaces and runtime-source qualification belong to that preparation assignment. Mission work consumes its artifacts and reports physical-map defects back to that owner. Do not recreate a second map-conversion pipeline inside mission builders.

Inventory at handoff: 18 MissionDefinition assets (CH01–CH03: five each; CH04: three). All reference the existing desert-base physical source through their chapter map definitions. Asset existence does not establish acceptance. Recheck the live checkout; older documents count 17 or describe eight future Campaign missions because Split Front was added later. The remaining seven of the planned 25 are future content and outside this existing-mission assignment.

Recommended scope is seven migrations, not eighteen map replacements. The eleven retained missions receive baseline classification and affected regression, not an unsolicited redesign. Optional later relocation of Establish the Base and Radar Warning is deferred. Skirmish and Operations are not migration targets; preserve their existing source compatibility and run affected shared-loader regression if required.

## Authorities and current implementation precedence

Read root `AGENTS.md` and these documents before implementation:

- [Map preparation handoff](HANDOFF_Map_Preparation.md) and [assessment](Integration_Assessment_2026-09-29.md).
- [Campaign mission catalog](../Campaign_Mission_High_Level_Design_Catalog.md), relevant chapter specifications under `Design/SagaChapters/`, and the [narrative/comic catalog](../Campaign_Narrative_Sequence_And_Comic_Catalog.md).
- [Mission product contract](../Monetization/Mission_Product_Contract_2026-09-28.md), [per-entry policy register](../Monetization/Mission_Product_Policies_2026-09-28.csv), and [existing mission remediation handoff](../Monetization/Implemented_Missions_Remediation_Handoff_2026-09-28.md). Do not duplicate or overwrite its active resource/access/reward work.
- [Implemented Support handoff](../AgentReports/SupportSystem/IMPLEMENTED_FEATURE_HANDOFF.md), its current policy/evidence files, and mission-specific readiness/production documents.
- [Split Front current readiness](../AgentReports/CH04M03SplitFront/readiness.md), [Air Corridor readiness](../AgentReports/CH04M01AirCorridor/readiness.md), [Steel Push readiness](../AgentReports/CH04M02SteelPush/readiness.md), and [Supply Line validation](../AgentReports/CH02M02SupplyLine/VALIDATION.md).

Canonical mission purpose remains authoritative. Later explicit user-approved behavior supersedes older implementation details. If documents and live code conflict, trace the approval/evidence and record the resolution before changing behavior; do not resurrect older UI based on a stale plan.

**Critical current Split Front behavior:** select launcher → normal Attack → target; Hold stops preparation but does not recall an in-flight missile. The custom launcher center panel and Confirm/Cancel flow were removed. Keep the existing unit card, colorful controls and visible ARIA Play/Stop. Target validity, range and civilian protection remain required; “confirmation” in older prose does not authorize restoring the removed panel. Current readiness records EN/FA Editor journeys but leaves human/device acceptance, dedicated defeat/replay/resume/access cases and voice approval separately pending. Re-audit these statuses; old wins cannot certify the new map.

## Complete coded-mission disposition

| Mission | Disposition | Target / reason |
|---|---|---|
| CH01-M01 First Contact | Retain urban source | Preserve the opening patrol/civilian tutorial. |
| CH01-M02 Establish the Base | Retain urban source | Preserve the established forward post and current Materials economy. |
| CH01-M03 Radar Warning | Retain urban source | Keep continuity with M02 and its warning/base-defense lesson. |
| CH01-M04 Airlift | Migrate | CityEdgeAirfield, rescue route and helipads. |
| CH01-M05 Breach Assault | Retain urban source | Preserve the district communications-node assault and chapter reveal. |
| CH02-M01 Gridlock | Retain urban source | Hospital/relief corridor remains urban. |
| CH02-M02 Supply Line | Migrate first | RefineryDistrict, working Oil/refinery/Fuel chain. |
| CH02-M03 Market Lifeline | Retain urban source | Market/civic dependencies remain central. |
| CH02-M04 Power Relay | Migrate | RefineryDistrict substation, sheltered route and supply access. |
| CH02-M05 Route Reopened | Migrate | AshLinePort hub, lifelines and two crossings. |
| CH03-M01 Signal Trace | Retain urban source | Populated-district signals and verified interception. |
| CH03-M02 Safehouse Sweep | Retain urban source | Residential neighbors, precision raid and evidence. |
| CH03-M03 False Front | Retain urban source | Civilian evacuation and false-report reversal. |
| CH03-M04 Evidence Chain | Retain urban source | Preserve urban witness/archive custody and extraction. |
| CH03-M05 Network Break | Retain urban source | Functioning district, audit bunker and evidence. |
| CH04-M01 Air Corridor | Migrate | CityEdgeAirfield runway approaches, radar and G2A coverage. |
| CH04-M02 Steel Push | Migrate | RefineryDistrict outer approach, depot and Relay endpoint. |
| CH04-M03 Split Front | Migrate last | RefineryDistrict depot, fork and launcher ridge. |

The candidate zones in prototype JSON are hints, not approved encounter specifications or canonical mission names. Do not rename Radar Warning to Convoy Approach or Supply Line to Supply Yard.

## Dependency gate and first work package

Start with MR-00: record branch/commit, dirty files, current builders, runtime partials, source/map/config hashes, latest approvals, evidence and open defects for all 18 entries. Preserve unrelated changes and ongoing resource/UI work. Use an existing suitable checkout or managed isolated worktree under the app's worktree rules; do not delete or reset active work.

For each target physical source obtain the preparation manifest, stable IDs, final coordinate transform, surface/blocker policy, destruction/attachment audit, known exclusions and loading/performance evidence. Record the exact candidate hash consumed by the mission.

Planning, anchor-schema design and code decoupling may proceed before all map gates pass. Integrated candidates may be tested provisionally when their dependency status is explicit. **Do not promote or switch default mission bindings onto an unqualified map foundation.** Pending device/visual acceptance remains pending, not waived by a successful mission Editor run.

If a prepared map cannot support a mission's required route, range, building footprint or civilian constraint, return a concrete change request to the map owner with coordinates, footprint and repro. Do not weaken victory rules, make invisible walkways, remove protected civilians or introduce hidden teleportation to accommodate the map.

## Shared migration contract

1. **Identity and rollback.** Preserve canonical mission/scenario IDs, chapter order, narrative sequence IDs, objective roles and progression keys. Create independent candidate logical-map definitions/IDs bound to the new physical source. Keep the previous binding available for rollback. Record old/new logical ID, physical ID and content hashes. Audit saved launch/replay/resume references and handle version mismatch explicitly; never reinterpret old coordinates on a new map silently.
2. **Generators are authoritative.** Update Editor config/environment builders and source descriptors. Do not hand-edit generated YAML or swap asset references without fixing regeneration. Existing builders clone old map definitions and write literal coordinates; inspect all callers, runtime assumptions, camera guidance and input probes. A rebuild must reproduce the candidate without reverting to dense-city geometry or injecting its old environmental content.
3. **Semantic anchors.** Keep stable objective/role anchor names where possible. Author final positions, radius/footprint, orientation, faction, lanes, surface layer and usage. Resolve heights from qualified composed surfaces; do not copy bridge/canal heights from prototype zones. Validate full spawn formations, movement access, production exits and interaction radii, not just anchor centers.
4. **One authoritative structure.** For functional refinery/depot/radar/etc., document whether the prepared map supplies the actual gameplay building or reserves a socket for a mission-spawned building. Bind existing owners or replace the socket presentation once. Never leave a static duplicate under a spawned functional building, spawn duplicate health/resource owners, or clear unrelated map entities by broad type/name matching.
5. **Mission-sized encounters.** Define a bounded mission footprint and meaningful routes. Prototype playable bounds are 600×400; existing Supply Line is 165×80 and Steel Push/Split Front are 255×80. Do not scale every coordinate or use the full map by default. Measure travel, warning and reinforcement times with actual units before adjusting balance. Capture before/after values and reasons; no global speed/range buffs to hide map mismatch.
6. **Gameplay and story.** Keep required success/failure facts, story clues and civilian/evidence protections. Landmarks must support the story; change spatial copy or framing only when the new geography demands it, including localized guidance. Preserve approved art and voice assets unless they materially contradict the new location. Do not regenerate/upload external media without the applicable authorization.
7. **Camera and UI.** Update intro tours, focus anchors, camera bounds, minimap projection, arrows, labels and return focus for actual geometry. A camera focus must not issue movement/attack orders. Keep mission purpose, objectives, next action and ARIA Play/Stop visible. New screens or substantial HUD redesign require ImageGen mockups based on actual Campaign references and user visual-direction review before implementation, per AGENTS.md. Routine approved-control reuse does not need repeated approval.
8. **Resources and Support.** Scenario-owned Materials/Oil/Fuel fund gameplay; account Credits/parts do not. Keep current authored costs and rewards except evidence-backed travel/balance changes within scope. Preserve protected reserves, shortage feedback, exact-once spend/refund and replay reset. Support stays optional; Smoke unlocks after Steel Push and is first used optionally in Split Front, which grants Strike after success. Do not reintroduce Support in earlier missions or require it to compensate for the new map.
9. **Runtime isolation.** Use additive source/catalog registration; preserve dense-city frozen-source tests. Verify only the selected physical map is present. Unload/retry/return must clear mission-owned entities and overrides without destroying shared source assets or leaking lighting/resource state.

## Mission work packages

### MR-01 — Supply Line / RefineryDistrict (pilot)

Inspect `CH02M02SupplyLineConfigBuilder`, `CH02M02SupplyLineEnvironmentBuilder`, rules/input probes and SupplyLine runtime partials. Establish the first complete map-to-mission integration and evidence format here before scaling.

- Place real Oil source, refinery and Fuel storage with clear service entrances and two distinct functioning hauling legs. Resolve each structure's map-versus-mission ownership explicitly.
- Preserve civilian reserve/allocation rules, threat pressure and alternate-lane behavior. The new map must support a meaningful alternate route rather than two labels on the same blocked path.
- Measure haul round-trip, turning/queue space, delivery rates and attack timing. Identify active infrastructure clearly among decorative tanks/pumps.
- Acceptance: normal manual-control and ARIA completion with actual hauling and deliveries; primary-route disruption/recovery; civilian reserve preserved; real result, rewards and Campaign return. A convoy merely reaching a refinery gate is not equivalent to the required service chain.

### MR-02 — Power Relay / RefineryDistrict

Inspect `CH02M04PowerRelayConfigBuilder`, rules, runtime, presentation and narrative builders. Use the current coded graph, not a simplified “hold substation” replacement.

- Author the safe family route, shelter, Fuel delivery route, engineer work point, power objective and defense approaches.
- Preserve required safe-route confirmation, families sheltered, Fuel delivered, power restored and final hold; engineer loss remains a real failure condition.
- Make the exposed shortcut visually distinguishable and prevent a short direct path from silently satisfying the protected-route requirement.
- Acceptance: full normal-input graph and ARIA completion; wrong-route/engineer-loss and missing-Fuel negative checks; visible restoration state, result and return. Keep this sector distinct from Supply Line's industrial hauling lesson.

### MR-03 — Airlift / CityEdgeAirfield

Inspect `M04AirliftConfigBuilder`, production/presentation builders, extraction runtime, `M04AirliftRuleTests`, integration tests and Editor probes.

- Preserve rescue, APC passenger flow, protected transit, landing-zone security, helicopter boarding/extraction and required passenger accounting as supported by the accepted implementation.
- Replace decorative aircraft at operational pads, reserve boarding/unloading space, and separate rescue and extraction locations enough to preserve the mission's movement decision.
- Measure APC path length, threats, boarding time, air approach/departure and deadline with the actual map. Recheck foot/vehicle collision and selection around pads and hangars.
- Acceptance: complete normal-input extraction with every required passenger; no ghost/duplicate passenger or aircraft owners; missing passenger and transport-loss behavior; manual/ARIA, result and return. Do not silently substitute a fallback extraction path merely to make the new geometry pass.

### MR-04 — Air Corridor / CityEdgeAirfield

Inspect `CH04M01AirCorridorConfigBuilder`, rules, input probe, runtime and guidance partials; reconcile latest Materials remediation first.

- Place radar, G2A deployment/build locations, production exits, protected relief corridor and aircraft entry/exit routes.
- Keep sufficient warning lead time and meaningful coverage/priority decisions. Camera bounds and minimap must reveal useful warning direction without disclosing hidden knowledge.
- Preserve launcher readiness/ammunition and actual missile/aircraft behavior. Holding a control tower alone cannot satisfy the mission.
- Acceptance: real threats, interception and corridor protection on manual/ARIA paths, local-resource affordability/shortage recovery, radar/objective failure behavior, EN/FA guidance and full result/return.

### MR-05 — Route Reopened / AshLinePort

Inspect `CH02M05RouteReopenedConfigBuilder`, rules, runtime and presentation/narrative builders.

- Author two useful supply routes using qualified bridge surfaces, relief/Fuel delivery endpoints, repairable disruption, hub assault entrance, garrison and protected records.
- Preserve relief delivered AND Fuel delivered AND restored link AND hub/garrison progress AND records preservation/hold. Capturing the hub alone must not win.
- Validate haulers/armor on both bridges, turns, staging and blocked-route recovery under live traffic. Assign water-edge and quay restrictions clearly.
- Acceptance: actual deliveries, repair and assault/records flow through ordinary input; loss of records and interrupted lifeline negatives; manual/ARIA, chapter clue, settlement and return. No invisible crossing or canal-bed movement.

### MR-06 — Steel Push / RefineryDistrict

Inspect `CH04M02SteelPushConfigBuilder`, Fuel scope/runtime, rules, input probe and current resource changes.

- Use an outer depot/approach sector distinct from the Supply Line crop. Place armor lanes, reserve, Relay endpoint, counterforce and production/build space.
- Preserve the documented 120 usable military Fuel and protected 40 civilian barrels unless a later explicit approved contract supersedes them. Nearby decorative/ambient storage must not provide extra usable Fuel.
- Measure the three-tank counterforce's movement demand, enemy arrival and command-vehicle escape path. Do not enlarge the map into a forced Fuel shortage or trivial long-range shooting gallery.
- Acceptance: real movement/combat and command-vehicle interception, reserve constraints and shortage recovery, manual/ARIA victory/defeat behavior, Smoke milestone settlement once, replay and return. Smoke remains unavailable as a required tool inside its unlock mission.

### MR-07 — Split Front / RefineryDistrict (last)

Inspect `CH04M03SplitFrontConfigBuilder`, latest launcher safety/Fuel/runtime/guidance code, rules, input probe and current readiness before using any older production plan.

- Place the forward base/diversion and verified battery as distinct fronts, with an accessible launcher position, valid minimum/maximum range, protected civilian space, retreat path and readable warning timing.
- Keep normal Attack/Hold and the current unit-card status; no custom confirmation panel. Hold before launch stops preparation; Hold after launch must not imply missile recall.
- Preserve required base defense, real battery/hostile defeat, civilian safety and optional first Smoke. Supply the required counterforce from the scenario and retain reserve accounting.
- Acceptance: real launcher preparation, launch, impact and durable deaths; invalid/too-close/too-far/protected targeting behavior; Hold, force split and base survival; successful no-Smoke path and a separate optional-Smoke path; manual/ARIA, EN/FA native screens, Strike milestone settlement, defeat/retry/result/return. Old confirmation-panel wins and pre-migration normal-Attack wins remain historical evidence only.

## Regression for the eleven retained missions

Record baseline status for every retained mission without promoting it. Their original map/source hashes and bindings should remain unchanged. Run existing focused contracts for affected shared loaders, map resolution, guidance, ownership, resource initialization and settlement.

If shared code changes, select normal-input regressions by behavior: fresh Chapter 1 launch/return chain and M02 construction budget; M03 warning/defense; M05 breach/destruction and chapter transition; Chapter 2 route/resource consumers; Chapter 3 confirmation, evidence and passenger flows. Explain omissions with an impact analysis. Broaden testing when failures or shared changes justify it rather than repeatedly running unrelated suites.

Preserve M02's 120 Materials → 90 Barracks → 20 squad → 10 remaining baseline and exact-once cancellation refunds. Track unrelated existing failures under their owning remediation work. A retained mission with incomplete acceptance stays incomplete even if its map did not change.

## Agent coordination and delivery order

Recommended sequence: MR-00 → MR-01 → MR-02 → MR-03 → MR-04 → MR-05 → MR-06 → MR-07. Each completed mission provides a reviewable slice rather than one campaign-wide scene swap.

For explicitly dispatched parallel work, one integration owner owns shared map resolution/catalog/Addressables, mission resource initialization, shared HUD and result/progression changes. Other agents own named mission config/runtime/guidance/test files. Record exact ownership before edits. Coordinate with existing monetization/Support/UI work; do not overwrite it or duplicate its logic.

Map preparation and mission work exchange a versioned manifest and concrete geometry requests. When a consumed map hash changes, identify and rerun affected mission checks. Do not accept a new map while keeping evidence pinned to the previous candidate.

## Validation requirements

Each mission needs separate rows for code/config checks, native visual review, normal-input manual-control completion, ARIA completion, real human acceptance and physical-device acceptance. A scripted state fixture proves rules only. It does not count as a normal-input win. Agent-operated mouse/touch is normal-input automation, not human acceptance.

Inspect these existing entry points before use; some rebuild assets or alter test setup. Update probes to the candidate map and assert active source/content hash before counting any outcome:

| Existing entry point / source | Use |
|---|---|
| `Game.Editor.CH02M02SupplyLineInputProbe.RunManual` / `RunWatch` | Pilot manual/ARIA input paths. |
| `Game.Editor.CH02M04PowerRelayRulesValidation.Run` | Protected-route/Fuel/engineer rules; not an input journey. |
| `Game.Editor.CH02M05RouteReopenedRulesValidation.Run` | Two-lifeline/records rules; not an input journey. |
| `Game.Editor.CH04M01AirCorridorInputProbe.RunEnglish` / `RunPersian` | Air Corridor locale paths. |
| `Game.Editor.CH04M02SteelPushInputProbe.RunEnglish` / `RunPersian` | Steel Push locale paths. |
| `Game.Editor.CH04M03SplitFrontInputProbe.RunEnglish` / `RunPersian` / `RunSmokeEnglish` | Current normal-Attack and optional Smoke paths. |
| `M04AirliftEditorProbe` and its partials | Inspect current full/committed-acceptance flows and side effects before selecting an entry point. |

Where no qualified normal-input probe exists, add or perform a real input journey. Never replace combat, logistics, transport or success with injected outcomes. Disclose pre-mission fixtures and ensure they do not bypass access/resource rules under test.

Required per migrated mission:

- Launch via normal Campaign selection → briefing/story → native gameplay on the exact new map → real victory → debrief/result/reward settlement → Campaign return and correct next progression.
- Full manual-control and ARIA journeys; ARIA start/stop and immediate handback; camera-only guidance never orders troops. ARIA must use the same legal information, resources and success predicates.
- Supported failure/defeat, retry, replay, withdrawal and resume/Continue semantics. Inspect actual Campaign behavior: existing pending resume may start a fresh attempt, not restore a mid-match snapshot. Test and describe it accurately.
- First-clear/replay/deduplicated settlement; zero/large account-balance equivalence for tactical starts; required-resource shortages and recovery; no new reward grant from a map-version change.
- EN/FA native briefing/HUD/objective/guidance/result/return review at supported aspect ratios, including RTL, voice/caption gaps, marker occlusion, touch reach and visible ARIA controls.
- Packed build/load parity and target-device performance with real units, HUD, destruction and camera movement. Map-foundation measurements alone do not certify a full mission load.

Follow AGENTS.md: RTK wraps the repository Unity wrapper; Hub stays open/signed in; no direct Editor executable or macOS batchmode. Use explicit log paths/timeouts and exact required pass markers; inspect full logs for compile errors and stale-assembly markers even when the wrapper returns zero. Preserve failed attempts. Windows uses the checked PowerShell wrappers. Do not terminate active Editors, reset IPC or bypass wrappers to make a lane pass.

Pin code revision/dirty-state identity, mission/config hash, physical/logical map hashes, build target and probe version with every run. Existing probe success on the old physical map is not evidence for this task. Screenshots of mockups or prepared empty maps do not certify implemented mission UI or gameplay.

## Deliverables and completion

Create `Design/AgentReports/MapVariantMissionRework/` containing:

1. `STATUS.md`: all 18 coded missions, migrate/retain decision, baseline readiness, current candidate and separate gates.
2. `BINDINGS.md`: old/new logical and physical maps, hashes, source manifests, saved-state compatibility and rollback instructions.
3. One per-mission plan/evidence directory: anchor table, route/camera diagrams or captures, building ownership, before/after timings/resources, code/config changes, commands, complete logs, failure history and screenshots.
4. Shared impact/regression record and list of pending map-owner requests or external acceptance needs.
5. Final handoff identifying exactly which candidates passed which gates and which default bindings, if any, were promoted.

Do not globally mark “seven missions ready.” Report each separately. Implemented/map-migrated, automated-pass, normal-input-pass, visual-approved, human-accepted and device-accepted are distinct facts. Promotion requires the applicable map and mission gates; missing human/device access leaves those named gates pending while other authorized work continues.

Completion of the implementation assignment means the scoped migration code/assets are reproducible, all executable required checks have evidence, remaining external gates are explicit, and rollback is preserved. It does not imply release readiness when player/device acceptance remains pending.

## Copy-ready first assignment

> Implement Design/MapVariants/HANDOFF_Existing_Mission_Rework.md, starting with MR-00 and the Supply Line pilot. Consume the versioned prepared Refinery source from HANDOFF_Map_Preparation.md; if unavailable, complete independent baseline/planning work and record the dependency rather than wiring an unqualified source into production. Preserve current mission identity, objectives, resources, Support, narrative and command controls. Rework generators, semantic anchors, functional-building ownership, routes, cameras and affected probes. Prove the exact new-map candidate through focused checks and complete normal-input manual/ARIA journeys, result and return. Record separate visual/human/device gates and all failures. Continue the remaining six migrations in the specified order as dependencies qualify, preserving regression for the eleven retained missions.
