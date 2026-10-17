# Scalable production strategy for 120 Skirmish missions

Date: 2026-10-14. Implementation proposal for the existing S001–S120 catalog.

Implementation assignment: [scalable mission production handoff](SCALABLE_MISSION_PRODUCTION_HANDOFF.md).

Build missions as data on a stable shared game engine, then process batches through a reusable validation queue. The immediate goal is a few qualified missions per day instead of spending weeks on one mission. Generating all 120 draft definitions in a few hours becomes plausible after the factory exists; completing all gameplay and device acceptance in those hours does not.

The recommended first investment is a small generic builder and test adapter, not another mission-specific implementation. Keep S005's remaining flight, AI, recovery and native evidence work with their existing owners. Transfer each shared repair into a reusable capability baseline so later missions inherit the implementation and appropriately scoped evidence.

## Current production bottlenecks

The catalog is already designed: five logical map groups × four objectives × three armies × two starts equals 120 entries. [SCENARIO_CATALOG.csv](SCENARIO_CATALOG.csv) has 30 entries per objective and 40 per army. [INITIAL_SETUP_MATRIX.csv](INITIAL_SETUP_MATRIX.csv) supplies 360 scenario/size rows. There is no need to invent 120 missions from scratch.

S005 has carried shared combat, aircraft, command input, fog, recovery and session repairs alongside mission qualification. Its recent north-route and paid-air preflight failures also include test-fixture type/response errors. A production lane must distinguish game defects from broken tests before repeating full matches.

Several existing authoring paths still grow by scenario ID. `SkirmishDefinitionBuilder` writes S002–S004 and invokes an S004 map builder; `SkirmishExpansionAuthoredSet`, `ToAuthoredSet`, launch dispatch and publication evaluation contain concrete S002–S005 branches. The packaged catalog already has a generic definitions array, so extending that data path is smaller than adding 120 builders.

The inspected objective configuration exposes Base Assault authoring, and the Skirmish runtime/objective tests inspected are Base Assault focused. Frontline Control, Breakthrough and Convoy Escort each account for 30 planned entries. Their shared runtime, planner and acceptance support must be completed and certified before the dependent 90 entries can qualify. Generating their assets alone cannot finish that work.

## Separate shared engineering from mission authoring

| Workstream | Owns | Completion unit |
|---|---|---|
| Shared systems | Objectives, enemy planning, ARIA skills, roles, aircraft, input, checkpoint, session lifecycle | A reusable capability with regression evidence and affected consumer list |
| Source and layout | Existing Campaign source selection, legal sectors, typed routes, pads and camera bounds | A source/sector/capability record plus native placement and route evidence |
| Mission content | Definitions, exact setup, briefing, EN/FA copy, objective anchors and meaningful tactical choices | A mission candidate using certified capabilities |
| Qualification | Normal input journeys, per-entry outcomes, edges, recovery, visuals and separate phone/player gates | Evidence for the exact mission and exposed combinations |

A content agent that finds an aircraft bug files one shared reproduction and moves to an unaffected ground mission. It does not repair aircraft independently inside its mission or spend days repeating the same failed journey. A capability failure blocks its consumers, not the entire catalog.

Keep all fixed counts, costs, caps, deadlines, difficulty parity, entitlements and stable IDs. Preserve S001/S025/S073 legacy mappings and the separate stress index. Work ordinals are not runtime or save identities. This proposal does not change publication or acceptance rules.

## Build the smallest useful mission factory

Use the existing catalog, setup matrix and [individual briefs](Scenarios/README.md) as inputs. Add an Editor builder that accepts a list of catalog IDs and authors only their logical definition/layout assets through Unity APIs. Do not use the existing broad rebuild as a harmless next-mission command: it rewrites shared configuration and calls a map builder.

Each mission specification should identify:

- Catalog ID and definition version; objective, army and start profiles.
- Existing Campaign definition/scene, source hash, sector and coordinate frame.
- Typed bases, production/delivery pads, objectives, routes and camera/minimap bounds.
- References to shared economy, role, difficulty and size policies; exact matrix row fingerprints.
- EN/FA briefing, objective/failure explanation, useful counters and two intended approaches.
- Required capabilities, scenario data hash and evidence requirements.

The builder must be idempotent, preserve existing GUIDs and unrelated assets, and report all failures. Generate missing draft entries without granting Playable status. A repeat with identical inputs should produce no asset diff. Import a content batch together to reduce asset refresh/reload overhead.

Generalize authored-set lookup, ordinary library launch and resolver inputs around the catalog identity and selected setup. Remove accidental Desert Base source/registry defaults for new entries. Retain explicit legacy adapters and existing certified behavior. An unknown identity must fail visibly rather than launch S001.

Generalize publication around valid catalog definitions and required capabilities while retaining every current readiness gate. The S003/S004 air gates and S005 role/aircraft requirements must become reusable requirements, not disappear when their ID checks are removed. Reference actual evidence records rather than treating file presence or a boolean supplied by a caller as acceptance.

## Reuse Campaign geometry and certify legal sectors

Follow [CAMPAIGN_MAP_REUSE_POLICY.md](../../MapVariants/CAMPAIGN_MAP_REUSE_POLICY.md). The policy records 25 Campaign mission bindings on 11 physical scene sources. The five Skirmish map labels remain logical catalog groups; missions can use a suitable existing Campaign source within the policy.

Create a small registry of legal sectors, route networks and facility pads on those sources. Record the source hash, surface/grid identity, usable bounds, role footprint and exit clearance, connected approaches, line-of-sight conditions and supported army/size combinations. Measure bilateral air infrastructure where required.

Cache unchanged geometry analysis. Check every new mission's actual placements, spacing, objective route and starting census against it, then prove source fit in the native scene before long matches. Moving a facility or changing its footprint invalidates that placement evidence even if the terrain is unchanged.

If an army, objective or size cannot fit, choose another existing source or record the unresolved fit gate. Do not create roads, runways, compounds or terrain, change budgets, or expose an unsupported combination to fill the catalog.

Retain scenario distinction: each briefing must connect its objective and force to concrete decisions on its routes. Review the two approaches and counter access; reject filler combinations that only change a title or spawn count.

## Run batches with clear file ownership

Use batches of six, matching the existing twenty map/objective packets. Schedule first by available capabilities, rather than requiring every preceding S-ID to finish. Base Assault ground entries provide the first pipeline pilot; advanced air and the other objectives follow their shared capability gates.

A practical team arrangement is three authoring agents preparing separate mission files plus one integration/validation owner. This is a proposed arrangement, not a request to open new chats or start more Editors now.

Content authors own disjoint mission folders and external job specifications. One integrator owns shared C#, common assets, catalog aggregation, packaging and publication. Authors prepare data in isolated checkouts; the integrator combines and imports the batch in a controlled Editor. A shared runtime defect goes to its system owner.

On the current Mac keep one dense native Play validation lane. Parallelize offline preparation and bounded configuration analysis around it. Several heavy Editors competing for memory can reduce throughput. Add a second native worker only on another suitable licensed machine, using the repository's platform wrappers and isolated profiles.

Freeze runtime, source bindings and the whole candidate content batch before qualification. Avoid merging a shared change in the middle of a counted run. Integrate at batch boundaries and select the affected rerun set from explicit dependencies.

## Replace copied test scripts with parameterized jobs

Adapt the existing normal-input harness. Its internal `RunEditorJourney` already accepts mission, seed, locale and control-mode parameters, although public wrappers and candidate catalog projection are specific to individual missions. The [existing reusable builder handoff](../../AgentReports/SkirmishExpansion/S005/REUSABLE_MISSION_BUILDER_HANDOFF_20261011.md) identifies this path.

Use one versioned job schema containing candidate/build identity, catalog ID, definition/setup hash, seed, locale, size, difficulty, control mode, required stages, timeout and output directory. Derive requirements from the mission capability set and acceptance contracts. Reject unsupported combinations before launch.

One stable helper must operate ordinary visible controls, including physically scrolling to offscreen squad cards, accepted selection/order receipts, legal resolved movement goals, ARIA Play/Stop, Save/Exit/Continue, result and return. Keep observer data read-only and outside ARIA's permitted perception. A camera instruction cannot issue troop orders. Direct bootstrap, injected combat, forced results and accelerated simulation are technical fixtures, not normal-input wins.

Preflight the driver and qualifier offline before spending native time: compilation/type contracts where feasible, response shape, job schema, filename/path aliases, required fields, proof counts, source guard and negative controls. Report each missing stage predicate separately. Stop producing new copied runner/observer/parser packages for every attempt; fix the shared driver and add a focused regression for the observed failure.

A queue should persist `queued`, `running`, `passed`, `failed`, `blocked dependency` and `interrupted` records outside gameplay state. Use exclusive worker claims, unique artifacts, wrapper ownership receipts and idempotent postprocessing. After interruption, resume pending jobs; an interrupted run cannot acquire a success marker by inference. Reset the isolated profile/session normally between runs and include replay cleanup checks.

## Validation stages and evidence reuse

| Stage | Batchable work | Evidence that still belongs to each mission |
|---|---|---|
| Static configuration | All 120 IDs, 360 setup rows, references, accounting, localization and capability requirements | Exact compile reasons and complete expected setup set |
| Shared systems | Role, objective, economy, checkpoint, planner, input and lifecycle fixtures on a frozen engine | Required mission-specific interactions and capability applicability |
| Source fit and visuals | Reusable geometry analysis and source atlas | Actual facility legality, route traversal, camera, briefing/HUD/result in EN/FA |
| Native gameplay | One queue and reusable driver | Full manual-control and ARIA journeys, two viable approaches, outcome/return and required recovery/edges |
| Device and player | Scheduled sessions grouped by build/family and device tier | Entry coverage required by the governing contract; actual phone and unfamiliar-player records |

Check all compiler results, not only a return value meaning that at least one setup compiled. Expand the supported difficulty/size combinations and reject unsupported ones explicitly. Simulation and isolated ECS tests should catch rule/accounting failures quickly before native journeys, but do not replace them.

Keep full candidate provenance and start/end source guards. Cache a shared fixture only when its relevant code/assets, runner and parameters match and its coverage applies. Preserve the original full-source identity and describe reused evidence as dependency-qualified. Do not relabel an old mission win as a win for new content or pretend an older candidate is the current whole-source replay.

Changes to camera/copy require their affected visuals/input checks. Changes to objective routes or placement require source fit and affected normal journeys. Planner/combat/air/checkpoint changes require the relevant shared fixtures and affected native journeys across existing consumers, including Campaign regressions. If the dependency graph is uncertain, use a broader rerun set.

## Preserve coverage while removing duplicate work

[AI_AND_ARIA.md](AI_AND_ARIA.md) and [ACCEPTANCE.md](ACCEPTANCE.md) require:

- Six normal-speed Regular ARIA runs per entry: three seeds in each language, at least two wins per locale, with all failures retained. Across 120 entries this is at least 720 full runs.
- Twenty stress seeds for each map/objective/army family, alternating starts: 60 families, or 1,200 family sample slots.
- An additional normal-speed ARIA win for every additional exposed difficulty; EN and FA wins for each additional exposed size, plus its device/recovery gates.
- Per-entry manual victory, two approaches, applicable objective edges, localization, recovery, role/source-fit evidence and separate device/player acceptance.

Plan family stress once per frozen relevant baseline, not once for every mission. Schedule a real run into both a mission matrix and a stress sample when its exact seed/start/locale/setup satisfies both. Count distinct stress seeds correctly: replaying one seed in two locales does not supply two different seeds. Generate an explicit coverage report before subtracting duplicate work. Extra difficulty and size runs cannot replace the six Regular runs.

Leave all acceptance obligations in place. Pairwise simulation, faster clocks or a few template victories do not bulk-certify 120 missions. Any future reduction in these requirements needs a separate owner-approved contract change.

## Prevent repeated failures from consuming days

Classify a failure as mission configuration, shared gameplay, driver/observer, or infrastructure. After two occurrences with the same failure signature, stop automatic full reruns of that job. Diagnose it, run the smallest valid reproducer, verify the correction, and then resume the full required journey. Preserve failed evidence.

Keep other jobs running if their dependencies remain valid. A driver response-shape error should not trigger map or AI changes. A missing device remains an explicit device gate, while Editor qualification can continue. A shared flight defect pauses aircraft consumers and leaves certified ground families available.

Maintain a compact queue dashboard: mission stage, blocker, owner, next required job, worker minutes, failures by category, and external gates. Track native utilization, first-attempt pass rate, average job duration and oldest blocker. Repeated fixture failures reopen the driver ticket; they do not justify weakening acceptance.

## First implementation package

Time budgets below are planning estimates for the limited pipeline work, assuming the existing connected Editor and harness remain usable. They are not estimates for completing the remaining aircraft, AI or three objective systems.

| Priority | Deliverable | Existing hook | Done when |
|---|---|---|---|
| P0 | Job schema and offline driver/qualifier preflight | Existing normal-input harness and `Tools/CI/audit_skirmish_native_contacts.py` | Malformed jobs and previously observed field/response errors fail before native launch |
| P0 | Small parameterized public journey adapter | `SkirmishS002AriaRunHarness.MenuProbe.cs` | Two already-authored missions use the same runner through ordinary Menu/input/result flow |
| P1 | Config-only builder and generic lookup/dispatch | `SkirmishDefinitionBuilder`, `SkirmishExpansionCatalogFactory`, `SkirmishExpansionCatalogConfig`, `SkirmishLaunchProjection` | A second build is unchanged; unknown IDs fail; existing mappings and asset GUIDs survive |
| P1 | Batch compile report and capability ledger | `SkirmishSetupCompiler`, packaged matrix and publication validator | All expected rows accounted for; unsupported rows have precise reasons; generated assets stay unpublished |
| P1 | Persistent queue and affected-test manifest | Repository wrappers and existing source/evidence tooling | Interrupted jobs stay incomplete; shared proofs retain dependency-qualified provenance |
| P2 | Ground Base Assault pilot and source registry | Existing Campaign sources and typed layout configuration | New candidates pass placement/route/visual gates before full matches |
| P2 | Shared FC, BT and CE owners | Existing architecture/objective contracts and test folder | Each objective has actual native behavior, planner/ARIA support, edges, recovery and result evidence |

Allocate the first one to two engineering days to P0 and the smallest usable BA-only P1 slice. If the generic launch change is larger than expected, ship the runner/preflight improvement first and keep the refactor as a bounded ticket. Do not wait for a new orchestrator service, dashboard website or every objective to obtain the first benefit.

For the first six draft candidates, use the uncoded ground BA pool S026, S049, S050, S074, S097 and S098, subject to source-fit/capability checks. Preserve the existing S001, S002, S025 and S073 bindings. These choices supply concrete work without touching S005's active candidate. Unresolved source fit stays blocked rather than changing the catalog tuple.

Prepare missing metadata for all 120 entries in parallel, but initially instantiate and qualify only proven objective/army families. Expand each shared objective across the remaining catalog when it passes its capability gate. The full catalog continues to include all planned sizes and difficulties; temporary internal staging is not final delivery.

## Throughput targets and capacity calculation

Use measured worker time, not the number of generated assets, to forecast completion:

`missions per day = native workers × usable minutes per worker per day / required worker minutes per mission`

The planned Regular plus family stress workload contains 1,920 sample slots before proven overlap, plus manual, recovery, edge, device and other exposed-setup work. At an illustrative eight worker-minutes per full job including setup/artifacts, those slots alone take 256 worker-hours. At eight scheduled hours/day and 70 percent usable capacity, one worker needs about 46 days for that workload; two need about 23. This excludes retries and the additional gates. Actual match durations and verified coverage overlap must replace these assumptions after the pilot.

Exposing all four difficulties adds at least 360 winning runs beyond Regular. Two additional sizes add at least 480 winning runs. At the same assumed duration, those 840 runs add 112 worker-hours before size-specific device/recovery work. This is why full-catalog acceptance cannot honestly be a few-hour task.

| Output | Proposed target after prerequisites | Condition |
|---|---|---|
| All 120 draft specifications/assets | A few hours of batch generation | Generic builder and source/spec inputs ready; generation does not resolve missing capabilities |
| Mission content preparation | Six candidates per batch, multiple batches in parallel | Disjoint ownership and approved physical source reuse |
| Editor qualification | Start with 2–4 missions/day on one native worker; 4–8 with two independent workers | Stable shared capabilities, low fixture failure rate, short measured jobs and scheduled family stress; validate against pilot totals |
| Published missions | Only the exact entries/combinations with all required gates closed | Phone/player evidence and external dependencies available; no publication target inferred from Editor counts |

The qualification targets are goals, not measured current throughput. Long matches, extra exposed combinations or repeated genuine losses can reduce them. Capture enough run telemetry to show whether the bottleneck is authoring, native execution, shared defects or external acceptance before buying more parallel capacity.

Run the first six-candidate pilot, measure complete applicable job sets and failures, then forecast the next twelve. Set a first-attempt job success goal of at least 90 percent and driver/infrastructure waste below 10 percent of native worker time. These are proposed production metrics, not gameplay win-rate claims. If the driver misses them, stabilize it before scaling the queue. Reforecast after each batch and retain an explicit backlog for remaining shared capabilities and external device/player gates.

## Implementation boundaries and references

This strategy adds a production workflow around the existing contracts. It does not authorize physical map derivatives, developer UI, outcome injection, lower readiness standards, interruption of active Unity runs or bulk publication.

Every macOS Unity authoring/capture/validation operation must use `Tools/CI/invoke_unity_macos.sh`, wrapped with RTK, with Hub open, explicit logs, timeout and required markers. Do not invoke the Editor executable directly, add batchmode, or kill an Editor to make room. Windows workers use the checked PowerShell wrappers. Reuse a connected Editor through the repository wrapper where supported, with verified idle/domain-reload state and run ownership; every Play attempt still starts a clean mission session.

Use these existing sources during implementation:

- [Campaign map reuse policy](../../MapVariants/CAMPAIGN_MAP_REUSE_POLICY.md)
- [Catalog and setup design](PLAN.md), [implementation handoff](IMPLEMENTATION_HANDOFF.md), [technical architecture](TECHNICAL_ARCHITECTURE.md)
- [Objective implementation](OBJECTIVE_IMPLEMENTATION.md), [roster and economy](ROSTER_AND_ECONOMY_IMPLEMENTATION.md)
- [ARIA coverage](AI_AND_ARIA.md), [acceptance](ACCEPTANCE.md), [work packages](AGENT_WORK_PACKAGES.md)
- [Existing reusable builder handoff](../../AgentReports/SkirmishExpansion/S005/REUSABLE_MISSION_BUILDER_HANDOFF_20261011.md)
