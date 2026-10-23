# Shared mission orchestration replay

Build and replay the production mission clock, fractional objective-time conversion, FC/BT/CE objective dispatch, Base Assault evaluation and result-freeze decisions without starting Unity. The native ECS adapters call the same `SkirmishMissionOrchestrationKernel` compiled byte-for-byte by this runner.

```sh
rtk proxy python3 -B Tools/CI/skirmish_mission_replay.py build --project . --output /private/tmp/mission-build
rtk proxy python3 -B Tools/CI/skirmish_mission_replay_cases.py --project . --output /private/tmp/mission-cases
rtk proxy python3 -B Tools/CI/skirmish_mission_replay.py run --build /private/tmp/mission-build --input /private/tmp/mission-cases/cases.json --output /private/tmp/mission-run
rtk proxy python3 -B Tools/CI/skirmish_mission_replay_test.py --build /private/tmp/mission-build --output /private/tmp/mission-tests
```

Use fresh output directories. Requires an already installed .NET 8 SDK and RTK; never downloads packages or launches Unity. The generator makes 1,800 **synthetic** traces parameterized by the existing 120 catalog entries, three sizes and five outcomes. These are rule regression probes, not successful mission playthroughs. It retains catalog/setup source hashes in its manifest.

## Trace contract

Top-level `schema_version: 1` and `cases`. Each case requires `id`, `family` (`BA`, `FC`, `BT`, `CE`), `size`, `session_id`, `setup_hash`, `initial_phase`, `legacy`, `deadline_seconds`, `area_ids`, `original_ids`, `steps` and a nonempty `expect`. Phase values must be exact named `SkirmishSessionPhase` members. Each independent case creates fresh reducer and session state; session/setup strings are caller-supplied provenance, not authenticated native identities.

Each step requires `delta_seconds`, `paused`, `surrender` and `world_facts`; optional `repeat`, `phase` and `expect`. Delta must be finite and nonnegative. `phase` is an explicit external lifecycle observation, not a simulated launch transition, and cannot overwrite a frozen result. No clock increments are supplied inside world facts: the runner projects the actual float clock, converts its delta using the native fractional accumulator, and fills Playing/Paused/SimulationDeltaTicks internally.

`world_facts` requires all fields of the corresponding production fact type except derived time/control fields. For BA, supply exactly `PlayerDesignatedAlive`, `EnemyDesignatedAlive`, `ReplacementBarracksPresent`, `FieldArmyWiped`. For FC, provide original-base presence/liveness and the three zone infantry counts. For BT/CE, provide exact original IDs and authoritative liveness/location/eligibility fields as in the generated examples. No missing fields silently become healthy units or open routes.

The BA sequence preserves native ordering: fact projection evaluates destruction/deadline before the later BA objective system reads surrender. Non-BA surrender bypasses collection. After an objective outcome, the runner freezes the result once, stops the clock, retains terminal BA facts and emits successful departure IDs exactly once. Save acknowledgment remains false because the runner does not execute persistence. Terminal rows remain replayable to catch resurrection or time advancement.

Snapshots expose phase, clock, fractional remainder, objective state, departures, result identities and freeze count. Expectations may match a subset of these fields but empty expectation objects are rejected. Output includes the captured input hash, complete source/build pins and process exit. Duplicate JSON keys/IDs, malformed identities, unknown fields, unsupported enum spellings, false expected wins, stale sources and altered binaries fail closed. Input is limited to 16 MiB, 10,000 cases/steps, 100,000 frames per case and a 120-second process timeout. Outputs cannot overwrite prior evidence or enter Assets, Packages, ProjectSettings or .git.

## Scope and native parity

This composes shared lifecycle/objective rules over supplied world facts. It does **not** autonomously produce combat, navigation, AI, economy, spawning, native tombstones/removals, save-journal transactions, scene transitions or UI. Use `skirmish_navigation_replay.py` for shared route retries/occupancy and `skirmish_combat_replay.py` for shared attacks, damage and infantry traces. These runners do not yet constitute a unified battle scheduler. Reports always say `full_battle_simulated=false`, `mission_qualified=false`, `world_facts_supplied=true`, `persistence_executed=false`.

Native `SkirmishMissionOrchestrationKernelTests.RunChecks` pins the original adapters/evaluator/collector, tests clock and phase parity, and reruns existing terminal/loss-count/fact-retention regressions. Mac native validation uses the mandatory repository GUI-licensing wrapper with explicit log/timeout/pass marker. Unity compilation and parity checks do not replace S074 and the two-mission normal-input acceptance test.
