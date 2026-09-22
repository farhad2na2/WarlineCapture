# Operations P2 tactical rules

Package 2 is the neutral tactical layer in `Game.Operations.Tactical`. It references only `Game.Operations.Contracts` and does not reference the engine. City rewards, scene launch, HUD, and ARIA stay in later packages.

## What this package runs

- Role binding and attempt-owned spawns (`SessionId`, stable object id, role id, group).
- Scan, Hold, Interact, Repair, Escort, and Extract. Other rule kinds fail closed in the compiler (`verb_not_in_package`) except the wave/clear dependency check, which reports `wave_clear_cycle` before that rejection.
- Objective graph activation, world facts, finite warned waves, and one frozen outcome.
- Dispatch is on `OperationsObjectiveRuleKind`. Mission ids are copied onto the session and are not read for control flow.

One tactical tick is one second. Channels do not advance without an issued order. Pause freezes the clock. A terminal result releases the attempt and does not spawn reserved waves.

Tick order: next-tick activation, queued deaths and destruction, movement, channels, exit facts, outcome, then waves. Wipe and failed required nodes are decided before Victory. Partial is only the authored predicate, via Conclude or the deadline. Withdraw is separate. Optional nodes do not gate Victory and do not arm waves.

## Greyboxes

D01 Old Quarter and D02 Civic Center are abstract meter plans in `OperationsMapGreyboxCatalog`. They are not Unity scenes. Demo 2 meshes are not imported. D03 Industrial Belt and D04 River Crossing art stay later; `opmap.operations.industrial_belt` is rejected as `missing_map` until that district has its own packet.

Both plans carry `spawn.player`, `spawn.enemy_a`, `spawn.enemy_b`, staging anchors, `exit.ground`, and `route.main` / `route.safe` / `route.flank`. D01's clinic is inside scan range of the player spawn. D02's clinic is not, so the same scan rule has to use the district's own anchors.

## Compiler rejections

The compiler fails closed on cycles, unreachable nodes, optional nodes gating required nodes, duplicate scan targets, duplicate singleton roles, bad or cross-district anchors, enemy counts above the package, reinforcement warning under 20 seconds, repair material floors, impossible deadlines, and a required clear whose members spawn only after that clear completes.

Default enemy groups use the 50% / 25% / remainder split. Group A warns when the first required node completes and arrives 30 seconds later. Group B warns when the final required node activates and arrives 45 seconds later. Occupied spawn anchors relocate that wave to the staging anchor inside the same budget.

## Not in this package

Rescue, civilian escort, evidence drop, Visit, Airlift, Stop, Breach, and full defense/finale composition remain package 5. Launch, HUD, and settlement remain package 3. O001–O003 content remains package 4.

`Game.Runtime` `ISystem` registration, `SaveDataModel`, `MatchSceneView`, shared asmdefs, and the localization catalog were not edited. Those stay Game PM seams. Windows Editor validation uses only `D:\Projects\WarlineCapture-Operations` and `Tools/Operations/Invoke-OperationsP2Validation.ps1`.

Host check, no Unity:

```bash
python3 Tools/Operations/check_p2.py
```

Pass marker: `[OperationsP2Validation] result=Passed checks=16`.
