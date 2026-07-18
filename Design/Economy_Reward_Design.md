# WarlineCapture Economy And Reward Design

Date: 2026-05-05

## Purpose

This document locks the player-facing resources and reward types used by UI screens, popups, panels, store designs, mission results, Operation reports, and profile progression. It removes generic resource names and generic reward language from the design layer.

It is also the economy-balancing source of truth. A balancer should be able to read this document and understand where each resource or reward comes from, how it leaves the economy, how it converts into other gameplay value, and which tuning knobs control the rate.

Mission and Operation reward pacing should follow `Gameplay_North_Star_And_Content_Grammar.md` and `Level_And_Mission_Content_Plan.md`: Chapter 1 exposes only the resources needed for teaching, Operation introduces district metrics through authored actions and consequences, and store/profile resources remain separate from objective and star completion.

Unit, building, support ability, gear, and upgrade-part reward targets must resolve to ids in `Combat_Catalog_And_Upgrade_Design.md` and `BalanceConfigs/Combat_Balance_Config_v0_1.json`. Visual reward presentation for those targets must use `VisualConfigs/Combat_Visual_Config_v0_1.json`.

## Canonical Persistent Resources

The persistent account economy has exactly two currencies. Neither currency is produced by battlefield Oil infrastructure, and neither has a second match-scoped balance with the same player-facing name.

| Resource | Gameplay Role | Primary Sources | Primary Sinks | UI Surfaces |
|---|---|---|---|---|
| `Credits` | Regular account progression currency earned primarily by playing. | Mission rewards, Operation end-of-day income, profile milestones, events, and capped store bundles. | Permanent upgrades, unlocks, normal account catalog items, and authored Operation actions. | Main Menu resource strip, Mission Result, Commander Profile, Operation Dashboard, Store. |
| `Command` | Rare account currency for optional premium and convenience content. `CommandAuthority` is the legacy serialization/config identifier until migration is complete. | Major profile milestones, limited events, season rewards, and optional purchases. | Cosmetics, fixed-content bundles, Rush Tickets, and other non-victory convenience items. | Main Menu resource strip, Commander Profile, Store. |

`CommanderXP`, Campaign stars, Rush Tickets, unlocks, inventory items, and Operation metrics are progression or inventory values, not additional persistent currencies and not permanent main-menu header counters.

## Match Resources

The active match has exactly three player-facing resources. They are initialized by scenario or Skirmish setup, exist only for that match, and are discarded when the match ends.

| Resource | Produced From | Used For | Required UI Surfaces |
|---|---|---|---|
| `Materials` | Starting scenario grant or Oil converted by a Field Fabrication Depot. | Construction, unit production where authored, and battlefield repair. | Match HUD, Build Drawer, placement confirmation, selected fabrication building. |
| `Fuel` | Starting scenario grant or Oil refined and delivered into usable faction storage. | Vehicle and aircraft operation, tactical support, and extraction actions inside the match. | Match HUD, relevant unit/building panels, logistics feedback. |
| `Oil` | Starting scenario grant or Oil Pump extraction delivered through physical logistics. | Input for Materials fabrication or Fuel refining. | Match HUD and contextual logistics/building panels. |

There is no player-facing tactical `Credits`, `Money`, `Supply`, or `Requisition` resource in the target design. Existing tactical Money/Credits fields and costs are migration debt: authored tactical costs must move to Materials and, where mobility is involved, Fuel. Persistent Credits must never be read or mutated directly by active-match simulation.

Field logistics sources:

- `Field_Logistics_Oil_Fuel_Design.md` owns the tactical Oil Pump -> Oil Truck -> Refinery -> Fuel -> Fuel Bladder/Tanker baseline, including build menu usage, match HUD rules, and config-aligned roles.
- `Automated_Fuel_Logistics_Design.md` owns the automated logistics model. Header Fuel is usable faction Fuel delivered into Fuel Bladder/base storage; raw Oil, Oil in transit, refinery input, refinery output Fuel, and tanker cargo are not header Fuel.
- `Field_Fabrication_Materials_Design.md` owns the tactical Oil -> Materials branch, the Field Fabrication Depot role, one canonical faction Materials value, and Materials-based battlefield construction costs.
- `Resource_Logistics_Exchange_Design.md` owns optional timed redistribution of match Oil, Materials, and Fuel. It must not read, grant, or spend persistent Credits or Command during a match.

## Canonical Reward Types

| Reward Type | Meaning | UI Presentation |
|---|---|---|
| `CommanderXP` | Profile progression. | XP chevron/badge icon, numeric delta, level progress bar. |
| `Credits` | Main spendable resource grant. | Credit stack icon, amount. |
| `Command` / legacy `CommandAuthority` | Rare account command resource. | Command insignia icon, amount. |
| `RushTicket` | Queue/action accelerator. | Stopwatch/order ticket icon, count. |
| `UnitUnlock` | New unit becomes available. | Unit portrait/card, unlock title, role subtitle. |
| `BuildingUnlock` | New structure becomes available. | Building icon/card, unlock title. |
| `SupportAbilityUnlock` | New tactical/Operation support ability. | Support icon/card, ability role. |
| `BlueprintParts` | Deterministic parts for a unit, building, support ability, or gear module. | Parts stack, target item icon, count, threshold progress. |
| `GearModule` | Deterministic gear/module item or parts. | Gear card, rarity border, count. |
| `Cosmetic` | Non-power identity item. | Frame/banner/HUD skin preview, ownership badge. |
| `OperationSupply` | Consumable Operation item such as Aid Convoy, Repair Convoy, or Readiness Boost. | Supply card icon, count, valid Operation action. |
| `CampaignStars` / legacy `SagaStars` storage | Campaign star progress. | Gold star row and chapter reward counter. |
| `OperationTrust` | District/city trust delta. | Trust meter delta. |
| `OperationSecurity` | District/city security delta. | Shield/security meter delta. |
| `OperationIntel` | District intel confidence delta. | Intel confidence meter delta. |
| `OperationInfrastructure` | District infrastructure delta. | Infrastructure meter delta. |

## Economy Event Model

Every source, sink, loss, conversion, store purchase, and reward grant should become a logged economy event.

| Field | Purpose |
|---|---|
| `EconomyEventId` | Stable event id for telemetry and balancing reports. |
| `SourceSystem` | Mission, Saga, Operation, Profile, Store, Season, Event, or TacticalMatch. |
| `SourceSurface` | UI surface or gameplay flow that triggered the event. |
| `ResourceOrRewardType` | One canonical resource or reward type from this document. |
| `ItemId` | Concrete item id for unlocks, parts, gear, cosmetics, and Operation supplies. |
| `Amount` | Signed amount. Positive grants value; negative spends or losses value. |
| `BalanceTag` | Tuning bucket such as `MissionClear`, `FirstClear`, `StoreBundle`, `OperationAction`, `RushTimer`, or `DuplicateFallback`. |
| `ConfigId` | `RewardConfig`, `StoreCatalogItem`, `OperationActionConfig`, `MissionConfig`, or `SeasonTrackNode` that authored the event. |
| `Before` / `After` | Wallet, inventory, progression, or Operation metric value before and after the event. |

Balancer reports should group by `ResourceOrRewardType`, `SourceSystem`, `BalanceTag`, and day/account-level band.

## Persistent Resource Lifecycle Specs

| Resource | Acquired From | Lost Or Reduced By | Used For | Conversion Rules | Store And Reward Rules | Balance Knobs |
|---|---|---|---|---|---|---|
| `Credits` | Mission clear rewards, first-clear bonuses, objective rewards, Operation end-of-day income, profile milestones, season nodes, events, and capped store bundles. | Permanent upgrades, unlocks, normal account catalog purchases, and authored Operation actions. No passive account decay. | Account progression and standard earned-currency purchases. | Credits never convert directly into match Materials, Fuel, or Oil. Match startup resources come only from authored scenario or Skirmish configuration. | Store may sell capped Credit bundles. Paid Credits cannot be injected into an active match. | `BaseMissionCreditGrant`, `FirstClearCreditMultiplier`, `DifficultyCreditMultiplier`, `OperationDailyCreditIncome`, `CreditBundleCap`, `CreditSpendCurve`, `Chapter1CreditCeiling`. |
| `Command` | Major profile milestones, limited events, free season nodes, paid season products, direct purchases, and fixed store bundles. | Cosmetics, fixed-content bundles, Rush Tickets, and explicitly non-victory convenience items. No gameplay failure, timer, or Operation event removes Command. | Optional premium and convenience content. | Command converts only to specific catalog items through Store purchase definitions. It never converts directly into match resources, Campaign stars, Operation metrics, active-match combat power, or victory. | Every spend must create a receipt/economy event and grant through `RewardService`. | `CommandEarnFreeTrack`, `CommandPriceAnchors`, `CommandBundleGrant`, `DailyCommandSpendCap`, `RushTicketCommandCost`, `CosmeticPriceBands`. |

Rush Tickets remain persistent inventory items governed by the reward lifecycle below; they are not currency and do not appear in the permanent header.

## Reward Lifecycle Specs

| Reward Type | Acquired From | Lost Or Reduced By | Used For | Conversion Rules | Balance Knobs |
|---|---|---|---|---|---|
| `CommanderXP` | Mission results, first clears, profile milestones, season/event nodes, Operation end-of-day reports. | Never spent and never reduced in normal play. Profile rollback can only come from save recovery tooling. | Commander level, profile rewards, feature unlock gates. | XP converts to Commander levels through the XP curve. Level-up nodes grant authored `RewardConfig` items. | `XpPerMission`, `FirstClearXpBonus`, `DifficultyXpMultiplier`, `XpCurve`, `LevelRewardCadence`. |
| `Credits` | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. |
| `Command` / legacy `CommandAuthority` | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. |
| `RushTicket` | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. |
| `UnitUnlock` | Saga first clears, chapter rewards, profile milestones, store starter packs, season/event nodes. | Never lost in normal play. Owned unlocks persist account-wide. | Roster availability, loadout choices, tutorial/progression gates. | Duplicate unit unlock grants convert to `BlueprintParts` using `DuplicateUnitUnlockParts`. | `UnitUnlockChapter`, `DuplicateUnitUnlockParts`, `UnitPartsThreshold`, `StarterUnlockEligibility`. |
| `BuildingUnlock` | Saga chapters, build tutorials, Operation progression, profile milestones, store packs that include blueprint parts. | Never lost in normal play. Owned unlocks persist account-wide. | Build catalog availability and Operation action availability. | Duplicate building unlock grants convert to `BlueprintParts` using `DuplicateBuildingUnlockParts`. | `BuildingUnlockChapter`, `BuildingPartsThreshold`, `DuplicateBuildingUnlockParts`. |
| `SupportAbilityUnlock` | Mission rewards, Operation milestones, profile milestones, armory bundles, season/event nodes. | Never lost in normal play. Owned unlocks persist account-wide. | Loadout support slots, Operation support actions, tactical command options. | Duplicate support unlock grants convert to `BlueprintParts` using `DuplicateSupportUnlockParts`. | `SupportUnlockChapter`, `SupportPartsThreshold`, `SupportChargeEconomy`. |
| `BlueprintParts` | Duplicate unlock conversion, armory bundles, mission rewards, profile rewards, season/event nodes. | Spent when a part threshold unlocks or upgrades the target item. | Unlocking or upgrading a specific unit, building, support ability, or gear module. | Parts are item-specific by `TargetItemId`. Cross-item conversion is not allowed. Overflow after threshold remains on the same `TargetItemId`. | `PartsPerMission`, `PartsPerBundle`, `PartsThresholdByRarity`, `DuplicatePartsFallback`. |
| `GearModule` | Mission rewards, armory bundles, support/drone kits, Operation rewards, season/event nodes. | Consumed by upgrade/fusion rules or converted to parts by duplicate logic. Equipped modules are not destroyed by mission failure. | Loadout power expression, unit role specialization, support ability tuning. | Duplicate module grants convert to `BlueprintParts` for that module using `DuplicateGearModuleParts`. | `GearDropCadence`, `GearUpgradeMaterialCost`, `DuplicateGearModuleParts`, `GearPowerBudget`. |
| `Cosmetic` | Store purchases, season pass, events, profile rewards, starter packs. | Never lost. Paid duplicate purchases are blocked by ownership checks. Free duplicate cosmetic rewards convert to Command through an explicit fallback grant. | Commander frames, unit card skins, banners, HUD accent themes, profile badges. | Cosmetics never convert into combat stats. Duplicate fallback grants Command only when the reward config includes that fallback. | `CosmeticPriceBand`, `DuplicateCosmeticCommandGrant`, `EventCosmeticCadence`. |
| `OperationSupply` | Operation rewards, store Operation tab, starter packs, event nodes. | Consumed by matching Operation actions. Event-tagged supplies use explicit `ExpiresAt`; permanent supplies use `ExpiresAt=null`. | Aid Convoy, Repair Convoy, Readiness Boost, Armory Stock Refresh, and other named Operation actions. | Supplies convert to Operation metric deltas only through the action that consumes them. | `AidConvoyTrustDelta`, `RepairConvoyInfrastructureDelta`, `ReadinessBoostDuration`, `SupplyStorePrice`, `SupplyDailyUseCap`. |
| `CampaignStars` / legacy `SagaStars` storage | Best mission star result from Campaign mission completion. | Never spent or reduced by normal play. Replays update the saved best star count for that mission. | Chapter reward thresholds, progression gates, achievement pacing. | Stars unlock reward thresholds; claiming a threshold does not consume stars. | `StarGoalDifficulty`, `ChapterStarThresholds`, `StarRewardCadence`. |
| `OperationTrust` | Aid actions, civilian-save outcomes, positive daily events, OperationSupply actions. | Civilian casualties, collateral damage, failed raids, negative daily events. | District stability, public support, action risk modifiers, narrative reports. | Trust is an Operation metric delta, not a wallet resource. Store items grant supplies/resources; they do not grant Trust directly. | `TrustGainAid`, `TrustLossCollateral`, `TrustDailyDecay`, `TrustRiskThresholds`. |
| `OperationSecurity` | Patrols, successful raids, defensive builds, threat clear missions, daily security actions. | Enemy influence, unresolved threats, failed defense, negative events. | Threat spawn rate, district lock state, Black Market risk, mission availability. | Security is an Operation metric delta, not a wallet resource. Store items grant supplies/resources; they do not grant Security directly. | `SecurityGainPatrol`, `SecurityGainRaid`, `SecurityLossThreat`, `ThreatSpawnCurve`. |
| `OperationIntel` | Drone Scan, Patrol evidence, Raid success, evidence archive progress, and authored Operation actions. | Enemy counter-intel events, stale intel decay, failed investigations. | Raid confidence, hidden-cell discovery, mission briefing accuracy, threat detail visibility. | OperationIntel is a mode metric, not a wallet currency, and cannot be purchased or converted back into account value. | `IntelConfidencePerScan`, `IntelDecayPerDay`, `RaidConfidenceThresholds`. |
| `OperationInfrastructure` | Repair actions, construction objectives, Repair Convoys, successful defense, daily recovery. | Collateral damage, attacks, sabotage, unresolved fires/damage events. | District income, build availability, civilian stability, Operation recovery pacing. | Repair Convoys and authored actions change Infrastructure. Match Materials never transfer into Operation state. | `InfrastructureRepairCost`, `InfrastructureDamageEvent`, `IncomePerInfrastructureBand`, `RepairConvoyDelta`. |

## Conversion Matrix

| From | To | Trigger | Rule |
|---|---|---|---|
| Match `Oil` | Match `Fuel` | Refinery/production gameplay plus tanker delivery into storage. | Production converts Oil into refinery output Fuel; it becomes usable faction Fuel only after delivery into Fuel Bladder/base storage. |
| Tactical `Oil` | Tactical `Materials` | Field Fabrication Depot conversion. | Existing tray logistics delivers physical Oil to the depot; an authored conversion consumes Oil and grants Materials into the one faction tactical Materials inventory, respecting capacity. |
| Match `Oil`, `Materials`, or `Fuel` | Match `Oil`, `Materials`, or `Fuel` | Authored Resource Logistics Exchange job. | Optional match recipes may redistribute tactical resources with an authored loss, queue time, and capacity checks. They never touch the account wallet. |
| `Credits` | Operation metric delta or account item | Authored account/Operation action. | Spending persistent Credits may apply an authored Operation action or grant an account item; it cannot grant active-match resources. |
| `Command` | Store item or `RushTicket` grant | Store purchase. | Purchase spends Command and grants the exact catalog `RewardConfig`. |
| `RushTicket` | Time reduction | Queue or Operation timer rush. | Each ticket reduces time by `RushTicketSecondsPerTicket`, capped per queue/day. |
| Duplicate unlock | `BlueprintParts` | Reward grant for already owned unit/building/support. | `RewardService` grants item-specific parts using the duplicate fallback in the reward config. |
| Duplicate gear module | `BlueprintParts` | Reward grant for already owned module. | Grants item-specific module parts using the duplicate fallback in the reward config. |
| Free duplicate cosmetic | `Command` | Reward grant for already owned cosmetic. | Grants the configured duplicate fallback; paid duplicate purchase is blocked before purchase. |
| `BlueprintParts` | Unlock or upgrade | Part threshold reached. | Item-specific threshold consumes required parts and leaves overflow on the same item id. |
| `OperationSupply` | Operation metric delta | Matching Operation action consumes supply. | Supply action applies configured metric deltas and consumes the item. |

## Store Grant Rules

- Store products are catalog entries that map to `RewardConfig` items. The store never writes wallet, inventory, progression, or Operation state directly.
- Store purchases cannot grant CampaignStars / legacy `SagaStars`, `OperationTrust`, `OperationSecurity`, `OperationIntel`, or `OperationInfrastructure` directly.
- Store purchases can grant Credits, Command, Rush Tickets, BlueprintParts, GearModule, Cosmetic, UnitUnlock, BuildingUnlock, SupportAbilityUnlock, and OperationSupply.
- Store purchases cannot grant match Materials, Fuel, or Oil, and cannot inject resources into an active match.
- Operation supplies bought in the Store create value only after the player spends them through Operation actions.
- Every gameplay-affecting store item must also have a non-paid earn path through Saga, Operation, profile levels, events, or season free-track rewards.
- Every purchasable bundle has `PurchaseLimit`, `DailyLimit`, `EligibilityRule`, and `AccountLevelBand` fields for balancing.

## RewardConfig Balancing Fields

Every `RewardConfig` and store catalog reward item should expose these fields:

| Field | Balancer Use |
|---|---|
| `RewardId` | Stable tuning and telemetry id. |
| `RewardType` | Canonical reward type from this document. |
| `TargetItemId` | Unit/building/support/gear/cosmetic/supply id for item rewards. |
| `Amount` | Resource amount, parts count, metric delta, XP, or supply count. |
| `SourceSystem` | Mission, Saga, Operation, Store, Profile, Season, Event. |
| `BalanceTag` | Report bucket and multiplier target. |
| `FirstClearOnly` | Prevents repeat farming for authored first-clear rewards. |
| `RepeatableGrantRule` | Defines repeat rewards for replayable missions and Skirmish. |
| `StarThreshold` | Campaign threshold for star-gated reward items. |
| `DifficultyMultiplierId` | Difficulty scaling curve for XP/resources. |
| `AccountLevelBand` | Controls offer/reward visibility and economy pacing. |
| `WalletCapRule` | Soft/hard cap behavior for resources. |
| `DuplicateFallbackRewardId` | Explicit fallback for duplicate unlocks, gear, and cosmetics. |
| `TelemetryBucket` | Aggregation key for balance reports. |

## Economy Balancer Reading Checklist

1. Start with `Resource Lifecycle Specs` to see all sources, sinks, conversions, and caps.
2. Read `Reward Lifecycle Specs` to understand progression items, duplicate rules, and Operation metric deltas.
3. Use `Conversion Matrix` to confirm that no reward or store item bypasses a gameplay action.
4. Resolve every unit, building, support ability, gear, and upgrade-part `TargetItemId` against `BalanceConfigs/Combat_Balance_Config_v0_1.json`.
5. Compare store products in `Design/Monetization/Monetization_Store_Catalog.md` against `Store Grant Rules`.
6. Tune grants and costs through `RewardConfig`, `OperationActionConfig`, `StoreCatalogItem`, combat balance config, and the balance knobs listed above.
7. Use `Balancing_Automated_Test_Plan.md` to add automated harness tests, opt-in probes, and report checks for the tuned economy.

## Reward Presentation Rules

- Reward tiles must use the canonical reward type names above.
- Persistent currency labels must use Credits and Command. Match resource labels must use Materials, Fuel, and Oil. Other rewards use their concrete progression, inventory, unlock, or Operation metric name.
- Supply cases are visual containers only when their contents are listed on the same card or row.
- Mission Briefing reward previews and Mission Result reward grants must use the same `RewardConfig`.
- POP-04 is for major unlocks and milestone grants. POP-05 is for match outcome and all earned rewards. POP-06 is for Operation daily deltas.
- Paid rewards and store items must never override objective completion, star scoring, or Operation consequences.

## UI Resource Strip Rules

| Surface | Resource Strip |
|---|---|
| Main Menu | Credits and Command. |
| Commander Profile | Commander XP, Credits, and Command. |
| Campaign Map | Campaign stars in the chapter header plus the account resource strip in the shell header. |
| Mission Briefing | Reward preview: CommanderXP, Credits, Command where authored, and explicit unlocks/items. |
| Loadout | Mission power requirement and authored roster restrictions; no persistent Fuel cost. |
| Battle HUD | Materials, Fuel, and Oil. Build Capacity may appear as a non-currency limit. |
| Build Drawer | Materials cost plus Fuel only where the authored action consumes Fuel. |
| Operation Dashboard | Credits, Command, Operation metrics, and day/time. |
| District Detail | Authored Credits or OperationSupply costs and resulting Operation metric deltas. |
| Store | Credits and Command. |

## Required Runtime And Data Migration

This design decision changes the target economy but does not silently redefine existing serialized fields. Implementation must be staged and save-safe:

1. Remove `SuppliesText` from the Main Menu resource contract/prefab and bind only persistent Credits and Command.
2. Deprecate `PlayerProfileSaveData.materials` and `PlayerProfileSaveData.fuel`. Define a reviewed save migration before deleting fields; neither may remain visible or spendable as account currency.
3. Remove Materials, Fuel, and wallet Intel from account `RewardConfig`, reward tracks, mission results, store products, and purchase grants. Keep named OperationSupply items and Operation metrics distinct.
4. Rename legacy `CommandAuthority` presentation to Command while preserving serialized/config identifiers until an explicit data migration updates them.
5. Replace `QuickGameConfig.StartingMoney`, `AIStartingMoneySetting`, tactical `FactionEconomy.Money`, and player-dollar UI with starting/authoritative match Materials.
6. Migrate building and unit tactical prices from Credits/Money to Materials, adding Fuel only for authored mobility-dependent costs.
7. Restrict `ResourceExchangeResourceKind` recipes and runtime wallet mutation to Materials, Fuel, Oil, and an explicitly projected Rush allowance. Remove Credits import/export routes and account-wallet access.
8. Update Match HUD, Build Drawer, placement, AI affordability, telemetry, balance tests, configs, and visual locks to use Materials/Fuel/Oil consistently.
9. Validate save migration, Campaign rewards, Store catalog, Skirmish startup, construction/production affordability, AI economy, Resource Exchange, mission result settlement, and both 16:9/20:9 resource headers before declaring migration complete.

Until those steps are implemented, current runtime fields are compatibility debt and must not be cited as product-economy authority.

## Popup And Panel Gameplay Goals

| Surface | Gameplay Goal |
|---|---|
| POP-01 Threat Alert | Convert threat detection into immediate tactical attention and camera/map focus. |
| POP-02 Confirm Raid | Force an informed decision on intel confidence, collateral risk, and raid cost. |
| POP-03 Build Placement | Prevent accidental resource spend by validating footprint, rotation, cost, and placement. |
| POP-04 Reward Unlock | Celebrate a concrete new unit, building, support ability, gear module, cosmetic, or major milestone. |
| POP-05 Mission Result | Resolve victory, partial success, defeat, withdrawal, and operation-resolved outcomes; explain objective/star outcomes, grant authored rewards, show consequences, and route to the source mode. |
| POP-06 End of Day Report | Resolve Operation day simulation, explain city/district deltas, persist state, and start next day. |
| POP-07 Pause / Options | Pause safely, expose settings/help/restart/exit without accidental loss. |
| POP-08 Intel Reveal | Turn patrol/scan/raid/capture results into evidence, Intel gain, and archive progress. |
| POP-12 Resource Logistics Exchange | Let enabled matches redistribute Materials, Fuel, and Oil through lossy timed logistics queue jobs, scenario-approved Rush acceleration, and clear economy-event feedback without touching persistent Credits or Command. |
| PREFAB-01 Objective Tracker | Keep required objectives, star goals, timers, and progress visible during play. |
| PREFAB-02 Squad Tray | Keep squad selection, health, status, and transport state readable for mobile command. |
| PREFAB-03 Build Drawer | Convert build/production catalogs, costs, locks, and queues into compact tactical actions. |
