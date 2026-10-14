"""Unique ignored evidence by default; explicit append-only durable publication."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import tempfile


def new_evidence_directory(project, lane):
    if not re.fullmatch(r"[a-z][a-z0-9-]*", lane):
        raise ValueError("Simple evidence lane required")
    parent = Path(project) / "Logs/SkirmishProduction/Offline/ObjectiveEvidence"
    parent.mkdir(parents=True, exist_ok=True)
    output = Path(tempfile.mkdtemp(prefix=lane + "-", dir=parent))
    print("[SkirmishOfflineEvidence] scope=NewIgnoredRerun published=0 path=" + str(output))
    return output


def publish(project, source, label):
    project = Path(project).resolve()
    source = Path(source).resolve()
    allowed = (project / "Logs/SkirmishProduction/Offline/ObjectiveEvidence").resolve()
    if not source.is_dir() or not source.is_relative_to(allowed) or source == allowed or not re.fullmatch(r"[a-z][a-z0-9-]*", label):
        raise ValueError("Publish one exact owned ignored evidence directory with simple label")
    files = sorted(source.rglob("*"))
    files = [p for p in files if p.is_file()]
    if not files or any(p.suffix not in (".log", ".json", ".txt", ".rsp") or p.is_symlink() for p in files):
        raise ValueError("Only nonempty text/JSON/log/rsp evidence; never DLLs or links")
    parent = project / "Design/AgentReports/SkirmishExpansion/OfflineReruns"
    parent.mkdir(parents=True, exist_ok=True)
    output = Path(tempfile.mkdtemp(prefix=label + "-", dir=parent))
    manifest = []
    for path in files:
        relative = path.relative_to(source)
        destination = output / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(path, destination)
        manifest.append(dict(file=relative.as_posix(), sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    receipt = dict(action="CopiedEvidenceAppendOnly", source=str(source), output=str(output), files=manifest,
                   nativeExecutionCertified=False, validationOutcomeInCopiedLogsOnly=True)
    (output / "publication-receipt.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    return receipt


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("--label", required=True)
    args = parser.parse_args()
    print(json.dumps(publish(Path(__file__).resolve().parents[2], args.source, args.label), indent=2))
