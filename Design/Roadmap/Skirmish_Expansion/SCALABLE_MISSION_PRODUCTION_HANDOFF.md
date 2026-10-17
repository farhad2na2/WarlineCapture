# Implementation handoff for scalable Skirmish production

Date: 2026-10-14. Start here, then read the [production strategy](SCALABLE_MISSION_PRODUCTION_STRATEGY.md).

## Assignment and expected outcome

Implement a reusable mission authoring and qualification workflow for the existing 120-entry catalog. Replace repeated mission-specific builder and test-script work with a small generic builder, a parameterized normal-input runner, preflight checks and a resumable queue. Establish measured throughput with six ground Base Assault candidates before expanding to all objective/army families.

The owner wants a few missions per day rather than weeks on one mission. Proposed steady-state targets are 2–4 Editor-qualified missions/day with one native worker, or 4–8 with two independent machines. These are pilot targets, not measured performance or publication promises. Batch generation of 120 draft definitions can take hours after the factory and source/spec inputs exist; full acceptance cannot.

Prepared against main commit `3ebb2a6f4137ab8af0f938c139d30d7fe276353a`. Reinspect current code and ownership at pickup. Only the strategy and this handoff were created for this assignment; no factory, new scenario assets or native qualification was implemented by this documentation task.

## Read these contracts before changing code

Read root `AGENTS.md`, the [Campaign map reuse policy](../../MapVariants/CAMPAIGN_MAP_REUSE_POLICY.md), [implementation handoff](IMPLEMENTATION_HANDOFF.md), [technical architecture](TECHNICAL_ARCHITECTURE.md), [objective implementation](OBJECTIVE_IMPLEMENTATION.md), [ARIA requirements](AI_AND_ARIA.md), [acceptance](ACCEPTANCE.md) and the [existing builder reuse handoff](../../AgentReports/SkirmishExpansion/S005/REUSABLE_MISSION_BUILDER_HANDOFF_20261011.md).

Author from [SCENARIO_CATALOG.csv](SCENARIO_CATALOG.csv), [INITIAL_SETUP_MATRIX.csv](INITIAL_SETUP_MATRIX.csv) and the [twenty scenario packets](Scenarios/README.md). Preserve S001–S120, all 360 setup rows, counts, costs, caps, deadlines, side rules, entitlement policy and existing runtime/save mappings. S001/S025/S073 legacy mappings and stress index 2 remain protected. Work ordinals never become runtime indices.

Use existing Campaign physical sources. New logical assets, anchors, bounds and legal scenario facilities are allowed; new terrain, roads, bridges, runways, scenery compounds or physical scene derivatives are outside this assignment. A source-fit failure means selecting another existing source or recording the unresolved gate, not changing the planned army/budget.

## Coordinate with active owners

The S005 owner is the existing chat **Verify skirmish mission maps**, ID `01a10222-a76b-7593-a577-416eff36f59b`, host `local`. Its evidence is under `Design/AgentReports/SkirmishExpansion/S005`. At preparation time, that agent was active and the main checkout had unrelated generated runtime scene and current-readiness changes. Do not absorb those changes into a production-pipeline commit.

Inspect the owner's current status, reserved files and native lane before working. Preserve its frozen candidates, test artifacts, Unity processes and remaining acceptance. Coordinate through the supervising chat; do not assume this document alone grants permission to message another chat. Parallel work must use disjoint files or an agreed integration boundary.

Use a dedicated `codex/` branch/checkout for implementation, with current main as the starting point. Prepare the runner/schema outside an active S005 source freeze. Integrate shared code changes at an agreed batch boundary. This handoff is not an instruction to interrupt S005 or take over its aircraft, AI or recovery work.

## Existing implementation hooks

All paths below are relative to the repository root.

| File | Existing behavior and required change |
|---|---|
| `Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.MenuProbe.cs` | Internal journey parameters already include mission, seed, locale and manual/ARIA mode. Expose a small reviewed generic adapter; replace mission-specific candidate projection without replacing ordinary Menu input. |
| `Assets/Game/Scripts/Editor/Skirmish/SkirmishDefinitionBuilder.cs` | Current rebuild rewrites shared S002–S004 assets and calls an S004 map builder. Add a config-only batch path with no physical map side effects. |
| `Assets/Game/Scripts/Configs/Skirmish/SkirmishExpansionCatalogFactory.cs` | Concrete authored-set fields include S002–S005. Add generic definition/profile lookup while preserving compatibility adapters. |
| `Assets/Game/Scripts/Configs/Skirmish/SkirmishExpansionCatalogConfig.cs` | Definitions array and `TryGetDefinition` are generic; `ToAuthoredSet` still has concrete ID dispatch. Make its general path preserve all authored definitions. |
| `Assets/Game/Scripts/Composition/SkirmishLaunchProjection.cs` | Ordinary launch contains scenario-index dispatch and expanded Regular/Standard defaults. Resolve valid catalog identity/source/registry generically; preserve existing supported publication and legacy behavior. |
| `Assets/Game/Scripts/Configs/Skirmish/SkirmishSetupCompiler.cs` | Reuse exact setup accounting and compile reasons. Check the complete expected result set; any-one-success is not batch success. |
| `Assets/Game/Scripts/Configs/Skirmish/SkirmishPublicationValidator.cs` | Generalize identity/capability evaluation while retaining current air, role, recovery, manual, ARIA and device obligations. Missing evidence stays blocked. |
| `Tools/CI/audit_skirmish_native_contacts.py` | Reuse actual normal-contact/receipt auditing; do not create a new lossy success detector. |
| `Tools/CI/invoke_unity_macos.sh` | Own every macOS Unity authoring/validation/capture run, with RTK, log, timeout and pass-marker verification. |

These are extension points, not permission for a broad engine rewrite. Search for additional ID branches and consumers before editing. The other 90 entries depend on shared Frontline Control, Breakthrough and Convoy Escort support; inspect their implementation status and retain separate capability tickets.

## Work order and acceptance for the first package

### Step 1 Stabilize job preparation and qualification

Define one versioned job format with catalog/definition identity, immutable candidate/build and source manifest, setup/content hash, integer packet seed, locale, size, difficulty, control mode, required stages, timeout and unique artifact directory. Resolve seeds from the relevant scenario packet. Keep job files and run outputs outside imported Unity source assets.

Build offline validation for schema, response shapes, required fields, paths, expected proof counts and qualifier negative controls. Turn observed fixture failures into reusable regression cases. C# compilation and live API compatibility must pass before a full Play journey; an offline syntax check does not prove Unity type compatibility.

Done when malformed jobs fail before native launch, actual driver failure categories remain visible, and the qualifier rejects missing or mismatched terminal, source, input, return and ownership evidence. No old failed run may become successful simply because parsing became more permissive.

### Step 2 Parameterize the existing normal journey

Add a small adapter over the existing harness. Launch through ordinary library/setup controls on an isolated QA profile. Candidate exposure for testing must not alter main publication or ship developer controls. Use genuine visible touch input, normal simulation speed, real paid resources, normal result rendering and return.

Both helper and ARIA must scroll to offscreen squad cards and verify the selected stable identity. Distinguish an accepted legal resolved destination from the raw world click. Keep short-lived command acceptance receipts separate from eventual movement/damage outcomes. Observers remain read-only and cannot feed hidden knowledge into ARIA or mutate gameplay. Camera guidance never issues troop orders.

Done when the same adapter completes two already-authored missions without copied mission-specific runner code. Record manual-control automation separately from unfamiliar human/player evidence. Reuse valid current evidence where applicable; do not repeat unchanged checks just to accumulate run counts.

### Step 3 Add generic config-only authoring

Create/import typed definition/layout assets for a requested catalog batch through Editor APIs. Reference shared profiles and existing Campaign sources. Preserve asset GUIDs and unrelated content. Do not call a broad map/environment rebuild.

Generalize lookup and launch incrementally. Unknown IDs must fail clearly; they cannot fall back to Desert Base/S001. Preserve existing coded sources and mappings. Keep draft entries unpublished and keep unsupported setup/capability reasons explicit.

Done when rebuilding an identical batch produces no asset diff, all expected rows have either a valid compiled result or a specific reason, and existing published/legacy entries retain their behavior. No generated definition earns Playable status from existence, screenshots or shared-system tests.

### Step 4 Queue frozen candidates and select affected checks

Persist job states and exclusive worker claims. After interruption, resume pending jobs and preserve interrupted attempts as incomplete. Keep unique logs/traces, wrapper completion ownership and idempotent postprocessing. Verify a clean mission session and restore the isolated profile between attempts.

Retain full source provenance at start/end. Reuse shared evidence only for matching relevant dependencies, runner parameters and applicable coverage, retaining its original source identity. Changed content still needs that mission's source-fit and affected native journeys. If dependency scope is uncertain, broaden the rerun set.

Done when a queue can resume safely, a shared capability defect blocks only its consumers, and each passed job links exact candidate/input/outcome artifacts. After two identical failure signatures, stop full automatic reruns and repair the smallest reproducer first. Keep unaffected jobs moving.

### Step 5 Measure the six-mission pilot

First draft pool: S026, S049, S050, S074, S097 and S098, the remaining ground Base Assault tuples. Confirm source fit and required capabilities before instantiation. Preserve S001, S002, S025 and S073; do not modify S005's active candidate.

For each draft, check native full-footprint placement, exits, starting census, camera/bounds, objectives, EN/FA briefing and two viable approaches before long matches. A source registry can reuse unchanged geometry analysis, but it cannot certify new facility coordinates without native evidence.

Run each applicable mission matrix. Report content preparation time, native minutes, first-attempt job success, losses, driver/infrastructure waste and external gates. Forecast the next twelve from these measurements. Proposed pipeline goals are at least 90 percent first-attempt job success and under 10 percent native time wasted on driver/infrastructure defects; these are not ARIA gameplay win-rate claims.

## Coverage and publication rules

### Camera readability (owner direction, 2026-10-15)

Every mission must open at a useful distance where nearby soldiers are readable rather than dots or impostors. Keep transport aircraft comfortably inside the unobscured viewport without dominating the screen. Do not widen ordinary ground zoom just to show an entire army.

Check actual native rendering at the normal view, both legal zoom limits and the restored view, including landscape and portrait layouts. Retain posed infantry/group visibility and occlusion evidence; one visible soldier does not certify a roof-obscured squad. For applicable aircraft, check their actual legal parked/staging, production, Follow and cruise views. Use mission-specific source elevation and actual drawn geometry, not culling bounds alone. Camera transitions and guidance must not issue troop orders.

Send native screenshots for asynchronous owner review and continue authorized work without waiting for visual acceptance. Record pending visual/device/player gates separately; shared camera tests and screenshots alone do not establish mission readiness.

Keep the governing requirements intact: six Regular ARIA runs per entry with three seeds per locale and at least two wins per locale; 20 stress seeds per map/objective/army family, alternating starts; additional wins for exposed difficulties/sizes; manual win, two approaches, objective edges, recovery/localization, applicable lifecycle/capacity, device and player gates.

Across the catalog, Regular contributes at least 720 full runs and the 60 family stress samples contribute 1,200 sample slots. Schedule a genuine run into multiple coverage sets only when its exact seed/start/locale/setup qualifies for all of them. Replaying a seed in another locale does not supply another distinct stress seed. Keep every loss and failure; do not replace required normal matches with accelerated, injected or template outcomes.

Keep visual review, automated fixtures, normal-input journeys and real phone/player acceptance separate in reports. The production queue can show Editor qualification and device pending independently; only the existing publication policy decides Playable for the certified combinations. No phone access means a device gate stays open.

## Deliverables and handback

The first one to two engineering days are a time budget for the smallest runner/preflight and BA-only pipeline slice, not a promise to complete aircraft, AI or three additional objectives. If lookup/launch refactoring proves larger, deliver the useful runner/preflight first and retain a bounded generic-authoring ticket.

Return these concrete artifacts:

1. A reviewed job schema, generic adapter, preflight tests and queue commands with documented ownership and exact pass markers.
2. A config-only batch builder and regression evidence for idempotence, identities, setup accounting and preserved current bindings.
3. A source/capability registry for the pilot with native fit evidence and unresolved gates.
4. Per-mission evidence links and status for the six candidates; failed attempts and external gates retained.
5. A measured capacity forecast and next batch list, plus separate shared FC/BT/CE and advanced-air dependencies.

Report actual implemented behavior and checks, then open work. Do not report all 120 ready because assets were generated. Commit only owned changes at stable boundaries; coordinate shared integration and publication with the active owners.

## Unity execution and process ownership

Keep Unity Hub open and signed in. Wrap all macOS Unity operations with RTK and the checked repository wrapper; never invoke the Editor executable directly or pass batchmode. Use explicit logs, timeout and required markers. Verify connected Editor idle/domain reload state before dispatch. Do not terminate any Unity/Hub/licensing process or clear IPC to make room.

Use one dense native Play lane on the current host while preparing content/offline checks in parallel. A second native worker belongs on a separate suitable licensed machine with platform wrappers, independent profiles and unique evidence paths. Preserve full validation logs and failed evidence.
