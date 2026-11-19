# Windows development queue

The Mac program lead edits this file. The Windows dev lead reads it (from `origin/missions/integration`), works
tasks in order, and never edits it. Rules: `WINDOWS_DEV_PROMPT.md`. Codex model `gpt-6.2-sol`, effort per task.
Status: `READY` (take it), `TAKEN by windows <date>` is written only in your results file, `DONE` / `HOLD` set by the Mac lead.

Background for every draft task: drafts live in `Tools/Missions/drafts/` and are checked with
`python -B Tools/Missions/check_draft.py <draft>` (a PASS is a draft preflight, not Unity validation) and
`python -B Tools/Missions/quickcheck.py --missions <ids>`. Read `Tools/Missions/README.md`,
`Design/MapVariants/CAMPAIGN_MAP_REUSE_POLICY.md`, `Tools/CI/skirmish_catalog_diagnostics.md` and the template drafts named
below first. Row data (map source, objective, army A/G/C, start F/E, layout id, budget template) is in
`Design/Orchestration/missions.csv`. Siblings of one map + objective share the measured layout of their template and
differ by army and start; copy the template's measured geometry, never invent coordinates. Anything the template marks
as a candidate or unmeasured stays marked so.

## W1 — Desert Base Frontline Control siblings S008–S012 (2026-11-18)
status: DONE (merged 6c1ff8fcd; Mac check_draft 5/5, quickcheck passed)
codex effort: medium
template: `Tools/Missions/drafts/s007-desert-base-fc-ground-field-candidate.json`
deliverable: one draft per row S008, S009, S010, S011, S012 (army and start from missions.csv), each passing
check_draft.py, plus one quickcheck run over S007–S012 at Standard. Branch `missions/win-w1-db-fc`.

## W2 — Desert Base Breakthrough siblings S014–S018 (2026-11-18)
status: DONE (merged 6c1ff8fcd; Mac check_draft 5/5, quickcheck passed)
codex effort: medium
template: `Tools/Missions/drafts/s013-desert-base-bt-ground-field-candidate.json`
deliverable: as W1 for S014–S018. Branch `missions/win-w2-db-bt`.

## W3 — Desert Base Convoy Escort siblings S020–S024 (2026-11-18)
status: DONE (merged 6c1ff8fcd; Mac check_draft 5/5, quickcheck passed)
codex effort: medium
template: `Tools/Missions/drafts/s019-desert-base-ce-ground-field-candidate.json`
deliverable: as W1 for S020–S024. Branch `missions/win-w3-db-ce`.

## W4 — City Crossroads Frontline Control S031–S036 (2026-11-18)
status: DONE (64ba63811; merged aee6ee07c with W7)
codex effort: high
templates: the S007 FC draft (structure) and `s026-city-crossroads-ba-ground-established.json` (City Crossroads source,
bases and measured anchors).
deliverable: S031 first as the family template: three control points, routes and stagings placed only on anchors and
positions the S026 draft or a Unity layout dump already measured on City Crossroads (`Tools/Missions/layout_dump_to_draft.py`).
Where a needed position is not measured, mark it unmeasured and list it. Then S032–S036 as siblings. check_draft.py on all,
quickcheck S031–S036 Standard. In Unity on Windows: the compile check, then any SkirmishMissionAuthoringEntry diagnostics
method the catalog docs name for a draft (read-only; do not write assets). Branch `missions/win-w4-cc-fc`.

## W5 — Mountain Pass Frontline Control S055–S060 (2026-11-18)
status: DONE as drafts (branch kept unmerged: area.measurement missing -> W8)
codex effort: high
templates: the S007 FC draft and `s049-mountain-pass-ba-ground-field.json`. Same method as W4. Branch `missions/win-w5-mp-fc`.

## W6 — Author and contain the 15 Desert Base sibling drafts (2026-11-18)
status: DONE (merged 652e88722 + results fb12cdaaa; CE-E startup failure -> W14)
codex effort: medium (scripting the runs); you run Unity yourself, serially, in the main Windows checkout
drafts: S008–S012, S014–S018, S020–S024 (on missions/integration since 6c1ff8fcd)
deliverable per row, in this order, each with its marker (Windows wrappers; same methods as the Mac steps in
`WarlineCapture-program-logs` lane 22, quoted here):
  b. `python -B Tools/Missions/pad_preflight.py --draft <draft>`, then `WARLINE_MISSION_DRAFT=<draft>` with
     `-quit -executeMethod Game.Editor.SkirmishMissionAuthoringEntry.AuthorFromDraft` → `[SkirmishMissionAuthoring] result=Passed catalog=<id>`
     (check the exact entry name in `Assets/Game/Scripts/Editor`; the S007 run used it).
  c. `WARLINE_MISSION_DRAFT=<draft>` with `-quit -executeMethod Game.Tests.Editor.SkirmishInstancePadContainmentTests.RunAuthoredLayoutValidation`
     → `[SkirmishInstancePadContainment] catalog=<id> result=Passed`.
  d. One NON-counted menu launch per row (`run_aria_queue.py --launcher windows --jobs <id>:130365:en --menu-journey --no-table`):
     record the menu-journey line, any `expandedStartupFailure`, `[StartingGrantPlacement] failed ... preferredFirstReject=`,
     result line and clock. A win is not required; fps does not matter here.
Commit only the authored assets for these rows (with their .meta files) and the results file on `missions/win-w6-db-author`.
Do not change catalog visibility. A row whose b or c fails twice: record the first failing line, leave its assets out, go on.

## W7 — City Crossroads FC area measurement and fixes, S031 then S032–S036 (2026-11-18)
status: DONE (985c00437; merged aee6ee07c; Mac quickcheck/counted runs next)
codex effort: high (draft fixes); Unity serial in the main checkout
Measure on the City Crossroads host match: `WARLINE_SKIRMISH_CATALOG=S026 WARLINE_S002_SEED=130365 WARLINE_S002_LOCALE=en
WARLINE_MISSION_DRAFT=<s031 draft> WARLINE_AREA_MEASUREMENT_OUT=Design/AgentReports/SkirmishExpansion/S031/area-measurement-r1.json`,
Play mode (no -quit), `-executeMethod Game.Editor.SkirmishS002AriaRunHarness.RunCatalogAreaMeasurementAndExit`. Fix the open items you
found (FlankNorth/FlankSouth sharing S026 waypoints 00–06; FrontlineB 19 m x 59 m) using only measured positions, re-measure,
set `area_measurement.artifact` and the `candidate=1, measured <date>` note, check_draft must PASS. Siblings reuse S031's
measurement. Then W6 steps b–d for S031–S036. Branch `missions/win-w4-cc-fc` (continue it).

## W8 — Mountain Pass FC area measurement and fixes, S055–S060 (2026-11-18)
status: READY
As W7 on the Mountain Pass host (`WARLINE_SKIRMISH_CATALOG=S050`, the same Citywide Alert binding the S049/S050 drafts use;
the missions.csv "Armor Break approach sector" text is stale; follow the drafts). Branch `missions/win-w5-mp-fc` (continue it).

## W9 — City Crossroads Breakthrough S037–S042 and Convoy Escort S043–S048 drafts (2026-11-18)
status: READY
codex effort: high. Templates: S013 BT / S019 CE drafts (structure) + S026 (source, bases, measured anchors). S037 and S043 first
as family templates, then siblings. Measure as W7 (host S026). Branches `missions/win-w9-cc-bt`, `missions/win-w9-cc-ce`.

## W10 — Mountain Pass Breakthrough S061–S066 and Convoy Escort S067–S072 drafts (2026-11-18)
status: READY
codex effort: high. As W9 on Mountain Pass (host S050). Branches `missions/win-w10-mp-bt`, `missions/win-w10-mp-ce`.

Order: W6 and W7 first (Unity serial: W6 runs, then W7 measurements); Codex can draft W9/W10 in parallel meanwhile.
quickcheck needs the .NET 8 SDK, which Windows lacks; the Mac lead runs quickcheck on your branches. Do not install software.

## W11 — SkirmishExpandedArmyTests paging failure (2026-11-18)
status: DONE (472ab340c; merged 1772a51b4)
codex effort: high
Mac lane 31 on integration 2529f7ee7: 15/16 pass; `FiveSlotPageSelectsFifthGroupAndPagingPreservesSelectionAndOrders` fails
"pageCount Expected: 2 But was: 3" (SkirmishExpandedArmyTests.cs:410), both in RunFocusedValidation and EditMode. The fixture
now loads packaged S002 v2 (Codex 039). Decide with evidence whether the test's expectation (2 pages) is stale for the S002 v2
roster or the tray paging is wrong; fix the right side minimally, never weaken the test. Prove with
`Game.Tests.Editor.SkirmishExpandedArmyTests.RunFocusedValidation` → `[SkirmishExpandedArmyTests] result=Passed` on Windows.
Branch `missions/win-w11-army-paging`.

## W12 — S002 ARIA stall on the second base alert, reason 114 (2026-11-18)
status: DONE (e81b2acd8; merged 1772a51b4; counted S002 runs on the Mac follow)
codex effort: xhigh
Evidence: `Design/AgentReports/SkirmishExpansion/S002/_Evidence/lane31-second-defense-stall-130365-en.txt`. The first defense now
selects before Attack (Codex 034 fix works; base held at 520). On the second alert (t=238.5, attacker shown, defenderSlot=-1)
home defense escalated to group=all with homeGroundRefusal=2/7, then `[AriaBlocked] reason=114 kind=Unavailable intent=DefendBase`
with `control=Attack selectedMask=15 homeDefenseStage=5`. Find why the Attack target became unavailable with four groups selected
(code: Assets/Game/Scripts/Simulation/AriaHomeDefensePolicy.cs, AriaGroundBaseAssaultPolicy.cs, UI/Shell/Ecs/AriaPlayInputSystem.cs,
AriaSkirmishPlanSystem.cs) and fix ARIA so it re-plans (e.g. aims at the visible attacker or the home ground it can tap) instead of
blocking. Public player controls only; fog of war stays. Add an EditMode test that reproduces the stage-5 state and passes on
Windows. Counted S002 matches stay on the Mac. Branch `missions/win-w12-s002-second-defense`.

## W13 — Starting structures and enemy construction placed on map runways (2026-11-18)
status: DONE (65bcf4d15; merged e11d22253; only S004 enemy base moves, S004 needs counted re-runs)
codex effort: xhigh
Owner saw an ENEMY refinery on an airport runway in a Mac Editor match. Traced (Windows + Mac, read-only, computed not measured):
S004 (CityEdgeAirfield) enemy refinery preferred rect x 894..919, z 562..577 covers ~225 of 375 m2 of the east runway end
(runway x 490.66..909.34, z 543.35..576.65, `PreparedEntities.unity:84951/:1539684`, Candidate `output-manifest.json:180427`); the enemy
barracks/HQ (x 888..916, z 533..548) overlaps the runway's south edge ~99 m2, enemy ground staging ~9 m2. Cause: `MapVariantCityAirfield.cs:136-139`
leaves the runway unreserved; the baked surface clears `MapSurfaceMovementMask.BuildingPlacement` on runway cells (decoded Candidate
`Surface.asset`, mask 15) but no placement code reads that bit (only `MapSurfaceSlopeClassifier.cs:48`); Plaza(6) is not a road in
`BuildingPlacementRoadSurfaceMask.cs:69-71`; starting grants use RequirePreferredOrigin=0 and accept the preferred cell
(`SkirmishStartingBuildingsService.cs:79-91`, `BuildingRuntimeProcessingCompositionSystemHelper.cs:453-487`).
Fix: make shared building placement (starting grants AND enemy/player construction) reject cells whose surface lacks
`BuildingPlacement`, so the nearest legal footprint is used; keep every existing check; no map/terrain/runway asset changes, no stat
changes. Also pass rotateVertical through at `BuildingRuntimeQueryCompositionSystemHelper.cs` ~:135 if the trace there holds.
Add a successful-placement log line for starting structures (faction, structure, preferred and actual origin), and a
runway/BuildingPlacement-bit check to `Tools/Missions/pad_preflight.py`. Tests: an EditMode test reproducing the S004 enemy refinery
case (refused on runway cells, placed clear of them), plus `RunFocusedValidation` for the placement tests you touch, the compile check, and
the Python tests. Report which other catalog rows' starting structures move (any whose actual origin changes). Counted matches stay on the Mac.
Branch `missions/win-w13-runway-placement`.

## W14 — Convoy Escort established starts abort: player convoy truck BlockedSpawn, S020/S022/S024 (2026-11-18)
status: DONE (0a6347e55; merged 94ec16fee; S019 keeps old bay overlap, fine for field roster)
codex effort: xhigh
Evidence (Windows W6 menu launches, `windows_dev_results/2026-11-18.md`): ce\unity.log:1491/7739/13898
`[SkirmishStartingForce] failed prefab=Unit_Veh_Truck_Tray faction=1 member=0`, then `expandedStartupFailure=BlockedSpawn`. Only the three
established (E) CE rows fail; field (F) CE rows S021/S023 and all FC/BT E rows start. Find with evidence why the convoy truck has no legal
spawn in CE-E (compiler-derived established pads, the larger established force taking the vehicle spawn pad / convoy.origin cells first,
or the established structures' footprints), and fix it minimally in the draft / authoring data or the shared compiler — using only measured
positions, no map changes, no starting-force or stat changes. Check whether S019 (Mac-authored CE-G-F template) is affected the same way at
established start. Re-author the affected rows (W6 steps b–c), then a menu launch for S020/S022/S024 with `result=Passed` menu journey and no
`expandedStartupFailure`. Add an EditMode or authoring test that fails on today's S020 layout. Branch `missions/win-w14-ce-established-spawn`.

## W15 — SkirmishExpandedAriaTests: EnemyStructureDefendersReactToVisibleUnarmedIntrusionAndResumeBaseObjective (2026-11-18)
status: DONE (e7a115bb2; merged deed06125)
codex effort: high
Windows W12 evidence: fails identically on base ca5b7ccbb and on W12 ("Expected Entity(47:9) But was Entity(34:9)"), so it predates W12 and
keeps `[SkirmishExpandedAriaTests] result=Passed` red. Decide with evidence whether the test's expected entity is stale (fixture/roster change)
or enemy structure-defender targeting is wrong; fix the right side minimally, never weaken the test, no stat changes. Prove with
`Game.Tests.Editor.SkirmishExpandedAriaTests.RunFocusedValidation` → `[SkirmishExpandedAriaTests] result=Passed` on Windows, cut from
missions/integration after W11/W12 are merged. Branch `missions/win-w15-structure-defenders`.

## W16 — Frontline Control established starts lose: S008/S010/S012 (2026-11-18)
status: READY
codex effort: xhigh
Mac lane 33 (full fps, seed 130365 en, `program-logs/2026-11-18/lane33/q/unity.log`): S009/S011 (field) Victory at 49% of deadline;
S008 Defeat on tickets 614.8 s zones 1/2 (:11332), S010 Defeat 863.2 s zones 0/3 (:36088), S012 Defeat 617.8 s (:54188). Windows W6 saw
the same, so it is not fps. ARIA chose Hold 0 times on S008/S012 (only Move ControlZone / ObserveObjective) but 9 and 16 times on the wins.
First diagnose with evidence (AGENTS.md: check input, spawning and placement against the current budget before tuning): does the
established roster spawn and get orders, why does the FC objective planner never hold captured zones on these rows. Fix ARIA objective
tactics if that is the defect; otherwise tune only starting forces/resources/deadline per the map reuse policy budget guidance (owner
direction 2026-11-06). No unit-stat or rule changes, no map changes. Windows proves with check_draft/quickcheck + the FC/ARIA EditMode
tests it touches; Mac runs the counted queue. Branch `missions/win-w16-fc-established`. Per-task results file.
Update 2026-11-19, Mac lanes 38/39 (130365 en, `program-logs/2026-11-18/lane38|lane39/q/unity.log`, extract.txt in each). This is not only
established rows. City Crossroads S031-S036 won 6/6 with ARIA holding throughout, 2-10 Holds per row. Mountain Pass S055 (field!), S056, S058 and
S060 lost on tickets at ~50%; S057/S059 won. On every MP row ARIA took both zones by ~45 s. On the losers it chose Hold only once (t=39-46 s,
lane39 :4698 :42390 :66405) and then only Move ControlZone, losing both zones by 108-150 s and never retaking them. The winners chose Hold again
after 90 s (S057 t=118-246, S059 t=93 :60264). Look at why the FC planner stops re-choosing Hold on owned zones under pressure, and at map-specific
inputs (zone radius/threat range) that make City Crossroads hold and MP/DB not. Include S055/S056/S058/S060 in the proof rows.

## W17 — Burst BC1016: managed byte[] in AriaCoverageRecruitPolicy.TryStart (2026-11-18)
status: READY
codex effort: high
Windows W15 log: Burst error BC1016 at `Assets/Game/Scripts/Simulation/AriaExpandedPreparationPolicy.cs:174`. `TryStart(..., byte[] order, ...)`
(:177) receives the `static readonly byte[] Order`/`AirPriority` arrays (:136, :139) as managed parameters inside Burst-compiled
AriaSkirmishPlanSystem/AriaPlayDecisionSystem (from a7c6eeabe). The Editor silently falls back to managed code; device/AOT Burst may fail.
Make it Burst-compatible with identical behaviour and priority order (e.g. pass a FixedList/unsafe pointer, or index the static readonly
arrays directly without passing them). No behaviour change. Grep the other Aria*Policy files for the same pattern and fix those too.
Prove: Editor log with no BC10xx for Aria* after a domain reload, compile marker, and `Game.Tests.Editor.SkirmishExpandedAriaTests`
EditMode all passing. Branch `missions/win-w17-aria-burst`. Per-task results file.

## W18 — Building readiness red on base: BuildingFootprintAudit EvidenceChain_Clinic + 6 older building EditMode failures (2026-11-18)
status: READY
codex effort: high
Windows W13 evidence (`windows_dev_results/2026-11-18-w13-runway-placement.md`): `BuildingFootprintReadinessValidation.Run` fails on base
7970b1c54 and on W13 at its first step `BuildingFootprintAudit.Run`: "EvidenceChain_Clinic: footprint must enclose the model with less than one
cell of rounding", so `[BuildingFootprintReadiness] result=Passed` is unreachable. Also failing on base: CanonicalBarracksRuntimeAndCatalogFootprintsFitTheM2Lot,
3x BuildingProductionQueueCompositionSystemHelperTests, SkirmishStartingBuildingsTests.StartingGrantsWaitForSharedResultsAndBindTheActualBaseOnce
(componentMissing), GroundMissileLauncherRuntimeTests.AttackOrder…. For each, decide with evidence: stale test/fixture vs real defect; fix the right
side minimally, never weaken a test, no stat changes, no model/prefab edits under Assets/Game/Prefabs/UI. If the Clinic footprint is a real
authored-footprint mismatch, report the numbers and stop for the Mac lead rather than resizing. Prove: the marker above on Windows, plus the
UTF `-testFilter Building` set with 0 failures. Branch `missions/win-w18-building-readiness`, cut after W13 is merged. Per-task results file.

## W19 — S049 (Mountain Pass BA) ARIA stalls with reason 101 on 2 of 4 seeds (2026-11-18)
status: READY
codex effort: xhigh
Mac lane 37 (`program-logs/2026-11-18/lane37/counted/unity.log`): 155923 en Abort ariaStopped:Blocked at 353 s `[AriaBlocked] reason=101 kind=Waiting
intent=Inspect` (:88940); 104731 en same at 323 s with intent=Recruit (:269540). 130365 en (smoke/unity.log:279722) and 130365 fa-IR (:382455) win.
Gate-3 unused on the wins: helipad, satellite_dish, apc_heavy, drone, gunner, radar, siege, tank, transport_heli (:279707).
Diagnose reason 101 from the log context (what ARIA waited on, why nothing was offered), fix ARIA (or the S049 data if a placement/spawn is wrong);
no stat/rule changes. Prove with a unit test reproducing the stall plus the ARIA EditMode suites. Shared code: owner OK in the Windows session.
Branch `missions/win-w19-s049-reason101`. Per-task results file.
Update 2026-11-19, Windows W9 CE: the field rows S043/S045/S047 abort the same way: ariaStopped:Blocked `[AriaBlocked] reason=101 kind=Waiting
intent=ObserveObjective` at actions 69-88, match ~176-223 s (`windows_dev_results/2026-11-18-w9-cc-ce-measure.md`). Include them in the proof rows.

## W20 — S003 (Desert Base BA, army A field) ARIA loses its base without starting home defense (2026-11-18)
status: READY
codex effort: xhigh
Mac lane 37 (`program-logs/2026-11-18/lane37/smoke-s003/unity.log`), seed 130366 en: Defeat MainBaseDestroyed at 185 s (17% of deadline, :477593).
First base_alert at t=162.8 with base 800 hp (:411875), 778 at t=174.5 (:442646); homeDefenseStage stayed 0; enemy base untouched at 800 (:477150).
Find why the home-defense policy never left stage 0 on S003 (S002 uses the same policy and reaches stage 5) and fix it; no stat/rule changes.
Prove with a unit test reproducing the S003 alert sequence plus the ARIA EditMode suites. Shared code: owner OK in the Windows session.
Branch `missions/win-w20-s003-home-defense`. Per-task results file. S003 first-visit seeds are 104732, 130366, 155924.

## W21 — ARIA queue cannot dispatch coded legacy presets S001/S025 (2026-11-18)
status: READY
codex effort: high
Mac lane 37 (`program-logs/2026-11-18/lane37/smoke/unity.log:601, :1064`): `launchFailed=1 error=Catalog S001 does not resolve to a definition in the
expansion catalog` (same for S025), with and without --validation-library. S001/S025 are coded presets with no SkirmishScenario asset. Give the
queue/harness a dispatch path for coded presets (the way the menu launches them), with no change to the presets themselves. Tooling/harness only;
prove with the queue's Python tests and a Windows menu-journey launch of S001 and S025. Branch `missions/win-w21-preset-dispatch`. Per-task results file.

## W22 — pad_preflight misses objective/defense pads on non-placeable cells (2026-11-18)
status: READY (after W9/W10)
codex effort: high
Windows W9 run 2: all 12 BT/CE menu launches aborted `expandedStartupFailure=BlockedSpawn`. The enemy guard towers for defense.approach sat on
S027-derived anchors that land on the S026 road band (z 348-360, no BuildingPlacement). pad_preflight skips objective pads, containment is
envelope-only, and r2 reported fitsAsIs=true, so no check caught it. Extend `pad_preflight.py` (and `--surface`, from W13) to check every
objective, defense and convoy pad that spawns a structure against the bitmap's BuildingPlacement flag (and road/sidewalk flags). It must
report which pad failed and where. Then run it over every merged draft (`git ls-files Tools/Missions/drafts`) and list any failure, without
fixing drafts in this task. Tooling only; prove with new Python tests (a known-bad W9 r2 pad must fail, the W9 fix must pass) and the full
`Tools/Missions` unittest run. Branch `missions/win-w22-preflight-objective-pads`. Per-task results file.
Update 2026-11-19: Windows' interim checker `notes/objective_pad_surface.py` (defense.* 3x4 tower and 10x10 pad vs bitmap flags and instance
pads) is the starting point. Bitmap origin = gridOrigin + bitmapOriginCell x cellSize. S026's gridOrigin is (0,0), which hides an origin bug, so
test on S050 (gridOrigin 860,300) too. It already caught road + not-level towers (flag 17) on all 12 W10 Mountain Pass drafts.

## W23 — City Crossroads BT exit next to the enemy base: S038/S039/S040/S042 BreakthroughImpossible (2026-11-19)
status: READY (W9 BT merged first)
codex effort: high
Windows W9 BT (`windows_dev_results/2026-11-18-w9-cc-bt-measure.md`): S037/S041 won by BreakthroughEvacuated 8/8 at ~20%, but S038/S039/S040/S042 lost by
BreakthroughImpossible, 1/8 completed and 5 lost at 14-22%. The rifle squad dies at (1076-1080, 399-417), inside BreakthroughExit (1064..1094.5,
374.5..409.5), built from S027 air.enemy plus the enemy rally. That is ~19 m from the enemy watchtower (1098,405) and next to the barracks (1116,380).
Move the BT exit (and the route end) on the S026 geometry away from the enemy base's defensive reach: a logical anchor/area change only, on legal
vehicle-passable cells, with no map, stat or rule change and no change to enemy structures. Then re-measure r3 on S037 and rebind the six BT drafts.
Prove with check_draft 6/6, preflight 6/6 (with W22's objective-pad check if merged), authoring/containment 6/6 and non-counted Windows menu runs of
all six. Report the new exit's distance to every enemy defensive structure. Branch `missions/win-w23-cc-bt-exit`. Per-task results file.
Update 2026-11-19, widened after Windows W10 (`windows_dev_results/2026-11-18-w10-mp-bt-measure.md`, `-w10-mp-ce-measure.md`). The same class of
defect is on Mountain Pass. BT S062/S064/S065/S066 lost BreakthroughImpossible 0/8 (exit ~1 m from enemy staging, 8 m from the enemy base). CE S067/S068/
S069/S071 lost ConvoyImpossible 0/2 (ConvoyDestination (1186..1202, 354..398) is ~2 m from the enemy barracks pad, 6 m from staging, 5 m from
rally.enemy; trucks lost at 72-110 s). Scope W23 to all three families: CC BT S037-S042, MP BT S061-S066 and MP CE S067-S072. Move each BT exit or CE
destination (plus the route end) clear of the enemy base, re-measure r3 on S037/S061/S067 and rebind. First add a small read-only check (in W22's
tool or alongside it) that prints, for every merged BT/CE draft, the exit or destination distance to each enemy structure and staging/rally anchor.
Use it as the acceptance check. Run it over the Desert Base BT/CE drafts too and list them; do not change those. One branch per family is fine:
`missions/win-w23-cc-bt-exit`, `-mp-bt-exit`, `-mp-ce-dest`. Data/anchor change only, no shared code.

## W23b — BT exits / CE destinations: move inward from the east play edge (2026-11-19)
status: DONE for CC BT (a692cf316 accepted, 6/6 Victory; Mac merges it when its Editor is free). MP BT 36c18a508 and MP CE 59599aba4 not merged: continue in W23c.
codex effort: high
W23 (`windows_dev_results/2026-11-18-w23-*.md`, branches 621ee1092 / b1bb762ea / e4ebfe2df) moved the endpoints clear of the enemy bases (CC BT 124 m,
MP 39.5-41 m), but ARIA now stops itself. Its objective Move to the new endpoint fails 6 times (`order_blocked failures=6`), then AriaBlocked
reason 101/111 at ~290-350 s, 0 evacuated/delivered. That breaks rows that won before (S037/S041, S061/S063). Both new endpoints sit at the
east edge of the playable area (x up to 1296/1300 and 1290/1292). Hypothesis: the endpoint ends up under the right HUD (play area ends at screen x
1444/1920) and ARIA cannot tap it. First, read the ARIA order-target and camera-framing code read-only to confirm or refute this (reading is not
a code change); write the finding in the results file. Then, data only: move each endpoint inward to a spot at least ~40 m inside the playable
bounds on every side and still clear of enemy defensive structures (keep the W23 distance check; report the nearest-enemy and nearest-edge
distance). Re-measure (S037 r5, S061 r4, S067 r4), rebind, and rerun the 18 non-counted menu runs. Accept only if order_blocked/AriaBlocked fall
back to 0 and the rows that won before W23 win again. Branches: continue the three W23 branches. No shared code changes.

## W24 — S070/S072 (Mountain Pass CE established, air/combined) abort: enemy transport helicopter BlockedSpawn (2026-11-19)
status: READY
codex effort: xhigh
Windows W10 CE: S070 and S072 abort with expandedStartupFailure=BlockedSpawn after every enemy building is placed. The log shows `[SkirmishStartingForce] failed
prefab=Unit_Veh_Helicopter_Transport faction=2 member=0`. The same helipad (AirReturn (1261,467.5)), army profile and start package launch fine in S058 and
S064/S066, and CE S046/S048 on S026 spawn the same helicopter. Diagnose first: what differs at spawn time on S070/S072 (CE destination/convoy pads
overlapping the air spawn cell, f2 satellite dish at (1173,447), occupancy from the moved defense tower). Extend the startup-allocator capture beyond S002 if
needed (harness/tooling). Fix the draft data if that is the cause; a shared-code fix needs the owner's OK in the Windows session. Prove with a menu launch of
S070/S072 with expandedStartupFailure=0. Branch `missions/win-w24-s070-heli-spawn`. Per-task results file.

## W23c — Mountain Pass BT S061-S066 / CE S067-S072: endpoint past the defenses, tappable (2026-11-19)
status: READY (after W24's read-only diagnosis; build on the two W23b MP branches)
codex effort: xhigh
W23b fixed the tap (order_blocked 0) but moved the MP exit/destination in front of the defense line: BT never opens the exit (0/6 wins, S061 Blocked 115),
CE wins in 64-68 s (6% of the deadline, under the 20% gate-2 floor) with no escort challenge. Decision (program lead): data only, option 1 of your list;
no shared code (option 3 also leaves players with an untappable objective under the HUD), and no source change yet.
1. Margins per edge, not 40 m on every side. From the W23b code read, derive the ground margin each edge needs for the tap point to land in the
   safe area and outside every HUD raycast rect at the camera heights ARIA uses: east (right HUD) ~60 m; north/south/west from the actual top/bottom/left
   HUD rects (likely much less). Write the derivation and the numbers in the results file.
2. With those margins, scan for an exit/destination rectangle that lies past both corridors (a column must cross Corridor A or B to reach it), keeps the
   W23 distance check (>=30 m from every enemy structure, staging and rally anchor), and is on passable measured cells.
3. If none exists, move the corridors instead (inside the measured passable area, between the start and the W23b endpoint, still covered by the
   defenses), re-measure and rebind. Keep the defense pads on legal cells (W22 rule).
4. If neither works, do not pick another source: record the measured depth/clearance that fails, list existing Campaign sources that would fit, and stop.
Re-measure (S061 r5, S067 r5), rebind, rerun the 18 non-counted menu runs. Diagnose S061 Blocked 115 (Control press during OpenObjectiveCorridor) and
S068 Blocked 101 from those logs. Accept only if: order_blocked 0, AriaBlocked 0, exitOpen=1 on BT wins, S061 and S063 win again, and CE wins land at
>=20% of the deadline. Branches: continue `missions/win-w23-mp-bt-exit` and `-mp-ce-dest`. Per-task results file.

## W25 — S021/S023 (Desert Base CE field) abort on seed 155923: player convoy truck BlockedSpawn (2026-11-19)
status: READY (after W23c)
codex effort: high
Mac lane 41 (`program-logs/2026-11-19/lane41/a/unity.log`): S021 at seed 155923 en aborts 24 s into the job, before the match:
`[SkirmishStartingForce] failed prefab=Unit_Veh_Truck_Tray faction=1 member=0` (:416162) then `aborted reason=expandedStartupFailure=BlockedSpawn` (:416187).
S023 the same (:448590/:448614). Both rows win on 104731 en and 130365 fa-IR; S020/S022/S024 win on all seeds. W14 fixed the established
rows; this is the field rows on one seed. Diagnose what occupies the truck spawn cell on that seed (seeded enemy/ambient placement, field start
package), fix the draft data, prove with menu launches of S021/S023 at 155923 en with expandedStartupFailure=0. Branch `missions/win-w25-db-ce-field-spawn`.

## Not yet (HOLD until the Mac lead decides)
- Industrial Basin (S079–S096) and Airport (S097–S120) rows: open source-fit decisions in `decisions_pending.md`.
- Counted ARIA runs on Windows (fps).
