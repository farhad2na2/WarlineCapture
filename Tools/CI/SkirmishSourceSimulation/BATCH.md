# Sequential source-simulation jobs

This standalone POSIX wrapper calls the existing normal `run_skirmish_source_simulation.py run` once per job. It adds no simulator, admission, private entry, cache, resume or parallel execution. It does not grant native/UI/player readiness. A retained two-job S002/S026 five-tick canary completed successfully. Those diagnostic prefixes do not establish full mission outcomes or broader mission admission.

```sh
rtk proxy python3 -B /path/run_skirmish_source_batch.py \
  --jobs /path/jobs.json --output /fresh/batch-output \
  --repo /actual/source-root --package-root /actual/tools-root \
  --data-repo /actual/data-root
```

The roots are explicit and forwarded unchanged after canonical path resolution; a supplied build receipt must identify the same current source root. Normal admission/build/run performs its existing full source/runtime/input checks. The batch wrapper does not override source or metadata-origin roots. It records any source/backend fields actually reported by normal run/build receipts, without inventing a default.

`jobs.json` has exactly schema_version and jobs. Every descriptor has exactly path and lowercase SHA256. Relative descriptor paths resolve from the jobs file. IDs must be unique even under case folding and contain only letters, digits, underscore or hyphen (1..64 characters). Up to 360 jobs are permitted.

```json
{
  "schema_version": 1,
  "jobs": [
    {
      "id": "s002-104731",
      "mission": "S002",
      "seed": 104731,
      "data_bundle": {"path": "data-bundle.json", "sha256": "REPLACE_WITH_ACTUAL_64_LOWERCASE_HEX_DIGEST"},
      "timeout": 600
    }
  ]
}
```

Optional job fields are `build_receipt` and `configuration_qualification`, both using the same exact descriptor shape, and positive integer `max_ticks` for a diagnostic prefix. Qualification files and their parent paths must not contain symlinks. The batch pins and forwards the qualification; the normal CLI validates its contents and source identity. Omitting build_receipt delegates normal fresh build/preflight to the existing CLI; it is not batch caching. Omitting max_ticks preserves the normal mission deadline and natural outcome. The wrapper forwards each timeout to normal CLI. Its outer process-tree bound is timeout + 180 seconds to cover build/staging/cleanup, with a five-second TERM grace; only the session it creates is terminated. POSIX process groups are currently required. No clocks, actor counts or mission outcomes are changed.

Each job may override the command-line defaults with absolute `source_repo`, `package_root`, and `data_repo` directories. The batch pins the CLI in each selected package, checks a supplied build against that job's source root and backend, and checks each run receipt against that job's source and data roots. It does not assume that different missions use compatible builds. For example, a bounded S002 legacy job and S003 typed-source job can use separate roots and builds:

```json
{
  "schema_version": 1,
  "jobs": [
    {"id": "s002", "mission": "S002", "seed": 104731,
     "source_repo": "/source/s002", "package_root": "/package/s002", "data_repo": "/data/campaign",
     "data_bundle": {"path": "s002-bundle.json", "sha256": "REPLACE_WITH_ACTUAL_64_LOWERCASE_HEX_DIGEST"},
     "build_receipt": {"path": "s002-build.json", "sha256": "REPLACE_WITH_ACTUAL_64_LOWERCASE_HEX_DIGEST"},
     "max_ticks": 1, "timeout": 600},
    {"id": "s003", "mission": "S003", "seed": 104732,
     "source_repo": "/source/s003", "package_root": "/package/s003", "data_repo": "/data/campaign",
     "data_bundle": {"path": "s003-bundle.json", "sha256": "REPLACE_WITH_ACTUAL_64_LOWERCASE_HEX_DIGEST"},
     "build_receipt": {"path": "s003-build.json", "sha256": "REPLACE_WITH_ACTUAL_64_LOWERCASE_HEX_DIGEST"},
     "configuration_qualification": {"path": "s003-qualification.json", "sha256": "REPLACE_WITH_ACTUAL_64_LOWERCASE_HEX_DIGEST"},
     "max_ticks": 1, "timeout": 600}
  ]
}
```

The example is a format, not a set of admitted inputs: use real source-bound descriptors and digests. Diagnostic environment flags required by a mission must be set explicitly for the batch and remain subject to the normal CLI's gates.

JSON objects reject duplicate keys at every depth and all non-finite constants/numbers. This applies to jobs, bundles, build receipts and run receipts. Before output creation, the batch checks each declared job descriptor and any supplied build's source and executable pins. The normal CLI checks nested runtime inputs during dispatch; batch preparation alone does not establish their validity. An existing/symlink output root rejects without overwriting. Each job gets a separate container with `cli` as the fresh normal-CLI output, so normal `cli-smoke` siblings cannot collide across jobs. `reproduce.json`, the process log and raw normal outputs/receipts remain in place. Input drift prevents further dispatch and is recorded as incomplete jobs. Jobs otherwise continue sequentially after failures; no job is retried.

`batch-receipt.json` records exact argv, explicit inputs/hashes, normal receipt path/hash/status/backend, CLI exit, worker exit through the raw receipt, and per-job classification. Missing, multiple, malformed, contradictory or wrongly bound receipts never complete. Unknown/unsupported receipt statuses remain unproved. Timeout, source process failure or unexpected surviving owned descendants never complete.

An actual normal `SourceOutcomeObserved` + worker exit0/non-timeout + Victory + validation_passed=true + CLI0 is execution-complete and victorious. A consistent Defeat has worker0, full_outcome_observed=true, validation_passed=false and normal CLI1; it is execution-complete **without** being a Victory. The CLI1 and raw receipt remain unchanged. A valid requested BoundedPrefix is execution-complete but is not a full outcome or Victory. These are receipt protocol interpretations, not new gameplay evidence.

The batch exits0 only when all jobs are execution-complete, with status `Completed`; this does not mean all missions were victorious. An explicitly unqualified terminal diagnostic is recorded as `DiagnosticSourceOutcomeObserved` with its source outcome, `validation_passed=false` and `mission_victory=null`; missing legacy qualification fields remain unqualified. A bounded prefix has no terminal outcome. `all_missions_victorious`, counts and every raw status remain separate. Incomplete jobs return1; invalid batch inputs return2. No aggregate `Passed` or readiness flag is synthesized.

## Controlled tests

```sh
rtk proxy python3 -B /path/test_skirmish_source_batch.py
```

The controlled tests exercise the production wrapper through labeled controlled subprocess seams, including fake Popen/signal timeout branches. They do not launch any process, World, Unity or real mission and do not certify mission validation. They validate complete120-ID and360size-row job manifests as protocol inputs only; this does not admit unsupported missions. Optional `--real-receipt PATH --real-receipt-sha256 SHA256` checks a retained validated, non-diagnostic S002/104731 Victory receipt without rerunning it. Run `Tools/CI/test_skirmish_source_batch_mixed_roots.py` for per-job root forwarding and pin-drift controls. For diagnostic receipt and qualification-path controls, run `Tools/CI/test_skirmish_source_batch_diagnostic.py` with `--s002-receipt`, `--s003-prefix`, `--s003-terminal`, `--qualification`, and `--bundle` pointing to the corresponding retained inputs; diagnostic outcomes remain unqualified.

### Sampled source-run evidence

To summarize an existing run without changing its receipt or batch status, run
`python3 Tools/CI/audit_skirmish_source_evidence.py /absolute/path/to/run`.
The directory must contain `run-receipt.json` and `validator-state.jsonl`. The
summary hashes and reads the sampled validator records, reports snapshot tick
ordering and whether the last sampled tick matches the terminal tick, and
counts sampled producer-reported material-counter balance statuses and queue
diagnostics, and repeats the last sample’s conservation fields. Missing or
malformed evidence is reported as unavailable. This comparison does not verify
that the receipt and snapshots came from the same world, establish liveness or
conservation between samples, or establish native acceptance or mission
readiness. It does not alter admission or batch aggregation.

For the specific S002/104731 recorded run with one queued production chain,
`python3 Tools/CI/skirmish_s002_observed_ledger.py /absolute/path/to/run`
checks recorded queue, producer binding, start, deliveries, produced actor
identities and terminal outcome. Other production sequences are unsupported.
It compares the result with the receipt, checks the subject against the request
and terminal, and reports input hashes. Hashes identify inspected bytes; they do
not authenticate a capture. Terminal session and setup identity are not bound
to the request or receipt. Death releases, rejected operations, startup before
observer installation, other economy and fuel history, command receipts,
general liveness, and full conservation are not established. The output is
diagnostic only and does not establish native or player acceptance.

Run the retained-trace and tamper controls with
`S002_LEDGER_RECORDED_RUN=/absolute/path/to/run python3 -m unittest discover -s Tools/CI -p test_skirmish_s002_observed_ledger.py -v`.
