# W41 — ARIA block diagnosis: S002 FindBase loop and S058 ControlZone (2026-11-21)

This is diagnosis only. No game code was changed and no repro run was made. The evidence is the existing Windows logs
under `D:\Projects\WarlineCapture-program-logs\` and the code on `origin/missions/integration` 59923db84. That code
matches `missions/win-w40-ui-objective-card` for every file cited here, except `SkirmishMatchView.PublicObjectiveHud.cs`;
its line numbers below are from integration. Code paths are under `Assets/Game/Scripts/`. Log paths are relative to
`D:\Projects\WarlineCapture-program-logs\`.

## Block 1 — S002 @130365: FindBase opens and closes the full map until AriaBlocked 101

### What the logs show (all four Windows cases)

| Log | Run | Army stops at (m from objective) | Last plan progress | AriaBlocked 101 | Enemy-base damage lines |
|---|---|---|---|---|---|
| `2026-11-20/win-uilab/aria-2/unity.log` | block | 22.61085 | time 246.62 | :310864, time 426.65 (= +180.03) | 0 |
| `2026-11-20/win-uilab/w40-aria/unity.log` | block | 22.61085 | 243.44 | :360347, time 423.47 (= +180.03) | 0 |
| `2026-11-18/win-dev-unity/w37g2-proof/menu/unity.log` (job 9) | block | 19.78005 | 222.13 | :392257, time 402.18 (= +180.05) | 0 |
| `2026-11-20/win-uilab/aria/unity.log` | win | 22.65727 | — | — | 7 (:176592–:177046) |
| `2026-11-20/win-uilab/aria-3/unity.log` | win | — | — | — | 7 (:186658–:187082) |
| `2026-11-20/win-uilab/w40-aria-2/unity.log` | win | — | — | — | 8 (:201095–:201506) |

The timeline in aria-2. Fields come from `[AriaS002Trace]`; `distance` is `view.NearestFriendlyObjectiveDistance`
(`AriaPlayInputSystem.cs:63`, set at `MatchHudAssistantUiSystemHelper.SkirmishWatch.cs:529`).
- :164899–:165304: the column is parked 22.61 m from the objective with `enemyHealth=0` (the base is never presented).
  The selected groups are 24/25 (page 1 slots 3/4), new recruits, with `attackMask=0`. FindBase presses ENEMY BASE
  (-167292) twice. It gives up twice (`find_base_given_up` at :165139 and :165321).
- :165354: a perimeter base alert. Home defence sends group 25 home (:165468) and the camera goes home (distance 700.71 at :165450).
- From :192556 the same loop runs 5 times: FindBase taps the minimap (-20010), the full map opens, and about 7 s later ARIA
  taps the popup's Close button. The Close button has a new ID each time: -237904, -243088, -248272, -253456, -258640.
  The open/close pairs are :192556/:200870, :210535/:218179, :244408/:253061, :262835/:270816 and :299257/:307482.
  It never pans the map with a drag (-20011).
  - Each open line shows `mapOpen=False distance=686.998`.
  - Each close line shows `mapOpen=True closeAvailable=True distance=22.61085`.
  - Give-ups and Attack-disarm presses fall in between: :220105 with :220572/:224515, and :272964 with :274439/:277486.
    `attackMode` stays True afterwards.
  - The "7 times" in the queue entry counts every FindBase map open in the job. Two of them are before the alert (:21819,
    :55812), and those did get drags (:22365, :57736).

### Why the open full map never offers a drag target

The full map and the compact minimap disagree about whether the army still has to advance.
1. S002 goes through `AriaS002NativeAdapter.CanHandle` (`UI/Shell/Ecs/AriaS002NativeAdapter.cs:5`), then
   `AriaGroundBaseAssaultPolicy.StepLegacyS002Observation`. It does not use the generic `AriaSkirmishPlanSystem.cs`
   map steps around 661/674. The observation comes from `ObserveAdvanceGround`, ordinary branch
   (`SkirmishWatch.cs:581-609`). S002 is map `DB` with `SelectedApproach.Required == false`
   (`Runtime/Skirmish/SkirmishPublicSelectedApproachProjection.cs:42-60`).
2. That branch sets `view.AdvancePreferred = AriaGroundApproachPolicy.AdvancePreferred(industrialBasin, distances)` and
   returns at once when it is false (`SkirmishWatch.cs:582-583`). For Desert Base this means
   `distances.Close < 4` (`Simulation/AriaGroundReadModelPolicy.cs:58`). `Close` counts every presented friendly contact
   within 45 m of the objective (`:17-21`), whether or not it is selected.
3. With the map closed and the camera at home, the compact minimap presents only home contacts. So `Close = 0`, advance
   is preferred, and the approach point is off screen. `ObserveAdvanceMapFocus` therefore offers the minimap centre
   (-20010) (`SkirmishWatch.cs:607-609`, `:620`, `:624`).
4. With the full map open, `ResolveAssaultObservationMap` switches `watchMap` to the full map (`SkirmishWatch.cs:445-448`).
   The full map presents the whole army, including the column parked 22.61 m from the base, so `Close >= 4`
   (`distance=22.61085` on every close line confirms that these contacts are counted). `AdvancePreferred` becomes false,
   and line 583 returns before the open-map branch (`:588-593`) can call `ObserveAdvanceMapFocus`. So `FocusAdvance` is
   never admitted while the map is open.

So the answer is that FindBase gets no map-drag target because the open-map branch is gated by a global quorum of
all friendly contacts. The selected groups that need the order are at home, but the unselected column already parked at
the base satisfies that quorum.

### Which condition closes the map again

In `AriaGroundBaseAssaultPolicy.StepExpandedAdvanceNavigation`:
- At stage 1 with the map open, `!view.FocusAdvance.Admitted` waits 5 s (`:746-748`). It then sets
  `MapNavigationStage = MapNavigationCloseWithoutFocus` (`:749`) and taps `view.CloseMap` (`:762-763`). That is
  `watchFullMap.CloseAction` (`SkirmishWatch.cs:795`).
- Once the map is closed, the stage-4 fallback to the reported-position button (`:730-737`) needs `view.FocusEnemy.Admitted`.
  It was never pressed after the alert in any of the three block logs: there is no FocusEnemy target after the alert.
  The S002 trace does not record FocusEnemy availability, so the reason is unproven. The step then resets the navigation
  with `MapNavigationReadyAt = time + 6` (`:738-740`).
- The FindBase branch (`:217-252`) sees the compact minimap's -20010 again and reopens the map (`:245-250`).

### Why there are 5 loops and then reason 101

- Each loop takes about 15–18 s: the open tap, the 5 s focus wait, the close tap, then 6 s before navigation is ready again.
- `AriaFindBasePolicy.GiveUp` gives up after `MaxTaps = 4` search taps (`Simulation/AriaGroundColumnPolicy.cs:247`,
  `:268-279`). That is two open/close pairs. Then `Recover` presses Attack up to twice and waits `RetrySeconds = 20`
  (`:248`, `:253-262`; `AriaGroundBaseAssaultPolicy.cs:231-243`, `:608-620`).
- **Reason 101 is the 180 s progress watchdog**, not the map:
  - `AriaGroundProgressPolicy.Observe` sets `blocked = time - LastProgressAt > 180` (`AriaGroundColumnPolicy.cs:311-316`).
    Progress means base or player health changing, infantry count changing, an economic milestone, or a closest approach
    at least 2 m better than the best (`AriaGroundBaseAssaultPolicy.cs:97-107`).
  - The best approach is already 22.61 m, health (464 / 0) and infantry (21) do not change, so nothing renews it.
  - `touch.Phase = Blocked` (`:107`) becomes `StopReason = 101` at `UI/Shell/Ecs/AriaSkirmishPlanSystem.cs:58-59`.
- The watchdog clock started **before** the alert. Last progress was at time 246.62, 243.44 and 222.13, while the column was
  parked at the base. The detour and the map loop only use up the remaining time.

### The upstream difference between wins and blocks (context for the fix)

All three blocks share an early enemy tank raid. All three wins do not.
- In the blocks, enemy group 16 (Tank) switches Move to Attack on the player base at about t=67–68 s
  (aria-2 :38642, w40-aria :42925, w37g2 :105263). The player base then takes damage (:38775, :43153, :105554;
  800 → 464 or 352).
- Early home defence then orders the anti-armour group: `antiArmorMask=16` at page 0 slot 4, which is group 5 (aria-2 :39424).
  That group is re-ordered at about t=95 (:62543, `attackMask` 15 → 31).
- After that, nothing ever damages the enemy base. In the wins, rocketeers `role.rocketeer.1.2.x` destroy the base from the
  same parking spot at t≈220–235 (for example aria :176592).
- Where group 5 and the rocketeers were after t≈95 is not traced. That is an evidence gap. It needs per-group role and
  position lines for t=95–250, which the current `[AriaArmyOrderTrace]` does not give.

### Smallest fix (proposed, not implemented)

**`ObserveAdvanceGround`, ordinary non-Industrial branch (`SkirmishWatch.cs:581-583`): do not let the global quorum veto an
advance while selected assault groups have no accepted order.**
- Change the gate to:
  `view.AdvancePreferred = AriaGroundApproachPolicy.AdvancePreferred(false, distances) || selectedUnordered`.
- Here `selectedUnordered = (view.ExpandedSelectedMask & view.ExpandedAssaultMask & ~acceptedApproachMask) != 0`.
- `acceptedApproachMask` is the mask from the same public `UiShellRuntimeGateway.TryReadAcceptedAssaultGoal(Approach, …)`
  read that `ObserveAcceptedAssaultGoal` does (`SkirmishWatch.cs:494-506`). It must be read before the gate, because
  `ObserveAcceptedAssaultGoal` currently runs after `ObserveAdvanceGround` (`:314-315`).
  - Read it locally in the ordinary branch. Do not move the call: the City branch's goal kind depends on `watchFlankReached`,
    which `ObserveAdvanceGround` updates.
  - Better, put the rule in a pure helper (an `AriaGroundApproachPolicy.AdvancePreferred` overload) so EditMode can test it.

What this changes:
- With the full map open, the open-map branch (`:588-593`) now runs, so the full map offers its -20011 drag to the approach.
  The camera lands at the base and the loop ends.
- After the map closes, the compact-map gate also passes, so `AdvanceGround` is offered on screen. The selected groups get an
  ordinary Attack Move instead of FindBase giving up. The same applies before the alert (:165098), where the selected
  recruits were never ordered.
- The rule changes nothing when the selected groups already have an accepted order, or when `Close < 4`.
- Whether S002 @130365 then **wins** is not proven. The recruits must march about 700 m, and an accepted order is not
  progress for the 180 s watchdog. The parked column's missing base damage (above) may still decide the match.

A narrower alternative only removes the loop. In the open-map branch, call `ObserveAdvanceMapFocus` before the
`AdvancePreferred` return. The camera would then pan to the base, but after closing, the compact-map quorum still refuses
`AdvanceGround`, and ARIA falls back to the FocusEnemy give-up it already shows before the alert. It changes how the
failure looks, not the result.

Which accepted missions it can touch:
- The ordinary `ObserveAdvanceGround` branch is consumed only by the base-assault planners: the S002 native adapter and the
  generic `AriaSkirmishPlanSystem`.
- FC, BT and CE (`StepObjectiveSkill`) never read `AdvanceGround`, `FocusAdvance` or `AdvancePreferred`
  (grep in `UI/Shell/Ecs`).
- City Crossroads (`:532-563`), the authored selected-own approach (`:564-580`, S003) and Industrial Basin (keep `:582` as it
  is for IB) are untouched.
- Of the 31 accepted Skirmish rows in `missions.csv`, only **S002** (DB, BaseAssault, ground, established) uses this path.
  No Operations row is accepted.

How to prove nothing else changes:
- EditMode:
  - A new pure-policy case table:
    - `Close>=4` with the selected groups unordered → true.
    - `Close>=4` with the selected groups all accepted → false (unchanged).
    - `Close<4` → true (unchanged).
    - Industrial Basin unchanged.
  - Rerun `AriaFindBaseRecoveryTests`, especially `S002MapWithoutApproachFallsBackToReportedPositionThenOrders(true/false)`
    and `AdvanceGroundIsGivenUpBeforeAFourthPress`.
  - Rerun `AriaColumnRecoveryTests`, `MatchHudAssistantUiSystemHelperTests.Skirmish`, the
    `AriaSkirmishPlanValidation.RunFocusedValidation` / `RunPublicAssaultFocusValidation` executeMethod validations, and
    `RunSkirmishAdvanceMapValidation`.
- ARIA regression with `run_aria_queue.py --menu-journey --validation-library --no-table`, after an uncounted warm-up:
  - `S031:130366:en` (warm-up, not judged)
  - `S002:130365:en` three times (the raid variant came up in 3 of 6 recent runs)
  - `S002:155923:en`, `S002:104731:en`, `S002:130365:fa-IR`
  - Same-observation controls on Desert Base: `S020:130365:en` (CE) and `S009:130365:en` (FC)
  - Require: wins at deadlinePct 20–70, no AriaBlocked, and gate 3 PASS.
  - Count how often the early raid occurs: an `enemy_order … group=16 … order=Attack` line before 90 s.

### Case (d) "road-lab" S002 run

Not found.
- Searched `Design/Orchestration/STATUS.md`, `daily/`, `notes/`, `windows_dev_queue.md` and all of `origin/missions/integration`
  for "road-lab", "road lab", "roadlab" and "road_lab".
- Searched every folder under `D:\Projects\WarlineCapture-program-logs\` (2026-11-18 and 2026-11-20; the 2026-11-17 zip was
  not opened). No road-lab S002 run is recorded on this machine or in the repo. It may live only on the Mac (`/Volumes/T7/…`).

## Block 2 — Mac lane 49 S058 (Frontline Control): AriaBlocked 101 kind=Waiting intent=ControlZone, control=Move

### Evidence available here

The Mac log `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-20/lane49/a/unity.log` is not on this machine.
The only excerpts are in the `missions.csv` S058 row (integration):
- 155923 en: ABORT at 36.97 % of 1200 s (t≈443.6 s), `[AriaBlocked] reason=101 target=-3842446 attempts=2 actions=166 kind=W…`
  at :451457.
- 104731 en: ABORT at 47.96 % (t≈575.5 s), `[AriaBlocked] reason=101 target=-3975074 attempts=2 actions=229 kind=W…` at :463641.
- 130365 en won at 45.49 %, and fa-IR 130365 won at 45.57 %.

Lane 49 ran code 97f2b0844. Every Block 2 file cited below is identical between 97f2b0844 and integration 59923db84.

### How reason 101 happens on this path (code)

- `StepObjectiveSkill` → `StepObjectiveOrder` → `StepPublicObjective`. The planner marks Blocked at
  `UI/Shell/Ecs/AriaSkirmishPlanSystem.Objective.cs:19` (frame not presented), `:48` (the 8 s receipt deadline after a
  ground tap) or `:61-62` (the 30 s sequence deadline, or 2 rejected re-presses).
- `StepObjectiveOrder` absorbs each failure and retries after `OrderRetrySeconds = 8`. It gives up for good at
  `MaxOrderFailures = 6` net failures (each accepted order earns one back) (`AriaSkirmishPlanSystem.ObjectiveSkill.cs:138-147`;
  `Simulation/AriaObjectiveSkillPolicy.cs:44-45`, `:101-114`). The final Blocked becomes 101 at `AriaSkirmishPlanSystem.cs:58-59`.
- `control=Move attempts=2` means the last pressed target in the final sequence was the Move button, pressed twice.
  The ground tap (ID `0x3xxxxxxx`) was never pressed in that sequence, so this is not the 8 s receipt path.
- 155923 timing: zones were lost at about 195 s and the block came at about 443.6 s. That is about 41 s per failure for 6
  failures, which matches a 30 s sequence deadline plus the 8 s pause plus select/focus taps. A rejected-press failure
  would be much faster.

So the final sequences reached Move and then published nothing until the 30 s deadline. With Move armed, three silent
returns in `StepPublicObjective` can do that:
- **H1 (most likely): `if (!target.Available) return;` (`Objective.cs:96`) while `GoalInView` is true.**
  - For a capture zone, `InView = visible && inspected` and `Ground.Available = visible && !mapOpen`
    (`UI/Components/SkirmishMatchView.PublicObjectiveHud.cs:211`, `:227-228`).
  - The observer then drops the ground when the zone centre's screen point hits any UI raycast target
    (`UI/Screens/MatchHudAssistantUiSystemHelper.PublicObjective.cs:84`; `WatchTargetIsReachable(..., world: true)` returns
    `watchHits.Count == 0`, `MatchHudAssistantUiSystemHelper.Watch.cs:300`).
  - So a zone that is on screen but under a HUD panel counts as "in view" and skips Focus (`Objective.cs:76`). Move is
    armed, and the ground is never offered. Nothing moves the camera, so every retry repeats the same failure.
  - Losing both zones fits this. With 0 owned, every infantry group becomes a Move candidate for an opposing zone
    (`Skirmish/Contracts/SkirmishPlayerObjectivePolicyUtilitySystemHelper.cs:117-133`), and the camera is wherever the
    last action left it. A Mac screen size and HUD layout would also explain why only the Mac lane shows it.
- H2: `if (!objective.CameraSettled) return;` (`Objective.cs:78`) while the goal is not in view and the camera keeps moving.
- H3: support, coverage or escort turns run between the order's taps, because only `Stage == 3`/`CommandPending` counts as
  pending (`ObjectiveSkill.cs:72`, `:84-104`). They could use up the 30 s and cancel Move mode, which would explain the
  second Move press.

### Smallest fix (proposed for H1, not implemented)

**`AriaSkirmishPlanSystem.StepPublicObjective` (`Objective.cs:76`): for `objective.Action == Move`, treat
"goal in view and camera settled, but ground not available" like "goal not in view".**
- Press the existing `objective.Focus` (zone camera chip or map focus) before arming Move. Change the condition to
  `(!objective.GoalInView || !objective.CameraSettled || objective.Action == AriaObjectiveAction.Move && !objective.Ground.Available)`.
  `Publish`'s `RejectedAttempts`/`InputPressesExhausted` limits keep Focus bounded.
- Focus centres the zone, which moves its centre out from under the HUD. Today this case is always a silent wait until the
  30 s deadline, so the change only replaces a guaranteed failure.
- Do not implement it until the Mac lines below confirm H1. If they show H2 or H3, the fix is different:
  - H2: bound the unsettled-camera wait.
  - H3: count a started sequence for the same goal (`ObjectiveTouch.LastTarget != 0 && Stage < 4`) as `pending` at
    `ObjectiveSkill.cs:72`.

Which accepted missions it can touch:
- `StepPublicObjective` serves every FC/BT/CE objective order and the escort Move (`ObjectiveSkill.cs:167`).
  - FC: S009, S010, S031, S033–S036, S055–S057, S059.
  - BT: S014–S018, S037–S042.
  - CE: S019–S024, S043, S044.
- Only Move orders with `GoalInView && CameraSettled && !Ground.Available` change. Hold, Unload and Repair are excluded.
  `PublicBoarding` and `OffensiveAir` have their own ground checks and are untouched. Base assault (S002) does not use this path.

How to prove nothing else changes:
- EditMode:
  - Add an `AriaObjectiveSkillTests` case: Move, `GoalInView = true`, `CameraSettled = true`, `Ground.Available = false`
    → the next press is Focus, not a silent wait. Then Ground becomes available → Move, ground tap, receipt → stage 4.
    Focus is never pressed a fourth time.
  - Rerun all of `AriaObjectiveSkillTests`, especially `ObjectiveOrderTimeoutIsRetriedNotBlocked`,
    `RejectedObjectivePressesAreRetriedBeforeTheInputLayerStops`, `RosterChangeBetweenSelectPressesKeepsTheInputBound`,
    `EscortGetsATurnAfterAnAcceptedObjectiveOrder`, the frontline garrison cases, `AriaObjectiveSupportTests` and
    `AriaObjectiveCoverageTests`.
- ARIA regression, on the **Mac lane** (where it fails) and the Windows uilab:
  - `S058:155923:en`, `S058:104731:en`, `S058:130365:en`
  - FC controls `S055/S056/S057/S059:130365:en`, `S009:130365:en`, `S010:130365:en`, `S031:130365:en`
  - BT `S014:130365:en`, `S037:130365:en`
  - CE `S019:130365:en`, `S043:130365:fa-IR`
  - Run after a warm-up, with `--menu-journey --validation-library --no-table`. Require no AriaBlocked, the existing win
    bands, and gate 3 PASS.

### Mac log lines needed to settle H1, H2 or H3

From lane49/a/unity.log for both blocked jobs (155923 up to :451457, 104731 up to :463641), from about t=180 s to the block:
1. Every `[AriaObjective] type=FC` line: `event=score`, `choice` (carries `inView=` and `ground=`), `order_tapped`,
   `order_accepted`, `order_retry failures=N retryAt=`, `order_blocked`, `recruit`, `coverage`, `escort_offer/retry`,
   `popup`, `frame_missing`, with their `t=`.
2. The full `[AriaBlocked]` and `[AriaBlockedControl]` lines (`moveMode=`, `mapOpen=`, `drawer=`, `selectedGroups=`).
3. The matching `[SkirmishAriaQueue]` result line and the screen size line for the job (`playArea=… screen=…`, from
   `[SkirmishHudChipPlacement]`).

How to read them:
- H1 is confirmed if the last `choice` before the retries shows `action=Move inView=True ground=False`, with no
  `order_tapped` in each failing sequence and `order_retry` lines about 38–41 s apart.
- Interleaved `recruit`/`coverage`/`popup` lines between `choice` and `order_retry` point to H3.
- `inView=False` points to H2.

`choice` is logged only on change. If it is ambiguous, a one-line trace of `GoalInView`, `CameraSettled`, `MoveMode` and
`Ground.Available` at each `order_retry` would settle it.

No Windows repro of S058 was run. The match is long, and the uilab tree has the W39/W40 HUD panels and a Windows screen
size, so a Windows pass would not rule H1 out on the Mac.

## Block 2, follow-up — S058 with the Mac lines: a stale group selection (H6), not H1–H3

The lead sent excerpts from Mac `lane49/a/unity.log` (155923 from :441808, 104731 from :452312). None of H1, H2 or H3 fits:
- the choice line says `inView=False ground=False`, so the camera is not settled on the zone;
- `order_tapped` appears, so the ground was tapped;
- at the block, `moveMode=False` with `selectedGroups=1 selectedMask=8`.

No game code was changed and no Unity run was made. Code lines below are from `origin/missions/integration` 59923db84,
read with `git show`. Every file cited is identical to lane 49's code (97f2b0844).

### What the two failure phases are

Each sequence has two phases, and they fail in different ways.

**Phase A: the order is tapped but never acknowledged.** At 155923 the sequence goes 370.03 `order_tapped`, then 379.87
`order_retry`. At 104731 it goes 497.81, then 507.16. That is about 9.5–9.8 s each time.
- That is the 8 s receipt deadline. A ground tap moves the sequence to `CommandPending` with `Deadline = time + 8`
  (`UI/Shell/Ecs/AriaSkirmishPlanSystem.Objective.cs:63-64`). It ends Blocked when no matching receipt arrives
  (`:47-48`), and then it is absorbed as a retry (`AriaSkirmishPlanSystem.ObjectiveSkill.cs:138-147`).
- The receipt needs all of these together (`Objective.cs:38-41`):
  - a newer accepted-order revision;
  - the same group, public goal (`AcceptedPublicKey`) and action;
  - a contact target and frame that match the tap.
- The HUD supplies `AcceptedGoalId` from `ResolvePublicOrderGoal` (`UI/Components/SkirmishMatchView.PublicObjectiveHud.cs:241`,
  `:266-282`). For a Move, both the accepted order's world point **and every member destination in
  `group.CurrentMoveGoals`** must lie inside the zone (`:278`).
- `CurrentMoveGoals` holds the `UnitTarget` / `UnitLongDistanceMove` goal of **every** living member with a `UnitTarget`,
  whether or not that member is selected (`UI/Shell/Ecs/UiShellEcsGateway.PublicObjective.cs:133-136`, `:165`).
- `contact` is set only when `acceptedGoal != null` (`PublicObjectiveHud.cs:243-246`). So if any member that took no part
  in the order has a goal outside the zone, the order is never acknowledged, even though the game accepted it.

**Phase B: later retries never reach the ground tap.** Retries 2–5 come about 11 s apart with no `order_tapped`. That is
the 8 s pause plus roughly 3 s of presses. The block shows `control=Move`, `moveMode=False`.
- In `StepPublicObjective`, with `ExclusiveGroupSelected` true the card is not pressed again (`Objective.cs:67-71`).
  `inView=False` presses Focus (`:76-79`), and then `!objective.MoveMode` presses Move (`:91`).
- If Move mode never turns on, Move is pressed again. `Publish` counts these as rejected re-presses (`:119-125`).
  At `RejectedAttempts >= 2` the attempt ends Blocked (`:61-62`) and becomes the next `order_retry`.
- After `MaxOrderFailures = 6`, it ends in `order_blocked` and AriaBlocked 101 (`Simulation/AriaObjectiveSkillPolicy.cs:44-45`).
- The Move button turns Move mode on only when at least one selected, living, movable player unit exists.
  `RtsSelectionMoveTargetModeCommandSystem.ProcessPendingRequests` refuses it with `NoSelection` otherwise
  (`Systems/RtsSelectionMoveTargetModeCommandSystem.cs:115-121`). Its query needs `SelectedUnitTag`, `UnitGrid` and
  `UnitMove`, and excludes `Disabled` and `UnitTransportPassenger` (`:67-75`, `:144-160`). The HUD mode is then
  cleared (`Systems/RtsSelectionCommandResultFlushCompositionSystemHelper.cs:468-475`).

### Why ARIA thinks the group is selected when no live unit is

The planner's "selected" comes from the group record. The Move button reads unit tags. The two can drift apart.
- `ExclusiveSelectedGroup` is `selectedCount == 1` over `row.Selected` (`PublicObjectiveHud.cs:201`, `:251`).
  `row.Selected` is `SkirmishArmyGroupRecord.Selected != 0` (`UiShellEcsGateway.PublicObjective.cs:164`).
- `record.Selected` changes only on an explicit select, deselect or clear (`Runtime/Skirmish/SkirmishArmySelectionService.cs:125-136`,
  `:139-149`, `:178-209`, `:253-265`). `SkirmishArmyGroupSystem` never touches it.
- Unit tags drift away from the record in two ways:
  - Selecting a group tags only the members alive at that moment (`ApplyTags`, `:267-297`).
  - Members delivered later join the same production group (`SkirmishProductionService.Native.cs:77-80`) with
    `SelectedUnitTag` stripped at spawn (`Systems/BuildingProductionRequestSystemHelper.cs:1375`;
    `Systems/SharedProductionActorInitializer.cs:62`).
  - A tagged member that dies loses its tag (`Systems/UnitDeathSystem.cs:284-285`), and the record stays Selected.
- So a reinforced or attrited group can show as selected (record), have living members (`AliveCount > 0`, so it stays in
  the chooser), and have no live tagged member. In that state Move mode is refused with `NoSelection` on every press,
  which matches `moveMode=False` and `selectedGroups=1` at the block. ARIA never taps the card again, because
  `ExclusiveGroupSelected` is already true.
- The same drift also explains Phase A while some tagged members are still alive. The Move goes only to the tagged
  subset, and an untagged member's own goal outside the zone keeps `ResolvePublicOrderGoal` null. This is the most likely
  cause, but not proven. See "Lines still needed".
- It fits both seeds:
  - At 104731, group 43 was produced by a Rifle recruit at 472.64, when `infantry=0`, so its members arrived over time.
  - At 155923, the 356.11 recruit was a LogisticsTruck and cannot be group 39 (see H5). Group 39 is an infantry group whose
    production history is not in the excerpt. The drift there comes from deaths or later deliveries.

### Why the chooser picks that group, and the lead's hypotheses

- **The chooser accepts it correctly.** `TryOwnObjectiveGroups` keeps a group when `AliveCount > 0` and at least one member
  is alive with a position (`UiShellEcsGateway.PublicObjective.cs:120-144`). It marks it `Living = true`,
  `CombatInfantry` for assault infantry roles, `Dismounted` when no member is a transport passenger, and
  `CanMove = ground && !mounted` (`:149-155`).
  - The FC candidate needs `CombatInfantry && Dismounted` and an owned-or-opposing zone (`SkirmishPlayerObjectivePolicyUtilitySystemHelper.cs:117-133`).
  - With 0 zones owned, every free infantry group is a Move candidate for an opposing zone.
  - `AcceptedOrderActive` needs every member to carry `ManualMoveOrderTag` with goals inside the zone
    (`UiShellEcsGateway.PublicObjective.cs:131`, `:165`; `PublicObjectiveHud.cs:218-219`). After a partial order it stays
    false, so the same group and zone are chosen again every 4 s.
- **H5 (logistics truck) is ruled out by the filter.** A truck group has `Domain = LogisticsSupport`, so
  `CombatInfantry = false` (`:154`), and the Capture candidate returns default (`policy :119-120`).
- **H4 is half right.** The selection really does hold no commandable unit; the `NoSelection` refusal is exactly that.
  But this is not because the units are still spawning or in transit.
  - In-transit units are excluded from the chooser as `mounted` → `Dismounted = false`.
  - The projection query does include disabled entities (`:113`, `IncludeDisabledEntities`), so a disabled member could
    still keep a group alive. That is possible but nothing in the excerpt points to it.
  - The evidence points to the record/tag drift above. I call this H6.
- **The lead's candidate fix (skip groups with no commandable units) does not fit.** These groups do have living
  commandable members; they just are not tagged. Skipping them would drop a valid reinforcement group and leave the stale
  selection in place for Hold and escort orders.

### Smallest fix (proposed, not implemented)

**`UiShellEcsGateway.TryOwnObjectiveGroups` (`UI/Shell/Ecs/UiShellEcsGateway.PublicObjective.cs:120-166`): report a group
as selected only when the player-visible unit selection actually covers it.**
- In the existing member loop, count living members that carry `SelectedUnitTag`. Then set
  `Selected = group.Selected != 0 && taggedLiving == points.Count`.
- Optionally also require at least one tagged member without `Disabled`, matching the Move-mode query.

What this changes:
- `ExclusiveSelectedGroup` then drops to 0 for a drifted group. `StepPublicObjective` takes its ordinary visible path:
  `ClearOtherSelection` (the selected card), then `SelectGroup` (`Objective.cs:67-71`, `MatchHudAssistantUiSystemHelper.PublicObjective.cs:77-78`).
- `TrySelectGroup` re-tags every living member (`SkirmishArmySelectionService.cs:125-136`). Move mode then turns on, and the
  order covers the whole group, so `CurrentMoveGoals` can all fall inside the zone and the receipt matches.
- These are extra taps through visible cards only. They are bounded by the existing `Publish` limits. There is no hidden
  state and no new order.
- When the record and the tags agree, nothing changes.
- I assume a tap on an already-selected card deselects it (`TryDeselectGroup`) and the next tap re-selects. The EditMode
  case below should confirm that.

A separate, player-facing issue for the owner: a reinforced or attrited group shows as selected on the tray card while
its new members are not selected. Fixing that in the runtime is a gameplay change and larger than this ARIA fix:
- tag new members when they join a selected group;
- clear `record.Selected` when its last tagged member dies.

Which accepted missions it can touch:
- `row.Selected` has one consumer, `PublicObjectiveHud.cs:201` → `ExclusiveSelectedGroup`. That feeds the objective and
  escort orders of every expanded FC, BT and CE session:
  - FC: S009, S010, S031, S033–S036, S055–S057, S059.
  - BT: S014–S018, S037–S042.
  - CE: S019–S024, S043, S044.
- Behaviour changes only while the record says selected and some living member lacks the tag.
- Base assault (S002) and the roster drawer do not read this row. `RosterRevision` does not hash `Selected`, so no plan resets.

How to prove nothing else changes:
- EditMode, new cases on the projection / frame and on `StepPublicObjective`:
  - (a) Record selected with all living members tagged → `ExclusiveSelectedGroup` set (unchanged).
  - (b) Record selected, tagged members dead, untagged delivered members alive → not exclusive. The planner presses the
    selected card, then the group card, then Move. It never presses Move into a `NoSelection` refusal.
  - (c) Record selected with a partial tag set → not exclusive until re-selected.
  - (d) An unselected group stays unselected.
  - Rerun `AriaObjectiveSkillTests` (especially `RejectedObjectivePressesAreRetriedBeforeTheInputLayerStops`,
    `RosterChangeBetweenSelectPressesKeepsTheInputBound`, `ObjectiveOrderTimeoutIsRetriedNotBlocked`, the escort and
    garrison cases), `AriaObjectiveSupportTests` and `AriaObjectiveCoverageTests`.
- ARIA regression, on the Mac lane first (where it failed) and the Windows uilab. Run after an uncounted warm-up, with
  `--menu-journey --validation-library --no-table`:
  - `S058:155923:en`, `S058:104731:en`, `S058:130365:en`, `S058:130365:fa-IR`
  - FC `S055/S056/S057/S059:130365:en`, `S009:130365:en`, `S010:130365:en`, `S031:130365:en`
  - BT `S014:130365:en`, `S037:130365:en`
  - CE `S019:130365:en`, `S043:130365:fa-IR`
  - Require: no AriaBlocked, the existing win bands, and gate 3 PASS. Also check that `order_tapped` →
    `order_accepted` follows every chosen Move after a recruit.

### Lines still needed (to confirm, not to choose the fix)

- For Phase A: any `[SelectionClick] moveModeEntered result=…` lines, and any rejected world-move result, near 370.03 and
  497.81 (only if those diagnostics are enabled on the lane).
- The roster at the choice: which page-1 slot holds group 39 / 43, and how many members are alive versus selected. The
  existing traces do not log selected-member counts.
- A one-line trace at each `order_retry` would settle Phase A without a repro: `row.Selected`, living members, tagged
  living members, and `CurrentMoveGoals` inside/outside the zone.

## Status

- Visual review: not applicable. Automated checks: none run (diagnosis only, including the S058 follow-up). Normal-input playthroughs: none new.
- Real player and device acceptance: still pending.
