"""Trusted baseline derivation plus append-only, exact-epoch slot transaction.

No Unity launcher. No caller-provided pass/facet receipt is an admission.
Current full source-fit/paid and native adapter-canary gates remain unimplemented:
this revision is deliberately incapable of granting an actual sample or credit.
"""
from pathlib import Path
import json
from skirmish_production_queue import require, fingerprint, load, write_new, now, guard
from skirmish_production_matrix_authority import SamplingAuthority, FIELDS, slot_key
from skirmish_production_operational_inspector import Reader, digest, _io_path
from skirmish_operational_compile_connector import inspect_immutable_claims as inspect_compiled

# No configurable provider/callback or saved boolean can remove these gaps.
# A source-reviewed successor must implement direct raw-evidence validation for
# each and exact-source normal/failure/interruption adapter-canary acceptance.
UNIMPLEMENTED = ("source_fit", "paid_resources", "adapter_canary_acceptance")
BASE = "Logs/SkirmishProduction/Credit/Baseline720"
PROTECTED = {"S001", "S025", "S073"}


def derive(project, completion_spec_ref, sampling_authority):
    require(type(sampling_authority) is SamplingAuthority and sampling_authority.kind == "Baseline720", "Complete canonical Baseline720 authority required; Stress1200 stays separate")
    require(Path(project).resolve() == sampling_authority.project, "Foreign sampling project")
    read = Reader(project)
    _, spec = read.obj(completion_spec_ref)
    require(set(spec) == {"schema_version", "compile_connector"} and type(spec["schema_version"]) is int and spec["schema_version"] == 1, "No caller pass/facet/provider fields allowed")
    connector = spec["compile_connector"]
    operational = inspect_compiled(project, connector)
    require(operational["result"] == "NormalCompletedUnqualified" and operational["errors"] == [] and len(operational["attempts"]) == 1, "One genuine completed normal attempt required")
    from skirmish_operational_compile_connector import merge_proof_pins
    merge_proof_pins(read, operational["proof_pins"])
    items = connector["operational"]["attempts"]
    require(len(items) == 1, "One actual attempt required")
    _, job = read.obj(items[0]["job"])
    _, immutable = read.obj(items[0]["immutable_claim"])
    claim = immutable
    require(claim == immutable and claim["job_sha256"] == fingerprint(job), "Actual canonical live/immutable job claim mismatch")
    sampling_authority.validate_job(job)
    require(type(job["schema_version"]) is int and job["schema_version"] == 1 and job["catalog_id"] not in PROTECTED, "Legacy/development/protected counterpart credit remains unsupported; denominator stays720")
    actual = operational["attempts"][0]
    require(actual["job_sha256"] == claim["job_sha256"] and actual["attempt_id"] == claim["attempt_id"] and actual["outcome"] in ("Victory", "Defeat", "Draw"), "Operational subject/outcome splice")
    missing = set(operational["admission"]["missing"])
    # Only an actually measured exact-candidate compile admission plus the
    # inspector's real METHOD/wrapper/normal lifecycle establishes this API scope.
    if "compile" not in missing:
        missing.discard("live_api")
    missing.update(UNIMPLEMENTED)
    subject = {k: job[k] for k in ("candidate_sha256", "build_sha256", "catalog_id", "seed", "size", "difficulty", "locale", "control_mode")}
    subject.update(job_sha256=fingerprint(job), attempt_id=claim["attempt_id"], worker=claim["worker"])
    read.stable();sampling_authority.recheck()
    return dict(result="PendingTrustedBaselineCompletion" if missing else "TrustedBaselineNormalSample", missing=sorted(missing), subject=subject, outcome=actual["outcome"], actual_native_wins=int(actual["outcome"] == "Victory"), slot={k: job[k] for k in FIELDS}, sampling_plan_sha256=sampling_authority.expected, proof_pins=[dict(path=str(p), sha256=h) for p,h in read.pins.items()], operational=operational, completion_spec=completion_spec_ref, job=job, claim=claim, whole_mission_accepted=False, player_device_accepted=False, stress_credit=0)


def _stable(project, refs):
    from skirmish_operational_compile_connector import merge_proof_pins
    read = Reader(project)
    merge_proof_pins(read, refs)
    read.stable()


def _committed_slot_owner(project, entry, entry_ref, facts):
    """The existing immutable attempt result is the commit point, not entry presence."""
    path = Path(facts["claim"]["artifact_directory"]) / "result.json"
    read = Reader(project);ref = dict(path=str(path), sha256=digest(path));_, result = read.obj(ref)
    require(all(result.get(k) == v for k,v in facts["claim"].items()) and
            result.get("status") == "completed" and result.get("native_qualification") == "accepted_single_baseline_sample" and
            all(type(result.get(k)) is int and result[k] == 1 for k in ("accepted_attempt_count", "baseline_sample_credit", "sampling_coverage_credit")) and
            result.get("whole_mission_accepted") is result.get("player_device_accepted") is result.get("publication_changed") is False and
            result.get("canonical_slot") == entry_ref and result.get("completion_spec") == entry["completion_spec"] and
            result.get("outcome") == facts["outcome"] and type(result.get("actual_native_wins")) is int and result["actual_native_wins"] == facts["actual_native_wins"],
            "Canonical slot lacks exact committed accepted owner result")
    read.stable();return ref


def _safe_directory(project, relative):
    root = Path(project).resolve();path = root / relative
    require(path.is_relative_to(root / "Logs/SkirmishProduction/Credit"), "Fixed canonical ledger namespace required")
    cursor = root
    for part in Path(relative).parts:
        cursor = cursor / part
        require(not _io_path(cursor).is_symlink() and not _io_path(cursor).is_junction(), "Ledger reparse path refused")
    _io_path(path).mkdir(parents=True, exist_ok=True)
    return path


def finish_baseline_sample(directory, token, project, completion_spec_ref, sampling_authority):
    # Derive before mkdir/result/claim/ledger writes. This revision always refuses
    # genuine missing measured gates; old IndependentlyAccepted... receipts fail.
    facts = derive(project, completion_spec_ref, sampling_authority)
    require(facts["result"] == "TrustedBaselineNormalSample" and facts["missing"] == [], "Trusted completion pending: " + ",".join(facts["missing"]))
    directory = Path(directory).resolve();root = Path(project).resolve()
    require(directory.is_relative_to(root / "Logs/SkirmishProduction/Queue"), "Canonical project queue directory required")
    epoch = dict(candidate_sha256=facts["subject"]["candidate_sha256"], build_sha256=facts["subject"]["build_sha256"], sampling_plan_sha256=sampling_authority.expected)
    ledger = _safe_directory(root, BASE + "/" + fingerprint(epoch))
    slot = facts["slot"];key = fingerprint(dict(zip(("catalog_id", "seed", "locale"), slot_key(slot))))
    entry_path = ledger / (key + ".json")
    # Global epoch then job guard, consistently ordered. No Unity run under lock.
    with guard(ledger):
        with guard(directory):
            record = load(_io_path(directory / "claim.json"));job = load(_io_path(directory / "job.json"))
            require(record == facts["claim"] and record["token"] == token and fingerprint(job) == record["job_sha256"] == facts["subject"]["job_sha256"], "Token/job/attempt ownership changed")
            require(load(_io_path(Path(record["artifact_directory"]) / "claim.json")) == record, "Immutable/live claim differs")
            _stable(root, facts["proof_pins"]);sampling_authority.validate_job(job)
            result_path = Path(record["artifact_directory"]) / "result.json"
            if _io_path(result_path).exists():
                prior_result = load(_io_path(result_path))
                require(all(prior_result.get(k) == v for k,v in record.items()) and
                        prior_result.get("status") == "completed" and prior_result.get("native_qualification") == "accepted_single_baseline_sample" and
                        prior_result.get("completion_spec") == completion_spec_ref and _io_path(entry_path).exists(),
                        "Existing failed/interrupted/conflicting result or missing canonical entry; refuse before any ledger append")
            duplicate = _io_path(entry_path).exists()
            if duplicate:
                reader = Reader(root);entry_ref = dict(path=str(entry_path), sha256=digest(entry_path));_, entry = reader.obj(entry_ref)
                require(entry["kind"] == "TrustedBaselineSlot" and entry["epoch"] == epoch and entry["slot"] == slot and entry["baseline_denominator"] == 720 and entry["stress_denominator"] == 1200, "Existing canonical slot entry conflicts")
                # An old entry is not trusted because it exists. Re-derive its
                # raw authority; forged entries and old candidate wins cannot count.
                previous = derive(root, entry["completion_spec"], sampling_authority)
                require(previous["result"] == "TrustedBaselineNormalSample" and previous["missing"] == [] and previous["subject"] == entry["subject"] and previous["outcome"] == entry["outcome"] and previous["slot"] == slot, "Existing slot is not attributable trusted evidence")
                require(previous["subject"]["candidate_sha256"] == epoch["candidate_sha256"] and previous["subject"]["build_sha256"] == epoch["build_sha256"], "Prior candidate/build cannot own this epoch")
                _stable(root, previous["proof_pins"]);reader.stable()
                owns_slot = entry["subject"] == facts["subject"]
                if owns_slot:
                    require(entry["completion_spec"] == completion_spec_ref, "Same attempt changed completion authority")
                else:
                    # Other attempts cannot consume a merely reserved slot. Only
                    # its original token owner may resume after entry-before-result crash.
                    _committed_slot_owner(root, entry, entry_ref, previous)
            else:
                owns_slot = True
                entry = dict(schema_version=1, kind="TrustedBaselineSlot", epoch=epoch, slot=slot, subject=facts["subject"], outcome=facts["outcome"], actual_native_wins=facts["actual_native_wins"], completion_spec=completion_spec_ref, baseline_denominator=720, stress_denominator=1200, created_utc=now(), whole_mission_accepted=False, player_device_accepted=False)
                write_new(_io_path(entry_path), entry)
                entry_ref = dict(path=str(entry_path), sha256=digest(entry_path))
            result_path = Path(record["artifact_directory"]) / "result.json"
            result = {**record, "status":"completed", "native_qualification":"accepted_single_baseline_sample", "accepted_attempt_count":1, "outcome":facts["outcome"], "actual_native_wins":facts["actual_native_wins"], "completion_spec":completion_spec_ref, "canonical_slot":entry_ref, "baseline_sample_credit":int(owns_slot), "sampling_coverage_credit":int(owns_slot), "coverage_status":"CanonicalSlotOwner" if owns_slot else "DuplicateCanonicalSlot_ZeroNewCredit", "whole_mission_accepted":False, "player_device_accepted":False, "publication_changed":False}
            if _io_path(result_path).exists():
                old = load(_io_path(result_path));require(all(type(old.get(k)) is type(v) and old.get(k) == v for k,v in result.items()), "Existing attempt result conflicts; preserve claim")
                result = old
            else:
                result["finished_utc"] = now();write_new(_io_path(result_path), result)
            # Crash after ledger entry or result creation resumes idempotently.
            # No pre-existing result/claim is overwritten or backfilled.
            _io_path(directory / "claim.json").unlink()
            return result


def baseline_coverage(project, sampling_authority, candidate_sha256, build_sha256):
    """Re-derive every counted entry; never trust file count or status booleans."""
    from skirmish_production_queue import sha
    require(type(sampling_authority) is SamplingAuthority and sampling_authority.kind == "Baseline720" and Path(project).resolve() == sampling_authority.project, "Full baseline authority required")
    require(sha(candidate_sha256) and sha(build_sha256), "Exact source/build epoch required")
    sampling_authority.recheck()
    epoch = dict(candidate_sha256=candidate_sha256, build_sha256=build_sha256, sampling_plan_sha256=sampling_authority.expected)
    folder = Path(project).resolve() / BASE / fingerprint(epoch)
    entries = sorted(folder / p.name for p in _io_path(folder).glob("*.json")) if _io_path(folder).exists() else []
    require(len(entries) <= 720, "More entries than canonical720 slots")
    accepted = {};pending = [];wins = {"EN":0,"FA":0};outcomes = {"Victory":0,"Defeat":0,"Draw":0}
    read = Reader(project)
    for path in entries:
        _, entry = read.obj(dict(path=str(path), sha256=digest(path)))
        require(entry["kind"] == "TrustedBaselineSlot" and entry["epoch"] == epoch and entry["baseline_denominator"] == 720 and entry["stress_denominator"] == 1200, "Ledger epoch/denominator differs")
        facts = derive(project, entry["completion_spec"], sampling_authority)
        require(facts["result"] == "TrustedBaselineNormalSample" and facts["missing"] == [] and facts["subject"] == entry["subject"] and facts["slot"] == entry["slot"] and facts["outcome"] == entry["outcome"], "Ledger record lacks current trusted raw authority")
        require(facts["subject"]["candidate_sha256"] == candidate_sha256 and facts["subject"]["build_sha256"] == build_sha256, "Old candidate/build credit transfer refused")
        key = slot_key(facts["slot"]);require(key not in accepted and sampling_authority.slots.get(key) == facts["slot"], "Noncanonical or duplicate slot")
        require(path.name == fingerprint(dict(zip(("catalog_id","seed","locale"),key))) + ".json", "Noncanonical slot filename")
        _stable(project, facts["proof_pins"])
        if not _io_path(Path(facts["claim"]["artifact_directory"]) / "result.json").exists():
            pending.append(dict(slot=entry["slot"],reason="CanonicalOwnerResultNotCommitted"));continue
        owner_ref = _committed_slot_owner(project, entry, dict(path=str(path),sha256=digest(path)), facts)
        read.obj(owner_ref)
        accepted[key] = entry;outcomes[facts["outcome"]] += 1;wins[facts["slot"]["locale"]] += facts["actual_native_wins"]
    read.stable();sampling_authority.recheck()
    return dict(kind="ProvenanceRevalidatedBaselineSlotCoverage",epoch=epoch,baseline_denominator=720,stress_denominator=1200,distinct_accepted_slots=len(accepted),missing_baseline_slots=720-len(accepted),pending_reservations=pending,outcomes=outcomes,wins_by_locale=wins,protected_slots_still_required=True,whole_mission_accepted=False,player_device_accepted=False,stress_credit=0)
