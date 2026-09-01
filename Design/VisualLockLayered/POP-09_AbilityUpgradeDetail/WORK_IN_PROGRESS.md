# POP-09 Ability / Upgrade Detail — Work in Progress

Current candidate: Iteration 2, pending clean dual-aspect Play Mode validation.

The legacy prefab was rejected because unresolved raster references rendered
the screen as a nearly solid white field. The replacement prefab now matches
the target's APC Armor Upgrade state with:

- one centered responsive 1110x783 modal;
- the existing `Portrait_Unit_Veh_APC_Heavy_Card_512.png` portrait reused in an
  aspect-fill masked viewport, with no duplicate unit art;
- three effects, availability/requirements, prerequisite, current-tier, and
  two-action footer regions;
- procedural directional gradients and one 3 px border contract;
- supported procedural completion check and a clean shared lock icon;
- functional Close, View Source, locked, and unlocked action states.

Deterministic renders at 1920x1080 and 4800x2160 pass, and
`[AbilityUpgradeDetailV3Validation] result=Passed tests=3` is recorded under
`work_in_progress/iteration_02_pending_live/`.

The first 1920x1080 live route produced the correct exact-size image and a
route pass marker, but Unity hung during Android ADB shutdown; the checked
wrapper therefore timed out and the run is not accepted. Two later wrapper
runs stalled before project initialization while the macOS GUI session was
confirmed locked. The 4800x2160 live image is still pending. Do not move this
candidate into `iterations/` or call it review-frozen until both checked
wrapper runs exit cleanly after the Mac is unlocked.
