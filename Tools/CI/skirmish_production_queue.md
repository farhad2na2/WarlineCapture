# Skirmish production queue: offline foundation v1

This bounded first slice validates pinned jobs and persists exclusive worker attempts.
It never launches Unity, changes game assets, marks a mission ready, or publishes a
configuration. It has no automatic stale-worker timeout: an old claim is still a
claim until its owner has been checked and the attempt explicitly interrupted.

## Commands

Run from the repository, with RTK. Put queue files outside `Assets`; for example an
ignored `Temp/SkirmishProduction` directory or an external evidence directory.

```powershell
rtk proxy python Tools/CI/skirmish_production_queue.py preflight --job <job.json>
rtk proxy python Tools/CI/skirmish_production_queue.py enqueue --job <job.json> --queue <queue-root>
rtk proxy python Tools/CI/skirmish_production_queue.py claim --directory <queue-root/job-id> --worker <worker-id>
rtk proxy python Tools/CI/skirmish_production_queue.py finish --directory <queue-root/job-id> --token <claim-token> --status interrupted --category infrastructure --signature host-interrupted
rtk proxy python Tools/CI/skirmish_production_queue_test.py
```

`preflight`/`enqueue`/`claim` require the project source files to exist with their
pinned bytes. Any error returns nonzero and keeps `native_qualification=pending`.
The returned claim supplies the unique artifact directory. It is allocated under
the queue job, rather than accepted from callers, so retries cannot reuse logs.

`finish` records `failed` or `interrupted` only. Use category `schema` for malformed
contracts, `driver` for helper/API/input-fixture defects, `game` for actual gameplay
defects or losses, and `infrastructure` for wrapper, licensing, host, storage or
connection failures. A repeated stable failure signature twice blocks another
claim. Repair the reproducer and enqueue a new pinned job ID; keep old attempts.

Never interrupt a job merely because its timestamp is old. Check the owning worker,
Editor and wrapper first. The short metadata transaction uses an exclusive
`.metadata-lock` directory. A process crash inside that transaction deliberately
leaves it fail closed. After proving no metadata writer remains, archive that lock
with the incident evidence before retrying; do not have an unattended sweeper erase
locks. A crash after writing a result can be finalized with the same token/status/
category/signature without overwriting the original result. Partial JSON after a
host/process failure fails closed and requires explicit investigation.

## Job format

All fields are required; unknown fields fail. Hashes are lowercase SHA256 hex.

| Field | Meaning |
|---|---|
| `schema_version` | Integer `1` |
| `job_id` | Unique safe identifier, maximum 80 characters |
| `catalog_id`, `definition_id`, `definition_version` | `S001`–`S120`, matching `skirmish.snnn`, positive integer version |
| `candidate_sha256`, `build_sha256` | Immutable candidate and built artifact identity; supplied by trusted capture owner |
| `setup_sha256`, `content_sha256` | Exact setup/content identity; not proof of legal placement |
| `source_manifest` | Nonempty array of `{path, sha256}`, project-relative forward-slash files; all checked against current bytes |
| `packet_path`, `packet_sha256` | Governing scenario packet, pinned and checked |
| `seed` | Integer packet seed, never string/bool |
| `locale`, `size`, `difficulty`, `control_mode` | `EN/FA`, `Standard/War/LargeWar`, `Recruit/Regular/Veteran/Commander`, `manual/aria` |
| `required_stages` | Exactly `compile`, `live_api`, `source_fit`, `normal_input`, `terminal`, `return`, once each |
| `timeout_seconds` | Integer 1–14400 |

The seed parser reads only the selected `## SNNN` section. It supports the packet's
three Standard/Regular baseline seeds, Standard/EN additional difficulty seeds,
and Regular additional size seeds in both locales. Combined non-Regular and
non-Standard configurations and family stress-seed plans are deliberately rejected
until they have a separate reviewed source. A seed merely occurring elsewhere in
the packet is insufficient. Manual jobs currently use these same reproducible
seeds; they do not establish unfamiliar-human acceptance.

The source manifest must be captured comprehensively by the candidate owner. The
offline tool checks the supplied files but cannot infer that every relevant native
dependency has been included. Commit SHA alone, caller hash strings and file
presence cannot prove candidate provenance or native readiness.

## Proposed adapter completion contract

`validate_completion()` is an offline consistency/negative-control API. It requires
job and attempt ownership, start/end source manifests, candidate/build/setup/content
identities, every stage passed, rendered terminal outcome, normal speed, zero
interventions, wrapper completion ownership/exit/timeout/pass marker, five distinct
hash-checked artifacts, input audit, real Menu launch, paid-resource invariant,
read-only observation, result return, isolated profile restore and clean session.
It calls the existing `audit_skirmish_native_contacts.audit` without weakening its
rules. Victory, Defeat and Draw are retained as distinct outcomes; a completed loss
is not a win. Synthetic tests stay labeled and return
`native_qualification=pending_adapter_canary` even when the envelope is consistent.

The native adapter is **not integrated**. `finish --status qualified` is rejected.
The offline checker cannot authenticate a fabricated receipt or replace the native
capture owner. There is no matrix aggregation, six-run/locale win-count judgment,
publication update, session reset actuator, Unity launcher or automatic scheduling
in this first slice. Those gates remain open.

## Next required canary

Add one small reviewed adapter to the existing Menu journey, rather than copying
S005 helpers. Through the checked platform wrapper, prove compile and live API
compatibility first, then use one job on each of two already-authored missions.
The adapter must capture the contract from actual ordinary Menu/setup/touch,
native contacts, result and return with unchanged relevant source bytes and a
restored isolated QA profile. Test missing/mismatched/old terminal, return, wrapper,
source and ownership artifacts against actual failed attempts before wiring a
success transition. Keep original failures. Preserve separate device/player and
visual-review gates. Do not use these offline tests as gameplay evidence.
