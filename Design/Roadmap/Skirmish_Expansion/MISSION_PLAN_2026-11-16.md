# Skirmish and Operations mission plan — 2026-11-16

Replacement goal and instructions for the agent that builds the missions. This file supersedes the
"shared C# simulator" goal. Owner decisions recorded on 2026-11-16 are in the last section.

## Goal

Ship all 120 Skirmish missions, then all 60 Operations missions, as accepted, playable missions in Unity.
The unit of progress is a mission row moving right in `Design/Orchestration/missions.csv`. Nothing else counts.

## Authority

- Unity is the only authority for mission results. Out-of-Unity simulator results are not evidence.
- Follow `AGENTS.md` and `Design/MapVariants/CAMPAIGN_MAP_REUSE_POLICY.md`: reuse existing Campaign maps,
  use only `Tools/CI/invoke_unity_macos.sh` on macOS, never `-batchmode`.

## Forbidden without asking the owner first

- Any work in `Tools/CI/SkirmishSourceSimulation`, `SkirmishOfflineSim`, `SkirmishAriaReplay`,
  `SkirmishProductionDrafts`.
- New tools, exporters, witnesses, receipt formats, hash pins or evidence schemas.
- New ledgers or status files. `missions.csv` is the only one.
- Proposal or diagnostic documents. Fix the defect, or log it in the `blocker` column in one line.
- New physical maps or map derivatives.

## How to run

- One persistent Editor: `invoke_unity_macos.sh --reuse`, using `SkirmishS002AriaRunHarness` and its
  `ProductionBatch` runner. Run missions in batches of 10 or more per session.
- Normal speed, normal input, ARIA started at mission start. No injected outcomes.
- Logs go outside the repo tree, one folder per batch. Hash sources once per batch, not per run.

## Design changes to make before mass production

Each change edits the authoritative design doc, `INITIAL_SETUP_MATRIX.csv` and the packaged copy
`Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv` together (they must stay byte-identical).
Record old and new values, as the map reuse policy requires.

**D1. Base Assault pacing.** Recorded ARIA wins end at 99–297 s against an 18-minute deadline; the design
target is 12–18 minutes. Target: ARIA wins at 20–70% of the deadline (owner, 2026-11-17: ARIA is
about 1.5–2× faster than a human; see `Design/Orchestration/daily/2026-11-17-directive.md`). Levers, in order: place the two
bases farther apart on the existing map, give both sides more starting defences, move budget from the
starting army into production. Do not change unit stats.

**D2. Army profile must be visible from the first second.** Today all 60 Field Base missions start with
the same force. Initial values to tune from, mirrored for the enemy in BA and FC:

| Profile | Field Base start | Established Base start |
|---|---|---|
| Ground Maneuver | as now, plus 1 tank at War and Large War | as now (tanks, no offensive air) |
| Air Mobile | Helipad built; 1 transport helicopter and 1 light attack helicopter replace the armored car and APC; 1 air missile launcher | Helipad and Airport built, readiness R3; 2 attack helicopters, 1 strike jet, 1 transport helicopter, 1 air missile launcher |
| Combined Arms | 1 APC, 1 light attack helicopter, 1 air missile launcher | 1 tank, 1 attack helicopter, 1 transport helicopter, 1 air missile launcher; Airport pad reserved, unbuilt |

Counter rule stays: no side faces offensive air without an air missile launcher it can afford at R1.

**D3. Jets on every map through the Airport building.** Jets need a runway, and the runway is the
placeable Airport building. Keep jets in all 80 Air Mobile and Combined missions.
- Every layout reserves one legal Airport pad per side, with measured taxi and takeoff clearance.
- If an existing map cannot fit two Airport pads, select another existing Campaign source (policy).
  Only if none fits does that layout become helicopter-only, with the card text saying so.
- Reachability rule: in a Field Base Air Mobile mission the player can afford R2, R3, an Airport and a
  first jet (about 1,320 materials plus the jet) by 40% of the deadline. Tune income, not unit cost.

**D4. Deadlines and attacker budgets per layout.** Convoy Escort deadline = measured route time at truck
speed × a factor tuned from ARIA results. Breakthrough and Convoy Escort start from a player force
advantage over the defender (who also has two extra towers); tune the size from ARIA win rates.

**D5. Base Assault tiebreak.** At the deadline the side with the higher main-base health percentage
wins; equal percentages draw. Show both base health bars on the HUD from mission start. This mirrors
Frontline Control, where higher tickets win at the deadline.

**D6. Prototype the three unplayed objectives first.** Build one Frontline Control, one Breakthrough and
one Convoy Escort mission on Desert Base. The owner plays each once and gives a go/no-go on the rules
before the other 15 layouts are authored.

**D7. Player-facing names match real terrain.** Rename "Mountain Pass" in player copy to fit the Armor
Break geography. Internal `MP` IDs do not change.

## Definition of done (per mission, all required)

1. Scenario assets exist under `Assets` and load from the Skirmish menu.
2. ARIA wins at Standard size and Regular difficulty, from mission start, normal input, with
   time-to-win at 20–70% of the deadline (owner, 2026-11-17). Run count by family (see "Certify by
   family"): type-first 3 English seeds + 1 Farsi; family-first 3 English seeds; sibling 1 English
   win. A Farsi-only failure is a UI or localization defect.
3. ARIA uses every unit type the mission makes available, including aircraft and the Airport.
   Record `units_available` and `units_used_by_aria`. An unused unit type is a defect.
4. Result screen shows and return to menu works; checkpoint and recovery pass where the game offers
   Save (otherwise the row records "checkpoint: not offered").
5. Native UI screenshots sent for owner review.
6. Owner manual win: one mission per layout (20 layouts), recorded separately, never inferred.

**Certify by family (owner, 2026-11-17).** A family is the missions sharing one map source and one
objective type. Type-first (first accepted mission of an objective type): 3 English seeds + 1 Farsi.
Family-first (first mission of each further family): 3 English seeds. Sibling: 1 English win with the
gate-3 trace and result/return in that run, plus the offline draft check, containment and startup
markers; a sibling that loses gets the full 3-seed set after the fix. Each row records the rule used
and the family mission it relies on; reopening a family-first mission reopens its siblings.

Other sizes (War, Large War) and difficulties (Recruit, Veteran, Commander) are certified once per
layout, not per mission. This replaces the 13-run ARIA matrix in each scenario brief.

## Quick check

- Record `quick_prediction` (win or lose, predicted time) for every mission before its Unity run.
- Record `unity_result` next to it. Never edit a prediction after the Unity run.
- The quick check may replace a Unity re-run only after 300 comparisons at 99% agreement or better, and
  only for re-checks after balance changes. First acceptance is always a Unity ARIA run.
- Do not build a new simulator. Use `Tools/Missions/quickcheck.py` and a model fitted on the table.

## Tuning loop (allowed by the 2026-11-06 owner direction)

- ARIA loses or times out: change one lever per iteration, in order: deadline, starting forces,
  resources, production capacity. At most 5 iterations and ±40% from the template.
- ARIA wins before 40% of the deadline: strengthen the enemy or reduce the player.
- ARIA Abort or Blocked is a defect in input, spawn or placement. Fix it; never tune around it.
- Tune shared templates, re-run every mission that consumes the value, record old and new values.

## missions.csv columns

`mission_id, mode, map_source, objective, layout_id, budget_template, code_commit, assets_ok,
aria_en (W/L/A x3), aria_fa, aria_time_s, deadline_s, units_available, units_used_by_aria,
quick_prediction, unity_result, tuning_changes, result_return_ok, checkpoint_ok, ui_review,
owner_manual_win, device_accept, blocker, last_log, updated`

Rows are filled only from harness log output. Never type a result by hand.

## Time boxes

- 2 hours per blocker. Then write one line in the `blocker` column and move to the next mission.
- No mission gets more than 1 working day before escalation to the owner.
- Commit at least once a day. Never leave gameplay code uncommitted overnight.
- If a day ends with no row advanced, stop and report why in 5 lines.

## Order of work

1. Prove S002 wins with ARIA on current code.
2. Fix Base Assault pacing on Desert Base (D1) and apply the army-profile starts (D2, D3).
3. Prototype Frontline Control, Breakthrough and Convoy Escort on Desert Base (D6); owner go/no-go.
4. ARIA skills: aircraft, Airport, then one skill per new objective type.
5. Base Assault on all 5 maps, then the three other objectives on the remaining layouts (D4).
6. Per-layout certification of other sizes and difficulties.
7. Operations production starts once the first authored Skirmish mission of each objective type is
   accepted (owner, 2026-11-17).

Lane priority until one authored mission starts and plays to a result (S026 first): only what that
mission needs to start, and S002/S004 acceptance runs. Everything else on the lane waits.

## Honesty rules

No fabricated evidence. Keep failed runs in the table. State pending gates explicitly. Report automated
checks, ARIA runs, owner manual play and device acceptance as separate columns.

## Daily report (10 lines at most)

Rows advanced today, rows failed and why, blockers needing the owner, tomorrow's batch.

## Owner decisions recorded 2026-11-16

- A human must be able to win; ARIA's Unity win is the proxy, checked by the owner's per-layout sample.
- English and Farsi must both win. ARIA must use all units like a human.
- Unnecessary simulator logs are to be removed. Operations waits until all 120 Skirmish missions are done.
- All design changes D1–D7 accepted ("Yes, all"). Jets need a runway; the runway is a placeable building
  that can go on any map.
- D2 starting values, the D3 reachability rule and the D5 tiebreak are recommendations made at the
  owner's request; the owner can veto or change them.
