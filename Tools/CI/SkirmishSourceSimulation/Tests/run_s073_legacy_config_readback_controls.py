#!/usr/bin/env python3
"""Run bounded parity and tampered-identity controls for the S073 readback."""
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def run_generator(repo, output, *extra):
    script = Path(__file__).with_name("generate_s073_legacy_config_readback.py")
    return subprocess.run([sys.executable, str(script), "--repo", str(repo),
                           "--output-dir", str(output), *map(str, extra)],
                          text=True, capture_output=True)


def main():
    checkout = Path(__file__).resolve().parents[4]
    fixture = checkout / "Tools/CI/SkirmishSourceSimulation/Fixtures/S073/retained"
    manifest = json.loads((fixture / "provenance.json").read_text())
    baseline = fixture / manifest["parity_baseline"]["path"]
    if sha(baseline) != manifest["parity_baseline"]["sha256"]:
        raise RuntimeError("Original InitialForces parity fixture hash changed")

    with tempfile.TemporaryDirectory(prefix="s073-readback-controls-") as temporary:
        root = Path(temporary)
        overlay = root / "source-overlay"
        audit = json.loads((fixture / "source-asset-reference-audit.json").read_text())
        identity = json.loads((fixture / "source-object-names.json").read_text())
        required_paths = {row["path"] for row in audit["configuration_roots"]}
        required_paths.update(path for path in identity.get("source_pins", {})
                              if path.startswith("Assets/Game/Prefabs/"))
        for relative in sorted(required_paths):
            destination = overlay / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            if relative == "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset":
                shutil.copyfile(baseline, destination)
            else:
                source = checkout / relative
                if not source.is_file():
                    raise RuntimeError("Parity control source missing: " + relative)
                destination.symlink_to(source)
        parser_rel = manifest["readback_parser"]["path"]
        parser_target = overlay / parser_rel
        parser_target.parent.mkdir(parents=True, exist_ok=True)
        parser_target.symlink_to(checkout / parser_rel)

        parity = run_generator(overlay, root / "parity-output", "--verify-retained")
        if parity.returncode != 0:
            raise RuntimeError("Unchanged-source parity control failed:\n" + parity.stderr[-3000:])
        parity_result = json.loads(parity.stdout)
        if parity_result.get("initial_spawn_matches_retained") is not True:
            raise RuntimeError("Unchanged-source parity control did not match retained initial_spawn")

        tampered_identity = root / "tampered-source-object-names.json"
        tampered = json.loads((fixture / "source-object-names.json").read_text())
        tampered["authority"] += " tampered-control"
        tampered_identity.write_text(json.dumps(tampered, sort_keys=True) + "\n")
        rejected = run_generator(checkout, root / "tampered-output",
                                 "--retained-identity", tampered_identity)
        expected_error = "differs from its durable provenance pin: source-object-names.json"
        if rejected.returncode == 0 or expected_error not in rejected.stderr:
            raise RuntimeError("Tampered identity hash was not rejected as expected:\n" +
                               rejected.stdout[-1500:] + rejected.stderr[-1500:])

        print(json.dumps({"status": "Passed", "kind": "S073LegacyConfigReadbackControls",
                          "unchanged_source_parity": parity_result.get("initial_spawn_matches_retained"),
                          "tampered_identity_rejected": True,
                          "tampered_identity_sha256": sha(tampered_identity),
                          "world_initialized": False, "mission_outcome_derived": False}, indent=2))


if __name__ == "__main__":
    main()
