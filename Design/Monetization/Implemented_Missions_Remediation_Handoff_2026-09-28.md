# Implemented missions: monetization alignment handoff

Date: 2026-09-28. Status: implementation assignment prepared for another agent; no remediation implemented by this document. Source checkpoint inspected: `a299f9c5f` (Campaign Support implementation), following `f419cd462` (monetization proposal). Start from the latest committed version of this handoff and recheck the live working tree.

## Objective and scope

Bring already implemented missions into the adopted [mission product contract](Mission_Product_Contract_2026-09-28.md), preserving existing gameplay, story, earned ownership and the newly implemented Support system. Produce candidate-specific evidence for every affected behavior. The [readiness audit](../AgentReports/2026-09-28_monetization-mission-change-readiness.md) distinguishes complete recorded Editor journeys from incomplete coded candidates; the [205-entry policy register](Mission_Product_Policies_2026-09-28.csv) owns product membership and per-entry changes.

Companion assignment: [menu and header remediation](Menu_Header_Remediation_Handoff_2026-09-28.md). This mission agent retains ownership of tactical costs/resources, reward settlement, content-access evaluation and Operations scope. The menu agent consumes those contracts and owns presentation; coordinate any shared match-header edits rather than creating duplicate logic or overwriting active changes.

Included work:

- Primary Campaign regression/remediation set: CH01-M01–M05, CH02-M02 Supply Line, CH04-M01 Air Corridor and CH04-M02 Steel Push.
- Existing but incompletely accepted Campaign candidates: Gridlock, CH02-M03–M05 and CH03-M01–M05. Apply shared access/resource/reward compatibility to their existing implementations; do not call asset registration a successful playthrough.
- Existing Skirmish prototype mappings S001/S025/S073 and expanded S002/S003/S004. Preserve their identity, setup and publication records; expanded revisions require their own evidence.
- Existing Operations O001–O003: add intro/full scope integration to their existing graphs, with shipping-flow integration where prototype-only behavior remains. Full human-controlled O001 Victory is still pending.

The remaining eight Campaign missions, the rest of the 120 Skirmish catalog and O004–O060 stay in their updated production plans. This assignment does not build that entire future catalog, publish storefront products, charge users, select a final price, buy assets/voices or deploy a release. Implement and test the access boundary without activating a live paywall around unfinished/unpurchasable content. Preserve development access through explicit non-shipping fixtures/configuration, not a hidden production ownership grant.

## Read first and recheck

1. Root `AGENTS.md`, current [monetization plan](Monetization_Plan_2026-09-28.md), product contract and policy register.
2. [Campaign mission catalog](../Campaign_Mission_High_Level_Design_Catalog.md), affected chapter plans, [Operations strategic rules](../Roadmap/Operations/STRATEGIC_RULES.md), [Skirmish setup](../Roadmap/Skirmish_Expansion/MATCH_SETUP.md) and their acceptance documents.
3. [Implemented Support handoff](../AgentReports/SupportSystem/IMPLEMENTED_FEATURE_HANDOFF.md) and its evidence index. Smoke's real Steel Push first-clear grant and profile migration are already implemented. Verify them; do not duplicate the reward or re-enable production flags merely because test encounters passed.
4. Mission-specific evidence linked in the readiness audit. Record current source/config hashes and list any later changes that invalidate historical evidence before relying on it.

Use `rtk git status`, current branch/HEAD and current processes to identify shared work. Preserve unrelated changes. Prefer the existing checkout when safe; use a managed worktree only when isolation is needed and follow the app's worktree rules. Never terminate an Editor or recover licensing without the permissions required by the current root instructions.

## Work packages, in order

| ID | Deliverable | Completion evidence |
|---|---|---|
| IM-00 | Re-audit current implementation, owners, existing validation entry points and dirty files; create an exact change/evidence ledger. | Baseline hashes; classified mission statuses; dependency/failure list; no assumptions that historical wins cover current source. |
| IM-01 | Trace account Credits versus tactical Money/Materials and all real affordability/spend/refund paths. Author per-mission migration budgets. | Cost ledger for construction, recruitment, cancellation and HUD; proof of actual consumers rather than serialized-value inference. |
| IM-02 | Migrate Radar Warning, Air Corridor and Steel Push to local Materials costs/presentation; retain M02's existing local economy. | Focused transactional checks plus real manual-control/ARIA journeys, shortages and retry on the changed candidates. |
| IM-03 | Reconcile mission reward presentation/ownership, particularly M05 APC parts; preserve canonical story and Support milestone settlement. | Versioned, idempotent migration and first-clear/replay/duplicate-callback tests; actual M05 result/return and Steel Push Smoke reward. |
| IM-04 | Implement one content-access authority outside simulation and wire Campaign/Skirmish/Operations launch, replay and resume paths. | Free/owned/mixed-owner/restore/offline fixtures; readiness and progression kept separate; no live commerce activation. |
| IM-05 | Apply policy to existing Skirmish candidates with unchanged tactical research, starting grants, Fuel and capacity. | Canonical S-ID mapping; S001 free access; core ownership on other candidates; account-isolation and affected normal-input regression. |
| IM-06 | Implement scoped Operations introduction for O001–O003, persistence and completion; preserve full-theater semantics. | Normal deploy → mission → result → dashboard for each, free intro completion/replay/recovery, scope-safe checkpoint and first-clear deduplication. |
| IM-07 | Complete affected regression, native review and evidence handoff; state remaining human/device/product-release gates individually. | No unsubstantiated readiness promotions; retained failures; source/build identity and exact remaining work. |

IM-03/04 must not activate offers before the complete advertised product and actual commerce layer are ready. No additional ownership prompt is necessary for routine implementation choices already specified here. New substantial UI direction follows the explicit visual-review requirement below.

## IM-01/02: concrete economy repair

Start at these existing source boundaries (inspect their current callers and partial classes):

- [FactionConstructionResourceUtilitySystemHelper](../../Assets/Game/Scripts/Systems/FactionConstructionResourceUtilitySystemHelper.cs): `Evaluate`, `TrySpend`, `TryRollback`, Money and `MaterialsOnlyConstruction` behavior.
- [CampaignMissionSpawnSystem](../../Assets/Game/Scripts/Runtime/Missions/CampaignMissionSpawnSystem.cs): starting-economy validation and scenario initialization. Some validation currently requires positive `StartingCredits`; change the affected contract deliberately rather than retaining fake money solely to satisfy an obsolete assertion.
- [MatchHudResourceHeaderPresentation](../../Assets/Game/Scripts/UI/Screens/MatchHudResourceHeaderPresentation.cs): mission Credits substitutions and actual Materials/Oil/Fuel display. Fuel must remain visible in Steel Push.
- [CampaignMissionProgressStore](../../Assets/Game/Scripts/Runtime/Campaign/CampaignMissionProgressStore.cs), [SaveDataModel](../../Assets/Game/Scripts/Persistence/SaveDataModel.cs) and [default menu read models](../../Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.DefaultState.cs): persistent grants, legacy balances and placeholder presentation. Do not connect the account wallet to tactical affordability.

For each affected mission, record the old cost, actual resource owner, used action, proposed Materials cost, starting/earned local budget and validation result. A serialized 50,000 Credits balance does not prove 50,000 Credits are spent. Do not mechanically convert Money 1:1 or globally zero every cost. Existing Materials-only logic can suppress a nonzero Credit component when a Materials component exists; simply turning on that flag may silently make an action cheaper. Author the intended complete cost first.

| Mission | Required change | Non-negotiable regression |
|---|---|---|
| CH01-M03 Radar Warning | Trace/migrate real preparation construction/recruitment costs, menu affordability and resource feedback. | Warning/preparation/defense rhythm, scan allowance, revised wait pacing, successful normal defense and recovery without account spending. |
| CH04-M01 Air Corridor | Remove unused tactical Money or replace used costs with a tested local Materials budget; update initializer validation/HUD consistently. | Supplied G2A/radar remain sufficient; actual missiles/waves, bilingual story, ARIA, result and return. |
| CH04-M02 Steel Push | Same migration; preserve the reserve-scoped resource model. | Three-tank counterforce, 120 usable military Fuel, 40 protected civilian barrels, real movement/combat and earned Smoke settlement. No ambient storage bypass. |
| CH01-M02 Establish The Base | Regression baseline, not a rebalance. | 120 Materials → Barracks 90 → squad 20 → 10 remaining; cancel/refund exactly once; no account Money requirement. |

Test zero/large account balances, insufficient local Materials, repeated confirmation, cancellation before/after commitment, blocked/failed creation and clean retry. Keep the existing rules for production/research time, Fuel and support reservations. Modify Unity assets through the repository's checked Editor builders, not hand-edited YAML.

## IM-03/04: rewards and access

- M01/M02/M04 and Supply Line need shared integration checks, not new objectives. Preserve their current supplied-force and tactical-resource baselines.
- M05 currently grants 35 parts targeting `Unit_Veh_APC_Heavy`; older design names `upgrade.vehicle.apc_armor`. Trace the actual consumer and UI before changing either. Preserve earned records/ownership. If no usable consumer exists, remove the misleading new reward teaser or replace future grants with an explicitly documented fixed earned reward; do not invent a paid completion path or silently erase old parts.
- Preserve XP/first-clear/replay amounts during the resource-ownership migration. Do not rebalance the entire account economy in the same change. Cosmetic sink tuning remains a separate earn/spend exercise.
- M05's complete debrief, fragment and rewards settle before any future package invitation. CH02 eligibility requires ordinary M05 completion plus the content entitlement; buying must not mark missions complete or supply battle power.
- Resolve product membership through stable mission/scenario IDs. Core membership is all 25 Campaign entries plus all S001–S120; Operations is an independent full-theater entitlement. Distinguish not owned, progression locked, uncertified/unavailable and installed-content missing states.
- Do not invent a functioning storefront purchase from a local boolean. Use an injectable verified-entitlement boundary, with isolated test fixtures. Keep the release commerce switch off until actual billing/restore and the complete advertised content pass their own gates.
- Preserve existing verified customer entitlements if any are found. Account reset and temporary offline state must not erase ownership. Never interrupt an active mission because ownership reconciliation finishes mid-battle.
- Validate manual and ARIA outcomes against identical reward rules. No paid ARIA tier, reward haircut, ad multipliers, paid retries or premium hints.

## IM-05: existing Skirmish content

Inspect the [runtime publication manifest](../../Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishPublicationManifest.asset) and [expanded launch resolver](../../Assets/Game/Scripts/Composition/Skirmish/SkirmishExpandedLaunchResolver.cs). Runtime indices and handoff work ordinals are not S-IDs. In particular S001/S025/S073 are prototype compatibility mappings, while S002/S003/S004 are expanded candidates.

S001 is the unlimited free sample when its exact released definition is certified. Other included entries resolve to the same core entitlement without Campaign grinding. Preserve Field/Established starts, local research, normal counter availability, player/AI parity and match reset/checkpoint semantics. No product supplies Fuel, improves stats, expands capacity or removes a queue timer.

S002–S004 `Playable` publication is not a complete manual/ARIA/device matrix. Preserve the flags/evidence rather than automatically promoting or demoting them. Address defects introduced or exposed by this remediation; existing air-cycle and full-catalog gaps remain explicitly tracked under their owning implementation plans. Do not build the other 114 expanded candidates to claim this assignment complete.

## IM-06: Operations O001–O003

Implement the contract's proposed `operations.sahrin.intro.v1` and `operations.sahrin.full.v1` scope identities as versioned mode data. Use the existing mission IDs and graphs, with scope in run/launch/checkpoint/result/commit validation. Scope filtering belongs in the shared offer, incident, adjacency, day and completion policies; do not create duplicate tactical controllers for the free versions.

- O001 retains scans, evidence recovery and extraction. O002 retains truck identities, both routes, delivery minimum and clinic protection. O003 retains two pumps, clinic protection, hold and the authored 80-Materials repair requirement funded by the scenario.
- Intro completion requires all three persisted Victories. Partial remains Partial; no O004/O010/finale purchase is needed. The recap describes local services, not victory over the six-district city.
- Keep explained in-game AP/End Day and free recovery/reoffers. Inaccessible districts cannot cause incidents, decay or victory requirements in the intro. After completion preserve recap and unlimited practice/replay under the existing reward rules.
- On purchase transition, retain account records, archive the intro and start a normal fresh full-city run. Do not import intro district state as a full-run checkpoint, silently overwrite a full run or re-grant O001–O003 first clears.
- Full scope retains all six finales, existing metric thresholds and two qualifying End Days; no Campaign purchase prerequisite or paid district repair.
- Tactical ARIA Watch and separately consented Operations Run Watch keep their approved scopes. Both are included in accessible content; neither may purchase anything. Preserve visible Play/Stop and camera-only guidance.

O001's latest ARIA wins are useful regression evidence, but the recorded mouse journey ended in Partial Success. Obtain a real complete normal-input manual-control Victory on the changed candidate and still distinguish agent-operated input from real human acceptance. O002/O003 model captures cannot substitute for shipping input, persistence and ARIA journeys; complete that integration using their existing authored contracts.

## Validation and visual review

Follow root `AGENTS.md` and read the Unity CLI skill before controlling a connected Editor. Keep Hub open/signed in. On macOS all validation/build/capture execution uses `rtk proxy Tools/CI/invoke_unity_macos.sh`; never invoke Unity directly or add batchmode. Inspect current `--reuse` support and prefer the verified existing Editor when appropriate. The wrapper's reuse path must retain full logs, explicit timeout, exit receipts and required markers. Do not stop/kill unrelated processes or reset IPC.

Existing entry points to inspect and reuse include:

| Entry point | Purpose |
|---|---|
| `Game.Editor.CH04M01AirCorridorInputProbe.RunEnglish` / `RunPersian` | Existing Air Corridor normal-input journeys. |
| `Game.Editor.CH04M02SteelPushInputProbe.RunEnglish` / `RunPersian` | Existing Steel Push normal-input journeys. |
| `Game.Editor.CH04M02SteelPushRulesValidation.Run` | Fuel binding/spending/civilian-floor and mission rules; marker `[SteelPushRules] result=Passed`. |
| `Game.Editor.CH02M02SupplyLineInputProbe.RunManual` / `RunWatch` | Existing Supply Line normal-input journeys. |

Re-read implementations and current probe setup before running; test-only pre-mission setup must be disclosed and cannot bypass the behavior under test. Use root Windows wrappers if assigned to Windows. A missing marker, timeout, compile error or project lock is a failed run. Existing repository architecture failures are documented in the Support handoff and must not be hidden or weakened.

Required evidence:

1. Focused transaction, profile migration, reward deduplication, scope/access and changed resource-owner checks with actual pass markers.
2. Complete normal-input manual-control and ARIA journeys for each changed tactical mission, with real combat/outcomes, story, settlement and return. Exercise retry/failure/checkpoint semantics actually supported by that mode. Campaign pending resume currently starts a fresh attempt; do not claim mid-match snapshot recovery without implementing it.
3. A fresh Chapter 1 chain and affected Supply Line/Skirmish/Operations regressions for shared changes. Separate fixture coverage of 205 membership rows from playable-content claims.
4. EN/FA native menu/HUD/result review at supported aspect ratios; never let removing Credits hide Fuel or obscure ARIA. New mission screens or substantial redesigns require ImageGen mockups using actual Campaign references and the owner's visual-direction review before implementation. Existing Support approval remains valid for routine reuse.
5. Real-player, packaged-player and physical-device acceptance recorded separately. Missing access to a player/device leaves that named gate pending; it does not erase code progress or justify a false readiness claim.

## Deliverables and completion

Create `Design/AgentReports/ImplementedMissionMonetization/` with a change ledger, before/after cost table, exact candidate hashes, per-mission evidence/status, all failed runs and a final handoff. Update only acceptance fields backed by new evidence. Keep runtime changes, validation fixes and documentation reviewable in focused commits; do not sweep unrelated work into this agent's commits without explicit authorization.

Completion means the implemented mission paths satisfy their scoped product/resource/reward rules, affected regressions pass, and every remaining acceptance or future-content dependency is named with evidence. Shipping monetization remains gated on the complete purchased collections and verified commerce; eight old Campaign Editor reports cannot certify a 25 + 120 + 60 release.

## Copy-ready instruction for the implementing agent

> Implement `Design/Monetization/Implemented_Missions_Remediation_Handoff_2026-09-28.md`, starting with IM-00 and continuing through the applicable existing-content packages. Preserve current Support implementation and unrelated work. Fix actual tactical resource ownership, rewards and shared content access; integrate the standalone O001–O003 introduction through existing mission graphs. Keep all 120 core Skirmish IDs included and do not expand the assignment into building the whole future catalog. Do not activate live purchases or lock unfinished content behind an unpurchasable offer. Validate through the repository Unity wrappers and real normal-input manual/ARIA journeys, preserve failed evidence, and report Editor, human and device acceptance separately. Produce the specified change/evidence ledger and final handoff.
