# WarlineCapture Economy And Reward Design

Date: 2026-05-05

## Purpose

This document locks the player-facing resources and reward types used by UI screens, popups, panels, store designs, mission results, Operation reports, and profile progression. It removes generic resource names and generic reward language from the design layer.

It is also the economy-balancing source of truth. A balancer should be able to read this document and understand where each resource or reward comes from, how it leaves the economy, how it converts into other gameplay value, and which tuning knobs control the rate.

Mission and Operation reward pacing should follow `Gameplay_North_Star_And_Content_Grammar.md` and `Level_And_Mission_Content_Plan.md`: Chapter 1 exposes only the resources needed for teaching, Operation introduces district metrics through authored actions and consequences, and store/profile resources remain separate from objective and star completion.

Unit, building, support ability, gear, and upgrade-part reward targets must resolve to ids in `Combat_Catalog_And_Upgrade_Design.md` and `BalanceConfigs/Combat_Balance_Config_v0_1.json`. Visual reward presentation for those targets must use `VisualConfigs/Combat_Visual_Config_v0_1.json`.

## Canonical Player-Facing Resources

| Resource | Gameplay Role | Primary Sources | Primary Sinks | UI Surfaces |
|---|---|---|---|---|
| `Credits` | Main spendable resource. Maps to the existing tactical `Money` economy where the player-facing UI needs a readable account/match resource name. | Mission rewards, Operation end-of-day income, resource sales, profile milestones, store bundles. | Building placement, unit production, Operation actions, upgrades. | Main Menu resource strip, Mission Result, Build Drawer, Operation Dashboard, Store. |
| `Materials` | Construction, repair, infrastructure, and upgrade resource. | Mission rewards, construction objectives, Operation repair/aide routes, chapter rewards, store bundles. | Building/outpost construction, district repair, gear/module upgrades. | Main Menu resource strip, Build Drawer, Operation Dashboard, District Detail, End of Day, Store. |
| `Fuel` | Tactical and strategic mobility resource. | Refineries, fuel production objectives, Operation logistics, mission rewards. | Deploy costs, air/vehicle operations, extraction missions, Operation readiness actions. | Loadout deploy cost, Battle HUD, Mission Result, End of Day. |
| `Intel` | Non-premium information resource for revealing threats, mission details, and hidden-cell evidence. | Drone Scan, Patrol, Raid success, CaptureIntel objectives, mission rewards, Operation events. | Mission intel reveal, threat detail unlocks, Operation evidence archive actions. | Mission Briefing, Operation Dashboard, District Detail, Intel Reveal. |
| `Command Authority` | Premium command resource used for fixed account convenience and cosmetics. | Profile milestones, events, season track, purchases. | Cosmetics, fixed-content bundles, rush tickets, season pass claims. | Main Menu third resource counter, Store, Commander Profile reward track. |
| `Rush Tickets` | Deterministic time-compression item for queues and Operation timers only. | Events, season rewards, starter packs, store bundles. | Production queue rush, Operation action timer acceleration. | Build Drawer, Store, Mission Result. |

## Tactical Faction Resources

The simulation-level faction resources remain:

- `Money`
- `Oil`
- `Fuel`

UI mapping:

- Player-facing `Credits` represents tactical/account money where a polished UI label is needed.
- `Oil` remains a production/economy stat when the screen specifically explains extraction/refining.
- `Fuel` keeps the same name across tactical and account surfaces.

Field logistics sources:

- `Field_Logistics_Oil_Fuel_Design.md` owns the tactical Oil Pump -> Oil Truck -> Refinery -> Fuel -> Fuel Bladder/Tanker baseline, including build menu usage, match HUD rules, and config-aligned roles.
- `Automated_Fuel_Logistics_Design.md` owns the automated logistics model. Header Fuel is usable faction Fuel delivered into Fuel Bladder/base storage; raw Oil, Oil in transit, refinery input, refinery output Fuel, and tanker cargo are not header Fuel.
- `Resource_Logistics_Exchange_Design.md` owns the optional timed match Resource Exchange popup. It can export surplus tactical Oil/Materials/Fuel into Credits or import Materials/Fuel with Credits through authored recipes, queue timers, Rush Ticket acceleration, and economy-event logging. It is not the Store and it is not instant free conversion.

## Canonical Reward Types

| Reward Type | Meaning | UI Presentation |
|---|---|---|
| `CommanderXP` | Profile progression. | XP chevron/badge icon, numeric delta, level progress bar. |
| `Credits` | Main spendable resource grant. | Credit stack icon, amount. |
| `Materials` | Construction/repair grant. | Material stack/ingot icon, amount. |
| `Fuel` | Mobility/deploy resource grant. | Fuel can/drum icon, amount. |
| `Intel` | Evidence/intel resource grant. | Dossier/radar icon, amount. |
| `CommandAuthority` | Premium command resource. | Command insignia icon, amount. |
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

## Resource Lifecycle Specs

| Resource | Acquired From | Lost Or Reduced By | Used For | Conversion Rules | Store And Reward Rules | Balance Knobs |
|---|---|---|---|---|---|---|
| `Credits` | Mission clear rewards, first-clear bonuses, objective rewards, Operation end-of-day income, resource-sale rewards, profile milestones, season nodes, event nodes, store resource bundles. | Building placement, unit production support, common upgrades, Operation actions, store catalog purchases priced in Credits, tactical match spend. No passive account decay. Tactical match Credits reset at match end unless granted through `RewardConfig`. | Base/build placement, unit queues, Operation actions, upgrades, resource-gated UI unlocks. | Tactical `Money` maps to player-facing Credits in UI. Mission result grants Credits only through `RewardConfig`; tactical match float is not automatically banked. | Store may sell capped Credit bundles. Paid Credits cannot be injected into an active tactical match. | `BaseMissionCreditGrant`, `FirstClearCreditMultiplier`, `DifficultyCreditMultiplier`, `OperationDailyCreditIncome`, `CreditBundleCap`, `CreditSpendCurve`, `Chapter1CreditCeiling`. |
| `Materials` | Mission rewards, build/construction objectives, Operation repair routes, chapter rewards, profile milestones, season nodes, event nodes, store resource bundles. | Building/outpost construction, district repair, gear/module upgrades, repair convoy crafting or action costs. No passive account decay. | Construction, repair, infrastructure recovery, upgrade crafting. | Materials can be converted into `OperationInfrastructure` only by authored Operation repair actions. Materials do not convert directly into Command Authority. | Store may sell capped Material bundles and bundles that include Materials with Operation supplies. | `BaseMaterialGrant`, `ConstructionObjectiveBonus`, `RepairActionMaterialCost`, `UpgradeMaterialCurve`, `MaterialBundleCap`, `InfrastructureDeltaPerMaterialBand`. |
| `Fuel` | Refineries, fuel production objectives, Operation logistics rewards, mission rewards, season/event nodes, store bundles. | Deploy costs, air/vehicle operation costs, extraction mission costs, Operation readiness actions. Pre-launch deploy cost is refunded when launch is canceled before match start; spent Fuel is not refunded after match start. | Mission launch, vehicle/air support, extraction, readiness. | Tactical `Oil` converts to tactical `Fuel` through refinery/production gameplay. Account Fuel is granted only through `RewardConfig`; tactical Oil is not sold in the store. | Store may include Fuel in fixed-content bundles only when the bundle is capped and does not bypass active-match resource pressure. | `FuelPerRefineryObjective`, `DeployFuelCostCurve`, `AirSupportFuelCost`, `OperationReadinessFuelCost`, `FuelBundleCap`, `FuelScarcityByChapter`. |
| `Intel` | Drone Scan, Patrol, Raid success, CaptureIntel objectives, evidence rewards, Operation events, mission rewards, season/event nodes, store Intel Dossier bundles. | Mission intel reveals, threat detail unlocks, evidence archive actions, Operation investigation actions. | Revealing enemy families, threat confidence, hidden-cell evidence, mission modifiers, Operation action clarity. | Spending Intel can increase an `OperationIntel` confidence metric through authored investigation actions. `OperationIntel` deltas do not automatically mint wallet Intel. | Store may sell Intel Dossier bundles. Paid Intel reveals information only; it cannot reveal hidden win states or auto-complete objectives. | `IntelPerScan`, `IntelPerRaidSuccess`, `RevealIntelCost`, `ThreatDetailIntelCost`, `StoreIntelCap`, `IntelConfidenceDeltaCurve`. |
| `Command Authority` | Profile milestones, event nodes, free season nodes, paid season products, direct platform purchases, fixed store bundles. | Cosmetics, fixed-content bundles, Rush Tickets, season pass claims that require Command Authority, store items priced in Command Authority. No gameplay failure, timer, or Operation event removes Command Authority. | Account convenience, cosmetics, fixed-content bundles, queue/timer accelerators. | Command Authority converts to specific catalog items only through Store/Command Exchange purchase definitions. It does not convert directly into Campaign stars, Operation Trust, Operation Security, Operation Infrastructure, active-match combat power, or victory. | Direct purchase resource. Every spend must create a receipt/economy event and grant through `RewardService`. | `AuthorityEarnFreeTrack`, `AuthorityPriceAnchors`, `AuthorityBundleGrant`, `DailyAuthoritySpendCap`, `RushTicketAuthorityCost`, `CosmeticPriceBands`. |
| `Rush Tickets` | Events, season rewards, starter packs, Command Exchange bundles, profile milestones. | Production queue rush, Operation timer acceleration. Consumed at confirmation; no refund after time is reduced. | Shortening build/production queues and Operation action timers. | Rush Tickets convert into time reduction by `RushTicketSecondsPerTicket`. They never reduce active combat cooldowns, objective timers, enemy timers, or star-goal timers. | Store may sell Rush Tickets directly or inside fixed-content bundles. Mission rewards may grant Rush Tickets as utility rewards. | `RushTicketSecondsPerTicket`, `RushTicketMaxPerQueue`, `RushTicketMaxPerDay`, `RushTicketStorePrice`, `QueueRushEligibility`. |

## Reward Lifecycle Specs

| Reward Type | Acquired From | Lost Or Reduced By | Used For | Conversion Rules | Balance Knobs |
|---|---|---|---|---|---|
| `CommanderXP` | Mission results, first clears, profile milestones, season/event nodes, Operation end-of-day reports. | Never spent and never reduced in normal play. Profile rollback can only come from save recovery tooling. | Commander level, profile rewards, feature unlock gates. | XP converts to Commander levels through the XP curve. Level-up nodes grant authored `RewardConfig` items. | `XpPerMission`, `FirstClearXpBonus`, `DifficultyXpMultiplier`, `XpCurve`, `LevelRewardCadence`. |
| `Credits` | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. |
| `Materials` | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. |
| `Fuel` | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. |
| `Intel` | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. |
| `CommandAuthority` | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. |
| `RushTicket` | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. | Same as resource lifecycle. |
| `UnitUnlock` | Saga first clears, chapter rewards, profile milestones, store starter packs, season/event nodes. | Never lost in normal play. Owned unlocks persist account-wide. | Roster availability, loadout choices, tutorial/progression gates. | Duplicate unit unlock grants convert to `BlueprintParts` using `DuplicateUnitUnlockParts`. | `UnitUnlockChapter`, `DuplicateUnitUnlockParts`, `UnitPartsThreshold`, `StarterUnlockEligibility`. |
| `BuildingUnlock` | Saga chapters, build tutorials, Operation progression, profile milestones, store packs that include blueprint parts. | Never lost in normal play. Owned unlocks persist account-wide. | Build catalog availability and Operation action availability. | Duplicate building unlock grants convert to `BlueprintParts` using `DuplicateBuildingUnlockParts`. | `BuildingUnlockChapter`, `BuildingPartsThreshold`, `DuplicateBuildingUnlockParts`. |
| `SupportAbilityUnlock` | Mission rewards, Operation milestones, profile milestones, armory bundles, season/event nodes. | Never lost in normal play. Owned unlocks persist account-wide. | Loadout support slots, Operation support actions, tactical command options. | Duplicate support unlock grants convert to `BlueprintParts` using `DuplicateSupportUnlockParts`. | `SupportUnlockChapter`, `SupportPartsThreshold`, `SupportChargeEconomy`. |
| `BlueprintParts` | Duplicate unlock conversion, armory bundles, mission rewards, profile rewards, season/event nodes. | Spent when a part threshold unlocks or upgrades the target item. | Unlocking or upgrading a specific unit, building, support ability, or gear module. | Parts are item-specific by `TargetItemId`. Cross-item conversion is not allowed. Overflow after threshold remains on the same `TargetItemId`. | `PartsPerMission`, `PartsPerBundle`, `PartsThresholdByRarity`, `DuplicatePartsFallback`. |
| `GearModule` | Mission rewards, armory bundles, support/drone kits, Operation rewards, season/event nodes. | Consumed by upgrade/fusion rules or converted to parts by duplicate logic. Equipped modules are not destroyed by mission failure. | Loadout power expression, unit role specialization, support ability tuning. | Duplicate module grants convert to `BlueprintParts` for that module using `DuplicateGearModuleParts`. | `GearDropCadence`, `GearUpgradeMaterialCost`, `DuplicateGearModuleParts`, `GearPowerBudget`. |
| `Cosmetic` | Store purchases, season pass, events, profile rewards, starter packs. | Never lost. Paid duplicate purchases are blocked by ownership checks. Free duplicate cosmetic rewards convert to Command Authority through explicit `DuplicateCosmeticAuthorityGrant`. | Commander frames, unit card skins, banners, HUD accent themes, profile badges. | Cosmetics never convert into combat stats. Duplicate fallback grants Command Authority only when the reward config includes that fallback. | `CosmeticPriceBand`, `DuplicateCosmeticAuthorityGrant`, `EventCosmeticCadence`. |
| `OperationSupply` | Operation rewards, store Operation tab, starter packs, event nodes. | Consumed by matching Operation actions. Event-tagged supplies use explicit `ExpiresAt`; permanent supplies use `ExpiresAt=null`. | Aid Convoy, Repair Convoy, Readiness Boost, Armory Stock Refresh, and other named Operation actions. | Supplies convert to Operation metric deltas only through the action that consumes them. | `AidConvoyTrustDelta`, `RepairConvoyInfrastructureDelta`, `ReadinessBoostDuration`, `SupplyStorePrice`, `SupplyDailyUseCap`. |
| `CampaignStars` / legacy `SagaStars` storage | Best mission star result from Campaign mission completion. | Never spent or reduced by normal play. Replays update the saved best star count for that mission. | Chapter reward thresholds, progression gates, achievement pacing. | Stars unlock reward thresholds; claiming a threshold does not consume stars. | `StarGoalDifficulty`, `ChapterStarThresholds`, `StarRewardCadence`. |
| `OperationTrust` | Aid actions, civilian-save outcomes, positive daily events, OperationSupply actions. | Civilian casualties, collateral damage, failed raids, negative daily events. | District stability, public support, action risk modifiers, narrative reports. | Trust is an Operation metric delta, not a wallet resource. Store items grant supplies/resources; they do not grant Trust directly. | `TrustGainAid`, `TrustLossCollateral`, `TrustDailyDecay`, `TrustRiskThresholds`. |
| `OperationSecurity` | Patrols, successful raids, defensive builds, threat clear missions, daily security actions. | Enemy influence, unresolved threats, failed defense, negative events. | Threat spawn rate, district lock state, Black Market risk, mission availability. | Security is an Operation metric delta, not a wallet resource. Store items grant supplies/resources; they do not grant Security directly. | `SecurityGainPatrol`, `SecurityGainRaid`, `SecurityLossThreat`, `ThreatSpawnCurve`. |
| `OperationIntel` | Drone Scan, Patrol evidence, Raid success, Intel spend actions, evidence archive progress. | Enemy counter-intel events, stale intel decay, failed investigations. | Raid confidence, hidden-cell discovery, mission briefing accuracy, threat detail visibility. | Wallet Intel can be spent to raise OperationIntel through authored actions. OperationIntel does not convert back to wallet Intel. | `IntelConfidencePerScan`, `IntelDecayPerDay`, `RaidConfidenceThresholds`, `InvestigationIntelCost`. |
| `OperationInfrastructure` | Repair actions, construction objectives, Repair Convoys, successful defense, daily recovery. | Collateral damage, attacks, sabotage, unresolved fires/damage events. | District income, build availability, civilian stability, Operation recovery pacing. | Materials and Repair Convoys convert to Infrastructure only through repair actions. Infrastructure does not convert back to Materials. | `InfrastructureRepairCost`, `InfrastructureDamageEvent`, `IncomePerInfrastructureBand`, `RepairConvoyDelta`. |

## Conversion Matrix

| From | To | Trigger | Rule |
|---|---|---|---|
| Tactical `Money` | `Credits` display label | Tactical HUD and result presentation. | UI labels tactical money as Credits. Account banking occurs only through `RewardConfig`. |
| Tactical `Oil` | Tactical `Fuel` | Refinery/production gameplay plus tanker delivery into storage. | Tactical production converts Oil into refinery output Fuel; it becomes header usable Fuel only after delivery into Fuel Bladder/base storage. Account Fuel grants are separate rewards. |
| Tactical `Oil`, `Materials`, or `Fuel` | `Credits` | Resource Logistics Exchange export job. | Allowed only when the active match enables `Resource_Logistics_Exchange_Design.md`; input is spent/reserved at confirmation and Credits are granted on timed completion using authored rate/fee/cap. |
| `Credits` | Tactical `Materials` or `Fuel` | Resource Logistics Exchange import job. | Allowed only when the active match enables `Resource_Logistics_Exchange_Design.md`; Credits are spent at confirmation and output is granted on timed completion, respecting storage/capacity rules. |
| `Credits`, `Materials`, `Fuel`, `Intel` | Operation metric deltas | Authored Operation action. | Spending resources applies the action's configured Trust, Security, Intel, or Infrastructure delta. |
| `Command Authority` | Store item or `RushTicket` grant | Command Exchange purchase. | Purchase spends Command Authority and grants the exact catalog `RewardConfig`. |
| `RushTicket` | Time reduction | Queue or Operation timer rush. | Each ticket reduces time by `RushTicketSecondsPerTicket`, capped per queue/day. |
| Duplicate unlock | `BlueprintParts` | Reward grant for already owned unit/building/support. | `RewardService` grants item-specific parts using the duplicate fallback in the reward config. |
| Duplicate gear module | `BlueprintParts` | Reward grant for already owned module. | Grants item-specific module parts using the duplicate fallback in the reward config. |
| Free duplicate cosmetic | `CommandAuthority` | Reward grant for already owned cosmetic. | Grants configured `DuplicateCosmeticAuthorityGrant`; paid duplicate purchase is blocked before purchase. |
| `BlueprintParts` | Unlock or upgrade | Part threshold reached. | Item-specific threshold consumes required parts and leaves overflow on the same item id. |
| `OperationSupply` | Operation metric delta | Matching Operation action consumes supply. | Supply action applies configured metric deltas and consumes the item. |

## Store Grant Rules

- Store products are catalog entries that map to `RewardConfig` items. The store never writes wallet, inventory, progression, or Operation state directly.
- Store purchases cannot grant CampaignStars / legacy `SagaStars`, `OperationTrust`, `OperationSecurity`, `OperationIntel`, or `OperationInfrastructure` directly.
- Store purchases can grant Credits, Materials, Fuel, Intel, Command Authority, Rush Tickets, BlueprintParts, GearModule, Cosmetic, UnitUnlock, BuildingUnlock, SupportAbilityUnlock, and OperationSupply.
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
- UI labels must use Credits, Materials, Fuel, Intel, Command Authority, Rush Tickets, or a concrete unlock item name.
- Supply cases are visual containers only when their contents are listed on the same card or row.
- Mission Briefing reward previews and Mission Result reward grants must use the same `RewardConfig`.
- POP-04 is for major unlocks and milestone grants. POP-05 is for match outcome and all earned rewards. POP-06 is for Operation daily deltas.
- Paid rewards and store items must never override objective completion, star scoring, or Operation consequences.

## UI Resource Strip Rules

| Surface | Resource Strip |
|---|---|
| Main Menu | Credits, Materials, Command Authority. |
| Commander Profile | Commander XP, Credits, Materials, Command Authority. |
| Campaign Map | Campaign stars in the chapter header plus the account resource strip in the shell header. |
| Mission Briefing | Reward preview: CommanderXP, Credits, Materials, explicit unlocks. |
| Loadout | Deploy cost: Fuel plus mission power requirement. |
| Battle HUD | Tactical Credits, Fuel, and Build Capacity. |
| Build Drawer | Tactical Credits, Materials, Fuel and Build Capacity. |
| Operation Dashboard | Credits, Materials, Fuel, Intel, day/time. |
| District Detail | Action costs shown in Credits, Materials, Fuel, Intel. |
| Store | Credits, Materials, Command Authority. |

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
| POP-12 Resource Logistics Exchange | Let enabled matches export surplus tactical resources for Credits or import Materials/Fuel with Credits through timed logistics queue jobs, Rush Ticket acceleration, and clear economy-event feedback. |
| PREFAB-01 Objective Tracker | Keep required objectives, star goals, timers, and progress visible during play. |
| PREFAB-02 Squad Tray | Keep squad selection, health, status, and transport state readable for mobile command. |
| PREFAB-03 Build Drawer | Convert build/production catalogs, costs, locks, and queues into compact tactical actions. |
