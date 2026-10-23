# Offline Skirmish objective simulation

Priority: run cheap data/rule checks during authoring before native integration.
This tool executes current production objective rules on an installed .NET 8 SDK,
without Unity, physics, package downloads, or a realtime clock.

## What runs

- FC: current SkirmishFrontlineReducer (capture, contest, tickets, bases, deadline).
- BT: current SkirmishBreakthroughReducer (corridor hold, original identities, evacuation).
- CE: current SkirmishConvoyReducer (original trucks, continuous delivery dwell, deaths).
- BA: the exact facts-based TryEvaluate overload extracted verbatim from the current
  SkirmishBaseAssaultObjectiveSystem. No runtime file is edited. Its original file
  hash, method hash and source line are recorded. Extraction/compilation fails if
  the source structure no longer matches. The offline session retains terminal
  state, as the native system's terminal guard does.

The production FC/BT/CE files and contract files are copied byte-for-byte into a
new build directory. No draft reducers, Unity shims or independently rewritten
objective rules are used. The harness supplies input facts and checks expectations.
Every nested fact field must be explicit; missing/unknown fields, invalid types,
duplicate keys, false expectations and invalid reducer facts fail.

## Commands

Use a short, new output path each time. The example paths below are Windows paths;
on Mac replace D:/W with a local output root such as /private/tmp/warline-offline.
Run from the repository using RTK. Both systems use the same Python commands.

```powershell
rtk proxy python -B Tools/CI/skirmish_offline_sim.py build --project . --output D:/W/objective-build-01
rtk proxy python -B Tools/CI/skirmish_offline_sim_cases.py --project . --output D:/W/objective-cases-01
rtk proxy python -B Tools/CI/skirmish_offline_sim.py run --build D:/W/objective-build-01 --input D:/W/objective-cases-01/cases.json --output D:/W/objective-run-01
rtk proxy python -B Tools/CI/skirmish_offline_sim_test.py --build D:/W/objective-build-01 --output D:/W/objective-tests-01
```

Build once, then run many traces without rebuilding. Rebuild when any pinned
production rule, contract or harness file changes. Existing output directories
are never overwritten. Imported Assets, Packages, ProjectSettings and .git are
rejected as output destinations. Build/trace logs, copied input, source/build pins,
per-case snapshots and reports remain in the requested directories.

No SDK is installed automatically. The build selects an installed stable .NET 8
SDK, disables package feeds/auditing and compiler-server reuse. A missing SDK or
reference pack fails with preserved logs. The run uses the installed dotnet
executable rather than executing an arbitrary path from a build manifest.

## Input format

The root is {"schema_version": 1, "cases": [...]}. Each case requires:

- id: distinct trace identifier (a label, not proof of mission identity).
- family: BA, FC, BT or CE.
- size: Standard, War or LargeWar; interpreted by production Create functions.
- area_ids: 0 for BA, 3 FC zones, 2 BT corridors, 1 CE destination.
- original_ids: 12 distinct BT subjects, 3 distinct CE trucks, empty for BA/FC.
- steps: ordered facts records, or {"surrender_playing": true/false}.
- expect: nonempty subset of final serialized state fields to assert.

A facts step has {"facts": {...}, "repeat": 1, "expect": {...}}. repeat and step
expect are optional. Facts use the exact public production struct names and all
fields, including nested subject/zone facts. SimulationDeltaTicks uses TimeSpan
units (10,000,000 per second). repeat applies the exact same supplied facts on each
step; it never multiplies the time delta or waits for wall time. BA instead takes
its explicit ElapsedSeconds and DeadlineSeconds facts, so repeating BA facts does
not advance its clock. In BA, size is context only; the supplied deadline is the
actual input to the production evaluator.

The generated cases.json provides complete editable examples for every family.
For a compact CE trace, use three original IDs and one destination, then provide
five repeats of one-second Playing/unpaused facts with two original trucks alive,
authoritatively known and InsideDestination=true. Expected final state is
Outcome=Victory, Reason=ConvoyDelivered, Terminal=true. The generator emits that
full structure without omitted fields.

Expectation objects can select nested state fields; arrays compare their exact
length and entries. Each step-group report includes state and actual transition
IDs emitted by the reducer. One failed case does not hide other case failures.
Inputs are bounded to 16 MiB, 10,000 cases/steps and 1,000,000 iterations per case;
the driver also has a 120-second process timeout. Reports include elapsed time.

## Catalog probes and scope

The generator validates the actual catalog/matrix with the existing validator,
then produces five synthetic probes (pause/deadline/win/loss/surrender) for each
of 360 size rows: 1,800 probes. These parameterize shared objective logic by the
120 catalog entries and fixed deadlines. Most rows intentionally exercise the
same shared rules; this is not 1,800 independent full-mission playthroughs.

The tool does not yet generate movement, shots, damage, economy, production, AI,
transport, occupancy or world facts. You must supply those facts from a trace or
a future shared deterministic world simulator. It does not establish that a
mission's placements or routes can actually produce the asserted facts. Outputs
therefore retain full_battle_simulated=false and mission_qualified=false.

A full offline battle simulator is the next architectural layer: reuse the game
world's movement/combat/economy/AI kernels to produce these facts, then compare
its state against the native data systems. Do not substitute approximate battle
rules or remove native UI, ARIA, result/return or device acceptance gates.

## Verification

The test driver exercises the actual 1,800 generated probes, deterministic replay,
fractional capture/dwell boundaries, contested capture, pause, non-retroactive BT
unlock, surrender precedence, invalid/missing identity and input rejection,
false-expectation failure, source/binary drift and output isolation. All fixtures
are explicitly synthetic. Re-run on Windows before adopting it there.
