# Agent handoff: prepare map variants for gameplay

## Existing Campaign maps only — 2026-10-03

Follow the [Campaign map reuse policy](CAMPAIGN_MAP_REUSE_POLICY.md) before implementing any Skirmish or Operations mission. Reuse current Campaign physical sources; author only logical bounds, anchors/routes, objectives, spawns and legal gameplay placements. Do not create terrain, roads, bridges, runways, procedural scenery, environment scenes or physical derivatives. This supersedes older mountain/highland, expanded-envelope, paired-runway and Demo 2 environment-authoring requirements for these modes. Preserve coded bindings, catalog IDs/counts, gameplay budgets/rules and evidence. Select another existing Campaign source if needed; otherwise record the source-fit gate without exposing unsupported content. New physical map design requires an explicit owner request.

Date: 2026-09-29

Status: **the RefineryDistrict candidate was revised for the CH02-M02 pump service gate on 2026-09-30. Candidate generation and packed content build passed at the new hash; earlier packed native route and switching evidence applies to the previous hash and must be rerun. The other three prepared candidates retain their earlier evidence. Native overview contrast, target-device performance and player acceptance remain open; no map foundation is qualified yet. Current candidate hashes are tracked in [Preparation/STATUS.md](Preparation/STATUS.md), the per-map Candidate manifests, and [Preparation/prepared-source-manifest.json](Preparation/prepared-source-manifest.json).**

## Objective and authorization boundary

Prepare the new map variants using the existing dense-city entity/presentation architecture: independent gameplay building ownership, clean intact/destroyed states, deterministic generation, valid surfaces and blockers, compatible runtime loading and measured rendering. Preserve each map's refinery, port or airfield identity.

The user requested this handoff after agreeing that buildings need entity ownership, duplicate/destroyed-model cleanup and proper preparation before use. This document instructs future preparation work; it does not claim that work is complete or dispatch agents by itself.

**Prepare maps before migrating missions.** Do not change current MissionDefinition/ScenarioSetup bindings, progression, rewards, mission rules, UI or the existing dense-city physical source as part of this work. Candidate catalogs and isolated runtime fixtures may be created to validate the new sources. Production catalog/Addressables changes must be additive and must preserve current source bindings and bundle behavior.

Priority: complete a representative Refinery District slice, finish Refinery District, then City-Edge Airfield and Ash Line Port. Include Frontier in inventory and shared conversion support; process its full candidate only after the three medium-map pipeline is proven. Frontier's campaign adoption remains deferred, and performance failures must remain visible rather than be worked around by weakening gates.

This handoff supersedes the simplified preparation recipe in [HANDOFF_Integration.md](HANDOFF_Integration.md), especially `Reserved`-to-blocker conversion, bridge heights and coordinate handling. Mission allocation recommendations remain in [Integration_Assessment_2026-09-29.md](Integration_Assessment_2026-09-29.md).

The subsequent mission assignment is [HANDOFF_Existing_Mission_Rework.md](HANDOFF_Existing_Mission_Rework.md): seven migrations, eleven retained-map regression entries, and candidate-specific mission acceptance. Supply it with the prepared-source manifest; this preparation assignment does not change mission bindings.

Future uncoded content consumes the same foundations through [FUTURE_CONTENT_MAP_PLAN.md](FUTURE_CONTENT_MAP_PLAN.md). Skirmish and Operations configure scenario-owned roles/routes on current Campaign geometry under the reuse policy. Larger two-base derivatives are no longer part of these mission assignments; existing source capability still needs per-mission qualification.

## Read first and preserve

Read the repository `AGENTS.md`, the two linked documents, and the relevant source below before editing. Follow the current Unity execution contract in AGENTS.md; do not copy an obsolete command from a report.

- Variants: `Assets/Game/Scripts/Editor/MapPrototypes/Variants/`.
- Inputs: `Assets/Game/Scenes/MapPrototypes/Variants/MapVariant_<MapId>.unity`, `Assets/Game/Art/MapPrototypes/Variants/<MapId>/`, and `Design/MapVariants/<MapId>/` captures/audits.
- Dense-city building records and realization: `Assets/Game/Scripts/Editor/MapPrototypes/DenseCityBuildingRecordFactory.cs`, `DenseCityBuildingPresentationRealizer.cs`, `DenseCityBuildingIntactVisualPolicy.cs`, `DenseCityBuildingAttachmentTransaction.cs`, and `DenseCityGenerationRecords.cs`.
- Dense-city orchestration: `DenseCityCandidateAuthoringTransaction.cs` in the same directory. Its entry points contain fixed dense-city paths and protected-source assumptions. Reuse appropriate helpers/contracts; **do not invoke its regeneration entry points against new maps or replace its path constants globally**.
- Runtime: `Assets/Game/Scripts/Systems/OperationMapBuildingDestructionSystem.cs`; locate `OperationMapBuildingAuthoring` and inspect its baker and validation contracts using `rg --files`.
- Packaging: `Assets/Game/Scripts/Editor/OperationMapAddressablesLayoutBuilder.cs`, `OperationMapDenseCityCandidateRuntimeContentBuilder.cs`, and the runtime-binding builders. These contain source-specific configuration; do not assume adding a scene registers a complete new physical map.

Record the starting commit and working-tree changes. This workspace already contained unrelated resource-initialization/test changes, modified font assets and mission evidence at handoff time; re-inspect and preserve them. Do not reset, clean or stage unrelated work. Use a managed isolated worktree when needed, following app/worktree instructions, and account for source files not yet committed before choosing its base.

Preserve original prototype scenes, captures and vendor prefabs as references. Write prepared candidates under new variant-specific paths with independent GUIDs. Keep generated meshes and scenes under Git LFS as required by `.gitattributes`. Never hand-edit scene YAML to implement the conversion. All candidate changes must be reproducible from supported Editor authoring/generation code through the required wrapper.

## Verified starting facts and known traps

| Map | World X/Z | Playable X/Z | Seed | Stored placement records |
|---|---|---|---|---:|
| RefineryDistrict | 0,0; 1400×1200 | 400,300; 600×400 | 40302 | 11,066 |
| CityEdgeAirfield | 0,0; 1400×1200 | 400,300; 600×400 | 10404 | 11,084 |
| AshLinePort | 0,0; 1400×1200 | 400,300; 600×400 | 20502 | 9,492 |
| Frontier | 0,0; 2400×1500 | 176,176; 2048×1024 | 90101 | 25,678 |

Stored placement audits have zero reported issues. They do **not** verify gameplay ownership, embedded visual alternatives, navigation, destruction, packed runtime parity or performance. Counts above are placement records, not certified runtime entity counts or draw calls.

- The current campaign has 18 mission definitions, all using the existing desert-base source. The older handoff says 17. No mission is currently qualified on these variants.
- `PlaceOptions.Vegetation.Reserve` is true. Grass, pebbles and sand-edge dressing reserve art placement space; they must not automatically become movement blockers.
- Port bridge zone centers have y = -4, while the authored bridge deck level is 0. Ground sampling alone puts anchors at the canal bed. `BridgeCells` supplies deck-level information; inspect final rendered offsets as well.
- Medium-map playable areas fit the 2048×1024 simulation grid, but their world height of 1200 extends beyond it. Out-of-grid backdrop is render-only, not wrapped or clamped into valid gameplay cells.
- Frontier requires an explicit coordinate conversion of (-176,0,-176) if adopting the current origin-zero grid. Apply it exactly once to every data domain, not just the scene root.
- Seven medium-map zones extend beyond declared playable rectangles: Port `RouteReopened_Hub`, `SupplyYard_East`, `Anchor_EastCheckpoint`; Refinery `SplitFront_Ridge`; Airfield `Airlift_Helipad`, `AirCorridor_Tower`, `Anchor_CityGate`. Resolve bounds intentionally; do not silently clamp critical locations.
- DenseCityBuildingDefinitionLibrary currently supports house/shop/civic and maps `Other` to house. Industrial plants, hangars, depots and military structures need explicit definitions or a documented static classification, not accidental house behavior.
- The existing building-record factory uses `RubbleRemainsBlocked`; the destruction system's inspected paths enforce that policy. Opening a path after destruction is separate functionality, not implied by conversion.

## Work packages and agent ownership

Use these packages for assignment if multiple agents are dispatched. Work can also be sequential. One integration owner controls shared contracts, generated output and Unity/package runs for a checkout. Parallel agents must not regenerate the same scenes, catalog, Addressables settings or source records concurrently.

| Package | Responsibility | Dependency / deliverable |
|---|---|---|
| A: inventory and shared schema | Prefab audit, source manifest, classification, stable identities, coordinate/surface schema | Publish the schema and representative mappings before B/C generate outputs. |
| B: building presentation | Intact/destroyed normalization, definitions, entity owners, attachments, render ownership | Use A; prove a representative Refinery slice before scaling. |
| C: surfaces and movement | Ground/decks, water, blockers, footprints, construction exclusion, route fixtures | Use A; agree footprint and ownership semantics with B. |
| D: integration and verification | Candidate scenes, rendering, isolated loading/packing, regression, evidence and performance | Integrate B/C; sole owner of shared packaging outputs. |

Before parallel work, record exact owned paths and integration points in the preparation status file. Keep conflicting shared edits with one owner. No agent may call another package complete solely from a message; verify its artifacts and test evidence.

## Phase 1 — inventory and deterministic input

1. Inventory unique prefab GUIDs and placement instances for every variant. Capture source GUID/local ID, stable placement key, map/seed/schema version, transform, category, full footprint, material references, hierarchy branches, physics components and attachments.
2. Audit intact/destroyed alternatives by actual prefab hierarchy. The existing policy handles direct `Model` and `Destroyed` siblings; do not assume every kit uses that shape. Ambiguous nested branches require an explicit mapping or a failed inventory row. A name substring is not enough to delete geometry.
3. Define explicit semantics for each prefab or placement override: gameplay building, gameplay vehicle, static obstacle, walkable surface, render-only prop, attachment, intentional ruin or backdrop. Include movement blocking, build exclusion, damage/target eligibility, intact/destroyed assets, attachment owner and surface precedence. Rendering category and gameplay ownership are separate fields.
4. Produce a machine-readable manifest plus a human-reviewable table of unresolved mappings. No destructive or runtime-active conversion of unresolved classes. Complete unaffected classes while recording exceptions.
5. Stable identities must be map-scoped and reproducible; avoid deriving building identity from traversal of every grass/prop record. Adding dressing must not renumber existing gameplay owners. Record schema/content hashes and deterministic generation order.

**Exit:** all gameplay-relevant classes mapped, unresolved classes explicit, export includes placements, roads, bridge deck data, surfaces, zones and coordinate transform. A rebuild from identical inputs produces identical semantic records and no duplicate accumulation.

## Phase 2 — representative building slice

Choose a small Refinery fixture containing a village/city building with embedded destroyed geometry, a warehouse, industrial structure, attachment-bearing structure, static prop and intentional ruin. Expand the fixture if the chosen inputs do not cover all required cases.

For each gameplay building:

- Create one independent authoritative building owner with stable identity, validated definition, faction/neutral status, health, footprint and blocker policy. Multiple child render entities are allowed. Do not combine several independent buildings under one damage owner merely to reduce entity count.
- Instantiate generated intact and destroyed presentations with matching placement transforms. Remove embedded destroyed alternatives from the **generated intact instance** using the existing normalization policy or a validated extension.
- Keep source/vendor destroyed assets. Start intact buildings with only intact presentation visible; activate destroyed presentation only for the correct state. An authored pre-destroyed building/ruin must have its own explicit initial state or scenery classification.
- Remove the corresponding original render-only instance from the candidate runtime output when its building presentation replaces it. Do not remove nearby objects just because bounding boxes overlap. Use provenance/stable IDs to prove duplicate ownership.
- Make roof parts, tanks, signs and supports follow an explicit intact/destroyed attachment policy. Prevent floating remnants and double registration in both building and global prop rendering. An attachment that should survive needs supported geometry in the destroyed state.
- Keep one presentation implementation active for an owner: resident or virtualized. `OperationMapBuildingDestructionSystem` rejects both simultaneously. Inspect material/shader compatibility and rendering support before selecting a mode.
- Do not grant production, resource generation, enemy targeting or mission objective status merely because an object became a building entity. Those roles require appropriate definitions and later mission policy. Neutral/civilian structures stay correctly classified.
- Resolve unsupported destruction models deliberately: supply a mapped compatible model, use an explicitly non-destructible role when appropriate, or leave the class unqualified. Do not substitute an unrelated house ruin for a refinery/hangar just to pass a test.

**Exit:** actual runtime destruction of representative structures shows exactly one correct visual state, no original duplicate, correct remaining blockers and no orphan attachments. Reload/reset restores the intended starting state. Test neighboring buildings to prove independent ownership.

## Phase 3 — surfaces, blockers and traversability

- Preserve the existing 1 m / 2048×1024 contract unless a separately reviewed change is needed. Express source-to-runtime coordinate conversion explicitly and test inverse mapping for representative landmarks.
- Bake ground heights using the builder's triangulation-consistent sampling. Compose walkable bridge/pad/runway overrides in a defined order; retain water exclusion outside valid crossings. Do not make every road cell unconditionally override a water/blocker error.
- Use actual horizontal footprints/yaw for static obstacles and foundation/blocker records. Account for agent width and vehicle turns in route validation. A placement AABB alone does not establish valid path clearance.
- Export separate movement and build restrictions. Grass and pebbles remain nonblocking; trees, large rocks, fencing, parked scenery vehicles and pipelines get deliberate rules. Road clearance must match visible geometry.
- Prevent double-blocking of the same building by both generic scenery import and authoritative building data. Verify the declared rubble policy through damage transitions.
- Derive final anchor heights from the composed playable surface, including bridge decks. Validate anchor radius/footprint, not just center points. Keep camera, minimap, zones and render transforms in the same coordinate space.
- Block simulation outside intended playable bounds even when background terrain exists. Correct the seven known zone-boundary exceptions with explicit recorded decisions. Provide deployment and camera margins without enlarging gameplay accidentally.
- Author preparation-only route fixtures for infantry, haulers/APCs and armor where appropriate. Test bridge approaches and both crossings, gates, refinery access, depot exits, ridge access and helipad boarding clearance. A standalone fixture is useful map evidence, not a completed mission.

**Exit:** all declared fixtures have valid surfaces and routes; invalid water/outside-map destinations reject safely; no subterranean bridge anchors, invisible obstacles from dressing, duplicate building blockers or unexplained clipping.

## Phase 4 — complete candidates and runtime packaging

Generate independent authoring and entity-presentation sources plus the necessary surface/grid, placement, render, minimap and binding assets. Suggested output roots, to adopt only after checking repository conventions:

- `Assets/Game/GeneratedOperationMaps/Variants/<MapId>/`
- `Assets/Game/Scenes/OperationMaps/Variants/<MapId>/`
- `Design/MapVariants/Preparation/<MapId>/`

Publish an output manifest with actual paths, GUIDs, source hashes, generator version and regeneration entry points. Keep original prototypes unchanged; regenerate candidate derivatives separately.

Use variant-aware descriptors around reusable dense-city helpers. Avoid copying the entire transaction with renamed constants or retargeting its protected original sources. Preserve current tests for existing dense-city identity and compatibility. Extend registration without replacing production map selection, chapter catalogs or mission bindings.

Verify only one map presentation loads; prototype GameObjects must not coexist with their baked replacements. Choose one lighting/volume owner. Support unload, reload and switching between a variant and the existing map without leaked objects, entities, materials or lighting. Test packed runtime loading; Editor-only success is insufficient.

Convert repeated dressing to the project's supported instanced/entity presentation as appropriate. Rendering entities do not imply every pebble needs health or independent gameplay logic. Preserve deliberate visual quality while measuring main-thread time, render time, draw calls, memory, load time and representative movement/combat load. Use existing target-device budgets and record device/build settings; never invent a passing threshold after seeing results.

Do not reduce Frontier's advertised scope silently to make it pass. If full Frontier misses budgets, retain a failed result with measured causes and a proposed chunk/culling or sector-extraction plan. Smaller candidates can still qualify independently.

## Phase 5 — required validation and evidence

Add meaningful regression coverage for the new risks. Existing suites below are architectural references and existing-source regressions; passing them alone does not qualify a variant:

- `Assets/Tests/Editor/DenseCityBuildingPresentationRealizerTests.cs`
- `Assets/Tests/Editor/DenseCityBuildingRecordFactoryTests.cs`
- `Assets/Tests/Editor/DenseCityBuildingAttachmentTransactionTests.cs`
- `Assets/Tests/Editor/OperationMapBuildingDestructionSystemTests.cs`
- `Assets/Tests/Editor/DenseCityDeterministicFixtureTests.cs`
- `Assets/Tests/Editor/DenseCityBakeReadinessValidatorTests.cs`
- `Assets/Tests/PlayMode/OperationMapDenseCityPackedRuntimeParityPlayModeTests.cs`
- `Assets/Tests/PlayMode/OperationMapDenseCityPackedRenderVirtualizationPlayModeTests.cs`

Provide variant-specific checks for identity uniqueness; provenance-based duplicate removal; missing destruction mappings; intact/destroyed transform and material parity; nested/ambiguous alternatives; attachment ownership; deterministic rebuild; visual-only dressing; bridge-deck sampling; bounds conversion; spawn/build clearance; packed load/unload and protected-source preservation.

Capture before/after views at matching coordinates, pitch/FOV and resolution. Include top-down, actual battle-camera frames, representative intact/destroyed pairs, attachments, bridges and active moving units. Capture the prepared native runtime, not only prototype views. Keep map readability and camera occlusion distinct from placement audit results.

Each run must record map ID, candidate content hash, code revision plus dirty-state/diff identity, command, target, log path, exit status, expected pass marker and observed marker. A timeout, missing marker or nonzero exit fails the run. Preserve failed logs alongside later successful runs.

On macOS, all executeMethod, test, capture and build execution goes through the repository wrapper, wrapped by RTK. Keep Unity Hub open and signed in. Do not add batchmode, launch the Editor executable directly, reset IPC, terminate Unity processes or use CLI build/run/test as an alternate route. CLI-connected live inspection must follow the unity-cli skill and AGENTS.md; required generation/validation still uses the repository wrapper.

Command **template only**—the named variant entry point and marker must be implemented and verified before use:

```bash
rtk proxy Tools/CI/invoke_unity_macos.sh \
  --timeout 900 --log /private/tmp/map-variant-<map>-<run>.log -- \
  -quit -executeMethod <VerifiedVariantValidationClass.Method>
```

Inspect wrapper behavior before running, including active-project/scene handling. Do not quit an unrelated or user-owned Editor to run validation. Retain full logs, check the exact per-map pass marker against those logs, and copy evidence into the report directory. On Windows use only the repository PowerShell wrappers with explicit logs/timeouts and the current AGENTS.md rules.

## Definition of done and status reporting

| Gate | Required evidence |
|---|---|
| Inventory complete | Every gameplay-relevant prefab classified; independent stable IDs; unresolved mappings explicitly counted. |
| Prepared authoring | Deterministic derivative generation; protected-source hashes preserved; correct entity owners; zero unexplained duplicate presentations. |
| Destruction and surfaces | Intact/destroyed transitions, attachments and blocker policy proven; bridge/bounds/movement fixtures pass. |
| Packed runtime | Variant-specific loading and presentation parity, unload/reload, old-map switch and clean ownership verified. |
| Visual review | Native prepared-map captures inspected; remaining visual issues and any user acceptance recorded separately. |
| Device performance | Actual device/build measurements against existing budgets; pending if unavailable, failed if exceeded. |
| Mission readiness | **Out of scope and pending** until each later migrated mission completes normal-input play, ARIA, result, return and player/device acceptance. |

Use per-map status: `Not started`, `In progress`, `Failed gate`, `Prepared candidate; pending device/visual acceptance`, or `Qualified map foundation`. Use the final label only when all map-level gates pass; never call it a production-ready mission.

Deliver:

1. Reproducible preparation/export code and focused tests.
2. Candidate assets, metadata/LFS files and output manifest.
3. Prefab/placement classification and duplicate/destruction audit counts, including excluded backdrop and intentional ruins.
4. Bounds/surface/route report with the known issues resolved or explicitly failing.
5. Full validation logs, fixed-camera evidence and device measurements.
6. `Design/MapVariants/Preparation/STATUS.md` with per-map gates, exact commands, unresolved items, rollback information and next-agent instructions.

Stop at prepared map foundations. The subsequent mission migration handoff should consume certified physical sources and author mission-specific anchors, gameplay objects and pacing. Do not switch seven campaign missions as a shortcut for demonstrating this preparation work.

## Suggested first agent assignment

> Read AGENTS.md and Design/MapVariants/HANDOFF_Map_Preparation.md. Begin the map-preparation pipeline with inventory of all four variants and a representative Refinery District conversion slice. Reuse the existing dense-city ownership, visual-state and rendering contracts while preserving original prototypes and current campaign bindings. Prove duplicate removal, independent building destruction, attachment behavior and surface/blocker correctness before expanding to full maps. Record reproducible commands, candidate hashes and separate validation gates in Design/MapVariants/Preparation/STATUS.md. Do not migrate missions or claim player readiness.
