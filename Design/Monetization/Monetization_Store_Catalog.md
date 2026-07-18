# WarlineCapture Store Catalog

Date: 2026-05-04

This catalog is the design-facing source of truth for starter packs, featured offers, and shop items. It maps directly to future ScriptableObject or JSON data under gameplay configs using `RewardService`, profile persistence, and wallet systems.

2026-07-18 economy rule: Main Menu and top-level store UI present exactly `Credits` and `Command`. Existing `command_authority` ids may remain as legacy compatibility identifiers until migrated. Materials, Fuel, and Oil are match-only and cannot be sold or granted to the account. Intel is an Operation metric or named dossier/supply item, not wallet currency.

## Product Id Convention

Use stable ids before platform-specific product ids are assigned.

```text
pc.<category>.<slug>.<tier>
```

Examples:

- `pc.starter.recon.entry`
- `pc.resource.command_authority.small`
- `pc.cosmetic.commander_frame.blue_vanguard`

## Categories

| Category | Store Tab | Routes |
|---|---|---|
| Featured | Featured | Main Menu Store |
| StarterPack | Starter Packs | Main Menu Store, onboarding prompts |
| ResourceBundle | Resources | Resource plus buttons |
| Armory | Armory | Store, Loadout, Operation Armory |
| Cosmetic | Cosmetics | Store, Commander Profile |
| Operation | Operation | Store, Operation Black Market |
| Season | Season | Commander Profile reward track |

Store products that target an ability or upgrade track must expose `POP-09 Ability / Upgrade Detail` from the product card. The popup displays the exact target id, unlock moment, earn path, duplicate conversion, and disabled purchase reason before any future receipt flow can start.

## Starter Packs

| Id | Name | Price | Eligibility | Contents | Visual Direction |
|---|---|---:|---|---|---|
| `pc.starter.recon.entry` | Recon Starter Pack | `$4.99` | Account level 1-8 or first 72 hours | 2,500 Credits; 120 Command; Ranger Squad unit unlock or 40 parts if owned; Blue Vanguard commander frame | Compact tactical supply case, cyan trim, infantry badge, first-purchase ribbon. |
| `pc.starter.base_builder.core` | Base Builder Pack | `$9.99` | Account level 3-15; after first build mission | 8,000 Credits; 4 Rush Tickets; Guard Tower blueprint parts x40; Construction Queue skin; 2 Aid Convoys | Heavy construction case, amber/gold trim, crane/fortification icon. |
| `pc.starter.operation_founder.premium` | Operation Founder Pack | `$19.99` | Unlocks when Operations opens | 12,000 Credits; 350 Command; 3 Repair Convoys; Founder district map marker set; Operation Founder profile badge | Large command container, blue/gold command-base trim, district planning display, premium badge. |

## Featured Offers

| Id | Name | Price | Rotation | Contents | Notes |
|---|---|---:|---|---|---|
| `pc.featured.command_ready.week01` | Command Ready Bundle | `$9.99` | Weekly | 7,500 Credits; 200 Command; 2 Rush Tickets | Good first featured offer after starter purchase. |
| `pc.featured.airlift_support.week01` | Airlift Support Bundle | `$14.99` | Weekly, after air units unlock | 300 Command; Helicopter skin; Transport upgrade parts x30; 3 Rush Tickets | Cosmetic-forward with listed parts. |
| `pc.featured.district_recovery.week01` | District Recovery Bundle | `$9.99` | Operation event | 1 Repair Convoy; 2 Aid Convoys; 1 Intel Dossier item | Appears after high-damage Operation day. |

## Resource Bundles

| Id | Name | Price | Contents |
|---|---|---:|---|
| `pc.resource.command_authority.small` | Command S | `$1.99` | 120 Command |
| `pc.resource.command_authority.medium` | Command M | `$4.99` | 330 Command |
| `pc.resource.command_authority.large` | Command L | `$9.99` | 750 Command |
| `pc.resource.credits.small` | Credit Cache | `$2.99` | 5,000 Credits |

## Armory Items

| Id | Name | Cost | Contents | Design Notes |
|---|---|---:|---|---|
| `pc.armory.ranger_parts.common` | Ranger Parts Case | 180 Command | Ranger Squad parts x40 | Helps unlock early recon roster without exclusive power. |
| `pc.armory.apc_upgrade.rare` | APC Upgrade Case | 320 Command | APC armor module parts x35; 1 Rush Ticket | Deterministic. Do not use random rarity reveal. |
| `pc.armory.support_drone.rare` | Support Drone Kit | 280 Command | Drone Scan support parts x30; 1 Intel Dossier item | Bridges Loadout and Operation scan loop. |
| `pc.armory.queue_rush.utility` | Queue Rush Tickets | 150 Command | 5 Rush Tickets | Only applies to production/Operation timers, not active combat cooldowns. |

## Cosmetics

| Id | Name | Cost | Contents | Surface |
|---|---|---:|---|---|
| `pc.cosmetic.commander_frame.blue_vanguard` | Blue Vanguard Frame | 250 Command | Commander profile frame | Commander Profile |
| `pc.cosmetic.squad_card.night_ops` | Night Ops Squad Cards | 300 Command | Unit card skin set | Loadout, Battle HUD |
| `pc.cosmetic.base_banner.iron_guard` | Iron Guard Banner | 220 Command | Base banner and profile badge | Main Menu, Profile |
| `pc.cosmetic.hud_accent.amber_command` | Amber Command HUD Accent | 400 Command | HUD accent theme | App shell and Store preview; runtime activation requires the UI theme service |

## Operation Supplies

| Id | Name | Cost | Contents | Operation Effect |
|---|---|---:|---|---|
| `pc.operation.intel_dossier` | Intel Dossier | 120 Command | 1 Intel Dossier item | Applies an authored OperationIntel delta when consumed. |
| `pc.operation.aid_convoy` | Aid Convoy | 180 Command | 2 Aid Convoys | Raises district trust/stability through existing action rules. |
| `pc.operation.repair_convoy` | Repair Convoy | 220 Command | 1 Repair Convoy | Supports infrastructure recovery through its authored Operation action. |
| `pc.operation.readiness_boost` | Readiness Boost | 160 Command | 1 readiness action authority | Improves Force Readiness for next Operation day only. |

## Season Products

| Id | Name | Price | Contents |
|---|---|---:|---|
| `pc.season.pass.standard` | Season Pass | `$9.99` | Unlocks premium reward track lane for current season |
| `pc.season.pass.plus10` | Season Pass + 10 Levels | `$19.99` | Premium lane plus 10 fixed level claims |

## First-Pass Store Layout

Featured row:

1. Recon Starter Pack
2. Command Ready Bundle
3. Blue Vanguard Frame

Starter Packs row:

1. Recon Starter Pack
2. Base Builder Pack
3. Operation Founder Pack

Grid:

1. Command M
2. Credit Cache
3. Ranger Parts Case
4. Support Drone Kit
5. Intel Dossier
6. Aid Convoy
7. Night Ops Squad Cards

## Balance Notes

- Every gameplay-affecting item should have an earn path through Campaign, Operations, profile levels, or events.
- Paid resource bundles should be useful but not so large that Chapter 1 economy tuning becomes irrelevant.
- Duplicate starter pack unit unlocks convert to item-specific BlueprintParts.
- Cosmetic ownership should be permanent and account-wide.
- Offer timers should never imply loss of critical gameplay access.
- Store balance tests and opt-in economy probes are planned in `../Balancing_Automated_Test_Plan.md`.

## Reward Type Mapping

Each catalog content line maps to canonical reward types in `Design/Economy_Reward_Design.md`. Unit, building, support ability, gear, and upgrade-part target ids must resolve to `Design/BalanceConfigs/Combat_Balance_Config_v0_1.json`; visual presentation must resolve through `Design/VisualConfigs/Combat_Visual_Config_v0_1.json`.

| Catalog Content | Canonical Reward Type |
|---|---|
| Credits, Command, Rush Tickets | Matching persistent currency or inventory reward type. |
| Ranger Squad unit unlock | `UnitUnlock` with duplicate fallback to `BlueprintParts`. |
| Ranger Squad parts, Guard Tower blueprint parts, Transport upgrade parts, APC armor module parts, Drone Scan support parts | `BlueprintParts` with `TargetItemId`. |
| Helicopter skin, commander frame, unit card skin set, base banner, HUD accent theme, profile badge, district map marker set | `Cosmetic`. |
| Aid Convoy, Repair Convoy, readiness action authority | `OperationSupply`. |
| Season pass and pass plus levels | Product entitlement that unlocks fixed `RewardConfig` claim nodes. |

Concrete early store target ids:

| Store Content | Required Target Id |
|---|---|
| Ranger Squad unit unlock / parts | `Unit_Chr_Soldier_Male_02_Alt_04`; duplicate fallback grants item-specific `BlueprintParts`. |
| Guard Tower blueprint parts | `Building_GuardTower`. |
| Airlift Support transport upgrade parts | `upgrade.air.transport_aircraft`. |
| APC armor module parts | `upgrade.vehicle.apc_armor`. |
| Drone Scan support parts | `ability.drone_scan`. |
