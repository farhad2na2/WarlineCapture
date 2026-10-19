"""Read-only attempt-bound compile connector; all acceptance gates stay separate.

Schema3 consumes hash-pinned immutable receipt files, not an asserted backend
return value. Existing schema2 inputs are deliberately refused, never upgraded.
"""
from pathlib import Path
from skirmish_production_operational_inspector import need
from skirmish_compile_attempt_adapter import (checked_operational, evaluate_attempt,
    normalized_ref, receipt_path, canonical, merge_proof_pins, ERRORS)


def inspect(project, spec):
    need(isinstance(spec, dict) and set(spec) == {"schema_version", "operational", "compile_receipts"} and
         type(spec["schema_version"]) is int and spec["schema_version"] == 3,
         "Exact connector schema3 with immutable attempt receipt references required")
    result, read = checked_operational(project, spec["operational"])
    receipts = []
    try:
        refs = spec["compile_receipts"]
        need(isinstance(refs, list) and len(refs) == len(result["attempts"]), "Every actual attempt needs exactly one compile receipt")
        paths = set()
        for item, actual, ref in zip(spec["operational"]["attempts"], result["attempts"], refs):
            stored_ref, stored = normalized_ref(read, ref)
            need(set(stored) == {"schema_version", "admission", "result", "subject", "compile_proof", "backend_sha256", "evidence",
                                "qualified", "queue_success_enabled", "mission_ready", "publication_changed", "sampling_coverage_credit"},
                 "Exact typed immutable compile receipt")
            need(type(stored["schema_version"]) is int and stored["schema_version"] == 1, "Exact receipt schema1")
            expected, folder = evaluate_attempt(read, item, actual, stored["compile_proof"])
            path = Path(stored_ref["path"])
            need(path not in paths and path == receipt_path(folder, expected), "Unique canonical per-attempt compile receipt path")
            paths.add(path)
            need(canonical(stored) == canonical(expected), "Stored receipt disagrees with independently validated actual subject/proof")
            receipts.append(dict(receipt=stored_ref, subject=expected["subject"], result=expected["result"]))
            need(expected["result"] == "MeasuredNativeEditorCompileForExactCandidate", "Compile proof remains missing or failed: " + str(expected["evidence"].get("errors")))
        # The full batch union includes old operational pins, all receipt/proof bytes
        # and every backend source/output pin; a later attempt cannot mutate earlier proof.
        read.stable()
        result["proof_pins"] = [dict(path=str(p), sha256=h) for p, h in read.pins.items()]
        result["admission"].update(missing=[x for x in result["admission"]["missing"] if x != "compile"],
                                   compile_receipts=receipts)
    except ERRORS as error:
        # Preserve operational result, wins and every other admission facet.
        result["admission"]["compile_errors"] = [str(error)]
        result["admission"]["compile_receipts"] = receipts
    result.update(qualified=False, queue_success_enabled=False, mission_ready=False, publication_changed=False)
    return result
