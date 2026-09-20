# Skirmish 1 and 2 delivery

Owner request, 2026-09-20: finish the current Base Assault and its Watch ARIA behavior, then add a second selectable Base Assault scenario with different battlefield layout and approach routes. Frontline Control and the hundreds-of-units expansion are not this second scenario.

## Delivery order

1. Finish the current scenario's autonomous English/Farsi baseline wins, visible hand/selection, Stop/restart, terminal cancellation and shared command checks. Retain failed-run evidence. No hidden gameplay commands or balance concessions for ARIA.
2. Author a separate scenario identity, deployment layout and battlefield window using the existing desert environment. Validate all starting structure footprints, staging ground, production exits, connected direct/flanking routes and camera/minimap bounds. Preserve campaign and Skirmish 1 assets.
3. Add two large, localized scenario choices in Skirmish setup. Show each name, battlefield preview, rules and selected state. Persist the selection through deployment, replay, adjust setup and save migration; existing saves default to scenario 1.
4. Resolve the selected map and preset through the launch boundary. Keep normal base destruction, time limit, economy, recruitment, combat and delivery rules shared. Remove assumptions that every Skirmish launch has the first map's identity or anchors.
5. Run player-control smoke checks and full touch-only ARIA matches in both languages on scenario 2. Recheck scenario 1 after the shared launch/setup changes. Verify Stop/handback and terminal UI, then document exact results and remaining release-only gates.

## Evidence and scope

The Editor baseline is separate from the roadmap's multi-seed statistical certification, physical Android performance/touch testing and unfamiliar-player teaching study. None may be marked passed without evidence. The requested two scenarios must be playable and independently selectable, and ARIA must demonstrate actual wins in both; working gesture traces alone are insufficient.

Status: **both selectable scenarios reached actual autonomous victories in English and Farsi in the Editor, using default seed 104729 and normal rules.** The player-facing ARIA control remains a development preview until wider release certification.

| Scenario | English | Farsi |
|---|---:|---:|
| 1 — Desert Base | Victory 5:38 | Victory 5:20 |
| 2 — City Crossroads | Victory 6:17 | Victory 6:22 |

Evidence: [structured results](skirmish-two-scenario-results.json). QA used the explicitly approved API start only; all gameplay used the shipping visible touch driver. All four runs ended with automatic input cancellation (stop reason 5) and a saved victory result. Three victory screens were reviewed; screenshot capture was unavailable for the final Farsi S1 terminal screen, whose result was verified from match state.

Current fixes:
- Map navigation cancels the delivery camera transition that previously pulled the view back and caused repeated map opening. The S2 English acceptance run had two sampled map openings.
- Both factions' actual tower weapons now use the configured Skirmish roster values; campaign weapon values are unchanged.
- ARIA recruits before spending the opening on construction, recovers partial army selections, replenishes losses, and preserves Attack Move through incidental contacts.
- S2 takes an eastern approach based on visible base/map information; S1 retains its verified base-relative approach.
- S2 has separate north/south deployment, localized scenario choices, saved selection and replay routing. Ten authored structure origins passed the placement audit without road, sidewalk or water overlap.

Focused validation: 96 planner checks, 14 shared decision checks, 18 touch-input checks, 15 scenario/weapon checks and the camera ownership regression. A final post-Play input fixture run initially failed to deliver its synthetic click; isolating its Editor input-routing setting fixed the fixture, and the rerun passed. The setting is restored during cleanup and does not change gameplay input safeguards. Full evidence and source hashes are in the structured results. Editor returned to Edit mode and the temporary save override was cleared.

## Historical investigation log

The entries below record rejected candidates and intermediate states. Their “pending” statements and temporary strategies are historical, superseded by the results above.

## Scenario 2 authoring candidate

The read-only surface survey shows the current scenario approaching the city from west/east. Evaluate a north/south approach for scenario 2: staging north of the city and an opposing base south of the highway. This provides a central urban route and routes around the city's western/eastern edges, rather than merely swapping the existing bases. Candidate world window: x=900–1300, z=260–875. These are survey bounds, not accepted deployment coordinates; footprint, road/sidewalk overlap, delivery clearance, connected navigation and visual inspection must pass before authoring is accepted.

Implementation boundaries:

- Add a stable scenario choice to `QuickGameConfig` and its UI/save projection; normalization preserves the supported choice and maps unknown/old values to scenario 1.
- Resolve preset, mission/scenario/map identity together from the queued launch snapshot. Every startup, resource, production, combat and layout-audit preset lookup must use that selection rather than the global scenario-1 resource constant.
- Create separate initial-force, construction and logical operation-map assets through Editor APIs. Reuse the accepted physical scene and shared combat rules.
- Add two localized, clearly selected setup cards; keep the existing launch, replay and adjust-setup flow.
- Validate the actual pending-touch sequence as well as completed actions. A plan must retain a map-opening target until the popup is visible; an instantaneous unit-test transition previously missed cancellation during the hand approach.

## Scenario 2 placement validation (2026-09-20)

The initial north/south candidate failed closed: nine sites intersected map reservations. The revised layout then exposed three sidewalk overlaps; the road and sidewalk masks are separate. Final live layout audit passed all ten exact authored origins, with zero road, sidewalk or water cells. Player staging is (1020,750), opposing staging (1100,400). The first autonomous run subsequently stopped at the opening tower preview: a camera transition hid a pending touch site beneath the HUD. This run is a failure, not a win. The planner now re-observes site reachability and switches to another visible site, normal valid confirmation, or Cancel; three focused regression cases cover the transition. Autonomous rerun is pending.

## Further QA findings (2026-09-20)

The opening construction recovery passed a subsequent real match, but the second assault later blocked after repeatedly focusing an already-visible enemy base. The approach observer depended on friendly contacts still being on the local minimap; camera movement could remove those contacts. It now uses the two public base objectives for an approach when no friendly contact is displayed, with a planner fallback to the visible base when no ground approach is reachable. A focused regression covers that fallback. Group observation now prioritizes a tighter visible contact cluster to avoid pulling the selection rectangle toward base logistics. These changes require fresh full-match wins; they are not yet accepted on gesture traces alone.

The tactical map title now follows the selected scenario. The English/Farsi setup chooser was visually reviewed, exposing and fixing live locale refresh for the new labels. The current focused checks pass: 82 planner, 14 decision, 18 touch-input and 11 scenario configuration checks. A previous fixed test-count string was replaced with an executed-check counter.

The current computer-control tool returns `windowNotFoundAtPosition` on Unity coordinate clicks, although screenshots and accessibility menu actions work. API-started QA was rejected by automatic approval review because it skips the visible consent confirmation. The user subsequently explicitly approved API starts for temporary QA only. This does not change the player confirmation flow. Window → Panels → Game through accessibility restored actual Game-view focus after ordinary window raises failed. Test starts use the approved API; gameplay remains the shipping visible touch driver. Focus-loss and physical-input takeover safeguards remain enabled. Full-match acceptance is still pending.

## Coordinated assault follow-up (2026-09-20)

Fresh S2 diagnostic runs (`aria-s2-cards-en`, `aria-s2-guard-en`, `aria-s2-rally-en`, and `aria-s2-rally2-en` under `/private/tmp`) did not produce an accepted victory. The first exposed a rectangle selecting only part of the army; the planner now falls back to squad cards when the displayed selection count is too small. The next showed the faster armored car pursuing into the city before recruitment finished. ARIA now visibly selects that card and presses Hold during the opening. A visible per-card Move sequence gathers the army behind the player base before its rectangle selection. The first rally point was obscured by the command bar, so it was moved to the opposite side of the public base objective. The later run selected the assembled force, but suffered repeated combat losses and left the enemy base intact; it was stopped for diagnosis, not recorded as a pass.

Read-only combat inspection then found building-breach orders where the planner intended ground Attack Move. The projected approach was too close to enemy structures. Ground candidates now avoid displayed map contacts, nearby threats also prefer clear ground, and direct base assaults require the force to be much closer. Completed ground orders get an observation interval instead of rapid reissuing. The updated focused planner suite passes 96 checks. Fresh full-match validation remains required. These changes neither alter unit statistics nor invoke hidden gameplay commands; only the explicitly approved QA start bypasses the player's Start confirmation.

The clear-ground trial (`aria-s2-clear-en.jsonl`, with `aria-s2-clear-orders.csv`) reached the normal time limit with both bases alive; enemy base health remained 1200. It is a failed win-acceptance run. A read-only order trace confirmed genuine Attack Move orders, but the force still lost too many units over repeated assaults. The next candidate removes the retreat-to-base rally sequence, holds the opening squads together through visible card/Hold gestures, and uses shorter approach points to limit separation between vehicles and infantry. The focused suite now executes 89 checks after removing the rejected rally implementation and its eight tests and adding an opening-infantry defense check. The live `aria-s2-step-en.jsonl` trial is pending; no S2 victory is claimed.

The short-advance trials exposed two navigation defects before a win could be accepted. First, the small-map opening target followed a contact beyond the minimap's clipped bounds (`aria-s2-step-en.jsonl`); opening now targets the fixed center of the visible minimap, then resolves the contact on the full map. Second, excessive friendly-contact clearance left no nearby ground point, and refocusing the group repeatedly rebuilt its selection (`aria-s2-fixed-en.jsonl`, `aria-s2-near-en.jsonl`). Friendly clearance is now smaller than hostile clearance, and an obscured nearby step falls back to the public enemy-base approach without another regrouping cycle. These diagnostic matches were stopped for correction and are not acceptance wins.

Correction to the earlier diagnosis: `BaseBreachOrder` alone is not proof of a mistaken building click. `SkirmishCombatApproachSystem` also uses it for ordinary firing approaches against mobile targets. The inspection did show ammunition-depot targets during some assaults, but the stronger evidence is the explicit `AttackMoveOrder` trace and actual target identity, not merely the presence of a breach component.

The reinforcement staging review found that S2's rally was at approximately (1050,702.5), separated from its original infantry around (1020,705). New arrivals were exposed on the eastern side. The scenario preset and its Editor builder now rally at base-relative (-14,0,-32), beside the initial infantry; this applies equally to manual players and Watch ARIA. The aligned-rally trial (`aria-s2-aligned-en.jsonl`) achieved a 21-infantry selected assault and kept the player base at 1200, but the assault still depleted before damaging the opposing base. It was stopped for correction, not accepted.

The next planner candidate replenishes an ongoing assault below 20 infantry instead of waiting for the selected group to fall below eight. A hidden/cleared selection panel no longer implies the entire army died; the HUD infantry count determines full-army rebuilding. Completed short ground advances wait eight seconds before reassessment. Hostile contact clearance is 15 world units rather than the overly conservative 28. Fresh full-match evidence is still required.

The east-flank candidate navigates using the map projection actually displayed to the player, with one map focus/viewport drag and close per camera relocation. It retains the army selection and uses Attack Move through the visible controls. The first flank trial still failed acceptance (`aria-s2-flank-en.jsonl`): the army was lost before base damage. No destruction/explosion cause was established.

A subsequent damage audit found a concrete roster-binding defect: watchtowers had skirmish values in `UnitAttack`, but `BuildingDefenseAttackSystem` fires `BuildingDefenseWeapon`, which retained campaign range 100 and cooldown 0.3 instead of the preset's 55 and 0.8. `ApplyRoster` now updates both components for both factions; campaign values, visual properties and concurrent slot count are preserved. A real match confirmed both towers had range 55/cooldown 0.8. Four isolated-world regression checks cover actual weapon values and preservation. This is a shared rules fix, not an ARIA-only advantage.

That corrected-weapon trial (`aria-s2-weapon-en.jsonl`, full read-only damage stream `aria-s2-weapon-damage.jsonl`) exposed an opening tactical weakness: infantry remained on Hold against a longer-range armored car while the defensive tower was too far behind the frontline. The next planner candidate responds to a nearby threat with at least eight infantry, instead of waiting for the recruitment deadline. Low-force recovery now also covers card rotation, not only rectangle-selected assaults. Full-match acceptance is still pending.

The opening Hold and early per-card raid-response candidates were rejected after further real matches: the first prevented short-range infantry from closing on armored attackers, while the second split the army before recruitment completed. Neither remains in the implementation. The assembled-force trial still did not damage the base. Its read-only positions/targets showed units chasing incidental logistics structures away from the eastern approach. The current candidate preserves the Attack Move destination when nearby contacts appear; ordinary Attack Move still engages enemies along that route. The planner suite passes 95 checks. Current trial: `aria-s2-route-en.jsonl`; no S2 victory yet, and the shared watchtower rules correction requires fresh S1 regression wins.

The recruitment-first candidate retains 17–19 infantry through the first raid by producing before opening tower placement. It exposed the camera-loop root cause in `aria-s2-recruitfirst-map.txt`: at time 228.117, the normal map gesture correctly requested (1185.16,434.51), then the camera returned toward a still-active delivery smooth-focus target (1027.20,726.44). Tactical-follow pose was invalid; this was an uncancelled smooth transition, not incorrect map projection. Manual map navigation now clears smooth focus and perspective transitions before applying its requested center. A focused camera ownership regression covers subsequent smooth-update ticks. Fresh end-to-end runs remain required.

### First accepted S2 English victory

`aria-s2-camera-en.jsonl`: fresh scenario 2 English, seed 104729, default normal rules. ARIA won at 377.624 match seconds (6:17), 13 player units lost, 36 enemy units lost, zero player buildings lost, four enemy buildings destroyed. Last active observation: player base 1200, 21 infantry. 118 completed gestures; two sampled map openings, with no navigation loop. The actual Victory screen was reviewed and terminal stop reason 5 confirmed automatic touch cancellation. Camera ownership regression and 96 planner checks passed before this run. Farsi S2 and fresh S1 runs remain pending.

S2 Farsi also won: 6:22, 28 units lost / 40 defeated, zero buildings lost / four destroyed; result UI reviewed, terminal stop reason 5. The fresh S1 English regression exposed repeated rejected ground attacks on neutral ruins (visible “Target cannot be attacked” feedback). The wide clear-contact approach candidate is rejected for S1; restored its prior base-relative 25-unit approach, preserving the separately verified S2 flank. The failed S1 diagnostic is retained as `aria-s1-camera-en.jsonl`; no S1 regression win is claimed from it.
