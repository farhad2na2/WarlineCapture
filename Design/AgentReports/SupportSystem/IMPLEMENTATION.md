# Support implementation specification

Approved direction: 2026-09-28. Owner said “I approve” after the project-asset correction. Approval covers the four abilities, full-screen Build-style selection popup, existing-model artwork and staged rollout. Do not ask again for that approval. This task prepares implementation instructions; no Support runtime is implemented yet.

Start with [AGENT_START.md](AGENT_START.md), then implement the ordered tasks in [TASKS.md](TASKS.md). Product behavior remains in [support_design_and_build_plan.md](support_design_and_build_plan.md); this document supersedes its earlier speculative runtime type names and resolves the technical details below. Use [support_asset_manifest.md](support_asset_manifest.md) for art. [support_defaults.json](support_defaults.json) is a machine-readable implementation fixture, not a runtime-loaded asset or a second permanent balance authority.

## 1. Scope and non-negotiable decisions

- Four abilities only: Smoke, Precision Strike, Paratroopers, Materials Supply Drop. First playable milestone is Smoke plus full-screen selection, targeting, confirmation, ARIA consent and actual combat mitigation.
- Campaign is the initial supported production mode. Test encounters may explicitly grant abilities in their own setup. Do not expose Support in Operations/Skirmish by merely carrying over old JSON `availableModes`; those modes need separate acceptance.
- Runtime is ECS data and scheduled ECS systems. Do not implement `SupportAbilityService`, a singleton, MonoBehaviour combat controller or static mutable registry. The old catalog's service name is historical and must be migrated to the actual ECS owner.
- Reuse exact source sprites or native model captures. No new aircraft, character model, weapon salvo, store, paid charge or extra failure charge.
- Separate player Support from owned transport and jet orders. Support aircraft presentation entities cannot be selected, auto-acquire targets, carry arbitrary units, drain normal movement Fuel in addition to the quoted cost, count toward objectives, or grant kill/reward credit as enemies.
- Do not add a global save/resume system, global damage rewrite or universal fog-of-war rewrite. Implement narrow adapters, fail closed on missing required data, and name the missing integration in evidence.
- Keep all current mission objectives/stars authoritative. An optional Support lesson must not become a wait condition for mission completion.

## 2. Verified source map

Paths below are repository-relative. They were inspected for this specification; recheck symbol existence when beginning a task because this workspace is active. `NEW` paths in later sections do not exist yet.

| Area | Existing source and concrete hook | Change boundary |
|---|---|---|
| Architecture | `Design/Architecture/gameplay_solid_ecs_contract.md` | ECS naming, directional assemblies, Burst, no recurring managed allocations, pure ECS airborne visuals |
| Runtime assembly | `Assets/Game/Scripts/Game.Runtime.asmdef` | New `Systems/Support/` inherits Game.Runtime; no new broad assembly or UI runtime dependency |
| Input alias | `Assets/Game/Scripts/UI/Screens/MatchOverlayCommandInputUiSystemHelper.CommandTabs.cs`, `IsScanAliasCommandButton`, `OnCommandTabRuntimeClick`, `OnScanButtonClicked`, `TryRequestMissionScan` | Remove only Support alias; preserve the dedicated Scan path, Radar mission and Operations Scan behavior |
| Action dispatch | `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestDispatchSystemHelper.cs`, `ProcessRequest` | Support/RightSupport opens new popup instead of `EnterScanTargetMode`; mirror Build's `CaptureUiClickSequence` discipline |
| Shell contracts | `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`; `UiShellPopupContracts.cs` | Append enum members, never reorder existing serialized values |
| Popup plumbing | `Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs`; `Assets/Game/Scripts/UI/Shell/UIShellView.cs`; `UIShellEcsPresentationSystem.cs` | Register/hide/destroy Support with normal shell lifecycle; close conflicts and suppress click-through |
| UI reuse | `Assets/Game/Scripts/UI/Screens/BuildDrawerView.cs` plus `.Resources.cs`, `.Readiness.cs` | Copy composition conventions, not production semantics or queued building state |
| Pause | `Assets/Game/Scripts/UI/Shell/Ecs/MissionDefensePauseSystem.cs` | Current whitelist is Pause/Settings/MissionFieldGuide, not BuildDrawer. Support also adds no automatic pause. Verify normal-input parity; never set Time.timeScale in Support views |
| Runtime activity | `Assets/Game/Scripts/Components/RuntimeGameplayStateComponents.cs` | Read SimulationActive/PlayRequested; do not write them to force Support execution |
| Visibility restriction | `Assets/Game/Scripts/Systems/CombatTargetPolicyUtility.cs`; `Assets/Game/Scripts/Components/CombatTargetPolicyComponents.cs` | Existing utility allows targets without policy; Support must require positive visibility/confirmation separately |
| Non-campaign visibility reference | `Assets/Game/Scripts/Runtime/Skirmish/SkirmishFogService.cs` | IsVisible treats missing sight as unknown. Reference only; do not assume this owns campaign visibility |
| Unit ranged damage | `Assets/Game/Scripts/Systems/UnitAttackSystem.cs` | Both main attack branch and `ProcessStandardAttackPlans` aggregate per-shot damage and update `_predictedHealth`; Smoke must affect both consistently |
| Building ranged damage | `Assets/Game/Scripts/Systems/BuildingDefenseAttackSystem.cs` | Apply Smoke to eligible ground unit targets before subtraction and observation |
| Damage observation | `Assets/Game/Scripts/Components/CombatDamageObservationComponents.cs`; `Assets/Game/Scripts/Systems/Combat/Observation/CombatDamageObservationSystem.cs` | Observation queue reports damage already applied; enqueueing an observation is not damage execution |
| Fuel storage | `Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs` | BuildingResourceStorageComponent owns StoredFuelBarrels, ReservedFuelOutboundBarrels, CivilianFuelReserveBarrels, capacity and Version |
| Fuel helper | `Assets/Game/Scripts/Systems/BuildingResourceStorageTransferSystemHelper.cs`; `BuildingResourceStorageReservationSystemHelper.cs` | Public TryReserveSource / ReleaseSourceReservation / TryConsumeSourceReservation are narrow reservation primitives |
| Existing Fuel drain | `Assets/Game/Scripts/Systems/VehicleFuelConsumptionSystem.cs`, `DrainRequestedFuel` | Physical usable Fuel = stored minus outbound reservations minus civilian floor; eligible storage has Fuel capacity and no Oil/Fuel production rate |
| Materials | `Assets/Game/Scripts/Components/FactionTacticalMaterialsComponents.cs`; `Assets/Game/Scripts/Systems/FactionTacticalMaterialsUtilitySystemHelper.cs`, `TryGrant` | TryGrant rejects an over-capacity amount; compute partial accepted amount before calling it |
| Profile | `Assets/Game/Scripts/Persistence/SaveDataModel.cs` | PlayerProfileSaveData.ownedSupportAbilityUnlocks stores permanent ownership, not charges |
| Campaign settlement | `Assets/Game/Scripts/Runtime/Campaign/CampaignMissionProgressStore.cs`, `ValidateRewards`, `SettleWithRewards`; `.RadarWarning.cs` | Add Support whitelist/grant handling in both validation and application; preserve receipt idempotence |
| Attempt identity | `Assets/Game/Scripts/Components/CampaignMissionComponents.cs` | CampaignMissionRuntimeComponent SessionToken, AttemptOrdinal, SourceVersion; use this identity instead of an unrelated random session |
| Airdrop state | `Assets/Game/Scripts/Components/GridComponents.cs` | UnitTransportAirdropRequest, UnitTransportPassengerElement, UnitTransportParachuteDropComponent, UnitTransportCargoDropComponent, UnitTransportAirdropVisualPrefabs |
| Airdrop execution | `Assets/Game/Scripts/Systems/UnitTransportAirdropSystem.cs` | Existing request pass, landing search, descent, restore, settling and cleanup; currently throws on missing visuals/no landing cell, so Support needs preflight and explicit failure results |
| Population reference | `Assets/Game/Scripts/Systems/SkirmishPopulationPolicy.cs` | Skirmish-only queue policy; not proof of a campaign-wide population gate. Do not call it and assume Campaign is protected |
| Config import reference | `Assets/Game/Scripts/Editor/M03RadarWarningGuideBuilder.cs` | Reads design JSON in Editor and generates referenced config; no general live Support JSON loader was found |

## 3. File and assembly plan

Use existing asmdef boundaries. Inspect neighboring namespaces before writing each file. New names below are prescribed; only rename if a repository architecture check requires it, then update this document and task ledger.

| NEW path under `Assets/Game/Scripts/` | Responsibility |
|---|---|
| `Components/SupportAbilityComponents.cs` | Unmanaged enums and runtime request/state/receipt buffers |
| `Components/SupportEffectComponents.cs` | Smoke zones, cover multiplier, target eligibility, aircraft/payload/collection state |
| `Configs/SupportAbilityCatalogConfig.cs` | Serialized four-definition config; scalar data plus asset references; no behavior |
| `Configs/SupportMissionPolicyConfig.cs` | Mission allow-list, unlock mapping, target/exclusion facts, air route anchors, reinforcement manifest/cap |
| `Authorings/SupportAbilityCatalogAuthoring.cs` | Baker projects config to blob definitions and entity-prefab references |
| `Systems/Support/SupportAbilityStartupSystem.cs` | One attempt-scoped boundary; project approved catalog, owned unlocks and mission policy |
| `Systems/Support/SupportAbilityRequestSystem.cs` | Ordered request consumption, pure validation calls, one commit/receipt per request |
| `Systems/Support/SupportFuelTransactionSystem.cs` | Physical Fuel reserve/consume/release and request-correlated results; no account wallet |
| `Systems/Support/SupportTargetValidationUtilitySystemHelper.cs` | Stateless numeric/domain validation over explicit inputs; no World.Default injection or global lookups |
| `Systems/Support/SupportSmokeSystem.cs` | Spawn/expire smoke and publish target cover factors before direct-fire combat |
| `Systems/Support/SupportDamageUtilitySystemHelper.cs` | Stateless integer damage factor; called at each supported direct-fire hook |
| `Systems/Support/SupportStrikeSystem.cs` | Revalidation and single authoritative impact, death/observation integration |
| `Systems/Support/SupportFlightSystem.cs` | Baked visual aircraft approach/departure and correlated delivery state; not owned transport autopilot |
| `Systems/Support/SupportParatrooperSystem.cs` | Spawn reserved manifest and submit existing descent state; transfer ownership/counting at release |
| `Systems/Support/SupportSupplySystem.cs` | Drop crate, explicit collect order, partial Materials transfer and crate depletion |
| `Systems/Support/SupportAttemptCleanupSystem.cs` | Invalid attempt/results cleanup, reservations, unclaimed payload and transient effects |
| `UI/Contracts/UiSupportModels.cs` | Immutable UI snapshots, action IDs and localized reason mappings |
| `UI/Shell/Ecs/UiSupportReadModelSystem.cs` | Project ECS data to existing shell boundary; update on versions/visible second changes |
| `UI/Shell/Ecs/UiShellEcsGateway.Support.cs` | Narrow read/request methods, no gameplay mutation from UI |
| `UI/Screens/SupportPopupView.cs` and `SupportAbilityCardView.cs` | Serialized references; render snapshot and emit UI actions |
| `UI/Screens/SupportTargetingInputUiSystemHelper.cs` | Existing pointer/raycast edge → preview/confirm/cancel requests, never direct effect |
| `UI/Shell/Ecs/AssistantSupportIntentSystem.cs` | Bound proposal/approve/decline input → same Support request queue |
| `Runtime/Campaign/CampaignMissionProgressStore.Support.cs` | Idempotent permanent grants and milestone migration, matching existing store style |
| `Editor/SupportAbilityCatalogBuilder.cs` | Import approved data, assign exact prefab/sprite references, emit manifest/hash; no runtime file IO |
| `Editor/SupportPopupPrefabBuilder.cs` | Build native popup from existing UI assets, register shell reference, preserve stable .meta |

Unit tests go under `Assets/Tests/Editor/Support/`, inheriting the existing test assembly. A small optional helper type is preferable to extending a 1,000-line class with a second feature owner. Do not build generic frameworks for four abilities. Use partial files for narrow integrations into existing large owners when appropriate.

## 4. Runtime data contract (new types; not existing APIs)

All runtime structs are unmanaged. `byte` flags, fixed strings or integer indices, Entity references with generation, BlobAssetReference for immutable catalog, Native containers with explicit owner only where needed. No sprites, GameObjects, strings, managed delegates or lists in simulation state.

```csharp
enum SupportAbilityKind : byte { None=0, Smoke=1, Strike=2, Paratroopers=3, Supply=4 }
enum SupportRequestSource : byte { Player=0, Aria=1 }
enum SupportTargetKind : byte { Ground=0, Entity=1, LandingZone=2 }
enum SupportExecutionPhase : byte {
    Reserved=0, Approaching=1, Released=2, Resolved=3, Aborted=4
}
// Types shown as field specifications; add IComponentData/IBufferElementData as below.
```

| Type / storage | Required fields |
|---|---|
| SupportSessionComponent / boundary IComponentData | SessionToken FixedString64Bytes; AttemptOrdinal int; MissionSourceVersion uint (match actual upstream type); FactionId byte; CatalogRevision uint; NextRequestId uint; SimulationSeconds double; Active byte |
| SupportAbilityStateElement / boundary buffer | Kind; ChargesRemaining int; CooldownUntil double; StateVersion uint; Enabled byte |
| SupportRequestElement / boundary buffer | session/attempt; RequestId uint; Kind; Source; TargetKind; Target Entity; Position float3; Cell int2; ExpectedAbilityVersion uint; PreviewId uint; ProposalId uint; ConsentVersion uint |
| SupportPreviewComponent / separate UI-input entity | PreviewId; Kind; candidate target/position; version; Valid byte; RejectionReason ushort; cost; footprint/payload count. No resource mutation |
| SupportReceiptElement / boundary buffer | request ID; ability; phase; reason; reserved/spent Fuel; previous cooldown; accepted charges; flight/payload Entity; effectApplied byte; reservationReleased byte |
| SupportFuelReservationElement / execution buffer | SourceStorage Entity; Amount float; request ID; reserved/consumed/released status |
| SupportSmokeZoneComponent / effect entity | session/attempt; Center float3; RadiusSquared float; ExpiresAt double; DirectDamagePermille ushort=650 |
| SupportRangedCoverComponent / eligible actor | DirectDamagePermille ushort; evaluated frame/version. Default 1000. Runtime derived, not permanent unit stats |
| SupportTargetEligibilityComponent / target | session/attempt; CurrentlyVisible byte; HostileConfirmed byte; Protected byte; IsMilitaryTarget byte; SourceVersion uint |
| SupportFlightComponent / visual entity | request ID; phase; aircraft entity prefab; start/release/exit positions; StartedAt double; approach duration; cleanup deadline |
| SupportSupplyCrateComponent / payload | request/session/attempt; RemainingMaterials int; FactionId; Released byte; LandingCell; claimant Entity (optional reservation only) |
| SupportCollectRequestComponent / collector | crate Entity; request/session/attempt; approach state; player order version. Target position is crate position, not arbitrary HUD coordinates |
| SupportProposalComponent / boundary | proposal ID; request snapshot; target version; cost/definition hash; ExpiresAt; Approved/Consumed/Declined flags |

Buffers are small and bounded by per-mission allocation (maximum 5 successful uses with defaults). Keep rejected request receipts in a fixed-size 64-entry ring, plus a highest-consumed sequence per input producer so an evicted old request cannot execute on replay. Valid in-flight receipts are never evicted. Do not let repeated invalid taps grow persistent buffers.

Increment versions only for meaningful state changes. Preview rendering may refresh per pointer movement; catalog read models do not rebuild every frame. Use a Support simulation clock advanced by ECS DeltaTime only while mission Engage, outcome None and SimulationActive are true. UI remains responsive when clock stops. Reject confirms during pause/comic/result; preserve or cancel preview using lifecycle policy and revalidate afterward.

## 5. Catalog and projection

The four IDs and tunables in `support_defaults.json` are the approved initial build values; they are not balance-ready evidence. T01 merges them into existing `Design/BalanceConfigs/Combat_Balance_Config_v0_1.json` using its schema, retaining unrelated entries. Put numeric effect fields in a typed Support block; do not parse the English `effect` sentence in gameplay. Change the catalog runtimeOwner to the actual `SupportAbilityRequestSystem`; state owner is attempt ECS state plus profile unlocks.

Editor importer validates unique IDs, nonnegative costs, finite radii/durations, valid assets, unlock order, all four definitions, and the absence of new finale exposure. Generate `Assets/Game/Configs/Support/SupportAbilityCatalog.asset`, referenced through the normal scene/config composition and baking path. A player build must work without a filesystem `Design/` folder. Keep Unity `.meta` stable; do not hand-write .asset/.prefab YAML when an Editor is reachable.

Mission policy includes `AllowedAbilityMask`, milestone ownership requirement, `ProductionEnabled` per ability, authored visible ground regions or a verified reveal adapter, protected/excluded regions, fixed air entry/exit route anchors, reinforcement unit prefab and count, and optional campaign population ceiling. Missing visibility/route/manifest data disables the relevant ability with an explicit reason; never fill it with permissive defaults.

The initial squad is the existing mission-approved rifle production group, with exact prefab/count imported from its authored configuration. S0 records the concrete resolved ID/count before S6 begins. Do not hard-code a fabricated squad size. Count alive deployed infantry, queued production and reserved Support passengers once. If the campaign has no existing cap, author the ceiling explicitly in SupportMissionPolicyConfig and include queued production in its calculation; do not invent a second global population economy.

## 6. Request state machine and Fuel transaction

UI state: Closed → Catalog → Targeting → ValidPreview → ConfirmPending → Closed/Receipt. Invalid target stays Targeting with a reason. Back from Catalog closes it; back/cancel from Targeting cancels with no spend. No direct transition Catalog → Executed. Changing commands cancels preview. Only explicit Confirm or an exact ARIA approval may request commit.

Validation order, before any write:

1. Match session/attempt and supported mode; active Engage/no outcome; valid config.
2. Owned unlock AND mission allow-list AND production-ready flag.
3. Fresh request/ability version and unused request ID.
4. Remaining charge and cooldown.
5. Target entity generation/ground bounds, current visibility, confirmed hostility as applicable, target domain, protected/objective exclusions.
6. Air route and landing slots for aircraft calls; population reservation for Paratroopers. Supply placement can be valid with temporarily full Materials storage; detail warns `Storage full`, later collection waits.
7. Physical Fuel availability across eligible friendly stores, excluding outbound reservations and civilian floor.
8. If ARIA, unexpired exact proposal token matching target, cost and definition revision.

Return one stable reason enum, localized by UI: WrongAttempt, NotActive, NotUnlocked, MissionRestricted, NotReady, NoCharges, Cooldown, InvalidGround, NotVisible, NotConfirmed, ProtectedTarget, InvalidTargetType, TargetGone, NoSafeAirRoute, LandingBlocked, PopulationFull, InsufficientFuel, StalePreview, ConsentRequired, ConsentExpired, AlreadyProcessed. Unknown state rejects.

### Resolve the refund problem with physical reservations

Do not debit physical Fuel into nowhere and later mint it if a depot was destroyed. At accepted commit, reserve the quoted Fuel in source stores and consume the charge; usable Fuel drops immediately because all normal consumers already subtract ReservedFuelOutboundBarrels. Record each store's contribution. At effect/release, consume those reservations atomically. Before release abort, release reservations and restore the charge/cooldown exactly once. This is the concrete meaning of the design's immediate cost commitment/refund; no account Fuel is touched.

Fuel algorithm runs as one ordered ECS transaction, not interleaved callbacks:

```text
Validate all conditions on a current snapshot.
Build a deterministic contribution plan across eligible stores (Entity index/version order).
Require sum(available) >= cost before mutating any store.
TryReserveSource on copies for all planned contributions.
If every reservation succeeds, write all copies + decrement one charge + append receipt.
Otherwise write nothing. Do not run partial ECB reservations before knowing the result.
At release: revalidate every reservation source and target; consume all on copies first.
If valid, commit all writes and apply/queue exactly one effect with the same request ID.
If invalid, release surviving reservations, restore charge/cooldown, record Aborted.
```

If a reserved depot is destroyed before release, its lost Fuel remains lost through ordinary depot destruction; releasing surviving reservations does not recreate stock. Charge is restored and the message explains `Support cancelled: supply source lost`. This is not a Fuel purchase or compensation grant. New production/movement sees reserved availability; integrate ordering after ordinary movement Fuel drain and before subsequent consumers as appropriate, with a same-frame contention test. The copied reservation primitives must include the existing civilian-floor rule; verify it, do not duplicate a subtly different formula.

For immediate Smoke, reserve and consume in the same transaction. For aircraft, cost stays reserved during approach. A successfully released call is never refunded for ordinary subsequent losses. Charge is not incremented twice on repeated cancellation or shutdown. UI “Fuel available” must use usable stock, not raw stored barrels, so acceptance visibly changes the displayed availability.

## 7. Smoke: exact damage integration

The inspected direct-fire code subtracts integer damage without a general armor/resistance stage. Do not invent an armor system because the earlier design said “through armor.” Preserve any existing per-path modifiers; apply Support's factor to the resulting per-shot value before prediction and aggregation.

For eligible ground infantry/vehicles inside an active zone:

```text
factor = minimum active zone factor, otherwise 1000
damage = baseDamage <= 0 ? 0 : max(1, (baseDamage * factor + 500) / 1000)
```

Use long intermediate multiplication or validated safe int range. Examples: 100 → 65, 10 → 7, 1 → 1. Apply once per shot, not again to the aggregate. In UnitAttackSystem change both attackRo.Damage and plan.Damage contribution points and `_predictedHealth` calculations consistently. In BuildingDefenseAttackSystem apply to eligible target damage before health write and CombatDamageObservationUtility.Append. No smoke protection for buildings, airborne units, artillery/missile splash, scripted mission damage or Supply crates. Do not insert the factor into a global health setter.

SupportSmokeSystem updates derived cover before both direct-fire owners, after movement and zone lifecycle. New units get the derived component through spawn/baking or a narrowly batched initial add; do not add/remove it every time they enter/leave smoke. Radius comparison uses XZ metres squared and ignores camera position. Both factions receive the same cover. Expiry/exit resets factor to 1000 that frame. Two overlapping zones still yield 650, not 422.5.

VFX must be a baked ECS presentation reference or existing permitted rendering boundary, never the gameplay owner. Reuse the smoke source in the asset manifest; renderer visibility is not proof of effect. A test must observe actual changed UnitHealth plus ordinary damage observations.

## 8. Precision Strike and target knowledge

Use a Support-specific positive eligibility projection. `CombatTargetPolicy.Visible` alone is insufficient: it may be missing or mode-authored rather than true live knowledge. Campaign adapters update SupportTargetEligibilityComponent from the mission's actual reveal/confirmation state. Default missing data is NOT visible/confirmed. No all-enemies pass or render-camera/frustum test. Existing Scan/hidden-objective reveal rules remain unchanged.

Before CH04 integration, add the narrow adapter to the mission's actual target/reveal owner and test hide/reveal/expiry. A test encounter may deliberately author a visible confirmed target; that is not campaign readiness. Ground targeting likewise requires current revealed/authorized ground regions, not merely a valid raycast.

Strike: one target, 250 raw starting damage, 3s approach, no radius, no guaranteed kill. At impact check entity generation, current health, positive visibility/hostility, military domain and exclusions again. Write UnitHealth once through a narrow ECS impact owner, append a new SupportStrike observation kind (append enum), and retain existing death/result ownership. Reuse normal impact/health-bar/audio presentation where available. Never call mission completion directly. Apply any actual existing target damage policy; no invented armor calculation. Smoke does not reduce this Support strike because its mitigation scope is direct ranged fire only.

Avoid the air-defense contradiction: to strike an AA site, the aircraft follows an authored stand-off route whose **release point lies outside known hostile AA coverage**; the target need not be outside that coverage. Validate aircraft path segments against known active AA range in XZ, including clearance margin. Do not reject merely because the target is an AA launcher, and do not show the aircraft flying through the site's coverage while claiming a safe route. No valid authored stand-off route → disabled with NoSafeAirRoute. Unknown enemy AA must not be revealed through detailed rejection text; use only knowledge the player already has. This initial feature does not simulate new interception mechanics.

## 9. Flights, Paratroopers and Supply

SupportFlightSystem uses entity-prefab references baked from the mapped aircraft model and a deterministic entry → release → exit path. It owns only this bounded flight, never target acquisition. Five seconds to payload release for transport is a starting presentation value, not a teleport timer. Show approach/progress; do not spawn payload directly on Confirm. Remove flight entity after exit; correlate failure/cleanup to its receipt.

Paratrooper adapter:

1. Resolve and reserve the complete unit manifest, population slots, grounded landing cells and parachute entity prefab before commit. Missing prefab/rig returns NotReady, not an exception during play.
2. Instantiate canonical entity prefabs with ordinary faction/stats/selection/source-key/attempt ownership components. Keep unreleased passengers hidden/non-commandable and excluded from objective/kill/population double counting.
3. At release, revalidate all landing slots and Fuel reservations. Hand the existing airdrop system the prepared passengers and request or extract its reusable descent initializer into a narrow stateless boundary. Do not invoke private methods with reflection or copy the 1,000-line class.
4. Existing request fields are DropReferenceCell, NextDropAt, DropIntervalSeconds, DropCount, DroppedCount, SoldierDropCount, VehicleDropCount, DropMode, PassReady. Use SoldierOnly for this feature. Existing descent is 3.4s with at least 0.65s between soldiers; preserve it rather than inventing a second parachute animation.
5. Existing landing search is per passenger and can throw. For Support, add an optional reserved-landing buffer/claim adapter so all slots are prevalidated and used, plus explicit failure events. Keep the original path for ordinary owned transports; run its regression suite.
6. If the situation changes after some valid passengers have been released, do not abort/refund the entire call or spawn them twice. Released units remain normal units. For unreleased passengers, search only within the approved landing region; if none exists, depart with them and record partial delivery. No automatic free retry. This resolves the difference between all-slots preflight and sequential real-time release.
7. When landing/settling completes, units are selectable and take ordinary commands. Their post-release casualties follow ordinary combat rules.

Supply uses the same flight/path lifecycle, with a separate crate payload adapter. Do not pretend a crate is a military vehicle to satisfy existing CargoDropComponent health/selection assumptions. Reuse descent curve/math and baked parachute visual, but keep crate stock in SupportSupplyCrateComponent. At landing, it becomes selectable as an interactable, not an enemy or production building. Use the existing crate model selected in the asset manifest after native inspection.

Collection: select friendly ground unit → explicit collect action on crate → normal move request toward a reachable interaction position → validate distance <=2m and alive/friendly/current-order at transfer. Never move units because the camera focused the crate. If a replacement player order occurs, cancel collection. Serialize claim resolution within one ECS owner. `accepted = min(crate.RemainingMaterials, max(0, capacity-current))`; if accepted==0 keep all stock and show Storage full; otherwise call TryGrant(ref materials, accepted, Reward), write both updated resources and remaining stock together. `Reward` here is the existing tactical accounting source kind, not a campaign/account grant. No zero-amount TryGrant. Destroy depleted crate after presentation cleanup. Full/partial collectors do not trigger mission stars.

## 10. UI wiring and ARIA

Append `UiShellPopupKind.Support`. Reuse existing Support/RightSupport actions for opening; append actions for CloseSupport, SelectSupport, BeginSupportTargeting, ConfirmSupport, CancelSupport, ApproveSupport, DeclineSupport and CollectSupply. Do not overload Build or Scan payload semantics. Rich target data goes in a typed Support request buffer, not a packed magic integer PayloadId.

Full-screen popup: fixed four-card maximum, right details, resources, close/back, mission-context summary, persistent ARIA Play/Stop. Selection is UI-only. BeginTargeting closes modal before enabling world input. Consume the pointer's opening/closing event and require a fresh pointer-up/down cycle before a world candidate can be chosen. Unit move/attack and Build placement must not also consume that click. Preview carries a monotonically increasing PreviewId; Confirm uses that exact snapshot and revalidates at runtime.

Use existing localization/TMP/RTL and serialized sprite references. No Resources.FindObjectsOfTypeAll or FindAnyObjectByType in a hot path. View callbacks enqueue only; unsubscribe on disable/dispose; no duplicate listeners after replay. Retain ordinary Scan functionality and reference ARIA highlights by semantic action rather than a newly shifted ordinal.

ARIA proposal is separate from execution. Append typed Support intent values without renumbering AssistantCommandIntentKind. Proposal snapshots target identity/position, ability version, cost and catalog revision; 15 simulation seconds expiry. Approve matches all fields and produces one normal Support request with Source=Aria. Do not use global state version as consent version because unrelated timer ticks would invalidate every proposal. Revalidate gameplay facts separately. Decline/expiry/Stop consumes the proposal without spend. ShowTarget/ShowMe are camera/highlight only. Play starts guidance, not blanket approval. No implicit replacement proposal on the same declined event.

## 11. Progression, replay and persistence

Add Support grant validation and application together; the existing store rejects unknown `MissionRewardKind.None` reward IDs. IDs: `reward.ch04.m02.smoke_screen_unlock`, `reward.ch04.m03.precision_strike_unlock`, `reward.ch04.m04.paratrooper_reinforcements_unlock`, `reward.ch05.m02.supply_drop_unlock`. Amount exactly 1, correct source mission, first clear only. Owned IDs are set-like: duplicates do not convert into blueprint currency for these new abilities. Do not copy RadarWarning's duplicate compensation rule blindly.

Migration: add a profile migration version and derive only these grants from first-clear completed milestones. Preserve unrelated ownedSupportAbilityUnlocks and legacy Radar Ping. Profile ownership is not mission availability: replay CH01–CH04-M02 still hides Support even on a completed profile. Rewards and exposure are enabled only when their implementation stage passes; never claim an unavailable reward in a shipped debrief.

Each attempt resets to authored charges. Pending proposal/preview does not persist across a new attempt. Do not serialize raw Entity handles as durable identifiers. Inspect the existing resume policy in S0: if missions restart after app termination, document and use that behavior; no new mid-match resume requirement. If an existing supported mission resume path is present, add versioned Support receipts/effects/flight/stock and remap stable actor IDs before enabling Support there. Until that adapter is tested, fail the readiness gate rather than silently duplicate stock.

## 12. Critical test vectors

| Test | Arrange → act | Required assertion |
|---|---|---|
| Smoke mitigation | 100 direct damage; target inside active 650 zone | Health decreases by 65; observation records 65; both unit attack branches and building defense covered |
| No double mitigation | Two zones overlap; same 100 damage | Still 65, not 42 |
| Boundaries | Leave zone / expire / airborne / missile splash | Original damage preserved outside approved scope |
| Fuel floor | Stored 10, outbound 3, civilian 4; cost 4 | Reject; every storage/charge/cooldown unchanged |
| Fuel reservation | Same stock; cost 3 accepted then target becomes invalid | Available goes 3→0→3; stored stays 10; one restored charge; no second restoration |
| Competing requests | Two requests cost 3, only 3 available | Exactly one commits; no negative Fuel, no partial writes |
| Duplicate input | Same session/request and two Confirm callbacks | One receipt, charge and effect only |
| Old attempt | Reuse prior SessionToken/AttemptOrdinal | Reject before mutation |
| Hidden target | Entity lacks eligibility or becomes hidden before impact | Reject/abort; no damage, no hidden reveal, reservation released |
| AA stand-off | Aircraft path outside coverage, target AA inside its own circle | Valid if other target rules pass; path crossing coverage rejects |
| Consent | Play/ShowMe/ShowTarget only | No charge/resource/health/order changes |
| Stale consent | Approve after target/cost/catalog changes or 15s expiry | Reject; no auto-retarget or fresh implicit consent |
| Full population | Existing+queued+reserved equals authored limit | No charge/Fuel/spawn mutation |
| Paratrooper release | N configured units; valid slots and flight | Exactly N canonical units, ordinary faction/selection after landing; no bonus hidden duplicate |
| Landing invalid | Slot blocked before first release | All-or-none abort with no spawned playable unit; refund once |
| Partial airborne change | Block remaining slots after first release | No recall/duplicate/refund of released units; explicit partial-delivery result |
| Materials partial | Current 590/600, crate 40 | Transfer 10; stock 600; crate 30; second full-storage collection transfers 0 |
| Concurrent collect | Two collectors, crate 40, enough capacity | Total increase <=40 and crate reaches 0 once |
| Modal lifecycle | Open/close repeatedly, pause/comic/result interrupts | No click-through, stuck modal, extra event listeners or unauthorized resume |
| Progression | First clear then duplicate settlement then early replay | One unlock, no bonus currency; early mission Support hidden, Scan still usable |

Also run a full normal-input test encounter for each ability and the actual campaign path at integration. Automated assertions do not establish native readability, real-player acceptance or device readiness.


## Cross-mode roadmap extension — 2026-09-28

The owner additionally requested Support planning for future Campaign missions, all 120 Skirmish battles and all 60 Operations. Follow [Roadmap/Support/PLAN.md](../../Roadmap/Support/PLAN.md) and its complete per-entry policy CSVs. Campaign remains the first implementation slice; X01–X05 then add separately validated mode adapters and coverage. This is authorization for the roadmap scope, not runtime acceptance or permission to expose old catalog availableModes without validation.
