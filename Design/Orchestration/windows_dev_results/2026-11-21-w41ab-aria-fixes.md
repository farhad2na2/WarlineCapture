# W41a / W41b — ARIA fixes: S002 FindBase map-drag gate and S058 stale group selection (2026-11-21)

- Windows UI lab checkout: `D:\Projects\WarlineCapture-uilab`
- Branch: `missions/win-w41ab-aria-fixes`, cut from `origin/missions/integration` at fd231f6e7.
- Diagnosis: `origin/missions/win-w41-aria-block-diagnosis:Design/Orchestration/windows_dev_results/2026-11-21-w41-aria-block-diagnosis.md` (Block 1 = W41a; "Block 2, follow-up: S058 with the Mac lines" = W41b). Both fixes are lead-approved.
- Editor: 6000.5.8f1, run only through `Tools\CI\InvokeUnity.ps1 -GuiLicensing` and `Tools\Missions\run_aria_queue.py --launcher windows`.
- Logs: `D:\Projects\WarlineCapture-program-logs\2026-11-21\win-uilab\w41ab-*` (log paths below are relative to that folder).

**Status: automated Windows checks pass for both fixes.**
- EditMode matches the baseline: the same 4 pre-existing failures, no new failure, and all 21 new tests pass.
- ARIA: every judged job won inside its band with menu journey Passed, return Passed MAIN MENU, no `[AriaBlocked]` and gate 3 PASS.
- S043 fa-IR won at 78.88 %. That is the known Windows CE divergence above 70 % and is not a W41 failure.
- **Pending:**
  - the Mac S058 lane (where the H6 block was seen);
  - real-player and device acceptance.
- The two `executeMethod` validations that fail also fail identically on the unmodified code (§2).

## Commits

| Commit | Content |
|---|---|
| 661521cdf | W41a: FindBase map-drag gate (`AriaGroundReadModelPolicy.cs`, `MatchHudAssistantUiSystemHelper.SkirmishWatch.cs`) |
| 6daa43a6f | W41b: stale group selection (H6) and the `order_retry_selection` evidence trace (`UiShellEcsGateway.PublicObjective.cs`, `UiSkirmishPublicObjectiveModel.cs`, `AriaPlayInputSystem.ObjectiveTrace.cs`) |
| 9fa6b9917 | Tests: `AriaAdvanceSelectedUnorderedTests`, `AriaStaleGroupSelectionTests` |
| (this commit) | This results note |

No unit stat, rule, deadline, objective data or camera was changed. GameObject names, button identities and raycasts are unchanged.

## What changed

### W41a — FindBase map-drag gate
`ObserveAdvanceGround`, ordinary branch (`Assets/Game/Scripts/UI/Screens/MatchHudAssistantUiSystemHelper.SkirmishWatch.cs:581-589`):
- `view.AdvancePreferred = AriaGroundApproachPolicy.AdvancePreferred(industrialBasin, distances, view.ExpandedSelectedMask, view.ExpandedAssaultMask, acceptedApproachMask)`.
- The new pure overload (`Assets/Game/Scripts/Simulation/AriaGroundReadModelPolicy.cs:59-63`) is the old rule `|| SelectedUnordered(...)`, where `SelectedUnordered = (selected & assault & ~acceptedApproach) != 0`.
- For Industrial Basin it returns the old rule unchanged.
- `acceptedApproachMask` is read locally (`:584-587`) with the same `UiShellRuntimeGateway.TryReadAcceptedAssaultGoal(Approach, …)` call that `ObserveAcceptedAssaultGoal` makes. That method is not reordered, because the City branch depends on it running after `ObserveAdvanceGround`.
- Effect:
  - the open full map is no longer vetoed by the global `Close >= 4` quorum while selected assault groups have no accepted Approach order, so it offers its -20011 drag;
  - after the map closes, `AdvanceGround` is offered on screen, so the selected groups get an ordinary Attack Move.

### W41b — stale group selection (H6)
`UiShellEcsGateway.TryOwnObjectiveGroups` (`Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.PublicObjective.cs`):
- The existing member loop now counts living members that carry `SelectedUnitTag` (`taggedLiving`, `:122`, `:131`).
- The row is selected by `UiSkirmishPublicGroupSelection.Selected(group.Selected != 0, taggedLiving, points.Count)` (`:167`). Its body is exactly `recordSelected && taggedLiving == living` (`Assets/Game/Scripts/UI/Shell/UiSkirmishPublicObjectiveModel.cs`).
- A drifted group (tagged members dead, or untagged reinforcements) is no longer `ExclusiveSelectedGroup`. The planner therefore takes its ordinary visible path: it taps the selected card, then the group card, and only then presses Move. It no longer presses Move into a `NoSelection` refusal.
- **The re-select path is confirmed.** A card tap goes to `SelectionGameplayStartupSystemHelper:432` → `SkirmishExpandedPresentedOrders.TryTogglePresentedGroup` (`Runtime/Skirmish/SkirmishExpandedPresentedOrders.cs:178-186`). The toggle reads the live tags, not the record:
  - **Partial tag set:** a tagged living member exists, so the tap runs `TryDeselectGroup`. That clears the record and all tags, and the next tap runs `TrySelectGroup`, which tags every living member. This is two taps.
  - **No tagged living member (case b):** the first tap goes straight to `TryPresentedGroup` → `TrySelectGroup`. This is one tap.
  - Both paths are covered in EditMode below.

**Evidence trace (log-only).**
- At each objective `order_retry` / `order_blocked`, `AriaPlayInputSystem.ObjectiveTrace.cs:65-73`, `:117-127` logs:
  `[AriaObjective] type=… event=order_retry_selection group= found= alive= tagged= recordSelected= selected= exclusive= moveMode= t=`
- The group is the retried order's group. `ObjectiveTouch` is reset on retry, so the trace keeps the last non-zero touch group.
- The counts come from the existing read-only public projection, through new evidence fields `RecordSelected` and `SelectedTaggedAlive` on `UiSkirmishPublicGroupRow`. The trace never feeds a decision.

## Validation

### 1. EditMode (`-runTests -testPlatform EditMode`, `-GuiLicensing`)
- Suites: `AriaFindBaseRecoveryTests`, `AriaColumnRecoveryTests`, `MatchHudAssistantUiSystemHelperTests` (all, including the Skirmish cases), `AriaObjectiveSkillTests`, `AriaObjectiveSupportTests`, `AriaObjectiveCoverageTests`, `SkirmishExpandedArmyTests`, `SkirmishPublicObjectiveHudTests` and `SkirmishConvoyServiceJourneyTests`. The two new classes were added after the baseline.
- **Baseline** (unmodified integration): `w41ab-editmode-baseline-results.xml:2` reads `total="160" passed="156" failed="4"`. The 4 failures are pre-existing `MatchHudAssistantUiSystemHelperTests` tutorial cases:
  - `FirstShowMe_SelectSquad_ShowsImmediateUiCueBeforeEcsHighlight` (missing `RectTransform` on "NameStrip");
  - `TutorialBriefing_AutoShowsVisibleIndicatorForEveryInstruction`;
  - `TutorialBriefing_PersianUsesRtlFontAndLocalizedSubsteps`;
  - `TutorialLocalization_MapsEveryM01StepToEnglishAndPersianText` (copy strings).
- **Final** (W41a + W41b): `w41ab-editmode-2-results.xml:2` reads `total="181" passed="177" failed="4"`. The same 4 failed; there is no new failure. The wrapper exits 1 only because tests failed.
- **New tests: 21/21 passed.**
  - `AriaAdvanceSelectedUnorderedTests` (10). The case table:
    - `Close>=4` with selected assault groups unordered → true;
    - all accepted → false;
    - nothing selected → false;
    - a selected non-assault group → false;
    - `Close<4` → true.
    - It also checks that Industrial Basin is unchanged over a full grid, and that the rule never refuses what the old quorum allowed.
  - `AriaStaleGroupSelectionTests` (11):
    - a pure rule table for (a) agree, (b) tagged members dead, (c) partly tagged, (d) unselected and stray tags;
    - the own-group projection on the S002 v2 starting forces for (a), (b), (c) and (d), including the one-tap (b) and two-tap (c) re-select through `TryTogglePresentedGroup`;
    - the planner tapping the stale card before Move, then Move, then the ground tap, with no failure;
    - the (a) control, where Move is pressed directly.
- **Intermediate runs:**
  - `w41ab-editmode-results.xml`: 181/173/8. The 4 new projection tests failed because the headless fixture leaves actors without `LocalTransform` (no map grid). `w41ab-editmode-diag-results.xml` shows the diagnostic: `actor=5 group=2 … lt=False`.
  - I fixed the fixture by placing the actors; the rule was not relaxed. `w41ab-editmode-diag-2-results.xml` then reads 11/11 passed.
- **Prefab churn:** none. `M03PresentationTests` was not in these suites, `M03_FieldGuide.prefab` was not rewritten, and no other asset was rewritten.

### 2. `executeMethod` validations (`InvokeUnity.ps1 -GuiLicensing`)
| Validation | W41 code | Unmodified integration |
|---|---|---|
| `AriaSkirmishPlanValidation.RunPublicAssaultFocusValidation` | **Passed**: `w41ab-assaultfocus.log:396` `[PublicAssaultFocusControls] result=Passed checks=27` | — |
| `AriaSkirmishPlanValidation.RunFocusedValidation` | Failed: `w41ab-planvalidation.log:396` `InvalidOperationException: unavailable approach cannot bypass the existing three-attempt camera bound` | **Same failure:** `w41ab-planvalidation-baseline.log:2159` |
| `MatchHudAssistantUiSystemHelperTests.RunSkirmishAdvanceMapValidation` (native Play Mode, `WARLINE_ADVANCE_MAP_NATIVE_RUN`) | Failed: `w41ab-advancemap.log:1204-1208` `Assert.IsTrue` at `MatchHudAssistantUiSystemHelperTests.Skirmish.cs:85` (compact-map `FocusAdvance.Available`), `:1255` `[SkirmishAdvanceMapNativeExit] result=Failed` | **Same failure:** `w41ab-advancemap-baseline.log:1206-1210`, `:1257` |

- Both failures are pre-existing on Windows, and neither involves the W41 lines.
  - The first fails at `AriaSkirmishPlanValidation.cs:953`, which is planner code. W41 does not touch it.
  - The second fails at the first compact-map observation, before any selected/assault mask is involved.
- In the advance-map run, the embedded `[SkirmishExpandedAriaTests] result=Passed` is at `w41ab-advancemap.log:930`.
- For the baseline runs, the five W41 files were checked out at fd231f6e7 and the two new test files were moved aside. The tree was then restored to HEAD.

### 3. ARIA regression (Windows uilab, `--menu-journey --validation-library --no-table --timeout 30000`)
**Warm-up (uncounted)**, `w41ab-aria-warmup`, `S031:130366:en`:
- Abort `startupFailure=Timeout` (`unity.log:3536`), as in W40.
- Menu journey Passed (`:3385`).
- The queue completed (`:3778`).

**W41a set, queue A** (`w41ab-aria-a\unity.log`). `:1065987` reads `result=Completed jobs=7 recorded=7 wins=7 sourceUnchanged=1 exitCode=0`.

| Job | Result (terminal line) | deadlinePct | Menu | Return MAIN MENU | AriaBlocked | Gate 3 |
|---|---|---|---|---|---|---|
| S031:130366 en (warm-up slot, not judged) | Victory `:10151` | 32.16 | `:1235` | `:10447` | 0 | PASS |
| S002:130365 en | Victory MainBaseDestroyed 228.675 s `:279879` | 21.17 | `:11356` | `:280119` | 0 | PASS |
| S002:155923 en | Victory 227.897 s `:533642` | 21.10 | `:280989` | `:533882` | 0 | PASS |
| S002:104731 en | Victory 232.016 s `:770216` | 21.48 | `:534749` | `:770456` | 0 | PASS |
| S002:130365 fa-IR | Victory 232.171 s `:1047424` | 21.50 | `:771322` | `:1047676` | 0 | PASS |
| S020:130365 en (CE, Desert Base) | Victory ConvoyDelivered `:1054739` | 45.45 | `:1048546` | `:1055035` | 0 | PASS |
| S009:130365 en (FC, Desert Base) | Victory TicketsDepleted `:1065433` | 35.00 | `:1055906` | `:1065690` | 0 | PASS |

Gate 3: `w41ab-aria-a-gate3.out` (S002 jobs=4 pass=4).

**W41a set, queue A2** (`w41ab-aria-a2\unity.log`). `:257598` reads `result=Completed jobs=2 recorded=2 wins=2 sourceUnchanged=1`.
- S031:130366 (warm-up slot): Victory.
- **S002:130365 en: Victory 226.142 s** (`:257134`), 20.94 %, menu `:13034`, return `:257374`, AriaBlocked 0, gate 3 PASS (`w41ab-aria-a2-gate3.out`).

**W41a outcome:**
- **S002 @130365 en won in all 3 runs** (21.17 %, 20.94 % and the fa-IR run at 21.50 %). There was no FindBase loop and no AriaBlocked.
- **Early raid count: 0 of 5 S002 runs.**
  - No `[SkirmishEnemyTrace] event=base_damage … base=PlayerBase` line occurs in any S002 run.
  - Enemy group 16 (Tank) only took its opening Move at t≈0.2 s. It had no `order=Attack` before 90 s.
  - The only `[AriaS002Trace] event=base_alert state=onset` lines are late base alerts (`reason=unknown`) at t=219.55 / 223.61 / 224.79 / 218.99 / 220.44 (`aria-a :265073`, `:525934`, `:769936`, `:1040117`; `aria-a2 :254351`). They came while the rocketeers were destroying the enemy base, and the match ended about 8–13 s later.
  - Each S002 run had one ordinary opening FindBase map use: a -20010 open at t≈37–43 s, then a -20011 map target (for example `aria-a :23040`/`:23886`). There were no Close loops.
- **The raid-variant path that W41a targets was therefore not exercised on Windows in these 5 runs.** The fix is proven only by EditMode and by unchanged wins. A Windows or Mac run in which the t≈67 s tank raid occurs is still needed to show the post-alert map drag and Attack Move live.

**W41b set, queue B, first attempt** (`w41ab-aria-b\unity.log`): an infrastructure failure, retried once.
- All 9 matches ended in Victory, with menu journey and return Passed and 0 AriaBlocked: S058 155923 60.46 %, 104731 55.80 %, 130365 54.55 %, fa-IR 53.23 %; S055 53.40 %, S056 45.49 %, S057 51.62 %, S059 47.47 %. Gate 3 PASS (`w41ab-aria-b-gate3.out`).
- The queue still recorded every job as Abort. `:96747` reads `[SkirmishAriaQueue] result=Failed reason=candidateSourceChangedDuringQueue … exitCode=2`.
  - The start fingerprint `e9d484…` (`:469`) differed from the end fingerprint `5bc8ff…` (`:96598`).
  - Cause: on the first load of the S058 map scene (job 2), Unity created the ignored local cache `Assets/SceneDependencyCache/04a80bf244d14877bb869bad34d2b350.sceneWithBuildSettings.meta` (05:16:57Z). `SkirmishCandidateSourceHash` includes `.meta` files.
  - The S031 warm-up only caches the S031 map. No tracked file changed.
- I retried the same queue once, with the cache now present.

**W41b set, queue B retry** (`w41ab-aria-b-retry\unity.log`). `:95376` reads `result=Completed jobs=9 recorded=9 wins=9 sourceUnchanged=1 exitCode=0`.

| Job | Result (terminal line) | deadlinePct | Menu | Return MAIN MENU | AriaBlocked | Gate 3 |
|---|---|---|---|---|---|---|
| S031:130366 en (warm-up slot) | Victory `:9727` | 32.13 | `:1321` | `:10003` | 0 | PASS |
| **S058:155923 en** | Victory TicketsDepleted `:21661` | 45.50 | `:10860` | `:21975` | 0 | PASS |
| **S058:104731 en** | Victory `:32933` | 45.43 | `:22830` | `:33249` | 0 | PASS |
| S058:130365 en | Victory `:44858` | 45.54 | `:34099` | `:45114` | 0 | PASS |
| S058:130365 fa-IR | Victory `:56828` | 51.18 | `:45948` | `:57096` | 0 | PASS |
| S055:130365 en | Victory `:65910` | 58.82 | `:57930` | `:66168` | 0 | PASS |
| S056:130365 en | Victory `:78800` | 64.24 | `:67018` | `:79057` | 0 | PASS |
| S057:130365 en | Victory `:86034` | 45.48 | `:79908` | `:86290` | 0 | PASS |
| S059:130365 en | Victory `:94792` | 59.80 | `:87140` | `:95048` | 0 | PASS |

Gate 3: `w41ab-aria-b-retry-gate3.out`.

**W41b set, queue C** (`w41ab-aria-c\unity.log`). `:120761` reads `result=Completed jobs=7 recorded=7 wins=7 sourceUnchanged=1 exitCode=0`.

| Job | Result (terminal line) | deadlinePct | Menu | Return MAIN MENU | AriaBlocked | Gate 3 |
|---|---|---|---|---|---|---|
| S031:130366 en (warm-up slot) | Victory `:11695` | 34.32 | `:1306` | `:11951` | 0 | PASS |
| S009:130365 en (FC) | Victory `:22079` | 34.98 | `:12864` | `:22353` | 0 | PASS |
| S010:130365 en (FC) | Victory `:38692` | 35.03 | `:23228` | `:38948` | 0 | PASS |
| S014:130365 en (BT) | Victory BreakthroughEvacuated `:84968` | 35.48 | `:39819` | `:85264` | 0 | PASS |
| S037:130365 en (BT) | Victory BreakthroughEvacuated `:105457` | 49.96 | `:86146` | `:105734` | 0 | PASS |
| S019:130365 en (CE) | Victory ConvoyDelivered `:111881` | 33.50 | `:106615` | `:112137` | 0 | PASS |
| S043:130365 fa-IR (CE) | Victory ConvoyDelivered `:120193` | 78.88 (known Windows CE divergence, >70 %) | `:113018` | `:120463` | 0 | PASS |

Gate 3: `w41ab-aria-c-gate3.out`.

**W41b outcome:**
- **S058 won on all four seeds/locales on Windows**, including 155923 and 104731, the two seeds that blocked on the Mac lane.
- **Retry trace: no objective `order_retry` or `order_blocked` occurred in any W41b run** (queues B, B retry and C). So no `order_retry_selection` line was produced; the trace is in place for the Mac lane.
- `order_tapped` → `order_accepted`:
  - every chosen order that stayed chosen was accepted;
  - the few taps without a receipt were superseded by a new public choice (for example the Move/Hold choice flips for group 1 at t=45–165 s in S058 104731, `w41ab-aria-b-retry :25905–:30404`);
  - they never ended in a retry.
- This Windows pass does not rule H6 out on the Mac. The Mac screen size and timing produced the original block.

### 4. Hygiene
- After every queue, I deleted the harness evidence under `Design/AgentReports/SkirmishExpansion/*/_Evidence/candidates/<sha>/` and restored `S002/_Evidence/s002-aria-watch-controls.png` (rewritten by S002 runs). None of it is committed.
- No added file is LFS-tracked: `git diff --name-only origin/missions/integration HEAD | git check-attr --stdin filter` shows `filter: unspecified` for all.
- Before each Unity run I confirmed that no Unity process had `-projectPath D:\Projects\WarlineCapture-uilab`. The main checkout's Editor, Hub and the licensing client were not touched.

## Pending
- **Mac S058 lane:** rerun `S058:155923/104731/130365:en`, `S058:130365:fa-IR` and the FC/BT/CE controls on the Mac, where H6 was observed. Collect any `order_retry_selection` lines.
- **S002 raid variant:** a run in which the t≈67 s enemy tank raid happens. That would show W41a's post-alert map drag and Attack Move live; none of the 5 Windows S002 runs had the raid.
- The pre-existing Windows failures of `RunFocusedValidation` and `RunSkirmishAdvanceMapValidation`, which are unrelated to W41.
- Separate player-facing issue, from the diagnosis, for the owner: a reinforced or attrited group's tray card still shows as selected while its new members are not. Fixing that is a runtime gameplay change and is not part of W41b.
- **Real-player and device acceptance: not done.** Everything above is automated evidence from the Windows lab.
