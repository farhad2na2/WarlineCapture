# Smallest generic Menu job adapter: reviewed integration proposal

2026-10-14. Source inspection only. No C# edits or native evidence in this package.
Keep the active Unity compile/candidate freeze intact. The v1 offline queue remains
preparation, never mission acceptance.

## Public surface and ownership

Add a new partial of `SkirmishS002AriaRunHarness`, with one parameterless wrapper
entry `RunProductionJobFromEnvironment()` reading `WARLINE_SKIRMISH_PRODUCTION_JOB`
and `WARLINE_SKIRMISH_PRODUCTION_CLAIM` absolute paths. Reuse the existing private
`RunEditorJourney` and its genuine Menu/setup/contact/result machinery; do not
reflect into it or copy a mission runner. An internal typed `BeginProductionJob`
is sufficient for focused tests. Keep legacy public entry points unchanged.

Input is the exact queue `job.json` v1 plus the current attempt `claim.json`.
The coordinator runs offline preflight and claims the job before wrapper dispatch.
The adapter independently rechecks claim ownership, source bytes and job identity
before mutating Editor/session state. Native code should hash the exact immutable
job bytes separately if needed; `job_sha256` means Python canonical object hash,
not the indented JSON file hash. Do not silently substitute one for the other.

Start with an explicitly bounded Regular/Standard two-mission canary and reject
all unsupported size/difficulty/objective combinations before Play. A generic
parameter type does not establish UI selection or runtime support. Expand those
dimensions only after normal control selection and actual resolved setup agree.

## Required seams in existing code

| Existing seam | Small change required for a job path |
|---|---|
| `MenuProbe.cs:54` `RunEditorJourney` | Accept validated typed request/context; retain private defaults for old wrappers. Set `MAIN MENU` for first generic job path, since v1 requires `returned_to_menu`. Existing ARIA wrapper uses `ADJUST SETUP`. |
| `MenuProbe.cs:59–83` S005 catalog projection | Keep legacy S005 exposure. Add a small requested-ID candidate scope from the actual definition/runtime mapping, never a generic DesertBase index. Draft exposure stays Editor memory only and restores on every exit. |
| `SkirmishS002AriaRunHarness.cs:473` `TryCreateSelectedPayload` | On job path use validated definition/version/size/difficulty and `SkirmishAriaAcceptancePayload.TryCreate`; check resolved actual identity. Current unknown-ID branch falls back to S002. Unknown job IDs must reject. |
| `MenuProbe.cs:274` setup entry | Current seed fixture is explicit; verify actual setup controls/resolved record against all requested parameters after normal launch. New size/difficulty support needs genuine selection controls, not a hidden forced setup. |
| `SkirmishS002AriaRunHarness.cs:465,578,1187` report/trace routing | Job path writes only into claim's unique artifact directory. Keep legacy report routing for old wrappers. Avoid per-catalog report fallback and shared destination overwrite. |
| `SkirmishS002AriaRunHarness.cs:1456` `TryReadCensus` | Job path uses generic actual catalog census with selected setup; current dispatch defaults unknown IDs to S002 and hardcodes Regular/Standard. |
| `TerminalProbe.cs:84–226` terminal/return | Attach raw result/return event rows at existing observation points, with actual session ID, phase/outcome, designated HP, elapsed/speed/frozen clock, route, loading alpha, presented control identity, attempt cleanup counts and touch references. Derive claims later from these rows. |
| `SkirmishS002AriaRunHarness.cs:1215–1523` finish/exit | Link native artifacts to queue attempt before completion; preserve native outcome and failure reason. Do not reinterpret a non-counted Defeat/Draw as infrastructure failure or Victory. Do not add another Editor exit path. |

The existing `RunEditorJourney` selects normal Menu controls, scrolls to actual
scenario card position and waits for stable presented touch targets. Preserve that
flow. Existing S005 normal journeys already prove some infrastructure; they cannot
be renamed as another mission's evidence.

## State lifecycle and cleanup

Before any changes, reject active Play/compile/update, an existing harness session,
or an owned production context. Capture original scene setup and modified-scene
state; fail visibly if opening Menu would discard unrelated scene edits. Capture
the exact environment variables the harness changes, including previously absent
values, Editor enter-Play settings and refresh suspension ownership. The existing
normal path clears numerous diagnostic variables but does not restore the whole
environment. A single idempotent context restore must run after owned Play ends
and on pre-Play exception; never broadly clear another worker's state.

`LaunchCatalog` already creates a UUID-specific isolated profile and saves the
previous `WARLINE_VALIDATION_SAVE_ROOT`; `FinishEvidence` restores that prior root.
Keep that isolation. Add native records of actual profile root binding before
launch and restored root after owned Play exits. Do not write `profile_restored`
from a requested flag. Preserve candidate-catalog scope disposal,
`RestoreS004EditorJourneyOptions`, auto-refresh release and background input
restoration. A pre-Play exception after refresh suspension currently needs an
explicit unwind seam. Host interruption leaves the queue attempt incomplete;
never mark it successful based on a stale SessionState counter.

The current full source hash owner is `SkirmishCandidateSourceHash.Compute`: sorted
source extensions under Assets/Packages/ProjectSettings, binary path-length/digest
manifest, then SHA256. Keep its exact identity semantics; a JSON array hash is
different. For `candidate_sha256`, record this actual start/end value, rather than
echoing the caller. The explicit `source_manifest` checks relevant dependency files
but remains incomplete unless the capture owner independently enumerates scope.
Build identity likewise needs a real build artifact or reviewed Editor assembly
manifest, not an invented sentinel. Define actual setup/content digest owners
against the resolved census before allowing generic completion.

## Evidence contract: raw facts before derived summaries

The queue's `validate_completion` is an intentionally untrusted consistency check.
Its Boolean fields must be derived from native raw events and audited by a trusted
postprocessor. They must never be populated from job input. Add an append-only
`native-events.jsonl` with sequence, wrapper/queue attempt, job hash, native session
identity, native frame and event type. Preserve real contact/command JSONL using
the existing writer and strict contact auditor. Record references to raw rows in
the derived terminal, return and journey artifacts.

Obtain wrapper exit/timeout/method/pass-marker ownership from the checked wrapper's
actual completion record after native cleanup; C# cannot assert its own wrapper
exit. Compile/live-API/source-fit stage records need their original artifacts and
matching relevant dependencies, not `stages={...passed}` caller declarations.
Preserve exact terminal outcomes even when the wrapper's old counted-win behavior
returns nonzero for a completed Defeat/Draw. First canary may treat that as a
retained failed job, then add a distinct `completed_loss` state after review.

## Integration acceptance sequence

1. Add tested request parsing, unsupported-configuration rejection and cleanup
   context first. Run wrapper compilation and focused live API checks.
2. Add the smallest parameter dispatch and raw evidence seams, keeping old wrappers
   and S005 candidate flow compatible. Native canaries use two already-authored
   entries and actual packet seeds/locales; normal result must return to Main Menu.
3. Stop/restart the coordinator mid-attempt; prove exclusive ownership and retained
   incomplete attempt before starting another unique attempt. Verify actual profile,
   candidate catalog, refresh and Editor setting restoration after failure.
4. Feed native failed/old/mismatched artifacts into strict postprocessing; remove
   each terminal/source/input/return/ownership proof independently and require
   rejection. Compare the existing S005 qualifier's failure classification.
5. Only then wire queue success. Add matrix aggregation independently: six baseline
   journeys, three seeds per locale, two wins per locale and all other gates.

All 120 baseline packet sections are checked offline by
`skirmish_production_queue_test.py`: 720 job preparations using current packet bytes,
not 720 native runs. Family stress, additional exposed configurations and final
device/player acceptance remain separate work.
