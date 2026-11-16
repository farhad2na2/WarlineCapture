"""Qualify instrumented candidate observations only after the Unity wrapper exits."""
import argparse
import hashlib
import json
from pathlib import Path
import re


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    p = argparse.ArgumentParser(); p.add_argument("--directory", required=True); p.add_argument("--repo", required=True)
    p.add_argument("--wrapper-exit", required=True, type=int); a = p.parse_args(); root = Path(a.directory); repo = Path(a.repo).resolve()
    if a.wrapper_exit != 0: raise ValueError("Checked Unity wrapper did not exit successfully")
    log_path = root / "editor.log"; log = log_path.read_text(); census_path = root / "census.json"; d = json.loads(census_path.read_text())
    if ("[S073PostStreamCensus] result=Passed ecsLoaded=1 ownedGrid=1 entities=4931" not in log
            or d.get("status") != "Passed" or d.get("mission_ready") is not False):
        raise ValueError("Completed same-session observation required")
    pattern = re.compile(r'^\[S073CandidatePlacement\] building=(\S+) origin=\((\d+), (\d+)\) rect=\(x:(\d+), y:(\d+), width:(\d+), height:(\d+)\) road=(True|False) water=(True|False) originalValid=(True|False) scope=InstrumentedPreSpawnCandidate_NotMissionReadiness$')
    matches = [pattern.fullmatch(line) for line in log.splitlines() if line.startswith("[S073CandidatePlacement]")]
    if len(matches) != 4 or any(row is None for row in matches): raise ValueError("Four exact candidate records required")
    expected = {("building_barrack", 828, 462, 28, 15), ("building_barrack", 951, 558, 28, 15),
                ("building_ammunition_depot", 867, 444, 19, 16), ("building_ammunition_depot", 867, 462, 19, 16)}
    records = []
    for match in matches:
        name, x, y, rx, ry, w, h, road, water, valid = match.groups(); x, y, rx, ry, w, h = map(int, (x,y,rx,ry,w,h))
        key = (name,x,y,w,h)
        if (rx,ry) != (x,y) or key not in expected: raise ValueError("Candidate identity or rectangle changed")
        expected.remove(key); records.append({"building":name,"origin":[x,y],"footprint":[w,h],"road":road=="True","water":water=="True","original_valid":valid=="True"})
    pins = d.get("source_pins") or {}; scene = d["post_stream"]["source_scene_path"]
    if scene not in pins: raise ValueError("Native runtime scene pin required")
    for relative, digest in pins.items():
        path = (repo / relative).resolve()
        if not path.is_relative_to(repo) or sha(path) != digest: raise ValueError("Native source pin changed: " + relative)
    result = {"kind":"S073NativeCandidateObservation","validation_status":"Passed","wrapper_exit_code":0,
              "native_log_sha256":sha(log_path),"native_census_sha256":sha(census_path),"candidates":records,
              "scope":"Instrumented pre-spawn predicates only; no authored placement, mission completion or readiness",
              "mission_ready":False}
    with (root / "candidate-receipt.json").open("x") as f: json.dump(result,f,indent=2); f.write("\n")
    print(json.dumps(result))


if __name__ == "__main__": main()
