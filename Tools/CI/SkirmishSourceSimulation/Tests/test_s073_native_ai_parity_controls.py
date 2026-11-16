"""Negative control for the bounded native/source AI replay comparator."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile


def digest(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--repo", required=True, type=Path)
    p.add_argument("--executable", required=True, type=Path)
    p.add_argument("--placement-dir", required=True, type=Path)
    p.add_argument("--graph", required=True, type=Path)
    p.add_argument("--snapshot-dir", required=True, type=Path)
    args = p.parse_args()
    root = args.repo.resolve(strict=True)
    with tempfile.TemporaryDirectory() as tmp:
        target = Path(tmp)
        for name in ("snapshot.json", "editor.log", "snapshot-receipt.json"):
            shutil.copyfile(args.snapshot_dir / name, target / name)
        snapshot_path = target / "snapshot.json"
        snapshot = json.loads(snapshot_path.read_text())
        snapshot["ai_build_plans"][0]["entries_ordered"][0] = "deliberately_wrong_fixture"
        snapshot_path.write_text(json.dumps(snapshot))
        receipt_path = target / "snapshot-receipt.json"
        receipt = json.loads(receipt_path.read_text())
        receipt["snapshot_sha256"] = digest(snapshot_path)
        receipt_path.write_text(json.dumps(receipt))
        env = os.environ.copy()
        env["MONO_PATH"] = os.pathsep.join(map(str, [args.placement_dir.resolve(), root / "Library/ScriptAssemblies",
            "/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/Resources/Scripting/Managed/UnityEngine",
            root / "Library/PackageCache/com.unity.nuget.newtonsoft-json@4dfd81071c64/Runtime"]))
        mono = "/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/Resources/Scripting/MonoBleedingEdge/bin/mono"
        command = [mono, "--optimize=-float32", str(args.executable.resolve()), str(root), str(args.graph.resolve()),
                   digest(args.graph), str(receipt_path), digest(receipt_path)]
        result = subprocess.run(command, env=env, capture_output=True, text=True, timeout=120)
        if result.returncode == 0 or "Observed AI build plan differs" not in result.stderr:
            raise AssertionError("Changed native plan was not rejected: " + result.stdout + result.stderr)
    print("[S073NativeAIParityNegativeControl] result=Passed scope=FixtureTamper_NotMissionReadiness")


if __name__ == "__main__": main()
