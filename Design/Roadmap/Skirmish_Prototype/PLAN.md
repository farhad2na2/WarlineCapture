# Skirmish prototype — detailed delivery plan

Updated: 2026-09-18. Roadmap milestone 2. Status: **S0–S4 implemented; S5 functional Editor validation complete with coverage limits; S6 pending**. The [completion report](COMPLETION.md) records exact checks, play results and unaccepted device/player/pacing criteria. The original plan below remains the scope reference.

Campaign baseline: `b9d17a700` — M1–M5 implementation and internal Editor QA complete, including the M1 story/commander follow-up. Android device and unfamiliar-player acceptance remain separate. Shared regressions and ten campaign entry checks were rerun during skirmish work; this was not a fresh full campaign playthrough.

Companion documents: [code baseline and risks](BASELINE.md), [acceptance cases](ACCEPTANCE.md), [parent roadmap](../README.md).

## 1. Purpose and scope

Prove that selecting forces, positioning them, building reinforcements and attacking are understandable and enjoyable when the player makes the decisions. The prototype must support multiple approaches without an ARIA lesson sequence dictating every click.

Deliver one complete loop: **Main Menu → Skirmish setup → match → result → Replay / Adjust Setup / Main Menu**. Use the existing command-base art direction, command bar, selection wheel, Build drawer, localization system and 3D operation-map infrastructure.

The recommended first preset is **Base Assault**: attack the enemy's marked main Barracks while protecting your own. This uses an existing building, gives the player a visible destination and avoids hunting the last enemy across a large map. It is a proposed new objective policy, not a relabeling of the existing `DestroyAllEnemies` enum.

| Included in this milestone | Deferred |
|---|---|
| One authored map layout; one player versus one AI faction | Multiple maps, procedural terrain, multiplayer, three-enemy matches |
| One objective preset, one Normal AI profile | Survival, Sandbox, capture modes, Easy/Hard tuning, every legacy AI slider |
| Infantry production, a small starting combined-arms force, useful construction choices | Full aircraft/transport/support roster, new unit models, tech trees |
| Materials/Oil/Fuel using existing tactical economy and logistics | Persistent rewards, store purchases, economy overhaul, paid advantages |
| English and conversational Farsi; clear combat and economy feedback | New story comics, mandatory tutorial chain, new paid voice recordings |
| Replay, restart, pause/resume, surrender, saved setup and result summary | Resume an interrupted match after app termination; serialized ECS world saves |

A seed is for repeatable AI choices and QA, not a promise of new terrain. Keep map and roster stable for the first balance pass. Treat the older broad [Skirmish spec](../../Skirmish_Mode_Implementation_Spec.md) as future direction; this document defines the smaller current milestone.

## 2. Match rules

### Objective and outcome

- Assign exactly one existing Barracks per faction the scenario role **Main Base**. Keep its catalog name and add a localized role badge; extra Barracks never inherit this objective role.
- Win when the enemy Main Base reaches zero health. Lose when the player's Main Base reaches zero health. If both die in the same authoritative simulation update, record a draw.
- Evaluate destruction before the time limit on that update. A decisive destruction at the deadline counts; otherwise an unfinished match at 15:00 is a draw. Pause suspends this clock.
- The designated Main Base cannot be sold, demolished through friendly Destroy, rebuilt into a replacement objective, or silently moved by placement recovery. Explain the unavailable action. Enemy attacks remain valid.
- Ending is immediate and final: stop accepting new gameplay orders/production, stop gameplay narration, remove dead-object cues, and show one result. A destroyed building uses its authored destroyed visual when available.
- Result reports outcome/reason, elapsed time, units lost/defeated and buildings lost/destroyed. No Campaign stars, unlocks, XP, Credits, Command or Operations changes in this prototype. Surrender records defeat; canceling a load records no result.

### Opening and pacing targets

These are initial tuning targets, not measured results or hardcoded global values:

| Moment | Target experience |
|---|---|
| World ready | Camera at the established default zoom of 40, centered on the player's force/base; orders immediately usable |
| First 10 seconds | Player can identify their base, the enemy objective and the route without opening a manual |
| First 30 seconds | At least one useful move, production or construction choice is affordable; no forced wait |
| First AI attack | Approximately 75–105 seconds; defenders react immediately if the player attacks sooner |
| Main match | Target 8–12 minutes at normal speed, with pressure tied to actual AI production and losses |
| Last minute | Existing match timer visibly signals time remaining; no hidden score tie-breaker or surprise overtime |

Use the same behavior rules for both factions. The AI has no free emergency spawns, unannounced resource grants or invulnerable objectives. Attack cadence is a planning target, not a scripted convoy timetable. A player may rush, reinforce, defend or flank without invalidating the match.

## 3. Map and roster

Start from `opmap.skirmish.desert_base_01` and `scenario.skirmish.desert_base_standard`. Create an isolated preset placement/layout configuration; do not move the shared map's campaign anchors or overwrite the M5 compound.

The required topology is two readable bases joined by a direct approach and a longer flank. This is a topology requirement, not an assertion that the current map already has suitable routes. Stage S0 measures real traversal and available build space before choosing positions.

- Initial enemy contact must not happen while the player is reading the setup/loading screen.
- Each base has visibly buildable ground, room for the largest allowed footprint, clear production exits and reliable logistics paths.
- The direct route exposes a defended approach; the flank trades travel time for a different attack angle. Neither route may depend on undocumented terrain/collision exceptions.
- Structures stay off roads and sidewalks. Road Barriers are optional and, if included, follow the validated carriageway-only placement rules. Do not add them merely to fill the catalog.
- Main Base markers, minimap locations and attack picking use the same runtime target. No marker may point to decorative walls or a displaced building.
- No required civilian-risk objective in this military exercise. Keep civilians out of the combat corridor in the scenario layer; do not add civilian penalties that the opening does not teach.

| Role | Initial recommendation | Implementation limit |
|---|---|---|
| Core infantry | Two groups of four existing rifle soldiers per faction | Barracks replenishes the existing four-soldier production entry |
| Mobile fire support | One existing Light Armored Car per faction | Starting unit only in the first slice; validate fuel use and infantry counterplay |
| Main Base | One designated Barracks per faction | Existing art and combat/production behavior; scenario objective role added separately |
| Construction choices | Extra Barracks, Guard Tower, Field Fabrication Depot | Add logistics recovery structures only where needed to prevent a resource dead end |
| Supply backbone | Existing Oil extraction/delivery, fabrication, refinery and usable Fuel storage | Pre-place a working chain; reuse automatic logistics rather than making manual truck control a new prerequisite |

Target a maximum of 24 combat soldiers plus one starting combat vehicle per faction for the initial balance pass. Logistics vehicles are counted separately and bounded. Population is a visible scenario budget, not a silent production failure. Do not enable aircraft, APC boarding or artillery until this smaller loop is accepted.

If the existing armored car cannot be countered by the supported infantry/buildings, resolve that in the prototype roster before enabling it. Do not globally rebalance the campaign to rescue a skirmish matchup.

## 4. Economy and construction

Use the canonical three match resources: **Materials, Oil and Fuel**. Persistent Credits remain outside simulation and must not appear as tactical costs. Legacy `Money` fields need explicit compatibility handling, not merely a different label.

S0 must record actual prefab costs, queue durations, delivered resource rates and storage caps. From those values, author one versioned economy preset:

- Starting Materials cover one useful defensive/construction choice plus two rifle production batches. Capacity must accommodate that grant and a normal delivery batch.
- Starting Fuel covers the car's base-to-base trip and a return/reposition margin; actual usable storage, not refinery output or tanker cargo, backs the HUD value.
- The prebuilt supply chain supports replacement infantry and incremental construction. Show shortages, stalled delivery and full storage distinctly.
- Use real Oil delivery and fabrication/refining. No invisible periodic Materials credit to conceal a broken logistics chain.
- AI construction and production reserve/deduct/refund through the same resource policies. Failed placement or canceled production refunds exactly once.
- Construction remains an active choice. Build closes after a successful production request; placement preview is centered, draggable without camera conflict, and commits at the shown location/rotation.
- Loss of a supply building is recoverable while resources and a valid rebuild route remain. If a faction is economically stranded, present the shortage honestly; the base-destruction/time-limit/surrender rules still terminate the match.

Do not publish precise resource amounts until this arithmetic is checked. Record the chosen values and expected affordable sequences in a balance table during S3; all tuning stays in scenario configuration.

## 5. AI behavior

Reuse `AIBuildPlannerSystem`, `AIProductionSystem`, `AISquadSystem`, `AITargetingSystem` and `AICombatOrderSystem` after checking their real integration. Prefer narrow per-preset policies over a parallel AI framework.

1. Maintain a small base defense and use available supply to replace losses.
2. Form a field group from actually produced units; only dispatch when a usable group and path exist.
3. Attack via a reachable approach, defend the Main Base when threatened and replan after a destroyed target or blocked route.
4. Use a seeded choice between the direct route and flank, with a bounded cooldown against order thrashing.
5. Respect population, build space, affordability, production times and the same target eligibility rules as the player.

Initial knowledge is explicit: **full-map visibility, no fog of war**. Both sides know the opposing base location. Hide the Fog/Intel toggles until a real visibility model exists. A preset must not claim stealth or scouting mechanics that are absent.

Validation must show the AI spending, producing, moving, fighting and reacting in a real match. Unit tests or a setup slider changing value do not prove an opponent works.

## 6. Mobile UI and assistance

### Setup

Reuse SCN-13 with one map preview, roster preview, the objective, the 15-minute limit, Normal opponent and Deploy. Preserve the existing style. Hide unsupported presets/rule controls in this prototype; do not present enabled options that silently fall back. Keep deterministic seed controls in an optional QA/advanced section.

One short objective example, finalized through localization:

- EN: “Destroy the marked enemy Barracks. Protect your own base.”
- FA: «پادگان علامت‌خوردهٔ دشمن رو نابود کن. نذار پایگاه خودت از بین بره.»

### During play

- Header: Materials, Oil and Fuel. Objective strip: both Main Base health states and match time, with taps that focus the corresponding target without taking prolonged camera control.
- Keep the wider squad tray and seven-command HUD from the campaign work. No restored Stop button or extra radar command. Unsupported support actions are absent or disabled with a meaningful reason.
- Quick-group cards select a specific, stable group in one tap. Keep slots stable during touch interaction; add newly produced groups only into available slots or through the existing group-selection affordance. Do not reorder cards every time threat priorities change.
- Do not say “tap these soldiers” when an action actually requires rectangle selection. Keep the existing optional rectangle gesture, but make tap-based group selection sufficient for the core match.
- ARIA provides concise event information and optional field help. No mandatory Continue chain or Do It button. No artificial M1/M3 lesson state in skirmish.
- If Show Me is offered, it identifies an available next action or relevant target, hides while its indicator is visible, and clears when the action is completed or no longer applicable. Use the existing animated chevron/double-border guide.
- Keep text readable at 1280×720 and 2400×1080, with scrolling only inside the text area. Stacked full-width actions never cover instructions. Minimap stays bottom-right above the command row.
- Accepted/rejected commands, production, damage, resource stalls and destruction have visible feedback. Important meaning never depends only on color or voice.

### Results and persistence

Replay uses the same validated preset/settings/seed with a new session ID. Adjust Setup restores the last valid settings. Main Menu returns without campaign progression changes. Restart and Surrender use a clear confirmation; canceling it resumes the same paused match.

Version and persist the full valid setup and a compact result summary using the existing save layer. Unknown/removed values migrate to this supported preset with a visible reset explanation. Interrupted application sessions reopen setup; they must not pretend to restore a match. Do not save raw entities, entity references or a half-finished reward transaction.

## 7. Implementation sequence

Each package ends with a reviewable change, evidence and an updated issue register. Commit separately at stable boundaries. Do not combine unrelated campaign changes with skirmish work.

| Package | Work and dependencies | Exit evidence |
|---|---|---|
| **S0 — Baseline and playable contract** | Run the existing setup/launch path in EN/FA; inventory faction startup, AI, costs, roads, bases, result and save behavior. Record current defects. Finalize base anchors, roster matchups and initial economy calculations. | Reproduction captures; chosen config IDs/values; no unverified “already implemented” claims |
| **S1 — Session/configuration ownership** | Depends on S0. Preserve full setup, create an immutable validated launch snapshot, identify mode/preset/session/seed/return route, apply it before simulation starts, and prevent duplicate Deploy. Implement mode-specific cleanup and setup persistence. | Config round-trip and runtime launch readback agree; campaign state/wallet unchanged |
| **S2 — Match with an ending** | Depends on S1. Bind isolated scenario placements and both Main Bases; implement outcome/time-limit policy; wire result, Replay, Adjust Setup, Surrender and restart. | A short real attack can cause victory and a real enemy attack can cause defeat; simultaneous destruction and timeout produce the documented results |
| **S3 — Economy, roster and opponent** | Depends on S2. Enable actual production/construction/logistics, limited roster/population, AI spend/produce/dispatch/defend loop and path recovery. | Normal-speed player-versus-AI match with no scripted victory or injected resources; balance sheet reconciles spend/refunds/delivery |
| **S4 — Readability and bilingual flow** | Depends on the working S2/S3 loop. Simplify SCN-13; objective/status UI; stable tap groups; contextual help; labels, descriptions, result reasons and existing voice routing. | Complete EN/FA flows at narrow/wide aspects, including wrong actions and interrupted placement |
| **S5 — Balance and recovery acceptance** | Depends on S4. Play aggressive, defensive and mixed strategies at normal speed; vary supported seeds; test repeated sessions, failure/retry, pause, language changes and campaign transitions. | [Acceptance matrix](ACCEPTANCE.md) closed for internal prototype; current input hashes and known limitations recorded |
| **S6 — Player/device decision** | After S5. Observe unfamiliar players and test representative phones when available. Android gameplay is explicitly deferred by the current direction, not marked passed. | Decide iterate/keep/expand using comprehension, choices, duration, input and performance evidence |

S0–S5 implementation and internal evidence are now recorded in [COMPLETION.md](COMPLETION.md). Next is S6: unfamiliar-player evaluation and separately scheduled device validation, before expanding maps, difficulties or rewards.

## 8. Architecture and protection of completed missions

Use a skirmish-owned session/rules state and project read models for UI. Proposed data includes preset revision, map/scenario ID, two faction definitions, objective entity roles, start budgets, roster, AI policy, timer, seed and result route. Naming is provisional until S1; extend existing boundaries where they fit.

- Do not fake a Campaign M1 launch to obtain a map, tutorial, reward or result flow. Resolve the actual skirmish scenario through the shared loader.
- Apply launch configuration before faction economy, AI plans and initial spawns become active. Reset it when leaving the session; avoid mutable global settings leaking into a later campaign mission.
- Keep one authoritative result writer. Presentation observes state; it never marks objectives complete because a button was clicked.
- Scenario overlays own new placements, resource/pacing values and roster restrictions. Shared prefab edits require targeted M1–M5 regression checks.
- Preserve Addressables address ownership and the strict duplicate-dependency gate. Use repository Unity wrappers for builds/tests/captures, keep Hub open, and follow AGENTS.md.
- Keep unit models fully rendered; do not reintroduce the rejected impostor/LOD behavior as an optimization shortcut. Reduce scoped counts or costly optional effects first and measure.

## 9. Completion and next decision

Internal prototype completion requires all S1–S5 acceptance cases to pass, no known crash/progression blocker/misleading required action, and at least three distinct tactical approaches exercised in both languages. Each normal-speed run must record its outcome and useful decisions; a loss is acceptable evidence, a broken route is not.

Observe 3–5 unfamiliar players before expanding scope. They should identify the objective, select a group, give an order, produce reinforcements and understand the result without coaching. Record where they hesitate and whether they want to replay; do not invent a satisfaction score or treat automated victory as proof of fun.

If the loop works, move to roadmap milestone 3, basic progression and rewards. If it fails, fix the specific control, pacing or decision problem within this one preset. New chapters, extra modes and decorative animation remain later work.
