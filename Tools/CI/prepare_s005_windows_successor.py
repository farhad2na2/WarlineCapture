#!/usr/bin/env python3
"""Prepare an external, dispatch-disabled S005 observer draft from preserved Git evidence.

This is preparation only. It never loads an assembly, starts Unity, reuses a Mac
permit, or qualifies gameplay. The coordinator must review the candidate and build
a portable checked-wrapper runner and strict qualifier before a native trial.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess

ARCHIVE = "Design/AgentReports/SkirmishExpansion/S005/native1628-executed-observer.cs.txt"
EXPECTED_ARCHIVE_SHA256 = "5e000245113dc390f731ebb05559bc3c851eef521c4b4edef34a6787664abd37"


def git(root, *arguments):
    return subprocess.run(["rtk", "proxy", "git", *arguments], cwd=root,
                          check=True, stdout=subprocess.PIPE).stdout


def prepare(root, output, run_id, native_identity=False):
    if not re.fullmatch(r"[0-9]{4,}", run_id) or int(run_id) <= 1631:
        raise ValueError("Use a unique numeric run ID above the historical 1631 draft.")
    if output.exists():
        raise ValueError("Preserve the existing output directory; choose a fresh run ID/path.")
    archived = git(root, "show", "HEAD:" + ARCHIVE)
    archive_hash = hashlib.sha256(archived).hexdigest()
    if archive_hash != EXPECTED_ARCHIVE_SHA256:
        raise ValueError("Archived observer differs from the native1628 recorded candidate SHA256.")
    source = archived.decode("utf-8")
    old = 'if(paid!=1004||!em.HasBuffer<SkirmishProductionReservation>(session))throw new InvalidOperationException("Expected exact native paid Heavy APC reservation1004");'
    new = 'if(paid==0||!em.HasBuffer<SkirmishProductionReservation>(session))throw new InvalidOperationException("Actual public paid Heavy APC reservation absent");'
    if source.count(old) != 1:
        raise ValueError("Expected exactly one known receipt-boundary defect.")
    source = source.replace(old, new)
    source = source.replace("Reservation1004 living ApcHeavy", "Actual public paid reservation living ApcHeavy")
    source = source.replace("Exact reservation1004 APC", "Exact public paid reservation APC")
    report = (root / "Design/AgentReports/SkirmishExpansion/S005").as_posix() + "/"
    old_report = 'const string Report = "/Volumes/T7/Projects/WarlineCapture/Design/AgentReports/SkirmishExpansion/S005/";'
    if source.count(old_report) != 1:
        raise ValueError("Expected exactly one archived report-root declaration.")
    archived_report = "/Volumes/T7/Projects/WarlineCapture/Design/AgentReports/SkirmishExpansion/S005/"
    path_replacements = source.count(archived_report)
    source = source.replace(archived_report, report)
    # Rename the immutable evidence identity, observer classes and callback owners together.
    source = source.replace("1628", run_id)
    if native_identity:
        fields="        public uint reservation;\n        public bool localizedObjectiveCopy"
        if source.count(fields)!=1:
            raise ValueError("Exact native binding row field boundary absent")
        source=source.replace(fields,"        public uint nativeSetupHash; public int definitionVersion; public string nativeContentIdentity=\"\";\n"+fields)
        binding="stage=\"ActualResolvedBinding\",catalog=setup.CatalogId,definition=setup.DefinitionId,size="
        if source.count(binding)!=1:
            raise ValueError("Exact ActualResolvedBinding authority boundary absent")
        source=source.replace(binding,"stage=\"ActualResolvedBinding\",catalog=setup.CatalogId,definition=setup.DefinitionId,definitionVersion=setup.ContentVersion,nativeSetupHash=setup.SetupHash,nativeContentIdentity=setup.ContentHash,size=")
    required = ["r.ReservationId==paid", "r.FactionId==1", "r.MaterialsPaid==280",
                "r.DeliveredMembers==1", "r.MemberCount==1", "o.ReservationId!=paid",
                "scoutMoveRequestId!=scoutMoveBaseline+1", "observedMovePath",
                'Write("PaidScoutStageHeld"', 'FileMode.CreateNew']
    if any(token not in source for token in required):
        raise ValueError("A preserved identity, paid cost, movement or evidence invariant is absent.")
    if "1004" in source or "/Volumes/T7/" in source:
        raise ValueError("A historical receipt/root remains; inspect before preparing.")
    head = git(root, "rev-parse", "HEAD").decode().strip()
    output.mkdir(parents=True)
    candidate = output / ("S005NormalNorthFlank" + run_id + "Observer.cs.txt")
    candidate.write_bytes(source.encode("utf-8"))
    metadata = {"result": "PreparedOfflineDraft", "runId": run_id,
                "gitHeadAtPreparation": head, "archive": ARCHIVE,
                "archiveSha256": archive_hash,
                "candidate": candidate.name,
                "candidateSha256": hashlib.sha256(candidate.read_bytes()).hexdigest(),
                "dispatchEnabled": False, "loadedInUnity": False,
                "nativeIdentityFieldsCaptured": native_identity,
                "normalMissionCertified": False, "playerReady": False,
                "portableReportPathReplacements": path_replacements,
                "patches": ["Actual public nonzero receipt instead of hard-coded 1004",
                            "New immutable evidence/observer identity", "Windows report paths"],
                "pending": ["Independent candidate review and connected compile/load proof",
                            "Fresh complete source fingerprint and candidate freeze",
                            "Portable checked-wrapper runner and strict raw north-flank qualifier",
                            "Fresh one-use release/permit for the current Windows Editor",
                            "Normal-input trial with exact actor effects and Victory/result/return"],
                "warning": "This draft is not the missing sealed 1631 package and inherits no historical release."}
    (output / "PREPARATION.json").write_text(json.dumps(metadata, indent=2) + "\n", encoding="utf-8")
    (output / "DISPATCH_DISABLED").write_text("Offline draft only; no executable runner or permit.\n", encoding="utf-8")
    print(json.dumps(metadata, indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--run-id", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--native-identity", action="store_true", help="Emit actual native uint32 setup/content/version fields; requires identity-aware qualifier")
    args = parser.parse_args()
    prepare(args.project.resolve(), args.output.resolve(), args.run_id, args.native_identity)
