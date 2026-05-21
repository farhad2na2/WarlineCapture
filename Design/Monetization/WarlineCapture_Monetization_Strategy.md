# WarlineCapture Monetization Strategy

Date: 2026-05-04

## Source Design Inputs

This plan is grounded in the current WarlineCapture design documents:

- `Design/GAME_DESIGN_REFERENCE.md`
- `Design/WarlineCapture_AAA_Mobile_Game_Design_Document_v0_1.md`
- `Design/WarlineCapture_Gameplay_Features_High_Level_Spec.md`
- `Design/WarlineCapture_Gameplay_Features_Detailed_Spec.md`
- `Design/WarlineCapture_Economy_Reward_Design.md`
- `Design/WarlineCapture_Balancing_Automated_Test_Plan.md`
- `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
- `Design/WarlineCapture_UIUX_Mockup_To_Canvas_Conversion_Plan.md`
- `Design/WarlineCapture_UIUX_MainMenu_Visual_Contract.md`
- `Design/Monetization/WarlineCapture_Monetization_Store_Catalog.md`
- `Design/VisualLockLayered/README.md`

## Monetization Goal

WarlineCapture should monetize like a premium mobile strategy game without damaging the tactical RTS promise. Purchases should make the account feel better equipped, better personalized, and more expressive, but mission outcomes should still be driven by objectives, loadout decisions, tactical control, district state, and AI difficulty.

The shop should support the three-mode product:

- Campaign: starter support, cosmetics, unlock catch-up, chapter reward bundles.
- Operations: OperationSupply drops, armory stock, Black Market rotations, and resource-backed district recovery support.
- Skirmish: cosmetic themes, map presets, no progression-pressure monetization.

Campaign monetization never sells campaign stars. Chapter reward bundles mean deterministic resource, BlueprintParts, GearModule, Cosmetic, Supplies, or Command bundles tied to chapter progress.

## Design Principles

1. No pay-to-win tactical resolution.
   Purchases may accelerate account growth or provide resources, but cannot override mission objectives, remove star-goal pressure, or sell guaranteed victory.

2. Preserve readable progression.
   Rewards in Mission Briefing, Mission Result, Reward Unlock, Commander Profile, and Operation End-of-Day must remain understandable. Paid grants use the same `RewardConfig` and `RewardService` model planned in gameplay docs.

3. Sell preparation and identity, not hidden odds.
   Avoid casino-style loot presentation. Supply cases should be deterministic bundles or transparent selection boxes with listed contents.

4. Match the visual lock.
   Store UI uses command-base dark black/olive HUD panels, worn metal bevels, olive selected states, amber/gold reward accents, muted blue command-resource accents, Oxanium-like typography, and separate reusable cards/icons.

5. Support live operations.
   The catalog is data-driven so events, rotating offers, and Operation Black Market stock can be tuned without code changes.

## Product Economy

### Wallet Resources

| Resource | Role | Earned From | Purchased? | Notes |
|---|---|---|---|---|
| Credits | Main spendable resource for buildings, unit production support, common upgrades | Missions, Operation actions, daily reports | Yes, in capped bundles | Maps to tactical Money where the UI needs a player-facing label. |
| Materials | Construction and repair economy | Build objectives, Operation repair, chapter rewards | Yes, in capped bundles | Used for base and district recovery, not instant wins. |
| Fuel | Tactical and strategic mobility resource | Refineries, fuel objectives, Operation logistics, mission rewards | Yes, in capped bundles | Used for deploy costs, vehicle/air support, extraction, and readiness; never injected into an active match. |
| Command Authority | Premium command resource | Profile level milestones, events, purchases | Yes | Used for cosmetics, deterministic bundles, rush tickets. |
| Intel | Limited tactical intel access | Mission rewards, Operation scans, evidence rewards, star-threshold rewards, store bundles | Limited | Reveals mission intel and evidence details, does not reveal hidden win states. |
| Rush Tickets | Time/completion accelerators | Events, starter packs, paid bundles | Yes | Only for production queues and Operation timers where design permits. |

Detailed acquisition, loss, spend, conversion, and balancing rules for all wallet resources and reward types live in `Design/WarlineCapture_Economy_Reward_Design.md`.

Automated balance harness tests, opt-in gameplay/economy probes, and report expectations are planned in `Design/WarlineCapture_Balancing_Automated_Test_Plan.md`.

### Monetized Content Types

| Type | Safe Use | Avoid |
|---|---|---|
| Starter packs | Early resources, cosmetic commander frame, small roster unlock, transparent value | Overpowered units that trivialize Chapter 1 |
| Resource bundles | Credits, Materials, Fuel, Intel, Command Authority, and Rush Tickets with caps and daily purchase limits | Infinite tactical resource injection during live matches |
| Cosmetic skins | Commander frames, unit card skins, squad banners, base HUD themes | Camouflage that affects readability or combat stats |
| Operation supplies | Aid drops, Repair Convoys, Intel Dossiers, Readiness Boosts as `OperationSupply` grants | Removing district consequences completely or granting OperationTrust, OperationSecurity, OperationIntel, or OperationInfrastructure directly |
| Armory bundles | Gear modules, support ability parts, upgrade materials | Selling exclusive combat-only power with no earn path |
| Season pass | Extra deterministic reward track lane | Random loot, hidden odds, mandatory competitive power |

## Store Grant Alignment

All monetized products must map to canonical reward types in `Design/WarlineCapture_Economy_Reward_Design.md`.

- Store products are catalog entries that grant through `RewardConfig` and `RewardService`; store UI does not write wallet, inventory, progression, or Operation state directly.
- Store purchases can grant Credits, Materials, Fuel, Intel, Command Authority, Rush Tickets, BlueprintParts, GearModule, Cosmetic, UnitUnlock, BuildingUnlock, SupportAbilityUnlock, and OperationSupply.
- Store purchases cannot directly grant CampaignStars / legacy `SagaStars`, `OperationTrust`, `OperationSecurity`, `OperationIntel`, or `OperationInfrastructure`.
- Operation supplies bought in the Store create district value only after the player spends them through authored Operation actions.
- Duplicate unlocks, gear, and cosmetics use explicit fallback rewards from the economy design.

## Store Surfaces

### SCN-14 Store / Command Exchange

New screen reached from the Main Menu `STORE` nav button and resource plus buttons.

Required modules:

- Top profile/resource bar reused from Main Menu.
- Left category rail: Featured, Starter Packs, Resources, Armory, Cosmetics, Operation.
- Large featured offer strip with one selected bundle.
- Starter pack row.
- Shop item grid.
- Bottom legal/status line with offer timers and purchase restore entry.
- Ability and upgrade products open `POP-09 Ability / Upgrade Detail` before purchase so target id, unlock source, earn path, duplicate conversion, and disabled purchase reason are visible.

### Operation Black Market

Reached from SCN-11 `BLACK MARKET`. This should be an Operation-flavored store category, not a separate economy.

Safe items:

- Intel dossier packs.
- Materials for authored repair actions.
- Aid Convoy and Repair Convoy `OperationSupply` grants.
- Armory stock refresh ticket.
- Cosmetic district map markers.

### Armory

Reached from SCN-11 `ARMORY` and SCN-07 Loadout gear prompts.

Safe items:

- Gear module bundles with transparent contents.
- Support ability parts.
- Loadout slot cosmetic frames.
- Unit card portrait treatments.

Armory product links route to `SCN-19 Armory` for owned roster/upgrade inspection and to `POP-09 Ability / Upgrade Detail` for the selected ability or upgrade track.

## Starter Pack Ladder

Starter packs should expire from the main featured area after the account matures, but remain claimable/purchasable under Starter Packs until the player owns them or passes their eligibility window.

| Pack | Intended Player | Positioning |
|---|---|---|
| Recon Starter Pack | First 24 hours / Chapter 1 | Cheap first-purchase value, focused on onboarding and identity. |
| Base Builder Pack | Player who likes construction/economy | Helps build and repair without bypassing objective skill. |
| Operation Founder Pack | Operations entry | Tactical support for district management and cosmetic prestige. |

Detailed contents are in `WarlineCapture_Monetization_Store_Catalog.md`.

## Battle Pass / Season Track

The Commander Profile visual lock already includes `REWARD TRACK / SEASON 7`. This should become a season track with:

- Free lane: Command Authority, Credits, cosmetic badges, deterministic resource bundles, CommanderXP.
- Premium lane: extra cosmetics, deterministic armory bundles, operation supplies, commander frame.
- No hidden random rewards.
- No paid-only unit that has no gameplay earn path.
- No direct CampaignStars, OperationTrust, OperationSecurity, OperationIntel, or OperationInfrastructure grants.

Recommended price anchor:

- Season Pass: `$9.99`
- Premium + 10 deterministic season claim nodes: `$19.99`

## Pricing Architecture

Use familiar mobile price anchors while keeping the first implementation conservative.

| Tier | Price | Use |
|---|---:|---|
| Entry | `$0.99` to `$1.99` | First purchase, small Command Authority bundles |
| Starter | `$4.99` | Primary starter pack |
| Core | `$9.99` | Season pass, strong bundles |
| Premium | `$19.99` | Operation Founder, pass plus deterministic season claim nodes |
| Whale-safe cap | `$49.99` | Large Command Authority/resource pack with strict purchase and economy caps |

First implementation ships the designed Command Exchange catalog with purchase CTAs in `DesignedUnavailable` state. Release purchase activation requires profile persistence, RewardService, wallet, receipt validation, and platform product ids.

## Implementation Plan

### Phase 1 - Design and Data

- Add this strategy.
- Add deterministic catalog definitions.
- Add visual targets and item card art under `Design/Monetization`.

### Phase 2 - Reward/Progression Foundation

- Implement `Wallet`, `CatalogItem`, `OfferDefinition`, `PurchaseGrant`, and `PurchaseReceipt` as data contracts.
- Reuse planned `RewardService` for grants.
- Persist owned cosmetics and purchased one-time offers in `PlayerProfileState`.

### Phase 3 - Store UI

- Add SCN-14 Store visual lock target.
- Build Store screen from separate cards, tabs, resource counters, offer timer, and purchase buttons.
- Wire Main Menu `STORE`, resource plus buttons, Operation `BLACK MARKET`, and Operation `ARMORY` to the correct categories.

### Phase 4 - Platform IAP

- Add platform product id mapping.
- Add receipt validation stub for editor, real validation for release.
- Add restore purchases.
- Add server-authoritative receipt hooks for release builds that use backend validation.

## Guardrails

- Do not sell match victory, star completion, enemy nerfs, or invulnerability.
- Do not inject paid resources directly into an active tactical match. Deploy-cost purchases are pre-launch actions governed by explicit mission rules.
- Do not sell hidden-odds containers.
- Do not make Operation district damage feel punitive without free recovery routes.
- Do not add monetization directly to `MenuView.cs`; use the routed UI layer and data-driven services planned in the UI/gameplay specs.
