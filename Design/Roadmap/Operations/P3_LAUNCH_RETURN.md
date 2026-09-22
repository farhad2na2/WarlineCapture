# Operations P3 launch, return, and recovery

Recorded 2026-09-22. Package 3 owns the mode loop in `Game.Operations.Loop`. It does not author O001–O003, import Demo 2, or edit the shared scene, save, or localization seams.

## What landed

Assembly `Game.Operations.Loop` (no engine references; references `Game.Operations.Contracts`, `Game.Operations.Strategic`, and `Game.Operations.Tactical`):

| System | Behavior |
|---|---|
| `OperationsLoopSession` | Dashboard, briefing, deploy, launch, active mission, result, settlement, return, refund, and withdraw. Interrupt abandons the uncommitted step and reloads the last committed journal |
| `OperationsModeLaunchRequest` | `Mode=Operations`, `Exclusive=true`, `ReturnRoute=Operations`, `DispatchOwner=Game.Operations.Loop`, `InvokesSharedSceneView=false`. A Campaign or Skirmish slot blocks launch. An Operations slot blocks the other modes |
| `OperationsMissionProjection` | Builds `OperationsMissionResult` from the live tactical session. Settlement replays the saved result text, not a second projection |
| `OperationsCheckpointCodec` / `OperationsLoopStore` | Stage bytes, then publish the reference. Interrupt drops the staged or pending blob and keeps the last published checkpoint. Restore replays the order log into a fresh tactical session and fails closed when the rebuilt checksum differs |
| `OperationsLoopFrames` | Shell, briefing, HUD, and result read models. Route names match the shell strings without referencing `Game.UI.Contracts` |

The strategic attempt stays `Reserved` until settlement. Loop phases (`LaunchDispatched`, `Active`, `PendingResult`, `Settled`) live in the Operations journal. `SubmitResult` gained `leavePending` so a settlement commit can be interrupted; the two-argument overload still commits immediately.

`OperationsLaunchFixtures` compiles the package 2 Old Quarter and Civic Center greyboxes (`ops-greybox-d01-v1`, `ops-greybox-d02-v1`). A map without a greybox fails `missing_map`. These graphs are not catalog missions.

## Commit boundaries

Each boundary writes a pending commit and completes only on the matching retry. Interrupt before complete restores the previous revision.

1. Reserve (strategic deploy).
2. Launch dispatch.
3. Active mark. The tactical session is created only when that commit completes.
4. Checkpoint stage (bytes only).
5. Checkpoint publish.
6. Result journal.
7. Settlement (`SubmitResult` with `leavePending`).
8. Return acknowledgement.
9. Readiness-failure refund.

A duplicate deploy command returns the journaled result. A second command while an attempt is reserved is `AttemptConflict`. A settlement whose session is not the open attempt is `Conflict` (`stale-session`) and does not grant again. An identical hash for the same settlement key, with no other open attempt, returns the original receipt.

## Recovery

| State | Offered choice |
|---|---|
| Reserved or launch dispatched | Resume the reservation |
| Active, valid published checkpoint | Resume that tick, repair progress, cargo position, and wave arm time |
| Active, no checkpoint | Restart the same session, seed, snapshot, and AP (`restart count++`, tick 0), or withdraw |
| Corrupt checksum | Refund technical failure only. Resume and restart stay closed so the timer is not zeroed |
| Pending result | Settle that result |
| Settled, return not acknowledged | Return to the dashboard |

In-mission withdraw is the tactical order, then the true result. Strategic withdraw is only for a reserved attempt with no live mission. A frozen terminal result rejects withdraw, so a victory cannot be rewritten. Technical failure refunds one AP and applies no district penalty. A second refund is `PreconditionFailed`.

## Outcomes covered by the host checks

Live Old Quarter recon victory: D01 `40,46,45,48,38,22,45`, D02 unchanged, 120 credits (first clear 100 plus repeat 20) and 50 commander XP. Civic Center recon victory changes only D02. Withdrawn applies S-2 E+3 and does not refund AP. Practice and non-victory grant nothing. Campaign stars are not awarded. Back on Match pauses and does not pop or withdraw. Root stays blocked while reserved, launching, active, or waiting on settlement. Opening or closing the end-of-day report does not change the day or AP and does not end the day.

Host marker, 12 checks:

```text
[OperationsP3Validation] result=Passed checks=12
```

`Tools/Operations/check_p3.py` compiles contracts, strategic, tactical, loop, and `OperationsP3Checks` with the .NET SDK and requires that marker. The same checks are the Unity `executeMethod` `Game.Tests.Editor.Operations.OperationsP3Validation.RunFocusedValidation`.

## Game PM seams still closed

| Seam | Why it is still blocked |
|---|---|
| `SaveDataModel` / `SaveService` | The loop journal is Operations-owned. The profile envelope is not a field on the shipping save |
| `MatchSceneView.OperationMapLaunch` | The launch request is data. This package does not call the shared scene view |
| `OperationMapIdentityRules` | Shared validation still rejects operations map and scenario ids |
| Shared asmdefs and the localization catalog | Not edited. Shipping HUD binders stay a later integration |

## Programmer 2

Refresh `D:\Projects\WarlineCapture-Operations` onto this branch. Do not open `D:\Projects\WarlineCapture`.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsP3Validation.ps1
```

Required log marker: `[OperationsP3Validation] result=Passed checks=12`.
