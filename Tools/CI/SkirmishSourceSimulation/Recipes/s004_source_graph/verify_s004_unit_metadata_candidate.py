#!/usr/bin/env python3
"""Rebuild and compare an S004 source-only candidate before diagnostic use.

This check does not admit the candidate to a gameplay world or establish native
prefab resolution. The candidate builder rechecks its underlying source inputs.
"""
import argparse
import hashlib
import json
from pathlib import Path

import build_s004_unit_metadata_candidate as candidate


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON property: " + key)
        result[key] = value
    return result


def verify(path, expected_sha):
    if len(expected_sha) != 64 or any(c not in "0123456789abcdef" for c in expected_sha):
        raise ValueError("A lowercase SHA-256 is required")
    before = path.read_bytes()
    digest = hashlib.sha256(before).hexdigest()
    if digest != expected_sha:
        raise ValueError("Candidate SHA-256 mismatch")
    observed = json.loads(before, object_pairs_hook=unique_object,
                          parse_constant=lambda value: (_ for _ in ()).throw(ValueError("Nonfinite JSON: " + value)))
    rebuilt = candidate.build()
    # JSON distinguishes booleans from numbers even though Python equates
    # True and 1; compare canonical encodings to retain that distinction.
    canonical = lambda value: json.dumps(value, sort_keys=True, separators=(",", ":"), allow_nan=False)
    if canonical(observed) != canonical(rebuilt):
        raise ValueError("Candidate differs from freshly verified source projection")
    if path.read_bytes() != before:
        raise ValueError("Candidate changed during verification")
    return {"status": "PassedSourceOnlyCandidateRebuild", "candidate_sha256": digest,
            "unit_count": len(observed["units"]), "native_prefab_resolution": False,
            "runtime_admission": False, "mission_ready": False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("candidate", type=Path)
    parser.add_argument("sha256")
    parser.add_argument("--receipt", type=Path, required=True)
    args = parser.parse_args()
    result = verify(args.candidate, args.sha256)
    with args.receipt.open("x") as output:
        json.dump(result, output, indent=2)
        output.write("\n")
    print(json.dumps(result))


if __name__ == "__main__":
    main()
