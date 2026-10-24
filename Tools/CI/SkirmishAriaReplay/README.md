# Headless ARIA planner replay

`skirmish_aria_replay.py` invokes the actual production `AriaSkirmishPlanSystem.Step` and every partial helper, retaining plan/session/output between observation frames. It compiles installed managed assemblies and never launches Unity.

```sh
python3 Tools/CI/skirmish_aria_replay.py build --repo . --build-dir /private/tmp/aria-build
python3 Tools/CI/skirmish_aria_replay.py template --build-dir /private/tmp/aria-build --output /private/tmp/aria-input.json
python3 Tools/CI/skirmish_aria_replay.py run --build-dir /private/tmp/aria-build --input /private/tmp/aria-input.json --output /private/tmp/aria-output.json
python3 Tools/CI/skirmish_aria_replay.py self-test --build-dir /private/tmp/aria-build --output /private/tmp/aria-checks.json
python3 Tools/CI/SkirmishAriaReplay/test_replay.py /private/tmp/aria-build /private/tmp/aria-cli-controls
```

Build and output paths must be new. The tool refuses overwrite. `--engine-dir` accepts an explicit directory containing the installed version's `UnityEngine.CoreModule.dll` and `UnityEngine.SharedInternalsModule.dll`; use it for a nonstandard/macOS installation or a Windows Editor's `Editor/Data/Managed/UnityEngine` directory. Installed .NET8 runtime and an installed SDK that can target it are required. Restore has no package/network sources. Runtime patch version, compiler, extracted source and managed assemblies are pinned; source changes during extraction/build reject.

The generated template contains **all** production fields. Field names use the source's exact case. Enums use exact names; vectors use `{x,y}` / `{x,y,z}`; the two uint fixed lists use JSON arrays, maximum127 entries. Unknown fields, unnamed enums, nonfinite/overflowing numbers, unsafe negative loop cursors, non-five-card masks, backwards observation clocks and bounds violations reject before executing a prefix.

Input:

```json
{
  "schema_version": 1,
  "mission_id": "S002",
  "provenance": {"kind": "synthetic", "detail": "Explicit synthetic example"},
  "compact_synthetic_defaults": true,
  "initial": {"plan": {}, "session": {"Phase": "Observing"}, "output": {}},
  "frames": [{
    "observation": {"Active": true, "ExpandedSession": true, "Frame": 1, "Time": 1},
    "feedback": {
      "session_patch": {},
      "receipt": {"id": "event-1", "status": "pending", "detail": "Annotation only", "target_id": 777}
    }
  }]
}
```

Compact recursive zero defaults require **both** an explicit `compact_synthetic_defaults:true` and synthetic provenance. Captured-labelled replay requires complete initial structs and complete observations. That label is user provenance, not a capture authenticity certificate. Initial state is bound once; later frames cannot replace the plan or feed planner output/accepted gameplay outcomes back automatically.

`feedback.session_patch` contains only explicitly supplied production session fields. For a known completed synthetic touch, callers can explicitly supply `Actions`, `TargetId`, `LastCompletedTargetId`, `LastCompletedFrame` and phase. The tool never generates these acknowledgements. Explicit nonnegative counter resets are retained with a negative `session_actions_delta`; negative absolute Actions values reject. A reset is not credited as execution or progress. Repeated IDs and accepted→completed updates are preserved as observations; consumers deduplicate them. Receipt status is one of `pending`, `accepted`, `completed`, `rejected`, `no_effect`, with a bounded nonempty id and bounded detail; optional `target_id` is explicit annotation. A receipt never changes `Actions`, credits progress, or proves gameplay command acceptance. Unknown executed target identity must stay unknown; never copy the proposed target into feedback without evidence.

Each result frame contains the complete observation, raw feedback, before state, session presented to Step after feedback, after state, intent, target, output kind and actual session-actions delta. The top-level initial state and provenance are retained. A trace checker can distinguish proposals, explicit touch feedback, and observed world progress. A planner `Blocked` transition is a replay result, not a runner crash.

Limits:16MiB input,64MiB output,1024 frames,32 typed nesting levels,1,000,000 decoded fields, fixed lists127. Original source loops receive admitted bounded slot/site cursors. The entire input is validated before the first Step; any later execution/serialization failure produces no result file.

Scope: the ECS `OnUpdate` lifecycle is omitted with exact source reconstruction proof; Step and all other production methods stay verbatim. Metadata ECS types and uint FixedList bindings do not implement a World. Real Unity managed vectors/math are used. The fixed list uses value-preserving copy-on-write, with the actual capacity; no raw layout/Burst/container-safety equivalence is claimed. The headless numerical lane is installed .NET8, not a native Unity parity claim. Source ECS owners/reset/session routing, Decision/Input timing, gameplay dispatch, world simulation, objectives and result/return still require their real integrations.

## Catch repeated actions and stalled support

Analyze the emitted replay with the companion checker:

```sh
python3 Tools/CI/skirmish_aria_trace.py --input /private/tmp/aria-output.json --output /private/tmp/aria-liveness.json
python3 -B -m unittest discover -s Tools/CI -p skirmish_aria_trace_test.py -v
```

The checker counts explicit completed-action counters and receipt observations separately from proposed targets. Accepted taps alone do not establish progress. It detects repeated rejected/no-effect receipts, repeated executed controls with unchanged public mission state, alternating action cycles and an observed planner watchdog stop. Changes to clock, frame, selected card or open drawer do not clear a stall. Repeated receipt IDs are deduplicated; absent execution evidence is reported as inconclusive rather than invented. A nonnegative session counter reset needs a separate trace for this liveness checker.

Defaults are five repeated actions and 15 seconds without relevant public-state change, or twelve actions in a cycle. Tune `--repeat-limit`, `--stall-seconds` and `--action-limit` to the scenario's legitimate preparation/service time. These are diagnostic thresholds, not gameplay rules. Relevant observation fields are listed in each report; adding a new mechanic may require adding its genuine progress fields to `PROGRESS_FIELDS` or calling `analyze` with an explicit field list.

The checker exits 0 for no detected liveness issue, 1 for a failed diagnostic and 2 for inconclusive execution evidence. A passed trace does not prove a win or full mission coverage. The next integration is the original decision/input and command-result bridge plus world advancement: public observation -> production planner -> command -> accepted/rejected result -> updated world -> next observation. Visual hit testing and normal-input result/return remain Unity acceptance checks.
