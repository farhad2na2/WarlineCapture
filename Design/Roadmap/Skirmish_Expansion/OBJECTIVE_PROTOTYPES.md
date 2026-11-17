# FC / BT / CE prototypes on Desert Base — lane steps and known gaps (2026-11-17)

Goal (owner): play S007 (Frontline Control), S013 (Breakthrough), S019 (Convoy Escort) from the real Skirmish menu to a result with normal input; ARIA wins are not required. All of this is **candidate** work: nothing below is measured, source-fit certified or player-ready until its step passes. Branch `missions/objective-prototypes`; no physical map assets, only logical configs (`CAMPAIGN_MAP_REUSE_POLICY.md`).

## Lane steps (per mission, in order; Unity only through `Tools/CI/invoke_unity_macos.sh`, never `-batchmode`)

0. **Offline (no Unity), already green:** `python3 Tools/Missions/check_draft.py Tools/Missions/drafts/s0{07,13,19}-*-candidate.json` fails only `area.measurement`; `python3 -m unittest Tools/Missions/test_check_draft.py`; `python3 Tools/Missions/test_drafts.py`. Optional: `python3 -B Tools/Missions/quickcheck.py --missions S007,S013,S019 --sizes Standard,War` (reducer rules on supplied facts; needs the full checkout).
1. **CE only, once:** pin the convoy logical assets (verify-or-create, never replaces a mismatch):
   `Tools/CI/invoke_unity_macos.sh --timeout 600 --log <log> -- -quit -executeMethod Game.Editor.SkirmishConvoyLogicalAssetsAuthoringEntry.EnsurePinnedLogicalAssets`
   pass marker `[SkirmishConvoyLogicalAssetsAuthoringEntry] result=Passed`. (`SkirmishConvoyTruckDefinition.asset` and `SkirmishConvoyRoleCatalog.asset` already exist in `Assets/Game/Resources`, so expect `created=0`.)
2. **Area measurement** (host S002, seed 130365, `en`; fall back to S006 if S002 fails to start; the probe takes bounds from the draft's `campaign_source` when the coded host has no logical map and refuses a host on a different source). No `-quit`:
   `WARLINE_SKIRMISH_CATALOG=S002 WARLINE_S002_SEED=130365 WARLINE_S002_LOCALE=en WARLINE_MISSION_DRAFT=Tools/Missions/drafts/<draft>.json WARLINE_AREA_MEASUREMENT_OUT=Design/AgentReports/SkirmishExpansion/<Sxxx>/area-measurement.json Tools/CI/invoke_unity_macos.sh --timeout 900 --log <log> -- -executeMethod Game.Editor.SkirmishS002AriaRunHarness.RunCatalogAreaMeasurementAndExit`
   marker `[SkirmishAreaMeasurement] catalog=S002 result=Completed ... previewMutated=0`.
3. **Read the JSON; accept only if** (adjust the draft from existing Desert coordinates and re-run otherwise): every area `insidePlayable=true`, `cellsInside>0`, and infantry-passable cells reachable from base/staging of the player (FC: A/B/C also from the enemy side); BT: both corridors and the exit reachable for infantry; CE: destination has truck-footprint cells reachable from the three bays; at least two vehicle routes with `nativePathMetres` (else `floodPathMetres`) non-null and different middles; bays `fitsAsIs`/`fitsRotated` true with truck-footprint cells in the pad; tower pads (BT/CE) `fitsAsIs` true. Write measured sizes into `pads{}` (bay sizes from `truck.footprintMetres`, tower pad from `guardTower.footprintMetres`).
4. **Fill the draft:** add `"area_measurement": {"artifact": "Design/AgentReports/SkirmishExpansion/<Sxxx>/area-measurement.json"}` (the entry hashes the file, so commit it first and never edit it afterwards); change `notes` from `candidate=1, unmeasured` to measured; keep `structure_footprints`.
5. `python3 Tools/Missions/check_draft.py Tools/Missions/drafts/<draft>.json` must PASS (needs the Desert asset readable so `area.bounds` is checked).
6. **Author:** `WARLINE_MISSION_DRAFT=Tools/Missions/drafts/<draft>.json Tools/CI/invoke_unity_macos.sh --timeout 900 --log <log> -- -quit -executeMethod Game.Editor.SkirmishMissionAuthoringEntry.AuthorFromDraft`; marker `[SkirmishMissionAuthoring] result=Passed catalog=<Sxxx> ... publication=InProgress library=Planned`. It writes `Scenarios/<Sxxx>/` (layout, definition, `SkirmishObjective_<Sxxx>.asset` with the matrix deadlines, logical binding with typed areas) and edits only the expansion catalog and publication manifest. A failure names the first failing check (`areas.<size>.<difficulty>`, `instancePads`, `convoy`, …).
7. **Containment:** the lane's standing check — the diff touches only `Assets/Game/Configs/SkirmishExpansion/Scenarios/<Sxxx>`, `SkirmishExpansionCatalog.asset`, `SkirmishPublicationManifest.asset`, the draft and the measurement file; no Campaign/physical map asset, no `Library`/`Packages`/`ProjectSettings`.
8. **Startup run** (no ARIA, no touches): `WARLINE_SKIRMISH_CATALOG=<Sxxx> WARLINE_S002_SEED=<seed> ... -executeMethod Game.Editor.SkirmishS002AriaRunHarness.RunS003StartupAndExit` (generic dispatch through the harness's scoped Editor library); read the startup roster/building audit lines for a clean opening, and for CE the three trucks.
9. **Real-catalog visibility** (owner allowed internal visibility on `missions/*` only): flip `<Sxxx>` to visible with the airport-probe branch's `SetLibraryVisibility` (not yet committed when this was written), then start the mission from the real Skirmish menu in the Editor. Record that the flip is internal and unpublished; the publication row stays `InProgress`.
10. **Owner play** with normal input to a result and back to the menu. Then, separately, the lane may run ARIA/normal-input evidence; nothing here claims it.

## What the owner will see that is known to be unfinished

Common to all three
- **Result text.** The end-reason mapping reaches the HUD, but the copy keys `ui.skirmish.reason.*` are not in the localization catalog (`Tools/Missions/proposed_end_reason_copy.csv` is only a proposal). `UiShellEcsGateway.Skirmish.cs:151` falls back to the raw enum name (e.g. `TicketsDepleted`, `ObjectiveDeadline`), and the headline for non-BA reasons is the neutral `MATCH COMPLETE` (`SkirmishExpandedCopyProjection.cs:223-236`).
- **No Save & Exit.** The product offers Save only for S005 and generic Base Assault (`SkirmishNativePlayerSaveService.cs:47-52`, 74-83); the pause menu shows RESTART MATCH for FC/BT/CE.
- **Menu/briefing copy** for the new missions is catalog fallback text unless the library row carries authored keys; no mission-specific purpose copy was written.
- **Geometry is a hypothesis** until step 3 passes: areas, bays and tower pads are built from existing Desert coordinates only; "reachable" and "fits" are claims of the probe, not of play.
- The harness terminal/menu probes are Base Assault specific; evidence runs for these missions need separate work.

S007 Frontline Control (War recommended, 1200/1500/1800 s by size)
- Three strip/block zones from existing anchors; capture needs dismounted combat infantry for 8 s (neutralise then capture 8+8); public zone/ticket HUD exists (`SkirmishMatchView.PublicObjectiveHud.cs:70`) but is unverified in a native match.
- Enemy allocation to the zones exists (`SkirmishEnemyObjectiveCommanderService.cs:20`) but its behaviour is unobserved; expect passive or lopsided play.

S013 Breakthrough (1080/1500/1800 s)
- Twelve designated riflemen must hold a corridor 20 s, then eight reach the exit and dwell 3 s; end reasons `BreakthroughEvacuated` / `BreakthroughImpossible` / `ObjectiveDeadline` show the raw-name fallback above.
- The two defender towers use placeholder sized pads (10x10) pending the probe; enemy defence policy (`SkirmishEnemyObjectivePublicPerspective.cs:48`) is unobserved; transport/evacuation by vehicle is unproven.
- "Tuned attacker advantage" from the mission plan is not applied: the army profile is the packaged matrix row.

S019 Convoy Escort (1080/1500/1800 s)
- **The deadline is the fixed size deadline**, not the planned measured-route-time deadline (`SkirmishConvoyReducer.cs:62`); the probe's `nominalTruckSeconds` is for judging whether it is fair, nothing consumes it.
- Truck bay sizes (4.6 m wide) are hypotheses; truck commands, repair/service (`service.player`) and enemy patrol/interdiction (`SkirmishEnemyObjectiveCommanderService.cs:47,155`) are unobserved in a native match. Three trucks are selectable objective trucks separate from Supply; two must deliver, dwelling 5 s.
- Needs step 1 on a clean Editor before authoring.
