# Windows dev results — W32 pacing sweep, DB CE S019–S024 (2026-11-19)

Branch `missions/win-w32-aria-starting-coverage` at `e60e40146` (the W32 build). The Windows dev lead did this task. The lead asked for 12 non-counted runs on seeds 155923 and 130365 (en, Standard Regular). Log: `win-dev-unity\w32-pacing-db-ce\menu\unity.log`. Gate 3 from `Tools/Missions/gate3_objective.py --summary`.

**Outcome:**
- **12/12 Victory.**
- **The median is 50.09 % of 420 s, just above 50 %.** The rows split by start package:
  - field rows win at 41.7–43.6 %;
  - established rows win at 56.6–57.8 %.
- **Gate 3: 4 of 6 rows pass.** S022 and S024 (air/combined established) fail on both seeds: the starting **transport heli** is never used (section 3).
- **Proposed deadline** (section 2), not applied.

## 1. Runs (game `deadline=420`)

| Row | Start | 155923 | 130365 | Gate 3 |
|---|---|---|---|---|
| S019 CE ground field | field | Victory 175.2 s, **41.73 %** | Victory 176.5 s, **42.02 %** | PASS 2/2 |
| S020 CE ground established | established | Victory 238.1 s, **56.68 %** | Victory 237.8 s, **56.63 %** | PASS 2/2 |
| S021 CE air field | field | Victory 178.0 s, **42.39 %** | Victory 182.9 s, **43.55 %** | PASS 2/2 |
| S022 CE air established | established | Victory 242.4 s, **57.71 %** | Victory 242.8 s, **57.81 %** | **FAIL** 0/2 (transport_heli ×2) |
| S023 CE combined field | field | Victory 177.6 s, **42.29 %** | Victory 182.1 s, **43.35 %** | PASS 2/2 |
| S024 CE combined established | established | Victory 237.8 s, **56.63 %** | Victory 238.7 s, **56.84 %** | **FAIL** 0/2 (transport_heli ×2) |

- No `aborted=True` lines. `roster=match` everywhere.
- Harness evidence (55 MB) moved to `w32-pacing-db-ce\harness-evidence`, not committed.
- **Comparison:** the Mac lane-43 counted runs (pre-W32, 420 s) won at 30–50 %. W32 adds about 60–90 s to the established rows, which spend escort turns on their larger starting forces (`units_used_by_aria` includes car, APC, tank, AA, gunner and rifle).

## 2. Proposed family deadline (data only; agents may tune deadlines)
The median win time over the 12 runs is **210.4 s**.

| Deadline (Standard) | min % | max % | median % |
|---|---|---|---|
| 420 (current) | 41.7 | 57.8 | 50.1 |
| **525 (proposed)** | **33.4** | **46.2** | **40.1** |
| 600 | 29.2 | 40.5 | 35.1 |

- **Proposal:** DB CE S019–S024 Standard **420 → 525**, War 583 → **729**, LargeWar 700 → **875**. This keeps the W31 ratios ×1.389 / ×1.667, and every measured win lands inside 25–50 %.
- **Alternative:** split by start package. Field stays 420 (42–44 %); established gets 600 / 833 / 1000 (about 40 %). That touches only S020, S022 and S024.
- **Not applied.** It's the lead's call.

## 3. Transport heli: needs more than an order path (split out, as the lead asked)
- **Eligibility is already fixed.** W32 makes `TransportHeli` eligible as an escort (`AriaObjectiveEscortPolicy.Eligible`, test `StartingGroupsOfEveryRoleAreEscortCandidates`).
- **It is never offered.** In S022, escort offers went to groups 1–8 and 29, never to the heli group. A candidate needs a presented objective target, and those are built only for groups the HUD can navigate to.
  - `SkirmishMatchView.PublicObjectiveHud.cs:196-199`: `navigation = row.Policy.Dismounted ? GroupId : UnloadCarrierGroupId`, and `tray.TryGetExpandedCardTarget(navigation, …)`, else `continue`.
  - The heli row does not pass this, so it has no `Target` and `Pick` skips it.
  - Which half fails (`Dismounted` for an aircraft, or the tray card) is unproven.
- **What it needs.** New HUD target behaviour, so an air group gets a presented objective target and a Move ground point. That is outside W32's order-path change.
- **Proposed as W32b:** give own air transport groups a presented target in `PublicObjectiveHud`, then prove an accepted Move receipt on S022/S024.

## 4. S047 ConvoyImpossible (cheap look; W32 log `w32-menu`)
- Truck group 15 is ordered to the service pad at 126.6 s (`ServiceOriginalTruck`). It holds there until it resumes the escort at 268.4 s, a detour of about 140 s.
- The first truck loss shows at 312.8 s, and the match ends as ConvoyImpossible at 339.2 s.
- **Likely cause (unproven):** the service detour costs the convoy time while the enemy builds up on the route. Possible fixes: a tighter service trigger or shorter service in CE combined field. Not changed here.

## Open
- **Deadline proposal** (section 2): lead decision.
- **W32b:** transport-heli objective target (section 3).
- **S047:** service-detour loss.
- W27b next.
