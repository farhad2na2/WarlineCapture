#!/usr/bin/env python3
"""Expose the already-collected passive path trace in an experimental S003 result."""
import hashlib
from pathlib import Path

SOURCE = "Tools/CI/SkirmishSourceSimulation/Adapters/S026/294-312_PaidSourceOwnerStep.retry.s003.cs"
OUTPUT = "SourceS003PathTraceResultOwner.cs"


def transform(source):
    yes = 'new JObject{["enabled"]=true,["acquisition_samples"]'
    no = 'new JObject{["enabled"]=false}'
    call = 'ground.PathPhase()'
    if source.count(yes) != 1 or source.count(no) != 1 or source.count(call) != 1:
        raise ValueError("Exact S003 workload result boundary required")
    return source.replace(yes, 'new JObject{["enabled"]=true,["path_schedule_trace"]=ground.SourcePathScheduleTrace,["acquisition_samples"]', 1).replace(
        no, 'new JObject{["enabled"]=false,["path_schedule_trace"]=ground.SourcePathScheduleTrace}', 1).replace(
        call, 'ground.PathPhase(checked((int)Tick))', 1)


def generate(repo, output):
    root = Path(repo).resolve(strict=True); path = root / SOURCE
    original = path.read_bytes(); rendered = transform(original.decode("utf-8")).encode("utf-8")
    if path.read_bytes() != original:
        raise ValueError("Result adapter changed during generation")
    target = Path(output); target.mkdir(parents=True, exist_ok=False)
    (target / OUTPUT).write_bytes(rendered)
    return {"source_pins": {str(path): hashlib.sha256(original).hexdigest()},
            "outputs": {OUTPUT: hashlib.sha256(rendered).hexdigest()},
            "experimental_only": True, "scope": "Expose existing source-only path trace; no scheduler mutation"}
