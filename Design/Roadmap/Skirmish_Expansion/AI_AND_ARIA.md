# Enemy decisions and ARIA for expanded Skirmish

Planning specification, updated 2026-09-21. Applies to the 120-scenario catalog; a future 200 extension remains separate scope. Current source contains a small Base Assault planner and three prototype mappings, not the expanded objective/army architecture. [OBJECTIVE_IMPLEMENTATION](OBJECTIVE_IMPLEMENTATION.md) specifies reusable policies, scores, skill transitions and exact terminal behavior; [TECHNICAL_ARCHITECTURE](TECHNICAL_ARCHITECTURE.md) names the public contracts and owners. [Each battle brief](Scenarios/README.md) supplies deterministic acceptance seeds and capability dependencies.

## Shared strategy knowledge, different execution

Enemy AI and ARIA should use common definitions of unit roles/counters, affordability, objective semantics, travel/transport constraints and command eligibility. They may share pure scoring functions and tactical concepts. They must not share a privileged observation stream or an actuator that lets ARIA bypass the interface.

Pipeline for both: perceive → maintain knowledge → choose objective → allocate groups → choose feasible action → execute → observe result → replan. Make objective/role policies reusable; scenario data supplies goals, terrain graph, allowed roster, legal start and personality. No switch on S001…S120 and no authored click sequence per battle. Objective-specific policies are legitimate reusable modules. Map coordinates belong in map data; neither planner gets a hardcoded east-flank coordinate or privileged successful route.

| Boundary | Enemy AI | ARIA |
|---|---|---|
| Observation | Its faction's legal visibility, last-seen intel, own force/economy and public objectives | Read-only player-visible UI/world/map observations and public goal semantics, matching what the player can inspect |
| Production/orders | Calls normal game command services with identical costs/caps/prerequisites | Simulates visible touches to open production, choose unit/quantity, confirm, select groups and issue orders |
| Navigation | Plans world routes from legal knowledge; no camera needed | Uses normal camera drag/pinch, minimap or public focus controls to reach interactable targets |
| Feedback | Command acceptance and legal observed outcomes | Visible acceptance/rejection, queue/force/objective changes and camera/selection feedback |
| Unknown information | Scout; stale estimates cannot become live target IDs | Scout/open public map; cannot read undisplayed enemy transforms or resources |

No neural/cloud model is required for the first implementation. This is a local data-driven hierarchical planner with bounded work. A future model must satisfy the same observation/action restrictions.

## Enemy AI decision layers

1. **Strategic:** select current priority from defend, expand, scout, build counters, contest objectives, attack, escort/interdict or recover. Reassess every 3–6 seconds and on significant legal events; stagger factions/groups. Strategic difficulty parameters can lengthen decision latency but must not slow pathing/combat simulation.
2. **Economy/production:** reserve recovery funds, estimate observed threat composition, buy affordable counters and production capacity, and replenish purposeful groups. No free units, free fuel, hidden research or infinite resources. Lost production triggers rebuild/alternate producer logic.
3. **Operational:** divide reserves/frontline/flank/air/logistics escorts according to objective progress and local risk. Gather a viable force before committing; use two routes when affordable. Reinforcements join real groups, not a stream of lone attackers.
4. **Tactical:** maintain suitable range, focus appropriate threats, escort vulnerable units, retreat before avoidable losses, use transport/anti-air intelligently. New targets require sustained evidence; avoid order oscillation and abandoning a near-complete objective for one passing enemy.
5. **Unit autonomy:** reuse movement, acquisition, firing, fuel safety and transport systems used by the player's units. Never tune shared weapon stats to make the demonstration win.

Score includes objective value, time remaining, own/local known strength, counters, route cost/exposure, supply and retreat feasibility. Record the selected reason and competing scores in developer diagnostics. Stale knowledge confidence decays; opponent cash/queues are not observable unless a supported mechanic exposes them. A full-visibility ruleset is symmetric and clearly labeled; recon-focused catalog entries require functioning fog/intel.

### Objective policies

- **BA:** preserve a reserve, scout approaches, contest supply and attack the base when force/route conditions warrant. A threatened home base can override an assault; one harmless scout should not pull the whole army back.
- **FC:** estimate ticket pressure; hold two sustainable zones, rotate reinforcements, send a harassment group only if it does not sacrifice the majority. Do not pile every group onto one captured point.
- **BT defense:** cover both corridors with a mobile reserve, observe the designated groups and intercept exposed evacuees. No knowledge of the player's selected exit route before observation. Do not camp an unreachable off-map exit.
- **CE interdiction:** observe the convoy, choose a legal intercept route, prioritize exposed escorts/transport based on risk, regroup after a failed interception. No timed enemy teleport waves; all reserves begin in legal deployment or use paid production/delivery.

Personality is a bounded weighting choice such as aggressive, defensive or mobile, seeded independently of spawn randomness. Every personality must still pursue the objective and recover. Difficulty does not select a secretly stronger roster. Four explicit levels are specified in [MATCH_SETUP.md](MATCH_SETUP.md).

## ARIA decision and teaching requirements

ARIA reads a versioned, publicly displayed goal model: objective type, progress, time, loss conditions, roster restriction and legal action capabilities. Rendered briefing and semantics come from the same definition in both languages. Text alone is not treated as executable arbitrary instructions. Unsupported objective mechanics block ARIA with a clear explanation during development; capability gating cannot substitute for finishing the release catalog.

Generic skills: inspect army/economy; choose counter; recruit/batch and verify delivery; build/rotate/confirm or cancel; research; group and split; Attack Move and direct attack; defend/hold/retreat; route/camera navigation; scout/Scan; board/unload; manage aircraft and fuel; capture; escort; evacuate; recover shortages; recognize terminal results. Each skill must verify normal visible state rather than assume a completed gesture succeeded.

Use objective policies analogous to the enemy's but with the player's role: attack/defend base; maintain zone majority; protect designated evacuees; escort two surviving trucks. Route choice comes from public map information and observed obstructions, not scenario-number branches. During migration remove the existing S1/S2 approach exceptions only after equivalent route reasoning wins both baseline regressions.

Human-readable pacing targets: roughly 0.6–1.2 seconds of anticipation before a new target, 0.12–0.25 seconds contact for taps, and 0.6–1.5 seconds for a visible drag, adjusted for travel distance and accepted responsiveness. Wait for interface/camera stability before contacting. These are animation tuning ranges, not reasons to delay emergency cancellation. Use batched recruitment/groups so large armies do not demand hundreds of tiny touches.

The cyan hand stays above all relevant UI, with a clear press pulse and drag trail. Every visible press corresponds to real input. Select button use is followed by the actual rectangle drag when custom selection is intended; squad cards use a tap. Camera gestures are visible and interruptible. Watch requires confirmation; Stop and genuine physical takeover cancel pending touch immediately and preserve accepted game orders. No automatic next battle or replay.

Explain decisions briefly in conversational EN/FA: “They have armor. I’m recruiting anti-armor infantry”; “We hold two zones—protect these approaches”; “Waiting for the helicopter; I’m keeping the landing area clear.” Show wait reason and progress instead of repeatedly opening the map. Display one current intention; rate-limit speech and stop gameplay voices at result. Wrong/rejected actions trigger a fresh observation and a bounded alternative, then an honest handback if unresolved.

ARIA is a strong demonstrator, not a victory cheat. It uses the selected difficulty and starting resources unchanged. No enemy slowdown, disabled attacks, stronger units, hidden money, objective completion or forced result. It may lose on high difficulty or irrecoverable handover. Explain that honestly; never label a scripted win universal intelligence. Monetization is separate; Start/Stop behavior and fairness never depend on payment.

## Generalization and acceptance

- Test reusable rules against maps/objective/start combinations withheld while tuning. Hold back at least one legal deployment seed per map and later one map's route arrangement. Do not train/tune only on seed 104729.
- Every released scenario requires three seeds in EN and three in FA on Regular at normal speed: at least two wins in each language, no input/deadlock violation in any run. This is a minimum acceptance sample, not a statistical 90% win-rate claim. Keep all losses, telemetry, content hashes and reasons. First catalog baseline therefore contains at least 720 touch-only full runs, expanded when failure patterns require it.
- For each map × objective × army family (60 strata), run a 20-seed stress sample on Regular, alternating starts; disclose measured outcomes rather than infer them from a few demonstrations. Rotate device/locale samples separately from logic coverage.
- All four enemy difficulties require legality tests and full simulated matches for every catalog entry. In addition to the six-run Regular sample, **each entry needs at least one full normal-speed ARIA win on every additional difficulty it exposes**; packets provide seeds. Each additional exposed size also needs an EN and FA ARIA win plus its device/recovery gate. These are supplemental samples, not permission to omit the Regular minimum or the stratified stress plan. Record size/difficulty/seed/locale explicitly and retain unsuccessful attempts. Recruit should be teachable; Veteran/Commander should challenge skilled players. Validate ordering with human play; never promise a precise difficulty win rate without evidence.
- Start ARIA at the beginning and recoverable mid-match states: casualties, lost base facility, low fuel, mixed selection, full queue, interrupted delivery, changing objectives. Test Stop/takeover during every skill. No arbitrary doomed-state win requirement.
- Record perception provenance and assert ARIA actuator dependencies exclude gameplay-mutation services. Developer omniscient diagnostics are read-only and never fed back into the planner.
- Mobile performance, readability and real touch evidence remain separate gates. Full catalog support is not complete because the automated win counter is green.

New scenario acceptance should normally require data, briefing, layout and validation work—not a new AI/ARIA code branch. If a new mechanic is needed, add a reusable skill/objective policy and recheck all scenarios sharing it.
