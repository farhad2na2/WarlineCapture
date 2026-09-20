# Skirmish battle catalog: 120, expandable to 200

Recorded 2026-09-20 at the owner's request. This is the product planning target, not implemented content. Numerical balance values are initial test specifications. This supplement supersedes the conversational suggestions of six maps or twelve battles as the complete experience. Operations design is a separate next planning task.

## Count and terminology

The first complete catalog targets **120 individually selectable, validated battle scenarios on five battlefields**. A battlefield is physical terrain. A scenario combines a battlefield, objective, army configuration and starting setup. Difficulty, language, seed, player side and army-size options do not create additional catalog entries.

**5 battlefields × 4 objective types × 3 army configurations × 2 starting setups = 120 candidate scenarios.** There are 24 candidates per battlefield. The [catalog CSV](SCENARIO_CATALOG.csv) enumerates every stable ID and its configuration references; all entries begin Planned. The formula is an authoring inventory, not evidence that all combinations are fun or valid. A rejected combination must be redesigned or replaced with a materially different authored setup before counting it toward the release target. Never publish filler to reach a number.

The two current small Base Assault scenarios are introductory prototypes. Evolve their expanded equivalents into S001 and S025; preserve the old configurations as regression/practice presets, without advertising them as extra battles. Existing play results must not migrate into victories for materially changed scenarios. Existing saves keep their old content version or return safely to setup.

Companions: [map briefs](MAPS.md), [complete setup and economy](MATCH_SETUP.md), [enemy AI and ARIA](AI_AND_ARIA.md), [implementation order](DELIVERY.md), [acceptance](ACCEPTANCE.md).

## Catalog dimensions

| Dimension | Stable values | What changes |
|---|---|---|
| Battlefield | DB Desert Base; CC City Crossroads; MP Mountain Pass; IB Industrial Basin; AP Airfield Plains | Terrain, connected routes, base slots, objectives, supply and air approaches |
| Objective | BA Base Assault; FC Frontline Control; BT Breakthrough; CE Convoy Escort | Winning decisions, group allocation and progress/outcome rules |
| Army configuration | G Ground Maneuver; A Air Mobile; C Combined Arms | Available role families and counters, starting composition and production path |
| Starting setup | F Field Base; E Established Base | Facilities, readiness, forces, balances and expansion pace |

Each card names all four: for example **City Crossroads · Convoy Escort · Air Mobile · Established Base**. S001–S024 belong to DB, S025–S048 CC, S049–S072 MP, S073–S096 IB and S097–S120 AP. Sort each block BA, FC, BT, CE; within each objective G, A, C; then F, E. IDs never change when labels are localized or cards reordered.

## Objectives: one shared rules engine per type

The following are initial rules to implement and playtest; only the small BA loop exists today. All countdowns/progress are large, localized and visible outside ARIA. Outcome evaluation uses one authoritative state and a single result transaction.

| Type | Player's task | Normal win/loss and deadline | Necessary map data |
|---|---|---|---|
| BA — Base Assault | Destroy the designated enemy main Barracks while protecting yours | Enemy base destroyed wins; own destroyed loses; both destroyed in the same simulation tick draw. Both alive at deadline draw. No hidden score tiebreak. | Two expandable bases; at least three viable approaches and supply routes |
| FC — Frontline Control | Use dismounted infantry to hold a majority of three zones | Each side starts with 500 tickets. Losing the majority drains one ticket/sec. Enemy tickets zero or enemy main base destroyed wins; reciprocal event loses. Simultaneous opposing terminal events draw. At deadline higher tickets wins; equal tickets draw. | Three zones with distinct tactical value; multiple approaches to every zone |
| BT — Breakthrough | Open a corridor, then evacuate designated friendly combat groups through the far exit | Start with 12 designated rifle soldiers; at least eight must exit. Exit opens after any one of two corridor objectives is held by dismounted infantry for 20 uninterrupted seconds. Then move the soldiers to the exit; each must remain alive there for three seconds. Eight evacuated wins. Fewer than eight alive plus evacuated makes victory impossible and loses; otherwise deadline loses. Main-base destruction prevents production but is not an additional terminal rule. | Two alternative defended corridors, muster area, exit, reinforcement approach; enemy cannot seal both with friendly construction |
| CE — Convoy Escort | Protect and deliver three objective supply trucks, choosing a safe route and escort | At least two trucks arrive with five uninterrupted seconds in the destination zone. Two destroyed makes victory impossible and loses; deadline with fewer than two delivered loses. Trucks are selectable/movable, fuel-exempt objective transports with no weapon, cannot be sold, boarded or replaced. Damage/repair rules remain normal. Main-base destruction disables production but does not override the convoy outcome. | Origin, destination, two valid truck routes, exposed segments and defensible rest points |

FC: eight uncontested seconds to neutralize, then eight to capture. Enemy dismounted infantry freezes progress. Partial progress starts decaying after ten empty seconds and drains at the same rate it accrued. An owned empty zone stays owned. Aircraft, vehicles and embarked passengers cannot capture. No bonus income from capture in the first release.

BT: corridor progress resets on enemy infantry contest or absence of friendly infantry. Once opened, the exit remains open. Designated soldiers stay normal combat units with a clear icon, can board compatible transport, but must disembark before exit credit; no teleport evacuation. The 12 soldiers are part of the starting roster, not 12 extra bodies outside the cap. In Field Base their initial groups are all rifles; recruit specialist support normally. Do not require a particular soldier's survival beyond the public eight-of-twelve rule.

CE: delivery vehicles are separate from the combat-unit totals but included in performance and support accounting. Starting logistics trucks remain separate and cannot masquerade as objective trucks. The convoy stays at origin until ordered; no arbitrary departure while the player reads the briefing. The match deadline still runs after briefing confirmation. No enemy spawn inside explored territory. Initial convoy truck health uses its validated shared definition, not difficulty multipliers.

BT/CE ship with the player in the attacker/escort role and enemy AI in the blocking/interdicting role. Custom role reversal is later, separately certified, and not part of the initial 120 count. Asymmetry is an explicit objective setup, never hidden difficulty assistance.

At each simulation tick resolve all destruction, evacuation, delivery and ticket events first. If both sides meet opposing terminal conditions in that tick, draw. Otherwise terminal success/failure precedes the deadline. The result explains the actual deciding rule. Surrender always concedes and stops ARIA.

## Army configurations and usefulness

| Profile | Included | Intentionally excluded |
|---|---|---|
| G — Ground Maneuver | Five infantry roles; armored car, APC variants, tank; ground missile launcher; Radar Tank when intel works; ground logistics; transport helicopter and recon drone | Attack helicopters, strike/fighter jets and transport plane |
| A — Air Mobile | Five infantry roles; armored car and fast/armored APC; attack/transport helicopters; recon drone, Radar Tank, air missile launcher; jets and transport plane at advanced readiness; ground logistics | Battle Tank, Heavy APC and ground siege launcher |
| C — Combined Arms | All validated military role families above, including heavy ground, offensive/transport air and both launcher roles | Unsupported abilities and noncombatant cosmetic entries presented as combat roles |

These are tactical rulesets, not permanent unlocks. All supported profiles/scenarios are selectable without finishing the campaign. A role's stage/facility/cost requirements still apply during a match. No air offense without affordable reachable air defense; no tank without anti-armor infantry. G does not need anti-air when it permits no hostile air offense. All restricted cards explain the scenario rule. Appearance variants do not count as additional roles or scenarios.

A role ledger must give every existing asset a disposition and at least two accepted use cases across different objective/map contexts, or explain why it is a variant, scenario-only, civilian or blocked. Artillery is represented by the existing ground missile-launcher candidate unless a real artillery system is separately implemented. Do not advertise a conventional artillery vehicle that does not exist. Pilots, civilians and leaders do not become recruitable combatants merely to claim “all units.”

## Selection experience

Use a battle library, not 120 tiny tabs. Show map collections (24 battles each), objective/army/start filters, search, favorites, Recently Played and recommended next battles. Large cards show screenshot, title, objective icon, army profile, duration range and readiness status; completion is tracked per scenario and difficulty. A map overview preview should show routes and public objectives without exposing hidden enemy deployment.

The selected card opens one briefing: objective and loss conditions; player starting force/buildings/resources; enemy rules and any asymmetric garrison; roster restrictions; duration, army cap, difficulty and device availability; one large Start Battle and one Back. Full force lists open in an optional drawer. Avoid a mandatory multi-screen setup chain. Restore filters/selection after returning from a match. EN/FA titles and compact descriptions are authored and visually reviewed, not assembled into unreadable strings.

Default Regular difficulty. Use the largest explicitly selected certified size; do not silently upscale from Standard. The first session starts at Standard. The CSV supplies a recommended size for later visits, with War recommendations gated by checkpoint and device readiness. Difficulty and size choices are remembered separately. Always show concrete unit caps and duration before start.

Advanced setup includes deployment seed, visibility (only certified options), resource/start profile and optional mirrored side for symmetric objectives. Changing a catalog profile creates a **Custom** run with its complete config snapshot; it cannot earn the original scenario/difficulty completion. Seed changes alone preserve scenario identity and remain eligible when legal. The seed randomizes legal deployment/AI personality choices within authored constraints; it never regenerates unsupported terrain or hides economy bonuses. Current code only seeds initial unit placement; wider seeded variation requires implementation.

## Meaningful variety and path to 200

Changing the objective must change how forces are allocated. Changing army profile must create different viable production/counterplay. Field vs Established must change the opening economy and capabilities, not merely add currency. Every combination needs an authored opening description, at least two viable strategies, counter availability and measured match pacing. Reuse geometry and systems, not identical battles under different labels.

Expansion target: **5 maps × 5 objective types × 4 army configurations × 2 starts = 200**. Add **Strongpoint Network** (capture and hold two linked installations to enable a final command-site capture; exact rules designed later) and **Siege & Supply** army configuration (siege, engineering/repair and vulnerable logistics; only after those roles work). This adds 30 battles from the fifth objective on old profiles, 40 from the fourth profile on old objectives, and ten intersections: 80 additional battles. These are reserved scope, not ready rules or accepted content. Do not add difficulty levels/seeds to claim the extra 80.

## Delivery count

Deliver in playable slices: expand DB BA-G-F first; then all 24 DB candidates; bring CC to 48 total; MP to 72; IB to 96; AP to 120. A slice opens only its validated entries; catalog placeholders clearly show unavailable content in development and are not sold as playable. Full 120 acceptance requires the shared ground/air/intel/transport/objective systems, ARIA coverage and supported-device evidence. This order does not mean postponing cross-map architecture until after DB: validate route/anchor contracts on DB and CC during the first slice.
