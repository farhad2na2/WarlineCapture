# ARIA unit coverage, S002 (gate 3), 2026-11-17

Evidence: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-17/lead-s002-gate3/unity.log` (`[SkirmishGateUnits]`),
actor trace `Design/AgentReports/SkirmishExpansion/S002/_Evidence/candidates/a2279367…/s002-aria-rs-104731-en-1.jsonl.actors.jsonl`.

## 1. Corrected "available" model
- `role.convoy_logic` is not available in S002. Army profile G (`Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishArmy_GroundManeuver.asset`
  `allowedRoleIds`) does not list it, so `SkirmishProductionCatalog.Create` (`profile.Allows(overlay.RoleId)`) never adds a
  convoy recipe; S002 Standard has `player_objective_trucks=0` (INITIAL_SETUP_MATRIX). It appeared only because the gate
  model resolves roles by runtime prefab key, and `SkirmishRoleCatalogConfig.RuntimePrefabKey` maps both ConvoyLogic and
  LogisticsTruck to `Unit_Veh_Truck_Tray`. Fixed: a role is listed only when the catalog overlay's own RoleKind matches.
- `role.logistics_truck` (2) and `role.tanker` (1) are in the player's starting roster (`player_logistics_support=3`) and are
  profile-allowed and producible. They haul Oil/Fuel on their own (`UnitResourceHaulOrder`, haul phases 1–4 in the 104731
  actor trace) with no player order. They are available, but their use is automatic. The model now reports them as
  `automatic=` when a player unit of that role is seen hauling (2 s poll), and they are no longer counted as `unused`.
- Corrected S002 Standard list: rifle, gunner, rocketeer, car, apc_armored, tank (starting, assault), logistics_truck,
  tanker (starting, support/automatic), plus producible: marksman, breacher (Barracks), apc_fast, radar (GroundStaging; radar
  needs readiness Established, and S002 starts at readiness 2). No producer structure is missing at start.

## 2. Why each genuinely available type is unused, and the smallest use
| Type | Cause | Smallest human-plausible use |
|---|---|---|
| car, apc_armored, tank | Fielded on roster page 1. Used in 130365. In 104731 the NEXT tray swipe overshot page 1 to page 2, and PREV went back to page 0; the loop repeated from t≈55 to 82 (gestures 55.6/59.1/62.2/65.9/69.0/…). Page 1 was never ordered. | Page by exactly one page: a shorter tray drag (one card-page width), or tap the tray's page arrow if one is presented. On overshoot, step back one page with the same short drag. Needs a Unity check of the tray scroll. |
| marksman | Never produced. ARIA's S002 production is rifles (`FallbackGroundRecruit`, infantry < 16), rocketeers (only after the structure force is lost), AA (hostile air) and trucks (air profile only). | After the assault is issued and no recruit is pending, queue one marksman squad when affordable. The existing periodic page inspection and re-sweep send the new group toward the base. |
| breacher | Never produced (same as marksman). Breacher is structure-capable (`IsStructureAssaultRole`). | Same as marksman. Also use it as the structure-force replacement when rocketeers are gone; this would also cover the 155923 fa-IR stall (rifles-only force at the base, 2026-11-17 q1). |
| apc_fast | Never produced (GroundStaging vehicle). | Same recruit step from the Vehicles drawer, once, after the assault order. |
| radar | Never produced, and not an assault role (`SkirmishExpandedPresentedOrders.IsAssaultRole` excludes Radar), so the sweep would never order it. | Produce one radar after the assault order, then one ordinary order: select its card, then Move or AttackMove onto ground behind the column. Its detector widens public sight (fog), which also helps spot slipping vehicles. |

Not implemented (not small or isolated): items 2–5 need new drawer targets (`ObserveCatalogTarget` with the role prefabs),
affordability and pending flags in `SkirmishAriaPublicProjection`, observation and adapter fields, and a policy step. Radar
also needs a non-assault order path. The paging fix needs Unity to verify the tray scroll distance. Suggested order: paging
fix (restores fielded vehicles), then breacher (structure value), then marksman and apc_fast, then radar. One recruit at a
time, only after `AssaultIssued`, so the opening and the first assault are not delayed.

## 3. Implemented (commit after b68ce6ba5)
- Tray paging: `MatchHudSquadTouchNavigation.PageTarget` taps the tray's presented page chevron (`NextSquads`/`PreviousSquads`,
  `ScrollPage(±1)`: exactly one page) and keeps the old swipe only as a fallback when no chevron is shown. Cause: with 11 groups
  in a 5-card viewport, page 1 aligns at 5 strides and the clamped last page at 6, so page 1 owns only the band 2.5–5.5
  strides; the 0.8-viewport swipe (~4 strides) plus ScrollRect inertia landed on page 2 going forward and page 0 going back.
  The tray's snap rule now lives in `NearestPage` (same arithmetic). The S005 observer already pages with these chevrons.
  Overshoot recovery is unchanged (the column policy pages back to the first unordered page), now one exact step per tap.
  Trace: `[AriaS002Trace] event=tray_page from= to= gesture=tap|swipe target= skipped=0|1 ... t=`.
- Breacher: `AriaBreacherPolicy` buys one breacher squad (Build, then the catalog card) once the first assault is issued (assault
  and structure orders accepted), only when affordable, with no rifle/rocketeer recruit pending, yielding to every other owner,
  bounded to 10 s, once per match; the roster sweep orders the new group. `AriaStructureReplacementPolicy` prefers a breacher,
  then a rocketeer, when the whole structure force is gone.
- Fixed with it: the structure-force replacement used to run only with the drawer closed, so its first tap (Build) opened the
  drawer and the drawer step then closed it or bought rifles; no replacement was ever bought (155923 fa-IR q1: rifles only at
  the base, watchdog stop). It now continues inside the drawer it opened.
  Trace: `[AriaS002Trace] event=breacher state=buying|done|pending|fielded reason=coverage|replacement ...`.
- Note: PageTarget is shared by all ARIA skirmishes, so every roster paging now taps the chevron when one is shown.

## 4. Lane result for section 3 (merge d67e1c650, gate log `.../2026-11-17/aria-unit-coverage/gate/unity.log`)
Counted 3/3 wins (130365 en 267.9 s, 104731 en 262.5 s, 130365 fa-IR 261.4 s; baseline 2/3). 155923 en: enemy base 0 at
t=233.2 but recorded Abort `terminal:terminalNotFrozen` (uncounted). Chevron paging reached and ordered page 1 in all 4 jobs;
breacher bought ~t=72, fielded ~t=77 in all 4. Gate units unused: apc_fast, marksman, radar.

## 5. Marksman, apc_fast, radar (branch missions/aria-unit-coverage-2)
- Radar is producible in S002: start package `SkirmishStart_Established.asset` sets `readiness: 2` (Established), the
  level `SkirmishProductionEligibility.RequiredReadiness(Radar)` needs (the starting tank needs the same); army profile G
  (`SkirmishArmy_GroundManeuver.asset`) lists `role.radar`; its producer, Ground Staging, is a starting structure (the
  gate's available line lists no `building.*`); `TryAfford` caps ground at 8 and S002 starts with 3 ground vehicles.
  The gate-3 model is correct and unchanged.
- `AriaCoverageRecruitPolicy`: after the breacher step settles and the first assault is issued, one marksman squad, one
  apc_fast and one radar (in that order, an unaffordable kind waits and the next goes first), one recruit at a time (no rifle,
  rocketeer, breacher or coverage purchase pending), through Build then the catalog card (`ResolveCatalogTarget` switches to the
  Vehicles tab), yielding to every other owner, 10 s per purchase, each kind once per match.
  Trace: `[AriaS002Trace] event=coverage kind=marksman|apc_fast|radar state=buying|done|gave_up|pending|fielded ...`.
- The roster sweep orders marksman and apc_fast (both assault roles). The radar is not, so `AriaRadarOrderPolicy` gives it one
  ordinary order once the column has its assault order: only with no visible selection and no other owner, select the radar
  card on the current page, arm Attack, tap clear ground 40 units behind the presented front toward home (`RearGround`,
  -20016, never goal-bound to the assault approach). Off-screen rear ground waits; paging away or a timeout releases; two
  attempts, one order per match. Trace: `event=radar_order state=selecting|arming|tapping|ordered|released|gave_up ...`.
- Data: three role bitmasks (afford/pending/fielded, bits 1 marksman, 2 apc_fast, 4 radar) through the public projection,
  UI model, observation and adapter, plus a per-page `RadarMask`. No stat, rule, enemy or force changes.

## 6. 155923 `terminal:terminalNotFrozen` (facts for the separate analysis)
The match ended at t=233.26 with the enemy base taken from 140 to 0 by `role.rocketeer.1.2.3` (8 damage, t=233.23); the player
base was never damaged. `[SkirmishTerminalProbe] result=Failed reason=terminalNotFrozen` follows `StartTerminalProbe` 6 log
lines later, i.e. on the first check after `terminalDue`. That check (`TerminalProbe.cs` ~95–101) folds six conditions into one
reason: phase/outcome, terminal attempt, elapsed within 0.05 s, player base HP, enemy base HP, simulation inactive. In both
passing 130365 runs the last logged base hit left 140 HP before Victory, so 155923 is the run whose kill came from a logged
direct-fire hit reaching 0; a late HP write or elapsed tick after Finished would fit, but the log cannot say which condition
failed. Suggested first step: log the failing condition and both values in that reason.

## 7. S026 coverage (branch missions/aria-unit-coverage-3)
Evidence: `.../2026-11-17/lane5-play/unity.log` (S026 130365 en, Victory 172.6 s). Available: apc_armored, apc_fast, apc_heavy,
breacher, car, drone, gunner, logistics_truck, marksman, radar, rifle, rocketeer, siege, tank, tanker, transport_heli. Used:
apc_armored, car, gunner, rifle, rocketeer, tank (+ trucks automatic). Unused: apc_fast, apc_heavy, breacher, drone, marksman,
radar, siege, transport_heli.
- Why breacher/marksman/apc_fast/radar were unused although d67e1c650 and 428bf4559 cover them: those steps run only in the
  S002 native adapter (`AriaS002NativeAdapter.CanHandle`: `SourceScenarioIndex==4`). S026 (same profile G, same Established
  start, `SkirmishStart_Established.asset` readiness 2) is played by the generic `AriaSkirmishPlanSystem.StepExpandedBaseAssault`,
  which had no coverage step; the S026 log has no `[AriaS002Trace]` line at all.
- S026's catalog also offers apc_heavy (Established, GroundStaging, assault role), drone (Established, Intel Station, unarmed,
  Scan only), transport_heli (Established, Helipad, unarmed) and siege (FullArsenal). The S026 layout has a helipad and a
  satellite dish, and the gate's available line lists no missing producer.
- Implemented:
  - `AriaCoverageRecruitPolicy` now covers six kinds in order: marksman, apc_fast, radar, apc_heavy, drone, transport_heli
    (bits 1..32). A kind the mission catalog does not offer is never affordable (`TryAfford` fails on the missing overlay),
    so S002 is unchanged.
  - The generic planner (`AriaSkirmishPlanSystem.Coverage.cs`) runs the same pure policies as the S002 adapter: breacher, then
    the coverage recruits, one at a time after the first assault order, same yields and bounds. It affects every generic
    expanded ground base assault (not air, Combined Arms Field or public-objective missions).
  - `AriaSupportOrderPolicy` replaces the radar-only order: each fielded support unit (radar, drone, transport heli; none are
    assault roles, so the sweep never orders them) gets one ordinary order through its roster card. Radar and transport heli:
    Move (Attack Move only if no Move button is presented) to clear ground 40 units behind the presented front (-20016). Drone:
    Scan (the HUD Scan button) on clear ground 60 units ahead of the front toward the enemy objective (-20017). Starts only with
    no visible selection or armed mode and no other owner; two attempts per kind; one order per kind per match.
  - Correction to 428bf4559: its radar order armed Attack. Radar has no weapon (overlay 0 damage, 0 range), so its Attack Move
    may be rejected; Move is now preferred.
  - Traces now cover every expanded base assault (not only S002): `event=breacher`, `event=coverage kind=...`,
    `event=support_order kind=radar|drone|transport_heli state=selecting|arming|tapping|ordered|released|gave_up`.
- Not implemented, plan: siege needs readiness Full Arsenal (`RequiredReadiness(Siege)`; S026 starts Established). A player
  would research it (Build drawer readiness button, 480 materials, 60 s; `SkirmishResearchCosts`) and then buy one siege
  (420 materials). That is ~900 materials and a minute of research on top of the coverage recruits, a pacing-level spend, so it
  is left as a plan: after the coverage recruits, if materials stay above the research cost plus a rifle reserve, tap the
  readiness upgrade once, then queue one siege when the readiness reaches Full Arsenal; the sweep orders it (assault and
  structure role). The gate-3 model lists siege because it does not model readiness (`readinessGating=0`); siege is
  reachable in-match, so the listing is correct.
