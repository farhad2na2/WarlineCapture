# WarlineCapture monetization plan and resource audit

Date: 2026-09-28
Status: Detailed monetization proposal; mission-planning application adopted on 2026-09-28 via the linked contract/register. Prices remain hypotheses; no billing, economy, mission or entitlement runtime changes activated.
Audit baseline: working tree at `e3dd2bb726428d82f4a7010ce1291990ee851066`, including concurrent uncommitted Support work.
Requested scope: re-audit menus, match resources and missions; recommend the business model and ARIA policy; specify implementation and validation work.

This plan develops and corrects the [initial recommendation](../AgentReports/2026-09-28_pm_monetization-model-recommendation.md). The owner subsequently requested application to all mission plans. The [mission product contract](Mission_Product_Contract_2026-09-28.md) and [205-entry register](Mission_Product_Policies_2026-09-28.csv) now govern those plans; dated amendments in the strategy/store/economy authorities supersede their conflicting consumable proposals for this scope. Runtime and commercial release work in section 12 remains pending. The approved September 28 Support scope remains intact.

## 1. Recommended business and player promise

**Sell the authored game and later additional content. Include ARIA with every accessible mission. Do not sell resources, stronger troops, faster recovery, or help escaping a deliberately frustrating mission.**

Recommended mobile offer:

- Chapter 1, all five missions, free with unlimited replays, its complete story/debriefs, and the same ARIA functionality as purchased content.
- One accepted Skirmish demonstration scenario, with a fixed scenario-provided roster, repeatable without a play counter. The existing Desert Base / S001 ground Field Base route is the proposed choice, subject to release certification.
- A free, self-contained **three-mission Operations introduction: O001–O003**, with its own local-services recap and unlimited replays after completion. It must not leave a six-district city decaying behind a paywall.
- A **Campaign Edition containing the complete 25-mission campaign and all 120 core Skirmish scenarios, S001–S120**. Keep **$9.99 USD as the initial price hypothesis**, and test $12.99 / $14.99 against it once the complete package's play value and costs are understood. These are candidate prices, not a validated range or an approved increase. Regional storefront prices are localized, not hardcoded dollar strings.
- A separate **Operations: Sahrin Theater**, provisionally **$7.99**, containing the complete connected six-district / 60-mission mode when it is finished. It can be purchased independently of Campaign Edition. The free three missions are included in the 60, not added to inflate the count.
- Later finished campaigns, Operations theaters and Skirmish collections can be additional permanent purchases. **All 120 currently planned Skirmish entries belong to the core purchase; later paid Skirmish content must add to S001–S120.** This finite package does not promise every future mission forever.
- No ads, season pass, premium currency bundles, paid Support, paid ARIA tier, subscription, or rotating scarcity offers at launch.
- Optional cosmetic packs are a later experiment. They are not included in the launch revenue forecast or on its critical path.

This is a recommendation for a sustainable premium product with a manageable ongoing workload, as requested by the owner. It is not a claim that a $9.99 price can finance the entire roadmap without sales evidence and a costed production budget. The owner's follow-up questions about free modes and lifetime inclusion are resolved by the finite packages above. These are recommended packaging decisions, not a claim that the owner has approved prices or activated products.

### Product promise to adopt

> Play Chapter 1, one Skirmish scenario and the Operations introduction free. Campaign Edition includes the complete 25-mission story and all 120 core Skirmish scenarios on five maps. The complete Operations theater and additional content beyond the stated package are separate optional purchases. ARIA guidance and Watch ARIA play are included wherever the mission is available. Every purchased campaign or theater can be finished without another purchase.

Replace the current “full campaign story ... never through payment” pillar with this explicit trial-plus-purchase promise. Retain the prohibition on paid stars, paid outcomes, hidden odds, and buying the correct ending. Expansions must contain additional stories, not the missing resolution of the purchased campaign.

### Content completion is a sales gate

There are **17 registered campaign missions**, through CH04-M02, in the audited catalog. The remaining eight are not present there. Do not activate an offer claiming a complete 25-mission game today. Finish and validate the story before charging for that promise.

The 60 Operations missions and 120 Skirmish scenarios are planning targets with staged readiness, not launch-ready inventory. Do not print those counts on a purchase page until each included entry passes its release gate. The current Operations plan describes one connected mode; its commercial packaging must be amended explicitly before a customer offer. Preserve all existing IDs and production plans while adding product membership. Internal delivery milestones do not reduce the agreed 120-scenario core scope or turn its unfinished entries into paid extras. Finish the advertised package before selling it as complete.

Campaign Edition can release before the full Operations theater. Do not sell or take payment for the unfinished theater. Its free introduction becomes visible only when all three introductory missions pass acceptance. If only O001 is ready, keep the three-mission offer unpublished rather than advertise two missing missions.

**Ownership rule:** purchased content and its fixes/balance/accessibility updates remain included permanently under the storefront entitlement. New content outside the product's stated scope can be paid; occasional free additions are discretionary. Never charge again to finish a story, run, scenario library or other content already promised or sold. Verify any existing public promises/customer entitlements before changing the current packaging. Do not use “Full Game,” “all future content” or “lifetime all-access” for Campaign Edition.

### Exact proposed initial content membership

| Offer | Proposed content | Count and dependency rule |
|---|---|---|
| Free Campaign | CH01-M01–M05 | Five of the 25 Campaign Edition missions; all five remain free on replay. |
| Free Skirmish | S001 / Desert Base, Base Assault, Ground Maneuver, Field Base | One of the 120 core scenarios, unlimited play with included ARIA. Must match its certified scenario identity, not silently substitute a different prototype. |
| Free Operations | O001 Street Signals, O002 Clinic Supply Route, O003 Courtyard Water Point | Three of the 60 theater missions. Standalone introduction rules and closure are required; see section 7. |
| Campaign Edition | CH01-M01–CH05-M05 plus S001–S120 | 25 campaign missions and 120 total Skirmish scenarios, including the free samples. Five maps × four objectives × three army profiles × two starting packages. These are planned contents, not readiness claims. |
| Operations: Sahrin Theater | O001–O060, all six districts, complete city-run conclusion and practice access | 60 total, including the three free samples. No Campaign Edition dependency and no additional district tolls. |
| Later Skirmish collections | Explicit accepted scenario/map sets beyond S001–S120 | Price only after defining and testing the additional content's value; $3.99–$4.99 is a provisional experiment, not a standing price per scenario. Do not sell difficulty/seed permutations as new missions or charge separately for completing the original 120. |

The [approved Skirmish scope](../Roadmap/Skirmish_Expansion/PLAN.md) explicitly targets 120 accepted scenarios on five maps. This revision removes the earlier, unjustified 12-scenario commercial subset. Preserve all S001–S120 in the core purchase; the remaining 108 are not DLC. Production batches are internal readiness milestones, not additional purchase boundaries. Each entry needs certified runtime, ARIA and performance support before the complete package can be sold. New packs sell additional battle situations, not a stronger version of the same purchased army. Every included scenario supplies all units/roles its rules require, irrespective of ownership of another pack.

The library comprises combinations of five maps, four objectives, three army profiles and two starting packages; it is **120 scenarios, not 120 unique maps**. Each combination must justify itself through distinct tactical decisions. Seeds, difficulty levels and army sizes do not add to that count. The current targets are Standard **12–18 minutes**, War **18–25 minutes** and Large War **20–30 minutes**. These are design targets, not measured playtime or a guaranteed total-hours claim. Do not shorten battles or pad the catalog to support a price. Evaluate actual duration, variety, voluntary replay and production cost when choosing the final price.

## 2. What the audit actually establishes

This is a source/configuration audit plus a review of existing native screenshots and recorded validation evidence. No new Unity session, build, billing transaction, normal-input playthrough, or device acceptance run was performed in this planning task. Older captures demonstrate their recorded candidates, not the entire current working tree.

The [machine-readable mission audit](../AgentReports/2026-09-28_monetization-resource-mission-audit.json) preserves the 17 definition/scenario records and source hashes inspected for this plan. Source pointers below distinguish implemented code from design intention.

| Area | Evidence found | Commercial implication |
|---|---|---|
| Store / Command Exchange | `StoreCommandExchangeV3View` has a static six-category catalog, fixed prices and “72H REMAINING” / weekly text. Purchase is disabled in both wiring and presentation. | Existing shop art and layout can be reused selectively; this is not a functioning revenue system or evidence of actual sales. |
| Reward/payment foundation | Campaign and Operations have real profile settlement paths. No `RewardService` / `RewardConfig` class implementations, platform receipt integration, or base-game entitlement service were found in the searched game scripts; `RewardConfigId` identifiers do exist. | The earlier recommendation overstates how much purchase infrastructure can simply be reused. Reuse settlement patterns and persistence, then implement verified purchase entitlements deliberately. |
| Main-menu balances | Several shell defaults seed `12,450` Credits and `78/100` Command; the prefab builder seeds `187,540` / `2,715`. Operations' presentation reads actual profile values. | A visible balance is not proof of a shared live wallet. Every account surface must use the same authoritative profile read model; remove the capacity/energy implication of `78/100`. |
| Account schema | Profile contains Credits, `commandAuthority`, Rush Tickets, XP, parts, unlocks and cosmetics, but also legacy Materials, Fuel and Intel fields. | Separate account progression, ownership and tactical state before commerce. Preserve old saves during migration. |
| Campaign rewards | All 17 inspected definitions grant XP and Credits. Chapter 1 first clears total **11,200 Credits**; all 17 total **83,700**. Replays give **250–500 Credits**. Current definitions do not grant Materials/Fuel. | Rewards already accumulate; pricing account sinks requires an actual earn/spend model. Do not assume the existing shop preview establishes balance. |
| Dormant reward conflict | Campaign settlement still accepts Materials/Fuel reward kinds and writes them into the persistent profile. | This is an available incompatible path, not evidence that the 17 current missions grant those resources. Reject future account grants of tactical resources and migrate legacy fields. |
| M02 tactical economy | Authored 120 Materials; Barracks 90, rifle squad 20, 10 left. Runtime sets legacy Money to zero for this mission despite serialized `startingCredits: 55000`. | A working local-resource model already exists. Preserve its taught budget and retry behavior. |
| Other tactical Credits | M03, Air Corridor and Steel Push author 50,000 starting Credits. Shared construction can spend Money and Materials. Steel Push's native HUD explicitly displays Materials / Credits / Fuel. | Account Credits and scenario Money are distinct owners with confusing shared terminology. This is not proof that paid Credits currently enter combat, but it must be resolved before an account shop is enabled. |
| Operations | Scenario task forces, strategic AP and metrics, tactical outcomes, persistent reward deltas and checkpoint integration exist. The current O001 report still leaves human/device acceptance pending. | Sell access to a complete playable mode; do not sell AP, recovery supplies, trust or readiness. Existing persistence work is valuable, but not a 60-mission readiness certificate. |
| Skirmish | Prototypes and expanded candidates exist. Manifest rows S002–S004 say `Playable` but explicitly retain ARIA/device or air-cycle gates. | `Playable` is not a paid-release certificate. Scenario variants, seed changes and difficulty settings must not inflate the advertised unique-content count. |
| ARIA | Local decision/planning and synthetic-touch paths exist. Chapter 1 has recorded bilingual baseline wins; newer O001 and Steel Push reports have additional wins and limitations. | Keep ARIA included. “Every mission” is a release coverage requirement, not something this audit independently proved across all configurations. |
| Support | Approved Smoke, Strike, Paratroopers and Supply implementation is actively changing the working tree. | Preserve the approved earned/optional schedule. Components or CSV policies alone do not certify effects or player readiness. |

### Native presentation samples reviewed

- [M01 gameplay handoff](../AgentReports/M01StoryReplay/Evidence/english-gameplay-handoff.png): strong recognizable ARIA and colored command controls; a resource strip remains visible even in an economy-disabled tutorial. It must not suggest purchasable entry costs or energy.
- [O001 dashboard](../AgentReports/Operations/O001_PLAYER_EXPERIENCE_20260924/evidence/final-first-scan-04/O001OperationsDashboard.png): Credits/Command at the top; district state and a one-AP deployment below. These are different kinds of values and need clear explanations.
- [O001 active scan](../AgentReports/Operations/O001_PLAYER_EXPERIENCE_20260924/evidence/final-first-scan-04/O001ActiveScan.png): scenario Materials, visible objectives, and Stop ARIA. Preserve these player-facing priorities.
- [Steel Push HUD](../AgentReports/CH04M02SteelPush/VisualReview/en-hud-aria-5.png): 200/200 Materials, 50K Credits and 120 Fuel, alongside ARIA. This directly corroborates the remaining match-Credits terminology conflict.

These are existing native captures, not newly generated mockups. Any substantial UI redesign resulting from this plan must follow the repository's ImageGen-with-actual-references and owner visual-review process before implementation.

## 3. Resource policy: menus, progression and matches

### Recommended account economy

For launch, use **Credits as the sole active spendable account currency**, earned through play and never sold. Use it for a finite, clearly priced collection of account cosmetics. Keep XP and stars as progression/mastery records. Required gameplay tools come from authored mission access and fixed milestones, not wallet spending.

This deliberately simplifies the earlier proposal to repurpose and potentially sell Command. Two currencies are not necessary for a one-time-purchase game with no consumable store. The existing two-counter visual contract must be amended rather than leaving a decorative or nonfunctional currency in the header.

Do not activate stat-changing account upgrades in certified Campaign/Operations/Skirmish baselines simply to create a Credit sink. Skirmish research paid with tactical Materials remains a match decision. A future optional account-sidegrade mode needs explicit loadout/balance rules and is not part of this launch plan.

| Value / inventory | Target ownership and use | Monetization rule | Migration / UI work |
|---|---|---|---|
| Credits | Persistent, earned account collection currency; optional free cosmetic unlocks. | Never bought, traded for cash, or spent to enter/retry a mission. | Bind real totals everywhere. Explain purpose after M01, when an actual sink exists. Remove purchase plus-buttons. |
| Command / `commandAuthority` | Retire as an active launch wallet; no new purchases or grants. | No paid Command packs. | Preserve legacy balance/history. Publish a migration policy before release; do not erase value or silently convert it using an invented exchange rate. See below. |
| Commander XP / level | Persistent recognition and profile milestones. | No XP boosters or paid level skips. | Required story access must not depend on XP beyond ordinary prior-mission completion. |
| Campaign stars | Best earned mastery record per mission. | Never sold; one-star completion advances the owned campaign. | Higher stars can award cosmetic recognition, not required story tools or ending access. |
| Protocol Fragments / story archive | Guaranteed base-story evidence on completion. | No fragment packs, paid archive recovery, or true-ending bundle. | Trial includes its complete earned story and debriefs. Paid chapters obey the same completion rule. |
| Unit/building/ability unlocks | Fixed milestones and scenario task-force access. | No paid early unlocks, roster rentals, charge packs or required preparation bundles. | Distinguish permanent collection ownership from temporary scenario access. Purchase does not auto-complete progression. |
| Blueprint Parts / gear | Legacy earned records; not a launch monetization system. | No cases, random rolls, purchasable upgrades or grind relief. | Preserve existing records. Replace future incomplete-part teasers with a useful fixed unlock or explicit cosmetic reward when its consumer is ready. Never silently delete an owned unit/upgrade. |
| Rush Tickets | No active paid or persistent accelerator economy. | No ticket sales, ad refills, premium skips or account-to-match projection. | Keep legacy records for migration. Scenario-local allowances, if retained, are tactical resources with their own clear name and fixed budget. |
| Cosmetics | Persistent ownership/equipment. Free earned collection at launch. | Later direct-price style packs; no stats or improved tactical readability for buyers. | Preview actual implemented appearance; ensure ownership/restore/equip work before listing. |
| Base/expansion access | Permanent content entitlement, separate from a wallet. | Direct storefront purchase; one-time, non-consumable. | Never represent ownership as Credits, stars, a mission-clear bit or a spendable item. |

**Legacy policy:** no live receipts or external customer sales were established by this audit. Inspect actual platform product history before assuming nobody paid. Back up/version the save, retain legacy fields read-only, preserve earned ownership, and make migration idempotent. If only internal/test balances exist, retain them in the archived save and initialize the new documented economy once. If real customer balances or products exist, map their verified entitlements and value explicitly before retiring a product; do not ship a reset or unpriced conversion. This is a migration investigation, not a reason to keep selling the old catalog.

### Match economy

| Resource / constraint | Keep? | Rule |
|---|---|---|
| Materials | Yes | Scenario/local production, construction, recruitment, repairs and in-match research. Same authoritative amount for HUD, affordability, spend and rollback. |
| Oil | Yes where the mission uses logistics | Physical extraction/hauling and allocation to refining/fabrication; no account carry-in. |
| Fuel | Yes | Actual usable delivered reserve, movement/aircraft costs and protected civilian allocation where authored. No paid refill. |
| Scenario Money currently labeled Credits | Migrate out of the target model | Replace dual Money+Materials costs with authored Materials costs and revalidate budgets; do not just delete costs. Until migration passes, keep it strictly scenario-owned and never connect it to the account wallet. |
| Exchange queues / production time | Yes when they create tactical decisions | Simulation-time logistics and opportunity costs are legitimate gameplay. No paid acceleration. Pausing, explanation and safe cancellation should follow gameplay rules. |
| Support charges / cooldown / Fuel cost | Yes, per certified mission policy | Fixed tactical allowance; unlocked by play or scenario task-force grant. No permanent paid charges. Supply's Materials crate is an in-match effect, never a store product. |
| Scan charges / reveal | Yes, where authored | Information is part of mission play. No bought confirmation of targets, hidden-objective reveals, or premium ARIA perception. |
| Infantry count / Supply capacity | Yes | Army capacity or count, not wallet money. Explain recruitment limits on the affected unit card. Never sell capacity. |
| Civilian risk, readiness, district security/trust/infrastructure/intel/heat | Yes | Consequences and strategic state, never currencies or products. |
| Operations AP | Yes | A per-in-game-day planning budget. Player advances the day; no real-world recharge, paid refill, offline decay or monetized delay. |

**A tactical shortage should lead to a gameplay decision:** defend a route, conserve Fuel, build/refine, use an authored alternative, withdraw, retry, or ask ARIA for help. It must not lead to a purchase dialog.

### Reward and sink tuning

Keep existing first-clear/replay grants during technical migration so resource ownership changes can be tested separately from balance. The current 11,200 Chapter 1 Credits are real configured grants, not a price target. A provisional free cosmetic ladder of 2,000 / 5,000 / 10,000 Credits would let a first-clear-only Chapter 1 player buy one meaningful item; these are test values, not implemented prices.

Before final tuning, produce a full 25-mission earn/spend sheet. It must show starting balance, first clear, optional mastery, replay reward, cumulative balance and cosmetic unlocks. A one-clear-per-mission player should obtain meaningful collection rewards without replay farming. Mission-critical power is already provided, so replay income cannot become mandatory.

Operations currently gives 100 first-clear Credits versus Campaign grants of 1,200–9,000; repeating a victory gives 20, with a 60 cap per in-game day. Check reward rates per active minute and reset semantics before sharing cosmetic prices across modes. Do not add a real-world daily cap to force return visits. Optional Skirmish collection rewards should be modest and tied to completed real matches, with no unattended multi-match farming. Numerical reward changes require migration and abuse checks, not an arbitrary global multiplier.

## 4. ARIA: include the core feature, charge for content

**Recommendation: leave ARIA free to use inside accessible content.** A trial player can use her throughout Chapter 1, the Skirmish sample and O001–O003. Owners receive the same ARIA support throughout each purchased Campaign Edition, Operations theater or content pack. There is no separate “ARIA Premium” unlock.

This preserves the game's command-assistant identity, lets players experience the differentiating feature before purchasing, and avoids making accessibility or frustration the purchase trigger. The inspected architecture uses local planners and pre-authored voice/text; there is no demonstrated per-mission cloud-model charge that requires metering current ARIA use. Development, voice production, QA and device processing still cost money and belong in the game's production budget.

The tradeoff is real: some players will watch most of the trial and some will watch the purchased story. That is an allowed way to enjoy this product. Measure satisfaction, return to manual control and purchasing behavior; do not deliberately degrade ARIA to protect a resource economy that is no longer being sold.

| Capability | Free samples | Owner of the relevant package | Restriction |
|---|---|---|---|
| Objectives, hints, field guide, camera Show Me | Included | Included | Never requires payment; camera guidance must not issue troop orders. |
| Watch ARIA play / Play / Stop | Included on every certified accessible mission | Included on every certified owned mission | Exact mission/ruleset capability and normal gameplay rules apply. Unsupported content is a readiness gap, not an upsell. |
| Take control / pause / accessibility | Always included | Always included | Immediate safe handback; no payment or extra confirmation to stop. |
| Paid chapter/theater/scenario demonstration | Unavailable until that content is owned | Included | ARIA obeys the same content gate as manual play; no unpaid next-chapter or paid-mode launch. |
| Optional future ARIA appearance/voice style | No launch product | Possible later cosmetic | Same tactical information, clarity, language coverage and ability. Default voice and all critical/accessibility speech remain included. |
| Future authored strategy lessons / cloud replay analysis | Deferred | Separate future product decision | Must add new value, not remove existing coaching or Watch. Only evaluate a paid service if new recurring costs and demand are proven. |

### Progression, fairness and stopping rules

1. A legitimate ARIA-assisted victory grants the same story progression, first-clear reward and earned stars as the same world outcome under manual control. No hidden reward haircut, extra grind or “pay to remove assisted” product.
2. Keep a non-punitive assisted-run marker and aggregate active-control duration for QA/analytics. If competitive leaderboards are ever added, define separate comparable categories before launch; ordinary hints do not automatically make a run an autoplay run.
3. Tactical Watch consent covers the current match only; it does not authorize next-mission/replay loops, End Day or permanent Operations decisions. Separately opted-in Operations Run Watch follows its existing explicit strategic scope. Neither consent permits purchases or account spending. Preserve the distinction in both free introduction and owned theater.
4. Stop cancels pending synthetic input, not previously accepted troop orders. A save/load restores manual control and requires fresh consent.
5. ARIA uses the same displayed information, costs, cooldowns and outcome rules as the player. No hidden enemy knowledge, extra resources or guaranteed wins.
6. Retain the approved Support-specific confirmation rules. Watch permission does not silently supersede fresh consent for the exact bounded Support action where that contract requires it.
7. A failure can offer retry, tactical explanation and ARIA assistance at no charge. An irrecoverable run must be reported honestly, not turned into a paid rescue offer.

### Trial design with ARIA

Do not disable Watch in M05 or ration it to the first mission. Make the choice clear: “Play with guidance” or “Watch ARIA play; take over anytime.” These are ordinary gameplay choices, not separate difficulty or purchase tiers. The complete Chapter 1 debrief still plays after an assisted victory. The purchase invitation appears afterward under the same rules as manual play.

Track manual, mixed and predominantly ARIA-led trial cohorts. Compare their comprehension, enjoyment, completion, purchase and refund behavior. Cohort correlation is not proof that ARIA caused conversion changes: less experienced players may self-select assistance. If watched sessions feel passive, improve explanations and optional handover invitations; do not charge for the remedy. Keep invitations dismissible and never repeatedly interrupt an explicitly chosen Watch session.

## 5. Purchase catalog and menu journey

### Product catalog

Product IDs below are proposed identifiers, not products registered with a store.

| Product | Reference price | Contents / access | Timing |
|---|---:|---|---|
| `warline.campaign.shattered_relay` | $9.99 hypothesis; compare $12.99 / $14.99 | Campaign Edition: complete 25-mission story + all 120 core Skirmish scenarios, S001–S120. ARIA included; progression prerequisites still apply. Does not include the full Operations theater. | Only after all included content and commerce gates. |
| `warline.operations.sahrin` | $7.99 test anchor | Complete six-district O001–O060 theater, replay/practice and ARIA. Independent purchase; no Campaign Edition prerequisite. | Later, only when the whole connected theater is accepted. |
| `warline.skirmish.pack_01` | $3.99–$4.99 provisional test | A published set of additional battle situations beyond S001–S120; all required roster and ARIA included. | After the original 120 are delivered and additional content/value is validated; no charge for seed/difficulty alone. |
| `warline.cosmetic.commander_style_01` | $2.99 test | One coherent, previewable frame/banner/card-art set, no stats, no exclusive tactical information. | Optional post-launch pilot; one pack, not a large rotating catalog. |
| `warline.expansion.campaign_01` | $5.99 reference; test $4.99–$7.99 | A complete additional campaign with its own resolved arc, certified missions, localization and ARIA coverage. | After the original campaign is delivered; show exact finished contents and any dependency before sale. |
| Supporter extras | Deferred | Optional art/soundtrack/cosmetics only if rights and delivery are verified. | Do not launch a $15–$20 pack before core value and demand are proven. |

No launch product sells Credits, Command, Rush Tickets, parts, units, supplies, star completion, ARIA minutes, mission retries or ending access. No recurring entitlement is needed for the proposed catalog. Expansion price depends on measured play value and production costs, not a promise to deliver on a fixed calendar.

Later paid cosmetics should be visibly different from the free earned collection and preview their exact contents. Preserve faction identity, selection/range/target cues, subtitles, color accessibility and unit silhouettes. Defer HUD themes and strike-VFX variants until they can pass readability/performance tests; a skin is not safe merely because it has no numeric stat modifier.

### Surface-by-surface changes

| Surface | Recommended behavior |
|---|---|
| Store listing | Explain the free samples and separate permanent Campaign Edition / Operations theater purchases. Show exact included content and only release-certified modes. Never imply $9.99 buys everything. |
| Cold open / identity / M01 / first debrief | No purchase interruption. Free portraits, subtitles, settings and ARIA remain available. |
| First command-base reveal | Continue Campaign dominates. Credits are shown only from the live profile and with a meaningful earned use. No store plus-buttons, fake balances, recharge bar, season banner or expiring starter pack. |
| Campaign selection | Chapter 1 free; Chapters 2–5 marked “Included with Campaign Edition” with the actual local price accessible on demand. Distinguish content ownership from previous-mission progression. |
| M05 victory and chapter debrief | Settle rewards and finish the narrative first. Then offer View Campaign Edition, Replay Chapter 1, Free modes, and Return. One deliberate offer; no repeated purchase modal on every return. Free modes leads to the Skirmish sample and accepted Operations introduction. |
| Purchase detail | List the exact package's content, permanent one-time nature, local price, separate-package exclusions, restore and purchase status. No countdown or invented discount. |
| Existing SCN-14 | Reuse typography, portrait language and suitable assets; simplify to finished Game Content and optional later Cosmetics. Remove Starter/Resources/Armory/Operations consumable categories; a theater product is content, not an Operation supply category. |
| Armory / Loadout | Inspect earned roster, scenario restrictions and fixed unlock milestones. Remove links to part cases or “buy readiness.” Required mission units are supplied without account spending. |
| Profile / rewards | Show earned milestones, actual collection ownership, XP and stars. Replace season/level-purchase panels with persistent achievements or collection progress. |
| Operations dashboard | Show district facts, AP and outcomes with End Day explanation. Repair/aid/intel actions remain gameplay choices; remove monetized Black Market routes. |
| Match HUD / Build / Scan / Support / Resource Exchange | Never open commerce. Show only scenario-relevant tactical resources and actionable shortage reasons. Retain purpose/objectives and ARIA Play/Stop. |
| Defeat / partial success | Explain the actual result and offer retry, help, withdrawal or return. No rescue purchase, refill, ad revive or reward multiplier. |
| Settings | Restore/refresh purchases and clear status/help; progress reset must not erase platform ownership. |

Concepts above specify behavior, not an approved new visual layout. If implementation materially changes an existing screen, obtain review of ImageGen mockups using the actual Campaign references before building it. Routine data binding fixes do not require a new design approval.

### Access logic

Keep three independent checks: **content released**, **entitlement owned**, and **progression prerequisite met**. A saved `available=true` mission flag is not proof of purchase; paying is not proof of mission completion. Use this rule in the chapter card, Deploy, Continue, replay, resume and all mode launch paths, not only the store screen.

An early purchaser may buy voluntarily from the menu after the uninterrupted opening, but Campaign still follows its normal sequence. Free mode samples and owned Operations/Skirmish content become available after Chapter 1 or an explicit equivalent controls tutorial; players interested only in those modes must not have to buy or grind Campaign. The free and purchased versions of the same scenario share tactical difficulty and rules. Operations demo completion does not require Campaign Edition ownership.

## 6. Ads, paid shortcuts and timers

**Choose no ads at all and no paid time skips.** “Premium” means a permanent purchase of a clearly specified content package, not a subscription, faster simulation, better ARIA or payment to remove ads from a deliberately degraded experience.

| Model considered | Decision | Reason for this game |
|---|---|---|
| Free trial + permanent content unlock, no ads | Recommended | Gives players a real test of touch control and ARIA, then sells a defined authored game. |
| Rewarded ad to skip construction, transport or exchange | Reject | The skip changes tactical timing and can change victory. It adds an external interruption to urgent world decisions. |
| Pay to skip the same tactical wait | Reject | A real strategic constraint becomes purchasable advantage; manual and ARIA balance would depend on spending. |
| Ads to refill AP, retries or Support | Reject | Converts freely available play and assistance into a rationed resource; contradicts scenario task forces and player-controlled Operations days. |
| Interstitial ads between missions plus an ad-removal purchase | Reject | Breaks chapter/debrief continuity and introduces an extra purchase before the actual game offer is understood. |
| Optional ad for cosmetics | Defer indefinitely at launch | Less damaging to combat, but adds ad SDK, consent, support and product complexity to an otherwise premium proposition. No forecast depends on this revenue. |
| Upfront paid download | Possible later/platform alternative | Clear ownership, but removes the useful mobile control/device trial. PC can use a conventional paid game plus demo; pricing needs its own evidence. |
| Cosmetic-only F2P with the entire campaign free | Not the recommended business | Compatible with fair gameplay, but demand, audience size and repeat content costs are unproven. No claim is made that ethical F2P is impossible. |
| Season pass / subscription | No launch product | Requires an additional ongoing value/content commitment; current mode readiness and retention do not establish that business. |

Three different kinds of time need different treatment:

1. **Tactical time:** recruitment, construction, scanning, flight, cooldowns, convoy movement and exchange queues can make choices meaningful. Preserve justified simulation-time constraints, with clear progress and cancellation rules. Shorten empty waits when playtests show they are boring; do not sell acceleration.
2. **Progression waiting:** no hours-long account build timers, lives, energy or real-world AP recharge. Advance Operations days when the player chooses, with authored strategic consequences rather than a real-time wait.
3. **Technical waiting:** loading, downloads and purchasing-network delays need performance work and honest progress/retry feedback. Never monetize skipping loading or make free-player loads artificially slower.

An optional future game-speed/accessibility control should be available equally within owned content and separately validated for simulation/ARIA correctness. It is not a consumable shortcut and is outside the current implementation scope. Campaign stars remain based on the published simulation rules.

## 7. Mission-by-mission changes

**Most missions need no new objectives for monetization.** The necessary changes are resource consistency, honest rewards, the Chapter 1 boundary, and preserving a complete purchased story. Do not make the last free mission harder, lengthen tutorials, or place an artificial power deficit at the first paid mission.

“Registered” below means present in the inspected 17-entry definition/scenario catalog, not newly playtested here. Later missions are designed in the high-level catalog but absent from that runtime catalog. Chapter 1 and Steel Push readiness reports provide stronger, still candidate-specific evidence; no blanket acceptance is inferred for Chapters 2–3.

### Chapter 1 — preserve the free vertical slice

| Mission | Current evidence / role | Required adjustment | Preserve |
|---|---|---|---|
| CH01-M01 First Contact | Registered; core command tutorial; 1,200 Credits + 260 XP first clear. Economy disabled. | Bind account rewards honestly; keep purchase offers out of opening/debrief; avoid suggesting inactive resource counters are entry requirements. Include ARIA at the first useful moment. | Select/move/attack lesson, immediate retry, story clue and free identity/accessibility. No extra lesson explaining the store. |
| CH01-M02 Establish The Base | Registered; Materials-only budget 120 → 30 → 10 after Barracks/squad; 1,500 Credits + 320 XP and Barracks unlock. | Keep serialized legacy Credits from becoming a live cost again. Validate cancel/refund, failed placement, retry and zero account-wallet dependence. | Current 90/20 costs until balance evidence justifies a change. Do not introduce paid builders, queue slots or a scarcity lesson. |
| CH01-M03 Radar Warning | Registered; preparation/defense, 100 Materials and 50,000 legacy scenario Credits; two Radar Ping charges; 2,000 Credits + 400 XP and fixed unlocks. | Audit all build/recruit affordability and migrate remaining Money costs to Materials. Check shortage recovery without tickets. Reconfirm the revised wait pacing with unfamiliar players. | Warning → preparation → defense rhythm, fixed scan allowance, free ARIA and normal retry. |
| CH01-M04 Airlift | Registered; APC/helicopter extraction, economy disabled, self-supplied transport; 2,500 Credits + 500 XP and named unlocks. | Check that ordinary transport is never described as a purchasable Support charge. Clarify any reward whose historical design name differs from its implemented consumer. | Full transport lesson and authored extraction failure/recovery. No Fuel sale, ad rescue or paid passenger capacity. |
| CH01-M05 Breach Assault | Registered; combat/breach/archive finale, economy disabled; 4,000 Credits + 750 XP, Ghillie unlock and 35 APC parts. | Complete narrative and settlement before the offer. Replace/resolve the incomplete-part reward if it has no usable progression consumer; current code targets `Unit_Veh_APC_Heavy`, while older design calls it an APC armor upgrade. Gate CH02 deployment by entitlement separately from earned availability. | Current mission difficulty, usable breach route, clue, rewards and replay access. ARIA remains free for the whole finale. |

Chapter 1 should show enough decisions to justify buying; it does not need jets, Supply Drop or a resource shop. Do not move the approved Chapter 4 Support introduction into the trial. Measure real first-session and five-mission completion times; a 6–10 minute session design target is not the measured length of the whole free chapter.

### Chapters 2–3 — purchased story, no preparation purchases

The current Chapter 2 scenario restrictions disable free building/production/economy; several missions use authored protected-route, reserve and delivery fallbacks. Do not advertise a fully player-built logistics network simply because the high-level design describes one.

| Mission | Current state | Adjustment / validation under this plan |
|---|---|---|
| CH02-M01 Gridlock | Registered; authored route-clearance/engineer objective. | First paid mission must launch from a one-star M05 clear with no wallet threshold, account upgrade or parts grind. Verify all needed engineer/route interactions are supplied. Avoid a new currency tutorial immediately after purchase. |
| CH02-M02 Supply Line | Registered; authored Fuel-chain/reserve protection. | Preserve physically meaningful Fuel and protected reserves. Validate zero-wallet completion and retry. No bought tankers, recovery Fuel or civilian-service refill. Keep the ready fallback if free network construction remains unqualified. |
| CH02-M03 Market Lifeline | Registered; supply/manifest/escort contract, economy disabled in scenario. | Use the actual authored delivery path. Do not add a cash-backed exchange to match the “Market” name. If tactical Resource Exchange is later enabled, certify local inputs, losses, queues and cancellation with no persistent ticket access. |
| CH02-M04 Power Relay | Registered; repair/protection contract. | Required repair/evacuation provisions must be scenario-owned. No paid repair supplies, purchased trust or premium consequence reversal. Keep story fragment guaranteed on success. |
| CH02-M05 Route Reopened | Registered; chapter logistics/route finale. | Validate a continuous one-star Chapter 2 run with only first-clear progression. All legitimate logistics alternatives must be funded by the scenario; no required store convoy or parts upgrade. |
| CH03-M01 Signal Trace | Registered; confirmation/recon mission. | Scan/reveal must be available without paid dossier or ARIA tier. Required information is earned in the mission; preserve civilian distinction. |
| CH03-M02 Safehouse Sweep | Registered; verified assault/evidence. | Required target verification and squad roles must be supplied. Do not sell Drone Scan parts as a substitute for the mission's verification route. |
| CH03-M03 False Front | Registered; changing information/protection. | Clear, free guidance for the objective change. No premium hints that disclose the “correct” target or buy away a bad decision. |
| CH03-M04 Evidence Chain | Registered; transport/evidence extraction. | Preserve included transport and necessary cargo capacity. Required witness/archive delivery cannot depend on a bought upgrade, insurance or revive. |
| CH03-M05 Network Break | Registered; precision/breach/archive finale. | Complete its revelation and unlock the next owned chapter on ordinary completion. No additional chapter charge, fragment grind or paid Breach kit. |

### Chapters 4–5 — preserve tactical scarcity and approved Support progression

The [approved Support rollout](../Roadmap/Support/PLAN.md) is the current schedule: Smoke after CH04-M02, Strike after CH04-M03, Paratroopers after CH04-M04, Supply after CH05-M02; first use is the next mission. All are optional for success and stars. Do not revive earlier conflicting ability schedules.

| Mission | Current state | Adjustment / validation under this plan |
|---|---|---|
| CH04-M01 Air Corridor | Registered; defense preparation with 100 Materials and 50,000 scenario Credits. | Migrate construction/recruit costs to Materials with a validated anti-air budget. Required radar/G2A comes from the scenario; no paid air defense or warning time. |
| CH04-M02 Steel Push | Registered; latest report has EN/FA complete normal-input automated Editor wins. 200 Materials, 50,000 scenario Credits, 120 military Fuel; 40 civilian barrels protected. | Replace match-Credits spending/presentation without damaging the finite-Fuel rules. Re-run real combat after budget changes. Add the earned Smoke grant only with the certified Support integration; never sell it. |
| CH04-M03 Split Front | Designed; absent from audited runtime catalog. | Deliver manual G2G/force-split mission and optional first Smoke lesson. No paid strike solution to difficult targeting. Award Strike after completion when certified. |
| CH04-M04 Grounded Signal | Designed; absent from audited runtime catalog. | Preserve ordinary transport, personnel/cargo delivery and optional first Strike use. Award Paratroopers afterward. No charge for a safe insertion route or necessary specialist transport. |
| CH04-M05 Armor Break | Designed; absent from audited runtime catalog. | Combine established systems and optional Paratroopers. Follow approved ARIA Support consent; no premium finale strike or new paid ability. |
| CH05-M01 Citywide Alert | Designed; absent from audited runtime catalog. | Multi-front battle with all required forces supplied; free ARIA and exact bounded-action consent. Do not sell offscreen-threat warnings or a second command slot. |
| CH05-M02 Trust Under Fire | Designed; absent from audited runtime catalog. | Civilian/route objectives remain gameplay consequences, never paid trust or evacuation. Award Supply afterward, before its teaching mission. |
| CH05-M03 Network Collapse | Designed; absent from audited runtime catalog. | Optional first Supply use, with the approved provisional 40-Materials tactical crate subject to balance validation. No ad or currency purchase for replacement crates; required objectives work without Support. |
| CH05-M04 Last Corridor | Designed; absent from audited runtime catalog. | Sustain meaningful transport/Fuel/logistics decisions. No paid deadline extension, convoy insurance or priority queue. Only previously validated systems may be required. |
| CH05-M05 Command Node | Designed; absent from audited runtime catalog. | Base purchase contains the complete resolution and canonical ending at ordinary completion. No new ability, missing final chapter, premium ending or paid replay. |

### Operations and Skirmish adjustments

- **Why the entire city should be one theater purchase:** current strategic rules require all six local finales and two consecutive committed stable End Days across every district. The runtime `EvaluateStability` checks every district. Selling access to individual districts inside that same run would make its conclusion require multiple purchases. Sell the complete O001–O060 theater as one package; future theaters must have their own complete conclusions. This preserves the existing 60-mission production scope and avoids redesigning a global victory condition around district microtransactions.
- **Free Operations scope:** O001 recon, O002 clinic escort and O003 water repair make a representative introduction. Give the intro an explicit scope/run identity, available-mission set and local-services conclusion after successful delivery of those three missions. Its recap should say the introductory local operation is complete, not that the six-district city has been won. Do not apply inaccessible-district pressure, incidents or completion requirements to this scope. Free replays remain available after the recap with ARIA and no AP purchase.
- **Intro progression/recovery:** retain real mission failure and partial outcomes, with free retries/reoffers and an explained day/AP cycle. Seed only the playable introductory district and its supported consequences. Avoid a deadline forcing the user to buy the theater to prevent harm. The three victories establish the intro completion milestone; no paid O004 or O010 is required for that milestone. This scope is a new required mode adjustment, not something the current six-district runtime already supplies.
- **Upgrade from intro to full theater:** preserve canonical O001–O003 completion/collection records and exactly-once account rewards. Start a separately versioned full-city run under its normal initial-state rules, explaining what transfers. Import any accepted mission milestone only through an explicitly authored migration; never pretend intro-only district state is a full-city checkpoint. Prefer archived intro + fresh full-city run with those missions available for optional replay over an opaque partial-state merge. Never overwrite an existing full run without its normal confirmation.
- **Purchase placement for Operations:** after the intro recap, offer the complete theater only when it is actually released, alongside Replay introduction and Return. When it is unfinished, there is no purchasable theater or missing-content promise. Manual/ARIA completion, partial results and failure use the same access rules. A Campaign Edition owner still sees the theater as a separate optional product, disclosed before buying Campaign Edition.
- **Operations:** keep scenario-provided task forces independent of Campaign collection ownership. AP is a strategic choice budget replenished through the authored day cycle, not a monetization meter. Mission loss can change district state but cannot create debt, a paid repair requirement or an unrecoverable run solely because no money was spent. Preserve practice/retry policies and explain whether an attempt consumes AP; technical launch/save failure must not double-charge AP or rewards.
- **O001 specifically:** finish the human-controlled full-win and device/usability gates recorded as pending. Preserve its visible objectives, free ARIA Play/Stop and camera-only guidance. A paid entitlement does not make the observed mouse-run Partial Success evidence into a full win.
- **Operations growth:** use the existing 3 → 12 → 30 → 60 production gates as internal milestones. Publish the separately completed free introduction first if useful, and sell the full theater only at complete acceptance. The 12/30 intermediate milestones are not paid chapters of an unfinished city run.
- **Skirmish:** accepted scenario determines armies, starting stocks, capacity, legal research and Support for both sides. No account-power advantage, paid faction strength, premium AI weakness, paid Supply cap or Campaign grind before trying a scenario's roster.
- **Demo:** start with one certified scenario; accepted seed/difficulty options can provide replay value without promising another authored mission. Validate whether the sample communicates the full mode's value. Campaign Edition includes all 120 core scenarios, including that free sample; later packs grant additional listed scenarios beyond S001–S120. Buying never improves unit stats in the same free battle.
- **120-scenario roadmap:** preserve every current ID, the five-map target and publication gates. All S001–S120 belong to the core product; do not carve unfinished rows out as paid DLC. Classify incomplete air-cycle, ARIA and performance evidence honestly. Use production batches to prove reusable systems, then certify every included scenario. A catalog row or trivial permutation is not evidence of a distinct, playable battle. Advertise and sell the complete package only after its content gates pass.

These changes mean the answer to “do current missions need adjustment?” is **yes, but chiefly at the mode/progression boundary**: O001–O003 need a separate, finishable free-intro scope; their individual tactical rules should remain intact. The full city keeps its existing six-district conclusion. Chapter 1 needs the transparent purchase transition, and the affected tactical economies need the resource migrations described above.

## 8. Commerce, persistence and offline behavior

### Minimum technical design

Create a data-driven product/entitlement boundary outside mission simulation. Proposed responsibilities are product catalog, platform billing adapter, verified entitlement repository, content-access evaluator and ownership presentation. These are responsibilities, not assertions that classes with these names already exist. Reuse the existing atomic profile commit and duplicate-settlement patterns; do not force a content license through a currency reward enum.

Store the platform/product identity, verified transaction reference, entitlement state and necessary verification metadata. Do not treat a writable local `owned=true` field or a developer fixture as proof of purchase. Protect release builds from test entitlement grants. Recover from a crash between payment, persistence and platform acknowledgement without charging again or losing access.

Use native Apple/Google billing for the initial store distribution plan. Both platforms document digital-content billing requirements and regional exceptions; the launch plan does not need a custom web checkout. Recheck the selected regions and program terms when publishing. [Apple guidelines](https://developer.apple.com/app-store/review/guidelines/), [Google Payments policy](https://support.google.com/googleplay/android-developer/answer/9858738?hl=en).

Campaign Edition, the complete Operations theater and later permanent content packs are non-consumable: purchased once and not depleted. Maintain separate entitlements and content membership for each; never infer theater access from Campaign ownership. Apple documents this product type and restoration expectations. [Apple purchase types](https://developer.apple.com/help/app-store-connect/reference/in-app-purchases-and-subscriptions/in-app-purchase-types/).

### Required purchase states

| State | Player experience / access |
|---|---|
| Trial / package not owned | Chapter 1, S001 and the accepted O001–O003 intro work offline when installed; optional purchase entry remains outside protected story flow. |
| Product information unavailable | Honest connection/store status, retry/restore path; no invented local price or apparently successful purchase. |
| Purchase pending / parental approval | Clearly pending; no duplicate tap purchase. Do not grant from an unverified pending response. |
| Verified purchase | Persist ownership, unlock the relevant content gate, acknowledge/finish as required, then resume the player's intended menu action. No automatic mission launch. |
| Purchase cancelled / failed | Return to the same safe menu state; preserve progress and show a useful reason. |
| Restore / reinstall / device change | Recover ownership using the same storefront account. Content ownership and save recovery are different operations; do not promise cross-platform saves or cross-buy without implementing them. |
| Offline known owner | Honor each previously verified local package entitlement for installed content; no daily online check or subscription-style expiry. Failed connectivity alone does not revoke ownership. |
| Confirmed refund / revocation | Reconcile at a safe boundary; stop future paid launches while preserving progress and trial access. Do not kill an active match or delete the campaign save. |
| Owned content not downloaded | Show required size, download/retry status and storage error. Purchase ownership survives missing content. |

For Google, validate a completed purchase and acknowledge a non-consumable; pending purchases must not be treated as completed. Google's current lifecycle documentation states that unacknowledged purchases are automatically refunded after three days. Make acknowledgement durable/retryable and test interrupted transactions. [Google one-time lifecycle](https://developer.android.com/google/play/billing/lifecycle/one-time).

Restore is present from the first paid release. Price labels come from the current platform product metadata. A profile reset must not require buying again. Do not promise iOS-to-Android or Steam-to-mobile ownership transfer under a single purchase; this plan grants ownership within the supported storefront/account rules.

### Entitlement and simulation separation

```text
Platform transaction -> verified permanent content ownership -> launch eligibility
Mission completion -> atomic earned rewards/progression -> profile presentation
Scenario setup -> match Materials/Oil/Fuel/forces -> tactical spending -> mission outcome
```

The first path never deposits match resources; the third never queries real-money balance or payment history. ARIA checks current mission capability and access, not a paid assistant tier. No store callback may mutate an active combat outcome.

## 9. Pricing, distribution and commercial validation

All numbers in this section are planning scenarios, not market benchmarks, measured conversion or a revenue forecast. The project has not supplied acquisition costs, refund rates, storefront analytics or a production budget in this task.

Keep $9.99 as the initial Campaign Edition willingness-to-pay hypothesis for the complete 25-mission story, all 120 core Skirmish scenarios and included ARIA. Compare $12.99 and $14.99 without changing package membership between price cohorts. The larger scope warrants checking economics; it does not by itself prove a higher price will sell. Test the later complete Operations theater separately around $7.99, including $9.99 if measured depth supports it; do not set its price solely by raw mission count. Do not infer willingness to pay from low-APM positioning alone. First test whether new players understand and enjoy each free sample, then test price with real purchase behavior where store tooling and a stable release candidate permit it.

Apple's Small Business Program offers 15% commission to eligible enrolled developers. Google fees depend on market, install cohort and program; its current September 2026 page describes changed EEA/UK/US terms as well as remaining-market rules. Therefore **15% and 30% below are sensitivity assumptions, not a universal platform fee**. Use the actual launch-market blend in the business sheet. [Apple program](https://developer.apple.com/app-store/small-business-program/), [Google fees](https://support.google.com/googleplay/android-developer/answer/112622?hl=en).

### Campaign Edition proceeds per install at $9.99

Before taxes, refunds, regional price differences, operating costs or acquisition:

| All-install purchase conversion | Proceeds/install at 15% fee | Proceeds/install at 30% fee | Proceeds per 100,000 installs at 15% fee |
|---:|---:|---:|---:|
| 1% | $0.085 | $0.070 | $8,492 |
| 3% | $0.255 | $0.210 | $25,475 |
| 5% | $0.425 | $0.350 | $42,458 |
| 10% | $0.849 | $0.699 | $84,915 |

Use:

`net unlock contribution/install = all-install conversion × realized net receipts/purchase − support/backend cost/install`

`break-even paid installs = remaining attributable production/fixed cost ÷ (net contribution/install − paid acquisition cost/install)`

If the denominator is zero or negative, paid acquisition does not recover fixed cost at that cohort's economics. Do not scale it on the assumption that future cosmetics will rescue it. For scale intuition only, $100,000 of cost needs approximately 11,777 purchases at $9.99 and a 15% fee before the other deductions; at 5% conversion that is roughly 235,540 installs, before acquisition spend.

At an illustrative 15% fee, the three price candidates leave $8.49 / $11.04 / $12.74 per purchase before the other deductions. Moving from $9.99 to $12.99 can retain the same gross revenue per install at about 77% of the prior conversion rate; $14.99 needs about 67%. These are arithmetic comparisons, not predictions; refunds, satisfaction and regional access also matter. Keep the same 120-scenario entitlement at whichever price is selected, and cost the work still needed for the entire package before treating any price as sustainable.

### Distribution plan

- Start with a polished trial, accurate native gameplay footage and clear ARIA demonstration. Prioritize a bounded organic/creator/store-page test before large paid campaigns.
- Track acquisition source separately; organic and paid cohorts can have very different costs and purchase intent. Downloads alone are not success.
- Use sequential or adequately powered storefront-supported price experiments, document country/source/build differences, and avoid claiming causality from incomparable cohorts. Do not personalize difficulty or offers based on failure or ARIA dependence.
- Treat PC as a separate paid-game-plus-demo opportunity after controls, performance and demand are validated. Do not copy the mobile price automatically.
- Distribution/licensing deals are optional business development, not assumed funding. This plan books no Apple Arcade, Netflix or publisher revenue and assumes no deal availability.
- Gate the first paid expansion on measured owner interest and a costed, complete content package. Forecast its development/support/localization costs against conservative owner attach, not a fixed seasonal obligation.

## 10. Analytics and decision rules

Instrument enough to answer product questions, with platform-appropriate disclosure and consent. No raw voice, personal call signs, story choices tied to identity, or detailed permanent touch recordings are needed for monetization. Keep diagnostic traces separate from ordinary product analytics.

| Event family | Required dimensions / question |
|---|---|
| Trial install/start and mission start/result | Build, platform/device class, locale, source cohort when available, mission, content package/intro scope, attempt, outcome, active duration. Where does each free sample lose players? |
| First meaningful command / lesson progress | Time to control, stalled lesson, retry/exit reason. Does the player understand the game before the purchase boundary? |
| ARIA start/stop/handback/result | Mode, mission, control duration, manual/mixed/ARIA-led classification and blocked/loss reason. Is the feature useful and trustworthy? |
| Chapter 1 / Operations intro complete; offer shown / product detail | First-completion versus replay, product identity, exact content version, localized price, view/dismiss. Are separate package boundaries clear? |
| Purchase initiated/pending/verified/failed/restored/refunded | Product, store transaction deduplication, platform reason category, cohort. Does commerce work and is value accepted? |
| First paid mission start/complete | Delay after verified purchase, entitlement/download failures, outcome. Does the purchase deliver the promised continuation? |
| Resource decisions | Authored starting stock, shortages, canceled orders, waits, retry; sampled where needed. Are tactical constraints fair without spending? |
| Base/expansion completion and replay | Completion, Operations/Skirmish frequency, qualitative satisfaction. Is there demand for additional content? |

Report both `purchases / all eligible installs` and `purchases / Chapter 1 completers`, with a fixed observation window and denominator. A high completer conversion can conceal a broken onboarding funnel. Segment by platform, locale, acquisition source and ARIA use; do not aggregate away an inaccessible device or translation problem.

Keep the Operations funnel separate: intro entrants → each mission → intro completion → theater detail → verified theater purchase → first full-city session. Report independent theater buyers and Campaign owners distinctly. Do not add conversion percentages across overlapping customers. Model total app receipts from deduplicated product transactions; Campaign economics above exclude theater, Skirmish pack and cosmetic revenue.

Suggested measurement windows: D1/D7 engagement, 7/30-day trial completion and purchase conversion, purchase-to-first-paid-mission continuation, base-campaign completion among owners, refund/support rate and 30/60-day replay-mode use. Retention after finishing a finite campaign is not automatically the same product failure as early abandonment.

### Decisions from the measurements

- Poor M01/M02 completion: fix controls, teaching or technical failures before price/advertising tests.
- Poor M03/M04 completion: inspect defense waits and transport handoff before shortening the trial or charging for assistance.
- Strong completion but weak purchasing: interview non-buyers and test price/content communication. Do not increase difficulty or add paid resources.
- Verified purchase but little CH02 play: inspect ownership, download/resume and the first paid mission before attributing it to lack of interest.
- Heavy ARIA use with good satisfaction: keep it included and promote it honestly. Heavy use with weak comprehension: improve coaching and optional handover, not monetization restrictions.
- Low expansion interest: stop producing paid add-ons until the base game and audience justify them.
- Scale paid acquisition only when mature cohort net receipts exceed acquisition and variable costs with a positive conservative margin. Set a test-spend cap from the actual budget before buying traffic; this task authorizes no ad spend.

Before testing, choose a minimum commercially meaningful effect and sample size using the observed baseline. Report uncertainty intervals and cohort maturity. Do not declare a winner from a handful of purchases or assign arbitrary industry conversion thresholds to this game.

## 11. Implementation work and release gates

The order prevents a polished purchase page from getting ahead of a coherent product. Work packages are sequential dependencies where indicated; this document does not authorize publishing products or charging customers.

| Package | Concrete deliverable | Dependency / exit condition |
|---|---|---|
| M0 — align commercial scope | Campaign Edition includes all 25 story missions and S001–S120; standalone complete Operations and later additional packs have explicit membership. Free five + one + three samples; included ARIA, no ads/skips, legacy treatment and price tests. Amend conflicting authorities together. | No simultaneous “all future content” promise and separate packs, or “story always free” promise and a paid chapter boundary. No paid reclassification of the original 120 Skirmish entries. |
| M1 — resource ownership | Shared live account read model; eliminate fake balances; typed account/tactical separation; block tactical rewards entering the account; migration plan. | Save backup/migration/deduplication checks pass; no account balance affects match start or spending. |
| M2 — tactical cost migration | Replace remaining scenario Money/Credits dependence with authored Materials budgets in affected Campaign and legacy mode paths; retain Fuel/logistics. | M02 budget regression plus M03, Air Corridor and Steel Push normal-input wins/retries; no free construction from deleting a cost. |
| M3 — content and ARIA coverage | Finish remaining eight campaign missions and ending; complete and certify every core Skirmish scenario S001–S120. Maintain product/ship manifests. Build O001–O003 intro scope independently and complete full theater separately. | Each sold package has complete manual/ARIA journeys, settlement/return, hashes and device/player gates. Internal batches do not reduce the core entitlement. Full-city completion cannot require another product. |
| M4 — entitlements and billing | Separate non-consumable Campaign/theater/pack adapters and verified ownership, atomic persistence, per-content launch/resume checks, restore/offline/refund handling. | Store-sandbox lifecycle and mixed-ownership matrix passes. No accidental theater access from Campaign purchase, duplicate sales of included scenarios or active-match mutation. |
| M5 — native purchase journey | Trial labeling, post-M05 offer, simplified store/profile, owned/download/restore states and localized pricing. | Required visual-direction review first; native EN/FA, large text, safe-area and normal-input navigation accepted. |
| M6 — trial/value test | Unfamiliar-player trial, actual offer comprehension, price experiment and source-separated economics. | Findings recorded with uncertainty; no paid launch justified by mockups or source inspection alone. |
| M7 — paid release candidate | Fixed content manifest, final price/territories, source/build identity, actual product metadata and support route. | All mandatory content/commerce/device gates met. Pending issues listed explicitly; no unsupported store products shown. |
| M8 — subsequent content | Complete Operations theater release, a themed Skirmish collection, a new campaign or one cosmetic pilot, chosen from evidence and production readiness. | Each offer is finite and accepted; no reselling content already included in that customer's purchase. |

### Required acceptance cases

1. Fresh profile completes M01–M05 manually and with supported ARIA, sees all story, gets each first-clear grant once and returns normally. The offer does not interrupt the debrief.
2. Trial player can replay all five missions, use ARIA again and play the accepted demo; no timer or play quota appears. CH02 cannot launch through Continue, replay, resume or another navigation path without ownership.
3. Verified purchase preserves name, story history, stars, rewards and current progress. CH02 becomes eligible when its progression prerequisite is met. Cancelling/pending purchase does not corrupt progress or falsely unlock content.
4. Owned base campaign completes from an ordinary one-star route without purchasing upgrades, grinding, ads or Support. The canonical ending is included. Optional mastery remains achievable under its published rules.
5. Zero and very large account balances produce identical tactical starts and costs. M02 remains 120/90/20, cancellation cannot mint money, and other migrated missions have separately validated sustainable budgets.
6. ARIA Start/Stop/physical takeover, cutscenes, pause, shortages, failure, result and return behave correctly. No automatic next mission or purchase occurs; scenario Support consent remains correct.
7. Operations AP/day transitions, partial result, technical failure, retry/checkpoint and reward commits neither double-spend nor duplicate rewards. No offline decay or monetized recovery appears.
8. Each included Skirmish scenario has matched publication/evidence hashes, valid production/Fuel/capacity and the required normal-input ARIA and human-control outcomes; difficulty/seed coverage is recorded separately.
9. Purchase success/cancel/pending, duplicate callback, process death, lost connection, refund/revocation, restore, reinstall, account mismatch, profile reset, installed-content offline play and missing download are tested in platform-supported environments.
10. Native UI review, automated checks, agent-operated normal-input journeys, unfamiliar-player feedback, packaged-player validation and physical-device acceptance each have their own record. One type does not substitute for another.
11. A completely free profile finishes and replays O001–O003 with ARIA or manual input, reaches the intro conclusion and suffers no inaccessible-district pressure. A theater-only buyer can start and finish all six districts without buying Campaign Edition. A Campaign-only buyer receives all 120 core Skirmish entries S001–S120 and keeps the free Operations intro. Reinstall/restore preserves each combination correctly. Every core ID maps to that same product; later pack ownership is never required for it.
12. Existing customers retain the product membership and fixes promised at purchase. Later packs have no duplicate included content charge, hidden unit dependency or requirement to finish an already purchased story/run. A new product's free sample remains playable after declining its offer.

All Unity implementation/build/capture validation follows the repository wrappers and licensing contract. Retain failed logs and required pass markers. New mission-screen or substantial HUD/store redesigns require actual-reference mockups and the user's visual-direction review; the plan itself creates no new screen and claims no such approval.

### Current evidence status at this audit

| Evidence category | What is available | Still pending / not established here |
|---|---|---|
| Source/config audit | Completed for the listed resource, commerce, ARIA, progression and 17 mission assets. | Not a runtime behavior certificate for every mode. |
| Visual review | Four existing native samples inspected for resource/ARIA presentation; historical reports reviewed. | Fresh current-build menu/store/match review and proposed commerce-screen approval. |
| Automated tests | Prior reports include Chapter 1 focused tests, ARIA input/decision checks and newer mission checks. | No tests rerun in this documentation task; commerce tests do not exist as passing evidence. |
| Normal-input gameplay | Prior Chapter 1 bilingual journeys and latest Steel Push journeys; O001 ARIA wins with retained manual Partial Success evidence. | Full current-build 25-mission path, all advertised mode variants, and new post-migration/purchase flows. |
| Real player / physical device | Reviewed reports explicitly distinguish these gates. | No new unfamiliar-player, phone-performance or paid-customer acceptance established. |
| Commercial evidence | Price/fee arithmetic and official platform references. | Actual conversion, willingness to pay, refunds, acquisition cost, production budget and signed distribution revenue. |

## 12. Authority changes and source register

### Documents/assets to update when adopting this plan

| Authority / implementation | Required update |
|---|---|
| `AAA_Mobile_Game_Design_Document_v0_2.md` | Transparent free samples and complete separately purchased content packages; no lifetime-all-content implication. Preserve fair completion and included ARIA. |
| `Monetization_Strategy.md` / `Monetization_Store_Catalog.md` | Replace consumable/F2P catalog with finite Campaign Edition (25 story missions + S001–S120) and complete Operations theater entitlements; later packs add to the stated contents; no ads/skips/season products or fake scarcity. |
| `Economy_Reward_Design.md` | Sole active earned Credit wallet recommendation, legacy migration, immutable ownership, no tactical account grants, fixed scenario access and no paid power. |
| `UIUX_MainMenu_Visual_Contract.md` and current menu/store visual authorities | Revise two-currency requirement, plus-buttons and live-balance binding; preserve established typography/art and mobile priorities. |
| `Resource_Logistics_Exchange_Design.md` | Remove persistent/paid Rush integration; keep scenario-owned tactical exchange costs and time. |
| `First_Player_Experience_And_Story_Onboarding_Design.md` | Transparent sample/package labels, protected opening, M05 post-story offer, independent mode onboarding, O003 local recap, resume/restore and demo reveal. |
| Campaign high-level catalog, chapter specs and narrative sequence catalog | Update “no purchases” progression wording at the single Campaign Edition ownership boundary; preserve full purchased story and fixed fragment grants. Resolve M05 parts/upgrade naming. |
| `Combat_Catalog_And_Upgrade_Design.md` / balance configs | Remove monetized unlock paths; distinguish fixed Campaign awards, scenario roster and optional tactical research. Keep approved Support schedule. |
| ARIA Demonstration plan/concept/coverage | Core Watch and coaching included in every accessible certified mission; future coaching candidates cannot remove existing functionality. Preserve consent, handback and non-punitive settlement. |
| Operations/Skirmish plans and publication manifests | Finite product membership and release counts; O001–O003 standalone intro scope and persistence; full six-district theater completion; no paid AP/supplies/capacity; scenario roster and ARIA gates. |
| Store view/builder/localization and reward/profile screens | Data-driven released products, actual prices/status, no developer placeholder copy in production, no inactive catalog advertisement. |
| Profile save/migrations, Campaign settlement, launch evaluators | Preserve old progress; separate permanent entitlement from progression; remove incompatible resource paths and fake defaults. |

### Local implementation sources inspected

- [Store view](../../Assets/Game/Scripts/UI/Screens/StoreCommandExchangeV3View.cs) and [store builder](../../Assets/Game/Scripts/Editor/StoreCommandExchangeV3PrefabBuilder.cs).
- [Menu resource builder](../../Assets/Game/Scripts/Editor/MainMenuPersistentResourcesPrefabBuilder.cs), [bootstrap defaults](../../Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs), [shell defaults](../../Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.DefaultState.cs), [shell state](../../Assets/Game/Scripts/UI/Shell/Ecs/UiShellStateSystem.cs).
- [Profile data](../../Assets/Game/Scripts/Persistence/SaveDataModel.cs), [Campaign settlement](../../Assets/Game/Scripts/Runtime/Campaign/CampaignMissionProgressStore.cs), [Breach reward mapping](../../Assets/Game/Scripts/Runtime/Campaign/CampaignMissionProgressStore.Breach.cs), [Operations atomic reward integration](../../Assets/Game/Scripts/Persistence/SaveService.Operations.cs).
- [Campaign registration](../../Assets/Game/Configs/Campaign/CampaignMissionCatalog.asset), [sequence](../../Assets/Game/Scripts/Missions/Contracts/CampaignMissionSequence.cs), mission/scenario paths listed in the JSON audit.
- [Attempt initialization](../../Assets/Game/Scripts/Runtime/Missions/CampaignMissionAttemptResourceInitializationSystem.cs), [construction spending/rollback](../../Assets/Game/Scripts/Systems/FactionConstructionResourceUtilitySystemHelper.cs), [HUD resources](../../Assets/Game/Scripts/UI/Screens/MatchHudResourceHeaderPresentation.cs), [HUD restrictions](../../Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.MissionHudRestrictions.cs).
- [Operations config/reward amounts](../../Assets/Game/Scripts/Operations/Contracts/OperationsConfigSchemas.cs), [strategic session](../../Assets/Game/Scripts/Operations/Strategic/OperationsStrategicSession.cs), [strategic settlement](../../Assets/Game/Scripts/Operations/Strategic/OperationsCitySystems.cs), [live dashboard profile projection](../../Assets/Game/Scripts/Composition/OperationsMissionPresentationSystem.cs).
- [Skirmish publication manifest](../../Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishPublicationManifest.asset), [ARIA skill policy](../../Assets/Game/Scripts/Configs/Skirmish/SkirmishAriaSkillPolicy.cs), [Campaign ARIA decision loop](../../Assets/Game/Scripts/UI/Shell/Ecs/AriaPlayDecisionSystem.cs).

### Design and readiness evidence used

- [Initial recommendation](../AgentReports/2026-09-28_pm_monetization-model-recommendation.md), [current strategy](Monetization_Strategy.md), [catalog](Monetization_Store_Catalog.md), [economy](../Economy_Reward_Design.md), [exchange](../Resource_Logistics_Exchange_Design.md).
- [Campaign mission catalog](../Campaign_Mission_High_Level_Design_Catalog.md), [Chapter 1 detail](../SagaChapters/Saga_Chapter01_First_Response.md), [maturity matrix](../Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md), [first-player flow](../First_Player_Experience_And_Story_Onboarding_Design.md). Older maturity labels can lag newer implementation; they were not treated as current build proof.
- [M01/M02 Materials correction](../AgentReports/M01_M02_Materials_Economy_Fix_2026-09-13.md), [M01–M05 readiness handoff](../Roadmap/M01_M05_Readiness/COMPLETION.md), [Steel Push readiness](../AgentReports/CH04M02SteelPush/readiness.md).
- [Operations plan](../Roadmap/Operations/PLAN.md), [O001 player-experience evidence](../AgentReports/Operations/O001_PLAYER_EXPERIENCE_20260924/README.md), [Skirmish plan](../Roadmap/Skirmish_Expansion/PLAN.md), [Skirmish 3 focused inspection](../AgentReports/Skirmish3Readiness/readiness.md).
- [ARIA plan](../Roadmap/Aria_Demonstration/PLAN.md), [historical implementation status](../Roadmap/Aria_Demonstration/IMPLEMENTATION_STATUS.md), [Skirmish AI/ARIA](../Roadmap/Skirmish_Expansion/AI_AND_ARIA.md), [approved Support audit/amendment](../AgentReports/2026-09-28_pm_support-ability-campaign-audit.md).

Official web sources were checked on 2026-09-28 and linked beside the applicable commerce/fee claims. No third-party conversion benchmark, guaranteed distribution deal, ad-network rejection or assumed user spending behavior is used as evidence.
