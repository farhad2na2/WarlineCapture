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


def make_overlay(root, config_source):
    checkout = Path(__file__).resolve().parents[4]
    fixture = checkout / "Tools/CI/SkirmishSourceSimulation/Fixtures/S073/retained"
    manifest = json.loads((fixture / "provenance.json").read_text())
    audit = json.loads((fixture / "source-asset-reference-audit.json").read_text())
    identity = json.loads((fixture / "source-object-names.json").read_text())
    overlay = root / "source-overlay"
    required_paths = {row["path"] for row in audit["configuration_roots"]}
    required_paths.update(path for path in identity.get("source_pins", {})
                          if path.startswith("Assets/Game/Prefabs/"))
    for relative in sorted(required_paths):
        destination = overlay / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        source = Path(config_source) if relative == "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset" else checkout / relative
        if source.is_file():
            if relative == "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset":
                shutil.copyfile(source, destination)
            else:
                destination.symlink_to(source)
        else:
            raise RuntimeError("Readback control source missing: " + str(source))
    parser_rel = manifest["readback_parser"]["path"]
    parser_target = overlay / parser_rel
    parser_target.parent.mkdir(parents=True, exist_ok=True)
    parser_target.symlink_to(checkout / parser_rel)
    return checkout, fixture, overlay


def main():
    checkout = Path(__file__).resolve().parents[4]
    fixture = checkout / "Tools/CI/SkirmishSourceSimulation/Fixtures/S073/retained"
    manifest = json.loads((fixture / "provenance.json").read_text())
    baseline = fixture / manifest["parity_baseline"]["path"]
    if sha(baseline) != manifest["parity_baseline"]["sha256"]:
        raise RuntimeError("Original InitialForces parity fixture hash changed")

    with tempfile.TemporaryDirectory(prefix="s073-readback-controls-") as temporary:
        root = Path(temporary)
        _, _, overlay = make_overlay(root, baseline)

        parity = run_generator(overlay, root / "parity-output", "--verify-retained")
        if parity.returncode != 0:
            raise RuntimeError("Unchanged-source parity control failed:\n" + parity.stderr[-3000:])
        parity_result = json.loads(parity.stdout)
        if parity_result.get("initial_spawn_matches_retained") is not True:
            raise RuntimeError("Unchanged-source parity control did not match retained initial_spawn")

        current_overlay = root / "current-source-overlay"
        current_overlay_root, _, current_overlay = make_overlay(current_overlay, checkout /
            "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset")
        current = run_generator(current_overlay, root / "current-output", "--verify-retained")
        if current.returncode != 0:
            raise RuntimeError("Exact current F1/F2 placement overlay failed:\n" + current.stderr[-3000:])
        current_result = json.loads(current.stdout)
        current_config = json.loads((root / "current-output/legacy-initial-spawn-data.json").read_text())
        f1_offsets = [(row["OriginOffsetX"], row["OriginOffsetY"])
                      for row in current_config["initial_spawn"]["Factions"][0]["Buildings"]]
        if f1_offsets != [(6, 35), (37, 22), (15, 22), (11, 4), (40, -16)]:
            raise RuntimeError("Current readback did not preserve the exact five F1 terrain-fix offsets")

        source_config = current_overlay / "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset"
        current_text = source_config.read_text()
        unexpected_config = root / "unexpected-config.asset"
        changed = current_text.replace("  blockerCount: 0", "  blockerCount: 1", 1)
        if changed == current_text:
            raise RuntimeError("Could not prepare unrelated-field negative control")
        unexpected_config.write_text(changed)
        _, _, unexpected_overlay = make_overlay(root / "unexpected-overlay", unexpected_config)
        unexpected = run_generator(unexpected_overlay, root / "unexpected-output")
        if unexpected.returncode == 0 or "exact authorized F1/F2 offset overlay" not in unexpected.stderr:
            raise RuntimeError("Unrelated InitialForces field change was not rejected:\n" + unexpected.stderr[-2000:])

        bad_f1 = root / "bad-f1-config.asset"
        changed = current_text.replace("originOffset: {x: 6, y: 35}", "originOffset: {x: 7, y: 35}", 1)
        if changed == current_text:
            raise RuntimeError("Could not prepare F1-offset negative control")
        bad_f1.write_text(changed)
        _, _, bad_f1_overlay = make_overlay(root / "bad-f1-overlay", bad_f1)
        bad_f1_result = run_generator(bad_f1_overlay, root / "bad-f1-output")
        if bad_f1_result.returncode == 0 or "exact authorized five-offset vectors" not in bad_f1_result.stderr:
            raise RuntimeError("Unapproved F1 offset value was not rejected:\n" + bad_f1_result.stderr[-2000:])

        def f2_variant(suffix, replacements):
            marker = "  - factionId: 2"
            before, found, after = current_text.partition(marker)
            if not found:
                raise RuntimeError("Could not locate faction two for F2 control")
            for old, new in replacements:
                if old not in after:
                    raise RuntimeError("Could not prepare F2 offset control: " + old)
                after = after.replace(old, new, 1)
            path = root / (suffix + ".asset")
            path.write_text(before + found + after)
            _, _, variant_overlay = make_overlay(root / suffix, path)
            result = run_generator(variant_overlay, root / (suffix + "-output"))
            expected = "exact authorized five-offset vectors"
            if result.returncode == 0 or expected not in result.stderr:
                raise RuntimeError(suffix + " F2 offset mutation was not rejected:\n" + result.stderr[-2000:])

        f2_variant("f2-tamper", [("originOffset: {x: 57, y: -32}",
                                  "originOffset: {x: 58, y: -32}")])
        f2_variant("f2-reversion", [
            ("originOffset: {x: 57, y: -32}", "originOffset: {x: 4, y: -26}"),
            ("originOffset: {x: -9, y: -25}", "originOffset: {x: -24, y: 8}"),
            ("originOffset: {x: 20, y: -32}", "originOffset: {x: 8, y: 10}"),
            ("originOffset: {x: -24, y: 40}", "originOffset: {x: -24, y: 42}"),
        ])

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
                          "current_f1_f2_overlay_accepted": True,
                          "unrelated_config_change_rejected": True,
                          "unapproved_f1_offset_rejected": True,
                          "f2_offset_tamper_rejected": True,
                          "f2_offset_reversion_rejected": True,
                          "tampered_identity_rejected": True,
                          "tampered_identity_sha256": sha(tampered_identity),
                          "world_initialized": False, "mission_outcome_derived": False}, indent=2))


if __name__ == "__main__":
    main()
