#!/usr/bin/env python3
"""Verify recoverable paid-air source references without inventing a normal observer.

Writes a compact recovery manifest only. It never copies prerequisite sources,
loads assemblies, stages Assets, starts Unity, or authorizes gameplay.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

REPORT = "Design/AgentReports/SkirmishExpansion/S005/"
PREREQUISITES = [
    ("native1548-fixture.cs.txt", "native1548-paid-air-effects-pass.json",
     "b4a068665200a81262937a0346abb88158d57e4d0cddb881214e8d301fdfc906",
     "Historical isolated exact paid AntiAir/Fighter native airborne effects; temporary setup disclosed"),
    ("native1564-fixture.cs.txt", "native1564-paid-plane-cargo-pass.json",
     "76f695478614f5032da43f0d82b5d5798d80fc4c982cea65b398db8869698b37",
     "Historical isolated exact paid Plane cargo/flight/Return/unload; temporary funding/readiness/fuel/enemy setup disclosed"),
]


def git(project, *arguments, allow_no_match=False):
    process = subprocess.run(["rtk", "proxy", "git", *arguments], cwd=project,
                             stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if process.returncode and not (allow_no_match and process.returncode == 1):
        raise RuntimeError(process.stderr.decode(errors="replace"))
    return process.stdout


def prepare(project, output):
    if output.exists():
        raise ValueError("Preserve existing recovery output; use a new output directory.")
    references = []
    for fixture, result_file, expected, scope in PREREQUISITES:
        path = REPORT + fixture
        raw = git(project, "show", "HEAD:"+path)
        actual = hashlib.sha256(raw).hexdigest()
        result_raw = git(project, "show", "HEAD:"+REPORT+result_file)
        result = json.loads(result_raw)
        if actual != expected or result.get("fixtureSha256") != expected:
            raise ValueError("Historical fixture/result SHA256 mismatch: " + fixture)
        references.append(dict(git_path=path,
            git_blob=git(project,"rev-parse","HEAD:"+path).decode().strip(),sha256=actual,
            result_path=REPORT+result_file,result_sha256=hashlib.sha256(result_raw).hexdigest(),
            historical_source_hash=result["sourceHash"],historical_result=result["result"],
            original_scope=scope,normalMissionCertified=False,
            source_copied=False,wrapperExit=result["wrapperExit"],nativeExit=result["nativeExit"]))
    search = git(project,"grep","-l","-e","S005NormalPaidCargo","-e","class S005NormalAirObserver",
                 "-e","class S005NormalPaidAir","HEAD","--",REPORT+"*.cs.txt",REPORT+"*.py.txt","Tools/CI/*",
                 allow_no_match=True).decode().splitlines()
    # The verifier's own strings are expected if it is subsequently committed.
    search = [line for line in search if not line.endswith("Tools/CI/prepare_s005_windows_paid_air.py")]
    manifest = dict(result="NormalSourceCandidateFound_ReviewRequired" if search else "VerifiedHistoricalReferences_NormalObserverMissing",dispatchEnabled=False,
        sourceRecoveryOnly=True,normalObserverPrepared=False,playerReady=False,
        head=git(project,"rev-parse","HEAD").decode().strip(),references=references,
        tracked_normal_observer_search_matches=search,
        search_scope="Tracked S005 *.cs.txt/*.py.txt and Tools/CI; excludes opaque compressed evidence blobs",
        missing=["Normal paid-air1630 observer and its normal-budget public input strategy",
                 "Normal paid-air1630 strict raw qualifier and 28 original negative controls",
                 "Normal1630 package source/build/setup pins and dependency list",
                 "Portable source-reviewed Windows successor with fresh compile/load and raw capture proof"],
        historical_only=["Mac PID37475/assembly/MVID no-reload state","sealed /private/tmp1630 packages and permits"],
        next_action="Recover exact normal package if accessible, otherwise design a new normal public-input observer; do not rename isolated fixtures")
    output.mkdir(parents=True)
    (output/"RECOVERY_MANIFEST.json").write_text(json.dumps(manifest,indent=2)+"\n",encoding="utf-8")
    (output/"DISPATCH_DISABLED").write_text("No normal observer source recovered or prepared. Historical prerequisite references only.\n",encoding="utf-8")
    print(json.dumps(manifest,indent=2))


if __name__ == "__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project",type=Path,default=Path(__file__).resolve().parents[2])
    parser.add_argument("--output",type=Path,required=True)
    args=parser.parse_args()
    prepare(args.project.resolve(),args.output.resolve())
