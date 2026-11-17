# Lane worker 14 (integration shift 14) 2026-11-17
Logs: /Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-17/lane14 (percent of 1080 s)
- 1 Merged menu-journey-generic, then aria-coverage-8 (with 7), --no-ff. Python 92/92 OK. Compile Passed (compile.log:1975, 0 error CS). 3 metas committed.
- 2 S006 re-authored Passed (author/unity.log:678); containment Passed failures=0 rows=268 (cont-S006/unity.log:759). Committed.
- 3 Queue A --menu-journey (qa/unity.log), every job MenuJourney result=Passed path=Hotspot>BattleLibrary(scroll)>ScenarioCard_<id>>LaunchMissionButton input=Touch matchStartedFromMenu=1 (:2623 S004, :371735, :430866, :682652, :960104).
  S004 warm-up Victory 232 s (not counted).
  S002 155923 en: Defeat MainBaseDestroyed 142.7 s (13.2%); gate-3 unused=apc_fast;breacher;marksman;radar (:428986); frozen Passed (:429035).
  S002 130365 en: Victory 269.3 s (24.9%); unused empty (:680810); frozen Passed (:680826).
  S002 104731 en: Victory 267.0 s (24.7%); unused empty (:958722); frozen Passed (:958739).
  S002 130365 fa-IR: Victory 266.8 s (24.7%); unused empty (:1201685); frozen Passed (:1201702).
  No research/build/support_order gave_up lines. S002 type-first set: 3/4 wins, not accepted (155923 lost).
- 4 Queue B (qb/unity.log), no startup aborts: S026 155923 Victory 191.9 s (17.8%) unused=apc_heavy;drone;siege;transport_heli (:44558); 130365 Victory 166.1 s (15.4%) unused=apc_heavy;drone;radar;siege;transport_heli (:117209); 104731 Victory 178.7 s (16.5%) unused=apc_heavy;drone;siege;transport_heli (:206245); frozen Passed on all. S049 Victory 122.3 s (11.3%) unused=helipad;satellite_dish;apc_fast;apc_heavy;drone;gunner;radar;siege;tank;tanker;transport_heli (:273479). S050 Abort 423.6 s (39.2%) ariaStopped:Blocked (:457699), no gate line. S006 Abort 541.6 s (50.1%) ariaStopped:Blocked (:516895), started (no startup abort), no gate line. No event=blocked lines.
