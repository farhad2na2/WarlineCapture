# Operations implementation architecture

All types below are **proposed**, except existing seams identified in [BASELINE](BASELINE.md). Build shared systems once; missions are validated assets, not 60 controllers. Follow the repository's ECS naming, dependency, allocation, and wrapper rules.

## Ownership and assembly placement

| Folder / assembly | Types to add | Responsibility |
|---|---|---|
| `Assets/Game/Scripts/Operations/Contracts/` / new `Game.Operations.Contracts` | `OperationsContracts.cs`, `OperationsMissionContracts.cs`, `OperationsObjectiveContracts.cs` | Plain immutable payload/result records, enums, IDs; no UI, ECS world, Unity scene, or save service references |
| `Assets/Game/Scripts/Configs/Operations/` / existing `Game.Configs` | `OperationsCampaignConfig`, `OperationsDistrictConfig`, `OperationsMissionCatalogConfig`, `OperationsMissionDefinitionConfig`, `OperationsObjectiveGraphConfig`, `OperationsActionConfig`, `OperationsBalanceConfig`, `OperationsForcePackageConfig`, `OperationsThreatProfileConfig`, `OperationsRewardConfig` | Serialized authoring data; validation at import/build, projected once at startup |
| `Assets/Game/Scripts/Components/Operations/` / existing `Game.Components` | Components and blob records listed below | Authoritative runtime state, request/event buffers, immutable catalog blobs |
| `Assets/Game/Scripts/Runtime/Operations/` / existing `Game.Runtime` | All simulation systems listed below | City policy, offers, launch validation, objective rules, consequences; ECS only |
| `Assets/Game/Scripts/Composition/Operations/` / existing `Game.Composition` | `OperationsCatalogCompositionSystemHelper`, `OperationsMatchSceneSystemHelper`, `OperationsSaveCompositionSystemHelper` | Config/blob projection, injected scene and persistence edges; no gameplay decisions |
| `Assets/Game/Scripts/Persistence/` / existing `Game.Runtime` | `OperationsSaveData`, `OperationsSaveMigration`, `OperationsCheckpointData`, `OperationsProfileCommitUtilitySystemHelper` | DTO serialization, migration, revision-checked atomic profile writes, checkpoint journal I/O |
| `Assets/Game/Scripts/UI/Contracts/` / existing `Game.UI.Contracts` | `IUiOperationsGateway`, `UiOperationsModels` | Commands and immutable read models for dashboard, district, briefing, result, report |
| `Assets/Game/Scripts/UI/Shell/Ecs/` / existing `Game.UI.Shell.Ecs` | `UiOperationsProjectionSystem`, `UiShellEcsGateway.Operations.cs`, `AriaOperationsPlanSystem` | ECS-to-UI projection and permitted ARIA intents; no direct save mutation |
| `Assets/Game/Scripts/UI/Screens/` / existing `Game.UI.Runtime` | `OperationsScreenBinderView`, `OperationsMissionBriefingView`, `OperationsMissionHudView`, `OperationsMissionResultView` | Serialize references, bind models, forward input; no objective arithmetic |
| `Assets/Game/Scripts/Editor/Operations/` / existing Editor boundary | `OperationsCatalogBuilder`, `OperationsMissionAssetBuilder`, `OperationsMapContractValidator`, `OperationsCatalogValidation` | Deterministic Unity asset construction and diagnostics |
| `Assets/Tests/Editor/Operations/` / existing `Game.Tests.Editor` | Focused simulation, persistence, UI, mission, ARIA validation suites | Fixtures and assertions, not runtime success shortcuts |

Only the contracts assembly is new initially. Add directional references from Components, Configs, Runtime, UI.Contracts, UI.Shell.Ecs, Composition, and tests as needed to `Game.Operations.Contracts`. Contracts itself references only base libraries. Do not reference `Game.Configs` from Components or make Game.Runtime depend on Game.Composition/UI.Runtime. Keep named enums in the contracts assembly so Configs and Components can share them without a cycle. Any later runtime split is a separate dependency change with architecture validation.

Pure recurring systems use `ISystem`/Burst and explicit update groups where feasible. Save/config/scene/UI managed edges are classified managed boundaries, never an excuse for hidden gameplay policy. FixedString IDs, blob data, reusable buffers, and change versions avoid recurring allocation. No `OperationsManager`, static `Instance`, global registry, or per-mission MonoBehaviour.

## Core contracts

Suggested fields, not a claim of already compilable API:

```csharp
OperationsLaunchPayload {
  SchemaVersion; RunId; DistrictId; OfferId; MissionId; ScenarioId; OperationMapId;
  RunRevision; DefinitionVersion; ContentHash; DifficultyId; Seed;
  TransactionId; SessionId; AttemptOrdinal; LaunchSnapshotHash; IsPractice;
}
OperationsOutcomeKind { None, Victory, Partial, Defeat, Withdrawn, TechnicalFailure }
OperationsMissionResult {
  SchemaVersion; RunId; OfferId; MissionId; SessionId; AttemptOrdinal;
  DefinitionVersion; Outcome; TerminalReason; ElapsedTicks;
  MandatoryObjectiveFacts; OptionalObjectiveFacts; CivilianDeaths;
  ProtectedSiteFacts; DeliveredCargoFacts; ExtractedEvidenceIds;
  TaskForceLosses; InitialTaskForceCount; ResultHash;
}
OperationsCommand {
  CommandId; ExpectedRunRevision; Kind; DistrictId; OfferId; ActionId;
}
OperationsCommandResult { CommandId; Accepted; ReasonCode; NewRevision; TransactionId; }
```

Use bounded IDs <=60 ASCII bytes in ECS. `MissionId=operation.o001`; `ScenarioId=scenario.operations.o001`; district IDs `district.operations.d01`; planned new map ID `opmap.operations.old_quarter`. `RunId`, `OfferId`, session and transaction identities are generated opaque IDs, distinct from content identity. Never encode locale, seed or asset path in content IDs. Validate map/scenario `operations` namespace explicitly in `OperationMapIdentityRules` and extend its tests/documented grammar without renaming published Campaign/Skirmish IDs.

## Authoring schema

`OperationsMissionDefinitionConfig` contains: stable IDs; display/brief/debrief localization keys; district/family; scenario/map references; prerequisite expression; min Intel rule; force package; enemy profile and bounded reinforcement schedule; graph reference; expected duration/hard deadline; allowed command/build/air/transport capabilities; required feature IDs; outcome/consequence/reward profile; site/route/milestone bindings; ARIA affordance profile; readiness and content version. It owns **no scene object pointers for gameplay identity**.

`OperationsObjectiveGraphConfig` contains nodes `{NodeId, RuleKind, RoleBindings, TargetCount, DurationTicks, DeadlineTicks, RadiusCells, PrerequisiteNodeIds, ActivationPolicy, FailurePolicy, Optional, LocalizationKey}` plus typed rule parameters. Edges are acyclic; parallel nodes activate on all prerequisites unless explicitly `AnyOf`. `ActivationPolicy` distinguishes activate-on-prerequisites from activate-on-launch. Rule data uses typed records; no arbitrary strings evaluated as scripts. A mission brief's `A -> (B || C) -> D` means A then B/C in parallel, both required, then D; an actual choice uses explicit `AnyOf(B,C)`.

Compile ScriptableObjects into `OperationsCatalogBlob` / graph blobs at startup. Validate graph cycles (including objective-to-wave dependencies), unreachable nodes, duplicate roles, incompatible force/transport capacity, deadline feasibility, required command availability, map routes, save support, enemy budgets and EN/FA keys. The Editor build fails with a mission/node ID and actionable reason; runtime loading fails safely rather than substituting a default mission.

Asset paths by convention:

```text
Assets/Game/Configs/Operations/Catalog/OperationsMissionCatalog.asset
Assets/Game/Configs/Operations/Districts/D01/OperationsDistrict_D01.asset
Assets/Game/Configs/Operations/Missions/O001/OperationsMission_O001.asset
Assets/Game/Configs/Operations/Missions/O001/OperationsObjectives_O001.asset
Assets/Game/Configs/Operations/Missions/O001/ScenarioSetup_O001.asset
Assets/Game/Configs/Operations/Shared/{ForcePackages,ThreatProfiles,Actions,Rewards}/...
```

Reuse `ScenarioSetupConfig` unit groups/roles/anchor records with a dedicated neutral projection. New graph data lives in Operations configs. Do not enable Campaign `MissionRuntime` accidentally. Build via Editor APIs/checked builders, preserving `.meta` files; content authors must not hand-write Unity YAML.

## ECS entity/data model

| Entity/component or buffer | Key fields and writer |
|---|---|
| `OperationsRunComponent` on one run entity | RunId, revision, day, AP, difficulty, seed, phase, consecutive stable days; command/day/settlement systems only |
| `OperationsDistrictComponent` on six district entities | DistrictId, seven metrics, density, change version; consequence/day systems |
| `OperationsSiteStateComponent`, `OperationsRouteStateComponent` buffers | Stable site/route ID, state, last transaction; settlement/day systems |
| `OperationsMilestoneComponent`, `OperationsCompletionComponent` buffers | Mission/flag ID, first success, attempt history summary; settlement system |
| `OperationsOfferComponent`, `OperationsIncidentComponent` buffers | Offer/incident IDs, mission/district, expiry, status, immutable resolved modifiers; director/day systems |
| `OperationsCommandRequestComponent`, `OperationsCommandResultComponent` buffers | Public command identity, expected revision, acceptance/reason; UI gateway writes requests, command system writes results |
| `OperationsPendingCommitComponent` | Proposed revision, transaction ID, phase, expected profile revision; commit bridge publishes acknowledgment, simulation finalizes only on ack |
| `OperationsAttemptComponent` on active attempt entity | Session, offer, snapshot, phase, ticks, terminal latch; launch/lifecycle/outcome systems |
| `OperationsAttemptOwnedComponent` on spawned/bound entities | SessionId, stable runtime object ID, role ID; spawn/cleanup systems |
| `OperationsObjectiveStateComponent` buffer | NodeId, activation/completion/failure, progress, remaining ticks, reason |
| `OperationsMissionFactComponent` buffer | Fact kind, stable object/role ID, tick, count/value, event sequence; fact projection system |
| `OperationsObjectiveBindingComponent` buffer | Node/role to stable object ID and current Entity handle; binding/recovery system |
| `OperationsWaveStateComponent` buffer | Wave ID, trigger armed/fired, warning tick, remaining finite roster budget |
| `OperationsMissionResultComponent` | Frozen typed result and hash, settled status; outcome then settlement acknowledgment |
| `OperationsReadModelVersionComponent` | District/offer/objective/result/report versions; changed-data UI projection only |

`*Component` buffers implement `IBufferElementData`; components implement `IComponentData`. DTOs/contracts/configs are edges and may use their descriptive suffixes. Do not persist Entity handles. Use a stable per-attempt object ID and rebuild Entity lookup after load. Neutral civilian faction is identified using `FactionIdentity`, never faction 0 as player.

## Simulation systems and execution order

| New system | Reads → writes / responsibility |
|---|---|
| `OperationsRunInitializationSystem` | Migrated run snapshot/catalog → entities; creates one active run, supports cold resume |
| `OperationsCommandSystem` | Public requests/current revision → accepted command or reason; rejects duplicates, unavailable offers, active-attempt conflicts |
| `OperationsActionSystem` | Accepted abstract action/config → pending AP/metric transaction |
| `OperationsOfferDirectorSystem` | Committed day/district/prerequisites → saved offers and incident candidates; runs on state changes, not each frame |
| `OperationsDeploymentSystem` | Accepted Deploy → immutable launch snapshot and reservation transaction; pauses until persistence ack |
| `OperationsMissionLaunchSystem` | Saved reservation + existing map/grid/catalog readiness → Active attempt or technical failure; no simulation before all flags ready |
| `OperationsMissionSpawnSystem` | Scenario groups/overlay → attempt-owned role entities through shared spawn boundaries; does not own combat |
| `OperationsMissionFactProjectionSystem` | Combat/death, transport, scan, interaction, route events → deduplicated facts for this session |
| `OperationsObjectiveActivationSystem` | Graph prerequisites → active nodes; uses next-tick activation to avoid iteration-order cascades |
| Rule systems in MISSION_IMPLEMENTATION | Facts/active typed nodes → objective progress/failure |
| `OperationsWaveScheduleSystem` | Typed node triggers and simulation ticks → warnings and finite, authored group spawn requests |
| `OperationsMissionOutcomeSystem` | Objective state + failure floors → one frozen result; fail/success precedence from strategic rules |
| `OperationsConsequenceSystem` | Frozen verified result + original launch snapshot → proposed metric/site/reward delta transaction |
| `OperationsSettlementSystem` | Verified matching result/receipt state → one pending commit; accepted ack closes attempt and enables return |
| `OperationsDayAdvanceSystem` | Accepted End Day → simultaneous tick, receipts/report and next offers; transaction is all-or-nothing |
| `OperationsCheckpointSystem` | Stable active state on pause/periodic safe point → checkpoint request; restore request → rebuilt attempt after validation |
| `OperationsAttemptCleanupSystem` | Terminal/cancel transition → release attempt-owned entities, waves, selections, targets, reservations and camera requests |
| `UiOperationsProjectionSystem` | Committed state and active objective versions → read models; no strategic mutation |
| `AriaOperationsPlanSystem` | Public read models and supported action semantics → ranked recommendations / visible-control intents |

Within a tactical tick: commands → shared movement/combat/transport → death/destruction and ECB playback → fact projection → objective evaluation → outcome latch → projections. Define actual Unity update attributes after auditing existing group order; do not assume table order schedules systems. City simulation does not advance while a mission is active. UI rendering never advances mission time. Pauses freeze objective/wave clocks.

## Shared integration boundaries

Add mode-tagged launch dispatch in the shared match transition, with Campaign/Skirmish/Operations exclusivity assertions. Reuse `OperationMapReadinessComponent` / `ActiveOperationMapComponent`, `OperationMapSceneLoadingSceneSystemHelper` and the existing scene-load pipeline. Composition passes immutable payloads; an Operations launch system owns policy. Loader failures return to the original district selection and release reservations only after an acknowledged rollback.

Extract mode-neutral scenario spawning, route following, objective interactions, extraction facts and breach facts only as each family needs them. Keep existing Campaign adapters and tests. A general `ScenarioRoleSpawnSystem` can own reusable projection if the first integration audit demonstrates that boundary; do not rename every Campaign class before the first Operations loop works. Shared transports, scan, building damage/placement, navigation, fog, selection and combat stay in their current owning systems.

One active mode owns its attempt entities. Campaign and Skirmish runtime/result systems must require their own active-mode tag, so an Operations result cannot award Campaign progress. Repeated launch/back/quit/restore tests must show no stale objective, enemy wave, HUD, audio, camera or entity state crossing modes.

## Transaction and crash protocol

Add `OperationsSaveData operations` to `PlayerProfileSaveData`, with `schemaVersion`, profile revision, active run, run summaries, first-clear reward IDs, practice records, receipt journal, committed reports, pending deployment and checkpoint reference. Keep settings and quickgame unchanged. Operations migration initializes an empty optional field; it does not reset Campaign progress or legacy wallets. Unknown newer schema is read-only with recovery UI, never silently overwritten.

All profile mutations (including existing Campaign/progression writers) must use a serialized, revision-checked commit boundary before Operations acceptance. A cached `SaveService` writer cannot overwrite a newer Operations save. Inject the commit boundary; do not add a static save singleton. JSON atomic replace protects one envelope; use that envelope for AP, district deltas, account reward amounts, completion and receipts together.

Deployment protocol:

1. Validate `(CommandId, ExpectedRunRevision)` and build a snapshot containing definition version/hash, force package, costs, city modifiers, map/scenario IDs, seed and return route.
2. Atomically save `Reserved` plus AP decrement and a launch transaction receipt. Only then begin scene transition. Double-click returns the original acceptance, without a second decrement.
3. On map readiness, mark `Active`; on deterministic load/validation failure, commit a `RolledBack` receipt and AP refund once. A crash while Reserved resumes that same reservation and snapshot.
4. Save tactical checkpoints after safe startup, objective transitions and periodic safe points (initial target 30 seconds), plus mobile pause. Checkpoint file includes schema/content hashes, stable entity IDs, all relevant shared runtime state and a checksum. Write/flush/replace the checkpoint first, then atomically publish its reference in the profile. Retain the prior valid checkpoint until the newer reference commits.
5. On terminal outcome, save the immutable result in the pending-attempt journal before requesting settlement. The settlement key is `(RunId, OfferId, SessionId, AttemptOrdinal)`; validate definition version and result hash, then atomically commit all consequences/rewards and the receipt.
6. A duplicate identical result returns its receipt. A differing result for the same key is rejected as a conflict, logged and kept recoverable; never reapply. A stale callback from a disposed session cannot mutate the current run.
7. Return to Operations only after settlement ack. If the UI crashes afterward, resume the committed report/result. Reading or closing a result never grants rewards.

Recovery choices: a valid checkpoint resumes exactly that attempt. A missing checkpoint after a hard crash offers **Restart Attempt** with the same seed/snapshot/AP reservation, or **Withdraw** with the normal consequences. Neither option rerolls threats or grants AP. Content incompatibility/load corruption caused by an update is TechnicalFailure with one refund, old evidence retained, and a safe dashboard return. A user Retry after Defeat is a new live deployment costing AP; Practice remains free. An explicit restart of an active mission resets to its saved starting snapshot without rewards or extra AP; record restart count in evidence. Regular acceptance wins prohibit restarts.

Checkpoint scope is real shared gameplay state: positions/health, orders and reservations, transport passengers/cargo, visibility/intel, buildings/production/resources, objective timers, wave triggers, RNG state and irreversible facts. A setup-only save does not satisfy mobile recovery. Reuse a certified Skirmish/shared checkpoint facility if available; otherwise implement the neutral snapshot layer in the owning shared systems before releasing Operations. Do not serialize managed scene references or ECS Entity indices.

## UI and ARIA contract

`IUiOperationsGateway` exposes read models and `RequestDeploy`, `RequestAbstractAction`, `RequestEndDay`, `RequestConclude`, `RequestWithdraw`, `RequestPractice`, `RequestResume`. Conclude is accepted only when the authoritative Partial predicate is true; it cannot request Victory or change facts. Results carry localized reason IDs and revision; views disable duplicate requests while pending, then show committed facts. Existing Back behavior is navigation/cancel only. Replace dashboard placeholder callbacks and builder aliases explicitly; use shared shell route history and root header.

HUD shows active required objectives, optional objective disclosure, progress, timer only when relevant, escort/protected-object state, civilian harm summary, withdraw/pause, and camera-focus actions. Markers resolve through stable role bindings. Results display outcome, achieved/missed conditions, before/after district metrics, harm, next offers, and received rewards separately. Farsi mirrors layout without reversing numeric identifiers or map world positions.

ARIA gets public objective affordances `{nodeId, action kind, visible/last-known target, eligible role, route choices, risk, deadline, prerequisite reason}`. It gets no hidden enemy entity IDs, unrevealed evidence locations, director RNG, or settlement methods. Intent execution uses the existing ARIA visible-input pipeline. The new Operations Run Watch opt-in permits dashboard actions and End Day through the same controls; tactical Watch alone stays within the current mission. Human interaction interrupts/cancels queued intents. Every mission and the complete city run must pass the mandatory ARIA win gate in ACCEPTANCE.
