# Expanded Skirmish technical architecture

Implementation specification, 2026-09-21. Existing owners are identified in [IMPLEMENTATION_HANDOFF](IMPLEMENTATION_HANDOFF.md); all newly named types below are **proposed**, not assumed to exist. Follow [SOLID/ECS](../../Architecture/gameplay_solid_ecs_contract.md). Runtime gameplay belongs in ECS systems/data; views bind and forward input. No global manager/singleton or per-battle controller.

## Files, assemblies and ownership

Paths are repository-relative under `Assets/Game/Scripts/` unless stated otherwise. Use existing asmdefs for new folders except the small new contracts boundary.

| Location / assembly | Add or extend | Concrete responsibility |
|---|---|---|
| `Skirmish/Contracts/` / new `Game.Skirmish.Contracts` | `SkirmishScenarioContracts.cs`, `SkirmishObjectiveContracts.cs`, `SkirmishRosterContracts.cs`, `SkirmishCheckpointContracts.cs` | Immutable IDs/enums/payloads; no Unity scene, UI implementation or runtime services |
| `Configs/Skirmish/` / `Game.Configs` | `SkirmishScenarioDefinitionConfig`, `SkirmishObjectiveConfig`, `SkirmishArmyProfileConfig`, `SkirmishStartPackageConfig`, `SkirmishSizeConfig`, `SkirmishDifficultyConfig`, `SkirmishEconomyConfig`, `SkirmishUpgradeConfig`, `SkirmishIntelConfig`, `SkirmishRoleCatalogConfig`, `SkirmishMapLayoutConfig`, `SkirmishPublicationConfig` | Authoring inputs, explicit references and versioned configuration |
| `Configs/SkirmishBattleCatalogConfig.cs` / existing | Add definition reference/content version and publication readiness | UI identity/index; legacy prototype mapping remains a compatibility record |
| `Components/Skirmish/` / `Game.Components` | Components/buffers listed below | Blittable authoritative state and compiled blobs |
| `Runtime/Skirmish/` / `Game.Runtime` | New ECS setup, economy/capacity, objective, upgrade, AI policy and checkpoint systems | Expanded behavior, gated by expanded-session tag |
| `Systems/Skirmish*.cs` / existing Runtime | Narrowly adapt/extract `SkirmishRulesSystem`, population/catalog/combat policies, world setup and squad assignment | Preserve old prototype path; route expanded sessions to shared typed owners |
| `Composition/Skirmish/` / `Game.Composition` | `SkirmishDefinitionCompositionSystemHelper`, `SkirmishSceneBindingSceneSystemHelper`, `SkirmishCheckpointCompositionSystemHelper` | Load/compile configs once, inject shared scene/persistence boundaries; no strategic policy |
| `Composition/SkirmishLaunchProjection.cs`, `MatchSceneView*.cs` / existing | Definition-based launch branch and active-mode dispatch | Existing scene loader/readiness is reused; no per-S-ID route cases |
| `UI/Contracts/` / `Game.UI.Contracts` | `UiSkirmishLibraryModel`, `UiSkirmishObjectiveModel`, `UiSkirmishArmyModel`, `UiSkirmishProductionModel`; extend `IUiSkirmishGateway` | Public models/commands with revision and reason codes |
| `UI/Shell/Ecs/` / `Game.UI.Shell.Ecs` | `SkirmishUiProjectionSystem`; extend `AriaSkirmishPlanSystem`, gateway and observation projection | Changed-data UI projection; legal public planning and visible input |
| `UI/Components/`, `UI/Screens/` / `Game.UI.Runtime` | Extend `SkirmishMatchView`, `QuickCustomScreenView`; add `SkirmishArmyPanelView`, `SkirmishObjectiveHudView`, `SkirmishUpgradePanelView` | Canvas/UI state only; use the shared shell and layout |
| `Persistence/` / `Game.Runtime` | `SkirmishExpandedSaveData`, `SkirmishCheckpointSaveData`, extend `SkirmishSaveMigration`, `SaveService` | Serialization/migration, atomic writes and result receipts |
| `Editor/Skirmish/` / Editor boundary | `SkirmishDefinitionBuilder`, `SkirmishSetupCompilerValidation`, `SkirmishMapLayoutBuilder`, `SkirmishPublicationValidator`; extend existing `SkirmishBattleCatalogBuilder` | Assets through Editor APIs, generated reports and readiness enforcement |
| `Assets/Tests/Editor/SkirmishExpansion/` / `Game.Tests.Editor` | Data-driven suites by system/objective and one scenario fixture table | Validate every manifest row without 120 bespoke test engines |

Directional references: Contracts → base libraries only; Components → Contracts/Unity ECS; Configs → Contracts/Components; Runtime → those lower boundaries; UI.Contracts → Contracts; UI/Composition/Editor → owners they consume. Runtime must not depend on concrete UI/Composition/Editor. Pure frequent systems use Burst/jobs; classified managed save/config/UI edges do not decide gameplay. Preserve existing `.meta` files and architecture tests.

## Definition and launch schema

`SkirmishScenarioDefinitionConfig` fields:

```text
CatalogId: S001...S120                # stable public catalog ID
DefinitionId: skirmish.sNNN          # expanded runtime definition
ScenarioSetupId: scenario.skirmish.sNNN
OperationMapId / MapLayoutId / ContentVersion / ContentHash
ObjectiveConfig / ArmyProfileConfig / StartPackageConfig
DefaultDifficulty=Regular / FirstVisitSize=Standard / RecommendedSize
EconomyConfig / UpgradeCatalog / IntelConfig / RoleCatalog
PlayerFaction=1 / EnemyFaction=2 / SideRule
AllowedCertifiedSizeIds / RequiredFeatureIds / AriaCapabilityIds
BriefingKeys / ResultReasonKeys / ReadinessManifestId
```

Do not use catalog `S001` as an operation-map scenario ID: the shared validator expects dot-scoped lowercase IDs. Each expanded definition has `scenario.skirmish.s001` etc.; old prototype scenario IDs remain in the compatibility map and save record. DB/CC/IB retain their observed physical/logical map IDs; new MP/AP IDs are declared in MAP_IMPLEMENTATION. Content revisions never change these IDs.

`SkirmishLaunchPayload` contains `{SessionId, CatalogId, DefinitionId, ContentVersion, AllConfigHashes, MapId, LayoutId, DifficultyId, SizeId, Seed, IsCustom, IsLegacy, TransitionId, ReturnSelection}`. `SkirmishResolvedSetup` is the immutable compiled result: exact per-faction unit/structure/support entries, queue capacities, usable stores, loaded vehicle fuel, objective-role assignments, caps, routes/anchors, AI profile, objective parameters, checkpoint version. Derive a canonical hash from stable serialized ordering. Briefing, simulation, enemy AI, ARIA, save and result all reference the same snapshot.

Compiler API at the managed boundary: `TryCompile(definition, difficulty, size, seed, contentManifest, out setup, out reasons)`. It must:

1. Resolve versioned references and supported device/content capabilities; reject missing definitions instead of defaulting to Desert Base.
2. Expand army/start/size tables, then apply BT designation and CE objective trucks/defender towers. `INITIAL_SETUP_MATRIX.csv` provides 360 expected numeric test vectors.
3. Bind each semantic role to exact certified config/prefab keys and canonical tactical stat overlays. Apply no profile/account upgrade to battle stats.
4. Assign stable starting-object IDs and explicit designated-base IDs. Expanded startup does not discover its HQ by searching for the first Barracks substring.
5. Check all population/Supply/support/storage/queue/transport/landing/route limits; check staged producers and all roster prerequisites. No cap fix by silently deleting a starting unit.
6. Resolve seeded legal deployment offsets and AI personality with independent named streams. Seed never changes fixed roster quantities, starting value, terrain or objective rules. Save resolved initial positions before activation.
7. Produce a report containing quantities, costs, capacities, preloaded fuel, prerequisites, map hashes, public briefing facts, and unfulfilled blockers.

Use typed enums and records; no runtime parsing of `working_title_en`, filenames, prose packets or comma-separated role strings. Source CSVs are design inventories; Editor builders can import validated rows into typed assets. Index catalogs once per transition rather than loading Resources in hot loops.

## ECS state and writers

| New component/buffer | Fields / sole writer |
|---|---|
| `SkirmishExpandedSessionComponent` | Payload identity, compiled blob, phase, tick clock, content version; launch/lifecycle owner |
| `SkirmishResolvedSetupComponent` | Immutable setup blob reference and hash; composition projection |
| `SkirmishAttemptOwnedComponent` | Session and stable object ID on each spawned unit/building/support object; startup/cleanup |
| `SkirmishObjectiveStateComponent` | Kind, active state, terminal flags and reason; objective systems/outcome arbiter |
| `SkirmishCaptureZoneComponent` | Stable zone ID, owner, neutralization/capture progress, empty grace ticks, counts; FC system |
| `SkirmishFactionTicketsComponent` | Faction, remaining integer tickets, fractional tick accumulator; FC system |
| `SkirmishBreakthroughComponent` | Corridor states, latched exit-open, 12 designated IDs, live/dead/evacuated counts; BT system |
| `SkirmishConvoyComponent` | Three truck IDs, delivery progress, Delivered/Destroyed/Live state; CE system |
| `SkirmishObjectiveRoleComponent` | Object role, scenario identity, subject ID; startup projection, never UI |
| `SkirmishCapacityComponent` | Faction/category/Supply/support caps and reserved/in-transit/live counters; reservation/lifecycle systems |
| `SkirmishProductionReservationComponent` buffer | Reservation ID, role, quantity, paid Materials, Supply/category cost, phase, producer, consumed receipt |
| `SkirmishUnitRoleComponent` | Immutable role ID, target domains, counter tags, population category, base-stat reference; startup/spawn |
| `SkirmishReadinessComponent`, `SkirmishUpgradeStateComponent` | Faction stage, completed upgrade bits and active research ticks/cost receipt; research system |
| `SkirmishArmyGroupComponent`, `SkirmishGroupMemberComponent` | Stable squad/group ID, members, current order version, pin state; group system |
| `SkirmishKnownContactComponent` buffer | Observer faction, visible/last-seen/approximate contact, age/confidence, legal location; shared visibility projection |
| `SkirmishRuntimeFactComponent` buffer | Stable object ID, tick, death/delivery/exit/research event, sequence; lifecycle/fact projection |
| `SkirmishResultComponent` | Frozen result/hash/session/version, deciding facts, save acknowledgment; arbiter then settlement |
| `SkirmishCheckpointRequestComponent` | Session, safe tick, requested reason, persisted revision/hash acknowledgment; checkpoint system |

Component buffers use `IBufferElementData`, state uses `IComponentData`; runtime IDs fit bounded fixed strings. Serialized DTOs store stable object IDs, not Entity values or scene object references. Keep pending objective subjects in the role ledger even after visual entities disappear; a missing Entity is not an unearned delivered/evacuated success.

## Runtime systems and integration order

| New owner | Work to implement |
|---|---|
| `SkirmishSessionInitializationSystem` | Require resolved snapshot + shared operation-map/grid/catalog readiness; project faction startup and explicit role identities once; reject duplicate initialization |
| `SkirmishScenarioSpawnSystem` | Submit starting buildings/units/logistics through existing shared spawn boundaries, preserving ownership and placement checks; request queued reinforcements only through paid production |
| `SkirmishRosterProjectionSystem` | Apply per-role base stats/capabilities once, then upgrade revisions; replace blanket soldier tuning for expanded sessions |
| `SkirmishCapacityReservationSystem` | Validate and reserve cost/category/Supply/support once on accepted production request; typed role eligibility; expose rejection reasons |
| `SkirmishCapacityLifecycleSystem` | Transfer reserved → in-transit → live without changing total; release on cancel/death exactly once; passengers are never recounted |
| `SkirmishResearchSystem` | R2/R3 and category upgrades, paid timer/refunds, producer failure, one effect application from base stats |
| `SkirmishArmyGroupSystem` | Stable four-person squads, explicit multi-group selection, replacements and paging-independent IDs; compatible mixed-domain orders |
| `SkirmishObjectiveFactProjectionSystem` | Shared combat/transport/zone/position/destruction output → session-specific facts, after relevant structural playback |
| `SkirmishBaseAssaultObjectiveSystem` | BA objective predicate from original designated base identities |
| `SkirmishFrontlineObjectiveSystem` | FC zone ownership/progress and tick-based ticket drain |
| `SkirmishBreakthroughObjectiveSystem` | BT corridor unlock and designated living dismounted exit accounting |
| `SkirmishConvoyObjectiveSystem` | CE per-truck arrival dwell and loss accounting; role restrictions at shared command boundary |
| `SkirmishOutcomeSystem` | Combine terminal facts, simultaneous events, surrender and objective-specific deadline; freeze one result and simulation |
| `SkirmishEnemyStrategySystem` | Public/own faction legal observations → objective/group/counter/expansion policy; normal affordable command requests |
| `SkirmishCheckpointSystem` | Snapshot/restore request coordination at safe simulation point; shared subsystem adapters own their state |
| `SkirmishResultSettlementSystem` | Exactly-once scenario/difficulty/size/version completion + last result; no account reward in this expansion |
| `SkirmishSessionCleanupSystem` | Dispose blobs/queries/session entities, cancel input/production/camera/audio, release capacities/routes/landing slots |

Schedule expanded systems explicitly: accepted input → normal production/research/movement/combat/transport → death/destruction/structural playback → objective facts → capture/exit/delivery/tickets → outcome → public projections/checkpoint request. Mission time advances only while active and unpaused. City Operations day systems are unrelated. No second combat/movement engine, no direct UI writes to health or tickets, no framerate-dependent ticket drain.

The existing `SkirmishRulesSystem` must not also execute its prototype base-death/900-second rule on an expanded FC/BT/CE session. Gate the legacy path on `IsLegacy`. Extract reusable lifecycle pieces in focused changes; do not rewrite the entire shared game before an expanded BA can run. Maintain M01–M05 and all three prototype configurations during these changes.

## Production, upgrades, and ownership seam

Reuse existing building production request/delivery/queue owners and shared faction Materials/Fuel/Oil. Introduce typed eligibility/reservation data at the **request acceptance** boundary so player UI and enemy AI cannot disagree. A capability check in a display card alone is insufficient. Ground vehicles leave a real staging pad, helicopters use valid helipads, jets/planes use runway-aware delivery. New roles never bypass `BuildingProductionRequestBoundary` or the corresponding shared production components.

Reservation state machine:

```text
Requested -> Rejected(no mutation)
Requested -> Reserved(cost deducted, capacity reserved) -> Producing
Producing -> WaitingForExit(no duplicate deduction) -> InTransit -> Live
Reserved cancellation -> 100% Materials refund, release reservation
Producing/Waiting cancellation -> 75% Materials refund, release reservation
Pre-dispatch system spawn failure -> 100% refund, release reservation
Producer destroyed before dispatch -> committed cost lost, reservation released
InTransit destruction -> cost lost, capacity released; no cancel/refund
Live death -> capacity released; no refund
```

Round 75% refunds down to integer Materials; record paid amount, not recalculated current price. A destroyed producer never grants a second refund from a late queue callback. If only a landing slot is blocked, wait/replan through supported routes with a visible reason; it is not a free refund/teleport. Command tokens and reservation IDs make retries idempotent.

Store four-person infantry production as quantity 4 with one squad cost and four individual capacity reservations. Partial dispatch must track each member, avoiding 4-for-1 budget mistakes. Request 2 squads means 8 infantry, twice the cost and Supply. Delivery aircraft are support carriers, not extra tactical aircraft; their passengers retain their own counts. CE trucks have a separate authored objective-support allowance and cannot enter ordinary sell/board/replace commands.

Each living member belongs to at most one persistent tactical group; multi-group selection is a temporary deduplicated union. Reassignment removes prior membership atomically. Squads retain their original identity through losses/boarding; a mixed-domain group dispatches only compatible orders to its members and reports exclusions. The most recent accepted manual order overrides autonomous group intent. Reinforcements join only an explicitly chosen compatible group; they never acquire a BT designated identity.

R2/R3 research lives on the designated faction research provider; initially the main Barracks. Loss of that building cancels unfinished research with the specified destruction loss. A replacement Barracks can act as research provider while alive, but cannot replace the destroyed **designated objective base identity** for BA/FC. R2/R3 stage persists once completed. Category research providers: infantry Barracks, vehicle Ground Staging, aircraft Helipad or Airport. Completed effects survive provider loss until the match ends. Apply effects from immutable base data using revision IDs; health upgrades preserve current percentage.

## Launch, UI, and replay

New `SkirmishSelectionSaveData` stores CatalogId, content version, difficulty, size, seed, filters, favorite IDs, and Custom overrides. Default selects S001 expanded only when published; otherwise the legacy prototype remains a clearly labeled entry. For S004 etc. do not synthesize an integer `ScenarioIndex=4` and add a Resources switch. Resolve the catalog definition reference through the new resolver. The stress probe remains Editor-only with no completion eligibility.

The library uses current `QuickCustomScreenView` surfaces; extend existing filters/search/card selection instead of a parallel screen. Expanded briefing shows exact setup compiler output, both sides' start/asymmetry, roster restrictions, objective win/loss/deadline, selected size/caps, shared fog rules, and readiness. Keep all 120 visible as development entries if desired, but disabled Planned rows explain missing capability and are not publishable.

Preserve current HUD command layout; add contextual objective panel, Army drawer/group cards, quantity/queue selection and upgrade surface. FC exposes three zones and both ticket counts; BT shows two corridor holds, exit state and alive/evacuated/required; CE shows three trucks' health/status, delivered/required, route choices and selected convoy. Every blocked action returns an actionable localized reason. UI does not manufacture world-state success from a button event.

Replay starts a **new** SessionId with the same selected setup/seed and reset upgrades/resources/results; Adjust Setup preserves selection but closes the old session. Stop/physical takeover stops ARIA touches immediately. ARIA does not auto-replay or auto-start the next battle. Run completion saves only `(CatalogId, ContentVersion, DifficultyId, SizeId, outcome, elapsed, seed, input provenance)` plus optional best records; Custom/legacy/stress stay separate. No new Credits/Command/progression rewards are in this scope.

## Save migration and full checkpoints

Keep prototype schema-2 quickgame data interpretable. Map indices 0/1/3 to legacy DB/CC/IB; 2 to the Editor stress setup only. Preserve legacy Normal/full-vision behavior for legacy replay. For a newly selected expanded match, map displayed defaults to Regular/Standard and a legal seed, without reusing old victories. Unknown IDs/versions or missing content return safely to setup with an explanation; never overwrite a newer save with defaults.

`SkirmishCheckpointSaveData` contains header/schema/version/checksum, immutable resolved setup, simulation tick/RNG streams, stable unit/building identities/transforms/health/fuel/base stats, queues and reservations, research, resources/stores/hauler cargo, unit orders/groups, transport passengers/landing reservations, visibility/last-seen state, FC progress/ticket remainder, BT designated identities/exit timers, CE cargo/dwell state, result receipt and user settings needed to resume. Shared adapters own movement, combat, production and transport snapshots; do not build an Operations-only or Skirmish-only duplicate of the same mechanic.

Checkpoint on safe startup, significant objective transitions, every 30 simulation seconds initially, and app pause. Pause simulation while collecting a consistent safe snapshot; write temp/flush/replace then atomically publish its reference, retaining a previous valid generation until acknowledgment. Never serialize Entity handles. Restore into a fresh world/session instance with the **same logical attempt identity**, remap stable IDs, reconstruct reservations/queries/visibility, validate hashes, then enable simulation. UI/ARIA ephemeral gestures are canceled and replanned from public state.

Save a terminal result journal before settlement, then atomically store result/completion/receipt. A duplicated identical callback returns its receipt; conflicting results for one session are rejected and preserved for diagnosis. Use a serialized revision-checked save boundary so other quickgame/profile writers cannot overwrite completion. Operations planning already proposes compatible commit/checkpoint foundations: reuse the owning shared subsystem when implemented; do not assume those proposed classes exist.

Valid interruption resumes the exact match. Corrupt/incompatible checkpoint offers explicit safe setup return or a fresh replay; it cannot grant a win or reset resources inside the same accepted attempt. Until full checkpoint evidence passes, War/Large War recommendations remain disabled. Technical/Editor fixtures that resume only setup must say so.

## Required engineering outputs

Every implementation package supplies source/asset changes, updated schema and capability versions, narrow tests, exact wrapper logs, a normal UI/world outcome, ARIA behavior evidence for touched skills, and affected Campaign/legacy regressions. Every scenario supplies its packet assets and the certification manifest row. Distinguish definition compile, actual launch, normal play, ARIA win and device acceptance; no single boolean in a CSV certifies them all.
