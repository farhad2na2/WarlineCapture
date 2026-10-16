# ARIA camera navigation for offscreen Skirmish approaches

The S005 normal-input diagnostic found a public25m approach point outside the viewport after focusing the enemy base at the valid18m/65-degree/35-degree/FOV46 camera profile. Three repeated base-focus clicks ended Blocked. The trace contained no HUD raycast obstruction at that point. Evidence: Offline/S005AriaStopRepairDraft/native-Defense-FindBaseDiagnostic-20261016-080552-78faba06a8534fb9a302724b0b865975/find-base.jsonl under Logs/SkirmishProduction.

Generic Skirmish approach observation now offers the existing map control when a ground target is unavailable and observes its focus gesture while expanded. The expanded planner opens the map, performs one public camera focus/viewport drag, closes the map and settles before offering a separate ordinary advance action. City Crossroads and Industrial Basin special paths remain intact. Camera height/FOV, Campaign physical geometry and fixed gameplay budgets are unchanged.

Validation completed through the checked Windows InvokeUnity wrapper, GUI licensing, recorded Editor6000.5.8f1 and Android target. Native17 evidence is in Logs/SkirmishProduction/LowMemoryMode/AdvanceMapFocus-01/native-17, including complete logs, source/tool/asset pins, owner and memory observations, verified-result.json and worker restoration receipt. The wrapper/caller exited0, all24 pinned endpoints remained unchanged, and these native markers passed:

- [SkirmishS002AriaHarnessTests] result=Passed
- [SkirmishExpandedAriaTests] result=Passed
- [SkirmishAdvanceMapControls] result=Passed controls=4 surface=2400x1080 cameraOrders=0
- [SkirmishAdvanceMapValidation] result=Passed tests=4 expanded=1 cameraOrders=0 nativeMissionAcceptance=Pending
- [SkirmishAdvanceMapNativeExit] result=Passed restored=1

The existing ledger suites ran in Edit Mode, their intended test mode. Their isolated profiles now read exact reviewed packaged map/source bindings, preserving the original prefab-free service fixtures and every assertion. Play Mode correctly refuses actors without registered runtime prefabs; the runner preserves that production guard. Four separate Play Mode controls exercise native screen/map click and viewport-drag observation, map closure before a distinct world-target action, zero camera-triggered gateway orders, and unchanged camera parameters. Their fixture camera settles from the public map event; actual battle-camera settlement remains a normal-input gate.

All failed/cancelled attempts01 through16 remain preserved. Early attempts exposed absent Edit Mode Screen dimensions, same-frame UI raycast setup, a square-viewport map-edge ambiguity, stale generated ledger layout bindings, and incorrect runner invocation of ledger tests during Play Mode. Resource-floor cancellations were recorded separately from assertions.

Native17 requested four job workers and scoped local import workers to desired1/standby0/idle1000ms; the three local settings were restored byte-for-byte afterward. No memory cancellation occurred. Minimum observed free RAM was12.2849GiB and commit16.3192GiB; these observations do not establish future resource safety or a performance improvement.

No full S005 match, win, result/return journey, aircraft framing or soldier readability acceptance is claimed here. Fresh source-pinned normal-input gameplay, native battle screenshots, full manual/ARIA acceptance and real player/device acceptance remain pending. No new whole mission is accepted by these control tests.
