# Shared standard-attack planner and offline checker

The production `UnitAttackSystem.PlanStandardAttacksJob` delegates to
`Game.Runtime.Combat.StandardAttackPlanner`. The same source file compiles on
.NET 8 without Unity, package downloads, graphics or physics. This allows quick
checks of attack configurations and candidate snapshots before native tests.

## Commands

Use fresh, short output paths. Replace `/private/tmp/...` with `D:/W/...` on Windows
and use `python` instead of `python3` where appropriate.

```sh
rtk proxy python3 -B Tools/CI/skirmish_combat_replay.py build --project . --output /private/tmp/combat-build-01
rtk proxy python3 -B Tools/CI/skirmish_combat_replay_test.py --build /private/tmp/combat-build-01 --output /private/tmp/combat-tests-01
rtk proxy python3 -B Tools/CI/skirmish_combat_replay.py run --build /private/tmp/combat-build-01 --input /private/tmp/combat-tests-01/examples.json --output /private/tmp/combat-run-01
```

The tests write `examples.json` containing 30 complete, editable candidate cases.
Copy that file outside Assets, change the inputs and expected results, then run
many times against one build. Any change to the kernel, runtime adapter, harness
or driver requires rebuilding; their exact hashes and the binary hashes are
checked before and after every run. Outputs preserve the captured input, process
logs and each actual plan. No run silently overwrites previous evidence.

## Input

Root: `{"schema_version":1,"cases":[...]}`. Each candidate has:

- `id`: unique nonempty label (not evidence of mission identity).
- `input`: every field of `StandardAttackInput`, including every nested `State`
  field. Positions are XZ world coordinates, ranges/radii use world units, and
  clocks use seconds. All values must be finite; byte flags must be 0 or 1.
- `delta_time`: finite, nonnegative seconds for this planner invocation.
- `cell_size`: finite, positive world units.
- `expect`: nonempty subset of output fields to compare exactly, including
  `Kind` (`None`, `ClearTarget`, `Windup`, `Shot`), `BaseState`, `State`, `Damage`
  and `ShowTracer`. Float expectations use the emitted JSON float values with no tolerance;
  equivalent number spellings such as `1` and `1.0` compare equally.

Each row is an independent, complete candidate snapshot. This checker does not
advance one row's state into the next, decrement animation clocks, select targets
or invent health/damage facts. To replay a sequence, supply each authoritative
snapshot explicitly. Empty/unknown/missing fields, duplicate JSON keys and IDs,
invalid types, false expectations and stale builds fail. Errors are aggregated
across independent cases. Limits: 16 MiB input, 10,000 cases and a 120-second
process timeout. Exit zero requires all expectations to pass.

## Production behavior preserved

The shared planner retains base cooldown/trace ticking, invalid-target clearing,
horizontal range with combat radii and a quarter-cell allowance, aircraft-window
checks, windup, shot cooldown, animation duration, tracer cadence and phase.
Windup uses the original cooldown as well as the post-tick eligibility check.
`BaseState` remains separate because a planned shot can be discarded by the host
when an earlier attack has exhausted the target's predicted health.

Production targeting and eligibility filters, no-engagement recovery, animation
system ticks, support damage adjustments, simultaneous-hit ordering, damage
aggregation, audiovisual effects and objective evaluation remain in their current
systems. A zero delta alone is not a pause gate inside this planner; the host owns
pause/terminal dispatch. The API preserves that behavior instead of changing it.

## Validation layers and next priorities

The offline suite checks 30 boundaries, 10,000-case repeated deterministic output,
malformed input, stale source/binary rejection and safe output isolation. Native
`StandardAttackPlannerTests` compares the new kernel against the original method
with actual Unity math, exercises Burst and checks the production ECS adapter.
Run its focused entry point through the checked repository wrapper and require
its explicit pass marker; offline passes are not substitutes for native checks.

On macOS, keep Hub signed in, confirm this project has no active Editor owner,
then use the checked GUI-licensing wrapper (or its supported `--reuse` mode for
an admitted idle Editor). Protect unrelated Editors.

```sh
rtk proxy Tools/CI/invoke_unity_macos.sh --timeout 900 --log /private/tmp/combat-native-01.log -- -quit -executeMethod Game.Tests.Editor.StandardAttackPlannerTests.RunFocusedValidation
```

Require wrapper exit zero **and** `[StandardAttackPlannerParity] result=Passed`
in the complete log, with no compile failure or failed-test marker. A zero Unity
exit on compilation failure is a failed validation. On Windows use
`InvokeUnityExecuteMethodValidation.ps1` with the same method and required marker,
the resolved project Editor version, explicit timeout and a fresh full log.
The oracle records both raw and canonical LF hashes for explicit CRLF portability;
only the test fixture permits this normalization. Runtime/build pins remain byte-exact.

This is one shared combat kernel, not a complete mission simulator. Reports keep
`full_battle_simulated=false`, `mission_qualified=false` and `native_runs=0`.
Continue with shared damage commit semantics, movement/path following, targeting
and AI/economy kernels; then produce objective facts from those systems and
compare native/offline traces. Mission UI, ARIA, result/return and device acceptance
remain separate gates. Keep map reuse policy, catalog bindings and gameplay budgets.

## Initial verification, 2026-10-23

Seven standalone test groups passed with no skips, including 30 boundary cases
and two identical 10,000-candidate batches. The Mac native focused suite passed
five parity tests plus six existing combat tests, with an explicit compiled-Burst
sentinel and unchanged source/settings hashes. Earlier compilation failures were
retained and repaired before the successful run. The successful Unity shutdown
reported four persistent allocations without attribution; that lifecycle warning
remains open and is not represented as a clean leak check or mission acceptance.
