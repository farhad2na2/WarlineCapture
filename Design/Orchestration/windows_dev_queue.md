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
status: DONE as drafts (branch kept unmerged: area.measurement missing -> W7)
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
status: READY
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
status: READY
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
status: READY
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

## Not yet (HOLD until the Mac lead decides)
- Industrial Basin (S079–S096) and Airport (S097–S120) rows: open source-fit decisions in `decisions_pending.md`.
- Counted ARIA runs on Windows (fps).
