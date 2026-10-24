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

The checker exits 0 for no detected liveness issue, 1 for a failed diagnostic and 2 for inconclusive execution evidence. A passed trace does not prove a win or full mission coverage. The next integration is autonomous world advancement through original command results: public observation -> production planner -> command -> accepted/rejected result -> updated world -> next observation. Visual hit testing and normal-input result/return remain Unity acceptance checks.


## Persistent autonomous semantic session

`skirmish_aria_replay.py serve --build-dir BUILD` keeps one managed process for JSONL RPCs. The Python `AutonomousClient(BUILD)` exposes `initialize(initial)`, `step(observation, controls, context)`, `complete_contact(contact, receipt)` and `close()`. Always close the client (or use its context manager). The original `run` command remains a separate planner-only replay mode with explicit feedback; its patch interface is never accepted by the autonomous session.

Build against an explicit, version-matched `--repo` source root. The build extracts the original planner and every partial unchanged except its ECS OnUpdate, then the original `AriaPlayDecisionSystem.Step` unchanged except removal of its OnUpdate. The input completion body is copied exactly with frame/unscaled-time parameters substituted for its two clock reads. The build records reversible extraction hashes and rejects an unreviewed source gesture-timing expression. Physical InputSystem/OnUpdate, native UI, device contact, gameplay commands, acceptance, world advancement and mission readiness are not implemented here.

Initialize once with full typed `initial: {plan, session, output}`. A fresh source-eligible session has phase `Starting` or `Observing`, Actions=0, Pressed=0 and GestureRequested=0. There are no compact defaults, later plan/output resets, session patches or external action-counter updates in this mode. Starting preserves the source DueAt and observation-freshness delay within this semantic actuator profile; it does not claim hardware Start succeeded.

Each step supplies a complete public `AriaSkirmishObservation`, a unique registry `[{target_id, control_id}]`, and explicit lifecycle context `{frame, unscaled_time, ended, route_match, focused, allow_background, time_scale}`. IDs are opaque; this server interprets no command payload or world state. Context clocks are monotone, with one unscaled-time value per frame; observation clocks are independently monotone and cannot name a future frame. Source planner Step runs before source Decision Step. Source lifecycle terminal/focus/route/timeScale/manual/blocked stops cancel outstanding contacts.

A requested gesture binds its control ID at PRESS. A click holds .15 source seconds. A drag holds .5 and travels .9 source seconds, retaining distinct intermediate/final/release frame opportunities. This is a semantic timing profile, not a device or UI-event replay; it does not invent physical accepted samples or scroll movement. No contact request is emitted before virtual release and no Actions increment occurs at proposal, press or release. A released contact contains `{contact_id,target_id,control_id,position,press_position,drag,press_frame,started_at,frame,unscaled_time,scope}`. The world owner must revalidate the same offered control at release, dispatch through its original source command worker and return its actual receipt. It must not substitute a different same-pixel control.

`complete_contact(contact, {receipt_id, accepted, reason})` binds an exact unique contact and receipt at that release frame/time. Future Step is forbidden while this world receipt is pending, except explicit lifecycle cancellation. Both accepted and rejected gameplay commands finish the semantic contact: the extracted source body increments Actions, sets LastCompletedTargetId/frame/position, enters Verifying and sets DueAt to release+.65. Accepted does not establish an effect and a rejected/no-effect receipt never gets automatic objective progress or successful-gameplay credit. The command worker remains the authority for costs, IDs, acceptance and state effects. Duplicate/wrong/late receipts stop the session before another action.

Responses contain `request_id,ok,scope,event,state,contact,gesture_in_progress,source_receipt,trace_frame,closed`. State is `{plan,session,output}`. Step/completion trace_frame follows the legacy trace shape (`index,time,frame,observation,before,step_input_session,after,intent,target_id,observation_kind,feedback,session_actions_delta,event`). Completion feedback mechanically maps the actual receipt into `{id,status,detail,target_id}`, retaining raw source_receipt separately; it is not a fabricated result. Initialize and close have null trace_frame. RPC IDs must be consecutive and unique. Protocol failures poison and exit the worker; no process restart or state reseed occurs.

Pins cover source, build outputs, the installed managed/native runtime and engine math/vector assemblies. Full SHA checks occur at startup and lifetime end; every RPC checks admitted dev/inode/size/mtime_ns/ctime_ns before and after execution. Drift stops the owned process before another Step. Bounds: 16 MiB input line, 64 MiB output, one session/process, positive signed-int RPC/contact IDs and 4096 unique registry entries. These are tool admission limits, not gameplay budget changes.

Focused controls (synthetic observations/receipts; no mission or hardware acceptance):

```sh
python3 Tools/CI/SkirmishAriaReplay/test_autonomous.py --build-dir BUILD --evidence NEW_EVIDENCE
python3 Tools/CI/SkirmishAriaReplay/test_protocol_limits.py --build-dir BUILD --evidence NEW_BOUNDARY_EVIDENCE
```

The boundary probe reflects private state only in its separate test process, exercising IDs beyond 65536 and rejecting signed-int overflow. The production RPC exposes no counter-reset or state-injection operation. Buffered standard-input reads preserve one JSON request/response at a time.
