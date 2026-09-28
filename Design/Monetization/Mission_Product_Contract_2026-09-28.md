# Mission product contract — all three modes

Date: 2026-09-28. Status: adopted for mission planning at the owner's request; implementation and release acceptance remain pending. Prices remain hypotheses. This document does not activate purchases or alter runtime assets.

This contract applies the [monetization plan](Monetization_Plan_2026-09-28.md) to **25 Campaign missions, 120 Skirmish scenarios and 60 Operations missions**. The [205-row policy register](Mission_Product_Policies_2026-09-28.csv) gives every entry an explicit product, free-access scope, progression rule, economy/reward policy, change requirement and owning brief. All IDs, objective graphs, maps, existing production batches and Support schedules are preserved.

For these collections, this contract supersedes older mission-planning instructions to sell currencies, parts, tactical supplies, accelerators, account power or story fragments. It also replaces the blanket claim that the entire Campaign is free. Older reward identifiers/save fields are migration inputs, not authorization to delete earned ownership. Historical readiness reports remain historical evidence and are not rewritten by this amendment.

## Product membership

| Collection | Included contents | Free access | Purchase boundary |
|---|---|---|---|
| Campaign Edition (`warline.campaign.shattered_relay`, proposed ID) | CH01-M01–CH05-M05 **and all S001–S120** | CH01-M01–M05 and S001, unlimited replay | One permanent package; Chapters 2–5 also retain ordinary prior-mission progression. No later toll for the original 120 scenarios. |
| Operations: Sahrin Theater (`warline.operations.sahrin`, proposed ID) | O001–O060, all six districts and the complete city conclusion | O001–O003 in the standalone introduction defined below | Independent of Campaign Edition; the whole connected theater is one package. |
| Future additions | Explicit finished content beyond those collections | Defined with each later offer | No charge to complete, repair or rebalance content already included. |

Membership does not certify implementation, override a player's existing verified entitlement, or make unfinished content purchasable. Before shipping access restrictions, check prior public promises/customer ownership. Store price is platform metadata, never a mission field. Delivery milestones such as Operations 3/12/30/60 remain internal production gates. Complete every advertised entry and its required acceptance before selling the package as complete.

## Shared rules and acceptance

| Rule | Authoring requirement | Required evidence before release |
|---|---|---|
| MP01 — access | Separate product ownership, prior-mission progression and runtime/device readiness. Check normal launch, Continue, replay and resume through the same content-access authority. ARIA has the same access. | Free/owned/theater-only/combined-owner cases; restored ownership; unavailable content cannot bypass a gate or start consuming gameplay costs. |
| MP02 — tactical resources | Scenario owns Materials, Oil, Fuel, forces, queues, research and capacity. Account Credits/Command/Rush/parts never fund them. Migrate legacy tactical Money costs to authored Materials, with sufficient budgets; never delete a cost without rebalancing. | Identical starts and affordability at zero/large account balances; exact-once spend/refund; real shortage recovery. Preserve M02's 120 Materials, 90 Barracks and 20 squad baseline until separate balance evidence justifies change. |
| MP03 — rewards/progression | Earned account Credits serve optional cosmetics; XP/stars record progress/mastery. Required units/tools come from the scenario or fixed earned milestones. One-star Campaign success includes its story clue and ordinary progression within owned content. | First-clear/replay/deduplication and save migration; same world outcome earns the same reward under manual or ARIA play. No mandatory parts grind, account upgrades or premium ending. Keep current reward amounts during the technical migration. |
| MP04 — ARIA | Guidance and Watch are included on accessible content; visible Play/Stop and immediate handback. Use existing consent scopes and public controls. Tactical Watch does not authorize strategic Run Watch; separately opted-in Operations Run Watch follows its own approved scope. Neither can purchase content. | Normal-input wins and the mode's existing seed/difficulty/language matrix; no hidden knowledge, free resources or forced results. Unsupported ARIA is a readiness defect, not a paid tier. |
| MP05 — Support | Consume the existing explicit Campaign/Skirmish/Operations Support policy. Charges and costs are earned or scenario-owned and optional under that policy. Preserve confirmation, civilian protection, caps and cleanup. | Real use, refusal, cancellation and save/result behavior; no purchase dependency. Campaign unlocks remain Smoke after CH04-M02, Strike after CH04-M03, Paratroopers after CH04-M04, Supply after CH05-M02. |
| MP06 — time/recovery | Keep tactical production/research/logistics time and authored deadlines. No ads, paid skips, lives, real-world AP recharge, offline decay or paid rescue. Failure can remain meaningful and an individual battle can be lost. | Normal retry/withdraw/checkpoint/return; shortages explain gameplay choices; purchase UI never interrupts an active battle or offers to reverse its outcome. |
| MP07 — completion | A purchased story/theater can reach its conclusion with its own included tools. Preserve all 120 core Skirmish IDs. Do not inflate counts with seeds/difficulty or weaken missions to create a sales funnel. | Full one-star Campaign path; full six-district Operations run; every included Skirmish scenario certified separately. Trial endings and free replay remain available after declining an offer. |
| MP08 — evidence | Track source/config, native visual review, automated checks, normal-input play, human acceptance and physical-device acceptance separately. | Pin candidate hashes and retain failed attempts. Old prototype wins, `Playable` labels and documentation checks do not establish current-build release readiness. |

Product access is evaluated before deployment. Once a permitted mission is active, payment state cannot increase or decrease forces, simulation speed, supply, AI behavior or success criteria. Restore/offline/refund behavior follows section 8 of the monetization plan at safe boundaries.

## Campaign application

Every high-level mission entry now carries its specific change requirement, and each chapter links this contract. Use the policy register's canonical mission ID when translating plans into configuration; short CH codes are documentation identifiers.

- **CH01-M01–M05:** complete free story/debrief/replay and included ARIA. M01/M02/M04 retain their core gameplay. M03 needs tactical-Money migration and pacing regression. M05 settles its story and earned rewards before one purchase invitation; resolve its APC-parts target/consumer without revoking existing ownership. CH02 entitlement does not replace the M05 completion prerequisite.
- **CH02-M01–CH03-M05:** included in Campaign Edition. A fresh owner progressing once through the story must have all required tools and counters. Retain mission-specific readiness fallbacks: Supply Line's working authored Oil/Fuel chain does not imply that all Chapter 2 free construction is ready. Chapter 3 target confirmation, evidence and ARIA guidance are included.
- **CH04-M01/M02:** migrate tactical Credits/Money where used, revalidate actual construction/recruitment budgets and keep meaningful Fuel/anti-air constraints. Steel Push retains 120 military Fuel and the protected 40-barrel civilian floor. The Smoke milestone is earned after CH04-M02 under the approved Support plan.
- **CH04-M03–CH05-M05:** implement the remaining eight mission plans with local resources and the approved optional Support lessons. Every required objective remains solvable without optional Support. Preserve the canonical ending, earned clues and no-new-mechanic finale rules.

The mission-spec template must record: membership/free scope; ordinary progression; starting resources and required costs; tool/counter source; shortage/failure/retry route; first-clear and replay settlement; ARIA and Support consent; and MP01–MP08 evidence. No new mission balance values are invented by this amendment.

## Skirmish application

Every S001–S120 entry belongs to the same Campaign Edition entitlement. S001 is the free sample; other entries need the package but **no Campaign grind or account roster purchases**. Available scenario units still require their normal in-match facilities, readiness, Materials, Fuel and capacity. All accepted sizes/difficulties of an included scenario are included; unsupported device configurations remain unavailable for readiness reasons, not as premium tiers.

Keep five maps × four objectives × three army profiles × two starts. Field Base development, Established Base grants, symmetric/asymmetric objectives, legal enemy behavior and all existing counter requirements remain intact. Preserve tactical research and its reset on a new battle. A resume restores the same battle rather than issuing fresh resources. Future paid collections must add to the original 120.

The twenty generated scenario packets carry an individual product-contract line for every entry. The documentation generator retains those lines on regeneration. Prototype S001/S025/S073 mappings are compatibility references only; original small-battle evidence cannot certify their expanded definitions.

## Operations introduction and full theater

The three-mission engineering prototype is not automatically a commercially finishable introduction. Add the following scope to the shared mode implementation without creating duplicate tactical mission IDs or changing their mandatory graphs.

| Contract | Introduction | Full theater |
|---|---|---|
| Proposed scope identity | `operations.sahrin.intro.v1` | `operations.sahrin.full.v1` |
| Available missions | O001, O002, O003 and their free replay/recovery paths | O001–O060 through normal district prerequisites |
| Active strategic state | Only the supported introductory D01 sites/routes and consequences | All six districts with existing adjacency, offers, incidents and stabilization rules |
| Completion | Persisted Victory milestones for all three introductory missions; local-services recap | All six local finales plus existing thresholds for two consecutive committed End Days |
| Failure/recovery | Keep Partial/Defeat/Withdraw semantics, retries/reoffers and explained AP/End Day; no inaccessible-mission requirement | Existing recovery actions, mission consequences and voluntary archive rules |
| Purchase presentation | After intro completion and settlement, when the complete theater is released; replay/return remain available | No further district purchase or Campaign Edition dependency |

**O001:** preserve scans, evidence, extraction and its distinct Partial predicate; close its currently pending full human-controlled Victory/device gates. **O002:** preserve truck identity, both routes, clinic protection and delivery minimum; provide its task force in both scopes. **O003:** preserve both pumps, clinic protection, repair/hold objectives and the authored 80-Materials repair requirement; provide the required Materials inside the scenario. Reaching O003 is not intro completion: all three Victory milestones are required. An O003 Partial cannot set that milestone.

Intro offers, incident selection, day ticks, adjacency, goals and ARIA strategic observations must filter by scope. No O004/O010 requirement, pressure from inaccessible districts, artificial city-wide victory or silent reset of damage. After intro completion, preserve its recap and unlimited free mission replay; practice follows the existing no-repeat-reward rule. Incomplete intros retain a recoverable AP/day loop. These are proposed mode rules requiring implementation and normal-input acceptance.

Save scope ID/version with the run, launch snapshot, checkpoint, result and atomic settlement. Validate scope on resume/return; never load an intro checkpoint as a full-city run. Retain account-wide first-clear ownership for canonical O001–O003 so buying/replaying the theater cannot duplicate grants. Default upgrade flow archives the intro and starts a normal fresh full-city run, clearly explaining that account records transfer but intro city state does not. Never overwrite an existing full run without the ordinary explicit confirmation. Existing full-run saves migrate to full scope; local progress is not evidence of a storefront entitlement.

Complete O001–O003 in the real player flow before publishing the intro; complete O001–O060 and the full-city acceptance before selling the theater. B12/B30 remain development milestones with no district tolls. No Operations action can sell AP, supply readiness, trust, repair, recovery or tactical resources.

## Delivery order and checks

1. Adopt the explicit 205-entry membership and this contract in mode/chapter/mission briefs (this documentation change).
2. Implement shared account/tactical separation and the three known Campaign economy migrations; preserve unchanged mission baselines with focused regressions.
3. Implement Operations scope, completion, save and reward deduplication; finish the introduction's normal-input/manual/ARIA gates.
4. Complete the remaining authored content under these contracts; readiness and dependency gates still apply per entry.
5. Implement verified content access and purchase/restore/offline flows, then validate free/owned profiles against the exact released manifests. Do not use live payments to test unfinished content.

The [coded-mission change list](../AgentReports/2026-09-28_monetization-mission-change-readiness.md) distinguishes existing complete Editor journeys, incomplete coded candidates and planned content. This task performs source/document inspection and planning validation only; it does not rerun Unity or implement the listed changes.

Execution handoff: [implemented-missions remediation](Implemented_Missions_Remediation_Handoff_2026-09-28.md) assigns ordered work packages, source boundaries and acceptance evidence to the next agent while preserving the completed Support implementation.
